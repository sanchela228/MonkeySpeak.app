using System;
using System.Collections.Generic;
using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Interface.Room;

public class DeviceSelectPopup : Node
{
    private readonly string _title;
    private readonly List<string> _devices;
    private readonly FontFamily _fontTitle;
    private readonly FontFamily _fontItem;

    private int _selectedIndex;

    private int _volumePercent;

    private bool _isDraggingSlider;

    private const int VolumeMin = 0;
    private const int VolumeMax = 200;
    private const int VolumeStep = 5;

    private const float HeaderHeight = 40f;
    private const float SliderBlockHeight = 34f;
    private const float RowHeight = 34f;

    public int VolumePercent => _volumePercent;

    public event Action<int>? OnSelected;
    public event Action? OnCloseRequested;
    public event Action<int>? OnVolumeChanged;

    public DeviceSelectPopup(string title, List<string> devices, int selectedIndex = 0, int initialVolumePercent = 100)
    {
        PointRendering = Engine.PointRendering.LeftTop;
        Order = 10000;

        _title = title;
        _devices = devices ?? new List<string>();
        _selectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, _devices.Count - 1));
        _volumePercent = QuantizeVolume(initialVolumePercent);

        _fontTitle = new FontFamily
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 20),
            Size = 20,
            Spacing = 1,
            Color = Color.White,
            Rotation = 0
        };

        _fontItem = new FontFamily
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 18),
            Size = 18,
            Spacing = 1,
            Color = Color.White,
            Rotation = 0
        };

        Size = new Vector2(360, 52 + SliderBlockHeight + _devices.Count * RowHeight);
    }

    public override void Update(float deltaTime)
    {
        var mousePos = Raylib.GetMousePosition();

        var sliderRect = GetSliderRect();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (!Raylib.CheckCollisionPointRec(mousePos, Bounds))
            {
                OnCloseRequested?.Invoke();
                Dispose();
                return;
            }
 
            if (Raylib.CheckCollisionPointRec(mousePos, sliderRect))
            {
                _isDraggingSlider = true;
                SetVolumeByPointer(mousePos, sliderRect, emit: true);
                return;
            }
        }

        if (_isDraggingSlider)
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                SetVolumeByPointer(mousePos, sliderRect, emit: true);
                return;
            }

            _isDraggingSlider = false;
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            for (int i = 0; i < _devices.Count; i++)
            {
                var row = GetRowRect(i);
                if (Raylib.CheckCollisionPointRec(mousePos, row))
                {
                    _selectedIndex = i;
                    OnSelected?.Invoke(i);
                    return;
                }
            }
        }
    }

    public override void Draw()
    {
        var rect = Bounds;

        Raylib.DrawRectangleRounded(rect, 0.15f, 12, new Color(20, 20, 20, 255));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.15f, 12, 1f, new Color(60, 60, 60, 255));

        Text.DrawPro(_fontTitle, $"{_title} ({_volumePercent}%)", new Vector2(rect.X + 16, rect.Y + 12), origin: new Vector2(0, 0));

        DrawSlider();

        for (int i = 0; i < _devices.Count; i++)
        {
            var row = GetRowRect(i);
            var isSelected = i == _selectedIndex;
            var rowBg = isSelected ? new Color(45, 45, 45, 255) : new Color(0, 0, 0, 0);

            if (rowBg.A > 0)
            {
                Raylib.DrawRectangleRec(row, rowBg);
            }

            var labelPos = new Vector2(row.X + 34, row.Y + row.Height / 2f);
            var deviceName = _devices[i];
            var displayName = deviceName.Length > 29 ? deviceName.Substring(0, 29) + "..." : deviceName;
            Text.DrawPro(_fontItem, displayName + "...", labelPos, origin: new Vector2(0, _fontItem.Size / 2f));

            var circleCenter = new Vector2(row.X + 16, row.Y + row.Height / 2f);
            Raylib.DrawCircle((int)circleCenter.X, (int)circleCenter.Y, 6f, new Color(80, 80, 80, 255));
            if (isSelected)
            {
                Raylib.DrawCircle((int)circleCenter.X, (int)circleCenter.Y, 3.5f, Color.White);
            }
        }
    }

    private Rectangle GetRowRect(int index)
    {
        var rect = Bounds;
        float y = rect.Y + HeaderHeight + SliderBlockHeight + index * RowHeight;
        return new Rectangle(rect.X + 12, y, rect.Width - 24, 32);
    }

    private Rectangle GetSliderRect()
    {
        var rect = Bounds;
        float x = rect.X + 16;
        float y = rect.Y + HeaderHeight + 6f;
        float w = rect.Width - 32;
        float h = 10f;
        return new Rectangle(x, y, w, h);
    }

    private static int QuantizeVolume(int percent)
    {
        percent = Math.Clamp(percent, VolumeMin, VolumeMax);
        int q = (int)MathF.Round(percent / (float)VolumeStep) * VolumeStep;
        return Math.Clamp(q, VolumeMin, VolumeMax);
    }

    private void SetVolumeByPointer(Vector2 mousePos, Rectangle sliderRect, bool emit)
    {
        float t = (mousePos.X - sliderRect.X) / sliderRect.Width;
        t = Math.Clamp(t, 0f, 1f);

        int raw = (int)MathF.Round(VolumeMin + t * (VolumeMax - VolumeMin));
        _volumePercent = QuantizeVolume(raw);

        if (emit)
            OnVolumeChanged?.Invoke(_volumePercent);
    }

    private void DrawSlider()
    {
        var sliderRect = GetSliderRect();

        Raylib.DrawRectangleRounded(sliderRect, 1.35f, 12, new Color(35, 35, 35, 255));
        Raylib.DrawRectangleRoundedLinesEx(sliderRect, 1.35f, 12, 1f, new Color(70, 70, 70, 255));

        float t = (_volumePercent - VolumeMin) / (float)(VolumeMax - VolumeMin);
        t = Math.Clamp(t, 0f, 1f);
        float knobX = sliderRect.X + t * sliderRect.Width;
        float knobY = sliderRect.Y + sliderRect.Height / 2f;

        var fillRect = new Rectangle(sliderRect.X, sliderRect.Y, (knobX - sliderRect.X), sliderRect.Height);
        if (fillRect.Width > 0)
            Raylib.DrawRectangleRounded(fillRect, 1.35f, 12, new Color(75, 185, 75, 255));

        Raylib.DrawCircle((int)knobX, (int)knobY, 9f, Color.White);
    }

    public override void Dispose()
    {
        OnCloseRequested?.Invoke();
    }
}
