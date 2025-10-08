using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

public class AudioTranslator
{
    private UdpClient _udpClient;
    private IPEndPoint _remoteEndPoint;
    private CancellationTokenSource _cancellationTokenSource;

    private AudioCaptureDevice captureDeviceWorker;
    private AudioPlaybackDevice playbackDeviceWorker;

    private SoundPlayer player;
    private ProducerConsumerStream pcmStream;
    
    public AudioTranslator(UdpClient client, IPEndPoint remoteEndPoint, CancellationTokenSource cts)
    {
        _udpClient = client;
        _remoteEndPoint = remoteEndPoint;
        _cancellationTokenSource = cts ?? new CancellationTokenSource();
        
        try
        {
            ConfigureClient(_udpClient);
            Task.Run(() => ReceiveTask(_cancellationTokenSource.Token));
            
            ConfigureAudioEngine();
            
            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Client started on {_udpClient.Client.LocalEndPoint}");
            Logger.Write(Logger.Type.Info, $"[AudioTranslator] Target endpoint: {remoteEndPoint}");

            Play();
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error starting UDP with existing client: {ex.Message}", ex);
        }
    }
    public event Action<byte[]> OnDataReceived;
    private async Task ReceiveTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                byte[] data = result.Buffer;
                
                Console.WriteLine("[AudioTranslator] ReceiveTask", data);
                
                OnDataReceived?.Invoke(data);
                
            }
            catch (Exception ex)
            {
                Logger.Write(Logger.Type.Error, $"[AudioTranslator] Error receiving data: {ex.Message}", ex);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }
    public async void SendAudioBytes(byte[] data, int length)
    {
        Console.WriteLine("SendAudioBytes: " + _remoteEndPoint);
        await _udpClient.SendAsync(data, length, _remoteEndPoint);
    }
    
    private void ConfigureAudioEngine()
    {
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
                
                this.SendAudioBytes(opusPacket, encodedBytes);
            }
        };

        this.OnDataReceived += (receivedData) =>
        {
            try
            {
                var decodedFrame = new float[frameSamplesTotal];
                int decodedSamples = decoder.Decode(receivedData, 0, receivedData.Length, decodedFrame, 0, frameSizePerChannel, false);

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
        
        while (true)
        {
            
            // TODO: ADD STOP AND OTHER
            
            
            Thread.Sleep(250);
        }
    }

    private void Play()
    {
       
    }
    
    private void ConfigureClient(UdpClient client)
    {
        client.Client.ReceiveTimeout = 1000;
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
            client.Client.IOControl((IOControlCode) SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"[AudioTranslator] Unable to disable ICMP reset: {ex.Message}", ex);
        }
    }
}