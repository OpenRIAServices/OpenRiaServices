using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using OpenRiaServices.DomainServices.Client.Test;
using DataTests.AdventureWorks.LTS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DescriptionAttribute = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace OpenRiaServices.Controls.DomainServices.Test
{
    /// <summary>
    /// Tests that the <see cref="CollectionViewConverter"/> can convert
    /// values properly.
    /// </summary>
    [TestClass]
    public class CollectionViewConverterTests
    {
        private static readonly CollectionViewConverter Converter = new CollectionViewConverter();

        [TestMethod]
        [Description("The ConvertBack method should throw a NotImplementedException")]
        public void ConvertBackIsNotImplemented()
        {
            ExceptionHelper.ExpectException<NotImplementedException>(() => Converter.ConvertBack(null, null, null, CultureInfo.CurrentCulture));
        }

        [TestMethod]
        [Description("An array of products should be reported as an enumerable entity type (using the HasElementType code path)")]
        public void ProductArrayIsEnumerableEntityType()
        {
            IEnumerable<Product> products = new Product[0];

            Type entityType;
            Assert.IsTrue(CollectionViewConverter.IsEnumerableEntityType(products.GetType(), out entityType));
            Assert.AreEqual<Type>(typeof(Product), entityType);
        }

        [TestMethod]
        [Description("A list of products should be reported as an enumerable entity type (using the IsGenericType code path)")]
        public void ProductListIsEnumerableEntityType()
        {
            IEnumerable<Product> products = new List<Product>();

            Type entityType;
            Assert.IsTrue(CollectionViewConverter.IsEnumerableEntityType(products.GetType(), out entityType));
            Assert.AreEqual<Type>(typeof(Product), entityType);
        }

        [TestMethod]
        [Description("The designer uses surrogate types to represent lists.  Mimic that behavior and ensure the type is an enumerable entity type.")]
        public void DesignInstanceListIsEnumerableEntityType()
        {
            IEnumerable<Product> products = new DesignInstanceListOfProducts();

            Type entityType;
            Assert.IsTrue(CollectionViewConverter.IsEnumerableEntityType(products.GetType(), out entityType));
            Assert.AreEqual<Type>(typeof(Product), entityType);
        }

        [TestMethod]
        [Description("A product array should get converted to a collection view")]
        public void CanConvertProductArrayToCollectionView()
        {
            Product first = new Product();
            Product second = new Product();
            IEnumerable<Product> products = new Product[] { first, second };

            object view = Converter.Convert(products, null, null, CultureInfo.CurrentCulture);
            Assert.IsInstanceOfType(view, typeof(PagedEntityCollectionView<Product>));
            Assert.IsTrue(products.SequenceEqual((PagedEntityCollectionView<Product>)view));
        }

        [TestMethod]
        [Description("A product list should get converted to a collection view")]
        public void CanConvertProductListToCollectionView()
        {
            Product first = new Product();
            Product second = new Product();
            IEnumerable<Product> products = new List<Product> { first, second };

            object view = Converter.Convert(products, null, null, CultureInfo.CurrentCulture);
            Assert.IsInstanceOfType(view, typeof(PagedEntityCollectionView<Product>));
            Assert.IsTrue(products.SequenceEqual((IEnumerable<Product>)view));
        }

        [TestMethod]
        [Description("A string array should not get reported as an enumerable entity type")]
        public void StringArrayIsNotEnumerableEntityType()
        {
            IEnumerable<string> strings = new string[0];

            Type entityType;
            Assert.IsFalse(CollectionViewConverter.IsEnumerableEntityType(strings.GetType(), out entityType));
            Assert.IsNull(entityType);
        }

        [TestMethod]
        [Description("A string list should not get reported as an enumerable entity type")]
        public void StringListIsNotEnumerableEntityType()
        {
            IEnumerable<string> strings = new List<string>();

            Type entityType;
            Assert.IsFalse(CollectionViewConverter.IsEnumerableEntityType(strings.GetType(), out entityType));
            Assert.IsNull(entityType);
        }

        [TestMethod]
        [Description("A string array should not get converted, and it should be returned as-is")]
        public void CannotConvertStringArrayToCollectionView()
        {
            IEnumerable<string> strings = new string[0];
            Assert.AreSame(strings, Converter.Convert(strings, null, null, CultureInfo.CurrentCulture));
        }

        [TestMethod]
        [Description("A string list should not get converted, and it should be returned as-is")]
        public void CannotConvertStringListToCollectionView()
        {
            IEnumerable<string> strings = new List<string>();
            Assert.AreSame(strings, Converter.Convert(strings, null, null, CultureInfo.CurrentCulture));
        }

        [TestMethod]
        [Description("A null value should be returned as null")]
        public void ConvertingNullReturnsNull()
        {
            Assert.IsNull(Converter.Convert(null, null, null, CultureInfo.CurrentCulture));
        }

        [TestMethod]
        [Description("Converting an ICollectionView will return the value as-is")]
        public void ConvertingICollectionViewReturnsValue()
        {
            ICollectionView view = new DomainDataSource().DataView;
            Assert.AreSame(view, Converter.Convert(view, null, null, CultureInfo.CurrentCulture));
        }

        [TestMethod]
        [Description("ICollectionViewFactory is supported, calling CreateView to construct the ICollectionView")]
        public void ConvertingICollectionViewFactoryCallsCreateView()
        {
            bool createViewCalled = false;
            ICollectionView view = new DomainDataSource().DataView;

            MockCollectionViewFactory factory = new MockCollectionViewFactory(() =>
                {
                    createViewCalled = true;
                    return view;
                });

            Assert.AreSame(view, Converter.Convert(factory, null, null, CultureInfo.CurrentCulture));
            Assert.IsTrue(createViewCalled);
        }

        #region Mocks

        private class MockCollectionViewFactory : ICollectionViewFactory
        {
            private readonly Func<ICollectionView> _createViewCallback;

            public MockCollectionViewFactory(Func<ICollectionView> createViewCallback)
            {
                this._createViewCallback = createViewCallback;
            }

            public ICollectionView CreateView()
            {
                return this._createViewCallback();
            }
        }

        private class DesignInstanceListOfProducts : List<Product>
        {
        }

        #endregion
    }
}
