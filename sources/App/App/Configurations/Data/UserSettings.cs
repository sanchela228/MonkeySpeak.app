using System.Xml.Serialization;
using App.System.Services;
using App.Configurations;

namespace App.Configurations.Data;



[Serializable]
public class UserSettingsData
{
    public string CaptureDeviceIdString { get; set; } = string.Empty;
    public string PlaybackDeviceIdString { get; set; } = string.Empty;
    public int MicrophoneVolumePercent { get; set; } = 100;
    public int PlaybackVolumePercent { get; set; } = 100;
    public string CaptureDeviceName { get; set; } = string.Empty;
    public string PlaybackDeviceName { get; set; } = string.Empty;
    
}
public class UserSettings : XmlConfigBase<UserSettings>
{
    protected override string RootDirectory => Path.Combine(Context.DataDirectory, "Configurations");
    private event Action OnDataChange;
    private bool _suppressAutoSave;
    
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
    
    [XmlIgnore]
    private string? _playbackDeviceName;
    [XmlIgnore]
    public string? PlaybackDeviceName { 
        get => _playbackDeviceName;
        set
        {
            _playbackDeviceName = value;
            OnDataChange?.Invoke();
        } 
    }
    
    [XmlIgnore]
    private string? _captureDeviceName;
    [XmlIgnore]
    public string? CaptureDeviceName { 
        get => _captureDeviceName;
        set
        {
            _captureDeviceName = value;
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

    private int _microphoneVolumePercent;
    [XmlElement("MicrophoneVolumePercent")]
    public int MicrophoneVolumePercent {
        get
        {
            return _microphoneVolumePercent;
        }  
        set 
        {
            _microphoneVolumePercent = value;
            OnDataChange?.Invoke();
        } 
    }
    
    private int _playbackVolumePercent;
    [XmlElement("PlaybackVolumePercent")]
    public int PlaybackVolumePercent {
        get
        {
            return _playbackVolumePercent;
        }  
        set 
        {
            _playbackVolumePercent = value;
            OnDataChange?.Invoke();
        } 
    }

    [XmlElement("CaptureDeviceName")]
    public string CaptureDeviceNameString => CaptureDeviceName ?? string.Empty;
    [XmlElement("PlaybackDeviceName")]
    public string PlaybackDeviceNameString => PlaybackDeviceName ?? string.Empty;
    
    
    public override string FileName => "UserSettings.xml";
    public UserSettings()
    {
        OnDataChange += () =>
        {
            if (!_suppressAutoSave)
                Save();
        };
    }

    public override void LoadOrDefault()
    {
        try
        {
            if (!Exists())
            {
                ApplyDefaults();
                Save();
                return;
            }

            var serializer = new XmlSerializer(typeof(UserSettingsData));
            using var reader = new StreamReader(FilePath);
            var loaded = (UserSettingsData)serializer.Deserialize(reader);

            _suppressAutoSave = true;
            try
            {
                _captureDeviceId = ParseIntPtrOrNull(loaded.CaptureDeviceIdString);
                _playbackDeviceId = ParseIntPtrOrNull(loaded.PlaybackDeviceIdString);
                _captureDeviceName = string.IsNullOrWhiteSpace(loaded.CaptureDeviceName) ? null : loaded.CaptureDeviceName;
                _playbackDeviceName = string.IsNullOrWhiteSpace(loaded.PlaybackDeviceName) ? null : loaded.PlaybackDeviceName;

                MicrophoneVolumePercent = Math.Clamp(loaded.MicrophoneVolumePercent, 0, 200);
                PlaybackVolumePercent = Math.Clamp(loaded.PlaybackVolumePercent, 0, 200);
            }
            finally
            {
                _suppressAutoSave = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Ошибка при загрузке настроек", ex);
            ApplyDefaults();
        }
    }

    public override void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var serializer = new XmlSerializer(typeof(UserSettingsData));
            var data = new UserSettingsData
            {
                CaptureDeviceIdString = CaptureDeviceIdString,
                PlaybackDeviceIdString = PlaybackDeviceIdString,
                MicrophoneVolumePercent = MicrophoneVolumePercent,
                PlaybackVolumePercent = PlaybackVolumePercent,
                CaptureDeviceName = CaptureDeviceName ?? string.Empty,
                PlaybackDeviceName = PlaybackDeviceName ?? string.Empty
            };
            
            using var writer = new StreamWriter(FilePath);
            serializer.Serialize(writer, data);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Ошибка при сохранении настроек", ex);
        }
    }

    protected override void ApplyDefaults()
    {
        _captureDeviceId = null;
        _playbackDeviceId = null;
        _captureDeviceName = null;
        _playbackDeviceName = null;
        MicrophoneVolumePercent = 100;
        PlaybackVolumePercent = 100;
    }

    protected override void CopyFrom(UserSettings other)
    {
        _captureDeviceId = other._captureDeviceId;
        _playbackDeviceId = other._playbackDeviceId;
        _captureDeviceName = other._captureDeviceName;
        _playbackDeviceName = other._playbackDeviceName;
        MicrophoneVolumePercent = other.MicrophoneVolumePercent;
        PlaybackVolumePercent = other.PlaybackVolumePercent;
    }

    private static IntPtr? ParseIntPtrOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return IntPtr.TryParse(value, out var ptr) ? ptr : null;
    }
}