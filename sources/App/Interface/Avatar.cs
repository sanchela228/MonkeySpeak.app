using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Interface;

public class Avatar : Node
{
    private Texture2D InterlocutorMutedIcon;
    public bool IsMuted = false;
    public Avatar(Vector2 pos)
    {
        Position = pos;
        InterlocutorMutedIcon = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
        Size = new Vector2(30, 30);
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
            var rect = new Rectangle(Bounds.Position.X + Bounds.Width - 30, Bounds.Position.Y + Bounds.Height - 10, 40, 40);
            
            var rect2 = new Rectangle(Bounds.Position.X + Bounds.Width - 30 - 25, Bounds.Position.Y + Bounds.Height - 10 - 25, 50, 50);
            Raylib.DrawRectangleRounded(
                rect2, 
                1f, 
                22, 
                Color.Red
            );
            
            Texture.DrawPro(InterlocutorMutedIcon, rect.Position, new Vector2(40, 40));
        }
    }

    public override void Dispose()
    {
        
    }
}