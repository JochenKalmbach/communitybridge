using System;
using System.Text;
using System.Diagnostics;

namespace CommunityForumsNNTPServer.NNTPServer
{
    public static class Traces
    {
        public static readonly TraceSource NntpServer = new TraceSource("NNTPServer");

        public static void NntpServerTraceEvent(TraceEventType eventType, string message)
        {
          NntpServer.TraceEvent(eventType, 1, message);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, string format, params object[] args)
        {
          NntpServer.TraceEvent(eventType, 1, format, args);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, Client client, string message)
        {
          if (client != null)
          {
              message = string.Format("[Client: {0}] ", client.ClientNumber) + message;
          }
          NntpServer.TraceEvent(eventType, 1, message);
          NntpServer.Flush();
        }
        public static void NntpServerTraceEvent(TraceEventType eventType, Client client, string format, params object[] args)
        {
          if (client != null)
          {
            format = string.Format("[Client: {0}] ", client.ClientNumber) + format;
          }
          NntpServer.TraceEvent(eventType, 1, format, args);
          NntpServer.Flush();
        }

        public static string ExceptionToString(Exception exp)
        {
            if (exp == null) return string.Empty;
            
            var sb = new StringBuilder();
            while (exp != null)
            {
                sb.Append("Exception:");
                sb.AppendLine();
                var exT = exp.GetType();
                if (exT != null)
                {
                    sb.AppendFormat("Type {0}", exT.FullName);
                    sb.AppendLine();
                }
                sb.AppendFormat("Source: {0}", exp.Source);
                sb.AppendLine();
                sb.AppendFormat("Message: {0}", exp.Message);
                sb.AppendLine();
                var se = exp as System.Net.Sockets.SocketException;
                if (se != null)
                {
                    sb.AppendFormat("Socket ErrorCode: {0}", se.ErrorCode);
                    sb.AppendLine();
                }
                sb.Append("Stack-Trace:");
                sb.AppendLine();
                sb.Append(exp.StackTrace);
                exp = exp.InnerException;
            }
            return sb.ToString();
        }

    }
}
