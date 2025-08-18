using System.Numerics;
using Raylib_cs;

namespace Engine.Helpers;


public enum PointRendering
{
    LeftTop,
    Center,
}

public static class Texture
{
    public static void DrawEx(Texture2D texture, Vector2 position, float rotation = 0, float scale = 1f, 
        Color? color = null, PointRendering pointRendering = PointRendering.Center )
    {
        if (pointRendering == PointRendering.Center)
            position = new Vector2(position.X - texture.Width / 2, position.Y - texture.Height / 2);
        
        Raylib.DrawTextureEx(
            texture, 
            position, 
            rotation,
            scale, 
            color ?? Color.White
        );
    }
}