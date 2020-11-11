// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project. 
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc. 
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File". 
// You do not need to add suppressions to this file manually. 

using System.Diagnostics.CodeAnalysis;
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "OpenRiaServices.Hosting.Local", Justification = "Matches other framework assemblies")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "OpenRiaServices.Hosting", Justification = "Matches other framework assemblies")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "It will be signed")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.ServiceUtils.#AuthenticationScheme", Justification = "Disposing inspector will attempt to close the host and result in an exception.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.ServiceUtils.#CredentialType", Justification = "Disposing inspector will attempt to close the host and result in an exception.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "domainServiceDescription", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.ODataEndpointFactory.#CreateEndpointForAddress(OpenRiaServices.Server.DomainServiceDescription,System.ServiceModel.Description.ContractDescription,System.Uri)", Justification = "The DSD will be useful when more functionality is supported.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.DomainDataServiceQueryOperationBehavior`1+DomainDataServiceQueryOperationInvoker.#domainServiceDescription", Justification = "The DSD will be useful when more functionality is supported.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.DomainDataServiceQueryStringConverter.#TryKeyStringToPrimitive(System.String,System.Type,System.Object&)", Justification = "It's really just a big switch statement.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.HttpProcessUtils+MediaType.#Parameters", Justification = "This is available since it was a constructor parameter.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "OpenRiaServices.Hosting.WCF.OData.DomainDataServiceException", Justification = "This internal exception is not intended to be serializable.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "OpenRiaServices.Hosting.WCF.OData.DomainDataServiceException", Justification = "This internal exception is not intended to be serializable.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "OpenRiaServices.Hosting.WCF.OData.DomainDataServiceDispatchMessageFormatter.#SerializeReply(System.ServiceModel.Channels.MessageVersion,System.Object[],System.Object)", Justification = "The message will dispose the stream")]
