using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using App.System.Managers;
using App.System.Services;
using Concentus.Enums;
using Concentus.Structs;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace App.System.Calls.Media;

public class AudioTranslator : IDisposable
{
    private readonly UdpUnifiedManager _udpManager;
    private CancellationTokenSource _cancellationTokenSource;

    private AudioCaptureDevice captureDeviceWorker;
    private AudioPlaybackDevice playbackDeviceWorker;

    private SoundPlayer player;
    private ProducerConsumerStream pcmStream;
    
    public AudioTranslator(UdpUnifiedManager udpManager, CancellationTokenSource cts)
    {
        _udpManager = udpManager ?? throw new ArgumentNullException(nameof(udpManager));
        _cancellationTokenSource = cts ?? new CancellationTokenSource();

        _udpManager.OnAudioData += data => OnDataReceived?.Invoke(data);

        _audioThread = new Thread(ConfigureAudioEngine);
        _audioThread.IsBackground = true;
        _audioThread.Start();
        Logger.Write(Logger.Type.Info, "[AudioTranslator] initialized with UdpUnifiedManager");
    }
    
    public event Action<byte[]> OnDataReceived;
    
    private volatile bool _isRunning = false;
    private volatile bool _captureEnabled = true;
    public void ToggleCaptureAudio(bool enable)
    {
        _captureEnabled = enable;
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Audio capture {(enable ? "enabled" : "disabled")}");
    }
    
    private Thread _audioThread;
    
    private async Task SendAudioBytesAsync(byte[] data, int length)
    {
        await _udpManager.SendAudioAsync(new ReadOnlyMemory<byte>(data, 0, length));
    }
    
    private void ConfigureAudioEngine()
    {
        try
        {
            _isRunning = true;
            
            using var engine = new MiniAudioEngine();
            var format = AudioFormat.Broadcast;
            var sampleFormat = SampleFormat.F32;

            captureDeviceWorker = engine.InitializeCaptureDevice(null, format);
            playbackDeviceWorker = engine.InitializePlaybackDevice(null, format);

            pcmStream = new ProducerConsumerStream();

            using var streamDataProvider = new RawDataProvider(
                pcmStream,
                sampleFormat,
                format.SampleRate,
                format.Channels
            );

            player = new SoundPlayer(engine, format, streamDataProvider);
            playbackDeviceWorker.MasterMixer.AddComponent(player);

            OpusEncoder encoder =
                new OpusEncoder(format.SampleRate, format.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
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

            captureDeviceWorker.OnAudioProcessed += (samples, capability) =>
            {
                if (_captureEnabled)
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

                        int encodedBytes = encoder.Encode(framePcm, 0, frameSizePerChannel, opusPacket, 0,
                            MaxOpusPacketBytes);
                        if (encodedBytes <= 0)
                        {
                            continue;
                        }

                        _ = SendAudioBytesAsync(opusPacket, encodedBytes);
                    }
                }
            };

            this.OnDataReceived += (receivedData) =>
            {
                try
                {
                    var decodedFrame = new float[frameSamplesTotal];
                    int decodedSamples = decoder.Decode(receivedData, 0, receivedData.Length, decodedFrame, 0,
                        frameSizePerChannel, false);

                    ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame);
                    var outBuf = new byte[decodedBytes.Length];
                    decodedBytes.CopyTo(outBuf);
                    pcmStream.Write(outBuf, 0, outBuf.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decoding error: {ex.Message}");
                }
            };

            player.Play();
            playbackDeviceWorker.Start();
            captureDeviceWorker.Start();

            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(250);
            }
            
            StopAudioDevices();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error in audio engine: {ex.Message}", ex);
            throw;
        }
    }
    
    private void StopAudioDevices()
    {
        try
        {
            captureDeviceWorker?.Stop();
            playbackDeviceWorker?.Stop();
            player?.Stop();
            pcmStream?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error stopping audio devices: {ex.Message}", ex);
        }
    }
    
    public void Dispose()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
        StopAudioDevices();
        
        _audioThread?.Join(1000);
        
        _cancellationTokenSource?.Dispose();
        
        Logger.Write(Logger.Type.Info, "[AudioTranslator] Disposed");
    }
}