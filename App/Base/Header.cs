using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;

namespace App.Base;

public class Header
{
    private bool _isDragging = false;
    private Vector2 _mouseStartPos = Vector2.Zero;
    private Vector2 _windowStartPos = Vector2.Zero;

    private FontFamily _fontFamily;
    private bool _isNear = false;
    private const int HeaderHeight = 60;
    private const float ButtonRadius = 10f;

    
    public Texture2D _textureMainPic;
    public Header()
    {
        _textureMainPic = Resources.Instance.Texture("Images\\Browse.png");
    }
    
    public void Update(float deltaTime)
    {
        Platforms.Windows.Mouse.GetCursorPos(out var globalMousePos);
        
        Font font = Resources.Instance.FontEx("Midami-Normal.ttf", 26);
            
        _fontFamily = new()
        {
            Size = 26,
            Font = font,
            Rotation = 0,
            Spacing = 1f,
            Color = Color.White
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
                
                Raylib.CloseWindow();
            }
        }
            
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            _isDragging = false;
        }
            
        if (_isDragging)
        {
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
        
        Texture.DrawEx(_textureMainPic, new Vector2(70 , 22), color: new Color{R = 255, G = 255, B = 255, A = 115});
        
        Raylib.DrawCircle(76, 28, 3.5f, Context.Instance.Network.GetStateColor());
        
    }
}