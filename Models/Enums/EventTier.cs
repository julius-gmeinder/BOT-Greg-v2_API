namespace BOT_Greg_v2_API.Models.Enums
{
    public enum EventTier
    {
        Major = 0,
        S_Tier = 1,
        A_Tier = 2,
        B_Tier = 3,
        C_Tier = 4,
    }

    public static class EventTierExtensions
    {
        public static string? GetString(this EventTier tier)
        {
            return tier switch
            {
                EventTier.Major => "Major",
                EventTier.S_Tier => "S-Tier",
                EventTier.A_Tier => "A-Tier",
                EventTier.B_Tier => "B-Tier",
                EventTier.C_Tier => "C-Tier",
                _ => null
            };
        }

        // returns null if liquipedia uses a tier that is not part of the enum (e. g. "-1" for misc events)
        public static EventTier? FromLiquipedia(string lpTier, string valveTier)
        {
            if (valveTier == "Major Championship")
                return EventTier.Major;

            if (!int.TryParse(lpTier, out int tier))
                return null;

            if (!Enum.IsDefined((EventTier)tier))
                return null;

            return (EventTier)tier;
        }
    }
}
