using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Generators.Bindings
{
    internal class BindingTransformationResult
    {
        public readonly string SpecifiedName;
        
        public readonly INamedTypeSymbol Symbol;
        public readonly ClassDeclarationSyntax SyntaxNode;
        public readonly Compilation Compilation;
        
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool InheritBaseMembers { get; set; }
        
        public readonly List<MemberInfo<IFieldSymbol>> Fields = new List<MemberInfo<IFieldSymbol>>();
        public readonly List<MemberInfo<IPropertySymbol>> Properties = new List<MemberInfo<IPropertySymbol>>();
        public readonly List<MemberInfo<IMethodSymbol>> Methods = new List<MemberInfo<IMethodSymbol>>();

        public BindingTransformationResult(string specifiedName, INamedTypeSymbol symbol, ClassDeclarationSyntax syntaxNode, Compilation compilation) 
        {
            SpecifiedName = specifiedName;
            Symbol = symbol;
            SyntaxNode = syntaxNode;
            Compilation = compilation;
        }
    }

    internal class MemberInfo<T> where T : ISymbol
    {
        public readonly string SpecifiedName;
        public readonly string ActualName;
        
        public readonly T Symbol;
        public readonly AttributeData AttributeData;
        
        public MemberInfo(string specifiedName, T symbol, AttributeData attributeData)
        {
            SpecifiedName = specifiedName;
            ActualName = symbol.Name;
            Symbol = symbol;
            AttributeData = attributeData;
        }
    }
}