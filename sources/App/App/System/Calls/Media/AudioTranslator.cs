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
using System.Collections.Concurrent;

namespace App.System.Calls.Media;

public class AudioTranslator : IDisposable
{
    private readonly UdpUnifiedManager _udpManager;
    private CancellationTokenSource _cancellationTokenSource;

    private AudioCaptureDevice captureDeviceWorker;
    private AudioPlaybackDevice playbackDeviceWorker;
    private MiniAudioEngine audioEngine;

    private readonly ConcurrentDictionary<string, InterlocutorAudioChannel> _channels = new();

    private IntPtr? _currentPlaybackDeviceId;
    
    private DeviceInfo? preferredCaptureDevice;
    private DeviceInfo? preferredPlaybackDevice;
    
    public AudioTranslator(UdpUnifiedManager udpManager, CancellationTokenSource cts)
    {
        _udpManager = udpManager ?? throw new ArgumentNullException(nameof(udpManager));
        _cancellationTokenSource = cts ?? new CancellationTokenSource();

        Logger.Write(Logger.Type.Info, "[AudioTranslator] Subscribing to OnAudioDataByInterlocutor event");
        
        _udpManager.OnAudioDataByInterlocutor += HandleInterlocutorAudioData;

        _audioThread = new Thread(ConfigureAudioEngine);
        _audioThread.IsBackground = true;
        _audioThread.Start();
        
        Logger.Write(Logger.Type.Info, "[AudioTranslator] Initialized with separate channels per interlocutor. Audio thread started.");
    }
    
    private volatile bool _isRunning = false;
    private volatile bool _captureEnabled = true;
    private volatile bool _playbackEnabled = true;
    private volatile bool _audioEngineReady = false;
    private readonly object _readyLock = new object();

    private volatile int _microphoneVolumePercent = 100;
    private volatile int _playbackVolumePercent = 100;

    private float _selfAudioLevel = 0f;
    private long _selfAudioLevelUpdatedAtTicks = 0;
    
    private readonly ConcurrentQueue<Action> _mixerActions = new();
    
    public void TogglePlaybackAudio(bool enable)
    {
        _playbackEnabled = enable;

        if (enable)
        {
            _mixerActions.Enqueue(() =>
            {
                try
                {
                    foreach (var kvp in _channels)
                    {
                        try
                        {
                            kvp.Value?.Stream?.Clear();
                        }
                        catch { }
                    }
                }
                catch { }
            });
        }
    }
    
    public void ToggleCaptureAudio(bool enable)
    {
        _captureEnabled = enable;
        
        if (!enable)
        {
            lock (rnnoiseBuffer)
            {
                rnnoiseBuffer.Clear();
            }
            
            lock (opusBuffer)
            {
                opusBuffer.Clear();
            }
            
            Logger.Write(Logger.Type.Info, "[AudioTranslator] Audio buffers cleared on mute");
        }
        
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Audio capture {(enable ? "enabled" : "disabled")}");
    }

    public void SetMicrophoneVolumePercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 200);
        _microphoneVolumePercent = percent;
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Microphone volume set to {_microphoneVolumePercent}%");
    }

    public void SetPlaybackVolumePercent(int percent)
    {
        percent = Math.Clamp(percent, 0, 200);
        _playbackVolumePercent = percent;
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Playback volume set to {_playbackVolumePercent}%");
    }
    
    public DeviceInfo? SearchNecessaryCaptureDeviceInfo(IntPtr? deviceId, string? deviceName = null)
    {
        audioEngine.UpdateDevicesInfo();
        foreach (var capDevice in audioEngine.CaptureDevices)
        {
            if (capDevice.Id == deviceId)
                return capDevice;
        }

        if (deviceName is not null)
        {
            foreach (var capDevice in audioEngine.CaptureDevices)
            {
                if (capDevice.Name == deviceName)
                    return capDevice;
            }
        }
        
        return null;
    }
    
    public DeviceInfo? SearchNecessaryPlaybackDeviceInfo(IntPtr? deviceId, string? deviceName = null)
    {
        audioEngine.UpdateDevicesInfo();
        foreach (var capDevice in audioEngine.PlaybackDevices)
        {
            if (capDevice.Id == deviceId)
                return capDevice;
        }

        if (deviceName is not null)
        {
            foreach (var capDevice in audioEngine.PlaybackDevices)
            {
                if (capDevice.Name == deviceName)
                    return capDevice;
            }
        }
        
        return null;
    }

    public DeviceInfo[] GetCaptureDevices()
    {
        try
        {
            return audioEngine?.CaptureDevices ?? Array.Empty<DeviceInfo>();
        }
        catch
        {
            return Array.Empty<DeviceInfo>();
        }
    }

    public DeviceInfo[] GetPlaybackDevices()
    {
        try
        {
            return audioEngine?.PlaybackDevices ?? Array.Empty<DeviceInfo>();
        }
        catch
        {
            return Array.Empty<DeviceInfo>();
        }
    }

    public void SwitchCaptureDevice(IntPtr? deviceId)
    {
        _mixerActions.Enqueue(() =>
        {
            try
            {
                if (audioEngine == null)
                {
                    Logger.Write(Logger.Type.Warning, "[AudioTranslator] SwitchCaptureDevice: audioEngine is null");
                    return;
                }

                audioEngine.UpdateDevicesInfo();

                DeviceInfo? selected = null;
                if (deviceId != null)
                {
                    foreach (var d in audioEngine.CaptureDevices)
                    {
                        if (d.Id == deviceId.Value)
                        {
                            selected = d;
                            break;
                        }
                    }
                }

                try
                {
                    if (captureDeviceWorker != null)
                    {
                        captureDeviceWorker.OnAudioProcessed -= OnAudioProcessedHandler;
                        captureDeviceWorker.Stop();
                        captureDeviceWorker.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write(Logger.Type.Warning, $"[AudioTranslator] SwitchCaptureDevice: error stopping old device: {ex.Message}");
                }

                captureDeviceWorker = audioEngine.InitializeCaptureDevice(selected, audioFormat);
                captureDeviceWorker.OnAudioProcessed += OnAudioProcessedHandler;
                captureDeviceWorker.Start();

                lock (rnnoiseBuffer)
                {
                    rnnoiseBuffer.Clear();
                }

                lock (opusBuffer)
                {
                    opusBuffer.Clear();
                }

                Logger.Write(Logger.Type.Info,
                    $"[AudioTranslator] Switched capture device to {(selected?.Name ?? "Default")}");
            }
            catch (Exception ex)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] SwitchCaptureDevice error: {ex.Message}", ex);
            }
        });
    }
    
    public void SwitchPlaybackDevice(IntPtr? deviceId)
    {
        _mixerActions.Enqueue(() =>
        {
            try
            {
                if (audioEngine == null)
                {
                    Logger.Write(Logger.Type.Warning, "[AudioTranslator] SwitchPlaybackDevice: audioEngine is null");
                    return;
                }

                audioEngine.UpdateDevicesInfo();

                _currentPlaybackDeviceId = deviceId;

                DeviceInfo? selected = null;
                if (deviceId != null)
                {
                    foreach (var d in audioEngine.PlaybackDevices)
                    {
                        if (d.Id == deviceId.Value)
                        {
                            selected = d;
                            break;
                        }
                    }
                }

                foreach (var kvp in _channels)
                {
                    var channel = kvp.Value;
                    if (channel == null)
                        continue;

                    try
                    {
                        if (channel.DedicatedPlaybackDevice != null && channel.Player != null)
                        {
                            try
                            {
                                channel.DedicatedPlaybackDevice.MasterMixer.RemoveComponent(channel.Player);
                            }
                            catch { }

                            try
                            {
                                channel.DedicatedPlaybackDevice.Stop();
                                channel.DedicatedPlaybackDevice.Dispose();
                            }
                            catch { }
                        }

                        var newDevice = audioEngine.InitializePlaybackDevice(selected, audioFormat);
                        if (newDevice == null)
                        {
                            Logger.Write(Logger.Type.Warning, $"[AudioTranslator] SwitchPlaybackDevice: failed to init playback device for channel {kvp.Key}");
                            continue;
                        }

                        channel.DedicatedPlaybackDevice = newDevice;
                        newDevice.Start();
                        newDevice.MasterMixer.AddComponent(channel.Player);
                        channel.Player.Play();

                        try
                        {
                            channel.Stream?.Clear();
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(Logger.Type.Warning, $"[AudioTranslator] SwitchPlaybackDevice: error rebinding channel {kvp.Key}: {ex.Message}");
                    }
                }

                Logger.Write(Logger.Type.Info,
                    $"[AudioTranslator] Switched playback device to {(selected?.Name ?? "Default")}");
            }
            catch (Exception ex)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] SwitchPlaybackDevice error: {ex.Message}", ex);
            }
        });
    }
    
    public Dictionary<string, float> GetAudioLevels()
    {
        var levels = new Dictionary<string, float>();
        foreach (var kvp in _channels)
        {
            var timeSinceLastAudio = DateTime.UtcNow - kvp.Value.LastAudioReceived;
            levels[kvp.Key] = timeSinceLastAudio.TotalMilliseconds < 500 ? kvp.Value.AudioLevel : 0f;
        }
        return levels;
    }

    public float GetSelfAudioLevel()
    {
        var updatedAt = new DateTime(Interlocked.Read(ref _selfAudioLevelUpdatedAtTicks), DateTimeKind.Utc);
        var timeSince = DateTime.UtcNow - updatedAt;
        if (timeSince.TotalMilliseconds > 500)
            return 0f;

        return _selfAudioLevel;
    }
    
    public void RemoveInterlocutorChannel(string interlocutorId)
    {
        Logger.Write(Logger.Type.Info, $"[AudioTranslator] Attempting to remove channel for {interlocutorId}. Current channels: {_channels.Count}");
        
        if (_channels.TryRemove(interlocutorId, out var channel))
        {
            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Removing channel for {interlocutorId}");
            
            channel.Dispose();
            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Channel disposed for {interlocutorId}. Remaining channels: {_channels.Count}");
        }
        else
        {
            Logger.Write(Logger.Type.Warning, $"[AudioTranslator] Channel not found for {interlocutorId}");
        }
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
            
            audioEngine = new MiniAudioEngine();

            preferredCaptureDevice = SearchNecessaryCaptureDeviceInfo(Context.UserSettings.CaptureDeviceId, Context.UserSettings.CaptureDeviceName);
            preferredPlaybackDevice = SearchNecessaryPlaybackDeviceInfo(Context.UserSettings.PlaybackDeviceId, Context.UserSettings.PlaybackDeviceName);

            captureDeviceWorker = audioEngine.InitializeCaptureDevice(preferredCaptureDevice, audioFormat);
            playbackDeviceWorker = audioEngine.InitializePlaybackDevice(preferredPlaybackDevice, audioFormat);
            
            denoiser = new Denoiser();

            encoder = new OpusEncoder(audioFormat.SampleRate, audioFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 64000; // 64 kbps
            encoder.Complexity = 8; // (0-10)
            encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
            
            captureDeviceWorker.OnAudioProcessed += OnAudioProcessedHandler;

            playbackDeviceWorker.Start();
            captureDeviceWorker.Start();
            
            lock (_readyLock)
            {
                _audioEngineReady = true;
                Monitor.PulseAll(_readyLock);
            }
            
            Logger.Write(Logger.Type.Info, "[AudioTranslator] Audio engine ready for channel creation");

            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                bool actionProcessed = false;
                while (_mixerActions.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                        actionProcessed = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error executing mixer action: {ex.Message}");
                    }
                }
                
                if (!actionProcessed) Thread.Sleep(10);
            }
            
            StopAudioDevices();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error in audio engine: {ex.Message}", ex);
            throw;
        }
    }

    private int _audioPacketCounter = 0;
    private readonly ConcurrentDictionary<string, int> _packetCountPerInterlocutor = new();
    
    private void HandleInterlocutorAudioData(string interlocutorId, byte[] bytes)
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        try
        {
            _audioPacketCounter++;
            var perInterlocutorCount = _packetCountPerInterlocutor.AddOrUpdate(interlocutorId, 1, (k, v) => v + 1);
            
            // DEMO LOGGING
            if (perInterlocutorCount <= 5)
            {
                Logger.Write(Logger.Type.Info, 
                    $"[AudioTranslator][Thread-{threadId}] Received packet #{perInterlocutorCount} from {interlocutorId.Substring(0, Math.Min(8, interlocutorId.Length))}. Bytes: {bytes.Length}, Total channels: {_channels.Count}");
            }
            else if (_audioPacketCounter % 500 == 0)
            {
                Logger.Write(Logger.Type.Info, $"[AudioTranslator] Received audio packet #{_audioPacketCounter}. Active channels: {_channels.Count}");
            }
            
            InterlocutorAudioChannel channel;
            if (!_channels.TryGetValue(interlocutorId, out channel))
            {
                Logger.Write(Logger.Type.Info, $"[AudioTranslator] Channel not found for {interlocutorId}, creating new one...");
                
                channel = CreateChannelForInterlocutor(interlocutorId);
                channel = _channels.GetOrAdd(interlocutorId, channel);
                
                Logger.Write(Logger.Type.Info, $"[AudioTranslator] Channel added to dictionary for {interlocutorId}. Current channels: {_channels.Count}");
            }

            var decodedFrame = new float[frameSamplesTotal];
            int decodedSamples = channel.CalculateAudioLevel(decodedFrame, bytes, frameSizePerChannel);

            float gain = _playbackVolumePercent / 100f;
            if (gain != 1.0f && decodedSamples > 0)
            {
                for (int i = 0; i < decodedSamples; i++)
                {
                    if (float.IsNaN(decodedFrame[i]) || float.IsInfinity(decodedFrame[i]))
                        decodedFrame[i] = 0f;
                    else
                        decodedFrame[i] = Math.Clamp(decodedFrame[i] * gain, -1.0f, 1.0f);
                }
            }
            
            ReadOnlySpan<byte> decodedBytes = MemoryMarshal.AsBytes<float>(decodedFrame);
            var outBuf = new byte[decodedBytes.Length];
            decodedBytes.CopyTo(outBuf);
            
            if (_playbackEnabled)
                channel.Stream.Write(outBuf, 0, outBuf.Length);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, 
                $"[AudioTranslator] Decode error for {interlocutorId}: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private InterlocutorAudioChannel CreateChannelForInterlocutor(string interlocutorId)
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        Logger.Write(Logger.Type.Info, $"[AudioTranslator][Thread-{threadId}] Creating audio channel for {interlocutorId}. Current channels count: {_channels.Count}");

        lock (_readyLock)
        {
            if (!_audioEngineReady)
            {
                Logger.Write(Logger.Type.Info, $"[AudioTranslator][Thread-{threadId}] Calling Monitor.Wait...");
                
                if (!Monitor.Wait(_readyLock, TimeSpan.FromSeconds(5)))
                {
                    Logger.Write(Logger.Type.Error, $"[AudioTranslator][Thread-{threadId}] Timeout waiting for audio engine initialization");
                    return null;
                }
                
                Logger.Write(Logger.Type.Info, $"[AudioTranslator][Thread-{threadId}] Monitor.Wait returned, engine should be ready");
            }
        }
        
        try
        {
            if (audioEngine == null)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] audioEngine is null, cannot create channel for {interlocutorId}");
                return null;
            }
            
            if (playbackDeviceWorker == null)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] playbackDeviceWorker is null, cannot create channel for {interlocutorId}");
                return null;
            }
            
            var stream = new ProducerConsumerStream();
            var dataProvider = new RawDataProvider(
                stream,
                audioFormat.Format,
                audioFormat.SampleRate,
                audioFormat.Channels
            );

            DeviceInfo? selectedPlayback = null;
            if (_currentPlaybackDeviceId != null)
            {
                foreach (var d in audioEngine.PlaybackDevices)
                {
                    if (d.Id == _currentPlaybackDeviceId.Value)
                    {
                        selectedPlayback = d;
                        break;
                    }
                }
            }

            var dedicatedPlaybackDevice = audioEngine.InitializePlaybackDevice(selectedPlayback, audioFormat);
            if (dedicatedPlaybackDevice == null)
            {
                Logger.Error($"[AudioTranslator] Failed to initialize dedicated playback device for {interlocutorId}");
                return null;
            }
            
            dedicatedPlaybackDevice.Start();

            var player = new SoundPlayer(audioEngine, audioFormat, dataProvider);
            dedicatedPlaybackDevice.MasterMixer.AddComponent(player);

            player.Play();

            var channel = new InterlocutorAudioChannel
            {
                Decoder = new OpusDecoder(audioFormat.SampleRate, audioFormat.Channels),
                Stream = stream,
                DataProvider = dataProvider,
                Player = player,
                DedicatedPlaybackDevice = dedicatedPlaybackDevice
            };
            
            Logger.Write(Logger.Type.Info, $"[AudioTranslator][Thread-{threadId}] Channel created successfully for {interlocutorId}. Total channels: {_channels.Count + 1}");
            
            return channel;
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator][Thread-{threadId}] Failed to create channel for {interlocutorId}: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }
    
    private void OnAudioProcessedHandler(Span<float> samples, Capability capability)
    {
        if (!_captureEnabled)
            return;
                
        float[] inputBuffer = samples.ToArray();

        float gain = _microphoneVolumePercent / 100f;

        if (gain != 1.0f)
        {
            for (int i = 0; i < inputBuffer.Length; i++)
            {
                if (float.IsNaN(inputBuffer[i]) || float.IsInfinity(inputBuffer[i]))
                    inputBuffer[i] = 0f;
                else
                    inputBuffer[i] = Math.Clamp(inputBuffer[i] * gain, -1.0f, 1.0f);
            }
        }

        // normalize entry
        for (int i = 0; i < inputBuffer.Length; i++)
        {
            if (float.IsNaN(inputBuffer[i]) || float.IsInfinity(inputBuffer[i]))
                inputBuffer[i] = 0f;
            else
                inputBuffer[i] = Math.Clamp(inputBuffer[i], -1.0f, 1.0f);
        }

        float rms = 0f;
        for (int i = 0; i < inputBuffer.Length; i++)
            rms += inputBuffer[i] * inputBuffer[i];

        if (inputBuffer.Length > 0)
        {
            rms = (float)Math.Sqrt(rms / inputBuffer.Length);
            _selfAudioLevel = Math.Clamp(rms * 3f, 0f, 1f);
            Interlocked.Exchange(ref _selfAudioLevelUpdatedAtTicks, DateTime.UtcNow.Ticks);
        }

        lock (rnnoiseBuffer)
        {
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

                lock (opusBuffer)
                {
                    opusBuffer.AddRange(rnFrame);
                }
            }
        }
    
        lock (opusBuffer)
        {
            while (opusBuffer.Count >= 480)
            {
                float[] opusFrame = opusBuffer.GetRange(0, 480).ToArray();
                opusBuffer.RemoveRange(0, 480);

                int encodedBytes = encoder.Encode(opusFrame, 0, 480, opusPacket, 0, MaxOpusPacketBytes);

                if (encodedBytes > 0)
                    _ = SendAudioBytesAsync(opusPacket, encodedBytes);
            }
        }
    }
   
    private volatile bool _denoiseEnabled = true;
    private float _vadGain = 1.0f;
    
    private void StopAudioDevices()
    {
        try
        {
            captureDeviceWorker?.Stop();
            playbackDeviceWorker?.Stop();
            
            foreach (var channel in _channels.Values)
            {
                try
                {
                    channel.Player?.Stop();
                }
                catch { }
            }
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

        foreach (var kvp in _channels)
        {
            try 
            { 
                playbackDeviceWorker?.MasterMixer.RemoveComponent(kvp.Value.Player);
                kvp.Value.Dispose(); 
            } 
            catch { }
        }
        _channels.Clear();
        
        // Dispose engine
        try
        {
            audioEngine?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Warning, $"[AudioTranslator] Error disposing engine: {ex.Message}");
        }
        
        _cancellationTokenSource?.Dispose();
        
        Logger.Write(Logger.Type.Info, "[AudioTranslator] Disposed");
    }
}
