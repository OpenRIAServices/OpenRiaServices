#if !NET
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace System
{
    /// <summary>
    /// Helper methods to allow "newer" .NET methods on older frameworks
    /// </summary>
    internal static class Polyfills
    {
        extension(ArgumentNullException)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                    ThrowArgumentNullException(paramName);
            }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (string.IsNullOrEmpty(argument))
                    ThrowArgumentNullException(paramName);
            }
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
        }

        [DoesNotReturn]
        private static void ThrowArgumentNullException(string? paramName)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper methods to allow "newer" .NET methods on older frameworks
    /// </summary>
    internal static class Polyfills
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

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; private set; }
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

    /// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values when returning with the specified return value condition.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated field or property member will not be null.
        /// </param>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = [member];
        }

        /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated field and property members will not be null.
        /// </param>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }

    /// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with a field or property member.</summary>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(string member) => Members = [member];

        /// <summary>Initializes the attribute with the list of field and property members.</summary>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(params string[] members) => Members = members;

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }

    /// <summary>
    /// Specifies that an output is not null even if the corresponding type allows it. Specifies that an input argument was not null when the call returns.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute { }

    /// <summary>
    /// Specifies that a method will never return under any circumstance.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute { }
}
#endif
