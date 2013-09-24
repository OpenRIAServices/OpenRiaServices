using System.Windows.Media;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Enum used for testing.
    /// </summary>
    public enum TestEnumeration
    {
        /// <summary>
        /// Enum value equal to 0.
        /// </summary>
        Zero = 0,

        /// <summary>
        /// Enum value equal to 1.
        /// </summary>
        One = 1,

        /// <summary>
        /// Enum value equal to 2.
        /// </summary>
        Two = 2
    }

    /// <summary>
    /// Class containing multiple property data types used for testing.
    /// </summary>
    public class DataTypeTestClass
    {
        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public int Int32F { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.TimeSpan TimeSpanP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public float SingleP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public char CharP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public byte ByteP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public ushort UInt16P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public uint UInt32P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public long Int64P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.DateTime DateTimeP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.DateTimeOffset DateTimeOffsetP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public string StringP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the property is true.
        /// </summary>
        public bool BooleanP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public decimal DecimalP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public double DoubleP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public EmptyTestClass TestClass2P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public TestEnumeration TestEnumP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public int? NInt32F { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.TimeSpan? NTimeSpanP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public float? NSingleP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public char? NCharP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public byte? NByteP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public ushort? NUInt16P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public uint? NUInt32P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public long? NInt64P { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.DateTime? NDateTimeP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public System.DateTimeOffset? NDateTimeOffsetP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public bool? NBooleanP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public decimal? NDecimalP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public double? NDoubleP { get; set; }

        /// <summary>
        /// Gets or sets a property used for testing.
        /// </summary>
        public TestEnumeration? NTestEnumP { get; set; }
    }

    /// <summary>
    /// Empty class used for testing.
    /// </summary>
    public class EmptyTestClass
    {
    }
}
