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

    public static void DrawPro(
        Texture2D texture,
        Vector2 position,
        Vector2 size,
        Raylib_cs.Rectangle? source = null,
        float rotation = 0f,
        Color? color = null,
        TextureFilter? textureFilter = null,
        PointRendering pointRendering = PointRendering.Center)
    {
        var src = source ?? new Raylib_cs.Rectangle(0, 0, texture.Width, texture.Height);

        float destW = size.X > 0 ? size.X : src.Width;
        float destH = size.Y > 0 ? size.Y : src.Height;

        float destX = position.X;
        float destY = position.Y;
        if (pointRendering == PointRendering.Center)
        {
            destX -= destW / 2f;
            destY -= destH / 2f;
        }
        
        Raylib.SetTextureFilter(texture, textureFilter ?? TextureFilter.Bilinear);

        var dest = new Raylib_cs.Rectangle(destX, destY, destW, destH);
        var origin = new Vector2(0, 0);

        Raylib.DrawTexturePro(
            texture,
            src,
            dest,
            origin,
            rotation,
            color ?? Color.White
        );
    }
}