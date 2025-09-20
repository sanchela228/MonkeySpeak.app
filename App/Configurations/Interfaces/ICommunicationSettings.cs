namespace App.Configurations.Interfaces;

public interface ICommunicationSettings
{
    IntPtr? CaptureDeviceId { get; set; }
    IntPtr? PlaybackDeviceId { get; set; }
}