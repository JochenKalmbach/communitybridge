using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityForumsNNTPServer.ArticleConverter;
using CommunityForumsNNTPServer.NNTPServer;
using CommunityForumsNNTPServer.WebServiceDataSource;
using System.Text.RegularExpressions;
using System.Text;

// History:
// 2010-05-25, V01: First release on http://communitybridge.codeplex.com/
// 2010-05-26, V02: 
// - MessageId now also contains the forumId, to support ARTICLE without a selected group
// 2010-05-26, V03: 
// - XOVER now returns sorted list of articles (sorted by article number)
// - XHDR: The "header" name is now case-insensitive / articles are sorted by article number
// - Subject is now base64 with utf-8 encoded
// 2010-05-26, V04:
// - Now listens only to the loopback adpater (127.0.0.1) instead of Any (0.0.0.0)
// - Displays an error message if the port is already in use or some other exception occurs during initialization
// - Bugfix in ARTICLE <id>
// - Tried the new icon
// - From is now base64 with utf-8 encoded
// 2010-05-26, V05:
// - Changed "charset=utf8" to the correct "charset=utf-8"
// 2010-05-26, V06:
// - Removed the ForumId from the MessageId / INFO: This will break the "threaded-view" of existing messages! Only new messages will be displayed in the threaded view!
// - Fixed the missing TimeZoneInfo (+0000) in the "Date" header
// - Adding an "Re: " prefix if the article is a reply and does not have a "Re: " in the subject
//   (for example if the article was replied via the forum)
// - Removed SharepointProvider.cs
// - The X-Newsreader header now reports the correct file version in the string
// - Corrected the Xref header (in the form: LOCALHOST.communitybridge.codeplex.com groupname:messageno)
// - Support for the "LIST NEWSGROUPS" command (for OE)
// - Displaying the file version info in the window title / Better title text
// 2010-05-26, V07:
// - Bugfix, if a thread is posted without retrieving the group first
// - Started to implement tracing...
// - by default the input will be decoded with utf-8; this needs to be improved in the future!
// - Catching Exceptions in prefetching of newsgroups
// 2010-05-27, V08:
// - Regression from V07: LIST might throw an internal exception
// - If no internet connection is available, a group will not be reported as "411 no such news group", now it will report an "503 program fault"
// 2010-05-27, V09:
// - ReSharper "enabled"
// - Prefetching button is now disabled while prefetching is active
// - Disabling the checkboxes will now be recognized
// - Subject/From will only be encoded if non-ASCII characters (>0x7F) are present
// - DetailedErrorResponse is now enabled by default (can be changed in the *.config file)
// - DomainName can now be configured in the *.config file
// - ARTICLE command with an invalid parameter will not reponse with 503, instead it will use 430
// - It is now configurable, if you want to bind the NNTP port to the "world" (default is loopback)
// - Last change of the messageId back to the MS NNTP Bridge format (+ @domain.name)
//   For more info see comments at function "GuidToId"
// - Optimized GROUP response time, at least for "answers" forums
// - Added an X-Comments header with the URL to the thread ;)
// - Renamed the app.config entries to match the service names (social, answers)
// 2010-05-27, V10:
// - The login-thread is now a background thread (which will be terminated if the mainwindow is closed during the login procedure)
// - Added X-Face support for MVPs, Microsofties and Admins
// - User-Settings now contains "UserName", "UserGuid" and "UserEmail". If either "UserName" or "UserGuid" is present (and the UserEmail), 
//   then this info will be used to change the "from" to include the email address.
//   This might be helpfull if you want to find "your" postings.
// 2010-05-28, V11
// - Added the UserName and UserEmail textbox to the GUI
// 2010-05-28, V12
// - app.config contains now a example to enable tracing
// - Setup will now remove previous versions, so only one will be displayed in the control panel (programs)
// - If the first/last parameter for XOVER is in the wrong order, then correct the order (some clients may sent XOVER 234-230 instead of "XOVER 230-234")
//   This has lead to System.ServiceModel.FaultException in the web-service
// - Moved the creation of X-Face and From (with email) into the ArticleConverter
// - Bugfix: Only encode the username of the From-header and not the email address
// - Added tracing for WebService: CreateQuestionThread / CreateReply
// - Removed format=flowed for text/html messages
// 2010-05-28, V13
// - Renamed "X-Comments" to "Archived-At" (see: http://www.ietf.org/rfc/rfc5064.txt) for the URL
// - BallonTooltip will now not be displayed anymore, if the app was minimized... this leads to problem with "auto-hide" of the taskbar
// - Postings are now using the correct encoding, if the charset is specified
// 2010-05-29, V14
// - Now fully supports XOVER from RFC2980 (XOVER n / XOVER n- / XOVER n-m); in addition it also supports XOVER <messageId> and "XOVER n n" / the results are now in the order of the requested numbers
// - Newsgroup descriptions are now not containing any linebreaks
// - Bugfix: A line which started with a dot and then some other characters (excpect \r\) was not correctly translated into one dot (it was displayed as two dots)
//   (all dots as first char must be send via two dots; and then the newsserver must replace the two dots with one...)
// - Bugfix: Now supports multi-line content-type headers to detect the correct encoding
// - Now using "<div>...</div>" instead of "</br"> for linebreaks for new messages.
// - On receiving of web-service articles, replace "<br/>" with "<br />" for better compatibility with Forte Agent 6.00/32.1186
// - Bugfix: XOVER for large ranges are now supported
// 2010-05-29, V15
// - Archived-At is now RFC conform (included in <URI>)
// - Regression: html-link conversion are now working again (failed in V14)
// - Beta feature: UsePlainTextConverter: This will try to convert the forums html messages into plain/text! 
//   This is a beta feature and deactivated by default! Thanks to Josef Poetzl who implemented this!
// - Groupnames may now contain trailing/leading spaces..
// 2010-05-30, V16
// - Rename "Auto start" to "Auto login"
// - Removed guid-email address for other users, because it has no real value for the enduser
// - LIST, XOVER and XHDR are now presenting results while downloading from the web-service (prefious it had lead to timeouts,if you want to download a very amount of range (>300)
// - Bugfix: UsePlainTextConverter was not set correctly after a restart of the program
// 2010-05-31, V17
// - IHAVE now generates 500 Command Not Recognised
// - Now supports DATE command
// - XHDR now returns the article number, except if an articleId was provided, then it will also return the articleId (RFC2980)
// - Added "MIME-Version: 1.0" Header
// - PlainTextConverter: Support for [CODE] tags (see: http://communitybridge.codeplex.com/documentation)
// - PlainTextConverter: Converting <a href.." correctly into link with text
// - PlainTextConverter: Linbreaks at 70 characters from html2text
// - PlainTextConverter: Support for signature line (<hr..> => "-- ")
// 2010-05-31, V18
// - PlainTextConverter: Support for img/i/b html tags / bugfix for hr tags
// - All user settings are now stored in the registry; these settings will survive any update ;)
// - Bugfix: Port was not saved in the user settings
// - Menu added
//   - File | Exit
//   - Tools | Advanced options...: To edit all current options
//   - Tools | Show Debug Window: Displays trace messages (text can be copied via context menu into the clipboard)
//   - Tools | Create LiveId auto login...: Create an authentication blob which will be used the next time the server starts, so no need to use the authentication dialog!
//   - Help | Info: Small info dialog
// 2010-05-31, V19
// - User setting: TextConverter: AutoLineWrap (0: disabled, >0: Number of chars at which a line should automatically break)
// - User setting: EncodingForClient: Optional you can provide an encoding for transmitting data to the newsreader (for example, Forte Agent does not like utf-8 in text/plain, so you can change to "Windows-1252" or whatever you want)
// 2010-06-01, V20
// - Bugfix: Now supporting "null" Body, and other null-strings in the article
// - Limited the number of cached articles (UserSettings: MaxCachedArticles=10000)
// - Bugfixes for user settings
// - PlainTextConverter: Better support for nested markups
// 2010-06-01, V21
// - Bugfix: html-Links now support the "&" and "!" character
//   See also: http://communitybridge.codeplex.com/WorkItem/View.aspx?WorkItemId=6365
// - "EncodingForClient" is now used for Body and Headers. See also: http://communitybridge.codeplex.com/WorkItem/View.aspx?WorkItemId=6383
// - After changing the options, the cache will be cleared so all articles will use the new settings
// - Renamed "Auto login" to "Auto start NNTP server" on the main dialog
// - Corrected category to "Header-Converter" and "PlainText-Converter" to supress confusion
// - Added traces for ArticleConverter and UserName matcher (named Converters)...
// - Bugfix: Completly redesigned the storage of "CurrentGroup" and "CurrentArticle"...
//    See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214370&ANCHOR#Post449268
//    See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214365&ANCHOR#Post449265
//    See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214368&ANCHOR#Post449264
// - Bugfix: Correct support for NEXT and LAST
//   See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214366&ANCHOR#Post449260
// 2010-06-02, V22
// - Better support for mime/multipart messages (WLM) / Now you can decide what will be used: "text/plain" or "text/html" (default).
//   If you want to use the plain/text converter, then you need to change these settings to "TextPlain" instaed of "TextHtml".
// - UserName compare is now done with current locale and case-insensitive
// - Fixed several issues in the plain/text converter (*/_ ...)
// 2010-06-02, V23
// - Added header "X-Comments" with the comment for "IsAdmin; IsMsft; IsMVP; Stars=x; Points=x; Posts=x; Answers=x" if available
// - Now support for NNTP command "NEWGROUPS
//   See: http://communitybridge.codeplex.com/workitem/6401
//   See: http://communitybridge.codeplex.com/workitem/6402
// - Bugfix: NNTP command LAST is now selecting the previous article
//   See: http://communitybridge.codeplex.com/workitem/6403
// - If the user has only msdn or answers enabled it will also work
// 2010-06-03, V24
// - Now you can select if you want to use "Answers" or "Social (msdn, technet, microsoft, expression)" forums or both
// - PlainTextComverter: Support for <ul>, <ol>, <il>
// - Bugfix: Tracing of UserName matches leads to exception if debug window was open
// 2010-06-03, V25
// - Bugfix: Postings without "Content-Type" will use text/plain and will be posted correctly as html to the web-service
//   See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214604
// - PlainTextConverter: Some Bugfixes and improvements with code tags (pre)
// - Started unit tests for PlainTextConverter
// 2010-06-04, V26
// - BugFix: PlainTextConverter: Hyperlinks with nestedt tags are now working
//   See: http://communitybridge.codeplex.com/workitem/6422
// - References header is now only present for replies; not for original posts
//   See: http://communitybridge.codeplex.com/workitem/6424
// - PlainTextConverter: Hyperlinks of the form <a href="http://abc.de/">abc.de</a> are now displayed without the <http://abc.de> tag in plantext; only "http://abc.de/" is displayed
//   See: http://communitybridge.codeplex.com/workitem/6365
// 2010-06-04, V27
// - PlainTextConverter: Various improvements with code tags and html<=>text conversion
// - Transmitting "User-Agent" via <a name title /> element; will be dispayed in the receiving client as X-Newsreader if article was sent via this bridge ;)
// - Added X-NNTPServer header
// 2010-06-04, V28
// - PlainTextConverter: First support for UserDefinedTags (see MarkupGuide: http://communitybridge.codeplex.com/wikipage?title=Markup%20Guide&referringTitle=Documentation)
// - Bugfix: Also use "X-Newsreader" for referencing the newsreader
// - PlainTextConverter: Several improvements
// 2010-06-05, V29
// - Bugfix: Linebreaks in subject had lead to errors
// - RFC conformance: LIST an NEWGROUPS now returning 403 if not connection is available
// - PlainTextConverter: Better support for UserDefinedTags (see MarkupGuide: http://communitybridge.codeplex.com/wikipage?title=Markup%20Guide&referringTitle=Documentation)
// - PlainTextComverter: Detection for signatures
// 2010-06-05, V30
// - Newsreader identification now also works in the "main (first) article"
// - Bugfix: Posting with MIME/multipart had lead to problems if text/plain was used
// - Internak: XPosts enabled: See: http://communitybridge.codeplex.com/workitem/6447
// 2010-06-06, V31
// - Bugfix: Subjects with tabs had lead to problems in agent
// - Better MIME/multipart support (correct support of multi-line headers in mime parts)
// - PlainTextConverter: Several improvements and bugfixes
// - Removed system.net section in app.config files (was accidently introduced in V2?)
// - Internal: XPosts deactivated
// 2010-06-07, V32
// - Bugfix: First dot (.) must be duplicated in sending articles to the client
// - NNTP Conformance: Support for LISTGROUP command, see: http://tools.ietf.org/html/rfc3977#section-6.1.2
// - PlainTextConverter: Several improvements
// 2010-06-08, V33
// - MyThreads: *Automatic detection of my posts; no need to enter Username and email anymore!*
// - Bugfix: On shutdown the XmlSerializer was started which failed because of the shutdown
// 2010-06-08, V34
// - Browsing of the newsgroups with title and description can now be done in the main dialog; search is also possible
// - Removed UserName/UserEmail from the main dialog (it is not needed anymore after adding the auto-detect feature)
// - PlainTextConverter: Several improvements / No longer in beta status...
// 2010-06-09, V35
// - Support for "quoted-printable" and "base64" as "text/plain" content-transfer-type
// - List of newsgroups in the main dialog is now alphabetically sorted
// - DetailedErrorResponse now always enabled and removed from UserSettings
// - Posting result will now also contain the exception text if an error occurred
// - PlainTextConverter: Several improvements
// 2010-06-12, V36
// - You can disable the transmittion of the news-agent to the forum
// - Display of MSFT, MVP, ADMIN in username (only for unkown users; it also can be disabled in the user settings)
// - The command LISTGROUP can be disabled via UserSettings
// - Saving of UserDefinedTags and UserMapping is now done without linbreaks
// - UsePlainTextConverter now has the options "None, OnlySend, OnlyReceiver, SendAndReceive"
// - New UserSetting: PostsAreAlwaysFormatFlowed (can be set if your newsclient is always sending in "format=flowed" but is not cetting the "content-type" correctly (like 40tude))
// 2010-06-13, V37
// - Posts with "text/plain" and no plaintextconverter activated now also preserves whitespaces
// - Tabs will now be converted into 4 spaces (user setting)
// - More descriptions for user settings
// - MVP, MSFT, ADMIN will only be added if they are not yet present in the username
// - Complete support for format=flowed
// - Order of X-Comment and X-Face for Admin, Msft, Mvp corrected
// 2010-06-14, V38
// - Added CodeColorizer inside PlainText Converter (default is on)
//   For more info see: http://colorcode.codeplex.com/
// 2010-07-05, V39
// - Small changes with LiveId authentication
// - Adding of history-info only if now activated in the user settings
// - BugFix: Mime-Decoding regarding QP corrected (thanks to Peter Fleischer for reporting this!)
// 2010-07-22, V40
// - Bugfix: MVP,MSFT,ADMIN now does not use "," as separater...
// - Bugfix: Now it can handle "null" UserName (GROUP Msdn.de-DE.sqlserverde; ARTICLE 623)
// 2011-01-22, V41
// - Bugfix: Decoding of Subject now supported (thanks to Robert Breitenhofer for pointing to the correct function)
// - Added "UserSettings.DisableArticleCache" to disable the bridge-internal article cache (by default the cache is on)
// 2011-01-23, V42
// - Bugfix: Decoding of Subject now also supports multi-line subjects
// 2011-02-03, V43
// - Bugfix: Now supporting multi-line headers in all headers ;) / Thanks to Kai Schätzl for reporting this!
// - Debug output optimized / Added a "Copy to clipboard" button in the debug window
// 2011-05-31 V44
// - Bugfix: Exception during Exception-Logging fixed (DataReceived)
// 2011-11-13 V45
// - Workaround for Web-Service Exception (This item has been deleted)
//   This might lead to slow responses of the bridge if you try to transfer the articles to the client and one article in a 
//   list of articles has this problem. Therefor this is only a work-around!!!
//   MS need to solve the problem. 
//   Bug even the problem is solved, you can still use this new version, because the workaround will not hit anymore, 
//   if the web-service has resolved the bug
//   For more info see also: http://communitybridge.codeplex.com/workitem/9553
//   and http://social.Msdn.microsoft.com/Forums/en-US/reportabug/thread/dbe344b1-c0c2-4015-9e79-b975e06e0fe1#e060c569-a3c4-4744-ab24-10093f5cf241

