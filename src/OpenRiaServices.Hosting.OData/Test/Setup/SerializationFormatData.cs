﻿namespace OpenRiaServices.Hosting.Wcf.OData.Test
{
    #region Namespaces
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    #endregion

    /// <summary>Provides information about available serialization formats.</summary>
    public sealed class SerializationFormatData
    {
        /// <summary>Atom format.</summary>
        private static SerializationFormatData atom;

        /// <summary>Binary format.</summary>
        private static SerializationFormatData binary;

        /// <summary>JSON format.</summary>
        private static SerializationFormatData json;

        /// <summary>Test values.</summary>
        private static SerializationFormatData[] values;

        /// <summary>Applicable MIME types.</summary>
        private string[] mimeTypes;

        /// <summary>The friendly name of this serialization format.</summary>
        private string name;

        /// <summary>Hideden constructor.</summary>
        private SerializationFormatData() { }

        private static SerializationFormatData ForData(string name, params string[] mimeTypes)
        {
            SerializationFormatData result = new SerializationFormatData();
            result.name = name;
            result.mimeTypes = mimeTypes;
            return result;
        }

        /// <summary>Atom format.</summary>
        public static SerializationFormatData Atom
        {
            get { CreateValues(); return atom; }
        }

        /// <summary>Binary format.</summary>
        public static SerializationFormatData Binary
        {
            get { CreateValues(); return binary; }
        }

        /// <summary>JSON format.</summary>
        public static SerializationFormatData Json
        {
            get { CreateValues(); return json; }
        }

        /// <summary>Interesting values for testing structions (non-primitive) serialization formats.</summary>
        public static SerializationFormatData[] StructuredValues
        {
            get
            {
                return new SerializationFormatData[] {
                    Json, Atom
                };
            }
        }

        /// <summary>Interesting values for testing serialization formats.</summary>
        public static SerializationFormatData[] Values
        {
            get { CreateValues(); return values; }
        }

        /// <summary>Whether this serialization format is used only for primitive (non-structured) payloads.</summary>
        public bool IsPrimitive
        {
            get
            {
                return this.name == "Binary" || this.name == "Text";
            }
        }

        /// <summary>Whether this serialization format is used only for structured (non-primitive) payloads.</summary>
        public bool IsStructured
        {
            get
            {
                return !this.IsPrimitive;
            }
        }

        /// <summary>The friendly name of this serialization format.</summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>Applicable MIME types.</summary>
        public string[] MimeTypes
        {
            get { return this.mimeTypes; }
        }

        public XmlDocument LoadXmlDocumentFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            bool succeeded = false;
            try
            {
                XmlDocument result;
                stream = TestUtil.EnsureStreamWithSeek(stream);
                if (this == Json)
                {
                    throw new InvalidOperationException("JSON format is not supported.");
                    // result = JsonValidator.ConvertToXmlDocument(stream);
                }
                else if (this == Atom)
                {
                    XmlDocument document = new XmlDocument(TestUtil.TestNameTable);
                    document.Load(stream);
                    result = document;
                }
                else
                {
                    throw new NotSupportedException("XmlDocument creation not supported for serialization format " + this.Name);
                }

                succeeded = true;
                return result;
            }
            finally
            {
                if (!succeeded)
                {
                    if (stream.CanSeek)
                    {
                        stream.Position = 0;
                        Trace.WriteLine("Failing document:");
                        Trace.Indent();
                        Trace.WriteLine(new StreamReader(stream).ReadToEnd());
                        Trace.Unindent();
                    }
                }
            }
        }

        public override string ToString()
        {
            return this.name;
        }

        private static void CreateValues()
        {
            if (values == null)
            {
                json = ForData("JSON", TestUtil.JsonFormat);
                atom = ForData("Atom", TestUtil.AtomFormat, "text/xml", "application/xml");
                binary = ForData("Binary", "application/octet-stream");
                values = new SerializationFormatData[] {
                        json,
                        atom,
                        binary,
                        ForData("Text", "text/plain"),
                    };
            }
        }
    }
}
