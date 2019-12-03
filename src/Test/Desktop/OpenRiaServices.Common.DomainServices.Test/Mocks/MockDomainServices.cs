using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using OpenRiaServices;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.EntityFramework;
using OpenRiaServices.DomainServices.Hosting;
using OpenRiaServices.DomainServices.Server;
using System.Web;
using System.Xml.Linq;
using DataModels.ScenarioModels;
using DataTests.AdventureWorks.LTS;
using OpenRiaServices.DomainServices.LinqToSql;
using TestDomainServices.Saleテ;
using System.Threading.Tasks;
using System.Threading;

[assembly: ContractNamespace("http://TestNamespace/ForNoClrNamespace")]

namespace TestDomainServices
{
    #region Invalid DomainServices

    public class OnErrorDomainService : DomainService
    {
        private readonly Cities.CityData _cityData = new Cities.CityData();

        public string ReplaceExceptionMessage
        {
            get;
            set;
        }

        public DomainServiceErrorInfo LastError
        {
            get;
            set;
        }

        protected override void OnError(DomainServiceErrorInfo errorInfo)
        {
            // save the error so tests can verify OnError was called correctly
            this.LastError = errorInfo;

            // log the original exception
            Exception e = errorInfo.Error;
            this.LogError(e);

            if (!String.IsNullOrEmpty(this.ReplaceExceptionMessage))
            {
                errorInfo.Error = new Exception(this.ReplaceExceptionMessage, e);
            }
        }

        private void LogError(Exception e)
        {
            // log the error to disk, including any nested
            // exception info, etc.
        }

        public IEnumerable<Cities.City> GetCities([Range(0, 5)]int a)
        {
            // example of a query method throwing an exception
            this.ThrowException();
            return null;
        }

        public IEnumerable<Cities.City> GetCitiesDeferredException()
        {
            yield return new Cities.City();
            throw new Exception("Query execution exception");
        }

        [Invoke]
        public void CityOperation([Range(0, 5)]int a)
        {
            ThrowException();
        }

        public void UpdateCity(Cities.City city)
        {
            // throw an exception with an inner exception
            ThrowException();
        }

        [EntityAction]
        public void CityCustomMethod(Cities.City city)
        {
            ThrowException();
        }

        private void ThrowException()
        {
            // throw a nested exception
            throw new Exception("Database meltdown",
                new Exception("Circuit boards on fire")
                );
        }
    }

    public class CachedQueryResultsDomainService : DomainService
    {
        public bool ExecutedQuery
        {
            get;
            private set;
        }

        public IEnumerable<Cities.City> GetCities()
        {
            yield return new Cities.City();
            this.ExecutedQuery = true;
            yield return new Cities.City();
        }
    }

    public class ResultLimitDomainService : DomainService
    {
        private static Cities.CityData s_cities = new Cities.CityData();

        protected override ValueTask<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(query.Count());
        }

        public IEnumerable<Cities.City> GetCities()
        {
            return s_cities.Cities;
        }

        [Query(ResultLimit = 0)]
        public IEnumerable<Cities.City> GetCities0()
        {
            return s_cities.Cities;
        }

        [Query(ResultLimit = -1)]
        public IEnumerable<Cities.City> GetCitiesM1()
        {
            return s_cities.Cities;
        }

