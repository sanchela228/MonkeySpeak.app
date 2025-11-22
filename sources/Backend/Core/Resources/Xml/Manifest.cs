using System.Xml.Serialization;

namespace MonkeySpeak.Backend.Core.Resources.Xml;

[XmlRoot("Manifest")]
public class Manifest()
{
    public int Version;
    public string VersionName;
    public string PathDownload;
    public string FileSource;
    public string FileSetup;
}