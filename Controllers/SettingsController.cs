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
    private readonly BotGregDbContext _db;

    public SettingsController(GuildsRepo guilds, BotGregDbContext db)
    {
        _guilds = guilds;
        _db = db;
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

    // all settings are toggled the same way, they only differ in the property of the guild settings that is read and set
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
