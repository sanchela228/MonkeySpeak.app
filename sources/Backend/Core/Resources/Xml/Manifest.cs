using System.Xml.Serialization;

namespace Core.Resources.Xml;

[XmlRoot("Manifest")]
public class Manifest()
{
    public int Version;
    public string VersionName;
    public string PathDownload;
    public string FileSource;
    public string FileSetup;
}