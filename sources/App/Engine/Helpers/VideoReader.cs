using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FFMpegCore;
using K4os.Compression.LZ4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Raylib_cs;

namespace Engine.Helpers
{
    public class VideoReader : IDisposable
    {
        private readonly Process _ffmpeg;
        private readonly Stream _stdout;
        public int Width { get; private set; }
        public int Height { get; private set; }
        private readonly int _frameSize;
        private readonly byte[] _buffer;
        private bool _finished;
        private string originalPath;

        public VideoReader(string path)
        {
            var info = FFProbe.Analyse(path);
            Width = info.PrimaryVideoStream.Width;
            Height = info.PrimaryVideoStream.Height;

            _frameSize = Width * Height * 4;
            _buffer = new byte[_frameSize];

            string args =
                $"-i \"{path}\" -f rawvideo -pix_fmt rgba -";

            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppContext.BaseDirectory,
                        OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg"),
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _ffmpeg.Start();
            _stdout = _ffmpeg.StandardOutput.BaseStream;
            originalPath = path;
        }

        public List<byte[]> GetCompressedFrames()
        {
            List<byte[]> compressedFrames = new();

            while (true)
            {
                var frame = ReadFrame();
                if (frame == null)
                    break;

                byte[] raw = new byte[frame.Width * frame.Height * 4];
                frame.CopyPixelDataTo(raw);

                byte[] output = new byte[LZ4Codec.MaximumOutputSize(raw.Length)];
                int len = LZ4Codec.Encode(raw, 0, raw.Length, output, 0, output.Length);

                Array.Resize(ref output, len);
                compressedFrames.Add(output);

                frame.Dispose();
            }

            return compressedFrames;
        }

        public static List<byte[]> GetCompressedFrames(string path)
        {
            using VideoReader reader = new(path);
            return reader.GetCompressedFrames();
        }
        
        public static byte[] UncompressFrame(byte[] compressedFrame, int originalSize)
        {
            byte[] output = new byte[originalSize];
    
            try
            {
                int decoded = LZ4Codec.Decode(
                    compressedFrame, 0, compressedFrame.Length,
                    output, 0, output.Length
                );
        
                if (decoded < 0)
                    throw new InvalidOperationException($"LZ4 decode failed with code: {decoded}");
        
                if (decoded != originalSize)
                    Array.Resize(ref output, decoded);
        
                return output;
            }
            catch (Exception ex)
            {
                return new byte[originalSize];
            }
        }

        public List<byte[]> GetCachedCompressedFrames()
        {
            return GetCachedCompressedFrames(originalPath);
        }
        
        public static List<byte[]> GetCachedCompressedFrames(string path)
        {
            try
            {
                string cachePath = Path.Combine(
                    AppContext.BaseDirectory, 
                    "cache", 
                    Path.GetFileNameWithoutExtension(path) + ".lz4"
                );
            
                if (!Directory.Exists(Path.GetDirectoryName(cachePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath) ?? string.Empty);
            
                if (File.Exists(cachePath))
                    return ReadLZ4Cache(cachePath);
                
                List<byte[]> compressedFrames = GetCompressedFrames(path);
                SaveToLZ4Cache(cachePath, compressedFrames);
                return compressedFrames;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            return new List<byte[]>();
        }
        
        public static void PrepareCache(string path) => GetCachedCompressedFrames(path);

        public static void PrepareCache(IEnumerable<string> paths)
        {
            foreach (var p in paths) 
                GetCachedCompressedFrames(p);
        }
        

        public Image<Rgba32>? ReadFrame()
        {
            if (_finished) return null;

            int totalRead = 0;

            while (totalRead < _frameSize)
            {
                int read = _stdout.Read(_buffer, totalRead, _frameSize - totalRead);
                if (read <= 0)
                {
                    _finished = true;
                    return null;
                }

                totalRead += read;
            }

            var img = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(_buffer, Width, Height);
            return img;
        }

        private static List<byte[]> ReadLZ4Cache(string cachePath)
        {
            using (FileStream fs = new FileStream(cachePath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                int count = reader.ReadInt32();
                List<byte[]> result = new List<byte[]>(count);
            
                for (int i = 0; i < count; i++)
                {
                    int length = reader.ReadInt32();
                    byte[] lz4Frame = reader.ReadBytes(length);
                    result.Add(lz4Frame);
                }
            
                return result;
            }
        }

        private static void SaveToLZ4Cache(string cachePath, List<byte[]> compressedFrames)
        {
            using (FileStream fs = new FileStream(cachePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(compressedFrames.Count);
                foreach (byte[] lz4Frame in compressedFrames)
                {
                    writer.Write(lz4Frame.Length);
                    writer.Write(lz4Frame);
                }
            }
        }
        
        public void Dispose()
        {
            try
            {
                if (!_ffmpeg.HasExited)
                    _ffmpeg.Kill();
            }
            catch { }

            _ffmpeg.Dispose();
            _stdout.Dispose();
        }
    }
}