using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Common.Test
{
    /// <summary>
    /// Helper class to sandbox code to run in partial trust
    /// </summary>
    public class SandBoxer : MarshalByRefObject
    {
#if NETFRAMEWORK
        /// <summary>
        /// Executes the given <paramref name="action"/>  in partial trust.
        /// </summary>
        /// <remarks>The given action must be static, because it will be called in the context
        /// of a new AppDomain.
        /// </remarks>
        /// <param name="action">Action to invoke in partial trust.</param>
        [SecuritySafeCritical]
        public static void ExecuteInMediumTrust(Action action)
        {
#if !MEDIUM_TRUST
            Assert.Inconclusive("Medium trust is obsolete");
#endif

            SandBoxer.ExecuteInMediumTrust(action.Method, null);
        }

        /// <summary>
        /// Executes the given <paramref name="action"/>  in partial trust, passing it the given <paramref name="parameter"/>
        /// </summary>
        /// <remarks>The given action must be static, because it will be called in the context
        /// of a new AppDomain.
        /// </remarks>
        /// <typeparam name="T">The type of <paramref name="parameter"/></typeparam>
        /// <param name="action">Action to invoke in partial trust.</param>
        /// <param name="parameter">The parameter value to pass to the action</param>
        [SecuritySafeCritical]
        public static void ExecuteInMediumTrust<T>(Action<T> action, T parameter)
        {
            SandBoxer.ExecuteInMediumTrust(action.Method, parameter);
        }

        /// <summary>
        /// Executes the given <paramref name="methodInfo"/> in partial trust, giving it
        /// the specified <paramref name="parameter"/> if its signature indicates it accepts a parameter.
        /// </summary>
        /// <remarks>The given delegate must be static, because it will be called in the context
        /// of a new AppDomain.
        /// </remarks>
        /// <param name="methodInfo">Method to invoke in partial trust.</param>
        /// <param name="parameter">Parameter to pass to the method.</param>
        [SecuritySafeCritical]
        public static void ExecuteInMediumTrust(MethodInfo methodInfo, object parameter)
        {
#if !MEDIUM_TRUST
            Assert.Inconclusive("Medium trust is only valid for signed builds");
#endif

            Type type = methodInfo.DeclaringType;

            // Instance methods are not supported because in common use, caller is in full trust
            if (!methodInfo.IsStatic)
            {
                throw new ArgumentException("The method " + type.Name + "." + methodInfo.Name + " must be static");
            }

            if (methodInfo.GetParameters().Length > 1)
            {
                throw new ArgumentException("The method " + type.Name + "." + methodInfo.Name + " accepts an unexpected number of parameters");
            }

            string methodName = methodInfo.Name;
            string typeName = methodInfo.DeclaringType.FullName;
            string assemblyName = methodInfo.DeclaringType.Assembly.FullName;
            string pathToAssembly = methodInfo.DeclaringType.Assembly.Location;

            // TODO (wilcob, roncain): Parse web_mediumtrust.config for a 100% correct sandboxed appdomain.
            // Use Intranet permission because that is how our framework is run in ASP.NET
            Evidence evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(SecurityZone.Intranet));
 
            PermissionSet permSet = SecurityManager.GetStandardSandbox(evidence);

            // We want the sandboxer assembly's strong name, so that we can add it to the full trust list
            StrongName sandBoxAssembly = typeof(SandBoxer).Assembly.Evidence.GetHostEvidence<StrongName>();

            AppDomainSetup adSetup = new AppDomainSetup();
            adSetup.ApplicationBase = Path.GetFullPath(pathToAssembly);

            // We need the following to be fully trusted:
            //  SandBoxer: does Reflection to invoke down to partial trust
            StrongName[] fullTrustAssemblies = new StrongName[] {
                sandBoxAssembly,
            };

            // DataAnnotations is in the GAC and will run full trust unless we ask otherwise
            string[] partialTrustAssemblies = new string[] {
                typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.FullName,
            };

            adSetup.PartialTrustVisibleAssemblies = partialTrustAssemblies;

            AppDomain newDomain = AppDomain.CreateDomain("SandBox", null, adSetup, permSet, fullTrustAssemblies);

            // Use CreateInstanceFrom to load an instance of the Sandboxer class into the new AppDomain. 
            ObjectHandle handle = Activator.CreateInstanceFrom(
                newDomain, typeof(SandBoxer).Assembly.ManifestModule.FullyQualifiedName,
                typeof(SandBoxer).FullName
                );

            // Unwrap the new domain instance into an reference in this domain and use it to 
            // execute the untrusted code
            SandBoxer newDomainInstance = handle.Unwrap() as SandBoxer;
            Exception exception = null;
 
            try
            {
                newDomainInstance.ExecuteUntrustedCode(assemblyName, typeName, methodName, parameter);
            }
            catch (TargetInvocationException tie)
            {
                exception = tie.InnerException == null ? tie : tie.InnerException;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception != null)
            {
                throw (exception is AssertFailedException)
                          ? exception
                          : new AssertFailedException(exception.Message, exception);
            }
        }
#endif

        /// <summary>
        /// Invokes the specified <paramref name="methodName"/> declared in the given <paramref name="typeName"/>
        /// in the given <paramref name="assemblyName"/> in medium trust, passing the method the given <paramref name="parameters"/>
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to load.</param>
        /// <param name="typeName">The public type in this assembly.</param>
        /// <param name="methodName">The name of the method to invoke.  It must be static.</param>
        /// <param name="parameters">If not null, the parameters to pass to the method</param>
        /// <returns><c>null</c> for success, otherwise whatever error was reported.</returns>
        [SecuritySafeCritical]
        public void ExecuteUntrustedCode(string assemblyName, string typeName, string methodName, object parameter)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            Type type = (assembly != null) ? assembly.GetType(typeName) : null;
            MethodInfo target = (type != null) ? type.GetMethod(methodName) : null;
           if (target == null)
            {
                throw new ArgumentException("Unable to locate " + typeName + "." + methodName + " in " + assemblyName);
            }
            else
            {
                try
                {
                    if (target.GetParameters().Length == 0)
                    {
                        target.Invoke(null, null);
                    }
                    else
                    {
                        target.Invoke(null, new object[] { parameter });
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                    {
                        ex = ex.InnerException;
                    }

                    // Here's the real reason we catch at all.  The real AssertFailedException
                    // cannot deserialize across AppDomains correctly, so we convert it into
                    // one that can.
                    if (ex is AssertFailedException)
                    {
                        throw new SandBoxerException(ex.Message);
                    }
                    throw ex;
                }
            }
        }
    }

    /// <summary>
    /// Exception type used to raise unit test assertion failures
    /// from partial trust code.
    /// </summary>
    [SecuritySafeCritical]
    [Serializable]
    public class SandBoxerException : Exception
    {
        public SandBoxerException(string message)
            : base(message)
        {
        }
        private SandBoxerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }
}
