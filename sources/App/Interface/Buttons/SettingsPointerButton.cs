using Engine;
using Raylib_cs;

namespace Interface.Buttons;

public class SettingsPointerButton : Node
{
    public SettingsPointerButton()
    {
        Size = new System.Numerics.Vector2(20, 20);
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
        Raylib.DrawCircle((int)x, (int)y, 11, Color.White);
    }

    public override void Dispose()
    {
        
    }
}