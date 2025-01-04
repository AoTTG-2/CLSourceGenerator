using System.Linq;
using CustomLogicSourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CustomLogicSourceGen.Generators.Bindings
{
    internal static class BindingEmitter
    {
        public const string FactoryClassName = "Factory";
        public const string CreateInstanceMethodName = "CreateInstance";
        public const string BindingsClassName = "Bindings";
        public const string CreateBindingMethodName = "CreateMemberBinding";

        internal static void Emit(SourceProductionContext context, BindingTransformationResult transformationResult)
        {
            var name = transformationResult.Symbol.ToValidIdentifier();
            var source = GenerateSource(transformationResult);

            context.AddSource($"{name}.g.cs", source);
        }

        private static string GenerateSource(BindingTransformationResult result)
        {
            var scopePrinter = new SyntaxNodeScopePrinter(Printer.Default, result.SyntaxNode.Parent);
            scopePrinter.PrintOpen();

            var printer = scopePrinter.Printer;
            var className = result.Symbol.Name;

            printer.PrintCompilerGeneratedAttribute();
            printer.PrintClassDeclaration(className, result.SyntaxNode);
            printer.OpenScope();
            {
                printer.PrintLine($"public override string ClassName => \"{result.SpecifiedName}\";");
                printer.PrintLine($"public override bool IsAbstract => {(result.IsAbstract ? "true" : "false")};");
                printer.PrintLine($"public override bool IsStatic => {(result.IsStatic ? "true" : "false")};");
                printer.PrintLine($"public override bool InheritBaseMembers => {(result.InheritBaseMembers ? "true" : "false")};");

                PrintFactoryClass(printer, className, result);

                printer.PrintLine($"public new static class {BindingsClassName}");
                printer.OpenScope();
                {
                    printer.PrintLine($"public static readonly {Types.HashSet}<string> MemberNames = new {Types.HashSet}<string>");
                    printer.OpenScope();
                    {
                        foreach (var member in result.Fields)
                        {
                            printer.PrintLine($"\"{member.SpecifiedName}\",");
                        }

                        foreach (var member in result.Properties)
                        {
                            printer.PrintLine($"\"{member.SpecifiedName}\",");
                        }

                        foreach (var member in result.Methods)
                        {
                            if (member.AttributeData.AttributeClass.Is(Types.Attributes.CLMethod) == false)
                                continue;

                            printer.PrintLine($"\"{member.SpecifiedName}\",");
                        }
                    }
                    printer.CloseScope("};");
                    PrintCreateMemberBinding(printer, className, result);
                    PrintCreatePropertyBindings(printer, className, result);
                    PrintCreateMethodBindings(printer, className, result);
                }
                printer.CloseScope();
            }
            printer.CloseScope();

            scopePrinter.PrintClose();
            return scopePrinter.ToSource();
        }

        private static void PrintFactoryClass(Printer printer, string className, BindingTransformationResult result)
        {
            printer.PrintLine($"public new static class {FactoryClassName}");
            printer.OpenScope();
            {
                printer.PrintLine($"public static {className} {CreateInstanceMethodName}(object[] args)");
                printer.OpenScope();
                {
                    // Constructor that takes a single object[] parameter.
                    IMethodSymbol paramsConstructor = null;

                    foreach (var method in result.Methods)
                    {
                        if (method.AttributeData.AttributeClass.Is(Types.Attributes.CLConstructor) == false)
                            continue;

                        if (method.Symbol.Parameters.Length == 1 && method.Symbol.Parameters[0].Type.ToFullName() == "object[]")
                        {
                            paramsConstructor = method.Symbol;
                            break;
                        }
                    }

                    foreach (var method in result.Methods)
                    {
                        if (method.AttributeData.AttributeClass.Is(Types.Attributes.CLConstructor) == false
                            || SymbolEqualityComparer.Default.Equals(method.Symbol, paramsConstructor))
                            continue;

                        printer.PrintLine($"if (args.Length == {method.Symbol.Parameters.Length})");
                        printer.OpenScope();
                        {
                            var parameters = PrintParameters(printer, method, "args");
                            printer.PrintLine($"return new {className}({parameters});");
                        }
                        printer.CloseScope();
                    }

                    if (paramsConstructor != null)
                    {
                        var paramName = paramsConstructor.Parameters[0].Name;
                        printer.PrintLine($"object[] {paramName} = args;");
                        printer.PrintLine($"return new {className}({paramName});");
                    }
                    else
                        printer.PrintLine($"throw new global::System.ArgumentException(\"No {className} constructor found that takes \" + args.Length + \" arguments\");");
                }
                printer.CloseScope();
            }
            printer.CloseScope();
        }

        private static void PrintCreateMemberBinding(Printer printer, string className, BindingTransformationResult result)
        {
            printer.PrintLine($"public static {Types.ICLMemberBinding} {CreateBindingMethodName}(string name)");
            printer.OpenScope();
            {
                printer.PrintLine("return name switch");
                printer.OpenScope();

                foreach (var property in result.Properties)
                {
                    var name = property.SpecifiedName;
                    printer.PrintLine($"\"{name}\" => __CreatePropertyBinding__{name}(),");
                }

                foreach (var method in result.Methods)
                {
                    if (method.AttributeData.AttributeClass.Is(Types.Attributes.CLMethod) == false)
                        continue;

                    var name = method.SpecifiedName;
                    printer.PrintLine($"\"{name}\" => __CreateMethodBinding__{name}(),");
                }

                printer.PrintLine($"_ => throw new global::System.Exception($\"Binding for '{{name}}' in {className} not found\")");
                printer.CloseScope("};");
            }
            printer.CloseScope();
        }

        // todo: PrintCreateFieldBindings

        private static void PrintCreatePropertyBindings(Printer printer, string className, BindingTransformationResult result)
        {
            const string instanceParamName = "__i";
            const string valueParamName = "__v";

            const string getterName = "__getter";
            const string setterName = "__setter";

            foreach (var property in result.Properties)
            {
                var exposedName = property.SpecifiedName;
                var actualName = property.ActualName;
                var type = property.Symbol.Type.ToFullName();
                var isReadonly = Types.Attributes.GetIsReadOnlyOrDefault(property.AttributeData, false);

                printer.PrintLine($"public static {Types.CLPropertyBinding}<{className}> __CreatePropertyBinding__{exposedName}()");
                printer.OpenScope();
                {
                    var getter = "null";
                    var setter = "null";

                    var instance = property.Symbol.IsStatic ? className : instanceParamName;

                    if (property.Symbol.GetMethod != null)
                    {
                        getter = getterName;
                        printer.PrintBeginLine($"static object {getterName}({className} {instanceParamName})");
                        printer.PrintEndLine($" => {instance}.{actualName};");
                    }

                    if (property.Symbol.SetMethod != null && isReadonly == false)
                    {
                        setter = setterName;
                        printer.PrintBeginLine($"static void {setterName}({className} {instanceParamName}, object {valueParamName})");
                        printer.PrintEndLine($" => {instance}.{actualName} = global::CustomLogic.CustomLogicEvaluator.ConvertTo<{type}>({valueParamName});");
                    }

                    printer.PrintLine($"return new {Types.CLPropertyBinding}<{className}>({getter}, {setter});");
                }
                printer.CloseScope();
            }
        }

        private static void PrintCreateMethodBindings(Printer printer, string className, BindingTransformationResult result)
        {
            const string classInstanceParamName = "__c";
            const string argsParamName = "__a";

            foreach (var method in result.Methods)
            {
                if (method.AttributeData.AttributeClass.Is(Types.Attributes.CLMethod) == false)
                    continue;

                var exposedName = method.SpecifiedName;
                var actualName = method.ActualName;

                printer.PrintLine($"public static {Types.CLMethodBinding}<{className}> __CreateMethodBinding__{exposedName}()");
                printer.OpenScope();
                {
                    printer.PrintLine($"return new {Types.CLMethodBinding}<{className}>(({classInstanceParamName}, {argsParamName}) =>");
                    printer.OpenScope();
                    {
                        var parameters = PrintParameters(printer, method, argsParamName);
                        var prefix = method.Symbol.IsStatic ? className : classInstanceParamName;

                        if (method.Symbol.ReturnsVoid)
                        {
                            printer.PrintLine($"{prefix}.{actualName}({parameters});");
                            printer.PrintLine("return null;");
                        }
                        else
                        {
                            printer.PrintLine($"return {prefix}.{actualName}({parameters});");
                        }
                    }
                    printer.CloseScope("});");
                }
                printer.CloseScope();
            }
        }

        private static string PrintParameters(Printer printer, MemberInfo<IMethodSymbol> method, string argsParamName)
        {
            if (method.Symbol.Parameters.Length == 1 && method.Symbol.Parameters[0].Type.ToFullName() == "object[]")
            {
                var paramName = method.Symbol.Parameters[0].Name;
                printer.PrintLine($"object[] {paramName} = {argsParamName};");
            }
            else
            {
                foreach (var parameter in method.Symbol.Parameters)
                {
                    var name = parameter.Name;
                    var type = parameter.Type.ToFullName();
                    var value = "default";

                    if (parameter.IsOptional == false)
                    {
                        value = type != "object"
                            ? $"{Types.Evaluator}.ConvertTo<{type}>({argsParamName}[{parameter.Ordinal}])"
                            : $"{argsParamName}[{parameter.Ordinal}]";
                    }
                    else if (parameter.HasExplicitDefaultValue)
                    {
                        if (parameter.ExplicitDefaultValue != null)
                        {
                            value = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, true, false);
                            if (type == "float")
                                value += "f";
                        }
                        else
                            value = "null";
                    }

                    printer.PrintLine($"{type} {name} = {value};");
                }

                foreach (var parameter in method.Symbol.Parameters)
                {
                    var type = parameter.Type.ToFullName();

                    if (parameter.IsOptional)
                    {
                        printer.PrintLine($"if ({argsParamName}.Length > {parameter.Ordinal})");
                        printer.OpenScope();
                        {
                            var value = type != "object"
                                ? $"{Types.Evaluator}.ConvertTo<{type}>({argsParamName}[{parameter.Ordinal}])"
                                : $"{argsParamName}[{parameter.Ordinal}]";
                            printer.PrintLine($"{parameter.Name} = {value};");
                        }
                        printer.CloseScope();
                    }
                }
            }

            var parameters = string.Join(", ", method.Symbol.Parameters.Select(x => x.Name));
            return parameters;
        }
    }
}