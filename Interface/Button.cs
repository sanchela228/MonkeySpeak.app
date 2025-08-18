using System.Numerics;
using Engine;
using Engine.Helpers;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Interface;

public enum ButtonState
{
    Normal,
    Hovered,
    Pressed,
    Disabled
}

public class Button : Node
{
    public event Action<Button> OnClick;
    public event Action<Button> OnHoverEnter;
    public event Action<Button> OnHoverExit;
    public event Action<Button> OnPress;
    public event Action<Button> OnRelease;

    #region Properties
    public string Text { get; set; } = "Test";
    public FontFamily Font { get; set; }
    
    public Vector2 Padding { get; set; } = new(10, 5);
    public float CornerRadius { get; set; } = 0;
    
    public float CornerWidth { get; set; } = 0f;
    public Color CornerColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.White;
    public Color? HoverBackgroundColor { get; set; } = null;
    public Color? PressedBackgroundColor { get; set; } = null;
    public Color? DisabledBackgroundColor { get; set; } = null;
    public Color? TextColor { get; set; }
    public Color? DisabledTextColor { get; set; } = null;

    public Rectangle GetLocalBounds
    {
        get
        {
            var localBounds = Bounds;
            localBounds.X -= Padding.X / 2;
            localBounds.Width += Padding.X;
        
            localBounds.Y -= Padding.Y / 2;
            localBounds.Height += Padding.Y;
            
            float textWidth = 0f;
            if (!string.IsNullOrEmpty(Text))
                textWidth = Raylib.MeasureTextEx(Font.Font, Text, Font.Size, Font.Spacing).X;
            
            localBounds.X -= textWidth / 2;
            localBounds.Width += textWidth;
            localBounds.Height += Font.Size + Font.Spacing;
            localBounds.Y -= Font.Spacing / 2;
            localBounds.Y -= Font.Size / 2;
            
            return localBounds;
        }
    }

    public ButtonState State { get; private set; } = ButtonState.Normal;
    public bool IsEnabled { get; set; } = true;
    
    public bool EnableHoverAnimation { get; set; } = true;
    public float HoverAnimationSpeed { get; set; } = 5f;
    
    private bool _isHovered = false;
    private bool _isPressed = false;
    #endregion

    public Button(string text = "", FontFamily? font = null)
    {
        if (!string.IsNullOrEmpty(text))
            Text = text;
        
        Font = font ?? new FontFamily
        {
            Font = Raylib.GetFontDefault(),
            Size = 16,
            Color = Color.Black,
            Spacing = 1f
        };
    }
    
    private void UpdateState()
    {
        if (!IsEnabled)
        {
            State = ButtonState.Disabled;
            return;
        }
        
        if (_isPressed)
        {
            State = ButtonState.Pressed;
        }
        else if (_isHovered)
        {
            State = ButtonState.Hovered;
        }
        else
        {
            State = ButtonState.Normal;
        }
    }
    
    protected Color GetCurrentBackgroundColor()
    {
        return State switch
        {
            ButtonState.Hovered => HoverBackgroundColor ?? BackgroundColor,
            ButtonState.Pressed => PressedBackgroundColor ?? BackgroundColor,
            ButtonState.Disabled => DisabledBackgroundColor ?? BackgroundColor,
            _ => BackgroundColor
        };
    }
    
    protected Color GetCurrentTextColor()
    {
        return State == ButtonState.Disabled ? DisabledTextColor ?? Font.Color  : TextColor ?? Font.Color;
    }
    
    private void HandleInput()
    {
        if (!IsEnabled) return;
        
        var mousePos = Raylib.GetMousePosition();
        var wasHovered = _isHovered;

        _isHovered = Raylib.CheckCollisionPointRec(mousePos, GetLocalBounds);
        
        if (_isHovered && !wasHovered)
        {
            Raylib.SetMouseCursor(MouseCursor.PointingHand);
            OnHoverEnter?.Invoke(this);
        }
        else if (!_isHovered && wasHovered)
        {
            Raylib.SetMouseCursor(MouseCursor.Default);
            OnHoverExit?.Invoke(this);
        }
        
        if (_isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _isPressed = true;
            OnPress?.Invoke(this);
        }
        
        if (_isPressed && Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            _isPressed = false;
            if (_isHovered)
            {
                OnClick?.Invoke(this);
            }
            OnRelease?.Invoke(this);
        }
        
        UpdateState();
    }
    
    private void UpdateHoverAnimation(float deltaTime)
    {
        if (!EnableHoverAnimation) 
            return;
    }

    public override void Update(float deltaTime)
    {
        HandleInput();
        UpdateHoverAnimation(deltaTime);
    }
    
    public override void Draw()
    {
        if (!IsActive) return;
        
        var backgroundColor = GetCurrentBackgroundColor();
        var textColor = GetCurrentTextColor();
        var localBounds = GetLocalBounds;
        
        if (CornerRadius > 0f)
        {
            Raylib.DrawRectangleRounded(
                localBounds, 
                CornerRadius, 
                10, 
                backgroundColor
            );
        }
        else
        {
            Raylib.DrawRectangleRec(localBounds, backgroundColor);
        }
        
        if (CornerWidth > 0f)
        {
            Raylib.DrawRectangleRoundedLinesEx(
                new Rectangle(
                    localBounds.X + 1, 
                    localBounds.Y + 1, 
                    localBounds.Width - 1, 
                    localBounds.Height - 1
                ), 
                CornerRadius, 
                10, 
                CornerWidth,
                CornerColor
            );
        }
        
        if (!string.IsNullOrEmpty(Text))
        {
            Engine.Helpers.Text.DrawPro(
                Font, 
                Text , 
                new Vector2(Bounds.X, Bounds.Y), 
                null, 
                0f, 
                textColor, 
                Font.Size, 
                Font.Spacing
            );
        }
    }
    
    public override void Dispose()
    {
        OnClick = null;
        OnHoverEnter = null;
        OnHoverExit = null;
        OnPress = null;
        OnRelease = null;
    }
}

