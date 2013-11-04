using System.Diagnostics;

namespace CommunityForumsNNTPServer
{
    public static class Traces
    {
        public readonly static TraceSource Main = new TraceSource("Main");

        public static void Main_TraceEvent(TraceEventType eventType, int id, string message)
        {
            Main.TraceEvent(eventType, id, message);
            Main.Flush();
        }
        public static void Main_TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            Main.TraceEvent(eventType, id, format, args);
            Main.Flush();
        }
    }
}
