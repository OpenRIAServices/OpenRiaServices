using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows.Common
{
    /// <summary>
    /// Utility class for DependencyObject/DependencyProperty related operations
    /// </summary>
    internal static class DependencyObjectExtensions
    {
        #region Static Fields and Constants

        private static Dictionary<DependencyObject, Dictionary<DependencyProperty, bool>> _suspendedHandlers =
            new Dictionary<DependencyObject, Dictionary<DependencyProperty, bool>>();

        #endregion Static Fields and Constants

        #region Static Methods

        /// <summary>
        /// Determines whether a dependency object has suspended the change handler for the provided dependency property.
        /// </summary>
        /// <param name="obj">The <see cref="DependencyObject"/> to examine.</param>
        /// <param name="dependencyProperty">The <see cref="DependencyObject"/>'s <see cref="DependencyProperty"/> to examine.</param>
        /// <returns>True when the change handler is suspended.</returns>
        public static bool IsHandlerSuspended(this DependencyObject obj, DependencyProperty dependencyProperty)
        {
            Debug.Assert(obj != null, "Unexpected null obj");
            if (_suspendedHandlers.ContainsKey(obj))
            {
                return _suspendedHandlers[obj].ContainsKey(dependencyProperty);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Suspends the change handler of a dependency property, sets the property value and reactivates the
        /// change handler.
        /// </summary>
        /// <param name="obj">The <see cref="DependencyObject"/> to update without triggering a change event.</param>
        /// <param name="property">The <see cref="DependencyObject"/>'s <see cref="DependencyProperty"/> to set.</param>
        /// <param name="value">The value that <paramref name="property"/> should be set to.</param>
        public static void SetValueNoCallback(this DependencyObject obj, DependencyProperty property, object value)
        {
            Debug.Assert(obj != null, "Unexpected null obj");
            obj.SuspendHandler(property, true);
            try
            {
                obj.SetValue(property, value);
            }
            finally
            {
                obj.SuspendHandler(property, false);
            }
        }

        private static void SuspendHandler(this DependencyObject obj, DependencyProperty dependencyProperty, bool suspend)
        {
            if (_suspendedHandlers.ContainsKey(obj))
            {
                Dictionary<DependencyProperty, bool> suspensions = _suspendedHandlers[obj];

                if (suspend)
                {
                    Debug.Assert(!suspensions.ContainsKey(dependencyProperty), "suspensions unexpectedly contain dependencyProperty");
                    suspensions[dependencyProperty] = true; // true = dummy value
                }
                else
                {
                    Debug.Assert(suspensions.ContainsKey(dependencyProperty), "suspensions unexpectedly do not contain dependencyProperty");
                    suspensions.Remove(dependencyProperty);
                    if (suspensions.Count == 0)
                    {
                        _suspendedHandlers.Remove(obj);
                    }
                }
            }
            else
            {
                Debug.Assert(suspend, "suspend unexpectedly false");
                _suspendedHandlers[obj] = new Dictionary<DependencyProperty, bool>();
                _suspendedHandlers[obj][dependencyProperty] = true;
            }
        }

        #endregion Static Methods
    }
}
