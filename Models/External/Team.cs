namespace BOT_Greg_v2_API.Models.External
{
    public class Team
    {
        public int TeamId { get; init; }
        public string Name { get; init; } = null!;
        public string RelUrl { get; init; } = null!;
        public string Url => $"{Constants.LpBaseUrl}/{RelUrl}";

        public string? NameShort { get; init; }

        public string? Country { get; init; }
        public string? Region { get; init; }

        public string LocationString => Country ?? Region ?? string.Empty;
    }
}
