using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
#if !SILVERLIGHT
using System.Web;
#endif


namespace OpenRiaServices.Client.Test
{
    public delegate void GenericDelegate();
    public delegate Task AsyncDelegate();

    [DebuggerStepThrough]
    [System.Security.SecuritySafeCritical]  // Because our assembly is [APTCA] and we are used from partial trust tests
    public static class ExceptionHelper
    {
        private static TException ExpectExceptionHelper<TException>(GenericDelegate del) where TException : Exception
        {
            return ExpectExceptionHelper<TException>(del, false);
        }

        private static TException ExpectExceptionHelper<TException>(GenericDelegate del, bool allowDerivedExceptions)
            where TException : Exception
        {
            try
            {
                del();
            }
            catch (TException e)
            {
                if (!allowDerivedExceptions)
                {
                    Assert.AreEqual(typeof(TException), e.GetType());
                }
                return e;
            }
            catch (TargetInvocationException e)
            {
                TException te = e.InnerException as TException;
                if (te == null)
                {
                    // Rethrow if it's not the right type
                    throw;
                }
                if (!allowDerivedExceptions)
                {
                    Assert.AreEqual(typeof(TException), te.GetType());
                }
                return te;
            }
            // HACK: (ron) Starting with VS 2008 SP1, I found the "catch (TException)" is not always catching the expected type
            // when running under the VS debugger.  Added what looks like redundant code below, since that's the catch block
            // that's actually receiving the TException thrown.
            catch (Exception ex)
            {
                TException te = ex as TException;
                if (te != null)
                {
                    if (!allowDerivedExceptions)
                    {
                        Assert.AreEqual(typeof(TException), ex.GetType());
                    }
                }
                else if (ex is AssertFailedException)
                {
                    throw;
                }
                else
                {
                    Type tExpected = typeof(TException);
                    Type tActual = ex.GetType();
                    Assert.Fail("Expected " + tExpected.Name + " but caught " + tActual.Name);
                }
                return te;
            }

            Assert.Fail("Expected exception of type " + typeof(TException) + ".");
            throw new Exception("can't happen");
        }

        public static TException ExpectException<TException>(GenericDelegate del) where TException : Exception
        {
            return ExpectException<TException>(del, false);
        }


#if !SILVERLIGHT
        private static Task ExecuteDelegateAsync(AsyncDelegate asyncDelegate)
        {
            try
            {
                return asyncDelegate();
            }
            catch (Exception ex)
            {
                var tcs = new TaskCompletionSource<int>();
                tcs.SetException(ex);
                return tcs.Task;
            }
        }

        public static Task<TException> ExpectExceptionAsync<TException>(AsyncDelegate asyncDel, bool allowDerivedExceptions = false) where TException : Exception
        {
            return ExecuteDelegateAsync(asyncDel)
                .ContinueWith(res =>
                {
                    return ExpectException<TException>(() => res.GetAwaiter().GetResult(), allowDerivedExceptions);
                });
        }

        public static Task<TException> ExpectExceptionAsync<TException>(AsyncDelegate del, string exceptionMessage)
                                                       where TException : Exception
        {
            var task = ExpectExceptionAsync<TException>(del);
            // Only check exception message on English build and OS, since some exception messages come from the OS
            // and will be in the native language.
            if (UnitTestHelper.EnglishBuildAndOS)
            {
                task = task.ContinueWith(res =>
                {
                    var ex = res.GetAwaiter().GetResult();
                    Assert.AreEqual(exceptionMessage, ex.Message, "Incorrect exception message.");
                    return ex;
                });
            }
            return task;
        }
#endif

        public static TException ExpectException<TException>(GenericDelegate del, bool allowDerivedExceptions)
            where TException : Exception
        {
            if (typeof(ArgumentNullException).IsAssignableFrom(typeof(TException)))
            {
                throw new InvalidOperationException(
                    "ExpectException<TException>() cannot be used with exceptions of type 'ArgumentNullException'. " +
                    "Use ExpectArgumentNullException() instead.");
            }
            else if (typeof(ArgumentException).IsAssignableFrom(typeof(TException)))
            {
                throw new InvalidOperationException(
                    "ExpectException<TException>() cannot be used with exceptions of type 'ArgumentException'. " +
                    "Use ExpectArgumentException() instead.");
            }
            return ExpectExceptionHelper<TException>(del, allowDerivedExceptions);
        }

        public static TException ExpectException<TException>(GenericDelegate del, string exceptionMessage)
                                                       where TException : Exception
        {
            TException e = ExpectException<TException>(del);
            // Only check exception message on English build and OS, since some exception messages come from the OS
            // and will be in the native language.
            if (UnitTestHelper.EnglishBuildAndOS)
            {
                Assert.AreEqual(exceptionMessage, e.Message, "Incorrect exception message.");
            }
            return e;
        }

