using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Models.Dtos;
using BOT_Greg_v2_API.Models.Entities;
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

    [HttpPatch("ToggleMentionOnDelays")]
    public async Task<IActionResult> ToggleMentionOnDelaysAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, settings => settings.MentionOnDelays, (settings, value) => settings.MentionOnDelays = value);
    }

    [HttpPatch("ToggleMentionOnResults")]
    public async Task<IActionResult> ToggleMentionOnResultsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, settings => settings.MentionOnResults, (settings, value) => settings.MentionOnResults = value);
    }

    [HttpPatch("ToggleCensorResults")]
    public async Task<IActionResult> ToggleCensorResultsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, settings => settings.CensorResults, (settings, value) => settings.CensorResults = value);
    }

    [HttpPatch("TogglePauseAlerts")]
    public async Task<IActionResult> TogglePauseAlertsAsync([FromBody] ToggleSettingDto toggleDto)
    {
        return await ToggleSettingAsync(toggleDto, settings => settings.AlertsPaused, (settings, value) => settings.AlertsPaused = value);
    }

    // toggling the role management is more than flipping a value, as all roles of the bot depend on it:
    // enabling:  the bot creates the alert role and one role per filter in discord first, then sends their ids
    // disabling: the api removes all roles and role links, and returns the roles the bot has to delete in discord
    [HttpPatch("ToggleRoleManagement")]
    public async Task<IActionResult> ToggleRoleManagementAsync([FromBody] ToggleRoleManagementDto toggleDto)
    {
        var guild = await _guilds.GetGuildAsync(toggleDto.GuildDiscordId);
        if (guild == null)
            return NotFound();

        if (toggleDto.NewValue == guild.Settings.RoleManagement)
            return Conflict();

        ToggleRoleManagementResDto res = new()
        {
            RoleManagement = toggleDto.NewValue
        };

        if (!toggleDto.NewValue)
        {
            res.RoleDiscordIdsToDelete = await _roles.DisableRoleManagementAsync(guild);
            return Ok(res);
        }

        if (toggleDto.AlertRoleDiscordId == null)
            return BadRequest("alertRoleRequired");

        // every filter of the guild needs exactly one role, otherwise filters without role would be left over
        var filterIds = guild.Filters.Select(x => x.Id).ToList();
        var filterIdsWithRole = toggleDto.FilterRoles.Select(x => x.FilterId).ToList();

        bool everyFilterHasOneRole = filterIdsWithRole.Count == filterIds.Count && filterIds.All(id => filterIdsWithRole.Contains(id));
        if (!everyFilterHasOneRole)
            return BadRequest("filterRoles");

        await _roles.EnableRoleManagementAsync(guild, toggleDto.AlertRoleDiscordId, toggleDto.FilterRoles);
        return Ok(res);
    }

    // all other settings are toggled the same way, they only differ in the property of the guild settings that is read and set
    private async Task<IActionResult> ToggleSettingAsync(ToggleSettingDto toggleDto, Func<GuildSettings, bool> getSetting, Action<GuildSettings, bool> setSetting)
    {
        var guild = await _guilds.GetGuildAsync(toggleDto.GuildDiscordId);
        if (guild == null)
            return NotFound();

        if (toggleDto.NewValue == getSetting(guild.Settings))
            return Conflict();

        setSetting(guild.Settings, toggleDto.NewValue);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
