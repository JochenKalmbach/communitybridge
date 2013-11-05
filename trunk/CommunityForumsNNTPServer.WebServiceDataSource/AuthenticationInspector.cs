#undef PASSPORT_HEADER_ANALYSIS

using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace CommunityForumsNNTPServer.WebServiceDataSource
{

    public class AuthenticationInspector : IClientMessageInspector, IEndpointBehavior
    {
      private string _passportTicket;

      public string PassportTicket
      {
        get { return _passportTicket; }
        set
        {
#if PASSPORT_HEADER_ANALYSIS
          var sb = new StringBuilder();
          sb.AppendFormat("CheckTicket-Old: {0}", CheckTicket());
          sb.AppendLine("---");
          _passportTicket = value;
          sb.AppendFormat("CheckTicket-New: {0}", CheckTicket());
          Traces.WebService_TraceEvent(TraceEventType.Information, 2, sb.ToString());
#else
          _passportTicket = value;
#endif
        }
      }

      public AuthenticationInspector(string ticket)
        { _passportTicket = ticket; }

        #region IClientMessageInspector Members
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
#if PASSPORT_HEADER_ANALYSIS
          Traces.WebService_TraceEvent(TraceEventType.Information, 3, reply.ToString());
#endif
        }
        public object BeforeSendRequest(ref Message request, System.ServiceModel.IClientChannel channel)
        {
            request.Headers.Add(MessageHeader.CreateHeader("Passport", "ms", PassportTicket));
          
#if PASSPORT_HEADER_ANALYSIS
            Traces.WebService_TraceEvent(TraceEventType.Information, 2, request.ToString());
#endif
            return null;
        }
        #endregion
        #region IEndpointBehavior Members
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
          clientRuntime.MessageInspectors.Add(this);
        }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }
        public void Validate(ServiceEndpoint endpoint) { }
        #endregion

      public string CheckTicket()
      {
        // Check the AuthenticationCode:
        string url = string.Format("https://apis.live.net/v5.0/me?access_token={0}", PassportTicket);
        try
        {
          var req = WebRequest.Create(url);
          var resp = req.GetResponse();
          return resp.Headers.ToString();
        }
        catch (WebException wexp)
        {
          var sb = new StringBuilder();
          sb.Append(wexp.Message);
          if (wexp.Response != null)
          {
            sb.AppendLine();
            sb.Append(wexp.Response.Headers);
          }
          return sb.ToString();
        }
        catch (Exception exp)
        {
          return NNTPServer.Traces.ExceptionToString(exp);
        }
      }
    }

}
