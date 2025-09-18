using System.Numerics;
using Engine;
using Engine.Managers;
using Raylib_cs;

namespace Interface.Buttons;

public class Classic : Button
{
    public Classic(FontFamily fontFamily, string text = "") : base(fontFamily, text)
    {
        CornerRadius = 0.4f;
        CornerWidth = 2f;
        CornerColor = new Color(30, 30, 30);
        BackgroundColor = new Color(15, 15, 15);
        Padding = new Vector2(20, 18);
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
    }
}