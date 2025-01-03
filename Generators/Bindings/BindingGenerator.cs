using System.Threading;
using CustomLogicSourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Generators.Bindings
{
    [Generator]
    internal class BindingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxProvider = context.SyntaxProvider
                .CreateSyntaxProvider(ProvideCandidate, Transform)
                .Where(x => x != null);

            context.RegisterSourceOutput(syntaxProvider, Emit);
        }

        public bool ProvideCandidate(SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (node is ClassDeclarationSyntax classDeclarationSyntax)
            {
                return classDeclarationSyntax.AttributeLists.Count > 0;
            }

            return false;
        }

        public BindingTransformationResult Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(node, cancellationToken);

            if (declaredSymbol is INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.HasAttribute(Types.Attributes.CLType, out var data))
                {
                    var specifiedTypeName = Types.Attributes.GetSpecifiedNameOrDefault(data, typeSymbol.Name);
                    var isStatic  = Types.Attributes.GetIsStaticOrDefault(data, false);
                    var isAbstract = Types.Attributes.GetIsAbstractOrDefault(data, false);
                    var inheritBaseMembers = Types.Attributes.GetInheritBaseMembersOrDefault(data, true);
                    
                    var result = new BindingTransformationResult(specifiedTypeName, typeSymbol, node, context.SemanticModel.Compilation)
                    {
                        IsStatic = isStatic,
                        IsAbstract = isAbstract,
                        InheritBaseMembers = inheritBaseMembers
                    };
                    
                    foreach (var member in typeSymbol.GetMembers())
                    {
                        if (member is IFieldSymbol fieldSymbol)
                        {
                            if (fieldSymbol.HasAttribute(Types.Attributes.CLProperty, out var attributeData))
                            {
                                var name = Types.Attributes.GetSpecifiedNameOrDefault(attributeData, fieldSymbol.Name);
                                var info = new MemberInfo<IFieldSymbol>(name, fieldSymbol, attributeData);
                                result.Fields.Add(info);
                            }
                        }
                        else if (member is IPropertySymbol propertySymbol)
                        {
                            if (propertySymbol.HasAttribute(Types.Attributes.CLProperty, out var attributeData))
                            {
                                var name = Types.Attributes.GetSpecifiedNameOrDefault(attributeData, propertySymbol.Name);
                                var info = new MemberInfo<IPropertySymbol>(name, propertySymbol, attributeData);
                                result.Properties.Add(info);
                            }
                        }
                        else if (member is IMethodSymbol methodSymbol)
                        {
                            if (methodSymbol.HasAttribute(Types.Attributes.CLMethod, out var attributeData)
                                || methodSymbol.HasAttribute(Types.Attributes.CLConstructor, out attributeData))
                            {
                                var name = Types.Attributes.GetSpecifiedNameOrDefault(attributeData, methodSymbol.Name);
                                var info = new MemberInfo<IMethodSymbol>(name, methodSymbol, attributeData);
                                result.Methods.Add(info);
                            }
                        }
                    }
                    return result;
                }
            }

            return null;
        }

        public void Emit(SourceProductionContext context, BindingTransformationResult result)
        {
            BindingEmitter.Emit(context, result);
        }
    }
}