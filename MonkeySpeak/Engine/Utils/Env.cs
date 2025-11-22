namespace Engine.Utils;

public static class Env
{
    private static Dictionary<string, string> _envVars = new();
    
    public static void Load(string envPath = "config.env")
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var parts = line.Split('=', 2);
            
            if (parts.Length == 2)
                _envVars[parts[0]] = parts[1];
        }
    }

    public static string Get(string key) => _envVars[key];
}