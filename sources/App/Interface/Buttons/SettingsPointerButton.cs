using Engine;
using Raylib_cs;

namespace Interface.Buttons;

public class SettingsPointerButton : Node
{
    public override void Update(float deltaTime)
    {
        
    }

    public override void Draw()
    {
        var x = Position.X + Parent.Bounds.Width / 2;
        var y = Position.Y - Parent.Bounds.Height / 2;
        Raylib.DrawCircle((int) x, (int) y, 10, Color.Red);
    }

    public override void Dispose()
    {
        
    }
}