namespace App.Configurations;

public interface IAppConfig
{
    string VersionName { get; set; }
    int Version { get; set; }
}