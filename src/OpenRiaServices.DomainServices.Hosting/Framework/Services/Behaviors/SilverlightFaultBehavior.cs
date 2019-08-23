using System.ServiceModel;
using OpenRiaServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace OpenRiaServices.DomainServices.Hosting
{
    // Taken from http://msdn.microsoft.com/en-us/library/dd470096(VS.95).aspx.
    internal class SilverlightFaultBehavior : IEndpointBehavior
    {
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SilverlightFaultMessageInspector());
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        private class SilverlightFaultMessageInspector : IDispatchMessageInspector
        {
            public void BeforeSendReply(ref Message reply, object correlationState)
            {
                if (reply != null && reply.IsFault)
                {
                    HttpResponseMessageProperty property = new HttpResponseMessageProperty();

                    // Here the response code is changed to 200.
                    property.StatusCode = System.Net.HttpStatusCode.OK;


                    reply.Properties[HttpResponseMessageProperty.Name] = property;
                }
            }

            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
            {
                // Do nothing to the incoming message.
                return null;
            }
        }
    }
}
