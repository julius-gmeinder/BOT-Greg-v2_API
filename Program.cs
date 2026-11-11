using BOT_Greg_v2_API.Data;
using BOT_Greg_v2_API.Repos;
using BOT_Greg_v2_API.Services;
using BOT_Greg_v2_API.Services.Alerts;
using Microsoft.EntityFrameworkCore;

namespace BOT_Greg_v2_API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMemoryCache();

        string connString = builder.Configuration.GetConnectionString("BotGregDb")!;
        builder.Services.AddDbContextPool<BotGregDbContext>(options =>
        {
            options
                .UseLazyLoadingProxies()
                .UseMySql(connString, ServerVersion.AutoDetect(connString));
        });

        builder.Services.AddHttpClient<LiquipediaService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
            });

        builder.Services.AddHttpClient<VrsService>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
            });

        // Repos
        builder.Services.AddScoped<GuildsRepo>();
        builder.Services.AddScoped<RolesRepo>();
        builder.Services.AddScoped<AlertsRepo>();
        builder.Services.AddScoped<FiltersRepo>();
        builder.Services.AddScoped<RoleLinksRepo>();
        builder.Services.AddScoped<HistoryMatchAlertsRepo>();
        builder.Services.AddScoped<HistoryResultAlertsRepo>();
        builder.Services.AddScoped<AutocompleteRepo>();

        // Alerts
        builder.Services.AddSingleton<AlertStreamService>();
        builder.Services.AddScoped<AlertsHelper>();
        builder.Services.AddScoped<MatchAlertsService>();
        builder.Services.AddScoped<MatchDelaysService>();
        builder.Services.AddScoped<ResultAlertsService>();

        // Backgroundworker
        builder.Services.AddHostedService<BackgroundWorker>();
        builder.Services.AddHostedService<AlertStreamHeartbeat>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Docs");
                options.RoutePrefix = string.Empty;
            });
        }

        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
