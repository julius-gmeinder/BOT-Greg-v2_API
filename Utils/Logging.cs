using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace BOT_Greg_v2_API.Utils;

/*
 * Everything related to the console logs, registered in the Program.cs. The logs are written in this format:
 * 21:05:01 INF [alerts] [matches] Found 3 starting matches, sending 2 match alerts
 *
 * LoggingTags:             which tags a class gets and which color a tag has (the part that is edited from time to time)
 * AnsiColors:              the color codes
 * LoggingFormatterOptions: the settings of the appsettings (Logging:Console:FormatterOptions)
 * LoggingFormatter:        builds the lines
 */

// decides which tags a log line gets and which color a tag has
// the tags come from the class that writes the log (the "category" of its ILogger<T>), so the messages themselves stay free of tags
public static class LoggingTags
{
    // class name -> tags
    // classes of this project that are not listed here get a tag made from their name, see GetTags
    private static readonly Dictionary<string, string[]> TagsByClassName = new()
    {
        { "BackgroundWorker", new[] { "worker" } },
        { "AlertStreamHeartbeat", new[] { "stream", "heartbeat" } },
        { "AlertStreamService", new[] { "stream" } },
        { "MatchAlertsService", new[] { "alerts", "matches" } },
        { "MatchDelaysService", new[] { "alerts", "delays" } },
        { "ResultAlertsService", new[] { "alerts", "results" } },
        { "LiquipediaService", new[] { "liquipedia" } },
        { "VrsService", new[] { "vrs" } },
        { "AlertsHelper", new[] { "alerts" } },

        // app.Logger in the Program.cs uses the name of the project as category
        { "BOT-Greg-v2_API", new[] { "app" } },
    };

    // start of the category -> tags, for everything that is logged by the framework
    // the first match wins, so longer prefixes have to be listed before shorter ones
    private static readonly List<KeyValuePair<string, string[]>> TagsByCategoryPrefix = new()
    {
        new("Microsoft.Hosting.Lifetime", new[] { "host" }),
        new("Microsoft.Extensions.Hosting", new[] { "host" }),
        new("Microsoft.EntityFrameworkCore", new[] { "database" }),
        new("System.Net.Http", new[] { "http" }),
        new("Microsoft.AspNetCore", new[] { "aspnet" }),
        new("Microsoft", new[] { "dotnet" }),
        new("System", new[] { "dotnet" }),
    };

    // tag -> color
    // tags that are not listed here get a color from the FallbackColors, see GetColor
    private static readonly Dictionary<string, string> ColorsByTag = new()
    {
        { "worker", AnsiColors.BrightBlue },
        { "stream", AnsiColors.BrightCyan },
        { "heartbeat", AnsiColors.Cyan },
        { "alerts", AnsiColors.BrightMagenta },
        { "matches", AnsiColors.BrightGreen },
        { "delays", AnsiColors.Yellow },
        { "results", AnsiColors.Cyan },
        { "liquipedia", AnsiColors.Blue },
        { "vrs", AnsiColors.Magenta },
        { "api", AnsiColors.Green },
        { "repo", AnsiColors.Green },
        { "database", AnsiColors.Yellow },
        { "app", AnsiColors.White },

        // framework tags stay unobtrusive
        { "host", AnsiColors.Gray },
        { "http", AnsiColors.Gray },
        { "aspnet", AnsiColors.Gray },
        { "dotnet", AnsiColors.Gray },
    };

    // red and bright yellow are missing on purpose, they are reserved for errors and warnings
    private static readonly string[] FallbackColors =
    {
        AnsiColors.Cyan,
        AnsiColors.Magenta,
        AnsiColors.Blue,
        AnsiColors.Green,
        AnsiColors.BrightCyan,
        AnsiColors.BrightMagenta,
        AnsiColors.BrightBlue,
        AnsiColors.BrightGreen,
    };

    private static readonly string OwnNamespace = typeof(LoggingTags).Namespace!.Split('.')[0];

    public static string[] GetTags(string category)
    {
        // e. g. "BOT_Greg_v2_API.Services.Alerts.MatchAlertsService" -> "MatchAlertsService"
        string className = category.Substring(category.LastIndexOf('.') + 1);

        if (TagsByClassName.TryGetValue(className, out var tags))
            return tags;

        foreach (var entry in TagsByCategoryPrefix)
        {
            if (category.StartsWith(entry.Key))
                return entry.Value;
        }

        if (category.StartsWith(OwnNamespace))
            return GetTagsFromClassName(className);

        // unknown library
        return new[] { className.ToLowerInvariant() };
    }

