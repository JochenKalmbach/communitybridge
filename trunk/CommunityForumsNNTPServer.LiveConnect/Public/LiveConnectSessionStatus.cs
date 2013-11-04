namespace CommunityForumsNNTPServer.LiveConnect.Public
{
  public enum LiveConnectSessionStatus
    {
        Unknown,
        Connected,
        NotConnected,
#if WEB
        Expired,
#endif
    }
}
