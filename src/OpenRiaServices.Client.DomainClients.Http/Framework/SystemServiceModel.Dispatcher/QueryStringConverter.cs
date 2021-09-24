//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// LICENSE MIT https://github.com/microsoft/referencesource/blob/master/LICENSE.txt
// Retreived from https://raw.githubusercontent.com/microsoft/referencesource/5697c29004a34d80acdaf5742d7e699022c64ecd/System.ServiceModel.Web/System/ServiceModel/Dispatcher/QueryStringConverter.cs
//------------------------------------------------------------
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

#pragma warning disable 1634, 1691
namespace System.ServiceModel.Dispatcher
{
    // Thread Safety: This class is thread safe
    class QueryStringConverter
    {
        private readonly HashSet<Type> _defaultSupportedQueryStringTypes;
        // the cache does not have a quota since it is per endpoint and is
        // bounded by the number of types in the contract at the endpoint
        private readonly Dictionary<Type, TypeConverter> _typeConverterCache;

        public QueryStringConverter()
        {
            this._defaultSupportedQueryStringTypes = new HashSet<Type>();
            this._defaultSupportedQueryStringTypes.Add(typeof(byte));
            this._defaultSupportedQueryStringTypes.Add(typeof(sbyte));
            this._defaultSupportedQueryStringTypes.Add(typeof(short));
            this._defaultSupportedQueryStringTypes.Add(typeof(int));
            this._defaultSupportedQueryStringTypes.Add(typeof(long));
            this._defaultSupportedQueryStringTypes.Add(typeof(ushort));
            this._defaultSupportedQueryStringTypes.Add(typeof(uint));
            this._defaultSupportedQueryStringTypes.Add(typeof(ulong));
            this._defaultSupportedQueryStringTypes.Add(typeof(float));
            this._defaultSupportedQueryStringTypes.Add(typeof(double));
            this._defaultSupportedQueryStringTypes.Add(typeof(bool));
            this._defaultSupportedQueryStringTypes.Add(typeof(char));
            this._defaultSupportedQueryStringTypes.Add(typeof(decimal));
            this._defaultSupportedQueryStringTypes.Add(typeof(string));
            this._defaultSupportedQueryStringTypes.Add(typeof(object));
            this._defaultSupportedQueryStringTypes.Add(typeof(DateTime));
            this._defaultSupportedQueryStringTypes.Add(typeof(TimeSpan));
            this._defaultSupportedQueryStringTypes.Add(typeof(byte[]));
            this._defaultSupportedQueryStringTypes.Add(typeof(Guid));
            this._defaultSupportedQueryStringTypes.Add(typeof(Uri));
            this._defaultSupportedQueryStringTypes.Add(typeof(DateTimeOffset));
            this._typeConverterCache = new Dictionary<Type, TypeConverter>();
        }

        public virtual bool CanConvert(Type type)
        {
            if (this._defaultSupportedQueryStringTypes.Contains(type))
            {
                return true;
            }
            // otherwise check if its an enum
            if (typeof(Enum).IsAssignableFrom(type))
            {
                return true;
            }
            // check if there's a typeconverter defined on the type
            return (GetStringConverter(type) != null);
        }