    public static string GetColor(string tag)
    {
        if (ColorsByTag.TryGetValue(tag, out var color))
            return color;

        // string.GetHashCode() changes with every start of the app, this sum keeps the color of a tag the same forever
        int sum = 0;

        foreach (char letter in tag)
            sum += letter;

        return FallbackColors[sum % FallbackColors.Length];
    }

    // fallback for classes of this project that are not in the list above
    // "FiltersController" -> [api] [filters], "GuildsRepo" -> [repo] [guilds], "JsonTools" -> [jsontools]
    private static string[] GetTagsFromClassName(string className)
    {
        if (className.EndsWith("Controller"))
            return new[] { "api", RemoveSuffix(className, "Controller") };

        if (className.EndsWith("Repo"))
            return new[] { "repo", RemoveSuffix(className, "Repo") };

        if (className.EndsWith("Service"))
            return new[] { RemoveSuffix(className, "Service") };

        return new[] { className.ToLowerInvariant() };
    }

    private static string RemoveSuffix(string className, string suffix)
    {
        string name = className.Substring(0, className.Length - suffix.Length);

        // a class that is only called "Service" would end up without a tag
        if (name.Length == 0)
            name = className;

        return name.ToLowerInvariant();
    }
}

// escape codes that color the console output
// only codes that the .NET console logger can translate for old windows consoles are used (30-37, bold, 39, 41, 49),
// that is why the bright colors are written as "bold + color" instead of the 90-97 codes
public static class AnsiColors
{
    public const string Red = "\x1B[31m";
    public const string Green = "\x1B[32m";
    public const string Yellow = "\x1B[33m";
    public const string Blue = "\x1B[34m";
    public const string Magenta = "\x1B[35m";
    public const string Cyan = "\x1B[36m";
    public const string Gray = "\x1B[37m";

    public const string BrightRed = "\x1B[1m\x1B[31m";
    public const string BrightGreen = "\x1B[1m\x1B[32m";
    public const string BrightYellow = "\x1B[1m\x1B[33m";
    public const string BrightBlue = "\x1B[1m\x1B[34m";
    public const string BrightMagenta = "\x1B[1m\x1B[35m";
    public const string BrightCyan = "\x1B[1m\x1B[36m";
    public const string White = "\x1B[1m\x1B[37m";

    // modern terminals show this darker than the normal text, old windows consoles ignore the "dim" and show normal gray
    public const string Dimmed = "\x1B[2m\x1B[37m";

    public const string RedBackground = "\x1B[41m";

    // back to the default text color, default background, normal intensity
    public const string Reset = "\x1B[39m\x1B[49m\x1B[22m";
}

// can be set in the appsettings under Logging:Console:FormatterOptions (or with envs, e. g. Logging__Console__FormatterOptions__ColorBehavior)
// TimestampFormat, UseUtcTimestamp and IncludeScopes are inherited
public class LoggingFormatterOptions : ConsoleFormatterOptions
{
    public LoggingFormatterOptions()
    {
        TimestampFormat = "HH:mm:ss";
    }

    // Default: colors are used when the output is a terminal, but not when it is redirected (file, pipe, docker without tty)
    // Enabled / Disabled: always / never use colors
    public LoggerColorBehavior ColorBehavior { get; set; } = LoggerColorBehavior.Default;
}

public sealed class LoggingFormatter : ConsoleFormatter, IDisposable
{
    public const string FormatterName = "greg";

    // the level column always has 3 letters, so the tags of all lines start at the same position
    private const int LevelLength = 3;

    private readonly IDisposable? _optionsReloadToken;
    private LoggingFormatterOptions _options;
    private bool _useColors;

    // the tags of a class never change, so they are only built once per class (category -> "[alerts] [matches] ")
    private readonly ConcurrentDictionary<string, string> _tagsByCategory = new();

    public LoggingFormatter(IOptionsMonitor<LoggingFormatterOptions> options) : base(FormatterName)
    {
        _options = options.CurrentValue;
        _useColors = ShouldUseColors(_options);
        _optionsReloadToken = options.OnChange(ReloadOptions);
    }

