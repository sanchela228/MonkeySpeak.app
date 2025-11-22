using System.Xml.Serialization;
using App;

namespace App.Configurations;

public abstract class XmlConfigBase<T> where T : class
{
    public abstract string FileName { get; }
    protected abstract string RootDirectory { get; }
    protected abstract void CopyFrom(T other);
    protected abstract void ApplyDefaults();

    public string FilePath => Path.Combine(RootDirectory, FileName);

    public bool Exists() => File.Exists(FilePath);

    public virtual void Save()
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StreamWriter(FilePath);
        serializer.Serialize(writer, (T)(object)this);
    }

    public virtual void LoadOrDefault()
    {
        try
        {
            if (Exists())
            {
                var serializer = new XmlSerializer(typeof(T));
                using var reader = new StreamReader(FilePath);
                var loaded = (T)serializer.Deserialize(reader);
                CopyFrom(loaded);
                return;
            }

            ApplyDefaults();
            Save();
        }
        catch (Exception)
        {
            ApplyDefaults();
            try { Save(); } catch { /* ignore secondary errors */ }
        }
    }
}