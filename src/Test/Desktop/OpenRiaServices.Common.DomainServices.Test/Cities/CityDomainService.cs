using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using OpenRiaServices;
using OpenRiaServices.Hosting;
using OpenRiaServices.Server;

namespace Cities
{
    /// <summary>
    /// A simple test <see cref="CodeProcessor"/>, does not alter the codegen output in any way.
    /// </summary>
    /// <remarks>This is used to verify the generated output of <see cref="DomainIdentifierAttribute"/> is correct.</remarks>
    public class NoOpCodeProcessor : CodeProcessor
    {
        public NoOpCodeProcessor(CodeDomProvider codeDomProvider)
            : base(codeDomProvider)
        {
        }

        public override void ProcessGeneratedCode(DomainServiceDescription domainServiceDescription, CodeCompileUnit codeCompileUnit, IDictionary<Type, CodeTypeDeclaration> typeMapping)
        {
            return;
        }
    }

    /// <summary>
    /// This base class exists solely to test whether [DomainIdentifier] can be inherited
    /// </summary>
    [DomainIdentifier("CityProvider", CodeProcessor = typeof(NoOpCodeProcessor))]
    public class BaseDomainService : DomainService
    {
    }

    /// <summary>
    /// This class exposes a DomainService over the simple City data types
    /// </summary>
    [EnableClientAccess]
    public class CityDomainService : BaseDomainService
    {
        private readonly CityData _cityData = new CityData();

        // maintain list of deleted cities
        // this list is static because each query creates new domain service instance
        private static List<CityWithEditHistory> _deletedCities = new List<CityWithEditHistory>();

        [Query]
        public IQueryable<City> GetCities()
        {
            return this._cityData.Cities.AsQueryable<City>();
        }

        [Query]
        public IQueryable<CityWithInfo> GetCitiesWithInfo()
        {
            return this._cityData.Cities.OfType<CityWithInfo>().AsQueryable();
        }

        [Query]
        public IQueryable<CityWithEditHistory> GetCitiesWithEditHistory()
        {
            return this._cityData.Cities.OfType<CityWithEditHistory>().AsQueryable();
        }

        [Query]
        public IQueryable<CityWithEditHistory> GetDeletedCities()
        {
            return _deletedCities.AsQueryable();
        }

        [Query]
        public IQueryable<City> GetCitiesInState(string state)
        {
            if (state == null)
            {
                // there is a unit test that relies on this null
                // check being here
                throw new ArgumentNullException("state");
            }
            IEnumerable<City> cities = this._cityData.Cities.Where(c => c.StateName.Equals(state));
            return cities.AsQueryable<City>();
        }

        [Query]
        public IQueryable<County> GetCounties()
        {
            return this._cityData.Counties.AsQueryable<County>();
        }

        [Query]
        public IQueryable<State> GetStates()
        {
            return this._cityData.States.AsQueryable<State>();
        }

        [Query]
        public IQueryable<State> GetStatesInShippingZone(ShippingZone shippingZone)
        {
            return this._cityData.States.Where(s => s.ShippingZone == shippingZone).AsQueryable<State>();
        }

        [Update]
        public void UpdateState(State current)
        {
            State original = this.ChangeSet.GetOriginal(current);
            State state = _cityData.States.SingleOrDefault(p => p.Name == original.Name && p.FullName == original.FullName);
            state.FullName = current.FullName;
            state.Name = current.Name;
            state.TimeZone = current.TimeZone;
        }

        [Query]
        public IQueryable<Zip> GetZips()
        {
            return this._cityData.Zips.AsQueryable<Zip>();
        }

        [Query]
        public async Task<IQueryable<Zip>> GetZipsWithDelay(TimeSpan delay)
        {
            // This method is used to test cancellation of query operations
            // Since the method might return to soon otherwise we add a delay
            await Task.Delay(delay, ServiceContext.CancellationToken);
            return GetZips();
        }

