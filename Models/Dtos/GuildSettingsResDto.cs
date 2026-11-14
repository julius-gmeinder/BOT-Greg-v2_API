namespace BOT_Greg_v2_API.Models.Dtos;

public class GuildSettingsResDto
{
    // the settings that are actually saved now
    public GuildSettingsDto NewSettings { get; set; } = null!;

    // roles the bot has to delete in discord, only filled if the role management got disabled
    public List<string> RoleDiscordIdsToDelete { get; set; } = new();
}
