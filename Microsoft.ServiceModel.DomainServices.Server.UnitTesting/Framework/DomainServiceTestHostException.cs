using System;

namespace Microsoft.ServiceModel.DomainServices.Server.UnitTesting
{
    /// <summary>
    /// <see cref="Exception"/> thrown by the <see cref="DomainServiceTestHost{TDomainService}"/>
    /// when an operation error occurs
    /// </summary>
    public class DomainServiceTestHostException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHostException"/>
        /// </summary>
        public DomainServiceTestHostException()
            : base()
        {        
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHostException"/> with the
        /// specified message
        /// </summary>
        /// <param name="message">The error message</param>
        public DomainServiceTestHostException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainServiceTestHostException"/>
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public DomainServiceTestHostException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
