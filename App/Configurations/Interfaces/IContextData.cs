using System.Xml.Serialization;

namespace App.Configurations.Interfaces;

public interface IContextData
{
    Guid ApplicationId { get; set; }
    string MachineId { get; set; }
    Language LanguageSelected { get; set; }
}

public enum Language
{
    Russian,
    English
}