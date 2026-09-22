using BOT_Greg_v2_API.Models.Enums;

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
}
public static class StreamPlatformExtensions
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
}