        public static ArgumentException ExpectArgumentException(GenericDelegate del, string exceptionMessage)
        {
            ArgumentException e = ExpectExceptionHelper<ArgumentException>(del);
            // Only check exception message on English build and OS, since some exception messages come from the OS
            // and will be in the native language.
            if (UnitTestHelper.EnglishBuildAndOS)
            {
                Assert.AreEqual(exceptionMessage, e.Message, "Incorrect exception message.");
            }
            return e;
        }

        public static ArgumentException ExpectArgumentException(GenericDelegate del, string exceptionMessage, string paramName)
        {
            ArgumentException e = ExpectExceptionHelper<ArgumentException>(del);
            var expectedException = new ArgumentException(exceptionMessage, paramName);
            Assert.AreEqual(expectedException.Message, e.Message);
            return e;
        }

        public static ArgumentNullException ExpectArgumentNullExceptionStandard(GenericDelegate del, string paramName)
        {
            ArgumentNullException e = ExpectExceptionHelper<ArgumentNullException>(del);
            var expectedException = new ArgumentNullException(paramName);
            Assert.AreEqual(expectedException.Message, e.Message);
#if !SILVERLIGHT
            Assert.AreEqual(paramName, e.ParamName, "Incorrect exception parameter name.");
#endif
            return e;
        }

        public static ArgumentNullException ExpectArgumentNullException(GenericDelegate del, string paramName)
        {
            return ExpectArgumentNullExceptionStandard(del, paramName);
        }

        public static ArgumentNullException ExpectArgumentNullException(GenericDelegate del, string exceptionMessage, string paramName)
        {
            ArgumentNullException e = ExpectExceptionHelper<ArgumentNullException>(del);
            var expectedException = new ArgumentNullException(paramName, exceptionMessage);
            Assert.AreEqual(expectedException.Message, e.Message);
            return e;
        }

        public static ArgumentOutOfRangeException ExpectArgumentOutOfRangeException(GenericDelegate del, string paramName, string exceptionMessage)
        {
            ArgumentOutOfRangeException e = ExpectExceptionHelper<ArgumentOutOfRangeException>(del);
#if !SILVERLIGHT
            Assert.AreEqual(paramName, e.ParamName, "Incorrect exception parameter name.");
#endif
            // Only check exception message on English build and OS, since some exception messages come from the OS
            // and will be in the native language.
            if (exceptionMessage != null && UnitTestHelper.EnglishBuildAndOS)
            {
                Assert.AreEqual(exceptionMessage, e.Message, "Incorrect exception message.");
            }
            return e;
        }

        public static InvalidOperationException ExpectInvalidOperationException(GenericDelegate del, string message)
        {
            InvalidOperationException e = ExpectExceptionHelper<InvalidOperationException>(del);
            Assert.AreEqual(message, e.Message);
            return e;
        }

        public static ValidationException ExpectValidationException(GenericDelegate del, string message, Type validationAttributeType, object value)
        {
            ValidationException e = ExpectExceptionHelper<ValidationException>(del);
            Assert.AreEqual(message, e.Message);
            Assert.AreEqual(value, e.Value);
            Assert.IsNotNull(e.ValidationAttribute);
            Assert.AreEqual(validationAttributeType, e.ValidationAttribute.GetType());
            return e;
        }

#if !SILVERLIGHT
        public static HttpException ExpectHttpException(GenericDelegate del, string message, int errorCode)
        {
            HttpException e = ExpectExceptionHelper<HttpException>(del);
            Assert.AreEqual(message, e.Message);
            Assert.AreEqual(errorCode, e.GetHttpCode());
            return e;
        }
#endif

        public static WebException ExpectWebException(GenericDelegate del, string message, int errorCode)
        {
            WebException e = ExpectExceptionHelper<WebException>(del);
            Assert.AreEqual(message, e.Message);

            HttpWebResponse response = e.Response as HttpWebResponse;
            Assert.AreEqual(errorCode, response.StatusCode);
            return e;
        }

        public static WebException ExpectWebException(GenericDelegate del, string message, WebExceptionStatus webExceptionStatus)
        {
            WebException e = ExpectExceptionHelper<WebException>(del);

#if !SILVERLIGHT // Message is set to "" in Silverlight in some cases
            Assert.AreEqual(message, e.Message);
#endif

            Assert.AreEqual(webExceptionStatus, e.Status);
            return e;
        }
    }
}
