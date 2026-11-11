namespace BOT_Greg_v2_API.Models.Dtos;

public class RoleLinksTreeItemDto
{
    public string BotRoleDiscordId { get; set; } = null!;
    public List<string> LinkedRoleDiscordIds { get; set; } = new();
}
