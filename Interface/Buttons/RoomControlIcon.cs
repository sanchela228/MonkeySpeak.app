using System.Numerics;
using Engine;
using Raylib_cs;

namespace Interface.Buttons;

public class RoomControlIcon : Button
{
    public bool IsActive = false;
    public Texture2D? SecondTexture;
    public Texture2D DefaultTexture;
    
    public RoomControlIcon(FontFamily font, string text = "") : base(font, text)
    {
    }

    public RoomControlIcon(Texture2D tex, Vector2 size, Texture2D? secondTex = null) : base(tex, size)
    {
        DefaultTexture = tex;
        SecondTexture = secondTex;
        CornerRadius = 0.6f;
        CornerWidth = 1f;
        CornerColor = new Color(30, 30, 30);
        
        Padding = new Vector2(20, 20);
        HoverBackgroundColor = new Color( 40, 40, 40);
        HoverCornerColor = new Color(40, 40, 40);

        OnHoverEnter += (node) =>
        {
            Raylib.SetMouseCursor(MouseCursor.PointingHand);
        };

        OnHoverExit += (node) =>
        {
            Raylib.SetMouseCursor(MouseCursor.Default);
        };
        
        OnPress += (node) =>
        {
            IsActive = !IsActive;

            if (IsActive)
            {
                BackgroundColor = Color.White;
                IconTexture = DefaultTexture;
            }
            else
            {
                BackgroundColor = new Color(15, 15, 15);
                
                if (SecondTexture is not null)
                {
                    IconTexture = SecondTexture;
                }
            }
        };
    }
}