namespace BOT_Greg_v2_API.Models.Dtos;

public class ToggleRoleManagementResDto
{
    public bool RoleManagement { get; set; }

    // roles the bot has to delete in discord, always empty if the role management got enabled
    public List<string> RoleDiscordIdsToDelete { get; set; } = new();
}