// TODO RFC conformance: 
// - Clean uninstall (%APPDATA%\Community\CommunityForumsNNTPServer\UICache.xml *and* HKEY_CURRENT_USER\Software\Community\CommunityForumsNNTPServer)
// - REGSZ nach REG_EXPAND_SZ ändern für UserName, DomainName and UserEmail
// - Filter for LIST NEWSGROUPS [match] (makes no sence, because the web-service cannot be filterd)
// - Fix the LIST overview.fmt behaviour:
//   See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214364&ANCHOR#Post449257
// - Retrieving by "ArticleId" must be possible if no group (or an invalid/other group) is selected
//   This is currently not possible, because the web-service does not return the groupname if an article was retrived.
// - NEWGROUPS 19900101 010101 GMT should work
//   See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214373&ANCHOR#Post449285
//   See: http://communitybridge.codeplex.com/Thread/View.aspx?ThreadId=214375&ANCHOR#Post449287

// TODO: 
// - Sending: Embed posted images as embedded img tags
//   For example: 
//   <img
//     src="data:image/gif;base64,R0lGODlhUAAPAKIAAAsLav///88PD9WqsYmApmZmZtZfYmdakyH5BAQUAP8ALAAAAABQAA8AAAPb
//     WLrc/jDKSVe4OOvNu/9gqARDSRBHegyGMahqO4R0bQcjIQ8E4BMCQc930JluyGRmdAAcdiigMLVr
//     ApTYWy5FKM1IQe+Mp+L4rphz+qIOBAUYeCY4p2tGrJZeH9y79mZsawFoaIRxF3JyiYxuHiMGb5KT
//     kpFvZj4ZbYeCiXaOiKBwnxh4fnt9e3ktgZyHhrChinONs3cFAShFF2JhvCZlG5uchYNun5eedRxM
//     AF15XEFRXgZWWdciuM8GCmdSQ84lLQfY5R14wDB5Lyon4ubwS7jx9NcV9/j5+g4JADs=
//     " />
// - Better proxy support, See: http://social.msdn.microsoft.com/Forums/en-US/wcf/thread/83bafcd0-5bab-4cb9-96cf-a53ce132c7ab
// - Better setup experience: AutoStart / StartMenu in folder and updatable links / No need for restart to display the menu entry
// - BindTo: IPv6Loopback, IPv6Any
// - E-Mail address verification
// - UserGuid different for social and answers!? Multiple UserGuids?
// - GPO aware: zuerst Software\POLICIES\Community\ auswerten; wenn dort was steht nur dies verwenden und diese Optionen im Settings-Dialog deaktivieren (read only)
// - Read ListPageSize / ArticlePageSize from Settings
// - Support for text/plan; format=flowed
// - Add "Stars" to the X-Face...
// - References header: Optionally provide the "full" references (by retrieving each parent article...)

