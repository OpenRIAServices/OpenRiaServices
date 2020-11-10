using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using OpenRiaServices.Server;

namespace OpenRiaServices.Hosting
{
    internal class DomainServiceSerializationSurrogate : IDataContractSurrogate
    {
        private readonly DomainServiceDescription description;
        private readonly Dictionary<Type, Tuple<Type, Func<object, object>>> exposedTypeToSurrogateMap;
        private readonly HashSet<Type> surrogateTypes;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="description">A description of the <see cref="DomainService"/> this type creates surrogates for.</param>
        /// <param name="exposedTypeToSurrogateMap">
        /// The map of known exposed types to surrogate types. This object is passed in externally for efficiency reasons. Its 
        /// contents won't change; the set is owned by this type.
        /// </param>
        /// <param name="surrogateTypes">
        /// The set of known surrogate types. This object is passed in externally for efficiency reasons. Its contents 
        /// won't change; the set is owned by this type.
        /// </param>
        public DomainServiceSerializationSurrogate(DomainServiceDescription description, Dictionary<Type, Tuple<Type, Func<object, object>>> exposedTypeToSurrogateMap, HashSet<Type> surrogateTypes)
        {
            this.description = description;
            this.exposedTypeToSurrogateMap = exposedTypeToSurrogateMap;
            this.surrogateTypes = surrogateTypes;
        }

        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return null;
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            return null;
        }

        public Type GetDataContractType(Type type)
        {
            Tuple<Type, Func<object, object>> surrogateInfo;
            if (this.exposedTypeToSurrogateMap.TryGetValue(type, out surrogateInfo))
            {
                return surrogateInfo.Item1;
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

        public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
        {
            throw new NotImplementedException();
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            Type exposedType = this.description.GetSerializationType(obj.GetType());

            if (exposedType != null && (exposedType != targetType))
            {
                Tuple<Type, Func<object, object>> surrogateInfo;
                if (this.exposedTypeToSurrogateMap.TryGetValue(exposedType, out surrogateInfo))
                {
                    // Return a new instance of the surrogate.
                    return surrogateInfo.Item2(obj);
                }
            }

            return obj;
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
    }
}
