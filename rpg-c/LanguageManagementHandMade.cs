using System.Text.Json;

public class LanguageManagementHandMade
{
    private Dictionary<string, string> translations = new();
    
    public void Load(string langue)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory() + $"/lang/{langue}.json");

        if (!File.Exists(path))
        {
            Console.WriteLine("langue introuvable");
            Console.WriteLine(path);
        }
        else
        {
            string json = File.ReadAllText(path);
            translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
    }

    public string Get(string motCle)
    {
        if (translations == null || !translations.ContainsKey(motCle))
            return $"[{motCle}]";
        return translations[motCle];
    }
    public string Get(string motCle, Dictionary<string, string> variables)
    {
        string text = Get(motCle);

        if (text.Contains("{") && text.Contains("}"))
        {
            foreach (var pair in variables)
            {
                text = text.Replace("{" + pair.Key + "}", pair.Value);
            }
        }

        return text;
    }
}