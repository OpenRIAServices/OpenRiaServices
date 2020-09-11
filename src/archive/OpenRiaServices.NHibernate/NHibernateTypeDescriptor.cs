using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using NHibernate.Cfg;
using NHibernate.Mapping;
using NHibernate.Type;
using OpenRiaServices.Server;

namespace OpenRiaServices.NHibernate
{
    internal class NHibernateTypeDescriptor : CustomTypeDescriptor
    {
        private readonly IEnumerable<ISelectable> _identifierCols;
        private readonly PersistentClass _classMetadata;
        private readonly Type entityType;
        private readonly Configuration _nhibernateConfiguration;
        private readonly Dictionary<string, IEnumerable<Attribute>> _metaDataAttributes = new Dictionary<string, IEnumerable<Attribute>>();
        private PropertyDescriptorCollection _properties;

        public NHibernateTypeDescriptor(Type entityType, ICustomTypeDescriptor parent,
                                        Configuration nhibernateConfiguration, Type metaDataType)
            : base(parent)
        {
            Type metaDataType1 = metaDataType;

            while (entityType != null && entityType.Name.EndsWith("Proxy") &&
                   entityType.Assembly.GetName().Name.EndsWith("ProxyAssembly"))
                entityType = entityType.BaseType;
            this.entityType = entityType;
            this._nhibernateConfiguration = nhibernateConfiguration;
            _classMetadata = nhibernateConfiguration.GetClassMapping(this.entityType);
            _identifierCols = _classMetadata.Identifier.ColumnIterator;

            if (metaDataType1 != null)
            {
                var memberInfos =
                    metaDataType1.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
                foreach (var memberInfo in memberInfos)
                {
                    var attributes = memberInfo.GetCustomAttributes(false).Cast<Attribute>();
                    if (attributes.Any())
                        _metaDataAttributes.Add(memberInfo.Name, attributes);
                }
            }
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            if (_properties == null)
            {
                bool hasEntityAttributes = false;
                _properties = base.GetProperties();
                var list = new List<PropertyDescriptor>();
                foreach (PropertyDescriptor descriptor in _properties)
                {
                    List<Attribute> attrs = GetEntityMemberAttributes(descriptor).ToList();
                    if (_metaDataAttributes.ContainsKey(descriptor.Name))
                        attrs.AddRange(_metaDataAttributes[descriptor.Name]);
                    if (attrs.Any())
                    {
                        hasEntityAttributes = true;
                        list.Add(new PropertyDescriptorWrapper(descriptor, attrs.ToArray()));
                    }
                    else
                    {
                        list.Add(descriptor);
                    }
                }
                if (hasEntityAttributes)
                    _properties = new PropertyDescriptorCollection(list.ToArray(), true);
            }
            return _properties;
        }

        protected virtual IEnumerable<Attribute> GetEntityMemberAttributes(PropertyDescriptor propertyDescriptor)
        {
            if (_classMetadata == null)
                return null;
            var attributes = new List<Attribute>();

            //KeyAttributes
            if (_classMetadata.Identifier != null)
            {
                foreach (Column id in _identifierCols)
                {
                    if (id.Name == propertyDescriptor.Name)
                    {
                        if (propertyDescriptor.Attributes[typeof(KeyAttribute)] == null)
                        {
                            attributes.Add(new KeyAttribute());
                        }
                        if (propertyDescriptor.Attributes[typeof(EditableAttribute)] == null)
                        {
                            //An identifier is not editable, sometimes anyway it allow an initial value
                            var editable = new EditableAttribute(false);
                            if (id.Value is SimpleValue)
                                editable.AllowInitialValue =
                                    "assigned".Equals(((SimpleValue)id.Value).IdentifierGeneratorStrategy,
                                                      StringComparison.InvariantCultureIgnoreCase);
                            attributes.Add(editable);
                        }
                        break;
                    }
                }
            }
            Property member = _classMetadata.PropertyIterator.FirstOrDefault(x => x.Name == propertyDescriptor.Name);
            if (member == null)             //If ther's no mapping in nhibernate... 
                return attributes;
            //Required
            if ((!member.IsNullable) &&
                (propertyDescriptor.PropertyType.IsValueType &&
                 (propertyDescriptor.Attributes[typeof(RequiredAttribute)] == null)))
            {

                attributes.Add(new RequiredAttribute());
            }
            //Association
            if (member.Type.IsAssociationType &&
                (propertyDescriptor.Attributes[typeof(AssociationAttribute)] == null))
            {
                string name;
                string thisKey = "";
                string otherkey = "";
                if (member.Type.IsCollectionType)
                {
                    name = propertyDescriptor.ComponentType.FullName + "_" + member.Name;

                    if (member.Type.ReturnedClass.GetGenericArguments().Length != 1)
                    {
                        throw new Exception(
                            String.Format(
                                "The property {0} is not a generic collection as expected (like IList<T>)...",
                                member.Name));
                    }
                    Type targetClassType = member.Type.ReturnedClass.GetGenericArguments()[0];

                    foreach (Column col in _identifierCols)
                    {
                        thisKey += (thisKey != "" ? ", " : "") + col.Name;

                        //*****Naming convention****
                        //Here I'm assuming that the name of each field in the type that holds the foreign key observe
                        //the following structure: 

                        string field = member.Name.Replace(Inflector.Pluralize(targetClassType.Name), "") +
                            propertyDescriptor.ComponentType.Name + "_" + col.Name;

                        otherkey += (otherkey != "" ? ", " : "") + field;

                        if (targetClassType.GetProperties(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(x => x.Name == field) == null)
                            throw new Exception(String.Format("The class {0} doesn't contain a Property named {1}",
                                                targetClassType.Name, field));
                    }

                }
                else //Member is a class type
                {
                    //Key could be composite, cycle every identifier on "the other side"
                    PersistentClass otherMappingClass = _nhibernateConfiguration.GetClassMapping(member.Type.ReturnedClass);
                    foreach (Column col in otherMappingClass.Key.ColumnIterator)
                    {
                        //Naming Convention:
                        //The name of each foreign key field be MUST BE the name of the class field + "_" + the name of the key field in the targetclass
                        thisKey += (thisKey != "" ? ", " : "") + member.Name + "_" + col.Name;
                        otherkey += (otherkey != "" ? ", " : "") + col.Name;
                    }

                    //Check: this name MUST ALWAYS BE the same on the both side of a bi-directional association
                    name = member.Type.ReturnedClass.FullName
                        + "_" + Inflector.Pluralize(member.PersistentClass.NodeName);
                }

                //CHECK: When do you want to add an IncludeAttribute ?
                if (!_classMetadata.IsLazy)
                {
                    var incAttr = new IncludeAttribute();
                    attributes.Add(incAttr);
                }

                var attribute = new AssociationAttribute(
                    name,
                    thisKey,
                    otherkey
                    );
                Type fromParent = ForeignKeyDirection.ForeignKeyFromParent.GetType();
                attribute.IsForeignKey =
                    fromParent.IsInstanceOfType(((IAssociationType)member.Type).ForeignKeyDirection);
                attributes.Add(attribute);
            }
            //RoundtripOriginal
            if (member == _classMetadata.Version)
                attributes.Add(new RoundtripOriginalAttribute());
            return attributes.ToArray();
        }
    }
}
