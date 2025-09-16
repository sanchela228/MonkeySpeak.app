using App.Configurations.Interfaces;
using Engine.Managers;

namespace App.System.Services;

public static class Language
{
    public static App.Configurations.Interfaces.Language CurrentLanguage { get; set; } = App.Configurations.Interfaces.Language.English;
    private static Dictionary<string, string> _dictionary = new();
    private static bool _loaded;

    public static string Get(string code) => _loaded ? _dictionary.GetValueOrDefault(code, code) : code;
    
    public static async Task Load(App.Configurations.Interfaces.Language language)
    {
        if (_dictionary.Count > 0)
            _dictionary = new();    
            
        _loaded = false;
        
        try
        {
            var path = Path.Combine(Resources.Instance.RootFolderPath, "Repositories\\Languages\\" + language + ".lang");
            foreach (var line in await File.ReadAllLinesAsync( path ) )
            {
                var parts = line.Split('=', 2);
        
                if (parts.Length == 2)
                    _dictionary[parts[0]] = parts[1];
            }
            
            CurrentLanguage = language;
            _loaded = true;
        }
        catch (Exception e)
        {
            Logger.Write(Logger.Type.Error, "Failed to load language", e);
            throw;
        }
    }
}