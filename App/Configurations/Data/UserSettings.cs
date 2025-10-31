using System.Xml.Serialization;
using App.System.Services;
using App.Configurations;

namespace App.Configurations.Data;



[Serializable]
public class UserSettingsData
{
    public string CaptureDeviceIdString { get; set; } = string.Empty;
    public string PlaybackDeviceIdString { get; set; } = string.Empty;
}
public class UserSettings : XmlConfigBase<UserSettings>
{
    protected override string RootDirectory => Context.DataDirectory;
    private event Action OnDataChange;
    
    [XmlIgnore]
    private IntPtr? _captureDeviceId;
    [XmlIgnore]
    public IntPtr? CaptureDeviceId
    {
        get => _captureDeviceId;
        set
        {
            _captureDeviceId = value;
            OnDataChange?.Invoke();
        }
    }

    [XmlIgnore]
    private IntPtr? _playbackDeviceId;
    [XmlIgnore]
    public IntPtr? PlaybackDeviceId { 
        get => _playbackDeviceId;
        set
        {
            _playbackDeviceId = value;
            OnDataChange?.Invoke();
        } 
    }
    
    [XmlElement("CaptureDeviceId")]
    public string CaptureDeviceIdString
    {
        get => CaptureDeviceId?.ToString() ?? string.Empty;
        set
        {
            if (string.IsNullOrEmpty(value))
                CaptureDeviceId = null;
            else if (IntPtr.TryParse(value, out var ptr))
                CaptureDeviceId = ptr;
        }
    }
    
    [XmlElement("PlaybackDeviceId")]
    public string PlaybackDeviceIdString
    {
        get => PlaybackDeviceId?.ToString() ?? string.Empty;
        set
        {
            if (string.IsNullOrEmpty(value))
                PlaybackDeviceId = null;
            else if (IntPtr.TryParse(value, out var ptr))
                PlaybackDeviceId = ptr;
        }
    }

    public override string FileName => Context.NameUserSettingsFile;

    public UserSettings()
    {
        OnDataChange += Save;
    }

    public override void Save()
    {
        try
        {
            var serializer = new XmlSerializer(typeof(UserSettingsData));
            var data = new UserSettingsData
            {
                CaptureDeviceIdString = CaptureDeviceIdString,
                PlaybackDeviceIdString = PlaybackDeviceIdString
            };
            using var writer = new StreamWriter(FilePath);
            serializer.Serialize(writer, data);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Ошибка при сохранении настроек", ex);
        }
    }

    public override void LoadOrDefault()
    {
        try
        {
            if (Exists())
            {
                var serializer = new XmlSerializer(typeof(UserSettingsData));
                using var reader = new StreamReader(FilePath);
                var cThis = (UserSettingsData)serializer.Deserialize(reader);

                if (IntPtr.TryParse(cThis.CaptureDeviceIdString, out var capturePtr))
                    _captureDeviceId = capturePtr;

                if (IntPtr.TryParse(cThis.PlaybackDeviceIdString, out var playbackPtr))
                    _playbackDeviceId = playbackPtr;

                return;
            }

            ApplyDefaults();
            Save();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Can't load UserSettings from xml. Applying defaults", ex);
            ApplyDefaults();
            try { Save(); } catch { /* ignore */ }
        }
    }

    public override void ApplyDefaults()
    {
        _captureDeviceId = null;
        _playbackDeviceId = null;
    }

    protected override void CopyFrom(UserSettings other)
    {
        _captureDeviceId = other._captureDeviceId;
        _playbackDeviceId = other._playbackDeviceId;
    }
}