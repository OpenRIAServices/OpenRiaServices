#if !SILVERLIGHT
using System;
#endif

using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace System.ServiceModel.DomainServices
{
    internal static class ExceptionHandlingUtility
    {
        /// <summary>
        /// Determines if an <see cref="Exception"/> is fatal and therefore should not be handled.
        /// </summary>
        /// <example>
        /// try
        /// {
        ///     // Code that may throw
        /// }
        /// catch (Exception ex)
        /// {
        ///     if (ex.IsFatal())
        ///     {
        ///         throw;
        ///     }
        ///     
        ///     // Handle exception
        /// }
        /// </example>
        /// <param name="exception">The exception to check</param>
        /// <returns><c>true</c> if the exception is fatal, otherwise <c>false</c>.</returns>
        public static bool IsFatal(this Exception exception)
        {
            Exception outerException = null;
            while (exception != null)
            {
                if (IsFatalExceptionType(exception))
                {
                    Debug.Assert(outerException == null || ((outerException is TypeInitializationException) || (outerException is TargetInvocationException)), 
                        "Fatal nested exception found");    
                    return true;
                }
                outerException = exception;
                exception = exception.InnerException;
            }
            return false;
        }

        private static bool IsFatalExceptionType(Exception exception)
        {
            if ((exception is ThreadAbortException) ||
#if SILVERLIGHT
                (exception is OutOfMemoryException))
#else
                ((exception is OutOfMemoryException) && !(exception is InsufficientMemoryException)))
#endif
            {
                return true;
            }
            return false;
        }
    }
}