using System;
using System.Numerics;
using Engine;
using Raylib_cs;

namespace Interface.Room;

public class SelfAudioWaveIndicator : Node
{
    public float RawLevel { get; set; } = 0f;

    public int OvalCount { get; set; } = 5;
    public float OvalWidth { get; set; } = 8f;
    public float OvalHeight { get; set; } = 8f;
    public float OvalSpacing { get; set; } = 4f;

    public float Sensitivity { get; set; } = 6f;
    public float CompressionStrength { get; set; } = 2.0f;

    public float MaxAmplitude { get; set; } = 12f;
    public float SilenceThreshold { get; set; } = 0.02f;

    public float SmoothingSpeed { get; set; } = 12f;
    public float BaseSpeed { get; set; } = 6f;
    public float SpeedByLevel { get; set; } = 5f;

    public byte MinAlpha { get; set; } = 40;
    public byte MaxAlpha { get; set; } = 255;

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

        float totalWidth = OvalCount * OvalWidth + (OvalCount - 1) * OvalSpacing;
        Size = new Vector2(totalWidth, MaxAmplitude * 2f + OvalHeight);
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

        float totalWidth = OvalCount * OvalWidth + (OvalCount - 1) * OvalSpacing;
        float startX = baseX - totalWidth / 2f;

        int center = OvalCount / 2;

        for (int i = 0; i < OvalCount; i++)
        {
            float distFromCenter = MathF.Abs(i - center) / (float)Math.Max(1, center);
            float envelope = 1f - distFromCenter * 0.6f;

            float phaseOffset = i * 0.5f;
            float oscillation = MathF.Sin(_phase + phaseOffset);

            float baseAmplitude = level > SilenceThreshold ? MaxAmplitude * level * envelope : 0f;
            float animatedScale = 0.5f + 0.5f * oscillation;
            float heightIncrease = baseAmplitude * animatedScale;
            float currentHeight = OvalHeight + heightIncrease;

            float cx = startX + i * (OvalWidth + OvalSpacing) + OvalWidth / 2f;
            float cy = baseY;

            Raylib.DrawEllipse((int)cx, (int)cy, OvalWidth / 2f, currentHeight / 2f, c);
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
