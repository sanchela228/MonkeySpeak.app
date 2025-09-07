using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;

namespace Interface.Inputs;

public class DemoInputInvited : Node
{
    public Char? Symbol;
    public bool IsFailed;
    private FontFamily _mainFontBack;
    public DemoInputInvited()
    {
        Size = new Vector2(40, 50);
        
        _mainFontBack = new FontFamily()
        {
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
    }
    
    public override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
    }

    public override void Draw()
    {
        Raylib.DrawRectangleRounded(Bounds, 0.3f, 10, new Color(255, 255, 255, 20));

        if (IsFailed)
        {
            Raylib.DrawRectangleRoundedLinesEx(Bounds, 0.3f, 10, 2, Color.Red);
        }
        
        if (Symbol.HasValue)
        {
            Text.DrawPro(
                _mainFontBack, 
                Symbol.ToString().ToUpper(), 
                new Vector2(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2)
            );
        }
    }

    public override void Dispose()
    {
        // throw new NotImplementedException();
    }
}