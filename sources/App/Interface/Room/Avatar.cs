using System;
using System.IO;
using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;

namespace Interface.Room;

public class Avatar : Node
{
    public float AudioLevel = 0f;

    public bool FramesLoaded { get; protected set; } = false;
    private static readonly Lock FramesLoadingLock = new();
    private Texture2D textureVideo;
    protected RenderTexture2D? _canvas;
    
    private List<byte[]> _frames;
    private int _currentFrame;
    private float _frameTime;
    private float _timer;
    
    private string _videoPath;
    private float _fps;
    private bool _texturesLoaded = false;
    private Texture2D InterlocutorMutedIcon;
    
    private int _videoWidth = 512;
    private int _videoHeight = 512;
    public bool IsMuted = false;

    public Avatar(string videoPath, float fps = 30f)
    {
        lock (FramesLoadingLock)
        {
            FramesLoaded = false;
        }
        
        _videoPath = videoPath;
        _fps = fps;
        _frameTime = 1f / fps;
        InterlocutorMutedIcon = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
        
        
        Task.Run(() =>
        {
            lock (FramesLoadingLock)
            {
                using var videoReader = new VideoReader(_videoPath);
                _videoWidth = videoReader.Width;
                _videoHeight = videoReader.Height;
                
                _frames = videoReader.GetCachedCompressedFrames();
                FramesLoaded = true;
            }
        });
    }
    
    public override void Update(float deltaTime)
    {
        if (!shaderLoaded)
        {
            circleShader = Resources.Shader("circle_mask.frag");
            shaderLoaded = true;
        }
        
        if (FramesLoaded)
        {
            if (_canvas is null)
                _canvas = Raylib.LoadRenderTexture(_videoWidth, _videoHeight);
        }
        
        if (FramesLoaded && _canvas is not null and {} canvas && _frames.Count > 0 && (_currentFrame == 0 || AudioLevel > 0.004f))
        {
            _timer += deltaTime;
            
            if (_timer >= _frameTime)
            {
                Raylib.UpdateTexture(canvas.Texture, VideoReader.UncompressFrame(_frames[_currentFrame], _videoWidth * _videoHeight * 4));
                
                _currentFrame++;
                if (_currentFrame >= _frames.Count)
                    _currentFrame = 0;
                
                _timer -= _frameTime;
          
                while (_timer >= _frameTime && _frames.Count > 0)
                {
                    _currentFrame++;
                    if (_currentFrame >= _frames.Count)
                        _currentFrame = 0;
                    _timer -= _frameTime;
                }
            }
        }
    }

    private Shader circleShader;
    private bool shaderLoaded = false;
    public override void Draw()
    {
        float currentAudioLevel = AudioLevel;

        if (!IsMuted && currentAudioLevel > 0.002f)
        {
            float radius = (Size.X / 2f) + 4f;
            Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, radius, new Color(10, 255, 10, 200));
        }
        
        if (shaderLoaded && FramesLoaded && _canvas is not null and {} canvas)
        {
            Raylib.SetShaderValue(
                circleShader,
                Raylib.GetShaderLocation(circleShader, "resolution"),
                new float[] { Size.X, Size.Y },
                ShaderUniformDataType.Vec2
            );
            
            float radius = Size.X / 2f;
        
            Raylib_cs.Rectangle source = new Raylib_cs.Rectangle(
                0, 0,
                canvas.Texture.Width,
                canvas.Texture.Height
            );
            
            Raylib_cs.Rectangle dest = new Raylib_cs.Rectangle(
                Position.X,
                Position.Y,
                radius * 2,
                radius * 2
            );
        
            Vector2 origin = new Vector2(radius, radius);

            Raylib.BeginShaderMode(circleShader);
            Raylib.DrawTexturePro(
                _canvas.Value.Texture,
                source,
                dest,
                origin,
                0f,
                Color.White
            );
            Raylib.EndShaderMode();
        }
        else
        {
            Raylib.DrawCircleV(Position, Size.X / 2, new Color(80, 80, 80, 50));
        }
        
        if (IsMuted)
        {
            var rect = new Raylib_cs.Rectangle(Bounds.Position.X + Bounds.Width / 2 - 35 + 35, Bounds.Position.Y + Bounds.Height / 2 - 35 + 35, 40, 40);
            
            var rect2 = new Raylib_cs.Rectangle(Bounds.Position.X + Bounds.Width / 2 - 35, Bounds.Position.Y + Bounds.Height / 2 - 35, 70, 70);
            Raylib.DrawRectangleRounded(
                rect2, 
                1f, 
                22, 
                Color.Red
            );
            
            Texture.DrawPro(InterlocutorMutedIcon, rect.Position, new Vector2(40, 40));
        }
    }
    
    public override void Dispose()
    {
        if (FramesLoaded)
        {
            _frames.Clear();
        }
    }
}