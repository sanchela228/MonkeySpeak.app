using System.Runtime.InteropServices;
using App.System.Calls.Media;
using Engine;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace App.Scenes;

public class Room : Scene
{
    public Room()
    {
        using var engine = new MiniAudioEngine();
        var format = AudioFormat.DvdHq;
        
        var captureDeviceWorker = engine.InitializeCaptureDevice(null, format);
        var playbackDeviceWorker = engine.InitializePlaybackDevice(null, format);
       

        var pcmStream = new ProducerConsumerStream();
        
        var sampleRate = 48000;
        var channels = 2;
        var sampleFormat = SampleFormat.F32;

        using var streamDataProvider = new RawDataProvider(
            pcmStream,
            sampleFormat,
            sampleRate,
            channels
        );
        
        using var player = new SoundPlayer(engine, format, streamDataProvider);
        playbackDeviceWorker.MasterMixer.AddComponent(player);
        
        captureDeviceWorker.OnAudioProcessed += (samples, capability) =>
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(samples);

            var buf = new byte[bytes.Length];
            bytes.CopyTo(buf);
            pcmStream.Write(buf, 0, buf.Length);
        }; 


        player.Play();
        playbackDeviceWorker.Start();
        captureDeviceWorker.Start();

        Console.WriteLine("Live microphone passthrough is active. Press any key to stop.");
        Console.ReadKey();
        
        
        player.Stop();
    }
    
    protected override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
    }

    protected override void Draw()
    {
        // throw new NotImplementedException();
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
}