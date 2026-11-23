using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace OpenRiaServices.Client.DomainClients.Http
{
    /// <summary>
    /// A dictionary of parameter name and types for a method
    /// </summary>
    internal sealed class MethodParameters
    {
        private readonly string _operationName;
        private readonly Dictionary<string, Type> _parameterNameAndTypeDictionary;

        /// <summary>
        /// Set up a new method parameters dictionary
        /// </summary>
        /// <param name="serviceInterface">Contract interface</param>
        /// <param name="operationName">Method name</param>
        internal MethodParameters(Type serviceInterface, string operationName)
        {
            _operationName = operationName;
            var method = serviceInterface.GetMethod(operationName);
            var legacyMethod = method is null || !typeof(Task).IsAssignableFrom(method.ReturnType);
            if (legacyMethod)
                method = serviceInterface.GetMethod($"Begin{operationName}");

            if (method is null)
                throw new MissingMethodException(string.Format(CultureInfo.InvariantCulture, Resources.DomainClient_Operation0DoesNotExist, operationName));

            var methodParameters = method.GetParameters();
            _parameterNameAndTypeDictionary = new Dictionary<string, Type>(methodParameters.Length);

            // Legacy APM contracts end with callback and asyncState parameters.
            var parameterCount = legacyMethod ? methodParameters.Length - 2 : methodParameters.Length;
            for (var i = 0; i < parameterCount; i++)
                _parameterNameAndTypeDictionary.Add(methodParameters[i].Name, methodParameters[i].ParameterType);
        }

        /// <summary>
        /// Get the type for a given method parameter
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Type of method parameters</returns>
        internal Type GetTypeForMethodParameter(string name)
        {
            if (!_parameterNameAndTypeDictionary.TryGetValue(name, out var parameterType))
                throw new MissingMethodException(string.Format(CultureInfo.InvariantCulture, Resources.BinaryXMLContents_NoParameterWithName0ForMethod1, name, _operationName));

            return parameterType;
        }
    }
}
