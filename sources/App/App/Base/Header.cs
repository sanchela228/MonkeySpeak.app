using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Raylib_cs;

namespace App.Base;

public class Header : IDisposable
{
    private bool _isDragging = false;
    private Vector2 _mouseStartPos = Vector2.Zero;
    private Vector2 _windowStartPos = Vector2.Zero;

    private FontFamily _fontFamily;
    private FontFamily _fontFamilyVersion;
    private bool _isNear = false;
    private const int HeaderHeight = 60;
    private const float ButtonRadius = 10f;

    
    public Texture2D _textureMainPic;
    public Header()
    {
        _textureMainPic = Resources.Texture("Images\\Browse.png");

        Engine.Managers.Scenes.OnScenePushed += () =>
        {
            MainBackground.Instance.AnimateSpeedChange(3f, 0.7f);
        };
    }
    
    public void Update(float deltaTime)
    {
        Platforms.Windows.Mouse.GetCursorPos(out var globalMousePos);
        
        Font font = Resources.FontEx("Midami-Normal.ttf", 26);
        
        _fontFamily = new()
        {
            Size = 26,
            Font = font,
            Rotation = 0,
            Spacing = 1f,
            Color = Color.White
        };
        
        _fontFamilyVersion = new()
        {
            Size = 20,
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 20),
            Rotation = 0,
            Spacing = 1f,
            Color = new Color(255, 255, 255, 80)
        };
        
        Vector2 mousePos = new Vector2(globalMousePos.X, globalMousePos.Y);
        Vector2 localMousePos = Raylib.GetMousePosition();
        
        _isNear = Vector2.Distance(localMousePos, new Vector2(Raylib.GetRenderWidth() - 40, 22)) < 38;
        
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 minimizeBtnPos = new Vector2(Raylib.GetRenderWidth() - 55, 25);
            Vector2 closeBtnPos = new Vector2(Raylib.GetRenderWidth() - 30, 25);

            var minimizeBtnPosLocal = Raylib.CheckCollisionPointCircle(localMousePos, minimizeBtnPos, ButtonRadius);
            var closeBtnPosLocal = Raylib.CheckCollisionPointCircle(localMousePos, closeBtnPos, ButtonRadius);
            
            if (localMousePos.Y <= HeaderHeight && (!minimizeBtnPosLocal && !closeBtnPosLocal))
            {
                _isDragging = true;
                _mouseStartPos = mousePos;
                _windowStartPos = new Vector2(Raylib.GetWindowPosition().X, Raylib.GetWindowPosition().Y);
            }
            
            if (minimizeBtnPosLocal)
            {
                Raylib.MinimizeWindow();
            }
            else if (closeBtnPosLocal)
            {
                Environment.Exit(0);
                return;
            }
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            _isDragging = false;
            Raylib.SetMouseCursor(MouseCursor.Default);
        }
            
        if (_isDragging)
        {
            Raylib.SetMouseCursor(MouseCursor.ResizeAll);
            
            Vector2 offset = mousePos - _mouseStartPos;
            Vector2 newWindowPos = _windowStartPos + offset;
            Raylib.SetWindowPosition((int)newWindowPos.X, (int)newWindowPos.Y);
        }
    }
    
    public void Draw()
    {
        Text.DrawPro(_fontFamily, "MonkeySpeak", new Vector2(Raylib.GetRenderWidth() / 2, 24));
        
        Color yellowColor = _isNear ? Color.Yellow : new Color(70, 70, 70);
        Color redColor = _isNear ? Color.Red : new Color(70, 70, 70);
        
        Raylib.DrawCircle(Raylib.GetRenderWidth() - 55, 25, 6.5f, yellowColor);
        Raylib.DrawCircle(Raylib.GetRenderWidth() - 30, 25, 6.5f, redColor);
        
        Text.DrawPro(
            _fontFamilyVersion, 
            $"e:old:a:{Context.AppConfig.VersionName}.{Context.AppConfig.Version}", 
            new Vector2(Raylib.GetScreenWidth() - 120, Raylib.GetScreenHeight() - 25)
        );
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(_textureMainPic);
    }
}