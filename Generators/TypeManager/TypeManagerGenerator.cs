using System.Collections.Generic;
using CustomLogicSourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Generators.TypeManager
{
    [Generator]
    internal class TypeManagerGenerator : ISourceGenerator
    {
        private readonly List<BuiltinType> _builtinTypes = new List<BuiltinType>();

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is SyntaxReceiver syntaxReceiver == false)
            {
                return;
            }

            ITypeSymbol typeManagerSymbol = null;
            ClassDeclarationSyntax classDeclarationSyntax = null;
            
            _builtinTypes.Clear();
            foreach (var classSyntax in syntaxReceiver.CandidateTypes)
            {
                var model = context.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
                var declaredSymbol = model.GetDeclaredSymbol(classSyntax);

                if (declaredSymbol is ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.HasAttribute(Types.Attributes.BuiltinTypeManager))
                    {
                        typeManagerSymbol = typeSymbol;
                        classDeclarationSyntax = classSyntax;
                    }
                    else if (typeSymbol.HasAttribute(Types.Attributes.CLType, out var attributeData))
                    {
                        var name = Types.Attributes.GetSpecifiedNameOrDefault(attributeData, typeSymbol.Name);
                        var isStatic  = Types.Attributes.GetIsStaticOrDefault(attributeData, false);
                        var isAbstract = Types.Attributes.GetIsAbstractOrDefault(attributeData, false);
                        var inheritBaseMembers = Types.Attributes.GetInheritBaseMembersOrDefault(attributeData, true);

                        var builtinType = new BuiltinType(name, typeSymbol, classSyntax)
                        {
                            IsStatic = isStatic,
                            IsAbstract = isAbstract,
                            InheritBaseMembers = inheritBaseMembers
                        };
                        _builtinTypes.Add(builtinType);
                    }
                }
            }
            
            if (typeManagerSymbol != null)
            {
                var source = TypeManagerEmitter.Emit(typeManagerSymbol, classDeclarationSyntax, _builtinTypes);
                context.AddSource($"{typeManagerSymbol.ToValidIdentifier()}.g.cs", source);
            }
        }
    }

    internal class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateTypes { get; } = new List<ClassDeclarationSyntax>();
        
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                if (classDeclarationSyntax.AttributeLists.Any())
                {
                    CandidateTypes.Add(classDeclarationSyntax);
                }
            }
        }
    }

    internal class BuiltinType
    {
        /// <summary>
        /// Name of the type specified in the attribute.
        /// For example: [CLType(Name = "Vector3")]
        /// </summary>
        public readonly string SpecifiedName;
        public readonly string ActualTypeName;
        
        public readonly ITypeSymbol Symbol;
        public readonly ClassDeclarationSyntax SyntaxNode;
        
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool InheritBaseMembers { get; set; }

        public BuiltinType(string specifiedName, ITypeSymbol symbol, ClassDeclarationSyntax syntaxNode)
        {
            SpecifiedName = specifiedName;
            ActualTypeName = symbol.Name;
            Symbol = symbol;
            SyntaxNode = syntaxNode;
        }
    }
}