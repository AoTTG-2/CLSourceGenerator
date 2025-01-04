using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Extensions
{
    public static partial class Extensions
    {
        public static string ToSource(this SyntaxNodeScopePrinter printer)
        {
            return printer.Printer.Result;
        }

        public static Printer PrintCompilerGeneratedAttribute(this Printer printer)
        {
            return printer.PrintLine("[global::System.Runtime.CompilerServices.CompilerGenerated]");
        }
        
        public static Printer PrintClassDeclaration(this Printer printer, string name, ClassDeclarationSyntax node)
        {
            var modifiers = node.Modifiers.Select(m => m.ToString());
            printer.PrintBeginLine();
            printer.Print(string.Join(" ", modifiers));
            printer.Print(" class ").PrintEndLine(name);
            return printer;
        }
    }
}