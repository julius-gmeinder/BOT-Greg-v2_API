using BOT_Greg_v2_API.Models.Enums;

namespace BOT_Greg_v2_API.Models.External
{
    public class MatchUpcoming : MatchBase
    {
        public List<Stream> Streams { get; init; } = null!;
    }

    public class Stream
    {
        public StreamPlatform Platform { get; init; }
        public string Channel { get; init; } = null!;
        public string? Url => Platform.GetUrl() == null ? null : $"{Platform.GetUrl()}/{Channel}";
    }
}
