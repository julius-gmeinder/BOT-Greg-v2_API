# Changes from BOT-Greg-API (v1) to BOT-Greg-v2_API

Everything the bot frontend has to adapt to. Internal changes that the bot does not notice are listed at the end.

## TL;DR for the bot

1. Replace the SignalR client with a server-sent-events client (`GET /api/Alerts/stream`).
2. Remove everything related to news, live matches and stars.
3. Adapt to the new match / result objects (Liquipedia instead of HLTV).
4. Filters: `stars` is gone, `eventTier` is a number now, `hltvCoverage` is new. Filters have an `id` and are deleted by it, `roleDiscordId` is optional.
5. Every new guild starts with a default filter (HLTV-covered matches only), a normal filter that can be deleted. Guilds without filters get every match again. See section 8.
6. All Discord ids got renamed to `...DiscordId` in request bodies, query parameters and responses (`guildId` -> `guildDiscordId`, `roleId` -> `roleDiscordId`, ...), see section 4.
7. **Role management is opt-in** (new setting `roleManagement`, default `false`). Without it the bot must not create any role: no alert role, no filter roles, no role links, alerts without mentions. See section 7.
8. **Settings are read and set as a whole** (`GET` / `POST api/Settings`, like the alert config). All `api/Settings/Toggle...` endpoints are gone. See section 4 (Settings) and section 7.

---

## 1. Alerts: SignalR -> Server-Sent-Events

The hub (`/hub`) no longer exists. Communication is one way (server -> bot), the bot only calls the api endpoints.

| | v1 | v2 |
|---|---|---|
| Connection | SignalR hub `/hub` | `GET /api/Alerts/stream` (`text/event-stream`) |
| Match alerts | method `matchAlerts` | event `matchAlerts` |
| Match delays | method `matchDelays` | event `matchDelays` |
| Result alerts | method `resultAlerts` | event `resultAlerts` |
| News alerts | method `newsAlerts` | removed |
| Keep alive | handled by SignalR | event `heartbeat` every 30 seconds |

- `data` of every alert event is a JSON array, with the same structure the SignalR methods received as argument (see section 3 for the changed objects).
- `data` of `heartbeat` is the current UTC time as JSON string. It can be ignored, or used to detect a dead connection (no heartbeat for more than ~60 seconds -> reconnect).
- There is no replay. Alerts that are sent while the bot is not connected are lost, same as with SignalR. The api logs a warning in that case.
- Reconnecting is up to the client (the `EventSource` implementations do this automatically).

Example (node, `eventsource` package):

```js
const stream = new EventSource(`${API_URL}/api/Alerts/stream`);

stream.addEventListener("matchAlerts", (e) => handleMatchAlerts(JSON.parse(e.data)));
stream.addEventListener("matchDelays", (e) => handleMatchDelays(JSON.parse(e.data)));
stream.addEventListener("resultAlerts", (e) => handleResultAlerts(JSON.parse(e.data)));
```

---

## 2. Removed

| What | Details |
|---|---|
| News | `api/FiltersNews/*`, event `newsAlerts`, `newsAlertChannelId` in the alert config, `newsFilterCount` in the config response, `api/Settings/ToggleMentionOnNews`, `api/Settings/ToggleAlwaysShowNewsImage`, `api/Autocomplete/newstypes`, the static news images (`/imgs/news`) |
| Live matches | `guildsLiveMatches` in the config response. Liquipedia data only contains upcoming matches and results |
| Stars | `stars` in filters, matches and results, `api/Autocomplete/stars`. Liquipedia has no star rating, use `eventTier`, `vrsRank` and `hltvCoverage` instead |
| `guildOwner` | no longer part of the guild data (`POST api/Guilds`, `POST api/Guilds/sync`), extra properties in the body are ignored |
| D-Tier | Liquipedia tiers are Major, S, A, B and C |
| `api/Test/*` | only `api/Test/ping` is left (docker healthcheck). The raw data is available under `api/Liquipedia/matches`, `results`, `events` and `teams` |

---

