using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenRiaServices.Tools.SharedTypes
{
    /// <summary>
    /// Subclass of <see cref="Type"/> used to create hypothetical types
    /// that may not yet exist.
    /// </summary>
    /// <remarks>
    /// Instances of this class are not fully-fledged types but are
    /// intended simply to let us describe a Type using conventions
    /// already known by Reflection clients.
    /// </remarks>
    internal class VirtualType : Type
    {
        private readonly string _name;
        private readonly string _namespaceName;
        private readonly Assembly _assembly;
        private readonly Type _baseType;
        private readonly List<MemberInfo> _declaredMembers = new List<MemberInfo>();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">The simple type name for this virtual type.</param>
        /// <param name="namespaceName">The namespace for this virtual type</param>
        /// <param name="assembly">The assembly to which this virtual type belongs.</param>
        /// <param name="baseType">The base type to use for this virtual type.  It may be null.</param>
        internal VirtualType(string name, string namespaceName, Assembly assembly, Type baseType)
        {
            this._name = name;
            this._namespaceName = namespaceName;
            this._assembly = assembly;
            this._baseType = baseType;
        }

        /// <summary>
        /// Creates a new instance using the given <paramref name="sourceType"/>
        /// </summary>
        /// <param name="sourceType">The type to use as the source to copy.</param>
        internal VirtualType(Type sourceType) : this(sourceType.Name, sourceType.Namespace, sourceType.Assembly, 
                                                    sourceType.BaseType == null ? null : new VirtualType(sourceType.BaseType))
        {
        }

        public override System.Reflection.Assembly Assembly
        {
            get { return this._assembly; }
        }

        public override string AssemblyQualifiedName
        {
            get { return this.FullName; }
        }

        public override Type BaseType
        {
            get { return this._baseType; }
        }

        public override string FullName
        {
            get { return string.IsNullOrEmpty(this.Namespace) ? this.Name : this.Namespace + "." + this.Name; }
        }

        public override Guid GUID
        {
            get { return default(Guid); }
        }

        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr)
        {
            return Array.Empty<ConstructorInfo>();
        }

        public override Type GetElementType()
        {
            return null;
        }

        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr)
        {
            return null;
        }

        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr)
        {
            return Array.Empty<EventInfo>();
        }

        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr)
        {
            return null;
        }

        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr)
        {
            return Array.Empty<FieldInfo>();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            return null;
        }

        public override Type[] GetInterfaces()
        {
            return Array.Empty<Type>();
        }

        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr)
        {
            return this._declaredMembers.ToArray();
        }

        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr)
        {
            return this._declaredMembers.OfType<MethodInfo>().ToArray();
        }

        public override Type GetNestedType(string name, System.Reflection.BindingFlags bindingAttr)
        {
            return null;
        }

        public override Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr)
        {
            return Array.Empty<Type>();
        }

        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr)
        {
            return Array.Empty<PropertyInfo>();
        }

        public override object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.Module Module
        {
            get { return null; }
        }

        public override string Namespace
        {
            get { return this._namespaceName; }
        }

        public override Type UnderlyingSystemType
        {
            get { return this; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return Array.Empty<object>();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return Array.Empty<object>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get { return this._name; }
        }

        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl()
        {
            return (TypeAttributes)0;
        }

        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, Type returnType, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            return null;
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        /// <summary>
        /// Adds the given member into the internal list of declared
        /// members.
        /// </summary>
        /// <param name="memberInfo">The member to consider declared by this type</param>
        internal void AddDeclaredMember(MemberInfo memberInfo)
        {
            this._declaredMembers.Add(memberInfo);
        }
    }
}
