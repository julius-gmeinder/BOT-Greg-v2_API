namespace BOT_Greg_v2_API;

public static class Constants
{
    public const string LpBaseUrl = "https://liquipedia.net/counterstrike";
    public const string LpApiBaseUrl = "https://api.liquipedia.net/api/v3";

    public const int SyncCooldownMinutes = 5;
    public const int StreamHeartbeatSeconds = 30;

    public const int MaxFiltersPerGuild = 20;
    public const int MaxRoleLinksPerGuild = 20;

    // event names of the server-sent-events stream
    public static class StreamEvents
    {
        public const string MatchAlerts = "matchAlerts";
        public const string MatchDelays = "matchDelays";
        public const string ResultAlerts = "resultAlerts";
        public const string Heartbeat = "heartbeat";
    }
}
