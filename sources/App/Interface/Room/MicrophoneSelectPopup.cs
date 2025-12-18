using System;
using System.Collections.Generic;
using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace Interface.Room;

public sealed class MicrophoneSelectPopup : Node
{
    private readonly List<string> _devices;
    private readonly FontFamily _fontTitle;
    private readonly FontFamily _fontItem;

    private int _selectedIndex;

    private int _volumePercent;

    public int VolumePercent => _volumePercent;

    public event Action<int>? OnSelected;
    public event Action? OnCloseRequested;
    public event Action<int>? OnVolumeChanged;

    public MicrophoneSelectPopup(List<string> devices, int selectedIndex = 0, int initialVolumePercent = 100)
    {
        PointRendering = Engine.PointRendering.LeftTop;
        Order = 10000;

        _devices = devices ?? new List<string>();
        _selectedIndex = Math.Clamp(selectedIndex, 0, Math.Max(0, _devices.Count - 1));
        _volumePercent = Math.Clamp(initialVolumePercent, 0, 200);

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

        Size = new Vector2(360, 52 + _devices.Count * 34);
    }

    public override void Update(float deltaTime)
    {
        var mousePos = Raylib.GetMousePosition();

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            _volumePercent = Math.Clamp(_volumePercent + 10, 0, 200);
            OnVolumeChanged?.Invoke(_volumePercent);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            _volumePercent = Math.Clamp(_volumePercent - 10, 0, 200);
            OnVolumeChanged?.Invoke(_volumePercent);
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            if (!Raylib.CheckCollisionPointRec(mousePos, Bounds))
            {
                OnCloseRequested?.Invoke();
                return;
            }
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

        Raylib.DrawRectangleRounded(rect, 0.15f, 12, new Color(20, 20, 20, 235));
        Raylib.DrawRectangleRoundedLinesEx(rect, 0.15f, 12, 1f, new Color(60, 60, 60, 255));

        Text.DrawPro(_fontTitle, $"Capture devices ({_volumePercent}%)", new Vector2(rect.X + 16, rect.Y + 12), origin: new Vector2(0, 0));

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
            Text.DrawPro(_fontItem, _devices[i], labelPos, origin: new Vector2(0, _fontItem.Size / 2f));

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
        float y = rect.Y + 40 + index * 34;
        return new Rectangle(rect.X + 12, y, rect.Width - 24, 32);
    }

    public override void Dispose()
    {
    }
}
