using System.Runtime.InteropServices;
using App.System.Calls.Media;
using Concentus.Enums;
using Concentus.Structs;
using Engine;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using System.Collections.Generic;

namespace App.Scenes;

public class Room : Scene
{
    public Room()
    {
        using var engine = new MiniAudioEngine();
        var format = AudioFormat.Broadcast;
        var sampleFormat = SampleFormat.F32;
        var commSettings = Context.Instance.CommunicationSettings;
        
        DeviceInfo PlaybackDeviceInfo = engine.PlaybackDevices.FirstOrDefault();
        // if (commSettings.PlaybackDeviceId is not null)
        //     PlaybackDeviceInfo = engine.PlaybackDevices.FirstOrDefault(x => x.Id == commSettings.PlaybackDeviceId);
        //
        DeviceInfo CaptureDeviceInfo = engine.CaptureDevices.FirstOrDefault();
        // if (commSettings.CaptureDeviceId is not null)
        //     CaptureDeviceInfo = engine.CaptureDevices.FirstOrDefault(x => x.Id == commSettings.CaptureDeviceId);
        //
        var captureDeviceWorker = engine.InitializeCaptureDevice(null, format);
        var playbackDeviceWorker = engine.InitializePlaybackDevice(null, format);
        
        var pcmStream = new ProducerConsumerStream();
        
        using var streamDataProvider = new RawDataProvider(
            pcmStream,
            sampleFormat,
            format.SampleRate,
            format.Channels
        );
        
        using var player = new SoundPlayer(engine, format, streamDataProvider);
        playbackDeviceWorker.MasterMixer.AddComponent(player);
        
        
        OpusEncoder encoder = new OpusEncoder(format.SampleRate, format.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = 32000; // 32 kbps
        encoder.Complexity = 5; // (0-10)
        encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
        var decoder = new OpusDecoder(format.SampleRate, format.Channels);
        
        int frameDurationMs = 20;
        int frameSizePerChannel = format.SampleRate / (1000 / frameDurationMs); // 480 / 48 кГц
        int channels = format.Channels;
        int frameSamplesTotal = frameSizePerChannel * channels;
        
        List<float> captureBuffer = new List<float>(frameSamplesTotal * 4);
        const int MaxOpusPacketBytes = 4096;
        
        var opusPacket = new byte[MaxOpusPacketBytes];
        
        Context.Instance.CallFacade.TestOnReciveAudioBytes += (receivedData) =>
        {
            try
            {
                var decodedFrame = new float[frameSamplesTotal];
                int decodedSamples = decoder.Decode(receivedData, 0, receivedData.Length, decodedFrame, 0, frameSizePerChannel, false);

                ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame);
                var outBuf = new byte[decodedBytes.Length];
                decodedBytes.CopyTo(outBuf);
                pcmStream.Write(outBuf, 0, outBuf.Length);
                
                // ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame.AsSpan(0, decodedSamples * channels));
                // var outBuf = new byte[decodedBytes.Length];
                // decodedBytes.CopyTo(outBuf);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decoding error: {ex.Message}");
            }
        };
        
        captureDeviceWorker.OnAudioProcessed += (samples, capability) =>
        {
            for (int i = 0; i < samples.Length; i++)
            {
                captureBuffer.Add(samples[i]);
            }

            while (captureBuffer.Count >= frameSamplesTotal)
            {
                var framePcm = new float[frameSamplesTotal];
                captureBuffer.CopyTo(0, framePcm, 0, frameSamplesTotal);
                captureBuffer.RemoveRange(0, frameSamplesTotal);
        
                int encodedBytes = encoder.Encode(framePcm, 0, frameSizePerChannel, opusPacket, 0, MaxOpusPacketBytes);
                if (encodedBytes <= 0)
                {
                    continue; 
                }
                
                // OLDDDDDDDDDDDDDD
                
                // var decodedFrame = new float[frameSamplesTotal];
                // int decodedSamplesPerChannel = decoder.Decode(opusPacket, 0, encodedBytes, decodedFrame, 0, frameSizePerChannel, false);
                
                // ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame);
                // var outBuf = new byte[decodedBytes.Length];
                // decodedBytes.CopyTo(outBuf);
                // pcmStream.Write(outBuf, 0, outBuf.Length);
                
                // ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame.AsSpan(0, decodedSamplesPerChannel * channels));
                // var outBuf = new byte[decodedBytes.Length];
                // decodedBytes.CopyTo(outBuf);
                
                Console.WriteLine($"encodedBytes: {encodedBytes}");
        
                Context.Instance.CallFacade.TestSendAudioBytes(opusPacket, encodedBytes);
            }
        };
        
        player.Play();
        playbackDeviceWorker.Start();
        captureDeviceWorker.Start();

        Console.ReadKey();
        
        Console.WriteLine($"End construct ROOM");
        
        player.Stop();
        playbackDeviceWorker.Stop();
        captureDeviceWorker.Stop();
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