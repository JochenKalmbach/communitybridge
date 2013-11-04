using System.Diagnostics;

namespace CommunityForumsNNTPServer.WebServiceDataSource
{
  public static class Traces
  {
    public readonly static TraceSource WebService = new TraceSource("WebService");

    public static void WebService_TraceEvent(TraceEventType eventType, int id, string message)
    {
      WebService.TraceEvent(eventType, id, message);
      WebService.Flush();
    }
    public static void WebService_TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
    {
      WebService.TraceEvent(eventType, id, format, args);
      WebService.Flush();
    }
  }
}
