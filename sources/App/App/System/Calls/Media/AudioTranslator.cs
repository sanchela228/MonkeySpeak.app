using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using App.System.Managers;
using App.System.Services;
using Concentus.Enums;
using Concentus.Structs;
using RNNoise.NET;
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
    
    // RNNoise (10 ms @ 48kHz, mono)
    private readonly List<float> rnnoiseBuffer = new(480);
    private readonly List<float> opusBuffer = new(480);

    private readonly AudioFormat audioFormat = new AudioFormat()
    {
        SampleRate = 48000,
        Channels = 1,
        Format = SampleFormat.F32
    };

    private OpusEncoder encoder;
    private OpusDecoder decoder;
    private Denoiser denoiser;

    private const int MaxOpusPacketBytes = 4096;
    private const int frameDurationMs = 10;
    private byte[] opusPacket = new byte[MaxOpusPacketBytes];
    
    private int frameSizePerChannel => audioFormat.SampleRate / (1000 / frameDurationMs);
    private int frameSamplesTotal => frameSizePerChannel * audioFormat.Channels;
    
    private void ConfigureAudioEngine()
    {
        try
        {
            _isRunning = true;
            
            using var engine = new MiniAudioEngine();

            captureDeviceWorker = engine.InitializeCaptureDevice(null, audioFormat);
            playbackDeviceWorker = engine.InitializePlaybackDevice(null, audioFormat);

            pcmStream = new ProducerConsumerStream();

            using var streamDataProvider = new RawDataProvider(
                pcmStream,
                audioFormat.Format,
                audioFormat.SampleRate,
                audioFormat.Channels
            );
            
            denoiser = new Denoiser();

            player = new SoundPlayer(engine, audioFormat, streamDataProvider);
            playbackDeviceWorker.MasterMixer.AddComponent(player);

            encoder = new OpusEncoder(audioFormat.SampleRate, audioFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 64000; // 64 kbps
            encoder.Complexity = 8; // (0-10)
            encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
            decoder = new OpusDecoder(audioFormat.SampleRate, audioFormat.Channels);
            
            captureDeviceWorker.OnAudioProcessed += OnAudioProcessedHandler;
            OnDataReceived += OnDataReceivedHandler;

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

    private void OnDataReceivedHandler(byte[] bytes)
    {
        try
        {
            var decodedFrame = new float[frameSamplesTotal];
            int decodedSamples = decoder.Decode(bytes, 0, bytes.Length, decodedFrame, 0, frameSizePerChannel, true);

            ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame);
            var outBuf = new byte[decodedBytes.Length];
            decodedBytes.CopyTo(outBuf);
            pcmStream.Write(outBuf, 0, outBuf.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Decoding error: {ex.Message}");
        }
    }
    
    private void OnAudioProcessedHandler(Span<float> samples, Capability capability)
    {
        if (!_captureEnabled)
            return;
                
        float[] inputBuffer = samples.ToArray();

        // normalize entry
        for (int i = 0; i < inputBuffer.Length; i++)
        {
            if (float.IsNaN(inputBuffer[i]) || float.IsInfinity(inputBuffer[i]))
                inputBuffer[i] = 0f;
            else
                inputBuffer[i] = Math.Clamp(inputBuffer[i], -1.0f, 1.0f);
        }

        rnnoiseBuffer.AddRange(inputBuffer);
                
        while (rnnoiseBuffer.Count >= 480)
        {
            float[] rnFrame = rnnoiseBuffer.GetRange(0, 480).ToArray();
            rnnoiseBuffer.RemoveRange(0, 480);

            if (_denoiseEnabled)
            {
                Span<float> span = rnFrame.AsSpan();
                float vadScore = denoiser.Denoise(span, finish: false) / 480.0f;
            }

            opusBuffer.AddRange(rnFrame);
        }
    
        while (opusBuffer.Count >= 480)
        {
            float[] opusFrame = opusBuffer.GetRange(0, 480).ToArray();
            opusBuffer.RemoveRange(0, 480);

            int encodedBytes = encoder.Encode(opusFrame, 0, 480, opusPacket, 0, MaxOpusPacketBytes);

            if (encodedBytes > 0)
                _ = SendAudioBytesAsync(opusPacket, encodedBytes);
        }
    }
   
    private volatile bool _denoiseEnabled = true;
    private float _vadGain = 1.0f;
    
    public void ToggleDenoise()
    {
        _denoiseEnabled = !_denoiseEnabled;
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Denoise {(_denoiseEnabled ? "enabled" : "disabled")}");
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