// Feature requests from occamrazor:
// - Add a "status area" (a status bar would be fine too) to the GUI to show what the program is 
//   doing (e.g. "connection from 127.0.0.1" or "fetching article..." and the like, that would give 
//   a visual feedback about what the program is doing and may be helpful for debugging)
// - Change the error messages area so that one may copy/paste its contents (again, helpful for debugging issues)
// - Change the handling of the error related to not enabling the "use brigde" flag in profile; 
//   due to the splitting of forums, one may enable the flag only for (e.g.) some forums and not for others, 
//   so, if at least one of the flags was enabled, don't handle that as an error but as a warning 
//   and just emit/log some kind of reminder like "you didn't enable the use bridge flag for 'forum name'"
// - Add OutputDebugString() logging and a config option for logging level, that would allow to pick (e.g.) 
//   http://technet.microsoft.com/en-us/sysinternals/bb896647.aspx and use it to trace program operations in real time 
//   and will also help in tracking bugs since the resulting log will be easy to save and email even for a non-tech user
// - Add an internal timer to allow the program to refresh (re-fetch) in background the groups list at a given interval 
//   (which may even be days...) or in any case to check the groups count and, if it doesn't match the stored count, to refresh the list
// - Change the window "close button" (the "X" one) so that it will just minimize the program to tray 
//   (if such an option is selected) and add an "exit" button (or tray menu option)


// INFO:
// - Donate-Request in "microsoft.private.mvp.newsreaderac" => http://communitybridge.kalmbach.eu/
    
// Feature Requests for the web-service-team:
// - Bug: if a article was moved, the NnntpIndexNumber 0 is assigned to this articld This is wrong! A new number should be assigned from the new forum!
//   As an example see:
//   GROUP Msdn.en-US.vcgeneral
//   ARTICLE <c2547336-aeb1-4260-b2a4-1ce9a0f60731@communitybridge.codeplex.com>
//   See also: http://social.msdn.microsoft.com/Forums/en-US/reportabug/thread/04ee089d-4f2e-4da4-91e9-ca4c01240fb9
// - Bug: The "GetAllForumNewsGroupsByBrand" method will not always return all forums; it also sometimes returns duplicates...
// - Bug: Posting <pre x-lang="cpp">...</pre> will not be displayed correctly in the forum with syntax highlightning (has this to do with "Resource.HasCodeLabel"?)
// - Bug: It seems that "GetAllNewForumNewsGroupsByBrand" does not work correctly; it should return the same list if the DateTime is 19700101 000000 GMT, or?
// - FeatureRequest: Resource.HasCodeLabel should be set via the web-service! (or by detecting a "<pre x-lang.." tag!
// - FeatureRequest: Add the "ForumId" to the result of the "http://schemas.datacontract.org/2004/07/Microsoft.Com.Forums.Service"+"Message", so it can be seen to which forums this message is currently assigned to
// - ReatureRequest: GetSessionUser which should return the current user of this current session (as provided with the LiveId) (why?)


namespace CommunityForumsNNTPServer
{
    // RFCs:
    // http://tools.ietf.org/html/rfc850 Standard for Interchange of USENET Messages
    // http://tools.ietf.org/html/rfc977
    // http://tools.ietf.org/html/rfc2980
    // http://tools.ietf.org/html/rfc3977
    // http://tools.ietf.org/html/rfc2076

    class ForumDataSource : DataProvider
    {
        readonly MicrosoftForumsServiceProvider[] _serviceProviders;
        public ForumDataSource(MicrosoftForumsServiceProvider[] serviceProviders, string domainName)
        {
            _serviceProviders = serviceProviders;
            _domainName = domainName;
        }

        public static Guid? UserGuid;
        public static string UserName;
        public static string UserEmail;
        public Encoding HeaderEncoding = Encoding.UTF8;

        private readonly string _domainName;

        #region DataProvider-Implenentation

        public IList<NNTPServer.Newsgroup> PrefetchNewsgroupList(Action<Newsgroup> stateCallback)
        {
            LoadNewsgroupsToStream(stateCallback);
            return GroupList.Values.ToList();
        }

