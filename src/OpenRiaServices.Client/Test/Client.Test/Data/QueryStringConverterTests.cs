using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.Client.DomainClients.Http;

namespace OpenRiaServices.Client.Test
{
    [TestClass]
    public class QueryStringConverterTests
    {
        [TestMethod]
        [DynamicData(nameof(SupportedConversionCases))]
        public void SupportedTypesHaveStringConversions(Type parameterType, object value, string expected)
        {
            Assert.IsTrue(WebQueryStringConverter.CanConvert(parameterType));
            Assert.AreEqual(expected, WebQueryStringConverter.ConvertValueToString(value, parameterType));
        }

        public static IEnumerable<object[]> SupportedConversionCases =>
        [
            [typeof(int), 42, "42"],
            [typeof(int?), 42, "42"],
            [typeof(DayOfWeek), DayOfWeek.Monday, "Monday"],
            [typeof(DayOfWeek?), DayOfWeek.Monday, "Monday"],
            [typeof(ConvertibleValue), new ConvertibleValue(42), "42"],
            [typeof(ConvertibleValue?), new ConvertibleValue(42), "42"]
        ];

        [TypeConverter(typeof(ConvertibleValueConverter))]
        public struct ConvertibleValue(int value)
        {
            public int Value { get; } = value;
        }

        public sealed class ConvertibleValueConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                => destinationType == typeof(string);

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                => sourceType == typeof(string);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
                => ((ConvertibleValue)value).Value.ToString(culture);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                => new ConvertibleValue(int.Parse((string)value, culture));
        }
    }
}