## 3. Changed objects

All dates are UTC. They are serialized without offset (`2026-11-10T22:00:00`), so parse them as UTC.

### Match (`match` in `matchAlerts` / `matchDelays`, `guildsNextUpcomingMatch`)

| v1 (`UpcomingMatch`) | v2 (`MatchUpcoming`) |
|---|---|
| `url` (hltv match page) | `matchId` (liquipedia id), `hltvUrl` (null if not covered by hltv), `faceitUrl` |
| `event` | `eventName`, `eventUrl`, `eventRelUrl`, `eventId` |
| `eventStage` | `stage` |
| `type` | `bestOf` (number), `environment` (e. g. `Online` / `Offline`) |
| `stars` | removed. New: `tier` (number, see below), `tierString`, `lpTier`, `valveTier`, `isHltvCovered` |
| `teamX` / `teamY` (string) | `teamX` / `teamY` (object, can be null): `{ name, nameShort, players: [{ name, country }] }` |
| `date` + `time` | `dateTime` |
| `createdAt` | removed |
| - | `streams`: `[{ platform, channel, url }]`, platform: `0` Twitch, `1` Youtube, `2` Kick |

Matches without two known teams (TBD) are never alerted, so `teamX` / `teamY` are always set in alerts. They can still be null in `api/Liquipedia/matches`.

### Result (`result` in `resultAlerts`)

Same properties as the match above (without `streams`), plus:

| v1 (`Result`) | v2 (`MatchResult`) |
|---|---|
| `teamX_Name` / `teamY_Name` | `teamX.name` / `teamY.name` |
| `teamX_Score` / `teamY_Score` | `teamXMapScore` / `teamYMapScore` (maps won) |
| `date` | `dateTime` (start of the match) |
| - | `isForfeit` |
| - | `maps`: `[{ name, teamXScore, teamYScore, mapPlayed }]` (round scores, null if the map was not played) |

### Event tier

`tier` / `eventTier` is a number everywhere: `0` Major, `1` S-Tier, `2` A-Tier, `3` B-Tier, `4` C-Tier.
Objects that contain a tier also contain the readable string (`tierString` / `eventTierString`).
The tier of a match is null if Liquipedia uses a tier outside of this list.

### Alert events

Besides `match` / `result`, only the id names in the `guilds` array changed (`channelId` -> `channelDiscordId`, `roleIds` -> `roleDiscordIds`):

```jsonc
// matchAlerts
[{ "match": { ... }, "guilds": [{ "channelDiscordId": "...", "roleDiscordIds": ["..."] }] }]

// matchDelays, match.dateTime is the new start time
[{ "match": { ... }, "guilds": [{ "channelDiscordId": "...", "mentionOnDelays": false, "roleDiscordIds": ["..."] }] }]

// resultAlerts
[{ "result": { ... }, "guilds": [{ "channelDiscordId": "...", "mentionOnResults": true, "censorResults": false, "roleDiscordIds": ["..."] }] }]
```

`roleDiscordIds` no longer contains duplicates. It is an empty array for guilds with disabled role management, the alert has to be sent without any mention then (`mentionOnDelays` / `mentionOnResults` have no effect in that case).

---

## 4. Endpoints

Routes, methods and status codes are the same as in v1 unless listed here.

### Discord ids renamed

Every Discord id is called `...DiscordId` now, so it can't be mixed up with the internal ids of the database. This affects request bodies, query parameters and responses of all endpoints and the alert events. The old names are no longer accepted (a missing `guildDiscordId` query parameter returns `400`).

