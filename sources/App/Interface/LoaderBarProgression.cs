using Engine;
using Raylib_cs;

namespace Interface;

public class LoaderBarProgression : Node
{
    public Color Color = new(46, 188, 75, 255);
    public Color BackgroundColor = new(255, 255, 255, 55);
    public float BorderRadius = 2f;

    public float Total = 0;
    public float Current = 0;
    
    public float Progress => Total > 0 ?  Current  / Total : 0;
    public override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
    }

    public override void Draw()
    {
        // draw background
        Raylib.DrawRectangleRounded(
            Bounds, 
            BorderRadius, 
            10, 
            BackgroundColor
        );
        
        // draw progress
        if (Progress > 0.02f)
        {
            Raylib.DrawRectangleRounded(
                new Rectangle(Bounds.X, Bounds.Y, Bounds.Width * Progress, Bounds.Height), 
                BorderRadius, 
                10, 
                Color
            ); 
        }
    }

    public override void Dispose()
    {
        // throw new NotImplementedException();
    }
}