using System.Numerics;
using Engine;
using Engine.Managers;
using Raylib_cs;

namespace Interface;

public class Avatar : Node
{
    private Texture2D InterlocutorMutedIcon;
    public bool IsMuted = false;
    public Avatar(Vector2 pos)
    {
        Position = pos;
        Size = new Vector2(220, 220);
    }
    
    public override void Update(float deltaTime)
    {
        
    }

    public override void Draw()
    {
        Raylib.DrawRectangleRounded(
            Bounds, 
            0.65f, 
            22, 
            Color.Gray
        );

        if (IsMuted)
        {
            Raylib.DrawRectangleRounded(
                new Rectangle(Bounds.Position.X, Bounds.Position.Y, 20, 20), 
                1f, 
                22, 
                Color.Red
            );
        }
    }

    public override void Dispose()
    {
        
    }
}