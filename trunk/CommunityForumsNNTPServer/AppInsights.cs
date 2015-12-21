using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ApplicationInsights;

namespace CommunityForumsNNTPServer
{
    sealed class AppInsights : IDisposable
    {
        public AppInsights(string key, string appName, string accountId, AppInsightsLevel level)
        {
            _Level = level;
            if (_Level != AppInsightsLevel.None)
            {
                _Client = new TelemetryClient();
                _Client.InstrumentationKey = key;

                // Set some context infos
                _Client.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                _Client.Context.Session.Id = Guid.NewGuid().ToString();
                _Client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
                _Client.Context.Device.Language = System.Globalization.CultureInfo.InstalledUICulture.ToString();
                _Client.Context.Operation.Name = appName;
                // Only use a GUID to identify this installation...
                _Client.Context.User.AccountId = accountId;
                _Client.Context.User.Id = accountId;
                // do not use the real user id...
                //_Client.Context.User.Id = Environment.GetUserName();

                _Client.TrackPageView("Main");
            }
        }

        static TelemetryClient _Client;
        static AppInsightsLevel _Level;

        public void Dispose()
        {
            if (_Client != null)
            {
                _Client.Flush(); // only for desktop apps
                _Client = null;

                // Allow time for flushing:
                System.Threading.Thread.Sleep(1000);
            }
        }

        public static void TrackException(Exception exp, bool handled = false)
        {
            if (_Level >= AppInsightsLevel.Default)
            {
                if (_Client != null)
                {
                    var td = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(exp);
                    if (handled)
                    {
                        td.HandledAt = Microsoft.ApplicationInsights.DataContracts.ExceptionHandledAt.UserCode;
                    }
                    _Client.TrackException(exp);
                }
            }
        }

        public static void TrackRequest(string name, DateTimeOffset start, TimeSpan ts, bool success, int? count = null, string response = null)
        {
            if (_Level >= AppInsightsLevel.Detailed)
            {
                if (response == null)
                    response = success ? "OK" : "FAILED";
                //var rt = new Microsoft.ApplicationInsights.DataContracts.RequestTelemetry(name, start, ts, response, success);
                var rt = new Microsoft.ApplicationInsights.DataContracts.PageViewTelemetry(name);
                rt.Name = rt.Name + "-" + response;
                rt.Duration = ts;
                rt.Timestamp = start;
                //rt., start, ts, response, success);
                rt.Properties.Add("Success", success.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (count != null)
                {
                    rt.Properties.Add("Count", count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                _Client.TrackPageView(rt);
                //_Client.TrackRequest(rt);
            }
        }
    }

    public enum AppInsightsLevel
    {
        /// <summary>
        ///  Disable Application Insights
        /// </summary>
        None,

        /// <summary>
        /// Log only session information
        /// </summary>
        Session,

        /// <summary>
        /// Log session information and exceptions
        /// </summary>
        Default,

        /// <summary>
        /// Log session information, exceptions and SOAP-usage
        /// </summary>
        Detailed,
    }
}
