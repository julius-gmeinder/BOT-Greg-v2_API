namespace BOT_Greg_v2_API.Models.Dtos;

public class RoleLinksTreeItemDto
{
    public string BotRoleId { get; set; } = null!;
    public List<string> LinkedRoleIds { get; set; } = new();
}
