using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// A dictionary of parameter name and types for method
    /// </summary>
    public class MethodParameters : Dictionary<string, Type>
    {
        /// <summary>
        /// Set up a new method parameters dictionary
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="parameters">Reflection parameters</param>
        public MethodParameters(string methodName, System.Reflection.ParameterInfo[] parameters)
        : base(parameters.Length)
        {
            for (var i = 0; i < parameters.Length - 2; i++)
                Add(parameters[i].Name, parameters[i].ParameterType);

            MethodName = methodName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetTypeForMethodParameter(string name)
        {
            if (!this.TryGetValue(name, out var parameterType))
                throw new MissingMethodException(string.Format(CultureInfo.InvariantCulture, Resources.BinaryXMLContents_NoParameterWithName0ForMethod1, name, MethodName));

            return parameterType;
        }

        /// <summary>
        /// Method name
        /// </summary>
        public string MethodName { get; private set; }
    }
}
