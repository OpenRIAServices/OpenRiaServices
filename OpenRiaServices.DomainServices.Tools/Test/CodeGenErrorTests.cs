using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using OpenRiaServices.DomainServices.Server;
using OpenRiaServices.DomainServices.Server.Test.Utilities;
using OpenRiaServices.DomainServices.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;

namespace OpenRiaServices.DomainServices.Tools.Test
{
    using IgnoreAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.IgnoreAttribute;

    /// <summary>
    /// Summary description for domain service code gen with errors
    /// </summary>
    [TestClass]
    public class CodeGenErrorTests
    {
        [TestMethod]
        public void CodeGen_DomainService_InvokeOpReturnsInvalidType()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(DomainService_InvokeOpReturnsInvalidType), logger);
            TestHelper.AssertHasErrorThatStartsWith(logger, string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidInvokeOperation_ReturnType, "GetAssembly"));
        }

        [TestMethod]
        public void CodeGen_DomainService_InvokeOpReturnsInvalidTypes()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(DomainService_InvokeOpReturnsInvalidTypes), logger);
            TestHelper.AssertHasErrorThatStartsWith(logger, string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidInvokeOperation_ReturnType, "GetAssemblies"));
        }

        [TestMethod]
        public void CodeGen_DomainService_InvalidUpdateMethod()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(DomainService_InvalidUpdateMethod), logger);
            TestHelper.AssertHasErrorThatStartsWith(logger, string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "UpdateEntity"));
        }

        [TestMethod]
        [Ignore] // Test is flaky
        public void CodeGen_DomainService_CtorThrows()
        {
            ConsoleLogger logger = new ConsoleLogger();
            string generatedCode = TestHelper.GenerateCode("C#", typeof(DomainServiceCtorThrows), logger);
            TestHelper.AssertHasErrorThatStartsWith(logger, "Test");
        }

        [TestMethod]
        public void CodeGen_NoSelectMethod()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainMethod_ParamMustBeEntity, "entity", "Update");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NoSelectMethod), error);
        }

        [TestMethod]
        public void CodeGen_VoidReturning_Select()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_InvalidQueryOperationReturnType, typeof(void), "Get");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_VoidReturning_Select), error);
        }

        [TestMethod]
        public void CodeGen_OverloadedMethod_Select()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainOperationEntryOverload_NotSupported, "Get");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_OverloadedMethod_Select), error);
        }

        [TestMethod]
        public void CodeGen_NoParameter_Insert()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidInsertUpdateDeleteMethod_IncorrectParameterLength, "InsertNewEntity", "1");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NoParameter_Insert), error);
        }

        [TestMethod]
        public void CodeGen_WrongParameterType_Select()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_ParamMustBeSimple, "Get", "s");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_WrongParameterType_Select), error);
        }

        [TestMethod]
        public void CodeGen_Key_Type_Not_Supported()
        {
            string error = string.Format(Resource.EntityCodeGen_EntityKey_PropertyNotSerializable, typeof(Mock_CG_Key_Type_Not_Serializable), "KeyField");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_KeyTypeNotSerializable), error);

            error = string.Format(Resource.EntityCodeGen_EntityKey_KeyTypeNotSupported, typeof(Mock_CG_Key_Type_Complex), "KeyField", typeof(List<string>));
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_KeyTypeComplex), error);
        }

        [TestMethod]
        public void CodeGen_NonVoidReturning_Insert()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, "ReturnInt", "E");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NonVoidReturningInsert), error);
        }

        [TestMethod]
        public void CodeGen_NonVoidReturning_Update()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, "ReturnInt");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NonVoidReturningUpdate), error);
        }

        [TestMethod]
        public void CodeGen_NonVoidReturning_Delete()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, "ReturnInt");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NonVoidReturningDelete), error);
        }

        [TestMethod]
        public void CodeGen_NonVoidReturning_DomainMethod()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidDomainOperationEntry_NonQueryMustReturnVoid, "ReturnInt");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_NonVoidReturningDomainMethod), error);
        }

        [TestMethod]
        public void CodeGen_Include_NonAssociation()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.InvalidInclude_NonAssociationMember, typeof(Bug523677_Entity1).Name, "E");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Bug523677_Entity1_DomainService), error);
        }

        [TestMethod]
        [WorkItem(566732)]
        public void CodeGen_EntityWithStructProperty()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.Invalid_Entity_Property, typeof(Mock_CG_Entity_WithStructProperty).FullName, "StructProperty");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_WithStructProperty_DomainService), error);
        }

        [TestMethod]
        [Description("DomainService with EnableServiceAccess=false.")]
        public void CodeGen_InaccessibleProvider()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Tools.Resource.ClientCodeGen_InvalidDomainServiceType, typeof(InaccessibleProvider).Name);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(InaccessibleProvider), error);
        }

        [TestMethod]
        [Description("DomainService with a shadowing domain operation entry.")]
        public void CodeGen_ShadowingDomainOperationEntry()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainOperationEntryOverload_NotSupported, "Get");
            TestHelper.GenerateCodeAssertFailure("C#", typeof(DomainServiceWithShadowingMethod), error);
        }

        [TestMethod]
        [Description("Abstract DomainService.")]
        public void CodeGen_AbstractProvider()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, typeof(AbstractProvider).FullName);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(AbstractProvider), error);
        }

        [TestMethod]
        [Description("Generic DomainService.")]
        public void CodeGen_GenericProvider()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, typeof(GenericDomainService<>).FullName);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(GenericDomainService<>), error);
        }

        [TestMethod]
        [Description("DomainService which doesn't derive from the DomainService base-class.")]
        public void CodeGen_NonProvider()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.DomainService_InvalidType, typeof(NonDomainService).FullName);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(NonDomainService), error);
        }

        [TestMethod]
        [Description("DomainService references entity type with an invalid include")]
        public void CodeGen_Attribute_Entity_Invalid_Include()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.Invalid_Include_Invalid_Entity, "StringField", typeof(Mock_CG_Attr_Entity_Invalid_Include).Name, "String", OpenRiaServices.DomainServices.Server.Resource.EntityTypes_Cannot_Be_Primitives);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Invalid_Include_DomainService), error);
        }

        [TestMethod]
        [Description("DomainService references entity type with an excluded key")]
        public void CodeGen_Attribute_Entity_Excluded_Key()
        {
            string error = string.Format(OpenRiaServices.DomainServices.Server.Resource.Entity_Has_No_Key_Properties, typeof(Mock_CG_Attr_Entity_Excluded_Key).Name, typeof(Mock_CG_Attr_Entity_Excluded_Key_DomainService).Name);
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Attr_Entity_Excluded_Key_DomainService), error);
        }

        [TestMethod]
        [Description("DomainService references entity type with no default constructor")]
        public void CodeGen_Entity_No_Default_Constructor()
        {
            string error = "Type 'Mock_CG_Entity_No_Default_Constructor' is not a valid entity type.  Entity types must have a default constructor.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_No_Default_Constructor_DomainService), error);
        }

        [TestMethod]
        [Description("DomainService references generic entity type")]
        public void CodeGen_Generic_Entity()
        {
            string error = "Type 'Mock_CG_Generic_Entity`1' is not a valid entity type.  Entity types cannot be generic.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(DomainServiceWithGenericEntity), error);
        }

        [TestMethod]
        [Description("Verifies errors are generated when a non-simple type is used as a Dictionary generic argument.")]
        public void CodeGen_DictionaryMember_InvalidGenericArg()
        {
            string error = "Entity 'OpenRiaServices.DomainServices.Tools.Test.CodeGenErrorTests+Mock_CG_Entity_InvalidDictionaryMember' has a property 'UnsupportedMemberType' with an unsupported type.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_InvalidDictionaryMember_DomainService), error);
        }

        [TestMethod]
        [Description("Verifies errors are generated when an Entity type is used as a Dictionary generic argument.")]
        public void CodeGen_DictionaryMember_InvalidGenericArg_EntityUsedAsDictionaryTypeArg()
        {
            string error = "Entity 'OpenRiaServices.DomainServices.Tools.Test.CodeGenErrorTests+Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg' has a property 'EntityUsedAsDictionaryTypeArg' with an unsupported type.";
            TestHelper.GenerateCodeAssertFailure("C#", typeof(Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg_DomainService), error);
        }

        public class Mock_CG_Entity_InvalidDictionaryMember_DomainService : GenericDomainService<Mock_CG_Entity_InvalidDictionaryMember> { }

        public class Mock_CG_Entity_InvalidDictionaryMember
        {
            [Key]
            public int ID { get; set; }
            public Dictionary<string, List<string>> UnsupportedMemberType { get; set; }
        }

        public class Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg_DomainService : GenericDomainService<Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg>
        {
            [Query]
            public IEnumerable<MockEntity1> GetMockEntity1s() { throw new NotImplementedException(); }
        }

        public class Mock_CG_Entity_InvalidDictionaryMember_EntityUsedAsDictionaryTypeArg
        {
            [Key]
            public int ID { get; set; }
            public Dictionary<string, MockEntity1> EntityUsedAsDictionaryTypeArg { get; set; }
        }

        public class Mock_CG_Attr_Entity_Invalid_Include_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Invalid_Include> { }

        public partial class Mock_CG_Attr_Entity_Invalid_Include
        {
            [Key]
            public int KeyField { get; set; }

            [Include]
            public string StringField { get; set; }
        }

        public class Mock_CG_Entity_No_Default_Constructor_DomainService : GenericDomainService<Mock_CG_Entity_No_Default_Constructor> { }

        public partial class Mock_CG_Entity_No_Default_Constructor
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }

            private Mock_CG_Entity_No_Default_Constructor()
            {
                //
            }
        }

        [EnableClientAccess]
        public class DomainServiceWithGenericEntity : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_Generic_Entity<T>> GetItems<T>()
            {
                return null;
            }
        }

        public class Mock_CG_Generic_Entity<T>
        {
        }

        public class Mock_CG_Attr_Entity_Excluded_Key_DomainService : GenericDomainService<Mock_CG_Attr_Entity_Excluded_Key> { }

        public partial class Mock_CG_Attr_Entity_Excluded_Key
        {
            [Key]
            [Exclude]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }

        public partial class Mock_CG_SimpleEntity
        {
            [Key]
            public int KeyField { get; set; }

            public string StringField { get; set; }
        }

        public class Mock_CG_Entity_WithStructProperty_DomainService : GenericDomainService<Mock_CG_Entity_WithStructProperty> { }

        public class Mock_CG_Entity_WithStructProperty : Mock_CG_SimpleEntity
        {
            public Mock_CG_Entity_StructProperty StructProperty { get; set; }
        }

        public struct Mock_CG_Entity_StructProperty { }

        public class DomainServiceWithShadowingMethod : GenericDomainService<Mock_CG_SimpleEntity>
        {
            [Query]
            public new IEnumerable<Mock_CG_SimpleEntity> Get()
            {
                throw new NotImplementedException();
            }
        }

        public class DomainService_InvalidUpdateMethod : GenericDomainService<Mock_CG_SimpleEntity>
        {
            public void UpdateEntity(Mock_CG_SimpleEntity entity, bool x)
            {
            }
        }

        [EnableClientAccess]
        public class DomainService_InvokeOpReturnsInvalidType : DomainService
        {
            [Invoke]
            public Assembly GetAssembly()
            {
                return null;
            }
        }

        [EnableClientAccess]
        public class DomainService_InvokeOpReturnsInvalidTypes : DomainService
        {
            [Invoke]
            public IEnumerable<Assembly> GetAssemblies()
            {
                return null;
            }
        }

        [DomainServiceDescriptionProvider(typeof(Mock_DomainServiceDescriptionProvider))]
        public class DomainServiceCtorThrows : GenericDomainService<Mock_CG_SimpleEntity_WithTdp>
        {
            public DomainServiceCtorThrows()
            {
                throw new Exception("Test");
            }
        }

        // Make sure that DomainServiceCtorThrows only ends up associating a custom TDP with 
        // this entity and not Mock_CG_SimpleEntity, because that one is shared across 
        // multiple domain services.
        public class Mock_CG_SimpleEntity_WithTdp : Mock_CG_SimpleEntity
        {
        }

        public class Mock_DomainServiceDescriptionProvider : DomainServiceDescriptionProvider
        {
            private readonly Type _domainServiceType;
            private readonly DomainServiceDescriptionProvider _descriptionProvider;

            public Mock_DomainServiceDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent) 
                : base(domainServiceType, parent)
            {
                this._domainServiceType = domainServiceType;
                this._descriptionProvider = parent;
            }

            public override System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(Type type, System.ComponentModel.ICustomTypeDescriptor parent)
            {
                try
                {
                    Activator.CreateInstance(this._domainServiceType);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

                return null;
            }
        }

        #region Mock Entities and Domain Services
        public class Bug523677_Entity1_DomainService : GenericDomainService<Bug523677_Entity1> { }

        [DataContract]
        public class Bug523677_Entity1
        {
            [Key]
            [DataMember]
            public int ID
            {
                get;
                set;
            }

            // Include a non-association member - verify that
            // this generates an error
            [Include]
            public Bug523677_Entity2 E
            {
                get;
                set;
            }
        }

        [DataContract]
        public class Bug523677_Entity2
        {
            [Key]
            [DataMember]
            public int ID
            {
                get;
                set;
            }
        }

        [DataContract]
        public class Mock_CG_MinimalEntity
        {
            [DataMember]
            [Key]
            public int ID { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Not_Serializable
        {
            [Key]
            public int KeyField { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Complex
        {
            [DataMember]
            [Key]
            public List<string> KeyField { get; set; }
        }

        [DataContract]
        public class Mock_CG_Key_Type_Uri
        {
            [DataMember]
            [Key]
            public Uri KeyField { get; set; }
        }

        public struct Mock_CG_Struct
        {
            public int MockValue;
        }

        [EnableClientAccess]
        public class Mock_NonVoidReturningInsert : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get()
            {
                throw new NotImplementedException();
            }

            [Insert]
            public int ReturnInt(Mock_CG_MinimalEntity entity)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_NonVoidReturningUpdate : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get()
            {
                throw new NotImplementedException();
            }

            [Update]
            public int ReturnInt(Mock_CG_MinimalEntity entity)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_NonVoidReturningDelete : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get()
            {
                throw new NotImplementedException();
            }

            [Delete]
            public int ReturnInt(Mock_CG_MinimalEntity entity)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_NonVoidReturningDomainMethod : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get()
            {
                throw new NotImplementedException();
            }

            [Update(UsingCustomMethod = true)]
            public int ReturnInt(Mock_CG_MinimalEntity e)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_WrongParameterType_Select : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get(Mock_CG_Struct s)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_NoParameter_Insert : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get()
            {
                throw new NotImplementedException();
            }

            [Insert]
            public void InsertNewEntity()
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_OverloadedMethod_Select : DomainService
        {
            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get(int i)
            {
                throw new NotImplementedException();
            }


            [Query]
            public IEnumerable<Mock_CG_MinimalEntity> Get(bool b)
            {
                throw new NotImplementedException();
            }

        }

        [EnableClientAccess]
        public class Mock_VoidReturning_Select : DomainService
        {
            [Query]
            public void Get()
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_NoSelectMethod : DomainService
        {
            [Update]
            public void Update(Mock_CG_MinimalEntity entity)
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class Mock_KeyTypeNotSerializable : GenericDomainService<Mock_CG_Key_Type_Not_Serializable>
        {
        }

        [EnableClientAccess]
        public class Mock_KeyTypeComplex : GenericDomainService<Mock_CG_Key_Type_Complex>
        {
        }

        [EnableClientAccess]
        public class Mock_KeyTypeUri : GenericDomainService<Mock_CG_Key_Type_Uri>
        {
        }
        #endregion
    }
}
