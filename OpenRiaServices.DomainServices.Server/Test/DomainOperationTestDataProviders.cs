using System;
using System.Collections.Generic;
using System.Linq;
using OpenRiaServices.DomainServices.Hosting;
using Cities;

namespace OpenRiaServices.DomainServices.Server.Test
{
    #region Valid domain method test providers
    [EnableClientAccess]
    public class DomainMethod_ValidProvider_MultipleMethods : CityDomainService
    {
        public string Invoked { get; set; }
        public byte[] InputData { get; set; }

        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, byte[] data)
        {
            this.Invoked += "ProcessCity_";
            this.InputData = data;
        }

        [Update(UsingCustomMethod = true)]
        public void ProcessCounty(County county)
        {
            this.Invoked += "ProcessCounty_";
        }
    }

    [EnableClientAccess]
    public class DomainMethod_ValidProvider_NoDomainMethods : DomainService
    {
    }

    [EnableClientAccess]
    public class DomainMethod_ValidProvider_SameNameDiffEntities : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void Approve(City city, string param1, string param2)
        {
        }

        [Update(UsingCustomMethod = true)]
        public void Approve(County county, bool flag)
        {
        }
    }
    #endregion

    #region Invalid domain method test providers
    // same domain method name used across multiple providers
    [EnableClientAccess]
    public class DomainMethod_DupNameAcrossProviders1 : DomainService
    {
        private CityData _cityData = new CityData();

        [Query]
        public IQueryable<City> GetCities()
        {
            return this._cityData.Cities.AsQueryable();
        }

        [Update(UsingCustomMethod = true)]
        public void Approve(City city)
        {
        }
    }
    [EnableClientAccess]
    public class DomainMethod_DupNameAcrossProviders2 : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void Approve(City city)
        {
        }
    }

    // domain method overloads not supported
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_MethodOverloads : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city)
        {
        }

        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, string data)
        {
        }
    }

    // non-void return type not supported
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_InvalidReturnType : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public string ProcessCity(City city)
        {
            return null;
        }
    }

    // first argument should be of entity type
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_FirstArgNonEntity : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(string name)
        {
        }
    }

    // argument of type object
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_ArgOfTypeObject : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, object objArg)
        {
        }
    }

    // argument of type IntPtr
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_ArgOfTypeIntPtr : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, IntPtr intPtrArg)
        {
        }
    }

    // argument of type IEnumerable
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_ArgOfComplexTypeIEnumerable : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, IEnumerable<City> ienumerableArg)
        {
        }
    }

    // argument of type List<>
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_ArgOfComplexTypeList : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, List<City> listArg)
        {
        }
    }

    // argument of type UIntPtr
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_ArgOfTypeUIntPtr : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, UIntPtr uintPtrArg)
        {
        }
    }

    // parameterless domain method
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_Parameterless : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity()
        {
        }
    }

    // other arguments should be of simple/primitive types
    [EnableClientAccess]
    public class DomainMethod_InvalidProvider_MultipleEntities : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void ProcessCity(City city, County county)
        {
        }
    }
    #endregion

    #region Valid invoke operation test providers
    [EnableClientAccess]
    public class OnlineMethod_ValidProvider_MultipleMethods : CityDomainService
    {
        [Invoke]
        public void Process_VoidReturn(string name)
        {
        }

        [Invoke]
        public int Process_IntReturn()
        {
            return 0;
        }

        [Invoke]
        public string Process_EntityParam(City city)
        {
            return city.Name;
        }

        [Invoke]
        public string Process_EntitiesAndSimpleParams(Zip zip, City city, string otherParam)
        {
            return string.Format("{0}_{1}_{2}", zip.Code.ToString(), city.Name, otherParam);
        }

        [Invoke]
        public IEnumerable<City> Process_Return_EntityListParam(IEnumerable<City> cities)
        {
            return cities;
        }
    }
    #endregion

    #region Invalid invoke operation test providers
    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_NonEntityParam : DomainService
    {
        // this provider does not define City
        [Invoke]
        public string TestMethod(NonEntity entity)
        {
            return null;
        }
    }

    public abstract class NonEntity
    {
        public int ID { get; set; }
    }

    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_NonSimpleParam : DomainService
    {
        [Invoke]
        public string TestMethod(LinkedList<string> param)
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_NonSimpleReturn : DomainService
    {
        [Invoke]
        public LinkedList<string> TestMethod()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_NonEntityReturn : DomainService
    {
        [Invoke]
        public NonEntity TestMethod()
        {
            return null;
        }
    }

    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_DupMethodName : DomainService
    {
        [Invoke]
        public void TestMethod(string param)
        {
        }

        [Invoke]
        public void TestMethod()
        {
        }
    }

    // argument of type IntPtr
    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_ArgOfTypeIntPtr : DomainService
    {
        [Invoke]
        public void TestMethod(IntPtr intPtrArg)
        {
        }
    }

    // argument of type UIntPtr
    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_ArgOfTypeUIntPtr : DomainService
    {
        [Invoke]
        public void TestMethod(UIntPtr uintPtrArg)
        {
        }
    }

    // argument of type IEnumerable
    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_ArgOfComplexTypeIEnumerable : DomainService
    {
        [Invoke]
        public void TestMethod(IEnumerable<object> ienumerableArg)
        {
        }
    }

    // argument of type List<string>
    [EnableClientAccess]
    public class OnlineMethod_InvalidProvider_ArgOfComplexTypeList : DomainService
    {
        [Invoke]
        public void TestMethod(List<object> listArg)
        {
        }
    }
    #endregion

    #region Invalid CUD method test providers
    [EnableClientAccess]
    public class UpdateMethod_InvalidProvider_VoidInput : DomainService
    {
        [Update]
        public void UpdateCounty1()
        {
        }
    }

    [EnableClientAccess]
    public class UpdateMethod_InvalidProvider_TooManyParams : CityDomainService
    {
        [Update]
        public void UpdateCounty2(County county1, County county2)
        {
        }
    }

    [EnableClientAccess]
    public class UpdateMethod_InvalidProvider_ByRefParam : CityDomainService
    {
        [Update]
        public void UpdateCounty3(ref County county1)
        {
        }
    }

    [EnableClientAccess]
    public class DeleteMethod_InvalidProvider_VoidInput : DomainService
    {
        [Delete]
        public void DeleteCounty1()
        {
        }
    }

    [EnableClientAccess]
    public class DeleteMethod_InvalidProvider_TooManyParams : CityDomainService
    {
        [Delete]
        public void DeleteCounty2(County county1, County county2)
        {
        }
    }

    [EnableClientAccess]
    public class DeleteMethod_InvalidProvider_ByRefParam : CityDomainService
    {
        [Delete]
        public void DeleteCounty3(ref County county1)
        {
        }
    }

    [EnableClientAccess]
    public class InsertMethod_InvalidProvider_VoidInput : DomainService
    {
        [Insert]
        public void InsertCounty1()
        {
        }
    }

    [EnableClientAccess]
    public class InsertMethod_InvalidProvider_TooManyParams : DomainService
    {
        [Insert]
        public void InsertCounty2(County county1, County county2)
        {
        }
    }

    [EnableClientAccess]
    public class InsertMethod_InvalidProvider_ByRefParam : DomainService
    {
        [Insert]
        public void InsertCounty3(ref County county1)
        {
        }
    }

    public class SelectMethod_InvalidProvider_ComnplexParams : CityDomainService
    {
        [Query]
        public IEnumerable<County> GetCounties(County prototype)
        {
            yield break;
        }
    }

    public class DomainMethod_InvalidProvider_ComnplexParams : CityDomainService
    {
        [Update(UsingCustomMethod = true)]
        public void LinkTo(County entity, County prototype)
        {
        }
    }
    #endregion
}
