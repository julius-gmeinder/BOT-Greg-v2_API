namespace BOT_Greg_v2_API.Models.Enums
{
    public enum StreamPlatform
    {
        Twitch = 0,
        Youtube = 1,
        Kick = 2
    }

    public static class StreamPlatformExtensions
    {
        public static string? GetUrl(this StreamPlatform platform)
        {
            string lpStreamBaseUrl = $"{Constants.LpBaseUrl}/Special:Stream";

            return platform switch
            {
                StreamPlatform.Twitch => $"{lpStreamBaseUrl}/twitch",
                StreamPlatform.Youtube => $"{lpStreamBaseUrl}/youtube",
                StreamPlatform.Kick => $"{lpStreamBaseUrl}/kick",
                _ => null
            };
        }
    }
}
