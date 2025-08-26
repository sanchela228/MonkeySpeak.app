namespace App.Configurations.Interfaces;

public interface IAppConfig
{
    string VersionName { get; set; }
    int Version { get; set; }
}