| v1 | v2 | Where |
|---|---|---|
| `guildId` | `guildDiscordId` | query parameter of every endpoint that takes a guild, bodies of `api/Guilds` (+ `sync`), `api/Filters`, `api/RoleLinks` |
| `roleId` | `roleDiscordId` | filters (`GET` / `POST api/Filters`) |
| `roleIds` | `roleDiscordIds` | body of `DELETE api/Filters`, `guilds` of the alert events |
| `botRoleId` | `botRoleDiscordId` | bodies of `POST` / `DELETE api/RoleLinks`, query parameter of `GET api/RoleLinks/linked`, `GET api/RoleLinks/tree` |
| `linkedRoleId` | `linkedRoleDiscordId` | bodies of `POST` / `DELETE api/RoleLinks` |
| `linkedRoleIds` | `linkedRoleDiscordIds` | `GET api/RoleLinks/tree` |
| `alertRoleId` | `alertRoleDiscordId` | alert config (`GET` / `POST api/Alerts/config`) |
| `matchAlertChannelId` | `matchAlertChannelDiscordId` | alert config |
| `resultAlertChannelId` | `resultAlertChannelDiscordId` | alert config |
| `channelId` | `channelDiscordId` | `guilds` of the alert events |

The tables below already use the new names.

### Alerts

| Endpoint | Change |
|---|---|
| `GET api/Alerts/stream` | new, see section 1 |
| `GET api/Alerts/config` | response without `newsAlertChannelId`. `alertRoleDiscordId` is always null if the role management is disabled |
| `POST api/Alerts/config` | body without `newsAlertChannelId`. Response: `{ newConfig, filterCount, guildsNextUpcomingMatch }` (without `newsFilterCount` and `guildsLiveMatches`). `newConfig` is the config that is actually saved now (v1 echoed the request body, so `alertRoleId` could be null although a role was set). `alertRoleDiscordId` in the body returns `409` with body `roleManagement` if the role management is disabled. The alert role is no longer needed to receive alerts, a channel is enough |

### Filters

| Endpoint | Change |
|---|---|
| `GET api/Filters` | filter: `{ id, nr, roleDiscordId, team, event, eventTier, eventTierString, vrsRank, hltvCoverage }`. `id` is the internal id (Guid) of the filter, `roleDiscordId` is null if the role management is disabled |
| `GET api/Filters/roles` | only contains the roles of filters that have one (empty if the role management is disabled) |
| `POST api/Filters` | body: `{ guildDiscordId, roleDiscordId?, team?, event?, eventTier?, vrsRank?, hltvCoverage? }` |
| `DELETE api/Filters` | body: `{ guildDiscordId, filterIds: ["<id>"] }` (v1: `roleIds`). Response is still the list of role ids the bot has to delete in Discord (empty if the role management is disabled) |

- `stars` removed.
- `eventTier` is a number (`0` - `4`) instead of a string. The match has to be in this tier or above.
- `hltvCoverage` (bool, optional) is new: `true` = only matches covered by hltv, `false` = only matches not covered by hltv, `null` / not set = both.
- `event` can be up to 128 characters long (v1: 64).
- `vrsRank` has to be between 1 and 1000 (v1: 500).
- `team` and `event` have to be the Liquipedia names (the `value` of the autocomplete endpoints). HLTV names from v1 will not match.
- Invalid values still return `400`, but the body is a validation problem object (`{ errors: { ... } }`) now.
- Deleting filters (`DELETE api/Filters`) also deletes the role links of the deleted filter roles. Unknown ids are ignored.
- `roleDiscordId` of `POST api/Filters` depends on the role management of the guild: enabled = required (`400` with body `roleRequired` if missing), disabled = must not be set (`409` with body `roleManagement`). So the bot only creates the filter role if the role management is enabled.

### Autocomplete

| Endpoint | Change |
|---|---|
| `GET api/Autocomplete/teams` | data from Liquipedia (teams with a VRS rank), same structure |
| `GET api/Autocomplete/events` | data from Liquipedia (running and upcoming events), same structure |
| `GET api/Autocomplete/tiers` | `value` is a number now (`[{ name: "🤩 S-Tier", value: 1 }]`), no D-Tier |
| `GET api/Autocomplete/vrs` | unchanged |
| `GET api/Autocomplete/stars`, `newstypes` | removed |

### Guilds

