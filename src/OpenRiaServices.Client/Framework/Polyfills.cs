#if !NET
using System;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper methods to allow "newer" .NET methods on older frameworks
    /// </summary>
    static class Polyfills
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            // This is expected to be used in scenarios where the add will almost always succeed, so we pay the cost of an exception
            // on duplicates instead of checking if the Key exists first
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch (ArgumentException)
            {
                // Duplicate key
                return false;
            }
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    //
    // Summary:
    //     Specifies the types of members that are dynamically accessed. This enumeration
    //     has a System.FlagsAttribute attribute that allows a bitwise combination of its
    //     member values.
    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        //
        // Summary:
        //     Specifies all members.
        All = -1,
        //
        // Summary:
        //     Specifies no members.
        None = 0,
        //
        // Summary:
        //     Specifies the default, parameterless public constructor.
        PublicParameterlessConstructor = 1,
        //
        // Summary:
        //     Specifies all public constructors.
        PublicConstructors = 3,
        //
        // Summary:
        //     Specifies all non-public constructors.
        NonPublicConstructors = 4,
        //
        // Summary:
        //     Specifies all public methods.
        PublicMethods = 8,
        //
        // Summary:
        //     Specifies all non-public methods.
        NonPublicMethods = 16,
        //
        // Summary:
        //     Specifies all public fields.
        PublicFields = 32,
        //
        // Summary:
        //     Specifies all non-public fields.
        NonPublicFields = 64,
        //
        // Summary:
        //     Specifies all public nested types.
        PublicNestedTypes = 128,
        //
        // Summary:
        //     Specifies all non-public nested types.
        NonPublicNestedTypes = 256,
        //
        // Summary:
        //     Specifies all public properties.
        PublicProperties = 512,
        //
        // Summary:
        //     Specifies all non-public properties.
        NonPublicProperties = 1024,
        //
        // Summary:
        //     Specifies all public events.
        PublicEvents = 2048,
        //
        // Summary:
        //     Specifies all non-public events.
        NonPublicEvents = 4096,
        //
        // Summary:
        //     Specifies all interfaces implemented by the type.
        Interfaces = 8192
    }

    //
    // Summary:
    //     Indicates that certain members on a specified System.Type are accessed dynamically,
    //     for example, through System.Reflection.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute
        //     class with the specified member types.
        //
        // Parameters:
        //   memberTypes:
        //     The types of the dynamically accessed members.
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) { }

        //
        // Summary:
        //     Gets the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes that
        //     specifies the type of dynamically accessed members.
        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }
}
#endif