        public void ClearCache()
        {
            lock(GroupList)
            {
                GroupList.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns <c>true</c> if now exception was thrown while processing the request</returns>
        /// <remarks>
        /// It might happen that this function is called twice!
        /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
        /// </remarks>
        protected override bool LoadNewsgroupsToStream(Action<Newsgroup> groupAction)
        {
            bool res = true;
            lock (this)
            {
                if (IsNewsgroupCacheValid())
                {
                    // copy the list to a local list, so we do not need the lock for the callback
                    List<Newsgroup> localGroups;
                    lock (GroupList)
                    {
                        localGroups = new List<Newsgroup>(GroupList.Values);
                    }
                    if (groupAction != null)
                    {
                        foreach (var g in localGroups)
                            groupAction(g);
                    }
                    return true;
                }

                // INFO: Always return every group...
                //var internalList = new Dictionary<string, Newsgroup>(StringComparer.InvariantCultureIgnoreCase);
                var internalList = new List<Newsgroup>();

                foreach (var provider in _serviceProviders)
                {
                    if (provider.IsDisabled)
                        continue;
                    try
                    {
                        var brands = provider.SupportedBrands;
                      //string

                        foreach (var b in brands)
                        {
                            bool success = false;
                            var dt = DateTimeOffset.UtcNow;
                            var sw = Stopwatch.StartNew();
                            try
                            {
                                // Load the list of all newsgroups:
                                string localBrand = b;
                                MicrosoftForumsServiceProvider localProvider = provider;
                                localProvider.GetAvailableForums(b,
                                    p =>  
                                    {
                                        var g = new ForumNewsgroup(p, localBrand, localProvider);
                                        bool bAdded = false;

                                        if (internalList.FirstOrDefault( p2 => string.Compare(g.GroupName, p2.GroupName, StringComparison.InvariantCultureIgnoreCase) == 0) != null)
                                        {
                                            Console.WriteLine(g.GroupName);
                                        }
                                        if (g.GroupName.IndexOf("officeprog") >= 0)
                                        {
                                            Console.WriteLine(g.GroupName);
                                        }

                                        //if (internalList.ContainsKey(g.GroupName) == false)
                                        {
                                            //internalList.Add(g.GroupName, g);
                                            internalList.Add(g);
                                            bAdded = true;
                                        }
                                    
                                        if ((bAdded) && (groupAction != null))
                                            groupAction(g);

                                        return g;
                                    },
                                    (retrivedCount, totalCount) =>
                                        System.Diagnostics.Debug.WriteLine(
                                        string.Format("{0}: Downloading {1} from {2}", localBrand,retrivedCount, totalCount)));

                                System.Diagnostics.Debug.WriteLine("Finished downloading of forums.");
                                success = true;
                            }
                            catch (Exception exp2)
                            {
                                AppInsights.TrackException(exp2, true);
                                res = false;
                                Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error during GetAvailableForums ({1}: {0}", NNTPServer.Traces.ExceptionToString(exp2), b);
                            }
                            finally
                            {
                                AppInsights.TrackRequest("GetAvailableForums", dt, sw.Elapsed, success);
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        AppInsights.TrackException(exp, true);
                        res = false;
                        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error during LoadNewsgroupsToStream (SupportedBrands): {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }

                }  // foreach providers

                // Now take all the groups into my own list...
                lock (GroupList)
                {
                    foreach (var g in internalList)
                    {
                        if (GroupList.ContainsKey(g.GroupName) == false)
                            GroupList.Add(g.GroupName, g);
                    }
                }

              if (GroupList.Count > 0)
                SetNewsgroupCacheValid();
            }  // lock

            return res;
        }  // LoadNewsgroups

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// It might happen that this function is called twice!
        /// For example if you are currently reading the newsgrouplist and then a client is trying to read articles from a subscribed newsgroup...
        /// </remarks>
        public override bool GetNewsgroupListFromDate(string clientUsername, DateTime fromDate, Action<Newsgroup> groupAction)
        {
            bool res = true;
            lock (this)
            {
                foreach (var provider in _serviceProviders)
                {
                    if (provider.IsDisabled)
                        continue;
                    try
                    {
                        var brands = provider.SupportedBrands;

                        foreach (var b in brands)
                        {
                            // Load the list of all newsgroups:
                            var localProvider = provider;
                            string localBrand = b;
                            try
                            {
                                var allForums = provider.GetAvailableForumSince(b, fromDate,
                                    p =>
                                    {
                                        var g = new ForumNewsgroup(p, localBrand, localProvider);
                                        if (groupAction != null)
                                            groupAction(g);
                                        return g;
                                    },
                                    (retrivedCount, totalCount) =>
                                        System.Diagnostics.Debug.WriteLine(
                                        string.Format("{0}: Downloading {1} from {2}", localBrand, retrivedCount, totalCount)));

                                System.Diagnostics.Debug.WriteLine("Finished downloading of forums.");
                            }
                            catch (Exception exp2)
                            {
                                res = false;
                                Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error during GetAvailableForumSince ({1}: {0}", NNTPServer.Traces.ExceptionToString(exp2), b);
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        AppInsights.TrackException(exp, true);
                        res = false;
                        Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error during GetNewsgroupListFromDate (SupportedBrands): {0}", NNTPServer.Traces.ExceptionToString(exp));
                    }

                }  // foreach providers
            }  // lock
            return res;
        }  // LoadNewsgroupsSince

        // Always gets the newgroup info from the sever; so we have always the corrent NNTPMaxNumber!
        public override Newsgroup GetNewsgroup(string clientUsername, string groupName, bool updateFirstLastNumber)
        {
            // 1st Part: Brand
            // 2nt Part: Locale
            // 3rd Part: Groupname
            string[] groupParts = groupName.Trim().Split('.');
            if (groupParts.Length <= 2)
            {
                Traces.Main_TraceEvent(TraceEventType.Verbose, 1, "GetNewsgroup failed (invalid groupname): {0}", groupName);
                return null;
            }
            // First try to find the group (ServiceProvider) in the cache...
            MicrosoftForumsServiceProvider provider = null;
            ForumNewsgroup cachedGroup = null;
            lock(GroupList)
            {
                if (GroupList.ContainsKey(groupName))
                {
                    cachedGroup = GroupList[groupName] as ForumNewsgroup;
                    if (cachedGroup != null)
                        provider = cachedGroup.Provider;
                }
            }

            // If we just need the group without actual data, then return the cached group
            if ((updateFirstLastNumber == false) && (cachedGroup != null))
                return cachedGroup;

            WebServiceDataSource.Forums.ForumNewsGroup webGroup = null;
            if (provider == null)
            {
                // Try to find the correct service if the groupname is not known to me...
                foreach (var p in _serviceProviders)
                {
                    if (p.IsDisabled)
                        continue;
                    bool success = false;
                    var dt = DateTimeOffset.UtcNow;
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        webGroup = p.GetForumNewsGroupByName(groupParts[2]);
                        success = true;
                    }
                    catch(Exception exp)
                    {
                        AppInsights.TrackException(exp, true);
                        throw;
                    }
                    finally
                    {
                        AppInsights.TrackRequest("GetForumNewsGroupByName", dt, sw.Elapsed, success);
                    }
                    if (webGroup != null)
                    {
                        provider = p;
                        break;
                    }
                }
            }
            else
            {
                bool success = false;
                var dt = DateTimeOffset.UtcNow;
                var sw = Stopwatch.StartNew();
                try
                {
                    webGroup = provider.GetForumNewsGroupByName(groupParts[2]);
                    success = true;
                }
                catch(Exception ex)
                {
                    AppInsights.TrackException(ex, true);
                    throw;
                }
                finally
                {
                    AppInsights.TrackRequest("GetForumNewsGroupByName", dt, sw.Elapsed, success);
                }
            }

            if (webGroup != null)
            {
                var g = new ForumNewsgroup(webGroup, groupParts[0], provider);
                lock (GroupList)
                {
                    if (GroupList.ContainsKey(g.GroupName) == false)
                    {
                        GroupList.Add(g.GroupName, g);
                    }
                    else
                    {
                        // Update the last and first article number
                        GroupList[g.GroupName].FirstArticle = g.FirstArticle;
                        GroupList[g.GroupName].LastArticle = g.LastArticle;
                        GroupList[g.GroupName].NumberOfArticles = g.NumberOfArticles;
                        g = GroupList[g.GroupName] as ForumNewsgroup;  // so return also all articles and so on...
                    }
                }
                return g;
            }

            // Only return null, if the group was not found!
            // In other cases (network error), an exception should be thrown
            return null;
        }

        private ForumNewsgroup GetNewsgroupFromForumId(Guid forumId)
        {
            foreach (var provider in _serviceProviders)
            {
                if (provider.IsDisabled)
                    continue;
                bool success = false;
                var dt = DateTimeOffset.UtcNow;
                var sw = Stopwatch.StartNew();
                WebServiceDataSource.Forums.ForumNewsGroup group = null;
                try
                {
                    group = provider.GetForumNewsGroup(forumId);
                    success = true;
                }
                catch(Exception ex)
                {
                    AppInsights.TrackException(ex, true);
                    throw;
                }
                finally
                {
                    AppInsights.TrackRequest("GetForumNewsGroup", dt, sw.Elapsed, success);
                }
                if (group != null)
                {
                    var g = new ForumNewsgroup(group, string.Empty, provider);
                    // INFO: Do not store this group in the list, because I do not have the "brand" name... and therefor the newsgroupname is wrong!
                    return g;
                }
            }
            return null;
        }

        public override Newsgroup GetNewsgroupFromCacheOrServer(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;
            groupName = groupName.Trim();
            ForumNewsgroup res = null;
            lock (GroupList)
            {
                if (GroupList.ContainsKey(groupName))
                    res = GroupList[groupName] as ForumNewsgroup;
            }
            if (res == null)
            {
                // We can specify "true" or "false" as last paremter, because the group is not in the cache, so the group will be retrived from the server.
                res = GetNewsgroup(null, groupName, true) as ForumNewsgroup;
            }
            return res;
        }

        private ForumNewsgroup GetNewsgroupFromCacheOrServerByForumId(Guid forumId)
        {
            ForumNewsgroup res = null;
            lock (GroupList)
            {
                foreach (var g in GroupList.Values)
                {
                    var fg = g as ForumNewsgroup;
                    if ((fg != null) && (fg.ForumId == forumId))
                    {
                        res = fg;
                        break;
                    }
                }
            }
            if (res == null)
                res = GetNewsgroupFromForumId(forumId);
            return res;
        }

        public override Article GetArticleById(string clientUsername, string groupName, string articleId)
        {
            var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
            return GetArticleById(g, articleId);
        }
        private ForumArticle GetArticleById(ForumNewsgroup g, string articleId)
        {
            // INFO: g can be NULL!

            Guid? forumId;
            Guid? id = ForumArticle.IdToGuid(articleId, out forumId);
            if (id == null) return null;

            if (forumId.HasValue)
            {
                var fg = GetNewsgroupFromCacheOrServerByForumId(forumId.Value);
                var a = GetArticleByIdInternal(fg, id.Value);
                if (a != null)
                    return a;
            }

            return GetArticleByIdInternal(g, id.Value);
        }

        private ForumArticle GetArticleByIdInternal(ForumNewsgroup g, Guid id)
        {
            if (g == null)
            {
                return null;

                //foreach (var provider in _serviceProviders)
                //{
                //    // INFO: I need to provide a valid forumId
                //    Guid? forumId = null;
                //    MicrosoftForumsServiceProvider prov = provider;
                //    var anyG = this.GroupList.Values.FirstOrDefault(p => (p as ForumNewsgroup).Provider == prov) as ForumNewsgroup;
                //    if (anyG == null)
                //    {
                //        // We need to fetch one group from this provider...
                //        var firstBrand = provider.SupportedBrands[0];
                //        var cont = provider.ForumsService.GetAllForumNewsGroupsByBrand(firstBrand, 1, 1);
                //        if (cont != null)
                //        {
                //            forumId = cont.Results[0].ForumId;
                //        }
                //    }
                //    else
                //    {
                //        forumId = anyG.ForumId;
                //    }

                //    if (forumId == null)
                //        continue;

                //    var msg = provider.ForumsService.GetMessage(forumId.Value, id);
                //    if (msg != null)
                //    {
                //        var correctGroup = CheckCorrectGroup(g, msg.ForumId);  // INFO: The Message does not have a "ForumId", therefor this is currently not possible...
                //        var ar = new ForumArticle(msg, g, _domainName);
                //        ConvertNewArticleFromWebService(ar);
                //        return ar;
                //    }
                //}  // foreach
                //return null;
            }


            if (UserSettings.Default.DisableArticleCache == false)
            {
                // Check if the article is in my cache...
                lock (g.Articles)
                {
                    foreach (var ar in g.Articles.Values)
                    {
                        var fa = ar as ForumArticle;
                        if ((fa != null) && (fa.Guid == id))
                            return fa;
                    }
                }
            }

            // Get it from the server...
            WebServiceDataSource.Forums.Message res = null;


            bool success = false;
            var dt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();
            try
            {
                res = g.Provider.GetMessage(g.ForumId, id);
                success = true;
            }
            catch(Exception ex)
            {
                AppInsights.TrackException(ex, true);
                throw;
            }
            finally
            {
                AppInsights.TrackRequest("GetMessage", dt, sw.Elapsed, success);
            }
            if (res == null) return null;

            // Store it in my cache...
            var a = new ForumArticle(res, g, _domainName, AddHistoryToArticle);
            ConvertNewArticleFromWebService(a);

            if (UserSettings.Default.DisableArticleCache == false)
            {
                lock (g.Articles)
                {
                    g.Articles[a.Number] = a;
                }
            }
            return a;
        }

        //private ForumNewsgroup CheckCorrectGroup(ForumNewsgroup g, Guid forumId)
        //{
        //    // TODO: ...
        //    return g;
        //}

        #region IArticleConverter

        public UsePlainTextConverters UsePlainTextConverter
        {
            get { return _converter.UsePlainTextConverter; }
            set { _converter.UsePlainTextConverter = value; }
        }

        public ArticleConverter.UserDefinedTagCollection UserDefinedTags
        {
            set { _converter.UserDefinedTags = value; }
        }

        public int AutoLineWrap
        {
            get { return _converter.AutoLineWrap; }
            set { _converter.AutoLineWrap = value; }
        }

        public bool ShowUserNamePostfix
        {
            get { return _converter.ShowUserNamePostfix; }
            set { _converter.ShowUserNamePostfix = value; }
        }

        public bool PostsAreAlwaysFormatFlowed
        {
            get { return _converter.PostsAreAlwaysFormatFlowed; }
            set { _converter.PostsAreAlwaysFormatFlowed = value; }
        }

        public int TabAsSpace
        {
            get { return _converter.TabAsSpace; }
            set { _converter.TabAsSpace = value; }
        }

        public bool UseCodeColorizer
        {
            get { return _converter.UseCodeColorizer; }
            set { _converter.UseCodeColorizer = value; }
        }

        public bool AddHistoryToArticle { get; set; }

        readonly ArticleConverter.Converter _converter = new ArticleConverter.Converter();
        private void ConvertNewArticleFromWebService(Article a)
        {
            try
            {
                _converter.NewArticleFromWebService(a, HeaderEncoding);
            }
            catch (Exception exp)
            {
                AppInsights.TrackException(exp, true);
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromWebService failed: {0}", NNTPServer.Traces.ExceptionToString(exp));
            }
        }

        private void ConvertNewArticleFromNewsClientToWebService(Article a)
        {
            try
            {
                _converter.NewArticleFromClient(a);
            }
            catch (Exception exp)
            {
                AppInsights.TrackException(exp, true);
                Traces.Main_TraceEvent(TraceEventType.Error, 1, "ConvertNewArticleFromNewsClientToWebService failed: {0}", NNTPServer.Traces.ExceptionToString(exp));
           }
        }
        #endregion

        public override Article GetArticleByNumber(string clientUsername, string groupName, int articleNumber)
        {
            var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
            if (g == null) return null;
            if (UserSettings.Default.DisableArticleCache == false)
            {
                lock (g.Articles)
                {
                    if (g.Articles.ContainsKey(articleNumber))
                        return g.Articles[articleNumber];
                }
            }

            // 2011-11-13: Workaround for bug in web-service:
            // For more info see: http://communitybridge.codeplex.com/workitem/9553
            WebServiceDataSource.Forums.BriefMessagesContainer m = null;
            bool success = false;
            var dt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();
            try
            {
              m = g.Provider.GetForumMessagesBriefForNNTP(g.ForumId, articleNumber, articleNumber, true);
              success = true;
            }
            catch (Exception ex)
            {
              AppInsights.TrackException(ex, true);
              // Handle special Exception from Web-Service (appeared by Nov 2011...)
              // See: http://communitybridge.codeplex.com/workitem/9553
              //[Client 2] DataReceived failed: Exception:
              //Type System.ServiceModel.FaultException`1[[System.ServiceModel.ExceptionDetail, System.ServiceModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
              //Source: mscorlib
              //Message: This item has been deleted
              //Stack-Trace:

              //Server stack trace: 
              //   at System.ServiceModel.Channels.ServiceChannel.ThrowIfFaultUnderstood(Message reply, MessageFault fault, String action, MessageVersion version, FaultConverter faultConverter)
              //   at System.ServiceModel.Channels.ServiceChannel.HandleReply(ProxyOperationRuntime operation, ProxyRpc& rpc)
              //   at System.ServiceModel.Channels.ServiceChannel.Call(String action, Boolean oneway, ProxyOperationRuntime operation, Object[] ins, Object[] outs, TimeSpan timeout)
              //   at System.ServiceModel.Channels.ServiceChannel.Call(String action, Boolean oneway, ProxyOperationRuntime operation, Object[] ins, Object[] outs)
              //   at System.ServiceModel.Channels.ServiceChannelProxy.InvokeService(IMethodCallMessage methodCall, ProxyOperationRuntime operation)
              //   at System.ServiceModel.Channels.ServiceChannelProxy.Invoke(IMessage message)

              //Exception rethrown at [0]: 
              //   at System.Runtime.Remoting.Proxies.RealProxy.HandleReturnMessage(IMessage reqMsg, IMessage retMsg)
              //   at System.Runtime.Remoting.Proxies.RealProxy.PrivateInvoke(MessageData& msgData, Int32 type)
              //   at CommunityForumsNNTPServer.WebServiceDataSource.Forums.IForumsService.GetForumMessagesBriefForNNTP(Guid forumId, Int32 nntpMessageStartIndex, Int32 nntpMessageEndIndex, Boolean loadBody)
              //   at CommunityForumsNNTPServer.WebServiceDataSource.Forums.ForumsServiceClient.GetForumMessagesBriefForNNTP(Guid forumId, Int32 nntpMessageStartIndex, Int32 nntpMessageEndIndex, Boolean loadBody)
              //   at CommunityForumsNNTPServer.WebServiceDataSource.MicrosoftForumsServiceProvider.<>c__DisplayClass5`1.<GetForumMessagesBriefForNntp>b__3(Int32 index, Int32 batchSize, Boolean& finished)
              //   at CommunityForumsNNTPServer.WebServiceDataSource.MicrosoftForumsServiceProvider.BuildResultsInBatch_[T,T2](Func`2 converter, MyFunc`4 generator, Action`1 progressResult)
              //   at CommunityForumsNNTPServer.WebServiceDataSource.MicrosoftForumsServiceProvider.GetForumMessagesBriefForNntp[T](Guid forumId, Int32 firstArticle, Int32 lastArticle, Boolean loadBody, Func`2 converter, Action`1 progress)
              //   at CommunityForumsNNTPServer.ForumDataSource.GetArticlesByNumberToStream(String clientUsername, String groupName, Int32 firstArticle, Int32 lastArticle, Action`1 articlesProgressAction)
              //   at CommunityForumsNNTPServer.NNTPServer.NntpCommandListGroup.Parse(String parameters, Action`1 writeAction, Client client)
              //   at CommunityForumsNNTPServer.NNTPServer.NntpServer.DataReceived(String data, Int32 clientNumber)

              if (string.Equals(ex.Message, "This item has been deleted", StringComparison.OrdinalIgnoreCase))
              {
                WebServiceDataSource.Traces.WebService_TraceEvent(TraceEventType.Error, 1, "CreateQuestionThread: GetForumMessagesBriefForNNTP has thrown an excpetion (This item has been deleted), ForumId: {0}, Name: {1}, Article#: {2}", g.ForumId, groupName, articleNumber);
                return null; // Article not found
              }
              throw;  // rethrow on unknown message exception
            }
            finally
            {
                AppInsights.TrackRequest("GetMessageBrief", dt, sw.Elapsed, success);
            }

            if (m.Results.Length > 0)
            {
                Article a = new ForumArticle(m.Results[0], g, _domainName, AddHistoryToArticle);
                ConvertNewArticleFromWebService(a);
                if (UserSettings.Default.DisableArticleCache == false)
                {
                    lock (g.Articles)
                    {
                        g.Articles[a.Number] = a;
                    }
                }
                return a;
            }
            return null;
        }

        public override void GetArticlesByNumberToStream(string clientUsername, string groupName, int firstArticle, int lastArticle, Action<IList<Article>> articlesProgressAction)
        {
            // Check if the number has the correct order... some clients may sent it XOVER 234-230 instead of "XOVER 230-234"
            //bool ascending = true;
            if (firstArticle > lastArticle)
            {
                // the numbers are in the wrong oder, so correct it...
                var tmp = firstArticle;
                firstArticle = lastArticle;
                lastArticle = tmp;
                //ascending = false;
            }

            var g = GetNewsgroupFromCacheOrServer(groupName) as ForumNewsgroup;
            if (g == null) return;

            bool success = false;
            var dt = DateTimeOffset.UtcNow;
            var sw = Stopwatch.StartNew();
            int cnt = 0;
            try
            {
                var articles = g.Provider.GetForumMessagesBriefForNntp(g.ForumId, firstArticle, lastArticle, true,
                    p =>
                    {
                        var article = new ForumArticle(p, g, _domainName, AddHistoryToArticle);
                        ConvertNewArticleFromWebService(article);

                        return article;
                    },
                    (articleList) =>
                    {
                        List<Article> al = new List<Article>();
                        foreach (var article in articleList.OrderBy(p => p.Number))
                        {
                            al.Add(article);
                            if (UserSettings.Default.DisableArticleCache == false)
                            {
                                lock (g.Articles)
                                {
                                    g.Articles[article.Number] = article;
                                }
                            }
                        }
                        if (articlesProgressAction != null)
                        {
                            articlesProgressAction(al);
                        }
                    });
                success = true;
                cnt = articles.Count;
            }
            catch(Exception exp)
            {
                AppInsights.TrackException(exp, true);
                throw;
            }
            finally
            {
                AppInsights.TrackRequest("GetMessageBriefList", dt, sw.Elapsed, success, cnt);
            }
        }

        private static readonly Regex RemoveUnusedhtmlStuffRegex = new Regex(".*<body[^>]*>\r*\n*(.*)\r*\n*</\\s*body>", 
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static string RemoveUnsuedHtmlStuff(string text)
        {
            var m = RemoveUnusedhtmlStuffRegex.Match(text);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            return text;
        }

        protected override void SaveArticles(string clientUsername, List<Article> articles)
        {
            foreach (var a in articles)
            {
                var g = GetNewsgroupFromCacheOrServer(a.ParentNewsgroup) as ForumNewsgroup;
                if (g == null)
                    throw new ApplicationException("Newsgroup not found!");

                ConvertNewArticleFromNewsClientToWebService(a);

                if (a.ContentType.IndexOf("text/html", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    a.Body = RemoveUnsuedHtmlStuff(a.Body);
                }
                else //if (a.ContentType.IndexOf("text/plain", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // It seems to be plain text, so convert it to "html"...
                    a.Body = a.Body.Replace("\r", string.Empty);
                    a.Body = System.Web.HttpUtility.HtmlEncode(a.Body);
                    a.Body = a.Body.Replace("\n", "<br />");
                }

                if ((UserSettings.Default.DisableUserAgentInfo == false) && (string.IsNullOrEmpty(a.Body) == false))
                {
                    a.Body = a.Body + string.Format("<a name=\"{0}_CommunityBridge\" title=\"{1} via {2}\" />", Guid.NewGuid().ToString(), a.UserAgent, Article.MyXNewsreaderString);
                }

                // Check if this is a new post or a reply:

                Guid myThreadGuid;
                if (string.IsNullOrEmpty(a.References))
                {
                    WebServiceDataSource.Traces.WebService_TraceEvent(TraceEventType.Verbose, 1, "CreateQuestionThread: ForumId: {0}, Subject: {1}, Content: {2}", g.ForumId, a.Subject, a.Body);

                    bool success = false;
                    var dt = DateTimeOffset.UtcNow;
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        myThreadGuid = g.Provider.CreateQuestionThread(g.ForumId, a.Subject, a.Body);
                        success = true;
                    }
                    catch(Exception ex)
                    {
                        AppInsights.TrackException(ex, true);
                        throw;
                    }
                    finally
                    {
                        AppInsights.TrackRequest("CreateQuestion", dt, sw.Elapsed, success);
                    }
                }
                else
                {
                    // FIrst get the parent Message, so we can retrive the discussionId (threadId)
                    // retrive the last reference:
                    string[] refes = a.References.Split(' ');
                    var res = GetArticleById(g, refes[refes.Length-1].Trim());
                    if (res == null)
                        throw new ApplicationException("Parent message not found!");

                    WebServiceDataSource.Traces.WebService_TraceEvent(TraceEventType.Verbose, 1, "CreateReply: ForumId: {0}, DiscussionId: {1}, ThreadId: {2}, Content: {3}", g.ForumId, res.DiscussionId, res.Guid, a.Body);

                    bool success = false;
                    var dt = DateTimeOffset.UtcNow;
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        myThreadGuid = g.Provider.CreateReply(g.ForumId, res.DiscussionId, res.Guid, a.Body);
                        success = true;
                    }
                    catch(Exception ex)
                    {
                        AppInsights.TrackException(ex, true);
                        throw;
                    }
                    finally
                    {
                        AppInsights.TrackRequest("CreateReply", dt, sw.Elapsed, success);
                    }
                }

                // TODO: Auto detect my email and username (guid):
                try
                {
                    // Try to find the email address in the post:
                    var m = emailFinderRegEx.Match(a.From);
                    if (m.Success)
                    {
                        string email = m.Groups[2].Value;

                        // try to find this email in the usermapping collection:
                        bool bFound = false;
                        lock (UserSettings.Default.UserMappings)
                        {
                            foreach (var um in UserSettings.Default.UserMappings)
                            {
                                if (string.Compare(um.UserEmail, email, 
                                    StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    // Address is already known...
                                    bFound = true;
                                    break;
                                }
                            }
                        }
                        if (bFound == false)
                        {
                            // I have not yet this email address, so find the user guid for the just posted article:
                            var a2 = GetArticleById(g, ForumArticle.GuidToId(myThreadGuid, g.ForumId, _domainName));
                            if (a2 != null)
                            {
                                var userGuid = a2.UserGuid;
                                // Now store the data in the user settings
                                bool bGuidFound = false;
                                lock (UserSettings.Default.UserMappings)
                                {
                                    foreach (var um in UserSettings.Default.UserMappings)
                                    {
                                        if (um.Id == userGuid)
                                        {
                                            bGuidFound = true;
                                            um.UserEmail = email;
                                        }
                                    }
                                    if (bGuidFound == false)
                                    {
                                        UserSettings.Default.UserMappings.Add(
                                            new UserMapping()
                                                {
                                                    Id = userGuid,
                                                    UserEmail = email,
                                                    UserName = a2.DisplayName
                                                }
                                            );
                                    }
                                }  // lock
                            }
                        }
                    }

                }
                catch (Exception exp)
                {
                    AppInsights.TrackException(exp, true);
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in retrieving own article: {0}", NNTPServer.Traces.ExceptionToString(exp));                    
                }
            }
        }

        Regex emailFinderRegEx = new Regex(@"(^|\s|<)([a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)(>|s|$)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        #endregion
    }  // class ForumDataSource

    public class ForumNewsgroup : Newsgroup
    {
        public ForumNewsgroup(WebServiceDataSource.Forums.ForumNewsGroup forum, string brand, MicrosoftForumsServiceProvider provider) :
            base(string.Format("{0}.{1}.{2}", brand, forum.Language, forum.UniqueName), 1, forum.MaxNNTPMessageIndex, true, forum.MaxNNTPMessageIndex, DateTime.Now)
        {
            // INFO: Currently we assume, that the brand-name will be unique across providers
            ForumId = forum.ForumId;
            Provider = provider;
            DisplayName = string.Empty;
            Description = forum.Description;
            if (string.IsNullOrEmpty(Description) == false)
            {
                Description = Description.Replace("\n", " ").Replace("\r", string.Empty).Replace("\t", string.Empty);
            }
            Language = forum.Language;
            Brand = brand;
            UniqueName = forum.UniqueName;
        }

        public ForumNewsgroup(WebServiceDataSource.Forums.Forum forum, string brand, MicrosoftForumsServiceProvider provider) :
            base(string.Format("{0}.{1}.{2}", brand, forum.Language, forum.UniqueName), 1, 0, true, 0, DateTime.Now)
        {
            if (forum.Statistics != null)
            {
                LastArticle = NumberOfArticles = forum.Statistics.MaxNNTPMessageIndex;
            }
            // INFO: Currently we assume, that the brand-name will be unique across providers
            ForumId = forum.ForumId;
            Provider = provider;
            DisplayName = forum.DisplayName;
            Description = forum.Description;
            if (string.IsNullOrEmpty(Description) == false)
            {
                Description = Description.Replace("\n", " ").Replace("\r", string.Empty).Replace("\t", string.Empty);
            }
            Language = forum.Language;
            Brand = brand;
            UniqueName = forum.UniqueName;
        }

        internal MicrosoftForumsServiceProvider Provider;
        internal Guid ForumId;
        internal string Language;
        internal string Brand;
        internal string UniqueName;
    }  // class ForumNewsgroup

    public class ForumArticle : Article
    {
        public ForumArticle(WebServiceDataSource.Forums.BriefMessage msg, ForumNewsgroup g, string domainName, bool addHistoryToArticle)
            : base(msg.NntpMessageIndex, GuidToId(msg.Id, g.ForumId, domainName))
        {
            Guid = msg.Id;
            DiscussionId = msg.DiscussionId;
            Body = msg.Body;


            // URL: Build an URL for this discussion thread
            // Special handling for "Microsoft" forums: the domain-part is ommitted for this brand
            var sb = new StringBuilder();
            sb.Append("http://social.");
            if (string.Compare(g.Brand, "Microsoft", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
              sb.Append(g.Brand);
              sb.Append(".");
            }
            sb.Append("microsoft.com/Forums/");
            sb.Append(g.Language);
            sb.Append("/");
            sb.Append(g.UniqueName);
            sb.Append("/thread/");
            sb.Append(DiscussionId);
            sb.Append("#");
            sb.Append(Guid);

            if (msg.NntpMessageIndex <= 0)
            {
                Traces.Main_TraceEvent(TraceEventType.Error, 1,
                                       "Invalid article number ({0}) for article {1} in group {2}", 
                                       msg.NntpMessageIndex, msg.Id, g.UniqueName);
            }

            if (addHistoryToArticle)
            {
                // Check message history:
                var m = msg as WebServiceDataSource.Forums.Message;
                if (m != null)
                {
                    if ((m.MessageHistory != null) && (m.MessageHistory.Length > 0))
                    {
                        var sbh = new StringBuilder();
                        sbh.Append("<p><strong>Message History</strong> <i>(added by Community NNTP bridge)</i>:<br />");
                        foreach (var mh in m.MessageHistory)
                        {
                            sbh.Append(System.Web.HttpUtility.HtmlEncode(mh.Edited.ToString("ddd, d MMM yyyy HH:mm:ss",
                                                                                            System.Globalization.
                                                                                                CultureInfo.
                                                                                                InvariantCulture)));
                            sbh.Append(" +0000");
                            sbh.Append(": ");
                            sbh.Append(System.Web.HttpUtility.HtmlEncode(mh.ItemType.ToString()));
                            sbh.Append(": ");
                            sbh.Append(System.Web.HttpUtility.HtmlEncode(mh.Reason));
                            sbh.Append("<br />");
                        }
                        sbh.Append("</p>\r\n");
                        Body = sbh.ToString() + Body;
                    }
                }
            }

            Date = string.Format("{0} +0000",
                msg.CreatedOn.ToString("ddd, d MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
                );
            if (msg.Subject != null)
            {
                Subject = msg.Subject.Replace("\n", string.Empty).Replace("\r", string.Empty);
            }
            else
            {
                Subject = "<null>";
            }
            if (msg.CreatedBy != null)
            {
                if (msg.CreatedBy.DisplayName != null)
                {
                    From =
                        DisplayName =
                        msg.CreatedBy.DisplayName.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace("\t",
                                                                                                                  string
                                                                                                                      .
                                                                                                                      Empty);
                }
                else
                {
                    From = DisplayName = "<null>";
                }
                UserGuid = msg.CreatedBy.Id;

                // Set UserEmail:
                try
                {
                    if (ForumDataSource.UserGuid.HasValue)
                    {
                        if ((msg.CreatedBy.Id == ForumDataSource.UserGuid) &&
                            (string.IsNullOrEmpty(ForumDataSource.UserEmail)))
                        {
                            UserEmail = ForumDataSource.UserEmail;
                            ArticleConverter.Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1,
                                                                         "Assigned UserEmail from match of GUID: {0}",
                                                                         msg.CreatedBy.Id);
                        }
                    }
                    if ((string.IsNullOrEmpty(UserEmail)) && (string.IsNullOrEmpty(ForumDataSource.UserName) == false) &&
                        (string.IsNullOrEmpty(ForumDataSource.UserEmail) == false))
                    {
                        ArticleConverter.Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1,
                                                                     "Article UserName: '{0}', Local UserName: '{1}'",
                                                                     From, ForumDataSource.UserName);
                        if (string.Compare(ForumDataSource.UserName, From, true) == 0) // do current culture compare
                        {
                            UserEmail = ForumDataSource.UserEmail;
                            ArticleConverter.Traces.ConvertersTraceEvent(TraceEventType.Verbose, 1,
                                                                         "Assigned UserEmail from UserName: {0}",
                                                                         ForumDataSource.UserName);
                        }
                    }

                    // Try to find the UserEmail from the automatic mapping table...
                    if (string.IsNullOrEmpty(UserEmail))
                    {
                        lock (UserSettings.Default.UserMappings)
                        {
                            foreach (var um in UserSettings.Default.UserMappings)
                            {
                                if (um.Id == UserGuid)
                                {
                                    UserEmail = um.UserEmail;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    AppInsights.TrackException(exp, true);
                    Traces.Main_TraceEvent(TraceEventType.Error, 1, "Error in UserMapping: {0}",
                                           NNTPServer.Traces.ExceptionToString(exp));
                }
            }

            if (msg.ParentId != null)
                References = GuidToId(msg.ParentId.Value, g.ForumId, domainName);
            Newsgroups = g.GroupName;
            ParentNewsgroup = Newsgroups;
            Path = "LOCALHOST." + domainName;

            ArchivedAt = "<" + sb.ToString() + ">";

            // Special handling of some special user-settings:
            if (msg.CreatedBy != null)
            {
                AnwsersCount = msg.CreatedBy.AnswersCount;
                IsAdministrator = msg.CreatedBy.IsAdministrator;
                IsMvp = msg.CreatedBy.IsMvp;
                IsMsft = msg.CreatedBy.IsMsft;
                Points = msg.CreatedBy.Points;
                PostsCount = msg.CreatedBy.PostsCount;
                Stars = msg.CreatedBy.Stars;
            }

          Body = Body + Environment.NewLine + "<p>-----<br/>"
                 + "<a href='" + sb + "'>" + sb + "</a>"
                 + "<br/>-----<br/></p>" + Environment.NewLine;
        }


        // INFO: Design decision (2010-05-26)
        // The MessageId must not contain the forumId, because the message can be moved to an other forum!
        // So the messageId only contains the guid of the message and the domain-name
        // Also the ARTICLE <id> command will not work without selection a group; this is by design!!!
        // Also changing the MessageId will always break existing messages from displaying the correct threaded.-view!
        // INFO: Design Re-Decision: (2010-05-27)
        // After discussion with "Kai Schaetzl" and "David Wilkinsion" 
        // we decided to use the "old" "-" format for the messageId.
        // For two resons: The MS NNTP Bridge also uses the "-"
        // The forum-web address uses also the "-" in the url 
        // (like http://social.msdn.microsoft.com/Forums/en-US/vcgeneral/thread/5e88658c-adb7-40da-82d0-4b7928cc775e)
        // The "-" is a valid character in the messageId field:
        // http://www.w3.org/Protocols/rfc1036/rfc1036.html#z2
        public static string GuidToId(Guid guid, Guid forumId, string domainName)
        {
            var s = guid.ToString();
            //s = s.Replace("-", "$");

            //string forumIdString = string.Empty;
            //var forumIdString = forumId.ToString();
            //forumIdString = forumIdString.Replace("-", "$") + ".";  // this would create an invalid domain name
            //return "<" + s + "@" + forumIdString + domainName + ">";

            return "<" + s + "@" + domainName + ">";
        }

        public static Guid? IdToGuid(string id, out Guid? forumId)
        {
            forumId = null;
            if (id == null) return null;
            if (id.StartsWith("<") == false) return null;
            id = id.Trim('<', '>');
            var parts = id.Split('@', '.');

            // The first part is always the id:
            // For backward compatibility we support both version ("-" and "$")
            var idStr = parts[0].Replace("$", "-");
            Guid guid;
            try
            {
                guid = new Guid(idStr);
            }
            catch
            {
                return null;
            }

            //if (parts.Length > 1)
            //try
            //{
            //    string forumIdStr = parts[1];
            //    forumIdStr = forumIdStr.Replace("$", "-");
            //    forumId = new Guid(forumIdStr);
            //}
            //catch
            //{
            //}

            return guid;
        }
    }

}
