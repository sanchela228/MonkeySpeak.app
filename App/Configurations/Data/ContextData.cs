using App.System.Services;
using App.Configurations;

namespace App.Configurations.Data;

public class ContextData : XmlConfigBase<ContextData>
{
    protected override string RootDirectory => Context.DataDirectory;
    public Guid ApplicationId { get; set; }
    public string MachineId { get; set; }
    public Language LanguageSelected { get; set; }

    public override string FileName => Context.NameDataFile;

    public override void ApplyDefaults()
    {
        ApplicationId = Guid.NewGuid();
        MachineId = ComputerIdentity.GetMacAddress();
        LanguageSelected = System.Services.Language.CurrentLanguage;
    }

    protected override void CopyFrom(ContextData other)
    {
        ApplicationId = other.ApplicationId;
        MachineId = other.MachineId;
        LanguageSelected = other.LanguageSelected;
    }
}

public enum Language
{
    Russian,
    English
}