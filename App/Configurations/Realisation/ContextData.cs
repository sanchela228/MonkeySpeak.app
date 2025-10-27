using System.Xml.Serialization;
using App.Configurations.Interfaces;

namespace App.Configurations.Realisation;

public class ContextData : IContextData
{
    public Guid ApplicationId { get; set; }
    public string MachineId { get; set; }
    public Language LanguageSelected { get; set; }

    public void SaveContext()
    {
        var serializer = new XmlSerializer(typeof(ContextData));
        using var writer = new StreamWriter(Context.DataDirectory + "/" + Context.NameDataFile);
        serializer.Serialize(writer, this);
    }
}