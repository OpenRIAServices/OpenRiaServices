using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting.Wcf
{
    internal class DomainServiceSerializationSurrogate
#if ASPNET_CORE
        : ISerializationSurrogateProvider
#else
        : IDataContractSurrogate
#endif
    {
        private readonly DomainServiceDescription description;
        private readonly Dictionary<Type, (Type surrogateType, Func<object, object> surrogateFactory)> exposedTypeToSurrogateMap;
        private readonly HashSet<Type> surrogateTypes;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="description">A description of the <see cref="DomainService"/> this type creates surrogates for.</param>
        public DomainServiceSerializationSurrogate(DomainServiceDescription description)
        {
            this.description = description;
            this.surrogateTypes = new HashSet<Type>();
            this.exposedTypeToSurrogateMap = new Dictionary<Type, (Type, Func<object, object>)>();

            // Cache the list of entity types and surrogate types.
            HashSet<Type> exposedTypes = new HashSet<Type>(description.EntityTypes);

            // Because complex types and entities cannot share an inheritance relationship, we can add them to the same surrogate set.
            foreach (Type complexType in description.ComplexTypes)
            {
                exposedTypes.Add(complexType);
            }

            foreach (Type exposedType in exposedTypes)
            {
                Type surrogateType = DataContractSurrogateGenerator.GetSurrogateType(exposedTypes, exposedType);
                Func<object, object> surrogateFactory = (Func<object, object>)DynamicMethodUtility.GetFactoryMethod(surrogateType.GetConstructor(new Type[] { exposedType }), typeof(Func<object, object>));
                exposedTypeToSurrogateMap.Add(exposedType, (surrogateType, surrogateFactory));
                surrogateTypes.Add(surrogateType);
            }
        }

        public IReadOnlyCollection<Type> SurrogateTypes => this.surrogateTypes;

#if ASPNET_CORE
        public Type GetSurrogateType(Type type)
#else
        public Type GetDataContractType(Type type)
#endif
        {
            if (this.exposedTypeToSurrogateMap.TryGetValue(type, out var surrogateInfo))
            {
                return surrogateInfo.surrogateType;
            }
            return type;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (this.surrogateTypes.Contains(obj.GetType()))
            {
                return ((ICloneable)obj).Clone();
            }

            return obj;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            Type exposedType = this.description.GetSerializationType(obj.GetType());

            if (exposedType != null && (exposedType != targetType))
            {
                if (this.exposedTypeToSurrogateMap.TryGetValue(exposedType, out var surrogateInfo))
                {
                    // Return a new instance of the surrogate.
                    return surrogateInfo.surrogateFactory(obj);
                }
            }

            return obj;
        }

#if !ASPNET_CORE
        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return null;
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            return null;
        }

        public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
        {
            throw new NotImplementedException();
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
