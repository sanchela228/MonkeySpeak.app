using System;
using App.System.Managers;

namespace App.System.Calls.Application.Controls;

public enum ControlCode : byte
{
    MuteState = 0x01,
    // Future: VolumeLevel = 0x02, HandRaise = 0x03, Typing = 0x04, ...
}

public class UdpControlService
{
    private UdpUnifiedManager _udp;
    private bool _attached;

    public event Action<ControlCode, ReadOnlyMemory<byte>>? OnControl;
    public event Action<bool>? OnRemoteMuteChanged;

    public void Attach(UdpUnifiedManager udp)
    {
        if (udp == null) throw new ArgumentNullException(nameof(udp));
        if (_attached && _udp == udp) return;
        Detach();
        _udp = udp;
        _udp.OnControlData += HandleControlData;
        _attached = true;
    }

    public void Detach()
    {
        try
        {
            if (_attached && _udp != null)
            {
                _udp.OnControlData -= HandleControlData;
            }
        }
        catch { }
        finally
        {
            _attached = false;
            _udp = null;
        }
    }

    // Generic send (payload as span)
    public void Send(ControlCode code, ReadOnlySpan<byte> payload)
    {
        try
        {
            if (_udp == null) return;
            var buf = new byte[1 + payload.Length];
            buf[0] = (byte)code;
            payload.CopyTo(buf.AsSpan(1));
            _ = _udp.SendControlAsync(buf);
        }
        catch { }
    }

    // Convenience for one-byte payload
    public void Send(ControlCode code, byte value)
    {
        Span<byte> payload = stackalloc byte[1];
        payload[0] = value;
        Send(code, payload);
    }

    // Backward compatibility helper for current code paths
    public void SendMuteState(bool enabled)
    {
        Send(ControlCode.MuteState, (byte)(enabled ? 1 : 0));
    }

    private void HandleControlData(byte[] data)
    {
        try
        {
            if (data == null || data.Length < 1) return;
            var code = (ControlCode)data[0];
            var payload = data.AsMemory(1);

            // Raise generic event first
            OnControl?.Invoke(code, payload);

            // Route known controls
            switch (code)
            {
                case ControlCode.MuteState:
                    if (payload.Length >= 1)
                    {
                        bool remoteMicEnabled = payload.Span[0] == 1;
                        OnRemoteMuteChanged?.Invoke(!remoteMicEnabled);
                    }
                    break;
            }
        }
        catch { }
    }
}
