// https://github.com/needle-mirror/com.unity.entities/blob/master/Unity.Entities/SourceGenerators/Source~/Common/SymbolExtensions.cs

using System.Linq;
using Microsoft.CodeAnalysis;

namespace CustomLogicSourceGen.Extensions
{
    public static partial class Extensions
    {
        private static SymbolDisplayFormat QualifiedFormat { get; } = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static SymbolDisplayFormat QualifiedFormatWithoutGlobalPrefix { get; } = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static bool Is(this ITypeSymbol symbol, string fullyQualifiedName, bool checkBaseType = true)
        {
            fullyQualifiedName = PrependGlobalIfMissing(fullyQualifiedName);

            if (symbol is null)
                return false;

            if (symbol.ToDisplayString(QualifiedFormat) == fullyQualifiedName)
                return true;

            return checkBaseType && symbol.BaseType.Is(fullyQualifiedName);
        }

        public static string ToFullName(this ITypeSymbol symbol) => symbol.ToDisplayString(QualifiedFormat);

        public static string ToValidIdentifier(this ITypeSymbol symbol) =>
            symbol.ToDisplayString(QualifiedFormatWithoutGlobalPrefix).Replace('.', '_');

        public static bool HasAttribute(this ISymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.GetAttributes()
                .Any(attribute => attribute.AttributeClass.ToFullName() == fullyQualifiedAttributeName);
        }

        public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName,
            out AttributeData attributeData)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);
            foreach (var data in symbol.GetAttributes())
            {
                if (data.AttributeClass.ToFullName() == fullyQualifiedAttributeName)
                {
                    attributeData = data;
                    return true;
                }
            }

            attributeData = null;
            return false;
        }

        private static string PrependGlobalIfMissing(this string typeOrNamespaceName) =>
            !typeOrNamespaceName.StartsWith("global::") ? $"global::{typeOrNamespaceName}" : typeOrNamespaceName;
    }
}