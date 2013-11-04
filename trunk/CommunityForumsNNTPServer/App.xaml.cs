using System;
using System.IO;
using System.Windows;
using System.Threading;

namespace CommunityForumsNNTPServer
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
    public partial class App
  {
        Mutex _singleInstanceMutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            bool ok;
            _singleInstanceMutex = new Mutex(true, "CommunityForumsNNTPBridge", out ok);

            if (!ok)
            {
                // Programm is already running, shutdown the current...
                Current.Shutdown();
                return;
            }

            // Initialize logging and exception handling...
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _logFile = new LogFile(UserSettings.Default.BasePath);

            base.OnStartup(e);
        }

        private LogFile _logFile;

      static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
          string expMsg = NNTPServer.Traces.ExceptionToString(e.ExceptionObject as Exception);
          if (string.IsNullOrEmpty(expMsg))
            expMsg = "<Unknown Exception>";
          Traces.Main_TraceEvent(System.Diagnostics.TraceEventType.Critical, 1, "UnhandledException: {0}",
                                 expMsg);

            MessageBox.Show(string.Format("UnhandledException occured: {0}", NNTPServer.Traces.ExceptionToString(e.ExceptionObject as Exception)));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_singleInstanceMutex != null)
                GC.KeepAlive(_singleInstanceMutex);
        }
    }
}