        [Query]
        [RequiresAuthentication]
        public IQueryable<Zip> GetZipsIfAuthenticated()
        {
            return this._cityData.Zips.AsQueryable<Zip>();
        }

        [Query]
        [RequiresRole("manager")]
        public IQueryable<Zip> GetZipsIfInRole()
        {
            return this._cityData.Zips.AsQueryable<Zip>();
        }

        [Query]
        [RequiresUser("ZipGuy", ErrorMessage="Only one user is authorized for this query, and it isn't you.")]
        public IQueryable<Zip> GetZipsIfUser()
        {
            return this._cityData.Zips.AsQueryable<Zip>();
        }

        #region CUD methods
        [Insert]
        [CustomValidation(typeof(CityMethodValidator), "ValidateMethod")]
        public void InsertCity(City city)
        {
            System.Diagnostics.Debug.WriteLine("CityDomainService.InsertCity(" + city.ToString() + ")");
            this._cityData.Cities.Add(city);
        }

        [Update]
        [CustomValidation(typeof(CityMethodValidator), "ValidateMethod")]
        public void UpdateCity(City city)
        {
            System.Diagnostics.Debug.WriteLine("CityDomainService.UpdateCity(" + city.ToString() + ")");
            this.DeleteCity(city);
            this._cityData.Cities.Add(city);
        }

        [Delete]
        [CustomValidation(typeof(CityMethodValidator), "ValidateMethod")]
        public void DeleteCity(City city)
        {
            System.Diagnostics.Debug.WriteLine("CityDomainService.DeleteCity(" + city.ToString() + ")");

            City cityInList = this._cityData.Cities.FirstOrDefault(c => string.Equals(c.Name, city.Name) &&
                                                            string.Equals(c.StateName, city.StateName) &&
                                                            string.Equals(c.CountyName, city.CountyName));
            if (cityInList == null)
            {
                throw new InvalidOperationException("City must be in our list first: " + city);
            }
            this._cityData.Cities.Remove(cityInList);
        }

        [Insert]
        public void InsertZip(Zip zip)
        {
        }

        [Update]
        public void UpdateZip(Zip current)
        {
        }

        // Note: this CUD method works only if the user is authenticated.
        // Further, it is extended to deny any attempt to alter zip codes in WA unless the user is explicitly "WAGuy".
        [Delete]
        [RequiresAuthentication]
        [RequiresUserForState("WAGuy", "WA", ErrorMessage = "Only one user can delete zip codes from that state, and it isn't you.")]
        public void DeleteZip(Zip zip)
        {
        }
        #endregion

        #region Domain Methods
        [EntityAction]
        [CustomValidation(typeof(CityMethodValidator), "ValidateMethod")]
        public void AssignCityZone(City city, string zoneName)
        {
            if (zoneName.StartsWith("Zone"))
            {
                int zoneID = 0;
                if (int.TryParse(zoneName.Replace("Zone", ""), out zoneID))
                {
                    city.ZoneID = zoneID;
                }
            }
            city.ZoneName = zoneName;
        }

        // this method is to demonstrate parameterless domain method, which uses a server state to 
        // update some properties on the city entity
        [EntityAction]
        public void AutoAssignCityZone(City city)
        {
            city.ZoneID++;
            city.ZoneName = string.Format("Auto_Zone{0}", city.ZoneID.ToString());
        }

        [EntityAction]
        [RequiresUserForState("WAGuy", "WA", ErrorMessage="Only one user is authorized to execute operation '{0}', and it isn't you.")]
        public void AssignCityZoneIfAuthorized(City city, string zoneName)
        {
            this.AssignCityZone(city, zoneName);
        }

        [EntityAction]
        [CustomValidation(typeof(ZipValidator), "IsZipValid", ErrorMessage = "Zip codes cannot have matching city and state names")]
        public void ReassignZipCode(Zip zip,
                                    [Range(-9999, 9999)] int offset,
                                    bool useFull)
        {
            zip.Code += offset;
            if (useFull)
            {
                zip.FourDigit += offset;
            }
        }

