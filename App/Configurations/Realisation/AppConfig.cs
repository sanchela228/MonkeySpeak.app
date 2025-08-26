using App.Configurations.Interfaces;

namespace App.Configurations.Realisation;

public class AppConfig : IAppConfig
{
    public string VersionName { get; set; }
    public int Version { get; set; }
}