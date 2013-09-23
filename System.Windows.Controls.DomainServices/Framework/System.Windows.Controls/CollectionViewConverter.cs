using System.Collections.Generic;
using System.ComponentModel;
using OpenRiaServices.DomainServices;
using OpenRiaServices.DomainServices.Client;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>
    /// Converter that creates an <see cref="ICollectionView"/> instance from an
    /// <see cref="IEnumerable{T}"/> value where the type derives from <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// If the value is already an <see cref="ICollectionView"/>, then it will be returned as-is.
    /// <para>
    /// If the value provided implements <see cref="ICollectionViewFactory"/>, then
    /// <see cref="ICollectionViewFactory.CreateView"/> will be called to create the view.
    /// </para>
    /// <para>
    /// If the value is neither an <see cref="ICollectionView"/> nor an <see cref="ICollectionViewFactory"/>,
    /// then an <see cref="PagedEntityCollectionView{TEntity}"/> will be created and returned.
    /// </para>
    /// </remarks>
    internal class CollectionViewConverter : IValueConverter
    {
        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="EntityCollectionView{T}"/> if
        /// the type is an <see cref="Entity"/> type.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The System.Type of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object Convert(object value, Type targetType, object parameter, Globalization.CultureInfo culture)
        {
            if (value == null || value is ICollectionView)
            {
                return value;
            }

            ICollectionViewFactory factory = value as ICollectionViewFactory;
            if (factory != null)
            {
                return factory.CreateView();
            }

            // Determine if the value is an IEnumerable<T>
            Type entityType;

            if (IsEnumerableEntityType(value.GetType(), out entityType))
            {
                // Create the appropriate EntityCollectionView<T> type
                Type viewType = typeof(PagedEntityCollectionView<>).MakeGenericType(entityType);

                // Use the constructor that accepts the IEnumerable<T> source to construct the EntityCollectionView
                return Activator.CreateInstance(viewType, value);
            }

            return value;
        }

        /// <summary>
        /// Unimplemented conversion back to the target type.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The System.Type of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>The value to be passed to the source object.</returns>
        /// <exception cref="NotImplementedException">Any time this method is called.  It is not implemented.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines if the specified <paramref name="type"/> represents a type that implements
        /// <see cref="IEnumerable{T}"/> where T : <see cref="Entity"/>.  When <c>true</c>, provide
        /// the type of <see cref="Entity"/> as the output parameter <paramref name="entityType"/>.
        /// </summary>
        /// <param name="type">
        /// The type to test for <see cref="IEnumerable{T}"/> support where T : <see cref="Entity"/>.
        /// </param>
        /// <param name="entityType">
        /// When the return value is <c>true</c>, this parameter will be set to the type of
        /// <see cref="Entity"/> that is represented in the <see cref="IEnumerable{T}"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="type"/> implements <see cref="IEnumerable{T}"/>
        /// where T : <see cref="Entity"/>, otherwise <c>false</c>.
        /// </returns>
        internal static bool IsEnumerableEntityType(Type type, out Type entityType)
        {
            entityType = null;

            // Determine if the type implements IEnumerable<T>, getting the IEnumerable<T>
            // type if it does so that the T can be extracted.
            Type enumerableType;
            if (!typeof(IEnumerable<>).DefinitionIsAssignableFrom(type, out enumerableType))
            {
                return false;
            }

            // Extract the T from IEnumerable<T>
            Type elementType = enumerableType.GetGenericArguments()[0];

            // Ensure T : Entity
            if (typeof(Entity).IsAssignableFrom(elementType))
            {
                entityType = elementType;
                return true;
            }

            return false;
        }
    }
}