        public virtual string ConvertValueToString(object parameter, Type parameterType)
        {
            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }
            if (parameterType.IsValueType && parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }
            switch (Type.GetTypeCode(parameterType))
            {
                case TypeCode.Byte:
                    return XmlConvert.ToString((byte)parameter);
                case TypeCode.SByte:
                    return XmlConvert.ToString((sbyte)parameter);
                case TypeCode.Int16:
                    return XmlConvert.ToString((short)parameter);
                case TypeCode.Int32:
                    {
                        if (typeof(Enum).IsAssignableFrom(parameterType))
                        {
                            return Enum.Format(parameterType, parameter, "G");
                        }
                        else
                        {
                            return XmlConvert.ToString((int)parameter);
                        }
                    }
                case TypeCode.Int64:
                    return XmlConvert.ToString((long)parameter);
                case TypeCode.UInt16:
                    return XmlConvert.ToString((ushort)parameter);
                case TypeCode.UInt32:
                    return XmlConvert.ToString((uint)parameter);
                case TypeCode.UInt64:
                    return XmlConvert.ToString((ulong)parameter);
                case TypeCode.Single:
                    return XmlConvert.ToString((float)parameter);
                case TypeCode.Double:
                    return XmlConvert.ToString((double)parameter);
                case TypeCode.Char:
                    return XmlConvert.ToString((char)parameter);
                case TypeCode.Decimal:
                    return XmlConvert.ToString((decimal)parameter);
                case TypeCode.Boolean:
                    return XmlConvert.ToString((bool)parameter);
                case TypeCode.String:
                    return (string)parameter;
                case TypeCode.DateTime:
                    return XmlConvert.ToString((DateTime)parameter, XmlDateTimeSerializationMode.RoundtripKind);
                default:
                    {
                        if (parameterType == typeof(TimeSpan))
                        {
                            return XmlConvert.ToString((TimeSpan)parameter);
                        }
                        else if (parameterType == typeof(Guid))
                        {
                            return XmlConvert.ToString((Guid)parameter);
                        }
                        else if (parameterType == typeof(DateTimeOffset))
                        {
                            return XmlConvert.ToString((DateTimeOffset)parameter);
                        }
                        else if (parameterType == typeof(byte[]))
                        {
                            return (parameter != null) ? Convert.ToBase64String((byte[])parameter, Base64FormattingOptions.None) : null;
                        }
                        else if (parameterType == typeof(Uri) || parameterType == typeof(object))
                        {
                            // URI or object
                            return (parameter != null) ? Convert.ToString(parameter, CultureInfo.InvariantCulture) : null;
                        }
                        else
                        {
                            TypeConverter stringConverter = GetStringConverter(parameterType);
                            if (stringConverter == null)
                            {
                                throw new NotImplementedException(
                                    $"Type {parameterType.ToString()} is not supported by QueryStringConverter");
                            }
                            else
                            {
                                return stringConverter.ConvertToInvariantString(parameter);
                            }
                        }
                    }
            }
        }

        // hash table is safe for multiple readers single writer
        [SuppressMessage("Reliability", "Reliability104:CaughtAndHandledExceptionsRule", Justification = "The exception is traced in the finally clause")]
        TypeConverter GetStringConverter(Type parameterType)
        {
            if (this._typeConverterCache.ContainsKey(parameterType))
            {
                return (TypeConverter)this._typeConverterCache[parameterType];
            }
            TypeConverterAttribute[] typeConverterAttrs = parameterType.GetCustomAttributes(typeof(TypeConverterAttribute), true) as TypeConverterAttribute[];
            if (typeConverterAttrs != null)
            {
                foreach (TypeConverterAttribute converterAttr in typeConverterAttrs)
                {
                    Type converterType = Type.GetType(converterAttr.ConverterTypeName, false, true);
                    if (converterType != null)
                    {
                        TypeConverter converter = null;
                        Exception handledException = null;
                        try
                        {
                            converter = (TypeConverter)Activator.CreateInstance(converterType);
                        }
                        catch (TargetInvocationException e)
                        {
                            handledException = e;
                        }
                        catch (MemberAccessException e)
                        {
                            handledException = e;
                        }
                        catch (TypeLoadException e)
                        {
                            handledException = e;
                        }
                        catch (COMException e)
                        {
                            handledException = e;
                        }
                        catch (InvalidComObjectException e)
                        {
                            handledException = e;
                        }
                        /* Original code has some logging of exceptions here, and rethrows Fatal exceptions like OutOfMemory */

                        if (converter == null)
                        {
                            continue;
                        }
                        if (converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
                        {
                            this._typeConverterCache.Add(parameterType, converter);
                            return converter;
                        }
                    }
                }
            }
            this._typeConverterCache.Add(parameterType, null);
            return null;
        }
    }
}
