# Changes from BOT-Greg-API (v1) to BOT-Greg-v2_API

Everything the bot frontend has to adapt to. Internal changes that the bot does not notice are listed at the end.

## TL;DR for the bot

1. Replace the SignalR client with a server-sent-events client (`GET /api/Alerts/stream`).
2. Remove everything related to news, live matches and stars.
3. Adapt to the new match / result objects (Liquipedia instead of HLTV).
4. Filters: `stars` is gone, `eventTier` is a number now, `hltvCoverage` is new.
5. Guilds without filters only get HLTV-covered matches (default filter).

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

Only `match` / `result` changed, the `guilds` array is the same as before:

```jsonc
// matchAlerts
[{ "match": { ... }, "guilds": [{ "channelId": "...", "roleIds": ["..."] }] }]

// matchDelays, match.dateTime is the new start time
[{ "match": { ... }, "guilds": [{ "channelId": "...", "mentionOnDelays": false, "roleIds": ["..."] }] }]

// resultAlerts
[{ "result": { ... }, "guilds": [{ "channelId": "...", "mentionOnResults": true, "censorResults": false, "roleIds": ["..."] }] }]
```

`roleIds` no longer contains duplicates.

---

## 4. Endpoints

Routes, methods and status codes are the same as in v1 unless listed here.

### Alerts

| Endpoint | Change |
|---|---|
| `GET api/Alerts/stream` | new, see section 1 |
| `GET api/Alerts/config` | response without `newsAlertChannelId` |
| `POST api/Alerts/config` | body without `newsAlertChannelId`. Response: `{ newConfig, matResFilterCount, guildsNextUpcomingMatch }` (without `newsFilterCount` and `guildsLiveMatches`). `newConfig` is the config that is actually saved now (v1 echoed the request body, so `alertRoleId` could be null although a role was set) |

### FiltersMatRes

| Endpoint | Change |
|---|---|
| `GET api/FiltersMatRes` | filter: `{ nr, roleId, team, event, eventTier, eventTierString, vrsRank, hltvCoverage }` |
| `POST api/FiltersMatRes` | body: `{ guildId, roleId, team?, event?, eventTier?, vrsRank?, hltvCoverage? }` |

- `stars` removed.
- `eventTier` is a number (`0` - `4`) instead of a string. The match has to be in this tier or above.
- `hltvCoverage` (bool, optional) is new: `true` = only matches covered by hltv, `false` = only matches not covered by hltv, `null` / not set = both.
- `event` can be up to 128 characters long (v1: 64).
- `vrsRank` has to be between 1 and 1000 (v1: 500).
- `team` and `event` have to be the Liquipedia names (the `value` of the autocomplete endpoints). HLTV names from v1 will not match.
- Invalid values still return `400`, but the body is a validation problem object (`{ errors: { ... } }`) now.
- Deleting filters (`DELETE api/FiltersMatRes`) also deletes the role links of the deleted filter roles.

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
| `POST api/Guilds`, `POST api/Guilds/sync` | guild data: `{ guildId, guildName, guildMemberCount }` (no `guildOwner`) |

### RoleLinks

| Endpoint | Change |
|---|---|
| `POST api/RoleLinks` | `botRoleId` has to be a bot role of the given guild (alert role or filter role), otherwise `404`. v1 accepted bot roles of any guild |

### Settings

`ToggleMentionOnDelays`, `ToggleMentionOnResults`, `ToggleCensorResults` and `TogglePauseAlerts` are unchanged. The two news toggles are removed.

---

## 5. Changed behavior

- **Default filter:** Liquipedia covers a lot more matches than HLTV. Guilds without filters got every match in v1, in v2 they only get matches and results that are covered by HLTV (`isHltvCovered`). As soon as a guild has at least one filter, only its own filters count. A filter without any property (only `roleId`) matches everything.
- **Linked roles:** roles linked to the alert role are pinged for guilds without filters too (v1 only added them if the guild had filters).
- **Match alerts:** sent for matches that start within the next 5 minutes, once per match.
- **Match delays:** sent when an already alerted match is upcoming again with a different start time. v1 only looked at the next 6 hours, v2 looks at all fetched matches (next 25 hours).
- **Result alerts:** sent for every finished match of the last 24 hours that was not processed yet.
- **History:** every processed match / result is saved, also if no guild was alerted (`AlertsSent = 0`). In v1 those were skipped, so a guild that finished its setup later got flooded with all results of the day.
- **VRS rank filter:** unchanged (both teams have to be ranked within the top x), but the ranks come from the Valve standings, matched to Liquipedia teams.
- **Event filter:** compares the filter with `eventName` of the match (case insensitive).

---

## 6. Database

New database, the tables of v1 are not migrated. Run `dotnet ef database update` (or `Update-Database`) to apply the new migration `AddedAlertTables`.

- Every table has a GuidV7 `Id` as primary key, plus `CreatedAt` and `UpdatedAt` (`BaseEntity`).
- Discord ids (`GuildId`, `RoleId`, channel ids, ...) are normal columns, `Guilds.GuildId` is unique.
- Filters and role links reference the guild by its `Id` (foreign key column `GuildId`, cascade delete).
- Tables: `Guilds`, `FiltersMatRes`, `RoleLinks`, `HistoryMatchAlerts`, `HistoryResultAlerts`.
- Removed tables: `FiltersNews`, `HistoryNewsAlerts` and all `HLTV_*` tables. Matches, results, events and teams are no longer stored, they come from the (cached) `LiquipediaService`.
- `HistoryMatchAlerts` / `HistoryResultAlerts` use the Liquipedia `MatchId` (unique) instead of the HLTV url.

---

## 7. Internal changes (not relevant for the bot)

- `LiquipediaService`: cache time of matches and results is 5 minutes (was 3). All `Get...Async` methods have an optional `refetch` parameter, that skips the cache and replaces the cached data.
- `BackgroundWorker`: runs every 5 minutes (synced to the clock), refetches matches and results once and passes them to `MatchDelaysService`, `MatchAlertsService` and `ResultAlertsService`. An exception in a run is logged and does not stop the worker.
- The 24 hour worker is gone, teams and events are cached for 24 hours by the `LiquipediaService`.
- `AlertsHelper` has one filter comparison for matches and results (v1 had a copy for upcoming matches, live matches and results). Guilds, filters and role links are loaded with one query per run instead of one query per guild / role.
- `AlertStreamService` (singleton) holds the connected stream clients, `AlertStreamHeartbeat` sends the heartbeat.
- Repos take the already loaded `Guild` instead of loading it a second time by its id.
- `MatchBase` has the new computed properties `Tier`, `TierString` and `IsHltvCovered`.
- The validation of request bodies moved from the controllers to attributes of the DTOs.
- AutoMapper, SignalR, Playwright and HtmlAgilityPack are no longer used.
