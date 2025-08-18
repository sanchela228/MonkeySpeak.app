using System.Numerics;
using Engine;
using Raylib_cs;

namespace Interface;

public class Loader : Node
{
    private const int DotCount = 3;
    private const float DotRadius = 7f;
    private const float JumpHeight = 5f;
    private const float AnimationSpeed = 5f;
    
    private readonly Vector2 _startPosition;
    private readonly float[] _dotTimers = new float[DotCount];
    private readonly Color[] _dotColors = new Color[DotCount]
    {
        Color.Gray,
        Color.Gray,
        Color.Gray
    };

    public Loader(Vector2 position)
    {
        _startPosition = position;
    }

    public override void Update(float deltaTime)
    {
        for (int i = 0; i < DotCount; i++)
        {
            _dotTimers[i] += deltaTime * AnimationSpeed;
            
            if (_dotTimers[i] > MathF.PI * 2)
            {
                _dotTimers[i] -= MathF.PI * 2;
            }
        }
    }

    public override void Draw()
    {
        const float spacing = DotRadius * 4;
        
        for (int i = 0; i < DotCount; i++)
        {
            float x = _startPosition.X + (i * spacing);
            
            float yOffset = MathF.Sin(_dotTimers[i] + (i * 1.5f)) * JumpHeight;
            float y = _startPosition.Y - yOffset;
            
            Raylib.DrawCircleV(new Vector2(x, y), DotRadius, _dotColors[i]);
        }
    }

    public override void Dispose()
    {
    }
}