| Endpoint | Change |
|---|---|
| `POST api/Guilds`, `POST api/Guilds/sync` | guild data: `{ guildDiscordId, guildName, guildMemberCount }` (no `guildOwner`). Every added guild gets the default filter (section 8) |
| `PUT api/Guilds/reset` | still returns the role ids to delete (empty if the role management was disabled). Also disables the role management and adds the default filter again, so the guild is in the same state as a new guild |

### RoleLinks

| Endpoint | Change |
|---|---|
| `POST api/RoleLinks` | `botRoleDiscordId` has to be a bot role of the given guild (alert role or filter role), otherwise `404`. v1 accepted bot roles of any guild. Returns `409` with body `roleManagement` if the role management is disabled |
| `GET api/RoleLinks/tree`, `bot`, `linked` | empty if the role management is disabled (disabling it deletes all links) |

### Roles

| Endpoint | Change |
|---|---|
| `GET api/Roles` | empty if the role management is disabled |
| `GET api/Roles/default` | `404` if the role management is disabled |

### Settings

The settings work like the alert config now: one endpoint to get all of them, one to set all of them. The single toggles are removed.
(The settings are stored in their own table, see section 6, the bot does not notice that.)

| Endpoint | Change |
|---|---|
| `GET api/Settings?guildDiscordId=` | new. `{ alertsPaused, mentionOnDelays, mentionOnResults, censorResults, roleManagement }`, `404` for unknown guilds |
| `POST api/Settings?guildDiscordId=` | new, sets all settings at once (see below) |
| `PATCH api/Settings/ToggleMentionOnDelays`, `ToggleMentionOnResults`, `ToggleCensorResults`, `TogglePauseAlerts` | removed, use `POST api/Settings` |
| `PATCH api/Settings/ToggleMentionOnNews`, `ToggleAlwaysShowNewsImage` | removed (no news) |

```jsonc
// POST api/Settings?guildDiscordId=...
{
  "alertsPaused": false,
  "mentionOnDelays": false,
  "mentionOnResults": true,
  "censorResults": false,
  "roleManagement": false
}

// 200
{
  "newSettings": { "alertsPaused": false, "mentionOnDelays": false, "mentionOnResults": true, "censorResults": false, "roleManagement": false },
  "roleDiscordIdsToDelete": []
}
```

- All five settings are required. A missing one returns `400` (validation problem object), it is never silently set to `false`. So the bot reads the settings with `GET`, changes what it wants and sends everything back.
- The guild is a query parameter (`guildDiscordId`), not part of the body (same as `POST api/Alerts/config`).
- Settings that already have the sent value just stay. There is no `409` for "already has this value" anymore, sending the unchanged settings returns `200`.
- `newSettings` is what is actually saved now.
- `roleManagement` is the only setting that needs more than its value: enabling it needs the role ids in the same body (`alertRoleDiscordId`, `filterRoles`), disabling it fills `roleDiscordIdsToDelete`. See section 7.
- The request is all or nothing: if it fails (e. g. because of the roles), none of the settings is saved.

| Status | Body | Meaning |
|---|---|---|
| `200` | `{ newSettings, roleDiscordIdsToDelete }` | saved |
| `400` | validation problem object | a setting is missing / invalid body |
| `400` | `alertRoleRequired`, `filterRoles` | role management gets enabled without (all of) its roles, see section 7 |
| `404` | | unknown guild |
| `409` | `roleManagement` | roles were sent, but the role management is enabled already, see section 7 |

---

## 5. Changed behavior

- **Default filter:** Liquipedia covers a lot more matches than HLTV, so every new guild starts with a filter that only lets HLTV-covered matches through (section 8). Guilds without filters get every match and result, same as in v1. A filter without any property matches everything.
- **Roles are optional:** a guild only needs a channel to get alerts, the alert role is no longer required (section 7).
- **Linked roles:** roles linked to the alert role are pinged for guilds without filters too (v1 only added them if the guild had filters).
- **Match alerts:** sent for matches that start within the next 5 minutes, once per match.
- **Match delays:** sent when an already alerted match is upcoming again with a different start time. v1 only looked at the next 6 hours, v2 looks at all fetched matches (next 25 hours).
- **Result alerts:** sent for every finished match of the last 24 hours that was not processed yet.
- **History:** every processed match / result is saved, also if no guild was alerted (`AlertsSent = 0`). In v1 those were skipped, so a guild that finished its setup later got flooded with all results of the day.
- **VRS rank filter:** unchanged (both teams have to be ranked within the top x), but the ranks come from the Valve standings, matched to Liquipedia teams.
- **Event filter:** compares the filter with `eventName` of the match (case insensitive).

