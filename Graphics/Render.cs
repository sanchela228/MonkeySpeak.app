using System.Numerics;
using Engine;
using Interface.Buttons;

namespace Graphics;

public class Render
{
    public static void PlaceInLine(IEnumerable<Node> nodes, int pixelSizeElement, Vector2 centerPoint, int pixelsMargin, float lerp = 0f, float deltaTime = 0f)
    {
        var count = nodes.Count();
        var nodes2 = nodes.ToList();
        
        int countMargins = count - 1;
        int totalWidth = (pixelSizeElement * count + countMargins * pixelsMargin);
            
        for (int i = 0; i < count; i++)
        {
            Vector2 targetPosition = new Vector2(
                (centerPoint.X + ((pixelSizeElement + pixelsMargin) * i)) - (totalWidth / 2) + pixelSizeElement / 2, 
                centerPoint.Y
            );
            
            if (lerp > 0 && deltaTime > 0)
            {
                float t = 1.0f - MathF.Exp(-18f * deltaTime);
                nodes2[i].Position = Vector2.Lerp(nodes2[i].Position, targetPosition, t);
            }
            else nodes2[i].Position = targetPosition;
        }
    }
    
    public static void PlaceInLine(IEnumerable<Node> nodes, Vector2 centerPoint, int pixelsMargin, float lerp = 0f, float deltaTime = 0f)
    {
        var nodesList = nodes.ToList();
        var count = nodesList.Count;
        
        if (count == 0) return;

        float totalWidth = nodesList.Sum(n => n.Size.X) + (count - 1) * pixelsMargin;
    
        float currentX = centerPoint.X - totalWidth / 2;

        for (int i = 0; i < count; i++)
        {
            Vector2 targetPosition = new Vector2(
                currentX, 
                centerPoint.Y
            );

            if (lerp > 0 && deltaTime > 0)
            {
                float t = 1.0f - MathF.Exp(-18f * deltaTime);
                nodesList[i].Position = Vector2.Lerp(nodesList[i].Position, targetPosition, t);
            }
            else nodesList[i].Position = targetPosition;
            
            currentX += nodesList[i].Size.X + pixelsMargin;
        }
    }
}