using System;
using System.Collections.Generic;
using System.Linq;

namespace Cities
{
    /// <summary>
    /// Sample data class
    /// </summary>
    /// <remarks>
    /// This class exposes several data types (City, County, State and Zip) and some sample
    /// data for each.  It can serve as a framework for building mocks of domain services, etc.
    /// <para>
    /// Like CityTypes.cs, nothing in this class depends on Silverlight or the desktop CLR.
    /// These 2 files can be included into other projects, including Silverlight, and used
    /// to build mock data sources.
    /// </para>
    /// </remarks>
    public partial class CityData
    {
        private List<State> _states;
        private List<County> _counties;
        private List<City> _cities;
        private List<Zip> _zips;
        private List<ZipWithInfo> _zipsWithInfo;
        private List<CityWithInfo> _citiesWithInfo;

        public CityData() {
            _states = new List<State>()
            {
                new State() { Name="WA", FullName="Washington", TimeZone = TimeZone.Pacific },
                new State() { Name="OR", FullName="Oregon", TimeZone = TimeZone.Pacific },
                new State() { Name="CA", FullName="California", TimeZone = TimeZone.Pacific },
                new State() { Name="OH", FullName="Ohio", TimeZone = TimeZone.Eastern, ShippingZone=ShippingZone.Eastern }
            };

            _counties = new List<County>()
             {
                new County() { Name="King",         StateName="WA" },
                new County() { Name="Pierce",       StateName="WA" },
                new County() { Name="Snohomish",    StateName="WA" },

                new County() { Name="Tillamook",    StateName="OR" },
                new County() { Name="Wallowa",      StateName="OR" },
                new County() { Name="Jackson",      StateName="OR" },

                new County() { Name="Orange",       StateName="CA" },
                new County() { Name="Santa Barbara",StateName="CA" },

                new County() { Name="Lucas",        StateName="OH" }
            };
            foreach (State state in _states) {
                foreach (County county in _counties.Where(p => p.StateName == state.Name)) {
                    state.Counties.Add(county);
                    county.State = state;
                }
            }

            _cities = new List<City>()
            {
                new CityWithInfo() {Name="Redmond", CountyName="King", StateName="WA", Info="Has Microsoft campus", LastUpdated=DateTime.Now},
                new CityWithInfo() {Name="Bellevue", CountyName="King", StateName="WA", Info="Means beautiful view", LastUpdated=DateTime.Now},
                new City() {Name="Duvall", CountyName="King", StateName="WA"},
                new City() {Name="Carnation", CountyName="King", StateName="WA"},
                new City() {Name="Everett", CountyName="King", StateName="WA"},
                new City() {Name="Tacoma", CountyName="Pierce", StateName="WA"},

                new City() {Name="Ashland", CountyName="Jackson", StateName="OR"},

                new City() {Name="Santa Barbara", CountyName="Santa Barbara", StateName="CA"},
                new City() {Name="Orange", CountyName="Orange", StateName="CA"},

                new City() {Name="Oregon", CountyName="Lucas", StateName="OH"},
                new City() {Name="Toledo", CountyName="Lucas", StateName="OH"}
            };

            _citiesWithInfo = new List<CityWithInfo>(this._cities.OfType<CityWithInfo>());

            foreach (County county in _counties) {
                foreach (City city in _cities.Where(p => p.CountyName == county.Name && p.StateName == county.StateName)) {
                    county.Cities.Add(city);
                    city.County = county;
                }
            }

            _zips = new List<Zip>()
            {
                new Zip() { Code=98053, FourDigit=8625, CityName="Redmond", CountyName="King", StateName="WA" },
                new ZipWithInfo() { Code=98052, FourDigit=8300, CityName="Redmond", CountyName="King", StateName="WA", Info="Microsoft" },
                new Zip() { Code=98052, FourDigit=6399, CityName="Redmond", CountyName="King", StateName="WA" },
            };

            _zipsWithInfo = new List<ZipWithInfo>(this._zips.OfType<ZipWithInfo>());

            foreach (City city in _cities) {
                foreach (Zip zip in _zips.Where(p => p.CityName == city.Name && p.CountyName == city.CountyName && p.StateName == city.StateName)) {
                    city.ZipCodes.Add(zip);
                    zip.City = city;
                }
            }

            foreach (CityWithInfo city in _citiesWithInfo)
            {
                foreach (ZipWithInfo zip in _zipsWithInfo.Where(p => p.CityName == city.Name && p.CountyName == city.CountyName && p.StateName == city.StateName))
                {
                    city.ZipCodesWithInfo.Add(zip);
                    zip.City = city;
                }
            }
        }

        public List<State> States { get { return this._states; } }
        public List<County> Counties { get { return this._counties; } }
        public List<City> Cities { get { return this._cities; } }
        public List<CityWithInfo> CitiesWithInfo { get { return this._citiesWithInfo; } }
        public List<Zip> Zips { get { return this._zips; } }
        public List<ZipWithInfo> ZipsWithInfo { get { return this._zipsWithInfo; } }
    }
}
