using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Repos;
using Microsoft.AspNetCore.Mvc;

namespace BOT_Greg_v2_API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly GuildsRepo _guilds;
    private readonly RolesRepo _roles;
    private readonly BotGregDbContext _db;

    public SettingsController(GuildsRepo guilds, RolesRepo roles, BotGregDbContext db)
    {
        _guilds = guilds;
        _roles = roles;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetSettingsAsync([FromQuery] string guildDiscordId)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        return Ok(_guilds.GetGuildsSettings(guild));
    }

    [HttpPost("")]
    public async Task<IActionResult> SetSettingsAsync([FromQuery] string guildDiscordId, [FromBody] GuildSettingsSetDto settingsDto)
    {
        var guild = await _guilds.GetGuildAsync(guildDiscordId);
        if (guild == null)
            return NotFound();

        bool enablesRoleManagement = settingsDto.RoleManagement && !guild.Settings.RoleManagement;
        bool disablesRoleManagement = !settingsDto.RoleManagement && guild.Settings.RoleManagement;

        if (settingsDto.RoleManagement && !enablesRoleManagement && (settingsDto.AlertRoleDiscordId != null || settingsDto.FilterRoles.Count > 0))
            return Conflict("roleManagement");

        if (enablesRoleManagement)
        {
            if (settingsDto.AlertRoleDiscordId == null)
                return BadRequest("alertRoleRequired");

            var filterIds = guild.Filters.Select(x => x.Id).ToList();
            var filterIdsWithRole = settingsDto.FilterRoles.Select(x => x.FilterId).ToList();

            bool everyFilterHasOneRole = filterIdsWithRole.Count == filterIds.Count && filterIds.All(id => filterIdsWithRole.Contains(id));
            if (!everyFilterHasOneRole)
                return BadRequest("filterRoles");
        }

        GuildSettingsResDto res = new();

        _guilds.SetGuildsSettings(guild, settingsDto);

        if (enablesRoleManagement)
            _roles.EnableRoleManagement(guild, settingsDto.AlertRoleDiscordId!, settingsDto.FilterRoles);

        if (disablesRoleManagement)
            res.RoleDiscordIdsToDelete = _roles.DisableRoleManagement(guild);

        await _db.SaveChangesAsync();

        res.NewSettings = _guilds.GetGuildsSettings(guild);
        return Ok(res);
    }
}
