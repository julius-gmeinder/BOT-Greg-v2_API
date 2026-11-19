namespace BOT_Greg_v2_API;

public static class Constants
{
    public const string LpBaseUrl = "https://liquipedia.net/counterstrike";
    public const string LpApiBaseUrl = "https://api.liquipedia.net/api/v3";

    public const int SyncCooldownMinutes = 5;
    public const int StreamHeartbeatSeconds = 30;

    public const int EventsHorizonMonths = 6; // how far ahead events are fetched
    public const int LiveMatchesLookbackHours = 12; // older matches that are not finished are treated as stale

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

    public static class SearchAliases
    {
        public static readonly string[][] Groups =
        {
            // organizers / events
            new[] { "IEM", "Intel Extreme Masters" },
            new[] { "EPL", "ESL Pro League" },
            new[] { "CCT", "Champion of Champions Tour" },
            new[] { "EWC", "Esports World Cup" },
            new[] { "RMR", "Regional Major Ranking" },

            // teams
            new[] { "NaVi", "Natus Vincere" },
            new[] { "EG", "Evil Geniuses" },
            new[] { "NIP", "Ninjas in Pyjamas" },
            new[] { "VP", "Virtus.pro" },
            new[] { "C9", "Cloud9" },
            new[] { "TL", "Team Liquid" },
            new[] { "GL", "GamerLegion" },
            new[] { "EF", "Eternal Fire" },
            new[] { "FNC", "Fnatic" },
            new[] { "COL", "Complexity" },
            new[] { "FLY", "FlyQuest" },
            new[] { "BB", "BetBoom" },
            new[] { "MOUZ", "mousesports" },
            new[] { "MIBR", "Made in Brazil" },
        };
    }
}
