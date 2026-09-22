using BOT_Greg_v2_API.Models.Enums;

namespace BOT_Greg_v2_API.Models.External
{
    public class Event
    {
        public int EventId { get; init; }
        public string EventName { get; init; } = null!;
        public string EventRelUrl { get; init; } = null!;
        public string EventUrl => $"{Constants.LpBaseUrl}/{EventRelUrl}";

        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }

        public string Environment { get; init; } = null!;

        public EventTier Tier { get; init; }
        public string? TierString => Tier.GetString();
        public string LpTier { get; init; } = null!;
        public string ValveTier { get; init; } = null!;

        public double? PrizePool { get; init; }
        public int? TeamsAttending { get; init; }

        public string? Region { get; init; }
        public string? Country { get; init; }
        public string? City { get; init; }
        public string LocationString => BuildLocationString();


        private string BuildLocationString()
        {
            if(City != null && Country != null)
                return $"{City}, {Country}";

            if(Country != null)
                return Country;

            if(Region != null)
                return Region;

            return string.Empty;
        }
    }
}
