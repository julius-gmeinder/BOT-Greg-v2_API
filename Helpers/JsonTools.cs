using System.Text.Json;

namespace BOT_Greg_v2_API.Helpers
{
    public static class JsonTools
    {
        public static void LogJson(JsonDocument json)
        {
            string formatted = JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            Console.WriteLine(formatted);
        }

    }
}
