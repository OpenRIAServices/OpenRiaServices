using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Spatial;
using System.Data.Linq;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Server;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.DomainServices.Hosting.UnitTests
{
    /// <summary>
    /// Tests <see cref="DataContractSurrogate"/> members.
    /// </summary>
    [TestClass]
    public class DataContractSurrogateGeneratorTest
    {
        #region Virtual properties.
        [TestMethod]
        [Description("Tests that a virtual writable property of a primitive type works as expected")]
        public void Surrogate_VirtualProperty_Primitive()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Primitive);
            TestVirtualProperty<int>(
                entityType,
                isReadOnly: false,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int), property.PropertyType, "Incorrect property type.");
                    Assert.AreEqual(0, property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.AreEqual(42, property.GetValue(obj, null), "Value wasn't updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual read-only property of a primitive type works as expected")]
        public void Surrogate_VirtualProperty_Primitive_ReadOnly()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Primitive_ReadOnly);
            TestVirtualProperty<int>(
                entityType,
                isReadOnly: true,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int), property.PropertyType, "Incorrect property type.");
                    Assert.AreEqual(0, property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.AreEqual(0, property.GetValue(obj, null), "Value was updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual writable property of a nullable primitive type works as expected")]
        public void Surrogate_VirtualProperty_Nullable_Primitive()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Nullable_Primitive);
            TestVirtualProperty<int?>(
                entityType,
                isReadOnly: false,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int?), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.AreEqual(42, property.GetValue(obj, null), "Value wasn't updated.");
                    property.SetValue(obj, null, null);

                    // Nulls are ignored.
                    Assert.AreEqual(42, property.GetValue(obj, null), "Value wasn't updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual read-only property of a nullable primitive type works as expected")]
        public void Surrogate_VirtualProperty_Nullable_Primitive_ReadOnly()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Nullable_Primitive_ReadOnly);
            TestVirtualProperty<int?>(
                entityType,
                isReadOnly: true,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int?), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.IsNull(property.GetValue(obj, null), "Value was updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual writable property of a string type works as expected")]
        public void Surrogate_VirtualProperty_String()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_String);
            TestVirtualProperty<string>(
                entityType,
                isReadOnly: false,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(string), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be null.");
                    property.SetValue(obj, "test", null);
                    Assert.AreEqual("test", property.GetValue(obj, null), "Value wasn't updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual read-only property of a string type works as expected")]
        public void Surrogate_VirtualProperty_String_ReadOnly()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_String_ReadOnly);
            TestVirtualProperty<string>(
                entityType,
                isReadOnly: true,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(string), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be null.");
                    property.SetValue(obj, "test", null);
                    Assert.IsNull(property.GetValue(obj, null), "Value was updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual writable property of a Binary type works as expected")]
        public void Surrogate_VirtualProperty_Binary()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Binary);
            TestVirtualProperty<Binary>(
                entityType,
                isReadOnly: false,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(byte[]), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be null.");

                    byte[] value = new byte[] { 1, 2 };
                    property.SetValue(obj, value, null);

                    byte[] returnValue = (byte[])property.GetValue(obj, null);
                    Assert.IsTrue(returnValue.SequenceEqual(value), "Value wasn't updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual read-only property of a Binary type works as expected")]
        public void Surrogate_VirtualProperty_Binary_ReadOnly()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_Binary_ReadOnly);
            TestVirtualProperty<Binary>(
                entityType,
                isReadOnly: true,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(byte[]), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be null.");
                    property.SetValue(obj, new byte[3], null);
                    Assert.IsNull(property.GetValue(obj, null), "Value was updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a virtual writable property of a DbGeometry type works as expected")]
        public void Surrogate_VirtualProperty_DbGeometry()
        {
            Type entityType = typeof(SurrogateTestEntity_VirtualProperty_DbGeometry);
            TestVirtualProperty<DbGeometry>(
                entityType,
                isReadOnly: false,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(DbGeometry), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be null.");

                    DbGeometry value = DbGeometry.FromText("POINT(1 2)");
                    property.SetValue(obj, value, null);

                    DbGeometry returnValue = (DbGeometry)property.GetValue(obj, null);
                    Assert.IsTrue(returnValue == value, "Value wasn't updated.");
                });
        }

        
        private void TestVirtualProperty<T>(Type entityType, bool isReadOnly, Action<Type, PropertyInfo, object> verify)
        {
            string propertyName = "TestProperty";
            MockTypeDescriptionProvider provider = new MockTypeDescriptionProvider(() =>
            {
                Dictionary<object, T> valueMap = new Dictionary<object, T>();

                Action<object, T> setter;
                if (isReadOnly)
                {
                    setter = null;
                }
                else
                {
                    setter = (obj, value) => valueMap[obj] = value;
                }

                return new MockPropertyDescriptor<T>(
                    propertyName,
                    entityType,
                    getter: obj =>
                    {
                        T value;
                        valueMap.TryGetValue(obj, out value);
                        return value;
                    },
                    setter: setter);
            });

            TypeDescriptor.AddProvider(provider, entityType);
            HashSet<Type> knownEntityTypes = new HashSet<Type>() { entityType };
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, entityType);
            object surrogateObj = Activator.CreateInstance(surrogateType, Activator.CreateInstance(entityType));
            var p = surrogateType.GetProperty(propertyName);
            Assert.IsNotNull(p, "Property doesn't exist.");
            verify(surrogateType, p, surrogateObj);
            TypeDescriptor.RemoveProvider(provider, entityType);
        }
        #endregion

        #region CLR properties.
        [TestMethod]
        [Description("Tests that a CLR writable property of a nullable primitive type works as expected")]
        public void Surrogate_ClrProperty_Nullable_Primitive()
        {
            Type entityType = typeof(SurrogateTestEntity_ClrProperty_Nullable_Primitive);
            TestClrProperty<int?>(
                entityType,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int?), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.AreEqual(42, property.GetValue(obj, null), "Value wasn't updated.");
                    property.SetValue(obj, null, null);

                    // Nulls are ignored.
                    Assert.AreEqual(42, property.GetValue(obj, null), "Value wasn't updated.");
                });
        }

        [TestMethod]
        [Description("Tests that a CLR read-only property of a nullable primitive type works as expected")]
        public void Surrogate_ClrProperty_Nullable_Primitive_ReadOnly()
        {
            Type entityType = typeof(SurrogateTestEntity_ClrProperty_Nullable_Primitive_ReadOnly);
            TestClrProperty<int?>(
                entityType,
                verify: (type, property, obj) =>
                {
                    Assert.AreEqual(typeof(int?), property.PropertyType, "Incorrect property type.");
                    Assert.IsNull(property.GetValue(obj, null), "Default value should be 0.");
                    property.SetValue(obj, 42, null);
                    Assert.IsNull(property.GetValue(obj, null), "Value was updated.");
                });
        }

        private void TestClrProperty<T>(Type entityType, Action<Type, PropertyInfo, object> verify)
        {
            string propertyName = "TestProperty";

            HashSet<Type> knownEntityTypes = new HashSet<Type>() { entityType };
            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, entityType);
            object surrogateObj = Activator.CreateInstance(surrogateType, Activator.CreateInstance(entityType));
            var p = surrogateType.GetProperty(propertyName);
            Assert.IsNotNull(p, "Property doesn't exist.");
            verify(surrogateType, p, surrogateObj);
        }
        #endregion

        [TestMethod]
        [Description("Tests that generic base types for entities are supported.")]
        public void GenericBaseTypesForEntity()
        {
            Type entityType = typeof(EntityWithGenericBaseType);
            HashSet<Type> knownEntityTypes = new HashSet<Type>() { entityType };

            Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, entityType);

            Assert.IsNotNull(surrogateType, "No surrogate type was generated.");
            Assert.AreNotEqual(entityType, surrogateType, "Surrogate type should be a different type than the actual entity type.");
            Assert.AreEqual(entityType.FullName, surrogateType.FullName, "Surrogate has an unexpected name.");
            Assert.AreEqual(typeof(object), surrogateType.BaseType, "Surrogate has an unexpected base type.");

            var additionalProperty = surrogateType.GetProperty("AdditionalProperty");
            Assert.IsNotNull(additionalProperty, "Missing AdditionalProperty property.");
            Assert.AreEqual(typeof(string), additionalProperty.PropertyType, "Unexpected property type.");

            var keyProperty = surrogateType.GetProperty("Key");
            Assert.IsNotNull(keyProperty, "Missing Key property.");
            Assert.AreEqual(typeof(int), keyProperty.PropertyType, "Unexpected property type.");
        }

        [TestMethod]
        [Description("Tests that entity hierarchies with non-exposed entities lifts CLR properties into surrogates")]
        public void InheritanceClrProperties()
        {
            // Actual inheritance is Hidden_Base <-- Visible_Base <-- Hidden_Derived1 <-- Hidden_Derived2 <-- Visible_Derived
            // Visible inheritance is only Visible_Base <-- Visible_Derived
            Type visibleBaseEntityType = typeof(SurrogateTestEntity_ClrProperty_Inheritance_Visible_Base);
            Type visibleDerivedEntityType = typeof(SurrogateTestEntity_ClrProperty_Inheritance_Visible_Derived);

            // Known types exclude the intermediate Hidden_Derived1 and Hidden_Derived2 and expect its properties to lift
            // up to the first visible subclass (Visible_Derived)
            HashSet<Type> knownEntityTypes = new HashSet<Type>() { visibleBaseEntityType, visibleDerivedEntityType };

            Type visibleBaseSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, visibleBaseEntityType);
            Type visibleDerivedSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, visibleDerivedEntityType);

            // The surrogate for the visible base should appear to derive from Object because its true base type is not exposed
            Assert.IsNotNull(visibleBaseSurrogateType, "No base surrogate type was generated.");
            Assert.AreNotEqual(visibleBaseEntityType, visibleBaseSurrogateType, "Base surrogate type should be a different type than the actual entity type.");
            Assert.AreEqual(visibleBaseEntityType.FullName, visibleBaseSurrogateType.FullName, "Visible base surrogate has an unexpected name.");
            Assert.AreEqual(typeof(object), visibleBaseSurrogateType.BaseType, "Visible base surrogate has an unexpected base type.");

            // The surrogate for the visible derived type should subclass the visible base.  The unexposed intermediate types are flattened.
            Assert.IsNotNull(visibleDerivedSurrogateType, "No visible derived surrogate type was generated.");
            Assert.AreNotEqual(visibleDerivedEntityType, visibleDerivedSurrogateType, "Visible derived surrogate type should be a different type than the actual entity type.");
            Assert.AreEqual(visibleDerivedEntityType.FullName, visibleDerivedSurrogateType.FullName, "Visible derived surrogate has an unexpected name.");
            Assert.AreEqual(visibleBaseSurrogateType, visibleDerivedSurrogateType.BaseType, "Visible derived surrogate has an unexpected base type.");

            // The visible base class should have its own property
            var visibleBaseProperty = visibleBaseSurrogateType.GetProperty("VisibleBaseProperty");
            Assert.IsNotNull(visibleBaseProperty, "Visible_Base type should have exposed VisibleBaseProperty");

            // The visible base should have lifted properties from its unexposed base
            var hiddenBaseProperty = visibleBaseSurrogateType.GetProperty("HiddenBaseProperty");
            Assert.IsNotNull(hiddenBaseProperty, "Visible_Derived type should have lifted HiddenBaseProperty");

            var hiddenDerived1Property = visibleDerivedSurrogateType.GetProperty("HiddenDerived1Property");
            Assert.IsNotNull(hiddenDerived1Property, "Visible_Derived should have lifted HiddenDerived1Property from unexposed base type");

            var hiddenDerived2Property = visibleDerivedSurrogateType.GetProperty("HiddenDerived2Property");
            Assert.IsNotNull(hiddenDerived2Property, "Visible_Derived should have lifted HiddenDerived2Property from unexposed base type");
        }

        [TestMethod]
        [Description("Tests that entity hierarchies with non-exposed entities lifts TypeDescriptor properties into surrogates.")]
        public void InheritanceVirtualProperties()
        {
            // Add some TDP properties to the same logical entity hierarchy tested above in InheritanceClrProperties
            AddVirtualProperty<SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Base, string>("HiddenBaseVirtualProperty");
            AddVirtualProperty<SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Base, string>("VisibleBaseVirtualProperty");
            AddVirtualProperty<SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived1, string>("HiddenDerived1VirtualProperty");
            AddVirtualProperty<SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived2, string>("HiddenDerived2VirtualProperty");
            AddVirtualProperty<SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Derived, string>("VisibleDerivedVirtualProperty");

            // Actual inheritance is Hidden_Base <-- Visible_Base <-- Hidden_Derived1 <-- Hidden_Derived2 <-- Visible_Derived
            // Visible inheritance is only Visible_Base <-- Visible_Derived
            Type visibleBaseEntityType = typeof(SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Base);
            Type visibleDerivedEntityType = typeof(SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Derived);

            // Known types exclude the intermediate Hidden_Derived1 and Hidden_Derived2 and expect its properties to lift
            // up to the first visible subclass (Visible_Derived)
            HashSet<Type> knownEntityTypes = new HashSet<Type>() { visibleBaseEntityType, visibleDerivedEntityType };

            Type visibleBaseSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, visibleBaseEntityType);
            Type visibleDerivedSurrogateType = DataContractSurrogateGenerator.GetSurrogateType(knownEntityTypes, visibleDerivedEntityType);

            // The visible base class should have its own property
            var visibleBaseProperty = visibleBaseSurrogateType.GetProperty("VisibleBaseVirtualProperty");
            Assert.IsNotNull(visibleBaseProperty, "Visible_Base type should have exposed VisibleBaseVirtualProperty");

            // The visible base should have lifted properties from its unexposed base
            var hiddenBaseProperty = visibleBaseSurrogateType.GetProperty("HiddenBaseVirtualProperty");
            Assert.IsNotNull(hiddenBaseProperty, "Visible_Derived type should have lifted HiddenBaseVirtualProperty");

            var hiddenDerived1Property = visibleDerivedSurrogateType.GetProperty("HiddenDerived1VirtualProperty");
            Assert.IsNotNull(hiddenDerived1Property, "Visible_Derived should have lifted HiddenDerived1VirtualProperty from unexposed base type");

            var hiddenDerived2Property = visibleDerivedSurrogateType.GetProperty("HiddenDerived2VirtualProperty");
            Assert.IsNotNull(hiddenDerived2Property, "Visible_Derived should have lifted HiddenDerived2VirtualProperty from unexposed base type");
        }

        // Helper to add a single TDP property to a given entity type.
        private static void AddVirtualProperty<TEntity, TProperty>(string propertyName)
        {
            Type baseType = typeof(TEntity).BaseType;
            bool inheritFromBase = baseType != typeof(object);
            ICustomTypeDescriptor parent = inheritFromBase ? TypeDescriptor.GetProvider(baseType).GetTypeDescriptor(baseType) : null;
            MockTypeDescriptionProvider provider = new MockTypeDescriptionProvider(() =>
            {
                return new MockPropertyDescriptor<TProperty>(propertyName, typeof(TEntity), getter: null, setter: null);
            }, parent);
            TypeDescriptor.AddProvider(provider, typeof(TEntity));
        }

        [TestMethod]
        [Description("Tests that entities and complex types specifying DataContract/DataMember attributes are reflected in the surrogates.")]
        public void DataContractPropagation()
        {
            Type entityType = typeof(MockReport);
            Type complexType = typeof(MockReportBody);

            // register TDPs
            DomainServiceDescription.GetDescription(typeof(MockCustomerDomainService));

            HashSet<Type> knownExposedTypes = new HashSet<Type>() { entityType, complexType };
            Type entityTypeSurrogate = DataContractSurrogateGenerator.GetSurrogateType(knownExposedTypes, entityType);
            Type complexTypeSurrogate = DataContractSurrogateGenerator.GetSurrogateType(knownExposedTypes, complexType);

            List<string> actualTypeResult = new List<string>();
            List<string> actualSurrogateResult = new List<string>();

            // DataContract first
            actualTypeResult.Add(GetDataContract(entityType));
            actualSurrogateResult.Add(GetDataContract(entityTypeSurrogate));

            actualTypeResult.Add(GetDataContract(complexType));
            actualSurrogateResult.Add(GetDataContract(complexTypeSurrogate));

            // DataMembers
            // Order exists on the type, but not on the surrogates.
            // We need to compare only the surrogate's Order to the expected.
            GetDataMember(entityType, false, actualTypeResult, "Customer");
            GetDataMember(entityTypeSurrogate, true, actualSurrogateResult);
            GetDataMember(complexType, false, actualTypeResult);
            GetDataMember(complexTypeSurrogate, true, actualSurrogateResult);

            Func<string, string, string> aggregate = (s1, s2) => s1 + " " + s2;
            string expected = actualTypeResult.Aggregate(aggregate);
            string surrogate = actualSurrogateResult.Aggregate(aggregate);

            Assert.AreEqual(expected, surrogate, "Surrogate does not match expected results.");
        }

        private static string GetDataContract(Type type)
        {
            string result = string.Empty;

            // Compare DataContract first
            DataContractAttribute dataContractAttr = TypeDescriptor.GetAttributes(type)[typeof(DataContractAttribute)] as DataContractAttribute;
            if (dataContractAttr != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("DataContract(");
                sb.Append("Name=");
                sb.Append(dataContractAttr.Name);
                sb.Append(", Namespace=");
                sb.Append(dataContractAttr.Namespace);
                result = sb.ToString();
            }

            return result;
        }

        private static void GetDataMember(Type type, bool emitOrder, List<string> results, params string[] associationsToRemove)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type).Sort();

            // Associations are not reflected in surrogates.
            foreach (string association in associationsToRemove)
            {
                properties.Remove(properties[association]);
            }

            // DataMembers properties
            foreach (PropertyDescriptor property in properties)
            {
                DataMemberAttribute dataMemberAttr = property.Attributes[typeof(DataMemberAttribute)] as DataMemberAttribute;
                string result = property.Name;

                if (dataMemberAttr != null)
                {
                    int order = emitOrder ? dataMemberAttr.Order : -1;

                    StringBuilder sb = new StringBuilder();
                    sb.Append(property.Name);
                    sb.Append(": DataMember(");

                    sb.Append("Name=");
                    sb.Append(dataMemberAttr.Name);
                    sb.Append(", IsRequired=");
                    sb.Append(dataMemberAttr.IsRequired);
                    sb.Append(", EmitDefaultValue=");
                    sb.Append(dataMemberAttr.EmitDefaultValue);
                    if (order >= 0)
                    {
                        sb.Append(", Order=");
                        sb.Append(order);
                    }

                    sb.Append(")");
                    result = sb.ToString();
                }

                results.Add(result);
            }
        }
    }

    public abstract class GenericEntity<TKey>
    {
        public TKey Key
        {
            get;
            set;
        }
    }

    public class EntityWithGenericBaseType : GenericEntity<int>
    {
        public string AdditionalProperty
        {
            get;
            set;
        }
    }

    public class SurrogateTestEntity_VirtualProperty_Primitive : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Primitive_ReadOnly : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_String : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_String_ReadOnly : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Binary : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_DbGeometry : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Binary_ReadOnly : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Nullable_Primitive : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Nullable_Primitive_ReadOnly : GenericEntity<int> { }

    #region Inheritance
    // Actual inheritance is Hidden_Base <-- Visible_Base <-- Hidden_Derived1 <-- Hidden_Derived2 <-- Visible_Derived
    // Visible inheritance is only Visible_Base <-- Visible_Derived
    // This tests the following:
    //  1. The visible root needs to lift properties from its invisible base entity
    //  2. The visible derived class needs to lift properties from more than one invisible base
    //  3. Abstract base classes are supported
    //  4. Abstract derived classes are supported.
    public class SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Base : GenericEntity<int>
    {
        public string HiddenBaseProperty { get; set; }
    }
    public class SurrogateTestEntity_ClrProperty_Inheritance_Visible_Base : SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Base
    {
        public string VisibleBaseProperty { get; set; }
    }
    public abstract class SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Derived1 : SurrogateTestEntity_ClrProperty_Inheritance_Visible_Base
    {
        public string HiddenDerived1Property { get; set; }
    }
    public class SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Derived2 : SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Derived1
    {
        public string HiddenDerived2Property { get; set; }
    }
    public abstract class SurrogateTestEntity_ClrProperty_Inheritance_Visible_Derived : SurrogateTestEntity_ClrProperty_Inheritance_Hidden_Derived2
    {
        public string VisibleDerivedProperty { get; set; }
    }

    // Same hierarchy as above, but we will inject virtual properties via TDP
    public class SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Base : GenericEntity<int> { }
    public class SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Base : SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Base { }
    public abstract class SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived1 : SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Base { }
    public class SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived2 : SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived1 { }
    public abstract class SurrogateTestEntity_VirtualProperty_Inheritance_Visible_Derived : SurrogateTestEntity_VirtualProperty_Inheritance_Hidden_Derived2 { }

    #endregion // Inheritance

    public class SurrogateTestEntity_ClrProperty_Nullable_Primitive : GenericEntity<int>
    {
        public int? TestProperty { get; set; }
    }

    public class SurrogateTestEntity_ClrProperty_Nullable_Primitive_ReadOnly : GenericEntity<int>
    {
        public int? TestProperty { get; private set; }
    }

    public class MockTypeDescriptionProvider : TypeDescriptionProvider
    {
        private Func<PropertyDescriptor> _createProperty;
        private ICustomTypeDescriptor _parent;

        public MockTypeDescriptionProvider(Func<PropertyDescriptor> createProperty)
        {
            this._createProperty = createProperty;
        }

        public MockTypeDescriptionProvider(Func<PropertyDescriptor> createProperty, ICustomTypeDescriptor parent) : this(createProperty)
        {
            this._parent = parent;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            ICustomTypeDescriptor parent = this._parent ?? base.GetTypeDescriptor(objectType, instance);
            return new MockTypeDescriptor(objectType, parent, this._createProperty);
        }

        private class MockTypeDescriptor : CustomTypeDescriptor
        {
            private Type _objectType;
            private Func<PropertyDescriptor> _createProperty;

            public MockTypeDescriptor(Type objectType, ICustomTypeDescriptor parent, Func<PropertyDescriptor> createProperty)
                : base(parent)
            {
                this._objectType = objectType;
                this._createProperty = createProperty;
            }

            public override PropertyDescriptorCollection GetProperties()
            {
                var properties = base.GetProperties().Cast<PropertyDescriptor>().ToList();
                properties.Add(_createProperty());
                return new PropertyDescriptorCollection(properties.ToArray());
            }
        }
    }

    public class MockPropertyDescriptor<T> : PropertyDescriptor
    {
        private string _name;
        private Type _declaringType;
        private Func<object, T> _getter;
        private Action<object, T> _setter;

        public MockPropertyDescriptor(string name, Type declaringType, Func<object, T> getter, Action<object, T> setter)
            : base(name, new Attribute[0])
        {
            this._name = name;
            this._declaringType = declaringType;
            this._getter = getter;
            this._setter = setter;
        }

        public override Type ComponentType
        {
            get
            {
                return this._declaringType;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return (this._setter == null);
            }
        }

        public override Type PropertyType
        {
            get
            {
                return typeof(T);
            }
        }

        public override bool CanResetValue(object component)
        {
            return true;
        }

        public override object GetValue(object component)
        {
            return this._getter(component);
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            this._setter(component, (T)value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }
    }
}
