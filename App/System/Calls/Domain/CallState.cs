namespace App.System.Calls.Domain;

public enum CallState
{
    Idle = 0,
    Negotiating = 1,
    HolePunching = 2,
    Connected = 3,
    Failed = 4,
    Closed = 5
}