---

## 6. Database

New database, the tables of v1 are not migrated. Run `dotnet ef database update` (or `Update-Database`) to apply the new migrations `AddedAlertTables`, `MovedSettingsToGuildSettings`, `RenamedDiscordIds` (only renames columns and an index, no data is lost) and `AddedRoleManagement`.

- Every table has a GuidV7 `Id` as primary key, plus `CreatedAt` and `UpdatedAt` (`BaseEntity`).
- Discord ids are normal columns named `...DiscordId` (`Guilds.DiscordId`, `Guilds.AlertRoleDiscordId`, `Filters.RoleDiscordId`, `RoleLinks.BotRoleDiscordId`, ...). `Guilds.DiscordId` is unique.
- Settings, filters and role links reference the guild by its internal `Id` (foreign key column `GuildId`, cascade delete). `GuildId` is always the internal Guid, never the Discord id.
- Tables: `Guilds`, `GuildSettings`, `Filters`, `RoleLinks`, `HistoryMatchAlerts`, `HistoryResultAlerts`.
- `Guilds` only holds the guild data and the alert config (`AlertRoleDiscordId`, `MatchAlertsChannelDiscordId`, `ResultAlertsChannelDiscordId`). The settings (`AlertsPaused`, `MentionOnDelays`, `MentionOnResults`, `CensorResults`, `RoleManagement`) are in `GuildSettings`, one row per guild (`GuildSettings.GuildId` is unique). Every new guild gets its settings row with the defaults and its default filter when it is created.
- `AddedRoleManagement` adds `GuildSettings.RoleManagement` (default `false`) and makes `Filters.RoleDiscordId` nullable. Already existing guilds are migrated so nothing changes for them: guilds that already have an alert role or filters (and therefore roles) get `RoleManagement = true`, guilds without filters get the default filter as a row. Reverting the migration deletes all filters without role.
- One leftover of that migration: an existing guild with an alert role but without filters has `RoleManagement = true` and a default filter without role. The api handles that (the filter just pings no extra role), disabling and enabling the role management once gives the filter its role.
- `MovedSettingsToGuildSettings` copies the settings of already existing guilds from the old `Guilds` columns into `GuildSettings` before it drops the columns (and back, when it is reverted).
- Removed tables: `FiltersNews`, `HistoryNewsAlerts` and all `HLTV_*` tables. Matches, results, events and teams are no longer stored, they come from the (cached) `LiquipediaService`.
- `HistoryMatchAlerts` / `HistoryResultAlerts` use the Liquipedia `MatchId` (unique) instead of the HLTV url.

---

## 7. Role management (opt-in)

New setting `roleManagement` per guild, default `false`. It controls every role the bot creates: the alert role and the one role per filter. Role links only exist with role management.

| | disabled (default) | enabled |
|---|---|---|
| Alert role | none (`alertRoleDiscordId` is null) | exists, created when the setting is enabled |
| Filters | no role (`roleDiscordId` is null) | one role per filter, like in v1 |
| Role links | none, can not be created | like in v1 |
| Alerts | `roleDiscordIds` is empty -> no mention | `roleDiscordIds` = alert role + roles of the matching filters + linked roles |
| `Manage Roles` permission | not needed | needed |

The api never talks to Discord. The bot creates and deletes the roles, the api only tells it which ones.

### Enabling

1. `GET api/Filters` to get the filters of the guild (the default filter is one of them).
2. Create the alert role and one role per filter in Discord.
3. Send all of them together with the settings:

