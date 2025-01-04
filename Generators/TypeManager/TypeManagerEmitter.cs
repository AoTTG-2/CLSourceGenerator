using System.Collections.Generic;
using CustomLogicSourceGen.Extensions;
using CustomLogicSourceGen.Generators.Bindings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Generators.TypeManager
{
    internal static class TypeManagerEmitter
    {
        public static string Emit(ITypeSymbol typeManagerSymbol, ClassDeclarationSyntax node, List<BuiltinType> builtinTypes)
        {
            var scopePrinter = new SyntaxNodeScopePrinter(Printer.Default, node.Parent);
            scopePrinter.PrintOpen();

            var printer = scopePrinter.Printer;
            var className = typeManagerSymbol.Name;

            printer.PrintCompilerGeneratedAttribute();
            printer.PrintClassDeclaration(className, node);
            printer.OpenScope();
            {
                printer.PrintLine($"private static {className} __selfForEasyReferencing;");
                
                PrintTypeNamesHashSet(builtinTypes, printer);
                PrintBaseTypeNamesDictionary(builtinTypes, printer);
                PrintMemberNamesDictionary(builtinTypes, printer);
                
                PrintStaticTypeNamesHashSet(builtinTypes, printer);
                PrintAbstractTypeNamesHashSet(builtinTypes, printer);

                PrintCreateBindingMethod(printer, builtinTypes);
                PrintCreateClassInstance(printer, builtinTypes);
                PrintCreateFactoryMethod(printer, builtinTypes);
            }
            printer.CloseScope();
            
            scopePrinter.PrintClose();
            return scopePrinter.ToSource();
        }

        private static void PrintTypeNamesHashSet(List<BuiltinType> builtinTypes, Printer printer)
        {
            printer.PrintLine($"public static readonly {Types.HashSet}<string> TypeNames = new {Types.HashSet}<string>");
            printer.OpenScope();
            {
                foreach (var type in builtinTypes)
                {
                    printer.PrintLine($"\"{type.SpecifiedName}\",");
                }
            }
            printer.CloseScope("};");
        }

        private static void PrintBaseTypeNamesDictionary(List<BuiltinType> builtinTypes, Printer printer)
        {
            printer.PrintLine($"public static readonly {Types.Dictionary}<string, string> BaseTypeNames = new {Types.Dictionary}<string, string>()");
            printer.OpenScope();
            {
                foreach (var type in builtinTypes)
                {
                    if (type.Symbol.BaseType == null)
                        continue;

                    if (type.Symbol.BaseType.HasAttribute(Types.Attributes.CLType))
                    {
                        var baseType = builtinTypes.Find(x => SymbolEqualityComparer.Default.Equals(x.Symbol, type.Symbol.BaseType));
                        printer.PrintLine($"[\"{type.SpecifiedName}\"] = \"{baseType.SpecifiedName}\",");
                    }
                }
            }
            printer.CloseScope("};");
        }

        private static void PrintMemberNamesDictionary(List<BuiltinType> builtinTypes, Printer printer)
        {
            printer.PrintLine($"public static readonly {Types.Dictionary}<string, {Types.HashSet}<string>> MemberNames = new {Types.Dictionary}<string, {Types.HashSet}<string>>()");
            printer.OpenScope();
            {
                foreach (var type in builtinTypes)
                {
                    printer.PrintLine($"[\"{type.SpecifiedName}\"] = {type.Symbol.ToFullName()}.{BindingEmitter.BindingsClassName}.MemberNames,");
                }
            }
            printer.CloseScope("};");
        }
        
        private static void PrintStaticTypeNamesHashSet(List<BuiltinType> builtinTypes, Printer printer)
        {
            printer.PrintLine($"public static readonly {Types.HashSet}<string> StaticTypeNames = new {Types.HashSet}<string>");
            printer.OpenScope();
            {
                foreach (var type in builtinTypes)
                {
                    if (!type.IsStatic) continue;
                    printer.PrintLine($"\"{type.SpecifiedName}\",");
                }
            }
            printer.CloseScope("};");
        }
        
        private static void PrintAbstractTypeNamesHashSet(List<BuiltinType> builtinTypes, Printer printer)
        {
            printer.PrintLine($"public static readonly {Types.HashSet}<string> AbstractTypeNames = new {Types.HashSet}<string>");
            printer.OpenScope();
            {
                foreach (var type in builtinTypes)
                {
                    if (!type.IsAbstract) continue;
                    printer.PrintLine($"\"{type.SpecifiedName}\",");
                }
            }
            printer.CloseScope("};");
        }

        private static void PrintCreateClassInstance(Printer printer, List<BuiltinType> builtinTypes)
        {
            printer.PrintLine($"public static {Types.BuiltinClassInstance} CreateClassInstance(string typeName, object[] args)");
            printer.OpenScope();
            {
                printer.PrintLine("return typeName switch");
                printer.OpenScope();
                {
                    foreach (var type in builtinTypes)
                    {
                        printer.PrintBeginLine($"\"{type.SpecifiedName}\" => {type.Symbol.ToFullName()}");
                        printer.Print($".{BindingEmitter.FactoryClassName}.{BindingEmitter.CreateInstanceMethodName}");
                        printer.PrintEndLine("(args),");
                    }
                        
                    printer.PrintLine("_ => throw new global::System.Exception($\"Builtin type '{typeName}' does not exist\")");
                }
                printer.CloseScope("};");
            }
            printer.CloseScope();
        }

        private static void PrintCreateFactoryMethod(Printer printer, List<BuiltinType> builtinTypes)
        {
            printer.PrintLine($"public static global::System.Func<object[], {Types.BuiltinClassInstance}> CreateFactory(string typeName)");
            printer.OpenScope();
            {
                printer.PrintLine("return typeName switch");
                printer.OpenScope();
                {
                    foreach (var type in builtinTypes)
                    {
                        printer.PrintBeginLine($"\"{type.SpecifiedName}\" => (object[] args) => ");
                        printer.Print($"{type.Symbol.ToFullName()}.{BindingEmitter.FactoryClassName}.{BindingEmitter.CreateInstanceMethodName}");
                        printer.PrintEndLine("(args),");
                    }
                        
                    printer.PrintLine("_ => throw new global::System.Exception($\"Builtin type '{typeName}' does not exist\")");
                }
                printer.CloseScope("};");
            }
            printer.CloseScope();
        }

        private static void PrintCreateBindingMethod(Printer printer, List<BuiltinType> builtinTypes)
        {
            const string parameters = "string typeName, string varName";
            printer.PrintLine($"public static {Types.ICLMemberBinding} CreateBinding({parameters})");
            printer.OpenScope();
            {
                printer.PrintLine("return typeName switch");
                printer.OpenScope();
                {
                    foreach (var type in builtinTypes)
                    {
                        printer.PrintBeginLine($"\"{type.SpecifiedName}\" => {type.ActualTypeName}");
                        printer.Print($".{BindingEmitter.BindingsClassName}.{BindingEmitter.CreateBindingMethodName}");
                        printer.PrintEndLine("(varName),");
                    }
                        
                    printer.PrintLine("_ => throw new global::System.Exception($\"Builtin type '{typeName}' does not exist\")");
                }
                printer.CloseScope("};");
            }
            printer.CloseScope();
        }
    }
}