        [Query(ResultLimit = 10)]
        public IEnumerable<Cities.City> GetCities10()
        {
            return s_cities.Cities;
        }
    }

    /// <summary>
    /// This mock provider is not marked with [EnableClientAccess], so should not
    /// be client accessible.
    /// </summary>
    public class InaccessibleProvider : DomainService
    {
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }
    }

    /// <summary>
    /// This mock provider is abstract.
    /// </summary>
    [EnableClientAccess]
    public abstract class AbstractProvider : DomainService
    {
    }

    /// <summary>
    /// This mock provider doesn't have a default constructor.
    /// </summary>
    [EnableClientAccess]
    public abstract class ProviderWithoutDefaultConstructor : DomainService
    {
        private ProviderWithoutDefaultConstructor()
        {
        }
    }

    /// <summary>
    /// This mock provider doesn't derive from DomainService
    /// </summary>
    [EnableClientAccess]
    public class NonDomainService
    {
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }
    }

    /// <summary>
    /// This domain service attempts to declare L2S description provider with EF context
    /// </summary>
    [EnableClientAccess]
    [LinqToSqlDomainServiceDescriptionProvider(typeof(AdventureWorksModel.AdventureWorksEntities))]
    public class InvalidLinqToSqlDomainServiceDescriptionProviderDS : DomainService
    {
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }
    }

    /// <summary>
    /// This domain service attempts to declare L2E description provider with L2S context
    /// </summary>
    [EnableClientAccess]
    [LinqToEntitiesDomainServiceDescriptionProvider(typeof(AdventureWorks))]
    public class InvalidLinqToEntitiesDomainServiceDescriptionProviderDS : DomainService
    {
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }
    }

    /// <summary>
    /// This domain service attempts to declare L2S description provider with a throwing L2S context
    /// </summary>
    [EnableClientAccess]
    [LinqToSqlDomainServiceDescriptionProvider(typeof(DataContextInstantiationScenarios))]
    public class LinqToSqlThrowingDataContextDS : DomainService
    {
        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }
    }

    #endregion // Invalid DomainServices

    #region DomainServices that throw exceptions

    /// <summary>
    /// This mock provider throws an exception in its constructor
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "TestDomainService")]
    public class ThrowingDomainService : DomainService
    {
        public ThrowingDomainService()
        {
            throw new InvalidOperationException("Can't construct this type.");
        }

        [Query]
        public IQueryable<A> GetProducts()
        {
            return null;
        }
    }

    /// <summary>
    /// This mock LINQ to SQL DomainService throws an exception in its constructor
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "TestDomainService")]
    public class ThrowingDomainServiceL2S : LinqToSqlDomainService<AdventureWorks>
    {
        public ThrowingDomainServiceL2S()
        {
            throw new InvalidOperationException("Couldn't construct this type.");
        }

        [Query]
        public IQueryable<Product> GetProducts()
        {
            return (Array.Empty<Product>()).AsQueryable();
        }

        [Query]
        public IQueryable<PurchaseOrder> GetPurchaseOrders()
        {
            return (Array.Empty<PurchaseOrder>()).AsQueryable();
        }

        [Query]
        public IQueryable<PurchaseOrderDetail> GetPurchaseOrderDetails()
        {
            return (Array.Empty<PurchaseOrderDetail>()).AsQueryable();
        }
    }

    /// <summary>
    /// This mock LINQ to Entities DomainService throws an exception in its constructor
    /// </summary>
    [EnableClientAccess]
    public class ThrowingDomainServiceL2E : LinqToEntitiesDomainService<ObjectContextInstantiationScenarios>
    {
        public ThrowingDomainServiceL2E()
        {
            string errorMessage = string.Format("Class '{0}' should not be instantiated", this.GetType().Name);
            throw new InvalidOperationException(errorMessage);
        }

        [Query]
        public IQueryable<AdventureWorksModel.Product> GetProducts()
        {
            return (new AdventureWorksModel.Product[0]).AsQueryable();
        }

        [Query]
        public IQueryable<AdventureWorksModel.PurchaseOrder> GetPurchaseOrders()
        {
            return (new AdventureWorksModel.PurchaseOrder[0]).AsQueryable();
        }

        [Query]
        public IQueryable<AdventureWorksModel.PurchaseOrderDetail> GetPurchaseOrderDetails()
        {
            return (new AdventureWorksModel.PurchaseOrderDetail[0]).AsQueryable();
        }
    }

    // TODO : Test cases
    // - a provider with no query methods (verify missing method exception)

    /// <summary>
    /// This provider doesn't have associated generated proxies - it's tested directly.
    /// </summary>
    [EnableClientAccess]
    [ServiceContract(Name = "TestDomainService")]
    public partial class TestCatalog1 : LinqToSqlDomainService<AdventureWorks>
    {
        private List<Product> products;

        public List<Product> Products
        {
            get
            {
                if (products == null)
                {
                    // for this mock provider, query all products once and cache so tests are performant
                    products = DataContext.Products.ToList();
                }
                return products;
            }
        }

        [Query]
        public IQueryable<Product> GetProductsMultipleParams(int subCategoryID, decimal minListPrice, string color)
        {
            return from p in Products.AsQueryable()
                   where p.ProductSubcategoryID == subCategoryID &&
                   p.ListPrice >= minListPrice &&
                   p.Color == color
                   select p;
        }

        [Query]
        public IEnumerable<Product> GetProducts_Enumerable_Composable()
        {
            return Products;
        }

        [Query(IsComposable = false)]
        public IEnumerable<Product> GetProducts_Enumerable_NotComposable()
        {
            return Products;
        }

        [Query]
        public IQueryable<Product> GetProducts_ReturnNull()
        {
            return null;
        }

        [Query]
        public IQueryable<Product> ThrowGeneralException()
        {
            throw new Exception("Athewmay Arelschay");
        }

        [Query]
        public IQueryable<Product> ThrowDataOperationException()
        {
            throw new DomainException("Athewmay Arelschay", 777);
        }
    }

    #endregion // DomainServices that throw exceptions

    #region Inheritance scenarios
    public class InheritanceBase
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public int T1_ID
        {
            get;
            set;
        }

        [Association("InheritanceBase_InheritanceT1", "T1_ID", "ID", IsForeignKey = true)]
        public InheritanceT1 T1
        {
            get;
            set;
        }

        [Association("InheritanceT1_InheritanceBase", "ID", "InheritanceBase_ID")]
        public IEnumerable<InheritanceT1> T1s
        {
            get;
            set;
        }
    }

    public class InheritanceT1
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        [Association("InheritanceBase_InheritanceT1", "ID", "T1_ID")]
        public InheritanceBase InheritanceBase
        {
            get;
            set;
        }

        public int InheritanceBase_ID
        {
            get;
            set;
        }

        [Association("InheritanceT1_InheritanceBase", "InheritanceBase_ID", "ID", IsForeignKey = true)]
        public InheritanceBase InheritanceBase2
        {
            get;
            set;
        }
    }

    // Base class for two derived children
    public class InheritanceA<TProp> : InheritanceBase
    {
        // Association in base class, which is inherited by all children
        [Association("InheritanceA_InheritanceD", "InheritanceD_ID", "ID", IsForeignKey = true)]
        public InheritanceD D
        {
            get;
            set;
        }

        public int InheritanceD_ID
        {
            get;
            set;
        }

        public TProp InheritanceAProp
        {
            get;
            set;
        }
    }

    public class InheritanceB : InheritanceA<string>
    {
        public string InheritanceBProp
        {
            get;
            set;
        }
    }

    public class InheritanceC : InheritanceA<string>
    {
        public string InheritanceCProp
        {
            get;
            set;
        }
    }

    public class InheritanceC2 : InheritanceC
    {
        public string InheritanceC2Prop
        {
            get;
            set;
        }
    }

    public class InheritanceD : InheritanceBase
    {
        [Association("InheritanceA_InheritanceD", "ID", "InheritanceD_ID")]
        public List<InheritanceA<string>> As
        {
            get;
            set;
        }

        public string InheritanceDProp
        {
            get;
            set;
        }
    }

    public class InheritanceE : InheritanceC
    {
        public string InheritanceEProp
        {
            get;
            set;
        }
    }

    /// <summary>
    /// In this provider, both the base and child are exposed
    /// </summary>
    [EnableClientAccess]
    public class TestProvider_Inheritance2 : DomainService
    {
        [Query]
        public IEnumerable<InheritanceC> GetCs()
        {
            throw new NotImplementedException();
        }

        [Query]
        public IEnumerable<InheritanceC2> GetC2s()
        {
            throw new NotImplementedException();
        }

        [Query]
        public IEnumerable<InheritanceD> GetDs()
        {
            throw new NotImplementedException();
        }
    }

    public class InheritanceTestData
    {
        private readonly List<InheritanceC> cs = new List<InheritanceC>();
        private readonly List<InheritanceE> es = new List<InheritanceE>();

        public InheritanceTestData()
        {
            es.Add(new InheritanceE
            {
                ID = 1,
                InheritanceAProp = "AVal",
                InheritanceCProp = "CVal",
                InheritanceEProp = "EVal"
            });
        }

        public IEnumerable<InheritanceC> Cs
        {
            get
            {
                return cs;
            }
        }

        public IEnumerable<InheritanceE> Es
        {
            get
            {
                return es;
            }
        }
    }

    /// <summary>
    /// This provider exposes leaf nodes of an inheritance hierarchy without
    /// exposing the base class. We expect the inheritance hierarchy to be
    /// flattened for these entities.
    /// </summary>
    [EnableClientAccess]
    public class TestProvider_Inheritance1 : DomainService
    {
        readonly InheritanceTestData data = new InheritanceTestData();

        [Query]
        public IEnumerable<InheritanceC> GetCs()
        {
            // return E instances via the base Type
            return data.Es.Cast<InheritanceC>();
        }

        [Query]
        public IEnumerable<InheritanceB> GetBs()
        {
            throw new NotImplementedException();
        }

        // Test for bug 625241 - here we expose InheritanceT1 and an association
        // InheritanceBase.T1 which B inherits. InheritanceT1 also has an association 
        // to InheritanceBase which is not exposed. We expect B.T1 to be generated, but
        // not InheritanceT1.InheritanceBase.
        [Query]
        public IEnumerable<InheritanceT1> GetInheritanceT1s()
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Codegen scenario mock
    /// <summary>
    /// DomainService and entities used to try out various corner cases
    /// for client codegen. Codegen is expected to succeed for this provider.
    /// </summary>
    [EnableClientAccess]
    public partial class TestProvider_Scenarios_CodeGen : DomainService
    {
        [Query]
        public IEnumerable<TestEntity_DataMemberBuddy> GetTestEntity_DataMemberBuddys()
        {
            return null;
        }
        [Query(IsComposable = false)]
        public IEnumerable<A> GetAs()
        {
            return null;
        }
        [Query(IsComposable = false, HasSideEffects = true)]
        public IEnumerable<A> GetAsWithSideEffects()
        {
            return null;
        }
        [Query]
        public IEnumerable<B> GetBs()
        {
            return null;
        }
        [Query]
        public IEnumerable<C> GetCs()
        {
            return null;
        }
        [Query]
        public IEnumerable<D> SelectDs1()
        {
            return null;
        }
        public IEnumerable<D> FetchDs2()
        {
            return null;
        }
        public IEnumerable<D> QueryDs3()
        {
            return null;
        }
        public IEnumerable<D> RetrieveDs4()
        {
            return null;
        }
        [Query]
        public IEnumerable<D> RetrieveDs5()
        {
            return null;
        }
        public IEnumerable<D> FindDs6()
        {
            return null;
        }
        [Ignore]
        public IEnumerable<D> GetDs7()
        {
            return null;
        }
        [Ignore]
        [Query]
        public IEnumerable<D> GetDs8()
        {
            return null;
        }
        public IEnumerable<D> RandomNameButStillAQuery()
        {
            return null;
        }
        public IEnumerable<D> GettyImages()
        {
            return null;
        }
        public IEnumerable<D> Get_Images()
        {
            return null;
        }
        // tests ability to generate enum and to handle nullable
        public IEnumerable<D> Get_Images_OfKind(ImageKindEnum? kind)
        {
            return null;
        }
        [Query]
        public IEnumerable<Turkishİ2> GetTurkishİ2()
        {
            return null;
        }
        [Invoke]
        public void OnlineMethod(D x)
        {
        }

        public IQueryable<SpecialDataTypes> GetSpecialDataTypes()
        {
            return null;
        }
    }

    /// <summary>
    /// In this test scenario, DataMemberAttribute for Prop1 is applied via
    /// the buddy class.
    /// </summary>
    [MetadataType(typeof(TestEntity_DataMemberBuddy_Metadata))]
    public partial class TestEntity_DataMemberBuddy
    {
        public int ID { get; set; }

        public int Prop1 { get; set; }
    }

    public partial class TestEntity_DataMemberBuddy_Metadata
    {
        [Key]
        public static int ID;

        [DataMember(Name = "P1", IsRequired = true)]
        public static int Prop1;
    }

    /// <summary>
    /// This class is used in perf tests and shouldn't have any validation
    /// attributes added to it.
    /// </summary>
    public class POCONoValidation
    {
        [Key]
        public int ID { get; set; }

        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string E { get; set; }
    }

    // This enum is not shared and will be generated on the client
    public enum ImageKindEnum
    {
        ThumbNail,
        Full
    }

    public class MultipartKeyTestEntity1
    {
        // don't expect this as part of the generated GetIdentity
        // null check
        [Key]
        public int A { get; set; }

        // should be part of the check
        [Key]
        public int? C { get; set; }

        // should be part of the check
        [Key]
        public string B { get; set; }

        // shouldn't be part of the check
        [Key]
        public char D { get; set; }
    }

    public class MultipartKeyTestEntity2
    {
        // this should be part of the generated GetIdentity null check
        [Key]
        public string B { get; set; }

        // should not be part of the check
        [Key]
        public int A { get; set; }

    }

    public class MultipartKeyTestEntity3
    {
        // don't expect this as part of the generated GetIdentity
        // null check
        [Key]
        public int A { get; set; }

        // should be part of the check
        [Key]
        public int? C { get; set; }

        // should not be part of the check
        [Key]
        public char B { get; set; }
    }

    public partial class SpecialDataTypes
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public IEnumerable<DateTime?> DateTimeProperty
        {
            get;
            set;
        }

        public List<bool?> BooleanProperty
        {
            get;
            set;
        }
    }

    public class TestSideEffects
    {
        [Key]
        public string Name
        {
            get;
            set;
        }

        public string Verb
        {
            get;
            set;
        }

        public Uri URL
        {
            get;
            set;
        }
    }

    public class TestCycles
    {
        [Key]
        public string Name
        {
            get;
            set;
        }

        public string ParentName
        {
            get;
            set;
        }

        // In this case neither the T nor the
        // Ts members are marked as Associations or Included. 
        // This test scenario verifies that we handle this properly
        public TestCycles T
        {
            get;
            set;
        }
        public List<TestCycles> Ts
        {
            get;
            set;
        }

        // Here, we are marking these cyclic properties with Include
        [Include]
        [Association("TestCycle_Parent", "ParentName", "Name", IsForeignKey = true)]
        public TestCycles IncludedT
        {
            get;
            set;
        }

        [Include]
        [Association("TestCycle_Parent", "Name", "ParentName")]
        public List<TestCycles> IncludedTs
        {
            get;
            set;
        }
    }

    public class RoundtripQueryEntity
    {
        [Key]
        public int ID { get; set; }

        public string PropB { get; set; }
        public string PropC { get; set; }
        public string Query { get; set; }
    }

    /// <summary>
    /// DomainService and entities used to try out various corner cases. Codegen is expected 
    /// to succeed for this provider.
    /// </summary>
    [EnableClientAccess]
    public partial class TestProvider_Scenarios : DomainService
    {
        private static int s_counter = 0;
        private readonly MixedTypeData _data = new MixedTypeData();
        private readonly MixedTypeData _dataSuperset = new MixedTypeData(true);

        private string query = string.Empty;

        public override ValueTask<ServiceQueryResult<T>> QueryAsync<T>(QueryDescription queryDescription, CancellationToken cancellationToken)
        {
            if (queryDescription.Method.Name == "GetRoundtripQueryEntities" && queryDescription.Query != null)
            {
                // This test query is used to test server query deserialization through the entire pipeline.
                this.query = queryDescription.Query.ToString();
            }

            return base.QueryAsync<T>(queryDescription, cancellationToken);
        }

        public IQueryable<RoundtripQueryEntity> GetRoundtripQueryEntities()
        {
            return new RoundtripQueryEntity[] { 
                new RoundtripQueryEntity { ID = 1, PropB = "Foo", PropC = "Bar", Query = this.query } }.AsQueryable();
        }

        [Invoke]
        public double RoundtripDouble(double d)
        {
            return d;
        }

        public IEnumerable<NullableFKParent> GetNullableFKParents()
        {
            return null;
        }
        public IEnumerable<RoundtripOriginal_TestEntity> GetRoundtripOriginal_TestEntities()
        {
            return null;
        }

        public void UpdateRoundtripOriginal_TestEntity(RoundtripOriginal_TestEntity entity)
        {

        }

        public IEnumerable<RoundtripOriginal_TestEntity2> GetRoundtripOriginal_ClassAttribute_TestEntities()
        {
            return null;
        }

        public IEnumerable<EntityWithDefaultDefaultValue> GetEntityWithDefaultValue()
        {
            return null;
        }

        public void UpdateRoundtripOriginal_ClassAttribute_TestEntity(RoundtripOriginal_TestEntity2 entity)
        {

        }

        public IEnumerable<Entity_TestEditableAttribute> GetEntity_TestEditableAttributes()
        {
            return null;
        }

        public void UpdateEntity_TestEditableAttribute(Entity_TestEditableAttribute entity)
        {

        }

        [Query]
        public IEnumerable<POCONoValidation> GetPOCONoValidations()
        {
            return null;
        }

        public void InsertPOCONoValidation(POCONoValidation e)
        {

        }

        public void UpdatePOCONoValidation(POCONoValidation e)
        {

        }

        public void DeletePOCONoValidation(POCONoValidation e)
        {

        }

        public IEnumerable<TimestampEntityA> GetTimestampEntityAs()
        {
            return new TimestampEntityA[] {
                new TimestampEntityA { ID = 1, Version = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, ValueA = "Foo", ValueB = "Bar" }
            };
        }

        public void UpdateTimestampEntityA(TimestampEntityA entity)
        {
            if (this.ChangeSet.GetOriginal(entity) != null)
            {
                throw new Exception("Expect original entity to be null!");
            }

            entity.ValueB += "ServerUpdated";
        }

        public IEnumerable<TimestampEntityB> GetTimestampEntityBs()
        {
            return new TimestampEntityB[] {
                new TimestampEntityB { ID = 1, Version = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, ValueA = "Foo", ValueB = "Bar" }
            };
        }

        public void UpdateTimestampEntityB(TimestampEntityB entity)
        {
            if (this.ChangeSet.GetOriginal(entity) == null)
            {
                throw new Exception("Expect original entity to be non-null!");
            }
            entity.ValueB += "ServerUpdated";
        }

        public IEnumerable<A> QueryWithParamValidation([Range(0, 10)] int a, [StringLength(2, MinimumLength = 0)] string b)
        {
            if (string.Compare(b, "ex", true) == 0)
            {
                throw new ValidationException("Server validation exception thrown!");
            }

            return null;
        }

        [Invoke]
        public bool InvokeOperationWithParamValidation([Range(0, 10)] int a, [StringLength(2, MinimumLength = 0)] string b, CityWithCacheData entity)
        {
            if (string.Compare(b, "ex", true) == 0)
            {
                throw new ValidationException("Server validation exception thrown!");
            }

            return true;
        }

        public A GetAReturnNull()
        {
            return null;
        }

        public IEnumerable<A> GetAsReturnNull()
        {
            return null;
        }

        [Query(HasSideEffects = true)]
        public IQueryable<TestSideEffects> CreateAndGetSideEffectsObjects(string name)
        {
            HttpRequest request = System.Web.HttpContext.Current.Request;
            return new TestSideEffects[] { 
                new TestSideEffects() 
                { 
                    Name = name, 
                    Verb = request.HttpMethod,
                    URL = request.Url,
                },
                new TestSideEffects()
                {
                    Name = "TestName", 
                    Verb = request.HttpMethod,
                    URL = request.Url,
                },
                new TestSideEffects()
                {
                    Name = "Foo", 
                    Verb = request.HttpMethod,
                    URL = request.Url,
                }
            }.AsQueryable();
        }

        [Query]
        public IQueryable<CityWithCacheData> GetCities()
        {
            s_counter++;
            return new CityWithCacheData[] {
                new CityWithCacheData() {
                    Name = "Redmond",
                    StateName = "WA",
                    CacheData = s_counter.ToString()
                },
                new CityWithCacheData() {
                    Name = "Portland",
                    StateName = "OR",
                    CacheData = s_counter.ToString()
                }
            }.AsQueryable();
        }

        [Query]
        [OutputCache(OutputCacheLocation.Client, 2)]
        public IQueryable<CityWithCacheData> GetCitiesWithCaching()
        {
            return GetCities();
        }

        private IQueryable<CityWithCacheData> GetCitiesWithCacheLocation()
        {
            HttpCachePolicy policy = HttpContext.Current.Response.Cache;
            
            HttpCacheability cacheability = (HttpCacheability)policy
                .GetType()
                .GetField("_cacheability", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(policy);

            return new CityWithCacheData[] {
                new CityWithCacheData() {
                    Name = "Redmond",
                    StateName = "WA",
                    CacheData = cacheability.ToString()
                }
            }.AsQueryable();
        }

        [Query]
        [OutputCache(OutputCacheLocation.Any, 1)]
        public IQueryable<CityWithCacheData> GetCitiesWithCacheLocationAny()
        {
            return GetCitiesWithCacheLocation();
        }

        [Query]
        [OutputCache(OutputCacheLocation.Downstream, 1)]
        public IQueryable<CityWithCacheData> GetCitiesWithCacheLocationDownstream()
        {
            return GetCitiesWithCacheLocation();
        }

        [Query]
        [OutputCache(OutputCacheLocation.Server, 1)]
        public IQueryable<CityWithCacheData> GetCitiesWithCacheLocationServer()
        {
            return GetCitiesWithCacheLocation();
        }

        [Query]
        [OutputCache(OutputCacheLocation.ServerAndClient, 1)]
        public IQueryable<CityWithCacheData> GetCitiesWithCacheLocationServerAndClient()
        {
            return GetCitiesWithCacheLocation();
        }

        [Query]
        [OutputCache("twoSecondsAbsoluteCaching")]
        public IQueryable<CityWithCacheData> GetCitiesWithCachingViaCacheProfile()
        {
            return GetCities();
        }

        // Cache on server because that's the only deterministic scenario.
        [Query]
        [OutputCache(OutputCacheLocation.Server, 5, VaryByHeaders = "foo")]
        public IQueryable<CityWithCacheData> GetCitiesWithCachingVaryByHeaders()
        {
            return GetCities();
        }

        [Query]
        [OutputCache(OutputCacheLocation.Client, 2)]
        public IQueryable<CityWithCacheData> GetCitiesWithCachingAndThrow()
        {
            throw new InvalidOperationException(s_counter++.ToString());
        }

        // Sliding expiration only applies to server-side caching.
        [Query]
        [OutputCache(OutputCacheLocation.Server, 2, UseSlidingExpiration = true)]
        public IQueryable<CityWithCacheData> GetCitiesWithCaching2()
        {
            return GetCities();
        }

        [Query]
        public IQueryable<CityWithCacheData> GetCitiesInState(string state)
        {
            if (state == null)
            {
                // there is a unit test that relies on this null
                // check being here
                throw new ArgumentNullException("state");
            }
            return GetCities().Where(c => c.StateName.Equals(state));
        }

        [Query]
        [OutputCache(OutputCacheLocation.Client, 2)]
        public IQueryable<CityWithCacheData> GetCitiesInStateWithCaching(string state)
        {
            return GetCitiesInState(state);
        }

        [Query]
        [OutputCache(OutputCacheLocation.Client, 2, UseSlidingExpiration = true)]
        public IQueryable<CityWithCacheData> GetCitiesInStateWithCaching2(string state)
        {
            return GetCitiesInState(state);
        }

        [Query]
        public IEnumerable<TestCycles> GetTestCyclesRoot()
        {
            TestCycles root = new TestCycles() { Name = "Root" };
            CreateTestCycles(root, 5, 2);
            return new List<TestCycles>() { root };
        }

        [Query]
        public IEnumerable<TestCycles> GetTestCyclesTier1()
        {
            TestCycles root = new TestCycles() { Name = "Root" };
            CreateTestCycles(root, 2, 16);
            return root.IncludedTs;
        }

        private static void CreateTestCycles(TestCycles node, int depth, int rate)
        {
            if (depth == 0)
            {
                return;
            }

            node.IncludedTs = node.Ts = new List<TestCycles>(rate);
            for (int i = 0; i < rate; ++i)
            {
                TestCycles childNode =
                    new TestCycles()
                    {
                        Name = string.Format("node_{0}_d{1}_p{2}", node.Name, depth, i),
                        IncludedT = node,
                        ParentName = node.Name,
                        T = node
                    };
                node.Ts.Add(childNode);
                CreateTestCycles(childNode, depth - 1, rate);
            }
        }

        [Query]
        public IQueryable<MixedType> GetMixedTypes()
        {
            return _data.Values.AsQueryable();
        }

        [Query]
        public IQueryable<MixedType> GetMixedTypeSuperset()
        {
            return _dataSuperset.Values.AsQueryable();
        }

        // Verify we generate the attributes and comments as expected.
        [Query]
        public IQueryable<MixedType> GetMixedTypes_BadAttributes(
            [CustomValidationAttribute(typeof(ServerOnlyValidator), "IsStringValid")] char broiled,
            [CustomValidationAttribute(typeof(ServerOnlyValidator), "IsStringValid")] string cheese)
        {
            return _data.Values.AsQueryable();
        }

        [Query]
        public IQueryable<MixedType> GetMixedTypesThrow()
        {
            throw new NotImplementedException("Not implemented yet.", new Exception("InnerException1", new Exception("InnerException2")));
        }

        #region Select methods testing exhaustive supported types
        [Query]
        public IQueryable<MixedType> GetMixedTypes_Primitive(string idToChange, Boolean b1, Byte b2, SByte sb, Int16 int16, UInt16 uint16, Int32 int32,
            UInt32 uint32, Int64 int64, UInt64 uint64, Char ch, Double d, Single s)
        {
            MixedType entity = _data.Values.FirstOrDefault(e => e.ID == idToChange);
            if (entity != null)
            {
                entity.BooleanProp = b1;
                entity.ByteProp = b2;
                entity.SByteProp = sb;
                entity.Int16Prop = int16;
                entity.UInt16Prop = uint16;
                entity.Int32Prop = int32;
                entity.UInt32Prop = uint32;
                entity.Int64Prop = int64;
                entity.UInt64Prop = uint64;
                entity.CharProp = ch;
                entity.DoubleProp = d;
                entity.SingleProp = s;
            }
            return _data.Values.AsQueryable();
        }

        [Query]
        public IQueryable<MixedType> GetMixedTypes_Predefined(string idToChange, string s, decimal d, DateTime dt, TimeSpan ts, DateTimeOffset dto, IEnumerable<string> strings, Uri uri, Guid g, Binary b, XElement x, byte[] bArray, TestEnum en, int[] ints, Dictionary<DateTime, DateTime> dictionaryDateTime, Dictionary<Guid, Guid> dictionaryGuid, Dictionary<String, String> dictionaryString, Dictionary<TestEnum, TestEnum> dictionaryTestEnum, Dictionary<XElement, XElement> dictionaryXElement, Dictionary<DateTimeOffset, DateTimeOffset> dictionaryDateTimeOffset)
        {
            MixedType entity = _data.Values.FirstOrDefault(e => e.ID == idToChange);
            if (entity != null)
            {
                entity.StringsProp = strings;
                entity.IntsProp = ints;
                entity.StringProp = s;
                entity.DecimalProp = d;
                entity.DateTimeProp = dt;
                entity.TimeSpanProp = ts;
                entity.DateTimeOffsetProp = dto;
                entity.GuidProp = g;
                entity.UriProp = uri;
                entity.BinaryProp = b;
                entity.ByteArrayProp = bArray;
                entity.XElementProp = x;
                entity.EnumProp = en;
                entity.DictionaryDateTimeProp = dictionaryDateTime;
                entity.DictionaryGuidProp = dictionaryGuid;
                entity.DictionaryStringProp = dictionaryString;
                entity.DictionaryTestEnumProp = dictionaryTestEnum;
                entity.DictionaryXElementProp = dictionaryXElement;
                entity.DictionaryDateTimeOffsetProp = dictionaryDateTimeOffset;
            }
            return _data.Values.AsQueryable();
        }

        [Query]
        public IQueryable<MixedType> GetMixedTypes_Nullable(string idToChange, Boolean? b1, Byte? b2, SByte? sb, Int16? int16, UInt16? uint16, Int32? int32,
            UInt32? uint32, Int64? int64, UInt64? uint64, Char? ch, Double? d, Single? s, decimal? dec, DateTime? dt, TimeSpan? ts, Guid? g, TestEnum? en,
            List<TimeSpan?> nullableTimeSpans, Dictionary<DateTime, DateTime?> nullableDictionaryDateTime)
        {
            MixedType entity = _data.Values.FirstOrDefault(e => e.ID == idToChange);
            if (entity != null)
            {
                entity.NullableTimeSpanListProp = nullableTimeSpans;
                entity.NullableBooleanProp = b1;
                entity.NullableByteProp = b2;
                entity.NullableSByteProp = sb;
                entity.NullableInt16Prop = int16;
                entity.NullableUInt16Prop = uint16;
                entity.NullableInt32Prop = int32;
                entity.NullableUInt32Prop = uint32;
                entity.NullableInt64Prop = int64;
                entity.NullableUInt64Prop = uint64;
                entity.NullableCharProp = ch;
                entity.NullableDoubleProp = d;
                entity.NullableSingleProp = s;
                entity.NullableDecimalProp = dec;
                entity.NullableDateTimeProp = dt;
                entity.NullableTimeSpanProp = ts;
                entity.NullableGuidProp = g;
                entity.NullableEnumProp = en;
                entity.NullableDictionaryDateTimeProp = nullableDictionaryDateTime;
            }
            return _data.Values.AsQueryable();
        }
        #endregion

        [Query]
        public IEnumerable<EntityWithXElement> GetXElemEntities()
        {
            List<EntityWithXElement> entities = new List<EntityWithXElement>();
            entities.Add(new EntityWithXElement
            {
                ID = 1,
                XElem = XElement.Parse("<Party>Who likes to party party</Party>")
            });
            entities.Add(new EntityWithXElement
            {
                ID = 2,
                XElem = XElement.Parse("<Party>We likes to party party</Party>")
            });

            return entities;
        }

        [Update]
        public void UpdateXElemEntity(EntityWithXElement current)
        {
            // no-op - just testing that the data gets here
            XElement xelem = current.XElem;
            if (string.IsNullOrEmpty(xelem.Value))
            {
                throw new DomainException("XElement is empty!");
            }
        }

        [Query(IsComposable = false)]
        public IEnumerable<A> GetAs()
        {
            return new A[]
            {
                new A("ReadOnlyData", "ReadOnlyData", "ReadOnlyData") {
                    ID = 1,
                    BID1 = 1,
                    BID2 = 1,
                },
                new A("ReadOnlyData", "ReadOnlyData", "ReadOnlyData") {
                    ID = 2,
                    BID1 = 2,
                    BID2 = 2,
                }
            };
        }

        [Query]
        public IEnumerable<B> GetBs()
        {
            return Array.Empty<B>();
        }

        [Update]
        public void UpdateA(A current)
        {
            // Make sure the value with a setter got roundtripped properly
            // The other two read-only values don't have setters so aren't
            // set by deserialization
            if (current.ReadOnlyData_WithSetter != "ReadOnlyData")
            {
                throw new DomainException("Value didn't round trip!");
            }

            if (current.ExcludedMember != 42)
            {
                throw new Exception("Excluded member was set!");
            }
        }

        [Query]
        public IEnumerable<C> GetCs()
        {
            return Array.Empty<C>();
        }

        [Insert]
        public void InsertC(C c)
        {
            // noop
        }

        [Insert]
        public void InsertD(D d)
        {
            // noop
        }

        [Query]
        public IEnumerable<D> GetDs()
        {
            return new D[] {
                new D() {
                    ID = 1,
                    BinaryData = new Binary(new byte[] { 20, 30 })
                }
            };
        }

        [Query]
        public IEnumerable<EntityWithDataContract> GetEntitiesWithDataContracts()
        {
            return new EntityWithDataContract[]
            {
                new EntityWithDataContract() {
                    Id = 1,
                    Data = "First",
                    IgnoredData = "Ignore"
                },
                new EntityWithDataContract() {
                    Id = 2,
                    Data = "Second",
                    IgnoredData = "Ignore"
                }
            };
        }

        [Query]
        public IEnumerable<EntityWithSpecialTypeName> GetEntitiesWithSpecialTypeName()
        {
            return new EntityWithSpecialTypeName[]
            {
                new EntityWithSpecialTypeName() {
                    Id = 1,
                    Data = "First"
                },
                new EntityWithSpecialTypeName() {
                    Id = 2,
                    Data = "Second"
                }
            };
        }

        [Query]
        public IEnumerable<EntityWithDataContract2> GetEntitiesWithDataContracts2()
        {
            return new EntityWithDataContract2[]
            {
                new EntityWithDataContract2() {
                    Id = 1,
                    Data = "First",
                    IgnoredData = "Ignore"
                },
                new EntityWithDataContract2() {
                    Id = 2,
                    Data = "Second",
                    IgnoredData = "Ignore"
                }
            };
        }

        [Query]
        public IEnumerable<Cart> GetCarts()
        {
            return null;
        }

        [Query]
        public IEnumerable<CartItem> GetCartItems()
        {
            return null;
        }

        [Insert]
        public void InsertCart(Cart c)
        {
            //
        }

        [Insert]
        public void InsertCartItem(CartItem c)
        {
            //
        }

        [Query]
        public IEnumerable<TestEntityForInvokeOperations> GetTestEntityForInvokeOperations()
        {
            return null;
        }

        #region Invoke operation tests
        [Invoke]
        [RequiresAuthentication]
        public void MethodRequiresAuthentication()
        {
        }

        [Invoke]
        public int VariousParameterTypes([Required] string str, int integer, bool boolean)
        {
            return str.Length + integer + (boolean ? 1 : 0);
        }

        [Invoke]
        public int IncrementBid1ForA(A a)
        {
            return a.BID1 + 1;
        }

        [Invoke]
        public int IncrementBid1ForABy(A a, [Range(5, 10)] int delta)
        {
            return a.BID1 + delta;
        }

        [Invoke]
        public void ThrowValidationException()
        {
            throw new ValidationException("Validation error.");
        }

        [Invoke]
        public void VoidMethod()
        {

        }

        [Invoke]
        public XElement ReturnsXElement(XElement value)
        {
            return value;
        }

        [Invoke]
        public Dictionary<string, int> ReturnsDictionary(Dictionary<string, int> value)
        {
            return value;
        }

        [Invoke]
        public void ThrowOnlineException()
        {
            throw new InvalidOperationException("Invalid operation.");
        }

        [Invoke]
        public IEnumerable<TestEntityForInvokeOperations> InvokeOpWithIEnumerableParam(IEnumerable<TestEntityForInvokeOperations> list)
        {
            return list;
        }

        [Invoke(HasSideEffects=false)]
        public IEnumerable<TestEntityForInvokeOperations> InvokeOpWithIEnumerableParamAndNoSideEffects(IEnumerable<TestEntityForInvokeOperations> list)
        {
            return list;
        }

        #endregion

        #region Domain methods testing exhaustive supported types
        [EntityAction]
        public void TestPrimitive(MixedType entity, Boolean b1, Byte b2, SByte sb, Int16 int16, UInt16 uint16, Int32 int32,
            UInt32 uint32, Int64 int64, UInt64 uint64, Char ch, Double d, Single s)
        {
            entity.BooleanProp = b1;
            entity.ByteProp = b2;
            entity.SByteProp = sb;
            entity.Int16Prop = int16;
            entity.UInt16Prop = uint16;
            entity.Int32Prop = int32;
            entity.UInt32Prop = uint32;
            entity.Int64Prop = int64;
            entity.UInt64Prop = uint64;
            entity.CharProp = ch;
            entity.DoubleProp = d;
            entity.SingleProp = s;
        }

        [EntityAction]
        // TODO: investigate blocking serialization format issues for XElement and uncomment the params below
        public void TestPredefined(MixedType entity, string s, decimal d, DateTime dt, TimeSpan ts, IEnumerable<string> strings, Uri uri, Guid g, Binary b, /*XElement x,*/ byte[] bArray, TestEnum en, Dictionary<string, string> dictionary, DateTimeOffset dto)
        {
            entity.StringProp = s;
            entity.DecimalProp = d;
            entity.DateTimeProp = dt;
            entity.TimeSpanProp = ts;
            entity.StringsProp = strings;
            entity.UriProp = uri;
            entity.GuidProp = g;
            entity.BinaryProp = b;
            entity.ByteArrayProp = bArray;
            //entity.XElementProp = x;
            entity.EnumProp = en;
            entity.DictionaryStringProp = dictionary;
            entity.DateTimeOffsetProp = dto;
        }

        [EntityAction]
        public void TestNullablePrimitive(MixedType entity, Boolean? b1, Byte? b2, SByte? sb, Int16? int16, UInt16? uint16, Int32? int32,
            UInt32? uint32, Int64? int64, UInt64? uint64, Char? ch, Double? d, Single? s)
        {
            entity.NullableBooleanProp = b1;
            entity.NullableByteProp = b2;
            entity.NullableSByteProp = sb;
            entity.NullableInt16Prop = int16;
            entity.NullableUInt16Prop = uint16;
            entity.NullableInt32Prop = int32;
            entity.NullableUInt32Prop = uint32;
            entity.NullableInt64Prop = int64;
            entity.NullableUInt64Prop = uint64;
            entity.NullableCharProp = ch;
            entity.NullableDoubleProp = d;
            entity.NullableSingleProp = s;
        }

        [EntityAction]
        public void TestNullablePredefined(MixedType entity, decimal? d, DateTime? dt, TimeSpan? ts, Guid? g, TestEnum? en, DateTimeOffset? dto)
        {
            entity.NullableDecimalProp = d;
            entity.NullableDateTimeProp = dt;
            entity.NullableTimeSpanProp = ts;
            entity.NullableGuidProp = g;
            entity.NullableEnumProp = en;
            entity.NullableDateTimeOffsetProp = dto;
        }

        #endregion

        #region Invoke operation testing exhaustive supported types
        [Invoke]
        public bool TestPrimitive_Online(MixedType entity, Boolean b1, Byte b2, SByte sb, Int16 int16, UInt16 uint16, Int32 int32,
            UInt32 uint32, Int64 int64, UInt64 uint64, Char ch, Double d, Single s)
        {
            entity.BooleanProp = b1;
            entity.ByteProp = b2;
            entity.SByteProp = sb;
            entity.Int16Prop = int16;
            entity.UInt16Prop = uint16;
            entity.Int32Prop = int32;
            entity.UInt32Prop = uint32;
            entity.Int64Prop = int64;
            entity.UInt64Prop = uint64;
            entity.CharProp = ch;
            entity.DoubleProp = d;
            entity.SingleProp = s;
            return true;
        }

        [Invoke]
        public bool TestPredefined_Online(MixedType entity, string s, decimal d, DateTime dt, TimeSpan ts, IEnumerable<string> strings, Uri uri, Guid g, Binary b, XElement x, byte[] bArray, TestEnum en, Guid[] guids, ulong[] ulongs, DateTimeOffset dto)
        {
            entity.StringProp = s;
            entity.DecimalProp = d;
            entity.DateTimeProp = dt;
            entity.TimeSpanProp = ts;
            entity.StringsProp = strings;
            entity.UriProp = uri;
            entity.GuidProp = g;
            entity.BinaryProp = b;
            entity.ByteArrayProp = bArray;
            entity.XElementProp = x;
            entity.EnumProp = en;
            entity.GuidsProp = guids;
            entity.UInt64sProp = ulongs;
            entity.DateTimeOffsetProp = dto;
            return true;
        }

        [Invoke]
        public bool TestNullable_Online(MixedType entity, Boolean? b1, Byte? b2, SByte? sb, Int16? int16, UInt16? uint16, Int32? int32,
            UInt32? uint32, Int64? int64, UInt64? uint64, Char? ch, Double? d, Single? s, decimal? dec, DateTime? dt, TimeSpan? ts, Guid? g, TestEnum? en,
            List<TimeSpan?> nullableTimeSpans, DateTimeOffset? dto)
        {
            entity.NullableBooleanProp = b1;
            entity.NullableByteProp = b2;
            entity.NullableSByteProp = sb;
            entity.NullableInt16Prop = int16;
            entity.NullableUInt16Prop = uint16;
            entity.NullableInt32Prop = int32;
            entity.NullableUInt32Prop = uint32;
            entity.NullableInt64Prop = int64;
            entity.NullableUInt64Prop = uint64;
            entity.NullableCharProp = ch;
            entity.NullableDoubleProp = d;
            entity.NullableSingleProp = s;
            entity.NullableDecimalProp = dec;
            entity.NullableDateTimeProp = dt;
            entity.NullableTimeSpanProp = ts;
            entity.NullableTimeSpanListProp = nullableTimeSpans;
            entity.NullableGuidProp = g;
            entity.NullableEnumProp = en;
            entity.NullableDateTimeOffsetProp = dto;
            return true;
        }

        [Invoke]
        public MixedType ReturnsEntity_Online(MixedType value, string id)
        {
            MixedTypeData data = new MixedTypeData();
            return data.Values.Single(p => p.ID == id);
        }

        [Invoke]
        public IEnumerable<MixedType> ReturnsEntityCollection_Online(int value)
        {
            MixedTypeData data = new MixedTypeData();
            return data.Values.Take(value);
        }

        [Invoke]
        public Boolean ReturnsBoolean_Online(Boolean value)
        {
            return value;
        }

        [Invoke]
        public Byte ReturnsByte_Online(Byte value)
        {
            return value;
        }

        [Invoke]
        public SByte ReturnsSByte_Online(SByte value)
        {
            return value;
        }

        [Invoke]
        public Int16 ReturnsInt16_Online(Int16 value)
        {
            return value;
        }

        [Invoke]
        public UInt16 ReturnsUInt16_Online(UInt16 value)
        {
            return value;
        }

        [Invoke]
        public Int32 ReturnsInt32_Online(Int32 value)
        {
            return value;
        }

        [Invoke]
        public UInt32 ReturnsUInt32_Online(UInt32 value)
        {
            return value;
        }

        [Invoke]
        public Int64 ReturnsInt64_Online(Int64 value)
        {
            return value;
        }

        [Invoke]
        public UInt64 ReturnsUInt64_Online(UInt64 value)
        {
            return value;
        }

        [Invoke]
        public Char ReturnsChar_Online(Char value)
        {
            return value;
        }

        [Invoke]
        public Double ReturnsDouble_Online(Double value)
        {
            return value;
        }

        [Invoke]
        public Single ReturnsSingle_Online(Single value)
        {
            return value;
        }

        [Invoke]
        public String ReturnsString_Online(String value)
        {
            return value;
        }

        [Invoke]
        public DateTime ReturnsDateTime_Online(DateTime value)
        {
            return value;
        }

        [Invoke]
        public TimeSpan ReturnsTimeSpan_Online(TimeSpan value)
        {
            return value;
        }

        [Invoke]
        public DateTimeOffset ReturnsDateTimeOffset_Online(DateTimeOffset value)
        {
            return value;
        }

        [Invoke]
        public IEnumerable<string> ReturnsStrings_Online(IEnumerable<string> value)
        {
            return value;
        }

        [Invoke]
        public DateTime[] ReturnsDateTimes_Online(DateTime[] value)
        {
            return value;
        }

        [Invoke]
        public DateTimeOffset[] ReturnsDateTimeOffsets_Online(DateTimeOffset[] value)
        {
            return value;
        }

        [Invoke]
        public List<TimeSpan> ReturnsTimeSpans_Online(List<TimeSpan> value)
        {
            return value;
        }

        [Invoke]
        public Decimal ReturnsDecimal_Online(Decimal value)
        {
            return value;
        }

        [Invoke]
        public byte[] ReturnsByteArray_Online(byte[] value)
        {
            return value;
        }

        [Invoke]
        public Binary ReturnsBinary_Online(Binary value)
        {
            return value;
        }

        [Invoke]
        public Uri ReturnsUri_Online(Uri value)
        {
            return value;
        }

        [Invoke]
        public Guid ReturnsGuid_Online(Guid value)
        {
            return value;
        }

        [Invoke]
        public TestEnum ReturnsEnum_Online(TestEnum value)
        {
            return value;
        }

        [Invoke]
        public Boolean? ReturnsNullableBoolean_Online(Boolean? value)
        {
            return value;
        }

        [Invoke]
        public Byte? ReturnsNullableByte_Online(Byte? value)
        {
            return value;
        }

        [Invoke]
        public SByte? ReturnsNullableSByte_Online(SByte? value)
        {
            return value;
        }

        [Invoke]
        public Int16? ReturnsNullableInt16_Online(Int16? value)
        {
            return value;
        }

        [Invoke]
        public UInt16? ReturnsNullableUInt16_Online(UInt16? value)
        {
            return value;
        }

        [Invoke]
        public Int32? ReturnsNullableInt32_Online(Int32? value)
        {
            return value;
        }

        [Invoke]
        public UInt32? ReturnsNullableUInt32_Online(UInt32? value)
        {
            return value;
        }

        [Invoke]
        public Int64? ReturnsNullableInt64_Online(Int64? value)
        {
            return value;
        }

        [Invoke]
        public UInt64? ReturnsNullableUInt64_Online(UInt64? value)
        {
            return value;
        }

        [Invoke]
        public Char? ReturnsNullableChar_Online(Char? value)
        {
            return value;
        }

        [Invoke]
        public Double? ReturnsNullableDouble_Online(Double? value)
        {
            return value;
        }

        [Invoke]
        public Single? ReturnsNullableSingle_Online(Single? value)
        {
            return value;
        }

        [Invoke]
        public DateTime? ReturnsNullableDateTime_Online(DateTime? value)
        {
            return value;
        }

        [Invoke]
        public DateTimeOffset? ReturnsNullableDateTimeOffset_Online(DateTimeOffset? value)
        {
            return value;
        }

        [Invoke]
        public TimeSpan? ReturnsNullableTimeSpan_Online(TimeSpan? value)
        {
            return value;
        }

        [Invoke]
        public Decimal? ReturnsNullableDecimal_Online(Decimal? value)
        {
            return value;
        }

        [Invoke]
        public Guid? ReturnsNullableGuid_Online(Guid? value)
        {
            return value;
        }

        [Invoke]
        public TestEnum? ReturnsNullableEnum_Online(TestEnum? value)
        {
            return value;
        }

        [Invoke(HasSideEffects = true)]
        public string ReturnHttpMethodWithSideEffects_Online()
        {
            return System.Web.HttpContext.Current.Request.HttpMethod;
        }

        [Invoke(HasSideEffects = false)]
        public string ReturnHttpMethodWithoutSideEffects_Online()
        {
            return System.Web.HttpContext.Current.Request.HttpMethod;
        }
        #endregion

        public IEnumerable<MultipartKeyTestEntity1> GetMultipartKeyTestEntity1s()
        {
            return null;
        }

        public IEnumerable<MultipartKeyTestEntity2> GetMultipartKeyTestEntity2s()
        {
            return null;
        }

        public IEnumerable<MultipartKeyTestEntity3> GetMultipartKeyTestEntity3s()
        {
            return null;
        }

        protected override ValueTask<bool> PersistChangeSetAsync(CancellationToken cancellationToken)
        {
            // Below is some test code to generate concurrency conflicts based
            // on client input
            IEnumerable<ChangeSetEntry> tsEntityUpdates = this.ChangeSet.ChangeSetEntries.Where(p => p.Entity.GetType() == typeof(TimestampEntityA) && p.Operation == DomainOperation.Update);
            foreach (ChangeSetEntry entry in tsEntityUpdates)
            {
                TimestampEntityA entity = (TimestampEntityA)entry.Entity;
                if (entity.ValueA == "TSConcurrencyFailure")
                {
                    byte[] serverVersion = new byte[] { 1, 2, 3 };
                    entry.ConflictMembers = new string[] { "Version" };
                    entry.StoreEntity = new TimestampEntityA { ID = entity.ID, ValueA = "ServerUpdatedValue", ValueB = entity.ValueB, Version = serverVersion };
                }
            }

            return base.PersistChangeSetAsync(cancellationToken);
        }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities : DomainService
    {
        public IEnumerable<EntityWithRoundtripOriginal_Derived> GetDerivedEntities()
        {
            return null;
        }

        public IEnumerable<EntityWithoutRoundtripOriginal_Base> GetBaseEntities()
        {
            return null;
        }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities2 : DomainService
    {
        public IEnumerable<RoundtripOriginalTestEntity_B> GetRTO_B_Entities()
        {
            return null;
        }

        public IEnumerable<RoundtripOriginalTestEntity_D> GetRTO_D_Entities()
        {
            return null;
        }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeB))]
    public class EntityWithCyclicMetadataTypeAttributeA
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeC))]
    public class EntityWithCyclicMetadataTypeAttributeB
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithCyclicMetadataTypeAttributeA))]
    public class EntityWithCyclicMetadataTypeAttributeC
    {
        [Key]
        public int Key { get; set; }
    }

    [MetadataType(typeof(EntityWithSelfReferencingcMetadataTypeAttribute))]
    public class EntityWithSelfReferencingcMetadataTypeAttribute
    {
        [Key]
        public int Key { get; set; }
    }

    public class EntityWithDefaultDefaultValue
    {
        [Key]
        [DefaultValue(0)]
        public int ID { get; set; }

        [DefaultValue(false)]
        public bool BoolProp { get; set; }

        [DefaultValue(0)]
        public float FloatProp { get; set; }

        [DefaultValue('\0')]
        public char CharProp { get; set; }

        [DefaultValue(0)]
        public byte ByteProp { get; set; }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities3 : DomainService
    {
        public IEnumerable<RTO_EntityWithRoundtripOriginalOnAssociationProperty> GetRTO_Entities()
        {
            return null;
        }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities4 : DomainService
    {
        public IEnumerable<RTO_EntityWithRoundtripOriginalOnAssociationPropType> GetRTO_Entities()
        {
            return null;
        }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities5 : DomainService
    {
        public IEnumerable<RTO_EntityWithRoundtripOriginalOnAssociationPropertyAndOnEntity> GetRTO_Entities()
        {
            return null;
        }
    }

    [EnableClientAccessAttribute]
    public class MockDomainService_WithRoundtripOriginalEntities6 : DomainService
    {
        public IEnumerable<RTO_EntityWithRoundtripOriginalOnMember> GetRTO_Entities()
        {
            return null;
        }
    }

    public class NullableFKParent
    {
        [Key]
        public int ID { get; set; }

        public string Data { get; set; }

        [Include]
        [Association("Parent_Child", "ID", "ParentID")]
        public IEnumerable<NullableFKChild> Children { get; set; }

        [Include]
        [Association("Parent_Child_Singleton", "ID", "ParentID_Singleton")]
        public NullableFKChild Child { get; set; }
    }

    public class NullableFKChild
    {
        [Key]
        public int ID { get; set; }

        public string Data { get; set; }

        // nullable FK
        public int? ParentID { get; set; }

        // nullable FK
        public int? ParentID_Singleton { get; set; }

        [Association("Parent_Child", "ParentID", "ID", IsForeignKey = true)]
        public NullableFKParent Parent { get; set; }

        [Association("Parent_Child_Singleton", "ParentID_Singleton", "ID", IsForeignKey = true)]
        public NullableFKParent Parent2 { get; set; }
    }

    #region Attribute Throwing DomainService, Entity, Attributes, and Exceptions

    [ThrowingService]
    [EnableClientAccess]
    public class AttributeThrowingDomainService : DomainService
    {
        public const string DomainContextTypeName = "AttributeThrowingDomainContext";
        public const string ThrowingQueryMethod = "GetThrowingQuery";
        public const string ThrowingQueryMethodParameter = "throwingQueryParam";
        public const string ThrowingUpdateMethod = "UpdateThrowing";
        public const string ThrowingUpdateMethodParameter = "throwingUpdateParam";
        public const string ThrowingInvokeMethod = "InvokeThrowing";
        public const string ThrowingInvokeMethodParameter = "throwingInvokeParam";

        [Query]
        [ThrowingQueryMethod]
        public IEnumerable<AttributeThrowingEntity> GetThrowing([ThrowingQueryMethodParameter] int throwingQueryParam)
        {
            throw new NotImplementedException();
        }

        [EntityAction]
        [ThrowingUpdateMethod]
        public void UpdateThrowing(AttributeThrowingEntity toUpdate, [ThrowingUpdateMethodParameter] int throwingUpdateParam)
        {
        }

        [Invoke]
        [ThrowingInvokeMethod]
        public bool InvokeThrowing([ThrowingInvokeMethodParameter] int throwingInvokeParam)
        {
            return false;
        }
    }

    [ThrowingEntity]
    public class AttributeThrowingEntity
    {
        public const string ThrowingPropertyName = "ThrowingProperty";
        public const string ThrowingAssociationProperty = "ThrowingAssociation";
        public const string ThrowingAssociationCollectionProperty = "ThrowingAssociationCollection";

        public AttributeThrowingEntity() { }

        [Key]
        public string NonThrowingProperty { get; set; }

        [ThrowingEntityProperty]
        public string ThrowingProperty { get; set; }

        [ThrowingEntityAssociation]
        [Association("Association", "ThrowingProperty", "NonThrowingProperty", IsForeignKey = true)]
        public AttributeThrowingEntity ThrowingAssociation { get; set; }

        [ThrowingEntityAssociationCollection]
        [Association("AssociationCollection", "NonThrowingProperty", "ThrowingProperty")]
        public IEnumerable<AttributeThrowingEntity> ThrowingAssociationCollection { get; set; }
    }

    public class ThrowingServiceAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingServiceAttributeProperty";
        public const string ExceptionMessage = "ThrowingServiceAttributeProperty throws a ThrowingServiceAttributeException";

        public ThrowingServiceAttribute() { }

        public string ThrowingServiceAttributeProperty
        {
            get { throw new ThrowingServiceAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingServiceAttributeException : Exception
    {
        public ThrowingServiceAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAttributeProperty throws a ThrowingEntityAttributeException";

        public ThrowingEntityAttribute() { }

        public string ThrowingEntityAttributeProperty
        {
            get { throw new ThrowingEntityAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAttributeException : Exception
    {
        public ThrowingEntityAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityPropertyAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityPropertyAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityPropertyAttributeProperty throws a ThrowingEntityPropertyAttributeException";

        public ThrowingEntityPropertyAttribute() { }

        public string ThrowingEntityPropertyAttributeProperty
        {
            get { throw new ThrowingEntityPropertyAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityPropertyAttributeException : Exception
    {
        public ThrowingEntityPropertyAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAssociationAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAssociationAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAssociationAttributeProperty throws a ThrowingEntityAssociationAttributeException";

        public ThrowingEntityAssociationAttribute() { }

        public string ThrowingEntityAssociationAttributeProperty
        {
            get { throw new ThrowingEntityAssociationAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAssociationAttributeException : Exception
    {
        public ThrowingEntityAssociationAttributeException(string message) : base(message) { }
    }

    public class ThrowingEntityAssociationCollectionAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingEntityAssociationCollectionAttributeProperty";
        public const string ExceptionMessage = "ThrowingEntityAssociationCollectionAttributeProperty throws a ThrowingEntityAssociationCollectionAttributeException";

        public ThrowingEntityAssociationCollectionAttribute() { }

        public string ThrowingEntityAssociationCollectionAttributeProperty
        {
            get { throw new ThrowingEntityAssociationCollectionAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingEntityAssociationCollectionAttributeException : Exception
    {
        public ThrowingEntityAssociationCollectionAttributeException(string message) : base(message) { }
    }

    public class ThrowingQueryMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingQueryMethodProperty";
        public const string ExceptionMessage = "ThrowingQueryMethodProperty throws a ThrowingQueryMethodAttributeException";

        public ThrowingQueryMethodAttribute() { }

        public string ThrowingQueryMethodProperty
        {
            get { throw new ThrowingQueryMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingQueryMethodAttributeException : Exception
    {
        public ThrowingQueryMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingQueryMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingQueryMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingQueryMethodParameterProperty throws a ThrowingQueryMethodParameterAttributeException";

        public ThrowingQueryMethodParameterAttribute() { }

        public string ThrowingQueryMethodParameterProperty
        {
            get { throw new ThrowingQueryMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingQueryMethodParameterAttributeException : Exception
    {
        public ThrowingQueryMethodParameterAttributeException(string message) : base(message) { }
    }

    public class ThrowingUpdateMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingUpdateMethodProperty";
        public const string ExceptionMessage = "ThrowingUpdateMethodProperty throws a ThrowingUpdateMethodAttributeException";

        public ThrowingUpdateMethodAttribute() { }

        public string ThrowingUpdateMethodProperty
        {
            get { throw new ThrowingUpdateMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingUpdateMethodAttributeException : Exception
    {
        public ThrowingUpdateMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingUpdateMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingUpdateMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingUpdateMethodParameterProperty throws a ThrowingUpdateMethodParameterAttributeException";

        public ThrowingUpdateMethodParameterAttribute() { }

        public string ThrowingUpdateMethodParameterProperty
        {
            get { throw new ThrowingUpdateMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingUpdateMethodParameterAttributeException : Exception
    {
        public ThrowingUpdateMethodParameterAttributeException(string message) : base(message) { }
    }

    public class ThrowingInvokeMethodAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingInvokeMethodProperty";
        public const string ExceptionMessage = "ThrowingInvokeMethodProperty throws a ThrowingInvokeMethodAttributeException";

        public ThrowingInvokeMethodAttribute() { }

        public string ThrowingInvokeMethodProperty
        {
            get { throw new ThrowingInvokeMethodAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingInvokeMethodAttributeException : Exception
    {
        public ThrowingInvokeMethodAttributeException(string message) : base(message) { }
    }

    public class ThrowingInvokeMethodParameterAttribute : Attribute
    {
        public const string ThrowingPropertyName = "ThrowingInvokeMethodParameterProperty";
        public const string ExceptionMessage = "ThrowingInvokeMethodParameterProperty throws a ThrowingInvokeMethodParameterAttributeException";

        public ThrowingInvokeMethodParameterAttribute() { }

        public string ThrowingInvokeMethodParameterProperty
        {
            get { throw new ThrowingInvokeMethodParameterAttributeException(ExceptionMessage); }
            set { }
        }
    }

    public class ThrowingInvokeMethodParameterAttributeException : Exception
    {
        public ThrowingInvokeMethodParameterAttributeException(string message) : base(message) { }
    }

    #endregion Attribute Throwing DomainService, Entity, Attributes, and Exceptions

    public partial class Entity_TestEditableAttribute
    {
        /// <summary>
        /// Generated as [Key, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        public int KeyField { get; set; }

        /// <summary>
        /// Generated as [Editable(true)]
        /// </summary>
        [Editable(true)]
        public string EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(false)]
        /// </summary>
        [Editable(false)]
        public string EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Editable(true)]
        /// </summary>
        [Editable(true, AllowInitialValue = true)]
        public string EditableTrue_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Editable(true, AllowInitialValue = false)]
        public string EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Editable(false, AllowInitialValue = true)]
        public string EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Editable(false)]
        /// </summary>
        [Editable(false, AllowInitialValue = false)]
        public string EditableFalse_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(false)]
        /// </summary>
        [Key]
        [Editable(false)]
        public int Key_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(true)]
        /// </summary>
        [Key]
        [Editable(true)]
        public int Key_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Editable(false, AllowInitialValue = true)]
        public int Key_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Key]
        [Editable(true, AllowInitialValue = false)]
        public int Key_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false)]
        /// </summary>
        [Timestamp]
        [Editable(false)]
        public int Timestamp_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(true)]
        /// </summary>
        [Timestamp]
        [Editable(true)]
        public int Timestamp_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Timestamp]
        [Editable(false, AllowInitialValue = true)]
        public int Timestamp_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(true, AllowInitialValue = false)]
        /// </summary>
        [Timestamp]
        [Editable(true, AllowInitialValue = false)]
        public int Timestamp_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(false)]
        public int ReadOnlyTrue_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(true)]
        public int ReadOnlyTrue_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(false, AllowInitialValue = true)]
        public int ReadOnlyTrue_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(true, AllowInitialValue = false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        [Editable(true, AllowInitialValue = false)]
        public int ReadOnlyTrue_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(false)]
        public int ReadOnlyFalse_EditableFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(true)]
        public int ReadOnlyFalse_EditableTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(false, AllowInitialValue = true)]
        public int ReadOnlyFalse_EditableFalse_AllowInitialValueTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true, AllowInitialValue = false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        [Editable(true, AllowInitialValue = false)]
        public int ReadOnlyFalse_EditableTrue_AllowInitialValueFalse { get; set; }

        /// <summary>
        /// Generated as [Key, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [System.ComponentModel.ReadOnly(true)]
        public int Key_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Key, ReadOnly(false), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [System.ComponentModel.ReadOnly(false)]
        public int Key_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        public int Key_Timestamp { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        [System.ComponentModel.ReadOnly(true)]
        public int Key_Timestamp_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Key, Timestamp, ReadOnly(true), Editable(false, AllowInitialValue = true)]
        /// </summary>
        [Key]
        [Timestamp]
        [System.ComponentModel.ReadOnly(false)]
        public int Key_Timestamp_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [Timestamp, Editable(false)]
        /// </summary>
        [Timestamp]
        public int Timestamp { get; set; }

        /// <summary>
        /// Generated as [Timestamp, ReadOnly(true), Editable(false)]
        /// </summary>
        [Timestamp]
        [System.ComponentModel.ReadOnly(true)]
        public int Timestamp_ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [Timestamp, ReadOnly(false), Editable(false)]
        /// </summary>
        [Timestamp]
        [System.ComponentModel.ReadOnly(false)]
        public int Timestamp_ReadOnlyFalse { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(true), Editable(false)]
        /// </summary>
        [System.ComponentModel.ReadOnly(true)]
        public int ReadOnlyTrue { get; set; }

        /// <summary>
        /// Generated as [ReadOnly(false), Editable(true)]
        /// </summary>
        [System.ComponentModel.ReadOnly(false)]
        public int ReadOnlyFalse { get; set; }
    }

    /// <summary>
    /// Test class used to verify codegen propagates ConcurrencyCheck,
    /// Timestamp and RoundtripAttributes correctly
    /// </summary>
    public class TimestampEntityA
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// We expect the client property to be marked with both
        /// Timestamp, ConcurrencyCheck, and RoundtripOriginal
        /// </summary>
        [Timestamp]
        [ConcurrencyCheck]
        public byte[] Version { get; set; }

        public string ValueA { get; set; }

        public string ValueB { get; set; }
    }

    public class TimestampEntityB
    {
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// We expect the client property to be marked with both
        /// Timestamp, ConcurrencyCheck, and RoundtripOriginal
        /// </summary>
        [Timestamp]
        [ConcurrencyCheck]
        public byte[] Version { get; set; }

        /// <summary>
        /// Non concurrency member that is still roundtripped. Since
        /// this is present, we still expect an original entity instance
        /// to be sent back to the server.
        /// </summary>
        [RoundtripOriginal]
        public string ValueA { get; set; }

        public string ValueB { get; set; }
    }

    public class RoundtripOriginal_TestEntity
    {
        [Key]
        public int ID { get; set; }

        [RoundtripOriginal]
        public int RoundtrippedMember { get; set; }

        public int NonRoundtrippedMember { get; set; }
    }

    [RoundtripOriginal]
    public class RoundtripOriginal_TestEntity2
    {
        [Key]
        public int ID { get; set; }

        // This member level RTO attribute is to test that the member level RTOs dont 
        // get propagated to the client if there is an RTO on the type. 
        [RoundtripOriginal]
        public int RoundtrippedMember1 { get; set; }

        public int RoundtrippedMember2 { get; set; }

        [Association("RTO_RTO2", "ID", "ID")]
        public RoundtripOriginal_TestEntity AssocProp { get; set; }
    }

    public class TestEntityForInvokeOperations
    {
        [Key]
        public int Key { get; set; }
        public string StrProp { get; set; }
        public TestCT CTProp { get; set; }
    }

    public class TestCT
    {
        public int CTProp1 { get; set; }
        public string CTProp2 { get; set; }
    }

    public class Cart
    {
        [Key]
        public int CartId
        {
            get;
            set;
        }

        [Include]
        [Association("CartItem_Cart", "CartId", "CartItemId")]
        public IEnumerable<CartItem> Items
        {
            get
            {
                // Returns null for test purposes. See InsertThrows_AssociationCollectionPropertyIsNull.
                return null;
            }
        }

        // Indexer should be ignored by code generator and other parts of the code.
        public string this[int index]
        {
            get
            {
                return null;
            }
            set
            {
            }
        }
    }

    public class CartItem
    {
        [Key]
        public int CartItemId
        {
            get;
            set;
        }

        public int CartId
        {
            get;
            set;
        }

        [Association("CartItem_Cart", "CartItemId", "CartId", IsForeignKey = true)]
        public Cart Cart
        {
            get;
            set;
        }

        public string Data
        {
            get;
            set;
        }
    }

    public class EntityWithXElement
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public XElement XElem
        {
            get;
            set;
        }
    }

    public class EntityWithIndexer
    {
        [Key]
        public int Prop1 { get; set; }
        // Indexer property. Should be ignored.
        public int this[int index]
        {
            get { return 0; }
            set { }
        }
    }

    public class CityWithCacheData
    {
        public CityWithCacheData()
        {
        }

        [Key]
        public string Name { get; set; }

        [Key]
        public string StateName { get; set; }

        public string CacheData { get; set; }
    }

    /// <summary>
    /// This server-only valicator should not be exposed as a shared type.
    /// </summary>
    public static class ServerOnlyValidator
    {
        public static ValidationResult IsStringValid(string name, ValidationContext context)
        {
            return ValidationResult.Success;
        }

        public static ValidationResult IsObjectValid(A a, ValidationContext context)
        {
            return ValidationResult.Success;
        }
    }

    [CustomValidation(typeof(ServerOnlyValidator), "IsObjectValid")]
    [DataContract]
    public class A
    {
        private readonly string readOnlyData_NoSetter;
        private string readOnlyData_WithSetter;
        private readonly string readOnlyData_NoReadOnlyAttribute;
        private int excludedMember = 42;

        public A()
        {
        }

        public A(string readOnlyData_NoSetter, string readOnlyData_WithSetter, string readOnlyData_NoReadOnlyAttribute)
        {
            this.readOnlyData_NoSetter = readOnlyData_NoSetter;
            this.readOnlyData_WithSetter = readOnlyData_WithSetter;
            this.readOnlyData_NoReadOnlyAttribute = readOnlyData_NoReadOnlyAttribute;
        }

        [Key]
        [DataMember]
        public int ID
        {
            get;
            set;
        }

        [DataMember]
        public int BID1
        {
            get;
            set;
        }

        [DataMember]
        public int BID2
        {
            get;
            set;
        }

        [StringLength(1234, ErrorMessageResourceType = typeof(string), ErrorMessageResourceName = "NonExistentProperty")]
        [CustomValidation(typeof(ServerOnlyValidator), "IsStringValid")]
        [Required]
        [Editable(true)]
        [DataMember]
        [CustomNamespace.Custom]
        public string RequiredString
        {
            get;
            set;
        }

        // Verify a one way singleton association (B doesn't have a collection for this association A_B)
        [Association("A_B", "BID1, BID2", "ID1, ID2", IsForeignKey = true)]
        public B B
        {
            get;
            set;
        }

        /// <summary>
        /// Read only because of the [Editable(false)] attribute and the fact that
        /// there is no setter - we expect [Editable(false)] to be applied to 
        /// the generated member
        /// </summary>
        [DataMember]
        [Editable(false)]
        public string ReadOnlyData_NoSetter
        {
            get
            {
                return this.readOnlyData_NoSetter;
            }
        }

        /// <summary>
        /// Read only because of the [Editable(false)] attribute - we expect [Editable(false)]
        /// to be applied to the generated member
        /// </summary>
        [DataMember]
        [Editable(false)]
        public string ReadOnlyData_WithSetter
        {
            get
            {
                return this.readOnlyData_WithSetter;
            }
            set
            {
                this.readOnlyData_WithSetter = value;
            }
        }

        /// <summary>
        /// Read only because there is no setter - we expect [Editable(false)]
        /// to be applied to the generated member
        /// </summary>
        [DataMember]
        public string ReadOnlyData_NoReadOnlyAttribute
        {
            get
            {
                return this.readOnlyData_NoReadOnlyAttribute;
            }
        }

        /// <summary>
        /// This member is used in a test to verify that even if the client
        /// sends a value for an excluded member, it is never set.
        /// </summary>
        [DataMember]
        [Exclude]
        public int ExcludedMember
        {
            get
            {
                return this.excludedMember;
            }
            set
            {
                this.excludedMember = value;
                // this exception will verify that during deserialization
                // the setter is never called
                //throw new Exception("Excluded member should not be set!");
            }
        }
    }

    [DataContract]
    public class B
    {
        [Key]
        [DataMember]
        public int ID1
        {
            get;
            set;
        }
        [Key]
        [DataMember]
        public int ID2
        {
            get;
            set;
        }

        // verify a one way collection association (C doesn't have a ref back for this association B_C)
        [Association("B_C", "ID1, ID2", "BID1, BID2")]
        [Display(Description = "Cs")]
        public IEnumerable<C> Cs
        {
            get;
            set;
        }
    }

    [DataContract]
    public class C
    {
        [Key]
        [DataMember]
        public int ID
        {
            get;
            set;
        }

        // Below we have two FK values referencing a B, but
        // no actual association member.  These are used for a
        // one way collection association from B to C.
        [DataMember]
        public int BID1
        {
            get;
            set;
        }

        [DataMember]
        public int BID2
        {
            get;
            set;
        }

        [DataMember]
        public int DID_Ref1
        {
            get;
            set;
        }

        [DataMember]
        public int DID_Ref2
        {
            get;
            set;
        }

        // verify below that we can have two different 1:1 associations between two entities
        [Association("C_D_Ref1", "DID_Ref1", "ID", IsForeignKey = true)]
        [Display(Description = "D_Ref1")]
        public D D_Ref1
        {
            get;
            set;
        }

        [Association("C_D_Ref2", "DID_Ref2", "ID", IsForeignKey = true)]
        public D D_Ref2
        {
            get;
            set;
        }
    }

    [DataContract]
    public class D
    {
        [Key]
        [DataMember]
        [UIHint("TextBlock")]
        [Range(0, 99999)]
        public int ID
        {
            get;
            set;
        }

        [DataMember]
        public int DSelfRef_ID1
        {
            get;
            set;
        }

        [DataMember]
        public int DSelfRef_ID2
        {
            get;
            set;
        }

        // verify that we can have a non FK singleton association
        [Association("C_D_Ref1", "ID", "DID_Ref1")]
        public C C
        {
            get;
            set;
        }

        // verify that we can have a bi-directional self reference,
        // with singleton and collection-sides.
        // For example Employee->Employee (employee's manager reference)
        // For this test, it's important that the collection side of the
        // association is defined first
        [Association("D_D", "ID", "DSelfRef_ID1")]
        public IEnumerable<D> Ds
        {
            get;
            set;
        }

        [Association("D_D", "DSelfRef_ID1", "ID", IsForeignKey = true)]
        [Include("ID", "ProjectedD1ID")]
        [Include("BinaryData", "ProjectedD1BinaryData")]
        public D D1
        {
            get;
            set;
        }

        // verify that we can have a bi-directional self reference,
        // with both singleton sides
        // For example Employee->Employee (employee has a single mentor, and a 
        // mentor has a single employee)
        [Association("D_D2", "ID", "DSelfRef_ID2")]
        public D D2_BackRef
        {
            get;
            set;
        }

        [Association("D_D2", "DSelfRef_ID2", "ID", IsForeignKey = true)]
        public D D2
        {
            get;
            set;
        }

        [DataMember]
        public Binary BinaryData
        {
            get;
            set;
        }
    }

    [DataContract]
    public class Turkishİ2
    {
        [DataMember]
        [Key]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }
    }

    // This entity should only be used by an invoke operation in TestProvider_Scenarios_CodeGen to 
    // verify that we generate the right code for this type of scenario.
    [DataContract]
    public class EntityUsedInOnlineMethod
    {
        [Key]
        public int Id
        {
            get;
            set;
        }
    }

    [DataContract(Namespace = "CustomNamespace", Name = "CustomName")]
    public class EntityWithDataContract
    {
        [DataMember]
        [Key]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }

        public string IgnoredData
        {
            get;
            set;
        }
    }

    // Tests [IgnoreDataMember]
    public class EntityWithDataContract2
    {
        [Key]
        public int Id
        {
            get;
            set;
        }

        public string Data
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public string IgnoredData
        {
            get;
            set;
        }
    }

    namespace Saleテ
    {
        public class EntityWithSpecialTypeName
        {
            [DataMember]
            [Key]
            public int Id
            {
                get;
                set;
            }

            [DataMember]
            public string Data
            {
                get;
                set;
            }

            public string IgnoredData
            {
                get;
                set;
            }
        }
    }
    #endregion

    #region Include scenarios
    public partial class IncludesA
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        public string P2
        {
            get;
            set;
        }

        // Three projections off of B, one directly, and the
        // other two via metadata below
        [Include("P1", "BP1")]
        public IncludesB B
        {
            get;
            set;
        }
    }

    [MetadataType(typeof(IncludesAMetadata))]
    public partial class IncludesA
    {

    }

    public class IncludesAMetadata
    {
        [Include("C.P1", "CP1")]
        [Include("C.D.P2", "DP2")]
        public static object B;
    }

    public class IncludesB
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        [Include("D.P1", "DP1")]
        public IncludesC C
        {
            get;
            set;
        }
    }

    public class IncludesC
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        // non-public property
        private string P2
        {
            get;
            set;
        }

        public IncludesD D
        {
            get;
            set;
        }
    }

    public class IncludesD
    {
        [Key]
        public int ID
        {
            get;
            set;
        }

        public string P1
        {
            get;
            set;
        }

        public string P2
        {
            get;
            set;
        }

        // Unsupported member type
        public object P3
        {
            get;
            set;
        }
    }

    [EnableClientAccess]
    public class IncludeScenariosTestProvider : DomainService
    {
        private readonly List<IncludesA> testAs = new List<IncludesA>();

        public IncludeScenariosTestProvider()
        {
            // fully linked hierarchy
            IncludesA a = new IncludesA
            {
                ID = 1,
                P1 = "AP1",
                P2 = "AP2",
                B = new IncludesB
                {
                    ID = 1,
                    P1 = "BP1",
                    C = new IncludesC
                    {
                        ID = 1,
                        P1 = "CP1",
                        D = new IncludesD
                        {
                            ID = 1,
                            P1 = "DP1",
                            P2 = "DP2"
                        }
                    }
                }
            };
            testAs.Add(a);

            // null link : A.B.C == null
            a = new IncludesA
            {
                ID = 2,
                P1 = "AP1",
                P2 = "AP2",
                B = new IncludesB
                {
                    ID = 2,
                    P1 = "BP1",
                    C = null
                }
            };
            testAs.Add(a);
        }

        [Query]
        public IEnumerable<IncludesA> GetAs()
        {
            return testAs;
        }
    }
    #endregion

    #region Test classes using supported types as properties
    [DataContract]
    public class MixedType
    {
        [Key]
        [DataMember]
        public string ID { get; set; }

        #region supported primitive types
        [DataMember]
        public bool BooleanProp { get; set; }

        [DataMember]
        public byte ByteProp { get; set; }

        [DataMember]
        public sbyte SByteProp { get; set; }

        [DataMember]
        public Int16 Int16Prop { get; set; }

        [DataMember]
        public UInt16 UInt16Prop { get; set; }

        [DataMember]
        public Int32 Int32Prop { get; set; }

        [DataMember]
        public UInt32 UInt32Prop { get; set; }

        [DataMember]
        public Int64 Int64Prop { get; set; }

        [DataMember]
        public UInt64 UInt64Prop { get; set; }

        [DataMember]
        public char CharProp { get; set; }

        [DataMember]
        public double DoubleProp { get; set; }

        [DataMember]
        public Single SingleProp { get; set; }
        #endregion

        #region predefined types
        [DataMember]
        public string StringProp { get; set; }

        [DataMember]
        public decimal DecimalProp { get; set; }

        [DataMember]
        public DateTime DateTimeProp { get; set; }

        [DataMember]
        public TimeSpan TimeSpanProp { get; set; }

        [DataMember]
        public DateTimeOffset DateTimeOffsetProp { get; set; }

        [DataMember]
        public IEnumerable<string> StringsProp { get; set; }

        [DataMember]
        public IEnumerable<DateTime> DateTimesCollectionProp { get; set; }

        [DataMember]
        public IEnumerable<DateTimeOffset> DateTimeOffsetsCollectionProp { get; set; }

        [DataMember]
        public List<TimeSpan> TimeSpanListProp { get; set; }

        [DataMember]
        public Guid[] GuidsProp { get; set; }

        [DataMember]
        public ulong[] UInt64sProp { get; set; }

        [DataMember]
        public int[] IntsProp { get; set; }

        [DataMember]
        public TestEnum[] EnumsProp { get; set; }

        [DataMember]
        public Uri UriProp { get; set; }

        [DataMember]
        public Guid GuidProp { get; set; }

        [DataMember]
        public Binary BinaryProp { get; set; }

        [DataMember]
        public byte[] ByteArrayProp { get; set; }

        [DataMember]
        public XElement XElementProp { get; set; }

        [DataMember]
        public TestEnum EnumProp { get; set; }

        [DataMember]
        public IDictionary<string, string> DictionaryStringProp { get; set; }

        [DataMember]
        public IDictionary<DateTime, DateTime> DictionaryDateTimeProp { get; set; }

        [DataMember]
        public IDictionary<DateTimeOffset, DateTimeOffset> DictionaryDateTimeOffsetProp { get; set; }

        [DataMember]
        public IDictionary<Guid, Guid> DictionaryGuidProp { get; set; }

        [DataMember]
        public IDictionary<XElement, XElement> DictionaryXElementProp { get; set; }

        [DataMember]
        public IDictionary<TestEnum, TestEnum> DictionaryTestEnumProp { get; set; }
        #endregion

        #region nullable primitive
        [DataMember]
        public bool? NullableBooleanProp { get; set; }

        [DataMember]
        public byte? NullableByteProp { get; set; }

        [DataMember]
        public sbyte? NullableSByteProp { get; set; }

        [DataMember]
        public Int16? NullableInt16Prop { get; set; }

        [DataMember]
        public UInt16? NullableUInt16Prop { get; set; }

        [DataMember]
        public Int32? NullableInt32Prop { get; set; }

        [DataMember]
        public UInt32? NullableUInt32Prop { get; set; }

        [DataMember]
        public Int64? NullableInt64Prop { get; set; }

        [DataMember]
        public UInt64? NullableUInt64Prop { get; set; }

        [DataMember]
        public char? NullableCharProp { get; set; }

        [DataMember]
        public double? NullableDoubleProp { get; set; }

        [DataMember]
        public Single? NullableSingleProp { get; set; }
        #endregion

        #region nullable predefined
        [DataMember]
        public decimal? NullableDecimalProp { get; set; }

        [DataMember]
        public DateTime? NullableDateTimeProp { get; set; }

        [DataMember]
        public TimeSpan? NullableTimeSpanProp { get; set; }

        [DataMember]
        public DateTimeOffset? NullableDateTimeOffsetProp { get; set; }

        [DataMember]
        public Guid? NullableGuidProp { get; set; }

        [DataMember]
        public TestEnum? NullableEnumProp { get; set; }

        [DataMember]
        public DateTime?[] NullableEnumsArrayProp { get; set; }

        [DataMember]
        public IEnumerable<DateTime?> NullableDateTimesCollectionProp { get; set; }

        [DataMember]
        public List<TimeSpan?> NullableTimeSpanListProp { get; set; }

        [DataMember]
        public IEnumerable<DateTimeOffset?> NullableDateTimeOffsetCollectionProp { get; set; }

        [DataMember]
        public IDictionary<DateTime, DateTime?> NullableDictionaryDateTimeProp { get; set; }

        [DataMember]
        public IDictionary<DateTimeOffset, DateTimeOffset?> NullableDictionaryDateTimeOffsetProp { get; set; }
        #endregion
    }

    // helper class that fully instantiates a few MixedTypes
    public class MixedTypeData
    {
        private readonly MixedType[] _values;

        public MixedTypeData()
        {
            #region instantiation of a few MixedType objects
            _values = new MixedType[]
            {
                new MixedType()
                {
                    ID = "MixedType_Other",
                    BooleanProp = true,
                    ByteProp = 123,
                    SByteProp = 123,
                    Int16Prop = 123,
                    UInt16Prop = 123,
                    Int32Prop = 123,
                    UInt32Prop = 123,
                    Int64Prop = 123,
                    UInt64Prop = 123,
                    CharProp = (char)123,
                    DoubleProp = 123.123,
                    SingleProp = 123,
                    StringProp = "other string",
                    DecimalProp = 123,
                    DateTimeProp = new DateTime(2008, 09, 03),
                    TimeSpanProp = new TimeSpan(123),
                    DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                    StringsProp = new string[] { "hello", "world" },
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.Now, DateTimeOffset.Now },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                    BinaryProp = new Binary(new byte[]{byte.MaxValue, byte.MinValue, 123}),
                    ByteArrayProp = new byte[]{byte.MaxValue, byte.MinValue, 123},
                    XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                    EnumProp = TestEnum.Value2,

                    NullableBooleanProp = true,
                    NullableByteProp = 123,
                    NullableSByteProp = 123,
                    NullableInt16Prop = 123,
                    NullableUInt16Prop = 123,
                    NullableInt32Prop = 123,
                    NullableUInt32Prop = 123,
                    NullableInt64Prop = 123,
                    NullableUInt64Prop = 123,
                    NullableCharProp = (char)123,
                    NullableDoubleProp = 123.123,
                    NullableSingleProp = 123,
                    NullableDecimalProp = 123,
                    NullableDateTimeProp = new DateTime(2008, 09, 03),
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?>(){new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), DateTimeOffset.Now, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableTimeSpanProp = new TimeSpan(123),
                    NullableGuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                    NullableEnumProp = TestEnum.Value2,

                    DictionaryStringProp = CreateDictionary("some string"),
                    DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                    DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>")),
                    DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)))
                },
                new MixedType()
                {
                    ID = "MixedType_Max",
                    BooleanProp = true,
                    ByteProp = byte.MaxValue,
                    SByteProp = sbyte.MaxValue,
                    Int16Prop = Int16.MaxValue,
                    UInt16Prop = UInt16.MaxValue,
                    Int32Prop = Int32.MaxValue,
                    UInt32Prop = UInt32.MaxValue,
                    Int64Prop = Int64.MaxValue,
                    UInt64Prop = UInt64.MaxValue,
                    CharProp = (char)0xFFFD, //char.MaxValue,
                    DoubleProp = double.MaxValue,
                    SingleProp = Single.MaxValue,
                    StringProp = "some string",
                    DecimalProp = decimal.MaxValue,
                    DateTimeProp = DateTime.MaxValue,
                    TimeSpanProp = TimeSpan.MaxValue,
                    DateTimeOffsetProp = DateTimeOffset.MaxValue,
                    StringsProp = new string[] { "hello", "world" },
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.MaxValue, DateTimeOffset.MaxValue },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    BinaryProp = new Binary(new byte[]{byte.MaxValue}),
                    ByteArrayProp = new byte[]{byte.MaxValue},
                    XElementProp = XElement.Parse("<someElement>max value</someElement>"),
                    EnumProp = TestEnum.Value3,

                    NullableBooleanProp = true,
                    NullableByteProp = byte.MaxValue,
                    NullableSByteProp = sbyte.MaxValue,
                    NullableInt16Prop = Int16.MaxValue,
                    NullableUInt16Prop = UInt16.MaxValue,
                    NullableInt32Prop = Int32.MaxValue,
                    NullableUInt32Prop = UInt32.MaxValue,
                    NullableInt64Prop = Int64.MaxValue,
                    NullableUInt64Prop = UInt64.MaxValue,
                    NullableCharProp = (char)0xFFFD, //char.MaxValue,
                    NullableDoubleProp = double.MaxValue,
                    NullableSingleProp = Single.MaxValue,
                    NullableDecimalProp = decimal.MaxValue,
                    NullableDateTimeProp = DateTime.MaxValue,
                    NullableTimeSpanProp = TimeSpan.MaxValue,
                    NullableDateTimeOffsetProp = DateTimeOffset.MaxValue,
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { DateTimeOffset.MaxValue, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableGuidProp = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    NullableEnumProp = TestEnum.Value3,
                    
                    DictionaryDateTimeProp = CreateDictionary(DateTime.MaxValue),
                    DictionaryDateTimeOffsetProp = CreateDictionary(DateTimeOffset.MaxValue),
                    DictionaryGuidProp = CreateDictionary(new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff")),
                    DictionaryStringProp = CreateDictionary("max string"), 
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>max string</someElement>")),
                },
                new MixedType()
                {
                    ID = "MixedType_Min",
                    BooleanProp = false,
                    ByteProp = byte.MinValue,
                    SByteProp = sbyte.MinValue,
                    Int16Prop = Int16.MinValue,
                    UInt16Prop = UInt16.MinValue,
                    Int32Prop = Int32.MinValue,
                    UInt32Prop = UInt32.MinValue,
                    Int64Prop = Int64.MinValue,
                    UInt64Prop = UInt64.MinValue,
                    CharProp = (char)1, //char.MinValue,
                    DoubleProp = double.MinValue,
                    SingleProp = Single.MinValue,
                    StringProp = "some other string",
                    DecimalProp = decimal.MinValue,
                    DateTimeProp = DateTime.MinValue,
                    TimeSpanProp = TimeSpan.MinValue,
                    DateTimeOffsetProp = DateTimeOffset.MinValue,
                    StringsProp = Array.Empty<string>(),
                    IntsProp = new int[] { 4, 2 },
                    EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1 },
                    DateTimesCollectionProp = new List<DateTime>() { DateTime.Now, DateTime.Now },
                    DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { DateTimeOffset.MinValue, DateTimeOffset.MinValue },
                    TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                    UriProp = new Uri("http://localhost"),
                    GuidProp = new Guid("00000000-0000-0000-0000-000000000000"),
                    BinaryProp = new Binary(new byte[]{byte.MinValue}),
                    ByteArrayProp = new byte[]{byte.MinValue},
                    XElementProp = XElement.Parse("<someElement>min value</someElement>"),
                    EnumProp = TestEnum.Value0,

                    NullableBooleanProp = false,
                    NullableByteProp = byte.MinValue,
                    NullableSByteProp = sbyte.MinValue,
                    NullableInt16Prop = Int16.MinValue,
                    NullableUInt16Prop = UInt16.MinValue,
                    NullableInt32Prop = Int32.MinValue,
                    NullableUInt32Prop = UInt32.MinValue,
                    NullableInt64Prop = Int64.MinValue,
                    NullableUInt64Prop = UInt64.MinValue,
                    NullableCharProp = (char)1, //char.MinValue,
                    NullableDoubleProp = double.MinValue,
                    NullableSingleProp = Single.MinValue,
                    NullableDecimalProp = decimal.MinValue,
                    NullableDateTimeProp = DateTime.MinValue,
                    NullableTimeSpanProp = TimeSpan.MinValue,
                    NullableDateTimeOffsetProp = DateTimeOffset.MinValue,
                    NullableDateTimesCollectionProp = new List<DateTime?>() { DateTime.Now, null },
                    NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { DateTimeOffset.MinValue, null },
                    NullableEnumsArrayProp = new DateTime?[] { DateTime.Now, null },
                    NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), null },
                    NullableGuidProp = new Guid("00000000-0000-0000-0000-000000000000"),
                    NullableEnumProp = TestEnum.Value0,

                    DictionaryDateTimeProp = CreateDictionary(DateTime.MinValue),
                    DictionaryDateTimeOffsetProp = CreateDictionary(DateTimeOffset.MinValue),
                    DictionaryGuidProp = CreateDictionary(new Guid("00000000-0000-0000-0000-000000000000")),
                    DictionaryStringProp = CreateDictionary("min string"), 
                    DictionaryTestEnumProp = CreateDictionary(TestEnum.Value1),
                    DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>min string</someElement>")),
                }
            };
            #endregion
        }

        public MixedTypeData(bool useSuperset)
            : this()
        {
            if (useSuperset)
            {
                #region instantiation of a superset of MixedType objects
                _values = new MixedType[] { _values[0], _values[1], _values[2],
                    new MixedType()
                    {
                        ID = "MixedType_Negative",
                        BooleanProp = false,
                        ByteProp = 123,
                        SByteProp = -123,
                        Int16Prop = -123,
                        UInt16Prop = 123,
                        Int32Prop = -123,
                        UInt32Prop = 123,
                        Int64Prop = -123,
                        UInt64Prop = 123,
                        CharProp = (char)123,
                        DoubleProp = -123.123,
                        SingleProp = -(Single)123.123,
                        StringProp = "some other string value",
                        DecimalProp = -(Decimal)123.123,
                        DateTimeProp = new DateTime(2008, 09, 03),
                        DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                        TimeSpanProp = new TimeSpan(123),
                        StringsProp = new string[] { "some string", "some other string", "some other string value" },
                        IntsProp = new int[] { -123, 123 },
                        EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1, TestEnum.Value2 },
                        DateTimesCollectionProp = new List<DateTime>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10) },
                        DateTimeOffsetsCollectionProp = new List<DateTimeOffset> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                        UriProp = new Uri("http://localhost"),
                        GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                        BinaryProp = new Binary(new byte[] { byte.MaxValue, byte.MinValue, 123 }),
                        ByteArrayProp = new byte[] { byte.MaxValue, byte.MinValue, 123 },
                        XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                        EnumProp = TestEnum.Value2,

                        NullableBooleanProp = false,
                        NullableByteProp = null,
                        NullableSByteProp = -123,
                        NullableInt16Prop = -123,
                        NullableUInt16Prop = 123,
                        NullableInt32Prop = -123,
                        NullableUInt32Prop = 123,
                        NullableInt64Prop = -123,
                        NullableUInt64Prop = 123,
                        NullableCharProp = (char)123,
                        NullableDoubleProp = -123.123,
                        NullableSingleProp = -(Single)123.123,
                        NullableDecimalProp = -(Decimal)123.123,
                        NullableDateTimeProp = null,
                        NullableDateTimeOffsetProp = null,
                        NullableDateTimesCollectionProp = new List<DateTime?>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        NullableEnumsArrayProp = new DateTime?[] { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), new TimeSpan(456), null },
                        NullableTimeSpanProp = null,
                        NullableGuidProp = null,
                        NullableEnumProp = null,

                        DictionaryStringProp = CreateDictionary("some string"),
                        DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                        DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0))),
                        DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                        DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                        DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>"))
                    },
                    new MixedType()
                    {
                        ID = "MixedType_Null",
                        BooleanProp = true,
                        ByteProp = 123,
                        SByteProp = -123,
                        Int16Prop = -123,
                        UInt16Prop = 123,
                        Int32Prop = -123,
                        UInt32Prop = 123,
                        Int64Prop = -123,
                        UInt64Prop = 123,
                        CharProp = (char)123,
                        DoubleProp = -123.123,
                        SingleProp = -(Single)123.123,
                        StringProp = "some other string value",
                        DecimalProp = -(Decimal)123.123,
                        DateTimeProp = new DateTime(2008, 09, 03),
                        DateTimeOffsetProp = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)),
                        TimeSpanProp = new TimeSpan(123),
                        StringsProp = new string[] { "some string", "some other string", "some other string value" },
                        IntsProp = new int[] { -123, 123 },
                        EnumsProp = new TestEnum[] { TestEnum.Value0, TestEnum.Value1, TestEnum.Value2 },
                        DateTimesCollectionProp = new List<DateTime>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10) },
                        DateTimeOffsetsCollectionProp = new List<DateTimeOffset>() { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        TimeSpanListProp = new List<TimeSpan>() { new TimeSpan(123), new TimeSpan(456) },
                        UriProp = new Uri("http://localhost"),
                        GuidProp = new Guid("12345678-1234-1234-1234-123456789012"),
                        BinaryProp = new Binary(new byte[] { byte.MaxValue, byte.MinValue, 123 }),
                        ByteArrayProp = new byte[] { byte.MaxValue, byte.MinValue, 123 },
                        XElementProp = XElement.Parse("<someElement>element text</someElement>"),
                        EnumProp = TestEnum.Value2,

                        NullableBooleanProp = null,
                        NullableByteProp = null,
                        NullableSByteProp = null,
                        NullableInt16Prop = null,
                        NullableUInt16Prop = null,
                        NullableInt32Prop = null,
                        NullableUInt32Prop = null,
                        NullableInt64Prop = null,
                        NullableUInt64Prop = null,
                        NullableCharProp = null,
                        NullableDoubleProp = null,
                        NullableSingleProp = null,
                        NullableDecimalProp = null,
                        NullableDateTimeProp = null,
                        NullableDateTimeOffsetProp = null,
                        NullableDateTimesCollectionProp = new List<DateTime?>() { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableDateTimeOffsetCollectionProp = new List<DateTimeOffset?> { new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)), new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0)) },
                        NullableEnumsArrayProp = new DateTime?[] { new DateTime(2008, 09, 03), new DateTime(2009, 12, 10), null },
                        NullableTimeSpanListProp = new List<TimeSpan?>() { new TimeSpan(123), new TimeSpan(456), null },
                        NullableTimeSpanProp = null,
                        NullableGuidProp = null,
                        NullableEnumProp = null,

                        DictionaryStringProp = CreateDictionary("some string"),
                        DictionaryDateTimeProp = CreateDictionary(new DateTime(2008, 09, 03)),
                        DictionaryDateTimeOffsetProp = CreateDictionary(new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0))),
                        DictionaryGuidProp = CreateDictionary(new Guid("12345678-1234-1234-1234-123456789012")),
                        DictionaryTestEnumProp = CreateDictionary(TestEnum.Value2),
                        DictionaryXElementProp = CreateDictionary(XElement.Parse("<someElement>element text</someElement>"))
                    }
                };
                #endregion
            }
        }

        public MixedType[] Values
        {
            get { return _values; }
        }

        private static Dictionary<TType, TType> CreateDictionary<TType>(TType seed)
        {
            return CreateDictionary(seed, seed);
        }

        private static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(TKey seedKey, TValue seedValue)
        {
            var d = new Dictionary<TKey, TValue>();
            d.Add(seedKey, seedValue);
            return d;
        }
    }
    #endregion

    #region Test DomainService to test unsupported properties (like Indexer)
    [EnableClientAccess]
    public class DomainServiceWithIndexerEntity : DomainService
    {
        public IEnumerable<EntityWithIndexer> GetEntityWithIndexer()
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Mock DomainServices with and without CUD operations
    [EnableClientAccess]
    public class DomainServiceWithoutCUD : DomainService
    {
        public IEnumerable<MockCustomer> GetDummyValue()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class DomainServiceWithUpdate : DomainService
    {
        public IEnumerable<MockCustomer> GetDummyValue()
        {
            throw new NotImplementedException();
        }

        public void UpdateCustomer(MockCustomer cust)
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class DomainServiceWithCreate : DomainService
    {
        public IEnumerable<MockCustomer> GetDummyValue()
        {
            throw new NotImplementedException();
        }

        public void CreateCustomer(MockCustomer cust)
        {
            throw new NotImplementedException();
        }
    }
    
    [EnableClientAccess]
    public class DomainServiceWithDelete : DomainService
    {
        public IEnumerable<MockCustomer> GetDummyValue()
        {
            throw new NotImplementedException();
        }

        public void DeleteCustomer(MockCustomer cust)
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class DomainServiceWithNamedUpdate : DomainService
    {
        public IEnumerable<MockCustomer> GetDummyValue()
        {
            throw new NotImplementedException();
        }

        [EntityAction]
        public void NamedUpdateCustomer(MockCustomer cust, string dummyString)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Test Entities and Providers used to verify Cross DomainContext functionality

    public partial class MockCustomer
    {
        [Key]
        public int CustomerId { get; set; }
        public string CityName { get; set; }
        [RoundtripOriginal]
        public string StateName { get; set; }

        [ExternalReference]
        [Association("Customer_City", "CityName,StateName", "Name,StateName", IsForeignKey = true)]
        public Cities.City City { get; set; }

        [ExternalReference]
        [Association("Customer_PreviousResidences", "StateName", "StateName")]
        public List<Cities.City> PreviousResidences { get; set; }
    }

    [DataContract(Name = "MR", Namespace = "Mock.Models")]
    [RoundtripOriginal]
    public partial class MockReport
    {
        [DataMember(Name = "CId")]
        public int CustomerId { get; set; }

        // project from customer
        [Include("StateName", "State")]
        [Association("R_C", "CustomerId", "CustomerId")]
        public MockCustomer Customer { get; set; }

        [Key]
        [DataMember(Name = "REFId", Order = 1)]
        public int ReportElementFieldId { get; set; }

        [DataMember(Name = "Title", IsRequired = true)]
        public string ReportTitle { get; set; }

        [DataMember(Name = "Data")]
        [Include("TimeEntered", "Start")]
        public MockReportBody ReportBody { get; set; }
    }

    [DataContract(Name = "MRB", Namespace = "Mock.Models")]
    [RoundtripOriginal]
    public partial class MockReportBody
    {
        [DataMember(Name = "EntryDate", Order = 1)]
        public DateTime TimeEntered { get; set; }

        [DataMember(Name = "Body", Order = 2, EmitDefaultValue = false)]
        public string Report { get; set; }
    }

    [EnableClientAccess]
    [DomainServiceDescriptionProvider(typeof(MockDomainServiceDescriptionProvider))]
    public class MockCustomerDomainService : DomainService
    {
        readonly List<MockCustomer> customers;
        readonly List<MockReport> reports = new List<MockReport>();

        public MockCustomerDomainService()
        {
            // populate mock data
            MockCustomer c1 = new MockCustomer() { CustomerId = 1, CityName = "Redmond", StateName = "WA" };
            MockCustomer c2 = new MockCustomer() { CustomerId = 2, CityName = "Orange", StateName = "CA" };
            MockCustomer c3 = new MockCustomer() { CustomerId = 3, CityName = "Bellevue", StateName = "WA" };
            customers = new List<MockCustomer>(new MockCustomer[] {c1, c2, c3});

            MockReportBody body = new MockReportBody() { TimeEntered = new DateTime(1970, 3, 15), Report = "Old report" };
            reports.Add(new MockReport() { ReportElementFieldId = 1, Customer = c1, CustomerId = c1.CustomerId, ReportTitle = "Book Favorites", ReportBody = body });
            reports.Add(new MockReport() { ReportElementFieldId = 2, Customer = c2, CustomerId = c2.CustomerId, ReportTitle = "Electronic Purchases" });
            reports.Add(new MockReport() { ReportElementFieldId = 3, Customer = c3, CustomerId = c3.CustomerId, ReportTitle = "Shoe trends", ReportBody = body });
        }

        [Query]
        public IQueryable<MockCustomer> GetCustomers()
        {
            return this.customers.AsQueryable<MockCustomer>();
        }

        [Query]
        public IQueryable<MockReport> GetReports()
        {
            return this.reports.AsQueryable<MockReport>();
        }

        [Update]
        public void MaskedUpdateCustomer(MockCustomer current)
        {
            MockCustomer serverModifiedCustomer = new MockCustomer();

            if (current.StateName.Length != 2)
            {
                throw new ValidationException("Expected state name of length 2");
            }

            // copy properties
            serverModifiedCustomer.City = current.City;
            serverModifiedCustomer.CityName = current.CityName;
            serverModifiedCustomer.CustomerId = current.CustomerId;
            serverModifiedCustomer.PreviousResidences = current.PreviousResidences;
            serverModifiedCustomer.StateName = "AA"; //current.StateName;

            this.ChangeSet.Replace(current, serverModifiedCustomer);
        }

        [EntityAction]
        public void MockCustomerCustomMethod(MockCustomer current, string expectedStateName, string expectedOriginalStateName)
        {
            // Get Original
            MockCustomer original = this.ChangeSet.GetOriginal(current);

            if (original == null)
            {
                throw new DomainException("Expected to find original entity.");
            }
            else if (!original.StateName.Equals(expectedOriginalStateName))
            {
                throw new ValidationException(
                    string.Format("Expected original state name: '{0}'.  Actual: '{1}'.", expectedOriginalStateName, original.StateName));
            }
            else if (!current.StateName.Equals(expectedStateName))
            {
                throw new ValidationException(
                    string.Format("Expected state name: '{0}'.  Actual: '{1}'.", expectedStateName, current.StateName));
            }

            current.StateName = "BB";
        }

        [EntityAction]
        public void MockReportCustomMethod(MockReport current)
        {
            MockReport original = this.ChangeSet.GetOriginal(current);

            if (original == null)
            {
                throw new DomainException("Expected to find original entity.");
            }

            MockReport localCopy = reports.Where(r => r.ReportElementFieldId == original.ReportElementFieldId).FirstOrDefault();
            List<string> errors = new List<string>();
            string format = "Expected original entity to match local copy. {0} (local = {1}, roundtripped = {2})";
            Action<bool, string, object, object> appendIf = (emit, property, local, roundtripped) =>
            {
                if (emit)
                {
                    errors.Add(string.Format(format, property, local, roundtripped));
                }
            };

            // verify original was roundtripped correctly
            appendIf(localCopy == null, "ReportElementFieldId", "Not found", original.ReportElementFieldId);
            if (localCopy != null)
            {
                appendIf(localCopy.CustomerId != original.CustomerId, "CustomerId", localCopy.CustomerId, original.CustomerId);
                appendIf(localCopy.ReportTitle != original.ReportTitle, "ReportTitle", localCopy.ReportTitle, original.ReportTitle);
                appendIf(localCopy.ReportBody.TimeEntered != original.ReportBody.TimeEntered, "ReportBody.TimeEntered", localCopy.ReportBody.TimeEntered, original.ReportBody.TimeEntered);
                appendIf(localCopy.ReportBody.Report != original.ReportBody.Report, "ReportBody.Report", localCopy.ReportBody.Report, original.ReportBody.Report);
            }

            format = "Expected changes in updated entity. {0} (local = {1}, roundtripped = {2})";

            // verify client changes were reflected properly
            appendIf(current.ReportTitle == null, "ReportTitle", localCopy.ReportTitle, original.ReportTitle);
            appendIf(current.ReportBody.Report == null, "ReportBody.Report", localCopy.ReportBody.Report, original.ReportBody.Report);
            appendIf(current.ReportBody.TimeEntered <= localCopy.ReportBody.TimeEntered , "ReportBody.TimeEntered", localCopy.ReportBody.TimeEntered, original.ReportBody.TimeEntered);

            if (errors.Count() > 0)
            {
                throw new InvalidOperationException(errors.Aggregate((s1, s2) => s1 + "\n" + s2));
            }
        }

        /// <summary>
        /// Emits data members on projection properties only.
        /// </summary>
        public class MockDomainServiceDescriptionProvider : DomainServiceDescriptionProvider
        {
            public MockDomainServiceDescriptionProvider(Type domainServiceType, DomainServiceDescriptionProvider parent)
                : base(domainServiceType, parent)
            {
            }

            public override ICustomTypeDescriptor GetTypeDescriptor(Type type, ICustomTypeDescriptor parent)
            {
                ICustomTypeDescriptor parentDescriptor = base.GetTypeDescriptor(type, parent);

                if (type == typeof(MockReport))
                {
                    return new MockReportTypeDescriptor(parentDescriptor);
                }

                return parentDescriptor;
            }

            private class MockReportTypeDescriptor : CustomTypeDescriptor
            {
                public MockReportTypeDescriptor(ICustomTypeDescriptor parent)
                    : base(parent)
                {
                }

                public override PropertyDescriptorCollection GetProperties()
                {
                    PropertyDescriptorCollection parentProperties = base.GetProperties();
                    List<PropertyDescriptor> properties = new List<PropertyDescriptor>(parentProperties.Count);
                    MockReportPropertyDescriptor state = null;
                    MockReportPropertyDescriptor start = null;

                    foreach (PropertyDescriptor prop in parentProperties)
                    {
                        properties.Add(prop);

                        if (prop.Name == "Customer")
                        {
                            PropertyDescriptor target = prop.GetChildProperties().Find("StateName", false);
                            state = new MockReportPropertyDescriptor("State", prop, target, "SN");
                        }

                        if (prop.Name == "ReportBody")
                        {
                            PropertyDescriptor target = prop.GetChildProperties().Find("TimeEntered", false);
                            start = new MockReportPropertyDescriptor("Start", prop, target, "Str");
                        }
                    }

                    properties.Add(state);
                    properties.Add(start);
                    return new PropertyDescriptorCollection(properties.ToArray(), true);
                }

                private class MockReportPropertyDescriptor : PropertyDescriptor
                {
                    readonly PropertyDescriptor _source;
                    readonly PropertyDescriptor _target;

                    public MockReportPropertyDescriptor(string propertyName, PropertyDescriptor source, PropertyDescriptor target, string dataMemberName)
                        : base(propertyName, MockReportPropertyDescriptor.GetAttributes(target.Attributes, dataMemberName))
                    {
                        this._source = source;
                        this._target = target;
                    }
                    
                    public override Type ComponentType
                    {
                        get { return this._source.ComponentType; }
                    }

                    public override bool IsReadOnly
                    {
                        get { return true; }
                    }

                    public override Type PropertyType
                    {
                        get { return this._target.PropertyType; }
                    }

                    public override bool CanResetValue(object component)
                    {
                        return false;
                    }

                    public override object GetValue(object component)
                    {
                        object value = this._source.GetValue(component);

                        if (value != null)
                        {
                            PropertyDescriptor pd = TypeDescriptor.GetProperties(value)[this._target.Name];
                            value = pd.GetValue(value);
                        }

                        return value;
                    }

                    public override void ResetValue(object component)
                    {
                        throw new NotImplementedException();
                    }

                    public override void SetValue(object component, object value)
                    {
                        throw new NotImplementedException();
                    }

                    public override bool ShouldSerializeValue(object component)
                    {
                        return true;
                    }

                    private static Attribute[] GetAttributes(AttributeCollection parentAttr, string dataMemberName)
                    {
                        IEnumerable<Attribute> newAttrs = parentAttr.Cast<Attribute>().Where(a => a.GetType() != typeof(DataMemberAttribute));
                        Attribute[] dataMemberAttr = new Attribute[]
                        {
                            new DataMemberAttribute() { Name = dataMemberName },
                            new EditableAttribute(false)
                        };
                        return newAttrs.Concat(dataMemberAttr).ToArray();
                    }
                }
            }
        }
    }

    #endregion

    #region Domain Services used to verify nesting/sharing behavior

    [EnableClientAccess]
    public class MockCustomerDomainService_SharedEntityTypes : DomainService
    {
        [Query]
        public IQueryable<MockCustomer> GetCustomers()
        {
            throw new NotImplementedException();
        }

        [EnableClientAccess]
        public class MockCustomerDomainService_Nested : DomainService
        {
            [Query]
            public IQueryable<MockCustomer> GetCustomers()
            {
                throw new NotImplementedException();
            }
        }
    }

    #endregion // Domain Services used to verify nesting/sharing behavior

    #region Test RequiresSecureEndpoint

    public class TestEntity_RequiresSecureEndpoint
    {
        [Key]
        public int Key { get; set; }
    }

    [EnableClientAccess(RequiresSecureEndpoint = true)]
    public class TestService_RequiresSecureEndpoint : DomainService
    {
        [Query]
        public IQueryable<TestEntity_RequiresSecureEndpoint> GetTestEntities()
        {
            return null;
        }
    }

    #endregion

    #region test provider and entity for Bug 626901
    [EnableClientAccess]
    public class IncorrectAssicationProvider_Bug626901 : DomainService
    {
        public IQueryable<A_Bug626901> GetA_Bug626901()
        {
            return null;
        }

        public IQueryable<B_Bug626901> GetB_Bug626901()
        {
            return null;
        }
    }

    public class A_Bug626901
    {

        [Key]
        public int ID { get; set; }  // B_ID instead of ID it would work

        [Association("A_Bug626901_B_Bug626901", "B_ID", "ID", IsForeignKey = true)]
        public B_Bug626901 B { get; set; }

    }

    public class B_Bug626901
    {

        [Key]
        public int ID { get; set; }

        [Association("A_Bug626901_B_Bug626901", "ID", "B_ID")]  // This has B_ID which does not exist in A
        public A_Bug626901 A { get; set; }

    }
    #endregion

    #region test provider and entity for Bug 629280
    [EnableClientAccess]
    public class Provider_RangeAttributeWithType_Bug629280 : DomainService
    {
        public IQueryable<A_Bug629280> GetA_Bug629280()
        {
            return null;
        }
    }

    public class A_Bug629280
    {
        [Key]
        public int ID { get; set; }

        [Range(typeof(DateTime), "1/1/1980", "1/1/2001")]
        public DateTime RangeWithDateTime { get; set; }

        [Range(1.1d, 1.1d)]
        public double RangeWithDouble { get; set; }

        [Range(typeof(double), "1.1", "1.1")]
        public double RangeWithDoubleAsString { get; set; }

        [Range(1, 1)]
        public int RangeWithInteger { get; set; }

        [Range(typeof(int), "1", "1")]
        public int RangeWithIntegerAsString { get; set; }

        [Range(typeof(int), null, null)]
        public int RangeWithNullStrings { get; set; }

        [Range(typeof(int), null, "1")]
        public int RangeWithNullString1 { get; set; }

        [Range(typeof(int), "1", null)]
        public int RangeWithNullString2 { get; set; }

        [Range(1, 10, ErrorMessage = "Range must be between 1 and 10")]
        public int RangeWithErrorMessage { get; set; }

        [Range(1, 10, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "String")]
        public int RangeWithResourceMessage { get; set; }
    }

    public static class SharedResource
    {
        public static string String { get { return "SharedResource.String"; } }
    }

    #endregion

    #region Domain Services with Interface Attributes

    [EnableClientAccess]
    [MockAttributeAllowOnce("Class")]
    [MockAttributeAllowMultiple("Class")]
    [MockAttributeAllowMultiple("Class")]
    [MockAttributeAllowMultiple("Class")]
    public class InterfaceInheritanceDomainService : DomainService, IProvider_InterfaceAttributes
    {
        public IEnumerable<EntityWithXElement> EntityWithXElement_Get()
        {
            return null;
        }
        public void EntityWithXElement_Update(EntityWithXElement entity) { }
        public void EntityWithXElement_Delete(EntityWithXElement entity) { }
        public void EntityWithXElement_Insert(EntityWithXElement entity) { }
        public bool EntityWithXElement_Resolve(EntityWithXElement curr, EntityWithXElement original, EntityWithXElement store, bool isDeleted)
        {
            return false;
        }

        [EntityAction]
        [MockAttributeAllowOnce("Class")]
        public void EntityWithXElement_Custom_AttributeOverrides(EntityWithXElement entity) { }

        [EntityAction]
        [MockAttributeAllowMultiple("Class")]
        [MockAttributeAllowMultiple("Class")]
        [MockAttributeAllowMultiple("Class")]
        public void EntityWithXElement_Custom_AttributeAggregation(EntityWithXElement entity) { }
    }

    [MockAttributeAllowOnce("Interface")]
    [MockAttributeAllowOnce_InterfaceOnly("Interface")]
    [MockAttributeAllowOnce_AppliedToInterfaceOnly("Interface")]
    [MockAttributeAllowMultiple("Interface")]
    [MockAttributeAllowMultiple("Interface")]
    [MockAttributeAllowMultiple("Interface")]
    [MockAttributeAllowMultiple_InterfaceOnly("Interface")]
    [MockAttributeAllowMultiple_InterfaceOnly("Interface")]
    [MockAttributeAllowMultiple_InterfaceOnly("Interface")]
    public interface IProvider_InterfaceAttributes
    {
        [Query]
        IEnumerable<EntityWithXElement> EntityWithXElement_Get();

        [Update]
        void EntityWithXElement_Update(EntityWithXElement entity);

        [Delete]
        void EntityWithXElement_Delete(EntityWithXElement entity);

        [Insert]
        void EntityWithXElement_Insert(EntityWithXElement entity);

        [EntityAction]
        [MockAttributeAllowOnce("Interface")]
        [MockAttributeAllowOnce_AppliedToInterfaceOnly("Interface")]
        void EntityWithXElement_Custom_AttributeOverrides(EntityWithXElement entity);

        [EntityAction]
        [MockAttributeAllowMultiple("Interface")]
        [MockAttributeAllowMultiple("Interface")]
        [MockAttributeAllowMultiple("Interface")]
        void EntityWithXElement_Custom_AttributeAggregation(EntityWithXElement entity);
    }

    #endregion // Domain Services with Interface Attributes

    #region Named Update Methods

    namespace NamedUpdates
    {
        [EnableClientAccess]
        public class NamedUpdate_CustomOnly : DomainService
        {
            [Query]
            public IQueryable<MockEntity1> GetEntities()
            {
                return new[] { new MockEntity1() }.AsQueryable();
            }

            [EntityAction]
            public void NamedUpdateMethod(MockEntity1 entity, string newProperty1)
            {
                var original = this.ChangeSet.GetOriginal<MockEntity1>(entity);
                entity.Property1 = newProperty1;
                entity.Property3 = original.Property3;
            }
        }

        [EnableClientAccess]
        public class NamedUpdate_CustomAndUpdate : DomainService
        {
            [Query]
            public IQueryable<MockEntity2> GetEntities()
            {
                return new[] { new MockEntity2() }.AsQueryable();
            }

            [EntityAction]
            public void NamedUpdateMethod(MockEntity2 entity, string newProperty1)
            {
                var original = this.ChangeSet.GetOriginal<MockEntity2>(entity);
                entity.Property1 = newProperty1;
                entity.Property3 = original.Property3;
            }

            [Update]
            public void MaskedUpdateMethod(MockEntity2 entity)
            {
                entity.Property1 = "UpdatedValue1";
                entity.Property2 = "UpdatedValue2";
                entity.Property3 = "UpdatedValue3";
            }
        }

        [EnableClientAccess]
        public class NamedUpdate_CustomValidation : DomainService
        {
            public NamedUpdate_CustomValidation()
            {
                DynamicTestValidator.Reset();

                // MockEntity3, MockComplexObject1 fails param validation
                AddValidationResult(typeof(MockEntity3));
                AddValidationResult(typeof(MockComplexObject1[]));
                AddValidationResult(typeof(MockComplexObject1));

                // MockEntity4, MockComplexObject2 fails type validation
                AddValidationResult(typeof(MockEntity4));
                AddValidationResult(typeof(MockComplexObject2));

                string errorMessage = "Property validation failed.";
                string[] memberNames = new string[] { "ValidatedProperty" };
                ValidationResult result = new ValidationResult(errorMessage, memberNames);

                // Property "Invalid" is always invalid.
                string serverInvalidProperty = "Invalid";
                DynamicTestValidator.ForcedValidationResults.Add(serverInvalidProperty, result);

                // MockComplexObject4 fails type validation
                AddValidationResult(typeof(MockComplexObject4));
            }

            private static void AddValidationResult(Type type)
            {
                ValidationResult result = NamedUpdate_CustomValidation.CreateValidationResult(type);
                DynamicTestValidator.ForcedValidationResults.Add(type, result);
            }

            private static ValidationResult CreateValidationResult(Type type)
            {
                string errorMessage = string.Format("Validation failed. {0}", type.Name);
                string[] memberNames = new string[] { "PlaceholderName" };
                return new ValidationResult(errorMessage, memberNames);
            }

            [Query]
            public IQueryable<MockEntity3> GetEntities3()
            {
                return new[] { new MockEntity3() }.AsQueryable();
            }

            [Query]
            public IQueryable<MockEntity4> GetEntities4()
            {
                return new[] { new MockEntity4() }.AsQueryable();
            }

            [Query]
            public IQueryable<MockEntity5> GetEntities5()
            {
                return new[]
                {
                    new MockEntity5()
                    {
                        CommonProperty = new MockComplexObject4()
                        {
                            Property1 = new MockComplexObject4(),
                        },
                        CommonArray = new MockComplexObject4[]
                        {
                            new MockComplexObject4()
                            {
                                Property1 = new MockComplexObject4(),
                            }
                        },
                    }
                }.AsQueryable();
            }

            [Query]
            public IQueryable<MockEntity6> GetEntities6()
            {
                return new[] { new MockEntity6() }.AsQueryable();
            }

            [EntityAction]
            public void NamedUpdateWithParamValidation(
                [CustomValidation(typeof(DynamicTestValidator), "Validate")] MockEntity3 entity,
                [CustomValidation(typeof(DynamicTestValidator), "Validate")] MockComplexObject1[] array,
                [CustomValidation(typeof(DynamicTestValidator), "Validate")] MockComplexObject1 complexObject)
            {
            }

            [EntityAction]
            public void NamedUpdateWithTypeValidation(
                MockEntity4 entity,
                MockComplexObject2[] array,
                MockComplexObject2 complexObject)
            {
            }

            [EntityAction]
            public void NamedUpdateWithPropValidation(
                MockEntity3 entity,
                MockComplexObject1[] array,
                MockComplexObject1 complexObject)
            {
            }

            [EntityAction]
            public void NamedUpdateWithCommonProperties(
                MockEntity5 entity,
                MockComplexObject3 complexObject)
            {
            }

            [EntityAction]
            public void NamedUpdateWithNoEntityValidation(
                MockEntity6 entity,
                MockComplexObject2 complexObject)
            {
            }
        }

        public class MockEntity1 : MockEntityBase { }
        public class MockEntity2 : MockEntityBase { }
        public class MockEntity3 : MockEntityBase
        {
            [CustomValidation(typeof(DynamicTestValidator), "Validate")]
            public string ValidatedProperty { get; set; }
        }

        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public class MockEntity4 : MockEntityBase
        {
            [CustomValidation(typeof(DynamicTestValidator), "Validate")]
            public string ValidatedProperty { get; set; }
        }

        public class MockEntity5 : MockEntityBase
        {
            public MockComplexObject4 CommonProperty { get; set; }
            public MockComplexObject4[] CommonArray { get; set; }
        }

        public class MockEntity6 : MockEntityBase
        {
        }

        public abstract class MockEntityBase
        {
            public MockEntityBase()
            {
                Key = 123;
                Property1 = "OriginalValue1";
                Property2 = "OriginalValue2";
                Property3 = "OriginalValue3";
            }

            [Key]
            public int Key { get; set; }
            public string Property1 { get; set; }
            public string Property2 { get; set; }
            public string Property3 { get; set; }
        }

        public class MockComplexObject1
        {
            [CustomValidation(typeof(DynamicTestValidator), "Validate")]
            public string ValidatedProperty { get; set; }
            public MockComplexObject1 Property1 { get; set; }
        }

        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public class MockComplexObject2
        {
            [CustomValidation(typeof(DynamicTestValidator), "Validate")]
            public string ValidatedProperty { get; set; }
            public MockComplexObject2 Property1 { get; set; }
        }

        public class MockComplexObject3
        {
            public MockComplexObject4 CommonProperty { get; set; }
            public MockComplexObject4[] CommonArray { get; set; }
        }

        [CustomValidation(typeof(DynamicTestValidator), "Validate")]
        public class MockComplexObject4
        {
            public MockComplexObject4 Property1 { get; set; }
        }

        [EnableClientAccess]
        public class CalculatorDomainService : DomainService
        {
            [Query]
            public IQueryable<CalculatorValue> GetEntities()
            {
                return new[] { new CalculatorValue() { Key = 1 } }.AsQueryable();
            }

            [EntityAction(AllowMultipleInvocations = true)]
            public void Add(CalculatorValue value, decimal rhs)
            {
                value.Value += rhs;
            }

            [EntityAction(AllowMultipleInvocations = true)]
            public void Multiply(CalculatorValue value, decimal rhs)
            {
                value.Value *= rhs;
            }

            [Query]
            public IQueryable<CalculatorValueOldCodeGen> GetEntitiesOldCodeGen()
            {
                return new[] { new CalculatorValueOldCodeGen() { Key = 1 } }.AsQueryable();
            }

#pragma warning disable 618 // Service should work with the "old" approach with [EntityAction]
            [EntityAction]
            public void AddTwice(CalculatorValueOldCodeGen value, decimal rhs)
            {
                value.Value += 2*rhs;
            }
#pragma warning restore 618
        }

        [DataContract]
        public class CalculatorValue
        {
            [DataMember]
            [Key]
            public int Key { get; set; }

            [DataMember]
            [RoundtripOriginal]
            public decimal Value { get; set; }
        }

        [DataContract]
        public class CalculatorValueOldCodeGen
        {
            [DataMember]
            [Key]
            public int Key { get; set; }

            [DataMember]
            [RoundtripOriginal]
            public decimal Value { get; set; }
        }
    }

    #endregion // Named Update Methods
    
    [EnableClientAccess]
    public class MetadataTypeAttributeCycleTestDomainService1 : DomainService
    {
        public IQueryable<EntityWithCyclicMetadataTypeAttributeA> GetEntitiesA()
        {
            return null;
        }

        public IQueryable<EntityWithCyclicMetadataTypeAttributeB> GetEntitiesB()
        {
            return null;
        }

        public IQueryable<EntityWithCyclicMetadataTypeAttributeC> GetEntitiesC()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class MetadataTypeAttributeCycleTestDomainService2 : DomainService
    {
         public IQueryable<EntityWithSelfReferencingcMetadataTypeAttribute> GetEntities()
        {
            return null;
        }
    }

    #region Test provider and entity for Bug 796616

    [EnableClientAccess]
    public class MockDomainService_ExcludedAssociation : DomainService
    {
        public IEnumerable<MockOrder> GetOrders() { return null; }
        public void CreateOrders(MockOrder o) { }
        public void UpdateOrders(MockOrder o) { }
        public void DeleteOrders(MockOrder o) { }
        public IEnumerable<MockOrderDetails> GetOrderDetails() { return null; }
        public void CreateOrderDetails(MockOrderDetails o) { }
        public void UpdateOrderDetails(MockOrderDetails o) { }
        public void DeleteOrderDetails(MockOrderDetails o) { }
    }

    public class MockOrder
    {
        [Key]
        public int OrderID { get; set; }

        [Exclude]
        [Association("Order_OrderDetails", "OrderID", "OrderID", IsForeignKey = false)]
        public List<MockOrderDetails> MockOrderDetails { get; set; }
    }

    public class MockOrderDetails
    {
        [Key]
        public int Key { get; set; }
        public int OrderID { get; set; }

        [Association("Order_OrderDetails", "OrderID", "OrderID", IsForeignKey = true)]
        public MockOrder MockOrder { get; set; }
    }

    #endregion //Test provider and entity for Bug 796616

    #region Excluded Properties Validation Scenarios
    [CustomValidation(typeof(CustomExcludeValidator), "Validate")]
    public partial class ExcludeValidationEntity
    {
        [Key]
        public int K { get; set; }

        [Range(1, 10)]
        public double P1to10 { get; set; }

        [Exclude]
        [Range(1, 10)]
        public double P1to10Excluded { get; set; }

        [Exclude]
        [Range(1, 20)]
        public double P1to20Excluded { get; set; }
    }

    public static class CustomExcludeValidator
    {
        public static ValidationResult Validate(ExcludeValidationEntity entity, ValidationContext validationContext)
        {
            if (entity.P1to20Excluded > 10)
            {
                return new ValidationResult("error", new string[] { "P1to10Excluded", "P1to20Excluded" });
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }

    [EnableClientAccess]
    public class ExcludeValidationEntityDomainService : DomainService
    {        
        public IEnumerable<ExcludeValidationEntity> GetExcludeValidationEntity()
        {
            return null;
        }

        public void InsertExcludeValidationEntity(ExcludeValidationEntity entity)
        {
        }
        
        public void UpdateExcludeValidationEntity(ExcludeValidationEntity entity)
        {
        }

        public void DeleteExcludeValidationEntity(ExcludeValidationEntity entity)
        {
        }
    }
    #endregion

#region Entities for RoundtripOriginalScenarios
    [RoundtripOriginal]
    public class EntityWithRoundtripOriginal_Derived : EntityWithoutRoundtripOriginal_Base
    {
        public string PropD { get; set; }
    }
    [KnownType(typeof(EntityWithRoundtripOriginal_Derived))]
    public class EntityWithoutRoundtripOriginal_Base
    {
        [Key]
        public int Key { get; set; }
        public string PropB { get; set; }
    }

    [RoundtripOriginal]
    public class RoundtripOriginalTestEntity_A
    {
        public string PropA { get; set; }
    }
    [KnownType(typeof(RoundtripOriginalTestEntity_D))]
    public class RoundtripOriginalTestEntity_B
    {
        [Key]
        public int Key { get; set; }
    }
    public class RoundtripOriginalTestEntity_C
    {
        public int PropC { get; set; }
    }
    public class RoundtripOriginalTestEntity_D
    {
        public int PropD { get; set; }
    }
    
    [EnableClientAccessAttribute]
    public class RTO_EntityWithRoundtripOriginalOnAssociationPropType
    {
        [Key]
        public int ID { get; set; }
        [Association("Assoc1_B", "ID", "Key")]
        public RoundtripOriginalTestEntity_B PropWithTypeLevelRTO { get; set; }
    }

    [EnableClientAccessAttribute]
    public class RTO_EntityWithRoundtripOriginalOnAssociationProperty
    {
        [Key]
        public int ID { get; set; }
        [RoundtripOriginal]
        [Association("Assoc2_B", "ID", "Key")]
        public EntityWithoutRoundtripOriginal_Base PropWithPropLevelRTO { get; set; }
    }

    [EnableClientAccessAttribute]
    [RoundtripOriginal]
    public class RTO_EntityWithRoundtripOriginalOnAssociationPropertyAndOnEntity
    {
        [Key]
        public int ID { get; set; }
        [RoundtripOriginal]
        [Association("Assoc2_B", "ID", "Key")]
        public EntityWithoutRoundtripOriginal_Base PropWithPropLevelRTO { get; set; }
    }

    [EnableClientAccessAttribute]
    [RoundtripOriginal]
    public class RTO_EntityWithRoundtripOriginalOnMember
    {
        [Key]
        public int ID { get; set; }
        [RoundtripOriginal]      
        public string PropWithPropLevelRTO { get; set; }
    }
#endregion
}

#region LTS Northwind Scenarios

namespace DataTests.Scenarios.LTS.Northwind
{
    [EnableClientAccess]
    public class LTS_NorthwindScenarios : LinqToSqlDomainService<NorthwindScenarios>
    {
        #region Bug479436 - Uni-Directional association
        [Query]
        public IQueryable<Customer_Bug479436> GetCustomer_Bug479436s()
        {
            return DataContext.Customer_Bug479436s;
        }

        [Query]
        public IQueryable<Order_Bug479436> GetOrder_Bug479436s()
        {
            return DataContext.Order_Bug479436s;
        }
        #endregion

        [Query]
        public IEnumerable<RequiredAttributeTestEntity> GetRequiredAttributeTestEntities()
        {
            throw new NotImplementedException();
        }
    }

    [MetadataType(typeof(OrderMetadata))]
    public partial class Order_Bug479436
    {
    }

    public class OrderMetadata
    {
        [Include]
        public static object Customer;
    }
}

#endregion LTS Northwind Scenarios

#region EF Northwind Scenarios

namespace DataTests.Scenarios.EF.Northwind
{
    [EnableClientAccess]
    public class EF_NorthwindScenarios_EmployeeWithExternalProperty : LinqToEntitiesDomainService<NorthwindEntities_Scenarios>
    {
        [Query]
        public IEnumerable<Employee> GetEmployees()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class EF_NorthwindScenarios_CustomerWithoutExternalProperty : LinqToEntitiesDomainService<NorthwindEntities_Scenarios>
    {
        [Query]
        public IEnumerable<Customer> GetCustomer()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class EF_NorthwindScenarios_RequiredAttribute : LinqToEntitiesDomainService<NorthwindEntities_Scenarios>
    {
        [Query]
        public IEnumerable<RequiredAttributeTestEntity> GetRequiredAttributeTestEntities()
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class EF_NorthwindScenarios_TimestampComparison : LinqToEntitiesDomainService<NorthwindEntities_Scenarios>
    {
        [Query]
        public IEnumerable<EntityWithNullFacetValuesForTimestampComparison> GetRequiredAttributeTestEntities()
        {
            throw new NotImplementedException();
        }
    }
}

#endregion EF Northwind Scenarios

#region VB Root Namespace Scenarios
namespace VBRootNamespaceTest
{
    using VBRootNamespaceTest.Other;
    using VBRootNamespaceTest3;

    [EnableClientAccess]
    public class VBRootNamespaceTestDomainService : DomainService
    {
        public IEnumerable<VBRootNamespaceDomainObject> M()
        {
            return null;
        }

        public IEnumerable<VBRootNamespaceDomainObject4> M4()
        {
            return null;
        }
    }

    [DataContract()]
    public class VBRootNamespaceDomainObject
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }

    namespace Inner
    {
        [EnableClientAccess]
        public class VBRootNamespaceTestProviderInsideInner : DomainService
        {
            public IEnumerable<VBRootNamespaceDomainObjectInsideInner> M()
            {
                return null;
            }

            public IEnumerable<VBRootNamespaceDomainObject3> M1()
            {
                return null;
            }
        }

        [DataContract()]
        public class VBRootNamespaceDomainObjectInsideInner
        {
            [DataMember()]
            [Key()]
            public int Key { get; set; }
        }
    }
}

namespace VBRootNamespaceTest2
{
    [EnableClientAccess]
    public class VBRootNamespaceTestDomainService2 : DomainService
    {
        public IEnumerable<VBRootNamespaceDomainObject2> M()
        {
            return null;
        }
    }

    [DataContract()]
    public class VBRootNamespaceDomainObject2
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }
}

namespace VBRootNamespaceTest.Other
{
    [DataContract()]
    public class VBRootNamespaceDomainObject4
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }
}

namespace VBRootNamespaceTest3
{
    [DataContract()]
    public class VBRootNamespaceDomainObject3
    {
        [DataMember()]
        [Key()]
        public int Key { get; set; }
    }

    [EnableClientAccess]
    public class VBRootNamespaceTestDomainService3 : DomainService
    {
        public IEnumerable<VBRootNamespaceEntityWithComplexProperty> M()
        {
            return null;
        }
    }

    public class VBRootNamespaceEntityWithComplexProperty
    {
        [Key]
        public int Key { get; set; }
        public ComplexType ComplexProp { get; set; }
    }

    public class ComplexType
    {
        public int Prop { get; set; }
    }
}

#endregion

#region Conflict Resolution Scenarios

namespace TestDomainServices.TypeNameConflictResolution
{
    [EnableClientAccess]
    public class OnlineMethodConflict : DomainService
    {
        [Query]
        public IEnumerable<Entity> GetEntities()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is deliberately named "Entities" to conflict with the 
        /// Entities property that will be auto-generated.
        /// </summary>
        [Invoke]
        public void Entities(Entity state)
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class DomainMethodConflict : DomainService
    {
        [Query]
        public IEnumerable<Entity> GetEntities()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is deliberately named "Name" to conflict with the 
        /// auto-generated "Name" entity property.
        /// </summary>
        [EntityAction]
        public void Name(Entity state)
        {
            throw new NotImplementedException();
        }
    }

    [EnableClientAccess]
    public class BaseTypeConflicts : DomainService
    {
        [Query]
        public IEnumerable<Entity> GetEntities()
        {
            throw new NotImplementedException();
        }
        [Query]
        public IEnumerable<DomainContext> GetDataContexts()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This DomainService type exposes type names that our codegen cannot handle
    /// unless emitting full type names.
    /// </summary>
    [EnableClientAccess]
    public class ForceTypeConflicts : DomainService
    {
        [Query]
        public IEnumerable<Attribute> GetAttributes()
        {
            throw new NotImplementedException();
        }
        [Query]
        public IEnumerable<DataMember> GetDataMembers()
        {
            throw new NotImplementedException();
        }
        [Query]
        public IEnumerable<DataMemberAttribute> GetDataMemberAttributes()
        {
            throw new NotImplementedException();
        }
    }

    public class Attribute
    {
        [Key]
        public string Name { get; set; }
    }

    public class DataMemberAttribute
    {
        [Key]
        public string Name { get; set; }
    }

    public class DataMember
    {
        [Key]
        public string Name { get; set; }
    }

    public class Entity
    {
        [Key]
        public string Name { get; set; }
    }

    public class DomainContext
    {
        [Key]
        public int DataContextID { get; set; }
    }

    namespace ExternalConflicts
    {
        public class GenericProvider<TEntity1, TEntity2> : DomainService
        {
            [Query]
            public IEnumerable<TEntity1> GetTEntity1()
            {
                throw new NotImplementedException();
            }
            [Query]
            public IEnumerable<TEntity2> GetTEntity2()
            {
                throw new NotImplementedException();
            }
        }

        [EnableClientAccess]
        public class DomainServiceScenario1 : GenericProvider<Namespace1.MockEntity1, Namespace2.MockEntity2> { }

        [EnableClientAccess]
        public class DomainServiceScenario2 : GenericProvider<Namespace2.MockEntity1, Namespace1.MockEntity2> { }

        namespace Namespace1
        {
            public class MockEntity1
            {
                [Key]
                public int EntityID { get; set; }

                [Association("MockEntity1_1", "EntityID", "EntityID")]
                public Namespace1.MockEntity2 Namespace1Entity2 { get; set; }

                [Association("MockEntity1_2", "EntityID", "EntityID")]
                public Namespace2.MockEntity1[] Namespace2Entity1 { get; set; }
            }

            public class MockEntity2
            {
                [Key]
                public int EntityID { get; set; }

                [Association("MockEntity2_1", "EntityID", "EntityID")]
                public Namespace1.MockEntity1 Namespace1Entity1 { get; set; }

                [Association("MockEntity2_2", "EntityID", "EntityID")]
                public Namespace2.MockEntity2[] Namespace2Entity2 { get; set; }
            }
        }

        namespace Namespace2
        {
            public class MockEntity1
            {
                [Key]
                public int EntityID { get; set; }
            }

            public class MockEntity2
            {
                [Key]
                public int EntityID { get; set; }
            }

            public class MockEntity3
            {
                [Key]
                public int EntityID { get; set; }
            }
        }

        namespace Namespace3
        {
            public class MockEntityWithExternalReferences
            {
                [Key]
                public int EntityID { get; set; }

                [ExternalReference, Association("MockEntityWithExternalReferences_1", "EntityID", "EntityID")]
                public Namespace1.MockEntity1 Namespace1Entity1 { get; set; }

                [ExternalReference, Association("MockEntityWithExternalReferences_2", "EntityID", "EntityID")]
                public Namespace2.MockEntity2 Namespace2Entity2 { get; set; }

                [ExternalReference, Association("MockEntityWithExternalReferences_3", "EntityID", "EntityID")]
                public Namespace2.MockEntity3 Namespace2Entity3 { get; set; }
            }
        }
    }
}

#endregion Conflict Resolution Scenarios

#region Global Namespace Scenarios

[EnableClientAccess]
public class GlobalNamespaceTest_DomainService_Invalid : DomainService
{
    public IEnumerable<GlobalNamespaceTest_Entity_Invalid> GetEntities()
    {
        return null;
    }
}

public class GlobalNamespaceTest_Entity_Invalid
{
    [Key]
    public int Key { get; set; }
}

public class GlobalNamespaceTest_AuthorizationAttribute : AuthorizationAttribute
{
    protected override AuthorizationResult IsAuthorized(System.Security.Principal.IPrincipal principal, AuthorizationContext authorizationContext)
    {
        return AuthorizationResult.Allowed;
    }
}

public enum GlobalNamespaceTest_Enum_NonShared
{
    DefaultValue,
    NonDefaultValue,
}

namespace GlobalNamespaceTest
{
    [EnableClientAccess]
    [GlobalNamespaceTest_Attribute(EnumProperty = GlobalNamespaceTest_Enum.NonDefaultValue)]
    [GlobalNamespaceTest_Authorization]
    public class GlobalNamespaceTest_DomainService : DomainService
    {
        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public IEnumerable<GlobalNamespaceTest_Entity> GetEntities()
        {
            return null;
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public void CreateEntity([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Entity entity)
        {
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public IEnumerable<GlobalNamespaceTest_Entity> ReadEntities([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Enum enumParameter)
        {
            return null;
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public void UpdateEntity([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Entity entity)
        {
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public void DeleteEntity([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Entity entity)
        {
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public void CustomUpdateEntity([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Entity entity, GlobalNamespaceTest_Enum enumParameter)
        {
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public void InvokeVoid([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Enum enumParameter)
        {
        }

        [GlobalNamespaceTest_Attribute]
        [GlobalNamespaceTest_Authorization]
        public GlobalNamespaceTest_Enum InvokeReturn([GlobalNamespaceTest_Validation] GlobalNamespaceTest_Enum enumParameter)
        {
            return GlobalNamespaceTest_Enum.DefaultValue;
        }
    }

    [GlobalNamespaceTest_Attribute]
    [GlobalNamespaceTest_Validation]
    public class GlobalNamespaceTest_Entity
    {
        [Key]
        [CustomValidation(typeof(GlobalNamespaceTest_Validation), "Validate")]
        [GlobalNamespaceTest_Validation]
        public int Key { get; set; }

        [GlobalNamespaceTest_Validation]
        public GlobalNamespaceTest_Enum EnumProperty { get; set; }

        [GlobalNamespaceTest_Validation]
        public GlobalNamespaceTest_Enum_NonShared EnumProperty_NonShared { get; set; }
    }
}

#endregion

#region System Namespace Scenarios

namespace System
{
    [EnableClientAccess]
    public class SystemDomainService : DomainService
    {
        public IEnumerable<SystemEntity> GetSystemEntities()
        {
            return null;
        }
    }

    [SystemNamespace]
    public class SystemEntity
    {
        [Key]
        public int Key { get; set; }

        [SystemNamespace]
        public SystemEnum SystemEnum { get; set; }

        [Subsystem.SubsystemNamespace]
        public Subsystem.SubsystemEnum SubsystemEnum { get; set; }

        public SystemGeneratedEnum SystemGeneratedEnum { get; set; }
    }

    public enum SystemGeneratedEnum
    {
        SystemGeneratedEnumValue
    }

    namespace Subsystem
    {
        [EnableClientAccess]
        public class SubsystemDomainService : DomainService
        {
            public IEnumerable<SubsystemEntity> GetSubsystemEntities()
            {
                return null;
            }
        }

        public class SubsystemEntity
        {
            [Key]
            public int Key { get; set; }

            [SystemNamespace]
            public SystemEnum SystemEnum { get; set; }

            [SubsystemNamespace]
            public SubsystemEnum SubsystemEnum { get; set; }

            public SubsystemGeneratedEnum SubsystemGeneratedEnum { get; set; }
        }

        public enum SubsystemGeneratedEnum
        {
            SubsystemGeneratedEnumValue
        }
    }
}

namespace SystemExtensions
{
    [EnableClientAccess]
    public class SystemExtensionsDomainService : DomainService
    {
        public IEnumerable<SystemExtensionsEntity> GetSystemExtensionsEntities()
        {
            return null;
        }
    }

    public class SystemExtensionsEntity
    {
        [Key]
        public int Key { get; set; }

        [SystemExtensionsNamespace]
        public SystemExtensionsEnum SystemExtensionsEnum { get; set; }

        public SystemExtensionsGeneratedEnum SystemExtensionsGeneratedEnum { get; set; }
    }

    public enum SystemExtensionsGeneratedEnum
    {
        SystemExtensionsGeneratedEnumValue
    }
}

#endregion
