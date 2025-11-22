using System.Numerics;
using Raylib_cs;

namespace Engine.Helpers;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public static class Text
{
    public static void DrawWrapped(FontFamily fontFamily, string text, Vector2 pos, float size, TextAlignment alignment = TextAlignment.Left, Color? color = null)
    {
        string[] words = text.Split(' ');
        string currentLine = "";
        float y = pos.Y;
        int lines = 0;

        foreach (string word in words)
        {
            string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
            float textWidth = Raylib.MeasureTextEx(fontFamily.Font, testLine, fontFamily.Size, fontFamily.Spacing).X;
        
            if (textWidth <= size) currentLine = testLine;
            else
            {
                if (currentLine.Length > 0)
                {
                    DrawAlignedText(fontFamily, currentLine, pos.X, y, size, alignment, color);
                    y += fontFamily.Size + fontFamily.Spacing;
                }
                currentLine = word;
            }
        }

        if (currentLine.Length > 0)
            DrawAlignedText(fontFamily, currentLine, pos.X, y, size, alignment, color);
    }

    public static float CalculateWrappedTextHeight(FontFamily fontFamily, string text, float maxWidth)
    {
        if (string.IsNullOrEmpty(text)) 
            return 0;

        string[] words = text.Split(' ');
        string currentLine = "";
        float totalHeight = fontFamily.Size;
        int lineCount = 1;

        foreach (string word in words)
        {
            string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
            float textWidth = Raylib.MeasureTextEx(fontFamily.Font, testLine, fontFamily.Size, fontFamily.Spacing).X;
    
            if (textWidth <= maxWidth)
            {
                currentLine = testLine;
            }
            else
            {
                if (currentLine.Length > 0)
                {
                    lineCount++;
                    totalHeight += fontFamily.Size + fontFamily.Spacing;
                }
                currentLine = word;
            }
        }

        return totalHeight;
    }

    public static void DrawWrappedWordBySymbols(FontFamily fontFamily, string text, Vector2 pos, bool reverse = false)
    {
        float y = pos.Y;
    
        foreach (char symbol in text)
        {
            DrawPro(fontFamily, symbol.ToString(), new Vector2(pos.X, y), rotation: reverse ? 180f : 0f);
            
            if (reverse)
                y -= fontFamily.Spacing;
            else
                y += fontFamily.Spacing;
        }
    }

    public static void DrawPro(FontFamily fontFamily, string text, Vector2 pos, Vector2? origin = null, float? 
        rotation = null, Color? color = null, int? size = null, float? spacing = null)
    {
        var originLocal = fontFamily.CalcTextSize(text);
        
        Raylib.DrawTextPro(
            fontFamily.Font,
            text,
            pos,
            origin ?? new Vector2(originLocal.X / 2, originLocal.Y / 2),
            rotation ?? fontFamily.Rotation,
            size ?? fontFamily.Size,
            spacing?? fontFamily.Spacing,
            color ?? fontFamily.Color
        );
    }

    private static void DrawAlignedText(FontFamily fontFamily, string text, float x, float y, float maxWidth, TextAlignment alignment, Color? color = null)
    {
        float textWidth = Raylib.MeasureTextEx(fontFamily.Font, text, fontFamily.Size, fontFamily.Spacing).X;
        float startX = x;

        switch (alignment)
        {
            case TextAlignment.Center:
                startX = x + (maxWidth - textWidth) / 2;
                break;
            case TextAlignment.Right:
                startX = x + maxWidth - textWidth;
                break;
        }

        Raylib.DrawTextEx(
            fontFamily.Font,
            text,
            new Vector2(startX, y),
            fontFamily.Size,
            fontFamily.Spacing,
            color ?? fontFamily.Color
        );
    }
}