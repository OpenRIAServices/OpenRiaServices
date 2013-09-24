using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using OpenRiaServices.DomainServices.Client;
using System.Windows.Common;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;
using Cities;
using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestDomainServices;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace System.Windows.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests the filtering aspects of the <see cref="DomainDataSource"/> feature.
    /// </summary>
    [TestClass]
    public class FilteringTests : DomainDataSourceTestBase
    {
        #region Filtering

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly.")]
        public void Filtering()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsNotEqualTo, Value = "Lucas" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that FilterDescriptor with dotted property path.")]
        public void FilteringWithDottedPropertyPath()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "County.State.Name", Operator = FilterOperator.IsEqualTo, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Description("Tests that FilterDescriptor with invalid dotted property path.")]
        public void FilteringWithInvalidDottedPath()
        {
            this._dds.AutoLoad = true;
            this._dds.QueryName = "GetCitiesQuery";
            this._dds.DomainContext = new CityDomainContext();
            AssertExpectedException(
                new ArgumentException("The property named 'County.Invalid.Name' cannot be found on type 'City'."),
                () => this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "County.Invalid.Name", Operator = FilterOperator.IsEqualTo, Value = "OH" }));
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with changing the value of an existing filter.")]
        public void FilteringChangingValues()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "OH";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].PropertyPath = "CountyName";
                this._dds.FilterDescriptors[0].Value = "Wood";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "Lucas";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "lucas";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsEqualTo.")]
        public void FilteringIsEqualTo()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "oH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsNotEqualTo.")]
        public void FilteringIsNotEqualTo()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsNotEqualTo, Value = "Oh" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsGreaterThan.")]
        public void FilteringIsGreaterThan()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();
            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsGreaterThan, Value = "oh" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(7, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsGreaterThanOrEqualTo.")]
        public void FilteringIsGreaterThanOrEqualTo()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsGreaterThanOrEqualTo, Value = "Oh" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsLessThan.")]
        public void FilteringIsLessThan()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsLessThan, Value = "oh" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsLessThanOrEqualTo.")]
        public void FilteringIsLessThanOrEqualTo()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsLessThanOrEqualTo, Value = "oh" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(4, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with StartsWith.")]
        public void FilteringStartsWith()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = "o" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(3, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with EndsWith.")]
        public void FilteringEndsWith()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.EndsWith, Value = "a" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(8, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with Contains.")]
        public void FilteringContains()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.Contains, Value = "o" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsContainedIn.")]
        public void FilteringIsContainedIn()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "CountyName", Operator = FilterOperator.IsContainedIn, Value = "kingston" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests a FilterDescriptor that uses a ControlParameter with a Converter/ConverterParameter.")]
        public void FilteringWithControlParameterAndConverter()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("Ohio"); // Ohio (4 characters) --> OH
                FilterDescriptor filterDescriptor = new FilterDescriptor() { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo };
                Binding binding = this.CreateBinding(filterDescriptor, FilterDescriptor.ValueProperty, this._textBox, "Text.Length", new StateConverter(), null);
                this._dds.FilterDescriptors.Add(filterDescriptor);
                this._dds.Load();
                //Assert.AreEqual("OH", (string)this._dds.FilterDescriptors[0].Value.GetConvertedValue(typeof(string)));
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("O"); // O (1 character) --> OR
                //Assert.AreEqual("OR", (string)this._dds.FilterDescriptors[0].Value.GetConvertedValue(typeof(string)));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsEqualTo and ControlParameters.")]
        public void FilteringIsEqualToWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsEqualTo, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsNotEqualTo and ControlParameters.")]
        public void FilteringIsNotEqualToWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsNotEqualTo, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(10, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsGreaterThan and ControlParameters.")]
        public void FilteringIsGreaterThanWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsGreaterThan, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(7, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsGreaterThanOrEqualTo and ControlParameters.")]
        public void FilteringIsGreaterThanOrEqualToWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsGreaterThanOrEqualTo, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(7, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(9, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsLessThan and ControlParameters.")]
        public void FilteringIsLessThanWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsLessThan, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(4, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsLessThanOrEqualTo and ControlParameters.")]
        public void FilteringIsLessThanOrEqualToWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OH");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.IsLessThanOrEqualTo, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(4, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("OR");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "IA";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with StartsWith and ControlParameters.")]
        public void FilteringStartsWithWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("O");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.StartsWith, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(3, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("C");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "W";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with EndsWith and ControlParameters.")]
        public void FilteringEndsWithWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("A");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("StateName", FilterOperator.EndsWith, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(8, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("H");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "R";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with Contains and ControlParameters.")]
        public void FilteringContainsWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("o");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("CountyName", FilterOperator.Contains, this._textBox, "Text"));
                this._dds.FilterDescriptors[0].IsCaseSensitive = true;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("e");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(2, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "a";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with IsContainedIn and ControlParameters.")]
        public void FilteringIsContainedInWithControlParameters()
        {
            this.LoadDomainDataSourceControl();
            this.LoadTextBoxControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("Kingston");
                this._dds.FilterDescriptors.Add(this.CreateValueBoundFilterDescriptor("CountyName", FilterOperator.IsContainedIn, this._textBox, "Text"));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
                this.ResetLoadState();
                this.TextBoxValueProvider.SetValue("Oranges");
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors[0].Value = "Jacksonville";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with LogicalOperator-AND.")]
        public void FilteringLogicalOperatorAND()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(FilterDescriptorLogicalOperator.And, this._dds.FilterOperator);
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = "O" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsGreaterThan, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(1, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with LogicalOperator-OR.")]
        public void FilteringLogicalOperatorOR()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterOperator = FilterDescriptorLogicalOperator.Or;
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = "C" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = "O" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(5, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly with changing the value of LogicalOperator")]
        public void FilteringChangingLogicalOperator()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterOperator = FilterDescriptorLogicalOperator.Or;
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "CA" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(4, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterOperator = FilterDescriptorLogicalOperator.And;
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(0, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property functions properly when the FilterDescriptors property is updated and when it gets NULL value.")]
        public void FilteringPropertyChanging()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterOperator = FilterDescriptorLogicalOperator.Or;
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "CA" });
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "OH" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(4, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.Clear();
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = "W" });
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(6, this._view.Count);
                this.ResetLoadState();
                this._dds.FilterDescriptors.Clear();
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.AreEqual(11, this._view.Count);
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property throws the right exceptions with StartsWith.")]
        public void FilteringStartsWithExceptions()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() => 
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                // Invalid Operator
                this._dds.FilterDescriptors.Add(new FilterDescriptor() { PropertyPath = "ZoneID", Operator = FilterOperator.StartsWith, Value = "A" });
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "ZoneID");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.FilterNotSupported, "ZoneID", "City", "Int32", "StartsWith");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");

                // Invalid Conversion
                this._dds.FilterDescriptors[0] = new FilterDescriptor() { PropertyPath = "StateName", Operator = FilterOperator.StartsWith, Value = Colors.Red };
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "StateName");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.IncompatibleOperands, "StartsWith", "String", "Color");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property throws the right exceptions with EndsWith.")]
        public void FilteringEndsWithExceptions()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                // Invalid Operator
                this._dds.FilterDescriptors.Add(new FilterDescriptor() { PropertyPath = "ZoneID", Operator = FilterOperator.EndsWith, Value = "A" });
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "ZoneID");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.FilterNotSupported, "ZoneID", "City", "Int32", "EndsWith");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");

                // Invalid Conversion
                this._dds.FilterDescriptors[0] = new FilterDescriptor() { PropertyPath = "StateName", Operator = FilterOperator.EndsWith, Value = Colors.Red };
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "StateName");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.IncompatibleOperands, "EndsWith", "String", "Color");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property throws the right exceptions with Contains.")]
        public void FilteringContainsExceptions()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                // Invalid Operator
                this._dds.FilterDescriptors.Add(new FilterDescriptor() { PropertyPath = "ZoneID", Operator = FilterOperator.Contains, Value = "A" });
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "ZoneID");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.FilterNotSupported, "ZoneID", "City", "Int32", "Contains");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");

                // Invalid Conversion
                this._dds.FilterDescriptors[0] = new FilterDescriptor() { PropertyPath = "StateName", Operator = FilterOperator.Contains, Value = Colors.Red };
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "StateName");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.IncompatibleOperands, "Contains", "String", "Color");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Tests that the FilterDescriptors property throws the right exceptions with IsContainedIn.")]
        public void FilteringIsContainedInExceptions()
        {
            this.LoadDomainDataSourceControl();

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();

                // Invalid Operator
                this._dds.FilterDescriptors.Add(new FilterDescriptor() { PropertyPath = "ZoneID", Operator = FilterOperator.IsContainedIn, Value = "A" });
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "ZoneID");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.FilterNotSupported, "ZoneID", "City", "Int32", "IsContainedIn");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");

                // Invalid Conversion
                this._dds.FilterDescriptors[0] = new FilterDescriptor() { PropertyPath = "StateName", Operator = FilterOperator.IsContainedIn, Value = Colors.Red };
            });

            this.AssertExpectedLoadError(() => this._dds.Load());

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                string message = string.Format(
                    DomainDataSourceResources.CannotEvaluateDescriptor, "FilterDescriptor", "StateName");
                Assert.AreEqual(message, this._ddsLoadedDataEventArgs.Error.Message,
                    "Exception messages should be equal.");

                string innerMessage = string.Format(
                    DomainDataSourceResources.IncompatibleOperands, "IsContainedIn", "String", "Color");
                Assert.AreEqual(innerMessage, this._ddsLoadedDataEventArgs.Error.InnerException.Message,
                    "Inner exception messages should be equal.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(803699)] // Raise LoadedData event with the error instead of throwing exception from Load()
        [Description("Tests FilterDescriptor with invalid Value.Value.")]
        public void FilterDescriptorWithInvalidValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.Load();
            });

            this.AssertLoadingData();
            FilterDescriptor filter = new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsGreaterThan, Value = "WA" };

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this._dds.FilterDescriptors.Add(filter);
                this._dds.FilterDescriptors[0].Value = Colors.Red;
            });

            string expectedMessage = string.Format(
                DomainDataSourceResources.CannotEvaluateDescriptor,
                "FilterDescriptor",
                filter.PropertyPath);
            this.AssertExpectedLoadError(typeof(InvalidOperationException), expectedMessage, () => this._dds.Load());

            EnqueueTestComplete();
        }

        /// <summary>
        /// Test class to define what filter expressions should succeed and which should fail to be built.
        /// <para>
        /// For testing the actual functionality of filter expressions, use <see cref="FilterDescriptorTestCase"/>.
        /// </para>
        /// </summary>
        private class TestCase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestCase"/> class to be used
            /// to ensure filter expressions can be built appropriately.
            /// </summary>
            /// <param name="memberName">The member name.</param>
            /// <param name="filter">The filter operator.</param>
            /// <param name="value">The value to test the member against.</param>
            /// <param name="expectException">Whether or not an exception is expected.</param>
            public TestCase(string memberName, FilterOperator filter, object value, bool expectException)
            {
                this.MemberName = memberName;
                this.FilterOperator = filter;
                this.Value = value;
                this.ExceptionExpected = expectException;
            }

            /// <summary>
            /// Gets or sets the member name.
            /// </summary>
            public string MemberName { get; set; }

            /// <summary>
            /// Gets or sets the filter operator.
            /// </summary>
            public FilterOperator FilterOperator { get; set; }

            /// <summary>
            /// Gets or sets the value to filter against.
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether or not an exception is expected.
            /// </summary>
            public bool ExceptionExpected { get; set; }
        }

        private TestCase[] _testCases = new TestCase[]
        {
            new TestCase("Int32F", FilterOperator.IsEqualTo, null, true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, 1, false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, -1, false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, true, false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("Int32F", FilterOperator.IsEqualTo, 'a', false),
            new TestCase("Int32F", FilterOperator.IsEqualTo, TestEnumeration.One, false),

            new TestCase("Int32F", FilterOperator.IsGreaterThan, null, true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, true, false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, 'a', false),
            new TestCase("Int32F", FilterOperator.IsGreaterThan, TestEnumeration.One, false),

            new TestCase("UInt32P", FilterOperator.IsEqualTo, null, true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, 1, false),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, -1, true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, true, false),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, 'a', false),
            new TestCase("UInt32P", FilterOperator.IsEqualTo, TestEnumeration.One, false),

            new TestCase("UInt32P", FilterOperator.IsGreaterThan, null, true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, true, false),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, 'a', false),
            new TestCase("UInt32P", FilterOperator.IsGreaterThan, TestEnumeration.One, false),

            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, null, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, 1, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, -1, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, Convert.ToString(1), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, true, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), false),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("TimeSpanP", FilterOperator.IsEqualTo, TestEnumeration.One, true),

            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, 1, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, Convert.ToString(1), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), false),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("TimeSpanP", FilterOperator.IsGreaterThan, TestEnumeration.One, true),

            new TestCase("SingleP", FilterOperator.IsEqualTo, null, true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, Int64.MaxValue, false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, true, false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, Convert.ToString(1.1), false),
            new TestCase("SingleP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("SingleP", FilterOperator.IsEqualTo, TestEnumeration.One, false),

            new TestCase("SingleP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, Int64.MaxValue, false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, true, false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), false),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("SingleP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),
            
            new TestCase("CharP", FilterOperator.IsEqualTo, null, true),
            new TestCase("CharP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("CharP", FilterOperator.IsEqualTo, -1, true),
            new TestCase("CharP", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("CharP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("CharP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("CharP", FilterOperator.IsEqualTo, true, true),
            new TestCase("CharP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("CharP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("CharP", FilterOperator.IsEqualTo, 'a', false),
            new TestCase("CharP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("CharP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("CharP", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("CharP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("CharP", FilterOperator.IsGreaterThan, 'a', false),
            new TestCase("CharP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),
            
            new TestCase("StringP", FilterOperator.IsEqualTo, null, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("StringP", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("StringP", FilterOperator.IsEqualTo, "foo", false),
            new TestCase("StringP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("StringP", FilterOperator.IsEqualTo, Int64.MaxValue, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, true, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("StringP", FilterOperator.IsEqualTo, Convert.ToString(1.1), false),
            new TestCase("StringP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("StringP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), false),
            new TestCase("StringP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("StringP", FilterOperator.IsEqualTo, 'a', false),
            new TestCase("StringP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("StringP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("StringP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, "foo", false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("StringP", FilterOperator.IsGreaterThan, Int64.MaxValue, false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, true, false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("StringP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("StringP", FilterOperator.IsGreaterThan, 'a', false),
            new TestCase("StringP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),
            
            new TestCase("BooleanP", FilterOperator.IsEqualTo, null, true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, Convert.ToString(1), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, Int64.MaxValue, false),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, true, false),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("BooleanP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, Convert.ToString(1), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, Int64.MaxValue, false),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, true, false),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("BooleanP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),

            new TestCase("TestEnumP", FilterOperator.IsEqualTo, null, false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, "10", false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, "One", false),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, true, true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("TestEnumP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, null, false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, "10", false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, "One", false),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("TestEnumP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),

            new TestCase("DateTimeP", FilterOperator.IsEqualTo, null, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, 1, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, -1, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, Convert.ToString(1), true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, true, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, Convert.ToString(1.1), false),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), false),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("DateTimeP", FilterOperator.IsEqualTo, TestEnumeration.One, true),

            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, 1, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(1), true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), false),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), false),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("DateTimeP", FilterOperator.IsGreaterThan, TestEnumeration.One, true),

            // Nullable Types
            new TestCase("NInt32F", FilterOperator.IsEqualTo, null, false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, 1, false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, -1, false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, true, false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, 'a', false),
            new TestCase("NInt32F", FilterOperator.IsEqualTo, TestEnumeration.One, false),

            new TestCase("NInt32F", FilterOperator.IsGreaterThan, null, false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, true, false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, 1.1, false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, 'a', false),
            new TestCase("NInt32F", FilterOperator.IsGreaterThan, TestEnumeration.One, false),
            
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, null, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, Convert.ToString(1), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, Int64.MaxValue, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, true, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, 1.1, false),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("NBooleanP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, null, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, 1, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, Convert.ToString(1), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("NBooleanP", FilterOperator.IsGreaterThan, TestEnumeration.One, true),

            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, null, false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, 1, false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, -1, false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, Convert.ToString(1), false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, "10", false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, Convert.ToString(-1), false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, "One", false),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, true, true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, Convert.ToString(1.1), true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("NTestEnumP", FilterOperator.IsEqualTo, TestEnumeration.One, false),
            
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, null, false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, 1, false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, -1, false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(1), false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, "10", false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(-1), false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, "One", false),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("NTestEnumP", FilterOperator.IsGreaterThan, TestEnumeration.One, false),
            
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, null, false),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, 1, true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, -1, true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, Convert.ToString(1), true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, Convert.ToString(-1), true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, "foo", true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, new EmptyTestClass(), true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, Int64.MaxValue, true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, true, true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, 1.1, true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, Convert.ToString(1.1), false),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, new DateTime(2008, 1, 1), false),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, 'a', true),
            new TestCase("NDateTimeP", FilterOperator.IsEqualTo, TestEnumeration.One, true),

            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, null, false),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, 1, true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, -1, true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(1), true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(-1), true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, "foo", true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, new EmptyTestClass(), true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, Int64.MaxValue, true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, true, true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, 1.1, true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, Convert.ToString(1.1), false),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, new TimeSpan(1, 1, 1, 1), true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, new DateTime(2008, 1, 1), false),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, new DateTimeOffset(new DateTime(2010, 12, 14), new TimeSpan(-2, 0, 0)), true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, 'a', true),
            new TestCase("NDateTimeP", FilterOperator.IsGreaterThan, TestEnumeration.One, true),
        };

        [TestMethod]
        [Description("Tests whether filter expressions can be built correctly and whether expected exceptions are created.")]
        public void BuildingFilterExpressions()
        {
            System.Linq.Expressions.Expression filterExpression = null;
            Exception exception = null;

            List<AssertFailedException> failures = new List<AssertFailedException>();
            foreach (TestCase tc in this._testCases)
            {
                Type type = typeof(DataTypeTestClass);
                PropertyInfo pi = type.GetPropertyInfo(tc.MemberName);
                exception = null;

                object convertedValue = null;
                // Just skip these cases, we're expecting other exceptions
                if ((pi != null) && !LinqHelper.IsSupportedOperator(type, tc.FilterOperator))
                {
                    try
                    {
                        convertedValue = Utilities.GetConvertedValue(CultureInfo.CurrentCulture, pi.PropertyType, tc.Value);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }

                if (exception == null)
                {
                    try
                    {
                        filterExpression = LinqHelper.BuildFilterExpression(
                            type,
                            tc.MemberName,
                            tc.FilterOperator,
                            convertedValue,
                            false);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }

                if (tc.ExceptionExpected && exception == null)
                {
                    failures.Add(new AssertFailedException(string.Format("Expected an exception, but none was caught. MemberName: {0}; Operator: {1}; Value: {2}.", tc.MemberName, tc.FilterOperator, tc.Value)));
                }
                else if (!tc.ExceptionExpected && exception != null)
                {
                    failures.Add(new AssertFailedException(string.Format("Unexpected exception. MemberName: {0}; Operator: {1}; Value: {2}.", tc.MemberName, tc.FilterOperator, tc.Value), exception));
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail(string.Format("{0} failures! ----- {1}", failures.Count, string.Join(" ----- ", failures.Select(e => e.ToString()).ToArray())));
            }
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that a FilterDescriptor is ignored when the Value matches the IgnoredValue")]
        public void FilterDescriptorIgnoredValue()
        {
            CityData cityData = new CityData();

            this.LoadCities(0, false);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Start with a String property
                this._dds.FilterDescriptors.Add(new FilterDescriptor());
                this._dds.FilterDescriptors[0].PropertyPath = "Name";
                this._dds.FilterDescriptors[0].Operator = FilterOperator.IsEqualTo;

                // Make sure null is not ignored by default
                this._dds.FilterDescriptors[0].Value = null;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Where(c => c.Name == null).Count(), this._view.Count,
                    "Only cities with null names should be loaded.");

                // Make sure string.Empty is not ignored by default
                this._dds.FilterDescriptors[0].Value = string.Empty;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Where(c => c.Name == string.Empty).Count(), this._view.Count,
                    "Only cities with empty names should be loaded.");

                // Make sure FilterDescriptor.DefaultIgnoredValue is ignored by default
                this._dds.FilterDescriptors[0].Value = FilterDescriptor.DefaultIgnoredValue;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Change to an Int32 property
                this._dds.FilterDescriptors[0].PropertyPath = "ZoneID";

                // Make sure a valid value is not ignored by default
                this._dds.FilterDescriptors[0].Value = 1;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Where(c => c.ZoneID == 1).Count(), this._view.Count,
                    "Only cities in Zone 1 should be loaded.");

                // Make sure a matching value is ignored; 1=1
                this._dds.FilterDescriptors[0].IgnoredValue = 1;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Make sure a convertable value is ignored; "1"=1 
                this._dds.FilterDescriptors[0].Value = "1"; 
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Make sure a matching convertable value is ignored; "1"="1"
                this._dds.FilterDescriptors[0].IgnoredValue = "1"; 
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Make sure a convertable value is ignored; 1="1"
                this._dds.FilterDescriptors[0].Value = 1;
                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Verifies that a FilterDescriptor is ignored when the Value matches the IgnoredValue when AutoLoad is true")]
        public void FilterDescriptorIgnoredValueWithAutoLoad()
        {
            CityData cityData = new CityData();

            this.LoadDomainDataSourceControl();
            this.LoadCities(0, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Start with a String property
                this._dds.FilterDescriptors.Add(new FilterDescriptor() { PropertyPath = "Name" });
                this._dds.FilterDescriptors[0].Operator = FilterOperator.IsEqualTo;

                // Make sure null is not ignored by default
                this._dds.FilterDescriptors[0].Value = null;
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Where(c => c.Name == null).Count(), this._view.Count,
                    "Only cities with null names should be loaded.");

                // Change to an Int32 property
                this._dds.FilterDescriptors[0].PropertyPath = "ZoneID";

                // Make sure a valid value is not ignored by default
                this._dds.FilterDescriptors[0].Value = 1;
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Where(c => c.ZoneID == 1).Count(), this._view.Count,
                    "Only cities in Zone 1 should be loaded.");

                // Make sure a matching value is ignored; 1=1
                this._dds.FilterDescriptors[0].IgnoredValue = 1;
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");

                // Make sure a non-convertable value does not autoload
                this._dds.FilterDescriptors[0].Value = string.Empty;
            });

            this.AssertNoLoadingData();

            EnqueueCallback(() =>
            {
                // Make sure a matching non-convertable value is ignored
                this._dds.FilterDescriptors[0].IgnoredValue = string.Empty;
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                Assert.AreEqual(cityData.Cities.Count, this._view.Count,
                    "All cities should be loaded.");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(731814)]
        [Description("Verifies that a FilterDescriptor is ignored when the IgnoredValue is updated to match the Value")]
        public void FilterDescriptorIgnoredValueUpdatingFilterDescriptor()
        {
            // Ensures the full code path will be hit, because CheckFilterDescriptor
            // exits early if the control is not loaded.
            this.LoadDomainDataSourceControl();

            string impossibleValue = Guid.NewGuid().ToString();
            FilterDescriptor filter = new FilterDescriptor("Name", FilterOperator.IsEqualTo, impossibleValue);

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();

                // Add a filter descriptor that, if used, would prevent any matches
                this._dds.FilterDescriptors.Add(filter);
                // Add an IgnoreValue so the filter will be ignored
                filter.IgnoredValue = impossibleValue;

                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                int allRecordsCount = new CityData().Cities.Count;
                Assert.AreEqual<int>(allRecordsCount, this._view.Count, "All records should have been loaded");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(731814)]
        [Description("Verifies that a FilterDescriptor is ignored when the FilterDescriptorCollection is changed to have an existing FilterDescriptor but now with an IgnoredValue")]
        public void FilterDescriptorIgnoredValueUpdatingFilterDescriptorCollection()
        {
            // Ensures the full code path will be hit, because CheckFilterDescriptor
            // exits early if the control is not loaded.
            this.LoadDomainDataSourceControl();

            string impossibleValue = Guid.NewGuid().ToString();
            FilterDescriptor filter = new FilterDescriptor("Name", FilterOperator.IsEqualTo, impossibleValue);

            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();

                // Add a filter descriptor that, if used, would prevent any matches
                this._dds.FilterDescriptors.Add(filter);
                // Remove, update, and re-add the filter
                // This hits the FilterDescriptors_CollectionChanged code path.
                this._dds.FilterDescriptors.Remove(filter);
                filter.IgnoredValue = impossibleValue;
                this._dds.FilterDescriptors.Add(filter);

                this._dds.Load();
            });

            this.AssertLoadedData();

            EnqueueCallback(() =>
            {
                int allRecordsCount = new CityData().Cities.Count;
                Assert.AreEqual<int>(allRecordsCount, this._view.Count, "All records should have been loaded");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Filtering an enum property using an enum value")]
        public void FilterEnumByEnumValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStates";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.FilterDescriptors.Add(new FilterDescriptor("TimeZone", FilterOperator.IsEqualTo, TimeZone.Eastern));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<State>().All(s => s.TimeZone == TimeZone.Eastern), "There should not be any states with time zone other than Eastern");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Filtering an enum property using a numeric value")]
        public void FilterEnumByNumericValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStates";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.FilterDescriptors.Add(new FilterDescriptor("TimeZone", FilterOperator.IsEqualTo, (int)TimeZone.Eastern));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<State>().All(s => s.TimeZone == TimeZone.Eastern), "There should not be any states with time zone other than Eastern");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Filtering a numeric property using an enum value")]
        public void FilterNumericByEnumValue()
        {
            EnqueueCallback(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.FilterDescriptors.Add(new FilterDescriptor("ZoneID", FilterOperator.IsEqualTo, TimeZone.Eastern));
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                Assert.IsTrue(this._view.Cast<City>().All(c => c.ZoneID == (int)TimeZone.Eastern), "There should not be any cities with a ZoneID other than the integer value of TimeZone.Eastern");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(792200)]
        [WorkItem(809074)]
        [Description("Filtering an enum property using an enum value from a different enum is expected to fail")]
        public void FilterEnumByDifferentEnumValueFails()
        {
            this.AssertExpectedLoadError(() =>
            {
                this._dds.AutoLoad = false;
                this._dds.QueryName = "GetStates";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.FilterDescriptors.Add(new FilterDescriptor("TimeZone", FilterOperator.IsEqualTo, ShippingZone.Eastern));
                this._dds.Load();
            });

            EnqueueTestComplete();
        }

        #endregion Filtering

        #region Filter Scenario Tests

        [TestClass]
        public class FilterScenarioTests : DomainDataSourceTestBase
        {
            #region Constants

            private const Byte KnownByteValue = (Byte)123;
            private const SByte KnownSByteValue = -(SByte)123;
            private const Int16 KnownInt16Value = -(Int16)123;
            private const UInt16 KnownUInt16Value = (UInt16)123;
            private const Int32 KnownInt32Value = -(Int32)123;
            private const UInt32 KnownUInt32Value = (UInt32)123;
            private const Int64 KnownInt64Value = -(Int64)123;
            private const UInt64 KnownUInt64Value = (UInt64)123;
            private const Decimal KnownDecimalValue = -(Decimal)123.123;
            private const Single KnownSingleValue = -(Single)123.123;
            private const Double KnownDoubleValue = -(Double)123.123;
            private const Char KnownCharValue = (Char)123;
            private const String KnownStringValue = "some other string value";
            private const String KnownStringStartValue = "some";
            private const String KnownStringEndValue = "value";
            private readonly DateTime KnownDateTimeValue = new DateTime(2008, 09, 03);
            private readonly DateTimeOffset KnownDateTimeOffsetValue = new DateTimeOffset(new DateTime(2008, 09, 03), new TimeSpan(10, 0, 0));
            private readonly TimeSpan KnownTimeSpanValue = new TimeSpan(123);
            private const TestEnum KnownEnumValue = TestEnum.Value2;
            private readonly Uri KnownUriValue = new Uri("http://localhost");
            private readonly Guid KnownGuidValue = new Guid("12345678-1234-1234-1234-123456789012");
            private readonly XElement KnownXElementValue = XElement.Parse("<someElement>element text</someElement>");

            #endregion Constants

            #region Helpers

            /// <summary>
            /// Class to easily create filter scenario tests.
            /// </summary>
            public static class FilterScenario
            {
                /// <summary>
                /// Create a new filter scenario test case that is expected to succeed.
                /// </summary>
                /// <param name="filter">The <see cref="FilterDescriptor"/> to use for the test.</param>
                /// <param name="assertions">
                /// The assertions to run after loading the results, to verify the test case, given the
                /// view of results.
                /// </param>
                public static FilterScenario<T> Success<T>(FilterDescriptor filter, Action<IEnumerable<T>> assertions)
                {
                    return new FilterScenario<T>
                    {
                        Filter = filter,
                        ExpectSuccess = true,
                        SuccessAssertions = assertions
                    };
                }

                /// <summary>
                /// Create a new filter scenario test case.
                /// </summary>
                /// <param name="propertyPath">The property path to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterOperator">The operator to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterValue">The value to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="assertions">
                /// The assertions to run after loading the results, to verify the test case, given the
                /// view of results.
                /// </param>
                public static FilterScenario<T> Success<T>(string propertyPath, FilterOperator filterOperator, object filterValue, Action<IEnumerable<T>> assertions)
                {
                    return Success<T>(new FilterDescriptor(propertyPath, filterOperator, filterValue), assertions);
                }

                /// <summary>
                /// Create a new filter scenario test case.
                /// </summary>
                /// <param name="propertyPath">The property path to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterOperator">The operator to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterValue">The value to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="trueAssertion">
                /// A single assertion that is expected to be <c>true</c>, given the results of the load.
                /// </param>
                public static FilterScenario<T> Success<T>(string propertyPath, FilterOperator filterOperator, object filterValue, Func<IEnumerable<T>, bool> trueAssertion)
                {
                    return Success<T>(new FilterDescriptor(propertyPath, filterOperator, filterValue), v => Assert.IsTrue(trueAssertion(v)));
                }

                /// <summary>
                /// Create a new filter scenario test case that is expected to cause a load error.
                /// </summary>
                /// <param name="filter">The <see cref="FilterDescriptor"/> to use for the test.</param>
                /// <param name="errorAssertions">
                /// The assertions to run after loading the results and getting the load error.
                /// </param>
                public static FilterScenario<T> Failure<T>(FilterDescriptor filter, Action<LoadedDataEventArgs> assertions)
                {
                    return new FilterScenario<T>
                    {
                        Filter = filter,
                        ExpectSuccess = false,
                        ErrorAssertions = assertions
                    };
                }

                /// <summary>
                /// Create a new filter scenario test case that is expected to cause a load error.
                /// </summary>
                /// <param name="propertyPath">The property path to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterOperator">The operator to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterValue">The value to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="assertions">
                /// The assertions to run after loading the results and getting the load error.
                /// </param>
                public static FilterScenario<T> Failure<T>(string propertyPath, FilterOperator filterOperator, object filterValue, Action<LoadedDataEventArgs> assertions)
                {
                    return Failure<T>(new FilterDescriptor(propertyPath, filterOperator, filterValue), assertions);
                }

                /// <summary>
                /// Create a new filter scenario test case that is expected to cause a load error.
                /// </summary>
                /// <param name="propertyPath">The property path to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterOperator">The operator to use for the <see cref="FilterDescriptor"/>.</param>
                /// <param name="filterValue">The value to use for the <see cref="FilterDescriptor"/>.</param>
                public static FilterScenario<T> Failure<T, TProperty>(string propertyPath, FilterOperator filterOperator, object filterValue)
                {
                    return Failure<T>(new FilterDescriptor(propertyPath, filterOperator, filterValue), e =>
                        {
                            Assert.IsInstanceOfType(e.Error, typeof(InvalidOperationException), e.Error.Message);
                            Assert.IsInstanceOfType(e.Error.InnerException, typeof(NotSupportedException), "InnerException of " + e.Error.Message);
                            Assert.AreEqual<string>(string.Format(
                                CultureInfo.InvariantCulture,
                                DomainDataSourceResources.FilterNotSupported,
                                propertyPath,
                                System.Windows.Common.TypeHelper.GetTypeName(typeof(T)),
                                System.Windows.Common.TypeHelper.GetTypeName(typeof(TProperty)),
                                filterOperator), e.Error.InnerException.Message);
                        });
                }
            }

            /// <summary>
            /// A utility class for defining filter scenario test cases.
            /// </summary>
            public class FilterScenario<T>
            {
                public FilterScenario()
                {
                }

                /// <summary>
                /// The <see cref="FilterDescriptor"/> to use for the test.
                /// </summary>
                public FilterDescriptor Filter { get; set; }

                /// <summary>
                /// Whether or not to expect a successful load.
                /// </summary>
                public bool ExpectSuccess { get; set; }

                /// <summary>
                /// The assertions to run after loading the results, to verify the test case, given the
                /// view of results.
                /// </summary>
                public Action<IEnumerable<T>> SuccessAssertions { get; set; }

                /// <summary>
                /// The assertions to run after a load error occurs.
                /// </summary>
                public Action<LoadedDataEventArgs> ErrorAssertions { get; set; }
            }

            /// <summary>
            /// Tests one or more <see cref="FilterScenario{T}"/>s for the specified query of <typeparam name="TEntity"/>.
            /// </summary>
            /// <param name="domainContext">The domain context to use for the test.</param>
            /// <param name="queryName">The <see cref="DomainDataSource.QueryName"/> to use for the test.</param>
            /// <param name="scenarios">The scenarios to test.</param>
            private void TestScenarios<TEntity>(DomainContext domainContext, string queryName, params FilterScenario<TEntity>[] scenarios)
                where TEntity : OpenRiaServices.DomainServices.Client.Entity
            {
                foreach (FilterScenario<TEntity> scenario in scenarios)
                {
                    Action invokeLoad = () =>
                    {
                        this.ResetLoadState();
                        this._dds.AutoLoad = false;
                        this._dds.DomainContext = domainContext;
                        this._dds.QueryName = queryName;
                        this._dds.FilterDescriptors.Add(scenario.Filter);
                        this._dds.Load();
                    };

                    if (scenario.ExpectSuccess)
                    {
                        EnqueueCallback(invokeLoad);
                        this.AssertLoadingData();

                        EnqueueCallback(() =>
                        {
                            this.ResetLoadState();
                            scenario.SuccessAssertions(this._view.Cast<TEntity>());
                        });
                    }
                    else
                    {
                        this.AssertExpectedLoadError(invokeLoad);

                        EnqueueCallback(() =>
                        {
                            scenario.ErrorAssertions(this._ddsLoadedDataEventArgs);
                        });
                    }
                }

                EnqueueTestComplete();
            }

            /// <summary>
            /// Tests one or more <see cref="FilterScenario{T}"/>s for a <see cref="MixedType"/> query.
            /// </summary>
            /// <param name="scenarios">The scenarios to test.</param>
            private void TestScenarios(params FilterScenario<MixedType>[] scenarios)
            {
                this.TestScenarios(new TestProvider_Scenarios(), "GetMixedTypeSuperset", scenarios);
            }

            #endregion Helpers

            #region Boolean

            [Asynchronous, TestMethod]
            public void BooleanIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsLessThan, true, v => v.All(m => m.BooleanProp.CompareTo(true) < 0)));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsLessThanOrEqualTo, true, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.BooleanProp.CompareTo(true) > 0), "Not Greater");
                        Assert.IsTrue(v.Any(m => m.BooleanProp.CompareTo(true) < 0), "Less");
                        Assert.IsTrue(v.Any(m => m.BooleanProp.CompareTo(true) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsEqualTo, true, v => v.All(m => m.BooleanProp.CompareTo(true) == 0)));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsNotEqualTo, true, v => v.All(m => m.BooleanProp.CompareTo(true) != 0)));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsGreaterThanOrEqualTo, false, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.BooleanProp.CompareTo(false) < 0), "Not Less");
                        Assert.IsTrue(v.Any(m => m.BooleanProp.CompareTo(false) > 0), "Greater");
                        Assert.IsTrue(v.Any(m => m.BooleanProp.CompareTo(false) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("BooleanProp", FilterOperator.IsGreaterThan, false, v => v.All(m => m.BooleanProp.CompareTo(false) > 0)));
            }

            [Asynchronous, TestMethod]
            public void BooleanStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Boolean>("BooleanProp", FilterOperator.StartsWith, false));
            }

            [Asynchronous, TestMethod]
            public void BooleanEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Boolean>("BooleanProp", FilterOperator.EndsWith, false));
            }

            [Asynchronous, TestMethod]
            public void BooleanContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Boolean>("BooleanProp", FilterOperator.Contains, false));
            }

            [Asynchronous, TestMethod]
            public void BooleanIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Boolean>("BooleanProp", FilterOperator.IsContainedIn, false));
            }

            #endregion Boolean

            #region Byte

            [Asynchronous, TestMethod]
            public void ByteIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsLessThan, KnownByteValue, v => v.All(m => m.ByteProp.CompareTo(KnownByteValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void ByteIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsLessThanOrEqualTo, KnownByteValue, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) > 0), "Not Greater");
                        Assert.IsTrue(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) < 0), "Less");
                        Assert.IsTrue(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void ByteIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsEqualTo, KnownByteValue, v => v.All(m => m.ByteProp.CompareTo(KnownByteValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void ByteIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsNotEqualTo, KnownByteValue, v => v.All(m => m.ByteProp.CompareTo(KnownByteValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void ByteIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsGreaterThanOrEqualTo, KnownByteValue, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) < 0), "Not Less");
                        Assert.IsTrue(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) > 0), "Greater");
                        Assert.IsTrue(v.Any(m => m.ByteProp.CompareTo(KnownByteValue) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void ByteIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("ByteProp", FilterOperator.IsGreaterThan, KnownByteValue, v => v.All(m => m.ByteProp.CompareTo(KnownByteValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void ByteStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Byte>("ByteProp", FilterOperator.StartsWith, KnownByteValue));
            }

            [Asynchronous, TestMethod]
            public void ByteEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Byte>("ByteProp", FilterOperator.EndsWith, KnownByteValue));
            }

            [Asynchronous, TestMethod]
            public void ByteContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Byte>("ByteProp", FilterOperator.Contains, KnownByteValue));
            }

            [Asynchronous, TestMethod]
            public void ByteIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Byte>("ByteProp", FilterOperator.IsContainedIn, KnownByteValue));
            }

            #endregion Byte

            #region SByte

            [Asynchronous, TestMethod]
            public void SByteIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsLessThan, KnownSByteValue, v => v.All(m => m.SByteProp.CompareTo(KnownSByteValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void SByteIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsLessThanOrEqualTo, KnownSByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void SByteIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsEqualTo, KnownSByteValue, v => v.All(m => m.SByteProp.CompareTo(KnownSByteValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void SByteIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsNotEqualTo, KnownSByteValue, v => v.All(m => m.SByteProp.CompareTo(KnownSByteValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void SByteIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsGreaterThanOrEqualTo, KnownSByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.SByteProp.CompareTo(KnownSByteValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void SByteIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SByteProp", FilterOperator.IsGreaterThan, KnownSByteValue, v => v.All(m => m.SByteProp.CompareTo(KnownSByteValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void SByteStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, SByte>("SByteProp", FilterOperator.StartsWith, KnownSByteValue));
            }

            [Asynchronous, TestMethod]
            public void SByteEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, SByte>("SByteProp", FilterOperator.EndsWith, KnownSByteValue));
            }

            [Asynchronous, TestMethod]
            public void SByteContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, SByte>("SByteProp", FilterOperator.Contains, KnownSByteValue));
            }

            [Asynchronous, TestMethod]
            public void SByteIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, SByte>("SByteProp", FilterOperator.IsContainedIn, KnownSByteValue));
            }

            #endregion SByte

            #region Int16

            [Asynchronous, TestMethod]
            public void Int16IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsLessThan, KnownInt16Value, v => v.All(m => m.Int16Prop.CompareTo(KnownInt16Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void Int16IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt16Value, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) > 0), "Not Greater");
                        Assert.IsTrue(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) < 0), "Less");
                        Assert.IsTrue(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void Int16IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsEqualTo, KnownInt16Value, v => v.All(m => m.Int16Prop.CompareTo(KnownInt16Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void Int16IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsNotEqualTo, KnownInt16Value, v => v.All(m => m.Int16Prop.CompareTo(KnownInt16Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void Int16IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt16Value, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) < 0), "Not Less");
                        Assert.IsTrue(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) > 0), "Greater");
                        Assert.IsTrue(v.Any(m => m.Int16Prop.CompareTo(KnownInt16Value) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void Int16IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int16Prop", FilterOperator.IsGreaterThan, KnownInt16Value, v => v.All(m => m.Int16Prop.CompareTo(KnownInt16Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void Int16StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int16>("Int16Prop", FilterOperator.StartsWith, KnownInt16Value));
            }

            [Asynchronous, TestMethod]
            public void Int16EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int16>("Int16Prop", FilterOperator.EndsWith, KnownInt16Value));
            }

            [Asynchronous, TestMethod]
            public void Int16Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int16>("Int16Prop", FilterOperator.Contains, KnownInt16Value));
            }

            [Asynchronous, TestMethod]
            public void Int16IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int16>("Int16Prop", FilterOperator.IsContainedIn, KnownInt16Value));
            }

            #endregion Int16

            #region UInt16

            [Asynchronous, TestMethod]
            public void UInt16IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsLessThan, KnownUInt16Value, v => v.All(m => m.UInt16Prop.CompareTo(KnownUInt16Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsEqualTo, KnownUInt16Value, v => v.All(m => m.UInt16Prop.CompareTo(KnownUInt16Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsNotEqualTo, KnownUInt16Value, v => v.All(m => m.UInt16Prop.CompareTo(KnownUInt16Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.UInt16Prop.CompareTo(KnownUInt16Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt16Prop", FilterOperator.IsGreaterThan, KnownUInt16Value, v => v.All(m => m.UInt16Prop.CompareTo(KnownUInt16Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt16StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt16>("UInt16Prop", FilterOperator.StartsWith, KnownUInt16Value));
            }

            [Asynchronous, TestMethod]
            public void UInt16EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt16>("UInt16Prop", FilterOperator.EndsWith, KnownUInt16Value));
            }

            [Asynchronous, TestMethod]
            public void UInt16Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt16>("UInt16Prop", FilterOperator.Contains, KnownUInt16Value));
            }

            [Asynchronous, TestMethod]
            public void UInt16IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt16>("UInt16Prop", FilterOperator.IsContainedIn, KnownUInt16Value));
            }

            #endregion UInt16

            #region Int32

            [Asynchronous, TestMethod]
            public void Int32IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsLessThan, KnownInt32Value, v => v.All(m => m.Int32Prop.CompareTo(KnownInt32Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void Int32IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void Int32IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsEqualTo, KnownInt32Value, v => v.All(m => m.Int32Prop.CompareTo(KnownInt32Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void Int32IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsNotEqualTo, KnownInt32Value, v => v.All(m => m.Int32Prop.CompareTo(KnownInt32Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void Int32IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.Int32Prop.CompareTo(KnownInt32Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void Int32IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int32Prop", FilterOperator.IsGreaterThan, KnownInt32Value, v => v.All(m => m.Int32Prop.CompareTo(KnownInt32Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void Int32StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32>("Int32Prop", FilterOperator.StartsWith, KnownInt32Value));
            }

            [Asynchronous, TestMethod]
            public void Int32EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32>("Int32Prop", FilterOperator.EndsWith, KnownInt32Value));
            }

            [Asynchronous, TestMethod]
            public void Int32Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32>("Int32Prop", FilterOperator.Contains, KnownInt32Value));
            }

            [Asynchronous, TestMethod]
            public void Int32IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32>("Int32Prop", FilterOperator.IsContainedIn, KnownInt32Value));
            }

            #endregion Int32

            #region UInt32

            [Asynchronous, TestMethod]
            public void UInt32IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsLessThan, KnownUInt32Value, v => v.All(m => m.UInt32Prop.CompareTo(KnownUInt32Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsEqualTo, KnownUInt32Value, v => v.All(m => m.UInt32Prop.CompareTo(KnownUInt32Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsNotEqualTo, KnownUInt32Value, v => v.All(m => m.UInt32Prop.CompareTo(KnownUInt32Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.UInt32Prop.CompareTo(KnownUInt32Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt32Prop", FilterOperator.IsGreaterThan, KnownUInt32Value, v => v.All(m => m.UInt32Prop.CompareTo(KnownUInt32Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt32StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt32>("UInt32Prop", FilterOperator.StartsWith, KnownUInt32Value));
            }

            [Asynchronous, TestMethod]
            public void UInt32EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt32>("UInt32Prop", FilterOperator.EndsWith, KnownUInt32Value));
            }

            [Asynchronous, TestMethod]
            public void UInt32Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt32>("UInt32Prop", FilterOperator.Contains, KnownUInt32Value));
            }

            [Asynchronous, TestMethod]
            public void UInt32IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt32>("UInt32Prop", FilterOperator.IsContainedIn, KnownUInt32Value));
            }

            #endregion UInt32

            #region Int64

            [Asynchronous, TestMethod]
            public void Int64IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsLessThan, KnownInt64Value, v => v.All(m => m.Int64Prop.CompareTo(KnownInt64Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void Int64IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void Int64IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsEqualTo, KnownInt64Value, v => v.All(m => m.Int64Prop.CompareTo(KnownInt64Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void Int64IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsNotEqualTo, KnownInt64Value, v => v.All(m => m.Int64Prop.CompareTo(KnownInt64Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void Int64IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.Int64Prop.CompareTo(KnownInt64Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void Int64IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("Int64Prop", FilterOperator.IsGreaterThan, KnownInt64Value, v => v.All(m => m.Int64Prop.CompareTo(KnownInt64Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void Int64StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int64>("Int64Prop", FilterOperator.StartsWith, KnownInt64Value));
            }

            [Asynchronous, TestMethod]
            public void Int64EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int64>("Int64Prop", FilterOperator.EndsWith, KnownInt64Value));
            }

            [Asynchronous, TestMethod]
            public void Int64Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int64>("Int64Prop", FilterOperator.Contains, KnownInt64Value));
            }

            [Asynchronous, TestMethod]
            public void Int64IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int64>("Int64Prop", FilterOperator.IsContainedIn, KnownInt64Value));
            }

            #endregion Int64

            #region UInt64

            [Asynchronous, TestMethod]
            public void UInt64IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsLessThan, KnownUInt64Value, v => v.All(m => m.UInt64Prop.CompareTo(KnownUInt64Value) < 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsEqualTo, KnownUInt64Value, v => v.All(m => m.UInt64Prop.CompareTo(KnownUInt64Value) == 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsNotEqualTo, KnownUInt64Value, v => v.All(m => m.UInt64Prop.CompareTo(KnownUInt64Value) != 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.UInt64Prop.CompareTo(KnownUInt64Value) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UInt64Prop", FilterOperator.IsGreaterThan, KnownUInt64Value, v => v.All(m => m.UInt64Prop.CompareTo(KnownUInt64Value) > 0)));
            }

            [Asynchronous, TestMethod]
            public void UInt64StartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt64>("UInt64Prop", FilterOperator.StartsWith, KnownUInt64Value));
            }

            [Asynchronous, TestMethod]
            public void UInt64EndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt64>("UInt64Prop", FilterOperator.EndsWith, KnownUInt64Value));
            }

            [Asynchronous, TestMethod]
            public void UInt64Contains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt64>("UInt64Prop", FilterOperator.Contains, KnownUInt64Value));
            }

            [Asynchronous, TestMethod]
            public void UInt64IsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, UInt64>("UInt64Prop", FilterOperator.IsContainedIn, KnownUInt64Value));
            }

            #endregion UInt64

            #region Decimal

            [Asynchronous, TestMethod]
            public void DecimalIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsLessThan, KnownDecimalValue, v => v.All(m => m.DecimalProp.CompareTo(KnownDecimalValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsLessThanOrEqualTo, KnownDecimalValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsEqualTo, KnownDecimalValue, v => v.All(m => m.DecimalProp.CompareTo(KnownDecimalValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsNotEqualTo, KnownDecimalValue, v => v.All(m => m.DecimalProp.CompareTo(KnownDecimalValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDecimalValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.DecimalProp.CompareTo(KnownDecimalValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DecimalProp", FilterOperator.IsGreaterThan, KnownDecimalValue, v => v.All(m => m.DecimalProp.CompareTo(KnownDecimalValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void DecimalStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Decimal>("DecimalProp", FilterOperator.StartsWith, KnownDecimalValue));
            }

            [Asynchronous, TestMethod]
            public void DecimalEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Decimal>("DecimalProp", FilterOperator.EndsWith, KnownDecimalValue));
            }

            [Asynchronous, TestMethod]
            public void DecimalContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Decimal>("DecimalProp", FilterOperator.Contains, KnownDecimalValue));
            }

            [Asynchronous, TestMethod]
            public void DecimalIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Decimal>("DecimalProp", FilterOperator.IsContainedIn, KnownDecimalValue));
            }

            #endregion Decimal

            #region Single

            [Asynchronous, TestMethod]
            public void SingleIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsLessThan, KnownSingleValue, v => v.All(m => m.SingleProp.CompareTo(KnownSingleValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void SingleIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsLessThanOrEqualTo, KnownSingleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void SingleIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsEqualTo, KnownSingleValue, v => v.All(m => m.SingleProp.CompareTo(KnownSingleValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void SingleIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsNotEqualTo, KnownSingleValue, v => v.All(m => m.SingleProp.CompareTo(KnownSingleValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void SingleIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsGreaterThanOrEqualTo, KnownSingleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.SingleProp.CompareTo(KnownSingleValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void SingleIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("SingleProp", FilterOperator.IsGreaterThan, KnownSingleValue, v => v.All(m => m.SingleProp.CompareTo(KnownSingleValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void SingleStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Single>("SingleProp", FilterOperator.StartsWith, KnownSingleValue));
            }

            [Asynchronous, TestMethod]
            public void SingleEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Single>("SingleProp", FilterOperator.EndsWith, KnownSingleValue));
            }

            [Asynchronous, TestMethod]
            public void SingleContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Single>("SingleProp", FilterOperator.Contains, KnownSingleValue));
            }

            [Asynchronous, TestMethod]
            public void SingleIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Single>("SingleProp", FilterOperator.IsContainedIn, KnownSingleValue));
            }

            #endregion Single

            #region Double

            [Asynchronous, TestMethod]
            public void DoubleIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsLessThan, KnownDoubleValue, v => v.All(m => m.DoubleProp.CompareTo(KnownDoubleValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsLessThanOrEqualTo, KnownDoubleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsEqualTo, KnownDoubleValue, v => v.All(m => m.DoubleProp.CompareTo(KnownDoubleValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsNotEqualTo, KnownDoubleValue, v => v.All(m => m.DoubleProp.CompareTo(KnownDoubleValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDoubleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.DoubleProp.CompareTo(KnownDoubleValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DoubleProp", FilterOperator.IsGreaterThan, KnownDoubleValue, v => v.All(m => m.DoubleProp.CompareTo(KnownDoubleValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void DoubleStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Double>("DoubleProp", FilterOperator.StartsWith, KnownDoubleValue));
            }

            [Asynchronous, TestMethod]
            public void DoubleEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Double>("DoubleProp", FilterOperator.EndsWith, KnownDoubleValue));
            }

            [Asynchronous, TestMethod]
            public void DoubleContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Double>("DoubleProp", FilterOperator.Contains, KnownDoubleValue));
            }

            [Asynchronous, TestMethod]
            public void DoubleIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Double>("DoubleProp", FilterOperator.IsContainedIn, KnownDoubleValue));
            }

            #endregion Double

            #region Char

            [Asynchronous, TestMethod]
            public void CharIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsLessThan, KnownCharValue, v => v.All(m => m.CharProp.CompareTo(KnownCharValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void CharIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsLessThanOrEqualTo, KnownCharValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.CharProp.CompareTo(KnownCharValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.CharProp.CompareTo(KnownCharValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.CharProp.CompareTo(KnownCharValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void CharIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsEqualTo, KnownCharValue, v => v.All(m => m.CharProp.CompareTo(KnownCharValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void CharIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsNotEqualTo, KnownCharValue, v => v.All(m => m.CharProp.CompareTo(KnownCharValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void CharIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsGreaterThanOrEqualTo, KnownCharValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.CharProp.CompareTo(KnownCharValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.CharProp.CompareTo(KnownCharValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.CharProp.CompareTo(KnownCharValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void CharIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("CharProp", FilterOperator.IsGreaterThan, KnownCharValue, v => v.All(m => m.CharProp.CompareTo(KnownCharValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void CharStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Char>("CharProp", FilterOperator.StartsWith, KnownCharValue));
            }

            [Asynchronous, TestMethod]
            public void CharEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Char>("CharProp", FilterOperator.EndsWith, KnownCharValue));
            }

            [Asynchronous, TestMethod]
            public void CharContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Char>("CharProp", FilterOperator.Contains, KnownCharValue));
            }

            [Asynchronous, TestMethod]
            public void CharIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Char>("CharProp", FilterOperator.IsContainedIn, KnownCharValue));
            }

            #endregion Char

            #region String

            [Asynchronous, TestMethod]
            public void StringIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsLessThan, KnownStringValue, v => v.All(m => m.StringProp.CompareTo(KnownStringValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void StringIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsLessThanOrEqualTo, KnownStringValue, v =>
                    {
                        Assert.IsFalse(v.Any(m => m.StringProp.CompareTo(KnownStringValue) > 0), "Not Greater");
                        Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringValue) < 0), "Less");
                        Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringValue) == 0), "Equal");
                    }));
            }

            [Asynchronous, TestMethod]
            public void StringIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsEqualTo, KnownStringValue, v => v.All(m => m.StringProp.CompareTo(KnownStringValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void StringIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsNotEqualTo, KnownStringValue, v => v.All(m => m.StringProp.CompareTo(KnownStringValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void StringIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsGreaterThanOrEqualTo, KnownStringValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.StringProp.CompareTo(KnownStringValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void StringIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsGreaterThan, KnownStringValue, v => v.All(m => m.StringProp.CompareTo(KnownStringValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void StringStartsWith()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.StartsWith, KnownStringStartValue, v =>
                    {
                        Assert.IsTrue(v.All(m => m.StringProp.Contains(KnownStringStartValue)), "Contains");
                        Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringStartValue) > 0), "Greater");
                    }));
            }

            [Asynchronous, TestMethod]
            public void StringEndsWith()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.EndsWith, KnownStringEndValue, v =>
                {
                    Assert.IsTrue(v.All(m => m.StringProp.Contains(KnownStringStartValue)), "Contains");
                    Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringStartValue) > 0), "Greater");
                }));
            }

            [Asynchronous, TestMethod]
            public void StringContains()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.Contains, KnownStringStartValue, v =>
                    {
                        Assert.IsTrue(v.All(m => m.StringProp.Contains(KnownStringStartValue)), "Contains Start Value");
                        Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringStartValue) > 0), "Greater Than Start Value");
                    }));

                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.Contains, KnownStringEndValue, v =>
                {
                    Assert.IsTrue(v.All(m => m.StringProp.Contains(KnownStringStartValue)), "Contains End Value");
                    Assert.IsTrue(v.Any(m => m.StringProp.CompareTo(KnownStringStartValue) > 0), "Greater Than End Value");
                }));
            }

            [Asynchronous, TestMethod]
            public void StringIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("StringProp", FilterOperator.IsContainedIn, KnownStringValue, v =>
                {
                    Assert.IsTrue(v.All(m => KnownStringValue.Contains(m.StringProp)), "Contains");
                    Assert.IsTrue(v.Any(m => KnownStringValue.CompareTo(m.StringProp) > 0), "Greater");
                }));
            }

            #endregion String

            #region DateTime

            [Asynchronous, TestMethod]
            public void DateTimeIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsLessThan, KnownDateTimeValue, v => v.All(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsLessThanOrEqualTo, KnownDateTimeValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsEqualTo, KnownDateTimeValue, v => v.All(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsNotEqualTo, KnownDateTimeValue, v => v.All(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDateTimeValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeProp", FilterOperator.IsGreaterThan, KnownDateTimeValue, v => v.All(m => m.DateTimeProp.CompareTo(KnownDateTimeValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTime>("DateTimeProp", FilterOperator.StartsWith, KnownDateTimeValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTime>("DateTimeProp", FilterOperator.EndsWith, KnownDateTimeValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTime>("DateTimeProp", FilterOperator.Contains, KnownDateTimeValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTime>("DateTimeProp", FilterOperator.IsContainedIn, KnownDateTimeValue));
            }

            #endregion DateTime

            #region DateTimeOffset

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsLessThan, KnownDateTimeOffsetValue, v => v.All(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsLessThanOrEqualTo, KnownDateTimeOffsetValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsEqualTo, KnownDateTimeOffsetValue, v => v.All(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsNotEqualTo, KnownDateTimeOffsetValue, v => v.All(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDateTimeOffsetValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("DateTimeOffsetProp", FilterOperator.IsGreaterThan, KnownDateTimeOffsetValue, v => v.All(m => m.DateTimeOffsetProp.CompareTo(KnownDateTimeOffsetValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTimeOffset>("DateTimeOffsetProp", FilterOperator.StartsWith, KnownDateTimeOffsetValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTimeOffset>("DateTimeOffsetProp", FilterOperator.EndsWith, KnownDateTimeOffsetValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTimeOffset>("DateTimeOffsetProp", FilterOperator.Contains, KnownDateTimeOffsetValue));
            }

            [Asynchronous, TestMethod]
            public void DateTimeOffsetIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, DateTimeOffset>("DateTimeOffsetProp", FilterOperator.IsContainedIn, KnownDateTimeOffsetValue));
            }

            #endregion DateTimeOffset

            #region TimeSpan

            [Asynchronous, TestMethod]
            public void TimeSpanIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsLessThan, KnownTimeSpanValue, v => v.All(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsLessThanOrEqualTo, KnownTimeSpanValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsEqualTo, KnownTimeSpanValue, v => v.All(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsNotEqualTo, KnownTimeSpanValue, v => v.All(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsGreaterThanOrEqualTo, KnownTimeSpanValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("TimeSpanProp", FilterOperator.IsGreaterThan, KnownTimeSpanValue, v => v.All(m => m.TimeSpanProp.CompareTo(KnownTimeSpanValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TimeSpan>("TimeSpanProp", FilterOperator.StartsWith, KnownTimeSpanValue));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TimeSpan>("TimeSpanProp", FilterOperator.EndsWith, KnownTimeSpanValue));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TimeSpan>("TimeSpanProp", FilterOperator.Contains, KnownTimeSpanValue));
            }

            [Asynchronous, TestMethod]
            public void TimeSpanIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TimeSpan>("TimeSpanProp", FilterOperator.IsContainedIn, KnownTimeSpanValue));
            }

            #endregion TimeSpan

            #region Enum

            [Asynchronous, TestMethod]
            public void EnumIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsLessThan, KnownEnumValue, v => v.All(m => m.EnumProp.CompareTo(KnownEnumValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void EnumIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsLessThanOrEqualTo, KnownEnumValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void EnumIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsEqualTo, KnownEnumValue, v => v.All(m => m.EnumProp.CompareTo(KnownEnumValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void EnumIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsNotEqualTo, KnownEnumValue, v => v.All(m => m.EnumProp.CompareTo(KnownEnumValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void EnumIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsGreaterThanOrEqualTo, KnownEnumValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.EnumProp.CompareTo(KnownEnumValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void EnumIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("EnumProp", FilterOperator.IsGreaterThan, KnownEnumValue, v => v.All(m => m.EnumProp.CompareTo(KnownEnumValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void EnumStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TestEnum>("EnumProp", FilterOperator.StartsWith, KnownEnumValue));
            }

            [Asynchronous, TestMethod]
            public void EnumEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TestEnum>("EnumProp", FilterOperator.EndsWith, KnownEnumValue));
            }

            [Asynchronous, TestMethod]
            public void EnumContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TestEnum>("EnumProp", FilterOperator.Contains, KnownEnumValue));
            }

            [Asynchronous, TestMethod]
            public void EnumIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, TestEnum>("EnumProp", FilterOperator.IsContainedIn, KnownEnumValue));
            }

            #endregion TimeSpan

            #region Guid

            [Asynchronous, TestMethod]
            public void GuidIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsLessThan, KnownGuidValue, v => v.All(m => m.GuidProp.CompareTo(KnownGuidValue) < 0)));
            }

            [Asynchronous, TestMethod]
            public void GuidIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsLessThanOrEqualTo, KnownGuidValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) > 0), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) < 0), "Less");
                    Assert.IsTrue(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void GuidIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsEqualTo, KnownGuidValue, v => v.All(m => m.GuidProp.CompareTo(KnownGuidValue) == 0)));
            }

            [Asynchronous, TestMethod]
            public void GuidIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsNotEqualTo, KnownGuidValue, v => v.All(m => m.GuidProp.CompareTo(KnownGuidValue) != 0)));
            }

            [Asynchronous, TestMethod]
            public void GuidIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsGreaterThanOrEqualTo, KnownGuidValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) < 0), "Not Less");
                    Assert.IsTrue(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) > 0), "Greater");
                    Assert.IsTrue(v.Any(m => m.GuidProp.CompareTo(KnownGuidValue) == 0), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void GuidIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("GuidProp", FilterOperator.IsGreaterThan, KnownGuidValue, v => v.All(m => m.GuidProp.CompareTo(KnownGuidValue) > 0)));
            }

            [Asynchronous, TestMethod]
            public void GuidStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Guid>("GuidProp", FilterOperator.StartsWith, KnownGuidValue));
            }

            [Asynchronous, TestMethod]
            public void GuidEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Guid>("GuidProp", FilterOperator.EndsWith, KnownGuidValue));
            }

            [Asynchronous, TestMethod]
            public void GuidContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Guid>("GuidProp", FilterOperator.Contains, KnownGuidValue));
            }

            [Asynchronous, TestMethod]
            public void GuidIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Guid>("GuidProp", FilterOperator.IsContainedIn, KnownGuidValue));
            }

            #endregion Guid

            #region Uri

            [Asynchronous, TestMethod]
            public void UriIsLessThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.IsLessThan, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.IsLessThanOrEqualTo, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UriProp", FilterOperator.IsEqualTo, KnownUriValue, v => v.All(m => m.UriProp == KnownUriValue)));
            }

            [Asynchronous, TestMethod]
            public void UriIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("UriProp", FilterOperator.IsNotEqualTo, KnownUriValue, v => v.All(m => m.UriProp != KnownUriValue)));
            }

            [Asynchronous, TestMethod]
            public void UriIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.IsGreaterThanOrEqualTo, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.IsGreaterThan, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.StartsWith, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.EndsWith, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.Contains, KnownUriValue));
            }

            [Asynchronous, TestMethod]
            public void UriIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Uri>("UriProp", FilterOperator.IsContainedIn, KnownUriValue));
            }

            #endregion Uri

            #region XElement

            [Asynchronous, TestMethod]
            public void XElementIsLessThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsLessThan, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsLessThanOrEqualTo, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsEqualTo, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsNotEqualTo, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsGreaterThanOrEqualTo, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsGreaterThan, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.StartsWith, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.EndsWith, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.Contains, KnownXElementValue));
            }

            [Asynchronous, TestMethod]
            public void XElementIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, XElement>("XElementProp", FilterOperator.IsContainedIn, KnownXElementValue));
            }

            #endregion XElement

            #region IEnumerable

            [Asynchronous, TestMethod]
            public void IEnumerableIsLessThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsLessThan, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsLessOrEqualToThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsLessThanOrEqualTo, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsEqualTo, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsNotEqualTo, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsGreaterThanOrEqualToThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsGreaterThanOrEqualTo, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsGreaterThan, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.StartsWith, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.EndsWith, new[] { KnownStringValue }));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.Contains, KnownStringValue));
            }

            [Asynchronous, TestMethod]
            public void IEnumerableIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, IEnumerable<String>>("StringsProp", FilterOperator.IsContainedIn, new[] { KnownStringValue }));
            }

            #endregion IEnumerable

            #region Array

            [Asynchronous, TestMethod]
            public void ArrayIsLessThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsLessThan, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsLessOrEqualToThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsLessThanOrEqualTo, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsEqualTo, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsNotEqualTo, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsGreaterThanOrEqualToThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsGreaterThanOrEqualTo, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsGreaterThan, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayStartsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.StartsWith, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayEndsWith()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.EndsWith, new[] { KnownInt32Value }));
            }

            [Asynchronous, TestMethod]
            public void ArrayContains()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.Contains, KnownInt32Value));
            }

            [Asynchronous, TestMethod]
            public void ArrayIsContainedIn()
            {
                this.TestScenarios(FilterScenario.Failure<MixedType, Int32[]>("IntsProp", FilterOperator.IsContainedIn, new[] { KnownInt32Value }));
            }

            #endregion Array

            #region Nullable<Boolean>

            [Asynchronous, TestMethod]
            public void NullableBooleanUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.IsLessThan, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.IsLessThanOrEqualTo, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.IsGreaterThanOrEqualTo, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.IsGreaterThan, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.StartsWith, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.EndsWith, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.Contains, true),
                    FilterScenario.Failure<MixedType, Nullable<Boolean>>("NullableBooleanProp", FilterOperator.IsContainedIn, true));
            }

            [Asynchronous, TestMethod]
            public void NullableBooleanIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableBooleanProp", FilterOperator.IsEqualTo, true, v => v.All(m => m.NullableBooleanProp == true)),
                    FilterScenario.Success<MixedType>("NullableBooleanProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableBooleanProp == null)));
            }

            public void NullableBooleanIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableBooleanProp", FilterOperator.IsNotEqualTo, true, v => v.All(m => m.NullableBooleanProp != true)),
                    FilterScenario.Success<MixedType>("NullableBooleanProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableBooleanProp != null)));
            }

            #endregion Nullable<Boolean>

            #region Nullable<Byte>

            [Asynchronous, TestMethod]
            public void NullableByteUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Byte>>("NullableByteProp", FilterOperator.StartsWith, KnownByteValue),
                    FilterScenario.Failure<MixedType, Nullable<Byte>>("NullableByteProp", FilterOperator.EndsWith, KnownByteValue),
                    FilterScenario.Failure<MixedType, Nullable<Byte>>("NullableByteProp", FilterOperator.Contains, KnownByteValue),
                    FilterScenario.Failure<MixedType, Nullable<Byte>>("NullableByteProp", FilterOperator.IsContainedIn, KnownByteValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableByteCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsLessThan, KnownByteValue, v => v.All(m => m.NullableByteProp < KnownByteValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsLessThanOrEqualTo, KnownByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableByteProp > KnownByteValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableByteProp < KnownByteValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableByteProp == KnownByteValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsEqualTo, KnownByteValue, v => v.All(m => m.NullableByteProp == KnownByteValue)),
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableByteProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsNotEqualTo, KnownByteValue, v => v.All(m => m.NullableByteProp != KnownByteValue)),
                    FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableByteProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsGreaterThanOrEqualTo, KnownByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableByteProp < KnownByteValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableByteProp > KnownByteValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableByteProp == KnownByteValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableByteIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableByteProp", FilterOperator.IsGreaterThan, KnownByteValue, v => v.All(m => m.NullableByteProp > KnownByteValue)));
            }

            #endregion Nullable<Byte>

            #region Nullable<SByte>

            [Asynchronous, TestMethod]
            public void NullableSByteUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<SByte>>("NullableSByteProp", FilterOperator.StartsWith, KnownSByteValue),
                    FilterScenario.Failure<MixedType, Nullable<SByte>>("NullableSByteProp", FilterOperator.EndsWith, KnownSByteValue),
                    FilterScenario.Failure<MixedType, Nullable<SByte>>("NullableSByteProp", FilterOperator.Contains, KnownSByteValue),
                    FilterScenario.Failure<MixedType, Nullable<SByte>>("NullableSByteProp", FilterOperator.IsContainedIn, KnownSByteValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableSByteCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsLessThan, KnownSByteValue, v => v.All(m => m.NullableSByteProp < KnownSByteValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsLessThanOrEqualTo, KnownSByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableSByteProp > KnownSByteValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableSByteProp < KnownSByteValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableSByteProp == KnownSByteValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsEqualTo, KnownSByteValue, v => v.All(m => m.NullableSByteProp == KnownSByteValue)),
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableSByteProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsNotEqualTo, KnownSByteValue, v => v.All(m => m.NullableSByteProp != KnownSByteValue)),
                    FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableSByteProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsGreaterThanOrEqualTo, KnownSByteValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableSByteProp < KnownSByteValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableSByteProp > KnownSByteValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableSByteProp == KnownSByteValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableSByteIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSByteProp", FilterOperator.IsGreaterThan, KnownSByteValue, v => v.All(m => m.NullableSByteProp > KnownSByteValue)));
            }

            #endregion Nullable<SByte>

            #region Nullable<Int16>

            [Asynchronous, TestMethod]
            public void NullableInt16Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Int16>>("NullableInt16Prop", FilterOperator.StartsWith, KnownInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<Int16>>("NullableInt16Prop", FilterOperator.EndsWith, KnownInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<Int16>>("NullableInt16Prop", FilterOperator.Contains, KnownInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<Int16>>("NullableInt16Prop", FilterOperator.IsContainedIn, KnownInt16Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableInt16CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsLessThan, KnownInt16Value, v => v.All(m => m.NullableInt16Prop < KnownInt16Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt16Prop > KnownInt16Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt16Prop < KnownInt16Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt16Prop == KnownInt16Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsEqualTo, KnownInt16Value, v => v.All(m => m.NullableInt16Prop == KnownInt16Value)),
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableInt16Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsNotEqualTo, KnownInt16Value, v => v.All(m => m.NullableInt16Prop != KnownInt16Value)),
                    FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableInt16Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt16Prop < KnownInt16Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt16Prop > KnownInt16Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt16Prop == KnownInt16Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt16IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt16Prop", FilterOperator.IsGreaterThan, KnownInt16Value, v => v.All(m => m.NullableInt16Prop > KnownInt16Value)));
            }

            #endregion Nullable<Int16>

            #region Nullable<UInt16>

            [Asynchronous, TestMethod]
            public void NullableUInt16Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<UInt16>>("NullableUInt16Prop", FilterOperator.StartsWith, KnownUInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt16>>("NullableUInt16Prop", FilterOperator.EndsWith, KnownUInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt16>>("NullableUInt16Prop", FilterOperator.Contains, KnownUInt16Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt16>>("NullableUInt16Prop", FilterOperator.IsContainedIn, KnownUInt16Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableUInt16CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsLessThan, KnownUInt16Value, v => v.All(m => m.NullableUInt16Prop < KnownUInt16Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt16Prop > KnownUInt16Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt16Prop < KnownUInt16Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt16Prop == KnownUInt16Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsEqualTo, KnownUInt16Value, v => v.All(m => m.NullableUInt16Prop == KnownUInt16Value)),
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableUInt16Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsNotEqualTo, KnownUInt16Value, v => v.All(m => m.NullableUInt16Prop != KnownUInt16Value)),
                    FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableUInt16Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt16Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt16Prop < KnownUInt16Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt16Prop > KnownUInt16Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt16Prop == KnownUInt16Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt16IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt16Prop", FilterOperator.IsGreaterThan, KnownUInt16Value, v => v.All(m => m.NullableUInt16Prop > KnownUInt16Value)));
            }

            #endregion Nullable<UInt16>

            #region Nullable<Int32>

            [Asynchronous, TestMethod]
            public void NullableInt32Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Int32>>("NullableInt32Prop", FilterOperator.StartsWith, KnownInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<Int32>>("NullableInt32Prop", FilterOperator.EndsWith, KnownInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<Int32>>("NullableInt32Prop", FilterOperator.Contains, KnownInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<Int32>>("NullableInt32Prop", FilterOperator.IsContainedIn, KnownInt32Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableInt32CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsLessThan, KnownInt32Value, v => v.All(m => m.NullableInt32Prop < KnownInt32Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt32Prop > KnownInt32Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt32Prop < KnownInt32Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt32Prop == KnownInt32Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsEqualTo, KnownInt32Value, v => v.All(m => m.NullableInt32Prop == KnownInt32Value)),
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableInt32Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsNotEqualTo, KnownInt32Value, v => v.All(m => m.NullableInt32Prop != KnownInt32Value)),
                    FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableInt32Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt32Prop < KnownInt32Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt32Prop > KnownInt32Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt32Prop == KnownInt32Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt32IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt32Prop", FilterOperator.IsGreaterThan, KnownInt32Value, v => v.All(m => m.NullableInt32Prop > KnownInt32Value)));
            }

            #endregion Nullable<Int32>

            #region Nullable<UInt32>

            [Asynchronous, TestMethod]
            public void NullableUInt32Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<UInt32>>("NullableUInt32Prop", FilterOperator.StartsWith, KnownUInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt32>>("NullableUInt32Prop", FilterOperator.EndsWith, KnownUInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt32>>("NullableUInt32Prop", FilterOperator.Contains, KnownUInt32Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt32>>("NullableUInt32Prop", FilterOperator.IsContainedIn, KnownUInt32Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableUInt32CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsLessThan, KnownUInt32Value, v => v.All(m => m.NullableUInt32Prop < KnownUInt32Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt32Prop > KnownUInt32Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt32Prop < KnownUInt32Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt32Prop == KnownUInt32Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsEqualTo, KnownUInt32Value, v => v.All(m => m.NullableUInt32Prop == KnownUInt32Value)),
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableUInt32Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsNotEqualTo, KnownUInt32Value, v => v.All(m => m.NullableUInt32Prop != KnownUInt32Value)),
                    FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableUInt32Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt32Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt32Prop < KnownUInt32Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt32Prop > KnownUInt32Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt32Prop == KnownUInt32Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt32IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt32Prop", FilterOperator.IsGreaterThan, KnownUInt32Value, v => v.All(m => m.NullableUInt32Prop > KnownUInt32Value)));
            }

            #endregion Nullable<UInt32>

            #region Nullable<Int64>

            [Asynchronous, TestMethod]
            public void NullableInt64Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Int64>>("NullableInt64Prop", FilterOperator.StartsWith, KnownInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<Int64>>("NullableInt64Prop", FilterOperator.EndsWith, KnownInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<Int64>>("NullableInt64Prop", FilterOperator.Contains, KnownInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<Int64>>("NullableInt64Prop", FilterOperator.IsContainedIn, KnownInt64Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableInt64CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsLessThan, KnownInt64Value, v => v.All(m => m.NullableInt64Prop < KnownInt64Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsLessThanOrEqualTo, KnownInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt64Prop > KnownInt64Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt64Prop < KnownInt64Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt64Prop == KnownInt64Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsEqualTo, KnownInt64Value, v => v.All(m => m.NullableInt64Prop == KnownInt64Value)),
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableInt64Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsNotEqualTo, KnownInt64Value, v => v.All(m => m.NullableInt64Prop != KnownInt64Value)),
                    FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableInt64Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableInt64Prop < KnownInt64Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableInt64Prop > KnownInt64Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableInt64Prop == KnownInt64Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableInt64IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableInt64Prop", FilterOperator.IsGreaterThan, KnownInt64Value, v => v.All(m => m.NullableInt64Prop > KnownInt64Value)));
            }

            #endregion Nullable<Int64>

            #region Nullable<UInt64>

            [Asynchronous, TestMethod]
            public void NullableUInt64Unsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<UInt64>>("NullableUInt64Prop", FilterOperator.StartsWith, KnownUInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt64>>("NullableUInt64Prop", FilterOperator.EndsWith, KnownUInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt64>>("NullableUInt64Prop", FilterOperator.Contains, KnownUInt64Value),
                    FilterScenario.Failure<MixedType, Nullable<UInt64>>("NullableUInt64Prop", FilterOperator.IsContainedIn, KnownUInt64Value));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableUInt64CompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsLessThan, KnownUInt64Value, v => v.All(m => m.NullableUInt64Prop < KnownUInt64Value)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsLessThanOrEqualTo, KnownUInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt64Prop > KnownUInt64Value), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt64Prop < KnownUInt64Value), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt64Prop == KnownUInt64Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsEqualTo, KnownUInt64Value, v => v.All(m => m.NullableUInt64Prop == KnownUInt64Value)),
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableUInt64Prop == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsNotEqualTo, KnownUInt64Value, v => v.All(m => m.NullableUInt64Prop != KnownUInt64Value)),
                    FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableUInt64Prop != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsGreaterThanOrEqualTo, KnownUInt64Value, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableUInt64Prop < KnownUInt64Value), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableUInt64Prop > KnownUInt64Value), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableUInt64Prop == KnownUInt64Value), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableUInt64IsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableUInt64Prop", FilterOperator.IsGreaterThan, KnownUInt64Value, v => v.All(m => m.NullableUInt64Prop > KnownUInt64Value)));
            }

            #endregion Nullable<UInt64>

            #region Nullable<Decimal>

            [Asynchronous, TestMethod]
            public void NullableDecimalUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Decimal>>("NullableDecimalProp", FilterOperator.StartsWith, KnownDecimalValue),
                    FilterScenario.Failure<MixedType, Nullable<Decimal>>("NullableDecimalProp", FilterOperator.EndsWith, KnownDecimalValue),
                    FilterScenario.Failure<MixedType, Nullable<Decimal>>("NullableDecimalProp", FilterOperator.Contains, KnownDecimalValue),
                    FilterScenario.Failure<MixedType, Nullable<Decimal>>("NullableDecimalProp", FilterOperator.IsContainedIn, KnownDecimalValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableDecimalCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsLessThan, KnownDecimalValue, v => v.All(m => m.NullableDecimalProp < KnownDecimalValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsLessThanOrEqualTo, KnownDecimalValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDecimalProp > KnownDecimalValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDecimalProp < KnownDecimalValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableDecimalProp == KnownDecimalValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsEqualTo, KnownDecimalValue, v => v.All(m => m.NullableDecimalProp == KnownDecimalValue)),
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableDecimalProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsNotEqualTo, KnownDecimalValue, v => v.All(m => m.NullableDecimalProp != KnownDecimalValue)),
                    FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableDecimalProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDecimalValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDecimalProp < KnownDecimalValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableDecimalProp > KnownDecimalValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDecimalProp == KnownDecimalValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDecimalIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDecimalProp", FilterOperator.IsGreaterThan, KnownDecimalValue, v => v.All(m => m.NullableDecimalProp > KnownDecimalValue)));
            }

            #endregion Nullable<Decimal>

            #region Nullable<Single>

            [Asynchronous, TestMethod]
            public void NullableSingleUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Single>>("NullableSingleProp", FilterOperator.StartsWith, KnownSingleValue),
                    FilterScenario.Failure<MixedType, Nullable<Single>>("NullableSingleProp", FilterOperator.EndsWith, KnownSingleValue),
                    FilterScenario.Failure<MixedType, Nullable<Single>>("NullableSingleProp", FilterOperator.Contains, KnownSingleValue),
                    FilterScenario.Failure<MixedType, Nullable<Single>>("NullableSingleProp", FilterOperator.IsContainedIn, KnownSingleValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableSingleCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsLessThan, KnownSingleValue, v => v.All(m => m.NullableSingleProp < KnownSingleValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsLessThanOrEqualTo, KnownSingleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableSingleProp > KnownSingleValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableSingleProp < KnownSingleValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableSingleProp == KnownSingleValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsEqualTo, KnownSingleValue, v => v.All(m => m.NullableSingleProp == KnownSingleValue)),
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableSingleProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsNotEqualTo, KnownSingleValue, v => v.All(m => m.NullableSingleProp != KnownSingleValue)),
                    FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableSingleProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsGreaterThanOrEqualTo, KnownSingleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableSingleProp < KnownSingleValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableSingleProp > KnownSingleValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableSingleProp == KnownSingleValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableSingleIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableSingleProp", FilterOperator.IsGreaterThan, KnownSingleValue, v => v.All(m => m.NullableSingleProp > KnownSingleValue)));
            }

            #endregion Nullable<Single>

            #region Nullable<Double>

            [Asynchronous, TestMethod]
            public void NullableDoubleUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Double>>("NullableDoubleProp", FilterOperator.StartsWith, KnownDoubleValue),
                    FilterScenario.Failure<MixedType, Nullable<Double>>("NullableDoubleProp", FilterOperator.EndsWith, KnownDoubleValue),
                    FilterScenario.Failure<MixedType, Nullable<Double>>("NullableDoubleProp", FilterOperator.Contains, KnownDoubleValue),
                    FilterScenario.Failure<MixedType, Nullable<Double>>("NullableDoubleProp", FilterOperator.IsContainedIn, KnownDoubleValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableDoubleCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsLessThan, KnownDoubleValue, v => v.All(m => m.NullableDoubleProp < KnownDoubleValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsLessThanOrEqualTo, KnownDoubleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDoubleProp > KnownDoubleValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDoubleProp < KnownDoubleValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableDoubleProp == KnownDoubleValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsEqualTo, KnownDoubleValue, v => v.All(m => m.NullableDoubleProp == KnownDoubleValue)),
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableDoubleProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsNotEqualTo, KnownDoubleValue, v => v.All(m => m.NullableDoubleProp != KnownDoubleValue)),
                    FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableDoubleProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDoubleValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDoubleProp < KnownDoubleValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableDoubleProp > KnownDoubleValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDoubleProp == KnownDoubleValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDoubleIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDoubleProp", FilterOperator.IsGreaterThan, KnownDoubleValue, v => v.All(m => m.NullableDoubleProp > KnownDoubleValue)));
            }

            #endregion Nullable<Double>

            #region Nullable<Char>

            [Asynchronous, TestMethod]
            public void NullableCharUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Char>>("NullableCharProp", FilterOperator.StartsWith, KnownCharValue),
                    FilterScenario.Failure<MixedType, Nullable<Char>>("NullableCharProp", FilterOperator.EndsWith, KnownCharValue),
                    FilterScenario.Failure<MixedType, Nullable<Char>>("NullableCharProp", FilterOperator.Contains, KnownCharValue),
                    FilterScenario.Failure<MixedType, Nullable<Char>>("NullableCharProp", FilterOperator.IsContainedIn, KnownCharValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableCharCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsLessThan, KnownCharValue, v => v.All(m => m.NullableCharProp < KnownCharValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsLessThanOrEqualTo, KnownCharValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableCharProp > KnownCharValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableCharProp < KnownCharValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableCharProp == KnownCharValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsEqualTo, KnownCharValue, v => v.All(m => m.NullableCharProp == KnownCharValue)),
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableCharProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsNotEqualTo, KnownCharValue, v => v.All(m => m.NullableCharProp != KnownCharValue)),
                    FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableCharProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsGreaterThanOrEqualTo, KnownCharValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableCharProp < KnownCharValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableCharProp > KnownCharValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableCharProp == KnownCharValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableCharIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableCharProp", FilterOperator.IsGreaterThan, KnownCharValue, v => v.All(m => m.NullableCharProp > KnownCharValue)));
            }

            #endregion Nullable<Char>

            #region Nullable<DateTime>

            [Asynchronous, TestMethod]
            public void NullableDateTimeUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<DateTime>>("NullableDateTimeProp", FilterOperator.StartsWith, KnownDateTimeValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTime>>("NullableDateTimeProp", FilterOperator.EndsWith, KnownDateTimeValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTime>>("NullableDateTimeProp", FilterOperator.Contains, KnownDateTimeValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTime>>("NullableDateTimeProp", FilterOperator.IsContainedIn, KnownDateTimeValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableDateTimeCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsLessThan, KnownDateTimeValue, v => v.All(m => m.NullableDateTimeProp < KnownDateTimeValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsLessThanOrEqualTo, KnownDateTimeValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDateTimeProp > KnownDateTimeValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeProp < KnownDateTimeValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeProp == KnownDateTimeValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsEqualTo, KnownDateTimeValue, v => v.All(m => m.NullableDateTimeProp == KnownDateTimeValue)),
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableDateTimeProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsNotEqualTo, KnownDateTimeValue, v => v.All(m => m.NullableDateTimeProp != KnownDateTimeValue)),
                    FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableDateTimeProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDateTimeValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDateTimeProp < KnownDateTimeValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeProp > KnownDateTimeValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeProp == KnownDateTimeValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeProp", FilterOperator.IsGreaterThan, KnownDateTimeValue, v => v.All(m => m.NullableDateTimeProp > KnownDateTimeValue)));
            }

            #endregion Nullable<DateTime>

            #region Nullable<DateTimeOffset>

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<DateTimeOffset>>("NullableDateTimeOffsetProp", FilterOperator.StartsWith, KnownDateTimeOffsetValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTimeOffset>>("NullableDateTimeOffsetProp", FilterOperator.EndsWith, KnownDateTimeOffsetValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTimeOffset>>("NullableDateTimeOffsetProp", FilterOperator.Contains, KnownDateTimeOffsetValue),
                    FilterScenario.Failure<MixedType, Nullable<DateTimeOffset>>("NullableDateTimeOffsetProp", FilterOperator.IsContainedIn, KnownDateTimeOffsetValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableDateTimeOffsetCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsLessThan, KnownDateTimeOffsetValue, v => v.All(m => m.NullableDateTimeOffsetProp < KnownDateTimeOffsetValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsLessThanOrEqualTo, KnownDateTimeOffsetValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDateTimeOffsetProp > KnownDateTimeOffsetValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeOffsetProp < KnownDateTimeOffsetValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeOffsetProp == KnownDateTimeOffsetValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsEqualTo, KnownDateTimeOffsetValue, v => v.All(m => m.NullableDateTimeOffsetProp == KnownDateTimeOffsetValue)),
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableDateTimeOffsetProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsNotEqualTo, KnownDateTimeOffsetValue, v => v.All(m => m.NullableDateTimeOffsetProp != KnownDateTimeOffsetValue)),
                    FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableDateTimeOffsetProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsGreaterThanOrEqualTo, KnownDateTimeOffsetValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableDateTimeOffsetProp < KnownDateTimeOffsetValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeOffsetProp > KnownDateTimeOffsetValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableDateTimeOffsetProp == KnownDateTimeOffsetValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableDateTimeOffsetIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableDateTimeOffsetProp", FilterOperator.IsGreaterThan, KnownDateTimeOffsetValue, v => v.All(m => m.NullableDateTimeOffsetProp > KnownDateTimeOffsetValue)));
            }

            #endregion Nullable<DateTimeOffset>

            #region Nullable<TimeSpan>

            [Asynchronous, TestMethod]
            public void NullableTimeSpanUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<TimeSpan>>("NullableTimeSpanProp", FilterOperator.StartsWith, KnownTimeSpanValue),
                    FilterScenario.Failure<MixedType, Nullable<TimeSpan>>("NullableTimeSpanProp", FilterOperator.EndsWith, KnownTimeSpanValue),
                    FilterScenario.Failure<MixedType, Nullable<TimeSpan>>("NullableTimeSpanProp", FilterOperator.Contains, KnownTimeSpanValue),
                    FilterScenario.Failure<MixedType, Nullable<TimeSpan>>("NullableTimeSpanProp", FilterOperator.IsContainedIn, KnownTimeSpanValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableTimeSpanCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsLessThan, KnownTimeSpanValue, v => v.All(m => m.NullableTimeSpanProp < KnownTimeSpanValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsLessThanOrEqualTo, KnownTimeSpanValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableTimeSpanProp > KnownTimeSpanValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableTimeSpanProp < KnownTimeSpanValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableTimeSpanProp == KnownTimeSpanValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsEqualTo, KnownTimeSpanValue, v => v.All(m => m.NullableTimeSpanProp == KnownTimeSpanValue)),
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableTimeSpanProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsNotEqualTo, KnownTimeSpanValue, v => v.All(m => m.NullableTimeSpanProp != KnownTimeSpanValue)),
                    FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableTimeSpanProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsGreaterThanOrEqualTo, KnownTimeSpanValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableTimeSpanProp < KnownTimeSpanValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableTimeSpanProp > KnownTimeSpanValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableTimeSpanProp == KnownTimeSpanValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableTimeSpanIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableTimeSpanProp", FilterOperator.IsGreaterThan, KnownTimeSpanValue, v => v.All(m => m.NullableTimeSpanProp > KnownTimeSpanValue)));
            }

            #endregion Nullable<TimeSpan>

            #region Nullable<Enum>

            [Asynchronous, TestMethod]
            public void NullableEnumUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<TestEnum>>("NullableEnumProp", FilterOperator.StartsWith, KnownEnumValue),
                    FilterScenario.Failure<MixedType, Nullable<TestEnum>>("NullableEnumProp", FilterOperator.EndsWith, KnownEnumValue),
                    FilterScenario.Failure<MixedType, Nullable<TestEnum>>("NullableEnumProp", FilterOperator.Contains, KnownEnumValue),
                    FilterScenario.Failure<MixedType, Nullable<TestEnum>>("NullableEnumProp", FilterOperator.IsContainedIn, KnownEnumValue));
            }

            [Asynchronous, TestMethod]
            [Description("These operators will always return false when comparing to null. The queries succeed but produce no results.")]
            public void NullableEnumCompareToNull()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsLessThan, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsLessThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsGreaterThanOrEqualTo, null, v => !v.Any()),
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsGreaterThan, null, v => !v.Any()));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsLessThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsLessThan, KnownEnumValue, v => v.All(m => m.NullableEnumProp < KnownEnumValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsLessThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsLessThanOrEqualTo, KnownEnumValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableEnumProp > KnownEnumValue), "Not Greater");
                    Assert.IsTrue(v.Any(m => m.NullableEnumProp < KnownEnumValue), "Less");
                    Assert.IsTrue(v.Any(m => m.NullableEnumProp == KnownEnumValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsEqualTo, KnownEnumValue, v => v.All(m => m.NullableEnumProp == KnownEnumValue)),
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsEqualTo, null, v => v.All(m => m.NullableEnumProp == null)));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsNotEqualTo()
            {
                this.TestScenarios(
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsNotEqualTo, KnownEnumValue, v => v.All(m => m.NullableEnumProp != KnownEnumValue)),
                    FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsNotEqualTo, null, v => v.All(m => m.NullableEnumProp != null)));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsGreaterThanOrEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsGreaterThanOrEqualTo, KnownEnumValue, v =>
                {
                    Assert.IsFalse(v.Any(m => m.NullableEnumProp < KnownEnumValue), "Not Less");
                    Assert.IsTrue(v.Any(m => m.NullableEnumProp > KnownEnumValue), "Greater");
                    Assert.IsTrue(v.Any(m => m.NullableEnumProp == KnownEnumValue), "Equal");
                }));
            }

            [Asynchronous, TestMethod]
            public void NullableEnumIsGreaterThan()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableEnumProp", FilterOperator.IsGreaterThan, KnownEnumValue, v => v.All(m => m.NullableEnumProp > KnownEnumValue)));
            }

            #endregion Nullable<Enum>

            #region Nullable<Guid>

            [Asynchronous, TestMethod]
            public void NullableGuidUnsupported()
            {
                this.TestScenarios(
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.IsLessThan, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.IsLessThanOrEqualTo, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.IsGreaterThan, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.IsGreaterThanOrEqualTo, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.StartsWith, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.EndsWith, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.Contains, KnownGuidValue),
                    FilterScenario.Failure<MixedType, Nullable<Guid>>("NullableGuidProp", FilterOperator.IsContainedIn, KnownGuidValue));
            }

            [Asynchronous, TestMethod]
            public void NullableGuidIsEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableGuidProp", FilterOperator.IsEqualTo, KnownGuidValue, v => v.All(m => m.NullableGuidProp == KnownGuidValue)));
            }

            [Asynchronous, TestMethod]
            public void NullableGuidIsNotEqualTo()
            {
                this.TestScenarios(FilterScenario.Success<MixedType>("NullableGuidProp", FilterOperator.IsNotEqualTo, KnownGuidValue, v => v.All(m => m.NullableGuidProp != KnownGuidValue)));
            }

            #endregion Nullable<Guid>
        }

        #endregion Filter Scenario Tests

        #region Paging Enabled

        [TestMethod]
        [Asynchronous]
        [Description("Filters are respecting with paging enabled")]
        public void PagingAndFiltering()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                // Paging and Loading pattern: {(3,3), (3,2)}
                this._dds.LoadSize = 7;
                this._dds.PageSize = 3; // This triggers the first PageChanged event
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });
                this._dds.Load();

                this._asyncEventFailureMessage = "Initial load";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "Count should be 3 after the initial load");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "ItemCount should be 6 after the initial load");
                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0 after the initial load");
                Assert.AreEqual("Redmond", (this._view[0] as City).Name);
                Assert.AreEqual("Bellevue", (this._view[1] as City).Name);
                Assert.AreEqual("Duvall", (this._view[2] as City).Name);

                // The 2nd page is already loaded
                this._ddsLoadingDataExpected = 0;
                this._view.MoveToNextPage();

                this._asyncEventFailureMessage = "MoveToNextPage()";
            });

            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(3, this._view.Count, "Count should be 3 after MoveToNextPage");
                Assert.AreEqual(6, this._pagedCollectionView.ItemCount, "ItemCount should be 6 after MoveToNextPage");
                Assert.AreEqual(1, this._view.PageIndex, "PageIndex should be 1 after MoveToNextPage");
                Assert.AreEqual("Carnation", (this._view[0] as City).Name);
                Assert.AreEqual("Everett", (this._view[1] as City).Name);
                Assert.AreEqual("Tacoma", (this._view[2] as City).Name);

                this._dds.FilterDescriptors[0].Value = "Not_A_State";
                this._dds.Load();

                this._asyncEventFailureMessage = "Load() after Not_A_State";
            });

            this.AssertLoadingData();
            this.AssertPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.Count, "Count should be 0");
                Assert.AreEqual(0, this._pagedCollectionView.ItemCount, "ItemCount should be 0");
                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should be 0");

                bool success = this._view.MoveToNextPage();
                Assert.IsTrue(success, "MoveToNextPage should succeed");
                Assert.IsTrue(this._view.IsPageChanging, "IsPageChanging should be true");

                this._asyncEventFailureMessage = "MoveToNextPage() with no next page";
            });

            this.AssertLoadingData();
            this.AssertNoPageChanged();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                Assert.AreEqual(0, this._view.Count, "Count should still be 0");
                Assert.AreEqual(0, this._pagedCollectionView.ItemCount, "ItemCount should still be 0");
                Assert.AreEqual(0, this._view.PageIndex, "PageIndex should still be 0");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [Description("Paging and filtering down to where there are no matches clears the current page")]
        public void PagingFilteringToClearPage()
        {
            EnqueueCallback(() =>
            {
                this._dds.QueryName = "GetCitiesQuery";
                this._dds.DomainContext = new CityDomainContext();
                this._dds.PageSize = 3;
                this._dds.FilterDescriptors.Add(new FilterDescriptor { PropertyPath = "StateName", Operator = FilterOperator.IsEqualTo, Value = "WA" });

                this._asyncEventFailureMessage = "Initial Load";
                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                this.ResetLoadState();
                this.ResetPageChanged();

                this._dds.FilterDescriptors[0].Value = "Not a state";
                this._asyncEventFailureMessage = "Load with 'Not a state' filter";
                this.TrackCollectionChanged();

                this._dds.Load();
            });

            this.AssertLoadingData();

            EnqueueCallback(() =>
            {
                // TODO: Change from 2 Reset events to 1 when bug 709185 is fixed and our workaround can be removed
                this.AssertCollectionChanged(0, 0, 2, "CollectionChanged should have been raised when the filter resulted in empty results.");
                Assert.AreEqual(0, this._view.Count, "Expected empty results when the filter has no matches");
            });

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling load after adding a filter decsriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void SettingFilterDescriptorCollectionAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling refresh after adding a filter decsriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void SettingFilterDescriptorCollectionAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void SettingFilterDescriptorCollectionWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void SettingFilterDescriptorCollectionWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void SettingFilterDescriptorCollectionWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling load after adding a filter decsriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void AddingFilterDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                this._dds.Load();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling refresh after adding a filter decsriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void AddingFilterDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void AddingFilterDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();

            this.LoadCities(3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void AddingFilterDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When adding a filter descriptor from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void AddingFilterDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadCities(3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("WA", "After adding the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling load after editing a filter descriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void EditingFilterDescriptorAndThenLoadingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors[0].Value = "CA";
                this._dds.Load();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After editing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling refresh after editing a filter descriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void EditingFilterDescriptorAndThenRefreshingLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors[0].Value = "CA";
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After editing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a filter descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void EditingFilterDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 1, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors[0].Value = "CA";
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After editing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a filter descriptor from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void EditingFilterDescriptorWithinDeferLoadLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.FilterDescriptors[0].Value = "CA";
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After editing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When editing a filter descriptor from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void EditingFilterDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            // PageSize of 1 to ensure there is more than 1 page for both WA and CA
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 1, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.FilterDescriptors[0].Value = "CA";
                }
            });

            this.AssertFirstPageLoadedAndFilteredByStateName("CA", "After editing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling load after removing a filter descriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void RemovingFilterDescriptorAndThenLoadingLoadsFirstPage()
        {
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.RemoveAt(0);
                this._dds.Load();
            });

            this.AssertFirstPageLoaded("After removing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When calling refresh after removing a filter descriptor from other than the first page, the first page is loaded and the data is filtered")]
        public void RemovingFilterDescriptorAndThenRefreshingLoadsFirstPage()
        {
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.RemoveAt(0);
                this._collectionView.Refresh();
            });

            this.AssertFirstPageLoaded("After removing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When removing a filter descriptor from other than the first page with AutoLoad set to true, the first page is loaded and the data is filtered")]
        public void RemovingFilterDescriptorWithAutoLoadLoadsFirstPage()
        {
            this.LoadDomainDataSourceControl();
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 3, true);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.RemoveAt(0);
            });

            this.AssertFirstPageLoaded("After removing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When removing a filter descriptor from other than the first page within a defer load, the first page is loaded and the data is filtered")]
        public void RemovingFilterDescriptorWithinDeferLoadLoadsFirstPage()
        {
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._dds.DeferLoad())
                {
                    this._dds.FilterDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the filter");

            EnqueueTestComplete();
        }

        [TestMethod]
        [Asynchronous]
        [WorkItem(797276)]
        [Description("When removing a filter descriptor from other than the first page within a defer refresh, the first page is loaded and the data is filtered")]
        public void RemovingFilterDescriptorWithinDeferRefreshLoadsFirstPage()
        {
            this.LoadFilteredCities(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"), 3, false);
            this.MoveToNextPage();

            EnqueueCallback(() =>
            {
                using (this._collectionView.DeferRefresh())
                {
                    this._dds.FilterDescriptors.RemoveAt(0);
                }
            });

            this.AssertFirstPageLoaded("After removing the filter");

            EnqueueTestComplete();
        }

        #endregion Paging Enabled

        #region Progressive Loading Enabled

        [TestMethod]
        [Asynchronous]
        [WorkItem(812133)]
        [Description("Can filter while progressive loading is enabled")]
        public void FilteringWithProgressiveLoading()
        {
            IEnumerable<City> cities = new CityData().Cities;
            int allCities = cities.Count();
            int citiesInWA = cities.Count(c => c.StateName == "WA");

            EnqueueCallback(() =>
            {
                // By using a loadsize of 1, we know the number of loads to be performed is equal to the
                // expected city count + 1 (the load that will determine that there are no more records).
                this._dds.LoadSize = 1;
                this._dds.QueryName = "GetCities";
                this._dds.DomainContext = new CityDomainContext();
                this._asyncEventFailureMessage = "First Progressive Load";
                this._dds.Load();
            });

            this.AssertLoadingData(allCities + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(allCities, this._view.Count, "The count should match the total city count after allowing the first progressive load to finish");

                this._dds.FilterDescriptors.Add(new FilterDescriptor("StateName", FilterOperator.IsEqualTo, "WA"));

                // Calling Refresh will test that the load of FirstItems is deferred properly
                this._collectionView.Refresh();
            });

            this.AssertLoadingData(citiesInWA + 1, true);

            EnqueueCallback(() =>
            {
                this.ResetLoadState();

                Assert.AreEqual<int>(citiesInWA, this._view.Count, "The count should match the number of cities in WA after allowing the second progressive load to finish");
                Assert.IsTrue(this._view.Cast<City>().All(c => c.StateName == "WA"), "All cities should be in WA after the second progressive load");
            });

            EnqueueTestComplete();
        }

        #endregion Progressive Loading Enabled

        #region Helper Methods

        /// <summary>
        /// Enqueue the necessary calls to load the cities with the specified filter,
        /// and with the specified page size and auto load properties.
        /// </summary>
        /// <param name="filterDescriptor">The <see cref="FilterDescriptor"/> to use.</param>
        /// <param name="pageSize">The <see cref="DomainDataSource.PageSize"/> to use.</param>
        /// <param name="autoLoad">What to set <see cref="DomainDataSource.AutoLoad"/> to.</param>
        private void LoadFilteredCities(FilterDescriptor filterDescriptor, int pageSize, bool autoLoad)
        {
            EnqueueCallback(() =>
            {
                this._dds.FilterDescriptors.Add(filterDescriptor);
            });

            this.LoadCities(pageSize, autoLoad);
        }

        /// <summary>
        /// Assert that the first page is loaded as the result of a previously enqueued callback,
        /// and that the view only contains cities with the specified state name.
        /// </summary>
        /// <param name="stateName">The state name expected for all cities in the view.</param>
        /// <param name="message">The message to include in the assert.</param>
        private void AssertFirstPageLoadedAndFilteredByStateName(string stateName, string message)
        {
            this.AssertFirstPageLoaded(message);

            EnqueueCallback(() =>
            {
                Assert.AreEqual<int>(0, this._view.Cast<City>().Count(c => c.StateName != stateName), string.Format("Not all cities had the state name '{0}'. {1}", stateName, message));
            });
        }

        #endregion Helper Methods
    }
}
