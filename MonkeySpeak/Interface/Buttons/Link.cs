using System.Numerics;
using Engine;
using Engine.Managers;
using Raylib_cs;

namespace Interface.Buttons;

public class Link : Button
{
    public Color LineColor;
    public Link(FontFamily fontFamily, string text = "") : base(fontFamily, text)
    {
        CornerRadius = 0;
        CornerWidth = 0;
        CornerColor = Color.Blank;
        BackgroundColor = Color.Blank;
        Padding = new Vector2(0, 0);

        LineColor = FontFamily.Color;
        
        this.OnDraw += (sender) =>
        {
            var pos = sender.Position;
            
            float textWidth = 0f;
            if (!string.IsNullOrEmpty(Text))
                textWidth = Raylib.MeasureTextEx(FontFamily.Font, Text, FontFamily.Size, FontFamily.Spacing).X;
            
            Raylib.DrawLineV(
                new Vector2(pos.X - textWidth / 2, pos.Y + 13), 
                new Vector2(pos.X + textWidth / 2, pos.Y + 13), 
                LineColor
            );
        };
    }
}