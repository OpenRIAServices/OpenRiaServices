using System.Diagnostics;
using System.Globalization;

namespace System.Windows.Common
{
    /// <summary>
    /// Utility class for sharing code
    /// </summary>
    internal static class Utilities
    {
        #region Methods

        /// <summary>
        /// Determines if two objects are equal
        /// </summary>
        /// <param name="value1">first object to compare</param>
        /// <param name="value2">second object to compare</param>
        /// <returns>True if the two values are equal</returns>
        public static bool AreValuesEqual(object value1, object value2)
        {
            return (value1 == null && value2 == null) || 
                   (value1 != null && value1.Equals(value2));
        }

        /// <summary>
        /// Converts a value to the provided targetType
        /// </summary>
        /// <param name="culture">Culture used for the conversion</param>
        /// <param name="targetType">Destination type</param>
        /// <param name="value">Value to convert</param>
        /// <returns>The converted value</returns>
        /// <exception cref="ArgumentException">Thrown when conversion fails.</exception>
        public static object GetConvertedValue(CultureInfo culture, Type targetType, object value)
        {
            Debug.Assert(targetType != null, "Unexpected null targetType");

            if (value == null)
            {
                return null;
            }

            Type nonNullableTargetType = targetType.GetNonNullableType();
            if (value.GetType() != nonNullableTargetType)
            {
                try
                {
                    // Don't parse a value that is already an enum, because that could
                    // implicitly convert from one enum type to another if they have
                    // members with the same name.
                    if (nonNullableTargetType.IsEnum && !value.GetType().IsEnumType())
                    {
                        return Enum.Parse(nonNullableTargetType, value.ToString(), true /*ignoreCase*/);
                    }
                    else if (value is IConvertible)
                    {
                        return System.Convert.ChangeType(value, nonNullableTargetType, culture);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.IsConversionException())
                    {
                        throw new ArgumentException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CommonResources.CannotConvertValue,
                                value.GetType().GetTypeName(),
                                nonNullableTargetType.GetTypeName()),
                            ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// Determines whether an exception is acceptable for type conversion operations.
        /// </summary>
        /// <param name="exception">Exception to check</param>
        /// <returns>True if the exception is an acceptable conversion exception</returns>
        public static bool IsConversionException(this Exception exception)
        {
            return exception is FormatException || exception is InvalidCastException ||
                exception is NotSupportedException || exception is OverflowException ||
                exception is ArgumentException;
        }

        #endregion Methods
    }
}