```jsonc
// POST api/Settings?guildDiscordId=...
{
  "alertsPaused": false,
  "mentionOnDelays": false,
  "mentionOnResults": true,
  "censorResults": false,
  "roleManagement": true,
  "alertRoleDiscordId": "...",
  "filterRoles": [{ "filterId": "<id of the filter>", "roleDiscordId": "..." }]
}

// 200
{ "newSettings": { ..., "roleManagement": true }, "roleDiscordIdsToDelete": [] }
```

`alertRoleDiscordId` and `filterRoles` are only needed (and only allowed) in a request that turns the role management from off to on. Leave them out in every other request.

If the request fails, nothing was saved (also none of the other settings) and the bot has to delete the roles it just created (same as v1 does when adding a filter fails).

### Disabling

```jsonc
// POST api/Settings?guildDiscordId=...
{ "alertsPaused": false, "mentionOnDelays": false, "mentionOnResults": true, "censorResults": false, "roleManagement": false }

// 200, the bot has to delete these roles in Discord
{ "newSettings": { ..., "roleManagement": false }, "roleDiscordIdsToDelete": ["<alert role>", "<filter role>", "..."] }
```

The api removes the alert role, the roles of all filters and all role links of the guild. The filters themselves stay.

Careful: the body always contains the whole settings, so `roleManagement: false` disables the role management whenever it is enabled, also if the bot only wanted to change another setting and read the settings before someone else enabled it. The bot should therefore always delete the roles of `roleDiscordIdsToDelete`, not only when it expects some.

### Role management status codes of `POST api/Settings`

| Status | Body | Meaning |
|---|---|---|
| `200` | `{ newSettings, roleDiscordIdsToDelete }` | saved |
| `404` | | unknown guild |
| `400` | `alertRoleRequired` | enabling without `alertRoleDiscordId` |
| `400` | `filterRoles` | enabling, but `filterRoles` does not contain exactly one role for every filter of the guild (e. g. a filter was added or deleted in the meantime) |
| `409` | `roleManagement` | `roleManagement` is `true` and roles were sent, but the role management is enabled already (e. g. by a second admin in the meantime). The sent roles were not saved, the bot has to delete them |

`roleManagement: true` without roles while it is enabled already is the normal case (nothing changes) and returns `200`.

### Locked while disabled

| Request | Response |
|---|---|
| `POST api/Filters` with `roleDiscordId` | `409`, body `roleManagement` |
| `POST api/Alerts/config` with `alertRoleDiscordId` | `409`, body `roleManagement` |
| `POST api/RoleLinks` | `409`, body `roleManagement` |

Everything else needs no lock, it just returns nothing: `GET api/Roles`, `GET api/Filters/roles`, `GET api/RoleLinks/tree` / `bot` / `linked` are empty, `GET api/Roles/default` and `DELETE api/RoleLinks` return `404`.

### What has to change in the bot (hints)

The bot should read `roleManagement` from `GET api/Settings` instead of guessing it from the roles.

| Where (v1 file) | Change |
|---|---|
| `commands/settings` | one `/settings` command without subcommands (the commands have to be deployed again): a container that shows every setting with a toggle button, plus save / cancel (like `/alerts config`). Saving sends all settings with one `POST api/Settings`. Role management: check `Manage Roles` before enabling. Enabling = create roles, then call the api (delete them again if it fails). Disabling = call the api, then delete the returned roles (warn before saving, members lose these roles) |
| `commands/alerts/config.ts` | no longer creates the alert role (it exists if and only if the role management is enabled) and does not need `Manage Roles` anymore. Never send `alertRoleDiscordId` while disabled. The alert role section of the container should show the role, or a hint how to enable the role management |
| `commands/filters/add` | only create the filter role (and check `Manage Roles`) if enabled, otherwise send the filter without `roleDiscordId`. The success message can not show a role mention then |
| `commands/filters/list.ts` | `roleDiscordId` can be null, show the filter without role mention |
| `commands/filters/delete.ts` | the select menu has to be built from `GET api/Filters` (value = `id`, label = properties of the filter or its role name) instead of the role ids, and sends `filterIds`. The permission check is only needed if roles have to be deleted |
| `commands/misc/roles.ts` | no roles to choose if disabled, the message should say that the role management is disabled (v1: "Alerts are currently not configured") |
| `commands/role/*` (link, unlink, list) | locked if disabled, show a hint instead of the select menus |
| `commands/misc/reset.ts` | `Manage Roles` is only needed if the returned list is not empty. The text should mention that the default filter comes back |
| alert services (`matchAlertsService`, `matchDelaysService`, `resultAlertsService`) | `roleDiscordIds` can be empty, send the message without mention then (no empty mention string) |
| `commands/misc/commands.ts` / help texts | mention that roles are optional and how to enable them |

