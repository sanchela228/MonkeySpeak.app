using System;
using System.Numerics;
using Engine;
using Raylib_cs;

namespace Interface.Room;

public class SelfAudioWaveIndicator : Node
{
    public float RawLevel { get; set; } = 0f;

    public float Length { get; set; } = 50f;
    public float LeftFlat { get; set; } = 12f;
    public float RightFlat { get; set; } = 12f;

    public float Sensitivity { get; set; } = 6f;
    public float CompressionStrength { get; set; } = 2.0f;

    public float MaxAmplitude { get; set; } = 16f;
    public float SilenceThreshold { get; set; } = 0.02f;

    public float SmoothingSpeed { get; set; } = 12f;
    public float BaseSpeed { get; set; } = 6f;
    public float SpeedByLevel { get; set; } = 5f;

    public byte MinAlpha { get; set; } = 40;
    public byte MaxAlpha { get; set; } = 255;

    public float LineThickness { get; set; } = 2f;

    public Color Color { get; set; } = new Color(80, 200, 80, 255);

    private float _levelSmoothed = 0f;
    private float _phase = 0f;

    public SelfAudioWaveIndicator()
    {
        Overlap = OverlapsMode.None;
        PointRendering = PointRendering.LeftTop;
    }

    public override void Update(float deltaTime)
    {
        float raw = Math.Clamp(RawLevel, 0f, 1f);
        float mapped = MapLevel(raw);

        float alpha = 1f - MathF.Exp(-MathF.Max(0f, SmoothingSpeed) * MathF.Max(0f, deltaTime));
        _levelSmoothed = _levelSmoothed + (mapped - _levelSmoothed) * alpha;

        _phase += MathF.Max(0f, deltaTime) * (BaseSpeed + SpeedByLevel * _levelSmoothed);

        Size = new Vector2(Length, MaxAmplitude * 2f + 2f);
    }

    public override void Draw()
    {
        float level = Math.Clamp(_levelSmoothed, 0f, 1f);

        byte a;
        if (MaxAlpha >= MinAlpha)
            a = (byte)Math.Clamp(MinAlpha + (MaxAlpha - MinAlpha) * level, 0f, 255f);
        else
            a = (byte)Math.Clamp(MaxAlpha + (MinAlpha - MaxAlpha) * (1f - level), 0f, 255f);

        var c = new Color(Color.R, Color.G, Color.B, a);

        float baseX = Position.X;
        float baseY = Position.Y;

        void DrawSegment(Vector2 a, Vector2 b)
        {
            if (LineThickness > 1.0f)
                Raylib.DrawLineEx(a, b, LineThickness, c);
            else
                Raylib.DrawLineV(a, b, c);
        }

        if (level <= SilenceThreshold)
        {
            DrawSegment(new Vector2(baseX, baseY), new Vector2(baseX + Length, baseY));
            return;
        }

        float leftFlat = MathF.Max(0f, LeftFlat);
        float rightFlat = MathF.Max(0f, RightFlat);
        float waveLen = MathF.Max(0f, Length - leftFlat - rightFlat);

        float amp = MathF.Min(MaxAmplitude, MaxAmplitude * level);

        DrawSegment(new Vector2(baseX, baseY), new Vector2(baseX + leftFlat, baseY));
        DrawSegment(new Vector2(baseX + leftFlat + waveLen, baseY), new Vector2(baseX + Length, baseY));

        int segments = Math.Max(6, (int)MathF.Round(waveLen));
        float step = segments > 0 ? waveLen / segments : waveLen;

        float waveStartX = baseX + leftFlat;
        var prev = new Vector2(waveStartX, baseY);
        for (int i = 1; i <= segments; i++)
        {
            float x = waveStartX + i * step;
            float t = i / (float)segments;

            float env = MathF.Sin(MathF.PI * t);
            env *= env;

            float y = baseY + MathF.Sin(_phase + t * 8f) * amp * env;
            var cur = new Vector2(x, y);
            DrawSegment(prev, cur);
            prev = cur;
        }
    }

    public override void Dispose()
    {
    }

    private float MapLevel(float raw)
    {
        float amplified = 1f - MathF.Exp(-MathF.Max(0f, Sensitivity) * raw);
        float compressed = MathF.Tanh(amplified * MathF.Max(0f, CompressionStrength));
        return Math.Clamp(compressed, 0f, 1f);
    }
}
