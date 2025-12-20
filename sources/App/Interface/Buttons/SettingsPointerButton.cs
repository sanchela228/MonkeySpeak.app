using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Interface.Buttons;

public class SettingsPointerButton : Node
{
    public Texture2D Arrow;
    
    public SettingsPointerButton()
    {
        Size = new System.Numerics.Vector2(20, 20);
        Arrow = Resources.Texture("Images\\Icons\\SettingPointerButton_Arrow_White.png");
    }

    public override void Update(float deltaTime)
    {
        if (Parent != null)
        {
            var parentBounds = Parent.Bounds;

            Position = Parent.Position + new System.Numerics.Vector2(
                parentBounds.Width / 2f,
                -parentBounds.Height / 2f
            );
        }
    }

    public override void Draw()
    {
        var rect = Bounds;
        var x = rect.X + rect.Width / 2f - 3f;
        var y = rect.Y + rect.Height / 2f + 3f;
        Raylib.DrawCircle((int)x, (int)y, 12.5f, Color.Gray);
        Raylib.DrawCircle((int)x, (int)y, 11, new Color(30, 30, 30, 255));
        
        Texture.DrawPro(
            Arrow, 
            new Vector2(x, y), 
            new Vector2(rect.Width - 3f, rect.Height - 3f)
        );
    }

    public override void Dispose()
    {
        
    }
}