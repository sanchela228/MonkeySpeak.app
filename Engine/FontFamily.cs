using System.Numerics;
using Raylib_cs;

namespace Engine;

public struct FontFamily
{
    public Color Color { get; set; }
    public Font Font { get; set; }
    public int Size { get; set; }
    public float Rotation { get; set; }
    public float Spacing { get; set; }

    public Vector2 CalcTextSize(string text) => Raylib.MeasureTextEx(Font, text, Size, Spacing);
    
}