---

## 8. Default filter

The default filter is no longer applied in code, it is a real row in `Filters`: `{ hltvCoverage: true }`, everything else null.

- It is added when a guild is created (`POST api/Guilds`, `POST api/Guilds/sync`) and when a guild is reset (`PUT api/Guilds/reset`).
- It is a normal filter in every way: part of `GET api/Filters` (usually `nr` 1), counts towards the limit of 20 filters and the duplicate check (adding `{ hltvCoverage: true }` again returns `409 duplicate`), gets its own role when the role management is enabled, and can be deleted with `DELETE api/Filters`.
- There is no flag that marks it. If the bot wants to label it ("default"), it can check for `hltvCoverage === true` with all other properties null.
- Deleting it has a consequence the bot should point out: a guild without any filter gets **every** match and result Liquipedia knows (a lot more than HLTV covered). With other filters left, only those count.
- It can be added again like every other filter (`POST api/Filters` with `hltvCoverage: true`).

UI hints: the texts in `alerts/config.ts` that depend on `filterCount === 0` ("you will get an alert for every match") are still correct, but new guilds have `filterCount` 1 now. `filters/list.ts` no longer hits the "no filters" case for new guilds.

---

## 9. Internal changes (not relevant for the bot)

- `LiquipediaService`: cache time of matches and results is 5 minutes (was 3). All `Get...Async` methods have an optional `refetch` parameter, that skips the cache and replaces the cached data.
- `BackgroundWorker`: runs every 5 minutes (synced to the clock), refetches matches and results once and passes them to `MatchDelaysService`, `MatchAlertsService` and `ResultAlertsService`. An exception in a run is logged and does not stop the worker.
- The 24 hour worker is gone, teams and events are cached for 24 hours by the `LiquipediaService`.
- `AlertsHelper` has one filter comparison for matches and results (v1 had a copy for upcoming matches, live matches and results). Guilds, settings, filters and role links are loaded with one query per run instead of one query per guild / role.
- `AlertStreamService` (singleton) holds the connected stream clients, `AlertStreamHeartbeat` sends the heartbeat.
- Repos take the already loaded `Guild` instead of loading it a second time by its id.
- `AlertsHelper` no longer has a hardcoded default filter. `GetRolesToPing` returns an empty list for guilds with disabled role management (null still means "do not alert").
- `AlertsRepo` no longer requires an alert role for alert guilds, only the channel.
- `RolesRepo` enables / disables the role management (`EnableRoleManagement`, `DisableRoleManagement`), `GuildsRepo` sets the other settings (`SetGuildsSettings`) and creates the default filter (`CreateDefaultFilter`). None of the three saves, the `SettingsController` saves all settings with one `SaveChangesAsync`.
- DTOs of the settings: `GuildSettingsDto` (get), `GuildSettingsSetDto` (set, adds the roles), `GuildSettingsResDto` and `FilterRoleDto`. `ToggleSettingDto`, `ToggleRoleManagementDto` and `ToggleRoleManagementResDto` are removed.
- `MatchBase` has the new computed properties `Tier`, `TierString` and `IsHltvCovered`.
- The validation of request bodies moved from the controllers to attributes of the DTOs.
- AutoMapper, SignalR, Playwright and HtmlAgilityPack are no longer used.
