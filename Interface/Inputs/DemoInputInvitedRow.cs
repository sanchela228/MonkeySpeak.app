using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Engine;
using Raylib_cs;

namespace Interface.Inputs;

public class DemoInputInvitedRow : Node
{
    private readonly DemoInputInvited[] _items = new DemoInputInvited[6];
    private float _spacing = 22f;
    private Vector2 _initialPosition;
    private CancellationTokenSource? _shakeCts;
    public bool IsLocked { get; private set; }

    public DemoInputInvitedRow()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            var item = new DemoInputInvited();
            item.PointRendering = PointRendering.Center;
            _items[i] = item;
            AddChild(item);
        }

        PointRendering = PointRendering.Center;
        Layout(_spacing);
        _initialPosition = Position;
    }

    public override void Update(float deltaTime)
    {
    }

    public override void Draw()
    {
    }

    public override void Dispose()
    {
        _shakeCts?.Cancel();
        _shakeCts?.Dispose();
        _shakeCts = null;
    }

    public IReadOnlyList<DemoInputInvited> Items => _items;

    public void Layout(float spacing)
    {
        _spacing = spacing;

        float itemWidth = _items[0].Size.X;
        int n = _items.Length;
        float total = n * itemWidth + (n - 1) * spacing;
        float startX = -total / 2f + itemWidth / 2f;

        for (int i = 0; i < n; i++)
        {
            float x = startX + i * (itemWidth + spacing);
            _items[i].Position = new Vector2(x, 0f);
        }
    }

    public void SetSymbol(int index, char? c)
    {
        if (index < 0 || index >= _items.Length) return;
        _items[index].Symbol = c;
    }

    public void ClearAllSymbols()
    {
        for (int i = 0; i < _items.Length; i++)
            _items[i].Symbol = null;
    }

    public void ResetStates()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i].IsFailed = false;
            _items[i].State = DemoInputInvited.BorderState.None;
        }
    }

    public void MarkSuccess()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i].IsFailed = false;
            _items[i].State = DemoInputInvited.BorderState.Success;
        }
        LockInput();
    }

    public async Task MarkErrorAsync(float duration = 0.35f, float amplitude = 8f, int oscillations = 3)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            _items[i].IsFailed = true;
            _items[i].State = DemoInputInvited.BorderState.Error;
        }

        _shakeCts?.Cancel();
        _shakeCts?.Dispose();
        _shakeCts = new CancellationTokenSource();
        var token = _shakeCts.Token;

        _initialPosition = new Vector2(Position.X, Position.Y);

        try
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) break;
                
                float t = elapsed / duration;
                
                float offset = (float)(Math.Sin(t * oscillations * Math.PI * 2) * (1.0 - t) * amplitude);
                Position = new Vector2(_initialPosition.X + offset, _initialPosition.Y);
                await Task.Delay(16, token); // ~60 FPS
                elapsed += 0.016f;
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            Position = _initialPosition;
        }
    }

    public void LockInput() => IsLocked = true;
    public void UnlockInput() => IsLocked = false;
}
