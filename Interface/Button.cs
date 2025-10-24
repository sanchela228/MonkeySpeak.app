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
    protected event Action<Button> OnDraw;

    #region Properties
    public string Text { get; set; } = "Test";
    public FontFamily FontFamily { get; set; }
    
    public Vector2 Padding { get; set; } = new(10, 5);
    public float CornerRadius { get; set; } = 0;
    
    public float CornerWidth { get; set; } = 0f;
    public Color CornerColor { get; set; } = Color.White;
    public Color? HoverCornerColor { get; set; } = null;
    public Color BackgroundColor { get; set; } = Color.White;
    public Color? HoverBackgroundColor { get; set; } = null;
    public Color? PressedBackgroundColor { get; set; } = null;
    public Color? DisabledBackgroundColor { get; set; } = null;
    public Color? TextColor { get; set; }
    public Color? DisabledTextColor { get; set; } = null;

    public Texture2D? IconTexture { get; set; } = null;
    public Vector2? IconSize { get; set; } = null;
    public Rectangle? IconSourceRect { get; set; } = null;
    public Color? IconTint { get; set; } = null;
    public TextureFilter? IconFilter { get; set; } = TextureFilter.Bilinear;

    public Rectangle GetLocalBounds
    {
        get
        {
            var localBounds = base.Bounds;
            localBounds.X -= Padding.X / 2;
            localBounds.Width += Padding.X;
        
            localBounds.Y -= Padding.Y / 2;
            localBounds.Height += Padding.Y;

            // Content sizing: prefer icon if provided, otherwise text
            if (IconTexture.HasValue)
            {
                var tex = IconTexture.Value;
                float contentW;
                float contentH;

                if (IconSize.HasValue && (IconSize.Value.X > 0f && IconSize.Value.Y > 0f))
                {
                    contentW = IconSize.Value.X;
                    contentH = IconSize.Value.Y;
                }
                else
                {
                    if (IconSourceRect.HasValue)
                    {
                        contentW = IconSourceRect.Value.Width;
                        contentH = IconSourceRect.Value.Height;
                    }
                    else
                    {
                        contentW = tex.Width;
                        contentH = tex.Height;
                    }
                }

                localBounds.X -= contentW / 2f;
                localBounds.Y -= contentH / 2f;
                localBounds.Width += contentW;
                localBounds.Height += contentH;
            }
            else
            {
                float textWidth = 0f;
                if (!string.IsNullOrEmpty(Text))
                    textWidth = Raylib.MeasureTextEx(FontFamily.Font, Text, FontFamily.Size, FontFamily.Spacing).X;
                
                localBounds.X -= textWidth / 2;
                localBounds.Width += textWidth;
                localBounds.Height += FontFamily.Size + FontFamily.Spacing;
                localBounds.Y -= FontFamily.Spacing / 2;
                localBounds.Y -= FontFamily.Size / 2;
            }

            return localBounds;
        }
    }
    
    public override Rectangle Bounds => GetLocalBounds;

    public ButtonState State { get; private set; } = ButtonState.Normal;
    public bool IsEnabled { get; set; } = true;
    
    public bool EnableHoverAnimation { get; set; } = true;
    public float HoverAnimationSpeed { get; set; } = 5f;
    
    #endregion

    public Button(FontFamily font, string text = "")
    {
        if (!string.IsNullOrEmpty(text))
            Text = text;

        FontFamily = font;
    }
    
    public Button(Texture2D tex, Vector2 size)
    {
        IconTexture = tex;
        IconSize = size;
        IconSourceRect = new Rectangle(0, 0, tex.Width, tex.Height);
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
    
    protected Color GetBorderColor()
    {
        return State == ButtonState.Hovered ? HoverCornerColor ?? CornerColor : CornerColor;
    }
    
    protected Color GetCurrentTextColor()
    {
        return State == ButtonState.Disabled ? DisabledTextColor ?? FontFamily.Color  : TextColor ?? FontFamily.Color;
    }
    
    private void HandleInput()
    {
        if (!IsEnabled) return;
        
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
        var borderColor = GetBorderColor();
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
                borderColor
            );
        }

        if (IconTexture.HasValue)
        {
            var tex = IconTexture.Value;

            if (IconFilter.HasValue)
            {
                Raylib.SetTextureFilter(tex, IconFilter.Value);
            }

            var src = IconSourceRect ?? new Rectangle(0, 0, tex.Width, tex.Height);

            float destW;
            float destH;
            if (IconSize.HasValue && (IconSize.Value.X > 0f && IconSize.Value.Y > 0f))
            {
                destW = IconSize.Value.X;
                destH = IconSize.Value.Y;
            }
            else
            {
                destW = src.Width;
                destH = src.Height;
            }

            var dest = new Rectangle(base.Bounds.X - destW / 2f, base.Bounds.Y - destH / 2f, destW, destH);
            var origin = new Vector2(0, 0);
            var tint = IconTint ?? Color.White;

            Raylib.DrawTexturePro(tex, src, dest, origin, 0f, tint);
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            Engine.Helpers.Text.DrawPro(
                FontFamily,
                Text,
                new Vector2(base.Bounds.X, base.Bounds.Y),
                null,
                0f,
                textColor,
                FontFamily.Size,
                FontFamily.Spacing
            );
        }
        
        OnDraw?.Invoke(this);
    }
    
    public override void Dispose()
    {
    }
}
