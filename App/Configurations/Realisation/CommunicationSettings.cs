using System.Xml.Serialization;
using App.Configurations.Interfaces;
using App.System.Services;
using SoundFlow.Structs;

namespace App.Configurations.Realisation;



[Serializable]
public class CommunicationSettingsData
{
    public string CaptureDeviceIdString { get; set; } = string.Empty;
    public string PlaybackDeviceIdString { get; set; } = string.Empty;
}
public class CommunicationSettings : ICommunicationSettings
{
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

    public CommunicationSettings()
    {
        string path = Path.Combine(Context.Instance.DataDirectory, Context.NameCommunicationSettingsFile);
    
        if (File.Exists(path))
        {
            try
            {
                var serializer = new XmlSerializer(typeof(CommunicationSettingsData));
                using var reader = new StreamReader(path);
                var cThis = (CommunicationSettingsData) serializer.Deserialize(reader);
        
                if (IntPtr.TryParse(cThis.CaptureDeviceIdString, out var capturePtr))
                    _captureDeviceId = capturePtr;
                
                if (IntPtr.TryParse(cThis.PlaybackDeviceIdString, out var playbackPtr))
                    _playbackDeviceId = playbackPtr;
            }
            catch (Exception ex)
            {
               Logger.Write(Logger.Type.Error, "Can't load CommunicationSettingsData from xml.", ex);
            }
        }
    
        OnDataChange += SaveSettings;
    }

    private void SaveSettings()
    {
        try
        {
            var serializer = new XmlSerializer(typeof(CommunicationSettings));
            string path = Path.Combine(Context.Instance.DataDirectory, Context.NameCommunicationSettingsFile);
            using var writer = new StreamWriter(path);
            serializer.Serialize(writer, this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
        }
    }
}