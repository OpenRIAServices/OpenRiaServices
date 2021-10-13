using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// A dictionary of parameter name and types for a method
    /// </summary>
    public class MethodParameters 
    {
        private readonly string _methodName;
        private readonly Dictionary<string, Type> _parameterNameAndTypeDictionary;

        /// <summary>
        /// Set up a new method parameters dictionary
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="parameters">The reflection parameters for method</param>
        public MethodParameters(string methodName, System.Reflection.ParameterInfo[] parameters)
        {
            _parameterNameAndTypeDictionary = new Dictionary<string, Type>(parameters.Length);
            // For loop to skip the two last default arguments cref="AsyncCallback" callback and cref="object" asyncState
            for (var i = 0; i < parameters.Length - 2; i++)
                _parameterNameAndTypeDictionary.Add(parameters[i].Name, parameters[i].ParameterType);

            _methodName = methodName;
        }

        /// <summary>
        /// Get the type for a given method parameter
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetTypeForMethodParameter(string name)
        {
            if (!_parameterNameAndTypeDictionary.TryGetValue(name, out var parameterType))
                throw new MissingMethodException(string.Format(CultureInfo.InvariantCulture, Resources.BinaryXMLContents_NoParameterWithName0ForMethod1, name, _methodName));

            return parameterType;
        }
    }
}
