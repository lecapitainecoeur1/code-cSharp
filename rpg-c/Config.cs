using System.IO;
using System.Text.Json;

public class Config
{
    public string? language { get; set; }

    public static Config Load()
    {
        string path = "config.json";

        if (!File.Exists(path))
        {
            // config par défaut
            var defaultConfig = new Config { language = "fr" };
            File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
            return defaultConfig;
        }

        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Config>(json);
        return config;
    }
    public void Save()
{
    string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText("config.json", json);
}
}