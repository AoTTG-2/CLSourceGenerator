using Microsoft.CodeAnalysis;

namespace CustomLogicSourceGen
{
    public static class Types
    {
        public const string HashSet = "global::System.Collections.Generic.HashSet";
        public const string Dictionary = "global::System.Collections.Generic.Dictionary";
        
        private const string CLRootNamespace = "global::CustomLogic.";
        
        public const string Evaluator = CLRootNamespace + "CustomLogicEvaluator";
        public const string ICLMemberBinding = CLRootNamespace + "ICLMemberBinding";
        public const string CLPropertyBinding = CLRootNamespace + "CLPropertyBinding";
        public const string CLMethodBinding = CLRootNamespace + "CLMethodBinding";
        
        public const string BuiltinClassInstance = CLRootNamespace + "BuiltinClassInstance";
        
        public static class Attributes
        {
            public const string CLType = CLRootNamespace + "CLTypeAttribute";
            public const string CLConstructor = CLRootNamespace + "CLConstructorAttribute";
            public const string CLProperty = CLRootNamespace + "CLPropertyAttribute";
            public const string CLMethod = CLRootNamespace + "CLMethodAttribute";
            public const string BuiltinTypeManager = CLRootNamespace + "BuiltinTypeManagerAttribute";
            
            /// <summary>
            /// Reads the value of the named argument "Name" from the attribute and returns its value.
            /// </summary>
            public static string GetSpecifiedNameOrDefault(AttributeData attributeData, string defaultValue)
            {
                return GetNamedArgumentValueOrDefault(attributeData, "Name", defaultValue)?.ToString() ?? defaultValue;
            }

            /// <summary>
            /// Reads the value of the named argument "ReadOnly" from the attribute and returns its value.
            /// </summary>
            public static bool GetIsReadOnlyOrDefault(AttributeData attributeData, bool defaultValue)
            {
                return GetNamedArgumentValueOrDefault(attributeData, "ReadOnly", defaultValue) is bool value
                    ? value
                    : defaultValue;
            }
            
            /// <summary>
            /// Reads the value of the named argument "Static" from the attribute and returns its value.
            /// </summary>
            public static bool GetIsStaticOrDefault(AttributeData attributeData, bool defaultValue)
            {
                return GetNamedArgumentValueOrDefault(attributeData, "Static", defaultValue) is bool value
                    ? value
                    : defaultValue;
            }
            
            /// <summary>
            /// Reads the value of the named argument "Abstract" from the attribute and returns its value.
            /// </summary>
            public static bool GetIsAbstractOrDefault(AttributeData attributeData, bool defaultValue)
            {
                return GetNamedArgumentValueOrDefault(attributeData, "Abstract", defaultValue) is bool value
                    ? value
                    : defaultValue;
            }
            
            /// <summary>
            /// Reads the value of the named argument "InheritBaseMembers" from the attribute and returns its value.
            /// </summary>
            public static bool GetInheritBaseMembersOrDefault(AttributeData attributeData, bool defaultValue)
            {
                return GetNamedArgumentValueOrDefault(attributeData, "InheritBaseMembers", defaultValue) is bool value
                    ? value
                    : defaultValue;
            }
            
            private static object GetNamedArgumentValueOrDefault(AttributeData attributeData, string name, object defaultValue)
            {
                if (attributeData.NamedArguments.IsEmpty)
                    return defaultValue;
                
                foreach (var arg in attributeData.NamedArguments)
                {
                    if (arg.Key == name)
                    {
                        return arg.Value.Value;
                    }
                }
                
                return defaultValue;
            }
        }
    }
}