    private void ReloadOptions(LoggingFormatterOptions options)
    {
        _options = options;
        _useColors = ShouldUseColors(options);

        // the cached tags contain the color codes, so they have to be rebuilt
        _tagsByCategory.Clear();
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        Exception? exception = logEntry.Exception;

        if (string.IsNullOrEmpty(message) && exception is null)
            return;

        string timestamp = GetTimestamp();
        string? messageColor = GetMessageColor(logEntry.LogLevel);

        // following lines (multiline messages, exceptions) are indented, so they start below the tags
        string indentation = new string(' ', timestamp.Length + LevelLength + 1);

        WriteColored(textWriter, timestamp, AnsiColors.Dimmed);
        WriteColored(textWriter, GetLevelName(logEntry.LogLevel), GetLevelColor(logEntry.LogLevel));
        textWriter.Write(' ');

        textWriter.Write(GetTags(logEntry.Category));

        if (_options.IncludeScopes && scopeProvider is not null)
            WriteScopes(textWriter, scopeProvider);

        if (!string.IsNullOrEmpty(message))
            WriteColored(textWriter, Indent(message, indentation), messageColor);

        textWriter.WriteLine();

        if (exception is not null)
        {
            textWriter.Write(indentation);
            WriteColored(textWriter, Indent(exception.ToString(), indentation), AnsiColors.Red);
            textWriter.WriteLine();
        }
    }

    private string GetTimestamp()
    {
        if (string.IsNullOrEmpty(_options.TimestampFormat))
            return "";

        DateTimeOffset now = DateTimeOffset.Now;

        if (_options.UseUtcTimestamp)
            now = DateTimeOffset.UtcNow;

        return now.ToString(_options.TimestampFormat) + " ";
    }

    private string GetTags(string category)
    {
        if (_tagsByCategory.TryGetValue(category, out var cachedTags))
            return cachedTags;

        string tags = "";

        foreach (string tag in LoggingTags.GetTags(category))
        {
            if (_useColors)
                tags += LoggingTags.GetColor(tag) + $"[{tag}]" + AnsiColors.Reset + " ";
            else
                tags += $"[{tag}] ";
        }

        _tagsByCategory[category] = tags;
        return tags;
    }

    // scopes are only written when IncludeScopes is turned on in the options (it is off by default)
    private void WriteScopes(TextWriter textWriter, IExternalScopeProvider scopeProvider)
    {
        scopeProvider.ForEachScope((scope, writer) =>
        {
            WriteColored(writer, $"{scope} > ", AnsiColors.Dimmed);
        }, textWriter);
    }

    private void WriteColored(TextWriter textWriter, string text, string? color)
    {
        if (!_useColors || color is null)
        {
            textWriter.Write(text);
            return;
        }

        textWriter.Write(color);
        textWriter.Write(text);
        textWriter.Write(AnsiColors.Reset);
    }

    private static string Indent(string text, string indentation)
    {
        if (!text.Contains('\n'))
            return text;

        return text
            .Replace("\r\n", "\n")
            .Replace("\n", Environment.NewLine + indentation);
    }

    private static bool ShouldUseColors(LoggingFormatterOptions options)
    {
        if (options.ColorBehavior == LoggerColorBehavior.Enabled)
            return true;

        if (options.ColorBehavior == LoggerColorBehavior.Disabled)
            return false;

        // https://no-color.org
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
            return false;

        // color codes would only be noise in a file or a pipe
        return !Console.IsOutputRedirected;
    }

    private static string GetLevelName(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
                return "TRC";
            case LogLevel.Debug:
                return "DBG";
            case LogLevel.Information:
                return "INF";
            case LogLevel.Warning:
                return "WRN";
            case LogLevel.Error:
                return "ERR";
            case LogLevel.Critical:
                return "CRT";
            default:
                return "???";
        }
    }

    private static string GetLevelColor(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Information:
                return AnsiColors.Green;
            case LogLevel.Warning:
                return AnsiColors.BrightYellow;
            case LogLevel.Error:
                return AnsiColors.BrightRed;
            case LogLevel.Critical:
                return AnsiColors.RedBackground + AnsiColors.White;
            default:
                return AnsiColors.Gray;
        }
    }

    // null means the default text color of the terminal
    private static string? GetMessageColor(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                return AnsiColors.Dimmed;
            case LogLevel.Warning:
                return AnsiColors.BrightYellow;
            case LogLevel.Error:
            case LogLevel.Critical:
                return AnsiColors.BrightRed;
            default:
                return null;
        }
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }
}
