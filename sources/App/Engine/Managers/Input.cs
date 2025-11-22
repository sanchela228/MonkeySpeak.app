using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;

namespace Engine.Managers;

public static class Input
{
    private static readonly HashSet<KeyboardKey> _down = new();
    private static readonly HashSet<KeyboardKey> _pressed = new();
    private static readonly HashSet<KeyboardKey> _released = new();
    private static HashSet<KeyboardKey> _prevDown = new();

    private static int _nextChordId = 1;
    private static readonly Dictionary<int, Chord> _chords = new();

    private static int _nextSeqId = 1;
    private static readonly Dictionary<int, Sequence> _sequences = new();

    private static float _time;

    public static void Update(float deltaTime)
    {
        _time += deltaTime;
        
        _pressed.Clear();
        _released.Clear();
        ScanKeyboardStates();

        foreach (var kv in _chords)
        {
            var chord = kv.Value;
            bool allDown = chord.Keys.All(_down.Contains);
            bool anyJustPressed = chord.Keys.Any(_pressed.Contains);

            if (allDown)
            {
                if (!chord.Active)
                {
                    if (chord.TriggerOnPress ? anyJustPressed : true)
                    {
                        chord.Active = true;
                        SafeInvoke(chord.Callback);
                    }
                }
            }
            else
            {
                chord.Active = false;
            }
        }

        if (_pressed.Count > 0)
        {
            foreach (var kv in _sequences)
            {
                var seq = kv.Value;

                // таймаут
                if (seq.Step > 0 && _time - seq.LastStepTime > seq.Timeout)
                {
                    seq.Step = 0;
                }

                var expected = seq.Keys[seq.Step];
                if (_pressed.Contains(expected))
                {
                    seq.Step++;
                    seq.LastStepTime = _time;

                    if (seq.Step >= seq.Keys.Count)
                    {
                        seq.Step = 0;
                        SafeInvoke(seq.Callback);
                    }
                }
                else if (_pressed.Any(k => seq.Keys.Contains(k)))
                {
                    var first = seq.Keys[0];
                    seq.Step = _pressed.Contains(first) ? 1 : 0;
                    seq.LastStepTime = _time;
                }
            }
        }
        
        _prevDown = new HashSet<KeyboardKey>(_down);
    }

    public static bool IsDown(KeyboardKey key) => _down.Contains(key);
    public static bool IsPressed(KeyboardKey key) => _pressed.Contains(key);
    public static bool IsReleased(KeyboardKey key) => _released.Contains(key);

    public static bool AnyDown(params KeyboardKey[] keys) => keys.Any(_down.Contains);
    public static bool AnyPressed(params KeyboardKey[] keys) => keys.Any(_pressed.Contains);
    public static bool AllDown(params KeyboardKey[] keys) => keys.All(_down.Contains);

    public static int RegisterChord(IEnumerable<KeyboardKey> keys, Action callback, bool triggerOnPress = true)
    {
        var chord = new Chord
        {
            Id = _nextChordId++,
            Keys = keys.Distinct().ToHashSet(),
            Callback = callback,
            TriggerOnPress = triggerOnPress,
            Active = false
        };
        _chords[chord.Id] = chord;
        return chord.Id;
    }

    public static void UnregisterChord(int id) => _chords.Remove(id);

    public static int RegisterSequence(IEnumerable<KeyboardKey> keys, float timeoutSeconds, Action callback)
    {
        var list = keys.Where(k => k != KeyboardKey.Null).ToList();
        if (list.Count == 0) throw new ArgumentException("Sequence must contain at least one key");

        var seq = new Sequence
        {
            Id = _nextSeqId++,
            Keys = list,
            Timeout = Math.Max(0.01f, timeoutSeconds),
            Callback = callback,
            Step = 0,
            LastStepTime = 0f
        };
        _sequences[seq.Id] = seq;
        return seq.Id;
    }

    public static void UnregisterSequence(int id) => _sequences.Remove(id);

    private static void ScanKeyboardStates()
    {
        _down.Clear();

        foreach (KeyboardKey key in Enum.GetValues(typeof(KeyboardKey)))
        {
            if (key == KeyboardKey.Null) continue;
            
            if (Raylib.IsKeyDown(key))
                _down.Add(key);
        }

        foreach (var key in _down)
            if (!_prevDown.Contains(key))
                _pressed.Add(key);

        foreach (var key in _prevDown)
            if (!_down.Contains(key))
                _released.Add(key);
    }

    private static void SafeInvoke(Action? callback)
    {
        try { callback?.Invoke(); }
        catch {  }
    }

    private class Chord
    {
        public int Id { get; set; }
        public HashSet<KeyboardKey> Keys { get; set; } = new();
        public Action? Callback { get; set; }
        public bool TriggerOnPress { get; set; }
        public bool Active { get; set; }
    }

    private class Sequence
    {
        public int Id { get; set; }
        public List<KeyboardKey> Keys { get; set; } = new();
        public float Timeout { get; set; }
        public Action? Callback { get; set; }
        public int Step { get; set; }
        public float LastStepTime { get; set; }
    }
}
