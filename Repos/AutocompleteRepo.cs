using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Enums;

namespace BOT_Greg_v2_API.Repos;

public class AutocompleteRepo
{
    private const string DefaultTierEmoji = "💤 ";

    private readonly Dictionary<EventTier, string> _tierEmojiMap = new()
    {
        { EventTier.Major, "🤯 " },
        { EventTier.S_Tier, "🤩 " },
        { EventTier.A_Tier, "🥱 " },
        { EventTier.B_Tier, "💤 " },
        { EventTier.C_Tier, "💤 " },
    };

    private readonly List<AutocompleteIntDto> _acVrs = new()
    {
        new AutocompleteIntDto() { Name = "🤯 Top 5", Value = 5 },
        new AutocompleteIntDto() { Name = "🤩 Top 10", Value = 10 },
        new AutocompleteIntDto() { Name = "😎 Top 20", Value = 20 },
        new AutocompleteIntDto() { Name = "🙂 Top 30", Value = 30 },
        new AutocompleteIntDto() { Name = "🤨 Top 50", Value = 50 },
        new AutocompleteIntDto() { Name = "🥱 Top 100", Value = 100 },
    };

    public string GetRankEmoji(int? vrsRank)
    {
        return vrsRank switch
        {
            1 => "🥇 ",
            2 => "🥈 ",
            3 => "🥉 ",
            >= 4 and <= 25 => "🏅 ",
            _ => ""
        };
    }

    public string GetTierEmoji(EventTier? tier)
    {
        if (tier == null)
            return DefaultTierEmoji;

        return _tierEmojiMap.GetValueOrDefault(tier.Value, DefaultTierEmoji);
    }

    public List<AutocompleteIntDto> GetTiers()
    {
        return Enum.GetValues<EventTier>()
            .Select(tier => new AutocompleteIntDto()
            {
                Name = $"{GetTierEmoji(tier)}{tier.GetString()}",
                Value = (int)tier
            })
            .ToList();
    }

    public List<AutocompleteIntDto> GetVrs()
    {
        return _acVrs;
    }
}
