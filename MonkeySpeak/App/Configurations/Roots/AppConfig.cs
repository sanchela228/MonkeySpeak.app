using App.Configurations;

namespace App.Configurations.Roots;

public class AppConfig : XmlConfigBase<AppConfig>
{
    protected override string RootDirectory => AppContext.BaseDirectory;
    public string VersionName { get; set; }
    public int Version { get; set; }

    public override string FileName => "AppConfig.xml";

    protected override void ApplyDefaults()
    {
        VersionName = "dev";
        Version = 1;
    }

    protected override void CopyFrom(AppConfig other)
    {
        VersionName = other.VersionName;
        Version = other.Version;
    }
}