        // domain method that throws ValidationException
        [EntityAction]
        [CustomValidation(typeof(ThrowExValidator), "IsThrowExValid")]
        public void ThrowException(Zip zip, string scenario)
        {
            switch (scenario)
            {
                case "ValidationException":
                    throw new ValidationException("testing");
                case "EntityValidationException":
                    // simulate an entity property validation error, ensuring that the specified entity
                    // property names are propagated back to the client
                    ValidationResult result = new ValidationResult("Invalid Zip properties!", new string[] { "CityName", "CountyName" });
                    throw new ValidationException(result, null, zip);
                case "DomainServiceException":
                    throw new DomainException("testing");
                case "InvalidOperationException":
                    throw new InvalidOperationException("testing");
                case "DomainServiceExceptionWithErrorCode":
                    throw new DomainException("testing with error code", 10);
                default:
                    // no op
                    break;
            }
        }

        // Custom method for CityWithInfo to update Info and to
        // update the edit history as well.
        [EntityAction]
        public void SetCityInfo(CityWithInfo cityWithInfo, string info)
        {
            cityWithInfo.Info = info;
            cityWithInfo.EditHistory = "info=" + info;
        }
        #endregion

        #region Invoke operations
        [Invoke]
        public string Echo(string msg)
        {
            return "Echo: " + msg;
        }

        [Invoke]
        public async Task<string> EchoWithDelayAsync(string msg, TimeSpan delay)
        {
            // This method is used to test cancellation of invoke operations
            // Since the method might return to soon otherwise we add a delay
            await Task.Delay(delay, ServiceContext.CancellationToken);
            return Echo(msg);
        }

        // This service operation is invoked to reset any static data
        // we retain across instances
        [Invoke]
        public void ResetData()
        {
            _deletedCities.Clear();
        }

        [Invoke]
        public bool UsesCustomHost()
        {
            return (OperationContext.Current.Host.GetType() == typeof(CityDomainServiceHost));
        }

        // Invoke that has a custom authorization attribute.  Permission denied for any City
        // whose state is Ohio unless the user is Mathew.
        [Invoke]
        [RequiresUser("WaGuy")]
        public string GetStateIfUser(City city)
        {
            return city.StateName;
        }

        #endregion

        #region Derived CUD/Custom/Service methods

        // Note: explicitly missing are CUD methods on CityWithInfo
        // so that we verify CUD operations execute against their
        // nearest base type, not the root.
        [Insert]
        public void InsertCityWithEditHistory(CityWithEditHistory city)
        {
            city.EditHistory = "insert";
            this.InsertCity(city);
        }

        [Update]
        public void UpdateCityWithEditHistory(CityWithEditHistory city)
        {
            city.EditHistory = "update";
            this.UpdateCity(city);
        }

        [Delete]
        public void DeleteCityWithEditHistory(CityWithEditHistory city)
        {
            city.EditHistory = "delete";
            // this version tombstones by moving to alternate list
            _deletedCities.Add(city);

            // Call base CUD to delete from main store
            this.DeleteCity(city);
        }

        [EntityAction]
        public void TouchHistory(CityWithEditHistory city, string touchString)
        {
            city.EditHistory = "touch=" + touchString;
        }
        #endregion //Derived CUD/Custom/Service methods
    }

    /// <summary>
    /// Test validator used to apply method level validation to CUD operations.
    /// </summary>
    public static class CityMethodValidator
    {
        public static ValidationResult ValidateMethod(object vtcObject, ValidationContext context)
        {
            ValidationResult validationResult = null;
            City city = vtcObject as City;

            // sentinal value used by tests to indicate that validation should fail.
            if (city.ZoneID == 693)
            {
                return new ValidationResult(string.Format("CityMethodValidator.ValidateMethod Failed ({0})!", context.MemberName));
            }

            return validationResult ?? ValidationResult.Success;
        }
    }
}
