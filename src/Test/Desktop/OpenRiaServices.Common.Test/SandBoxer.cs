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
