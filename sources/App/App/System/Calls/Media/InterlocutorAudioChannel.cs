using App.System.Services;
using Concentus.Structs;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using SoundFlow.Providers;

namespace App.System.Calls.Media;

public class InterlocutorAudioChannel : IDisposable
{
    public OpusDecoder Decoder { get; set; }
    public ProducerConsumerStream Stream { get; set; }
    public RawDataProvider DataProvider { get; set; }
    public SoundPlayer Player { get; set; }
    public AudioPlaybackDevice DedicatedPlaybackDevice { get; set; }
    public int PacketCount { get; set; }
    public DateTime LastResetTime { get; set; } = DateTime.UtcNow;
    public float AudioLevel { get; set; } // 0-1
    public DateTime LastAudioReceived { get; set; } = DateTime.UtcNow;
    public int TotalPacketsReceived { get; set; }

    public void Dispose()
    {
        try
        {
            Player?.Stop();
            Player?.Dispose();
            DedicatedPlaybackDevice?.Stop();
            DedicatedPlaybackDevice?.Dispose();
            DataProvider?.Dispose();
            Stream?.Dispose();
            Decoder?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Warning, $"[InterlocutorAudioChannel] Dispose error: {ex.Message}");
        }
    }
    
    public void CalculateAudioLevel(float[] decodedFrame, byte[] bytes, int frameSizePerChannel)
    {
        int decodedSamples = Decoder.Decode(
            bytes, 0, bytes.Length, 
            decodedFrame, 0, frameSizePerChannel, false
        );
        
        float rms = 0f;
        for (int i = 0; i < decodedSamples; i++)
            rms += decodedFrame[i] * decodedFrame[i];
        
        rms = (float)Math.Sqrt(rms / decodedSamples);
        AudioLevel = Math.Clamp(rms * 3f, 0f, 1f);
        LastAudioReceived = DateTime.UtcNow;
            
        TotalPacketsReceived++;
        PacketCount++;
    }
}