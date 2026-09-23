using BOT_Greg_v2_API.Services;

namespace BOT_Greg_v2_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

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
}
