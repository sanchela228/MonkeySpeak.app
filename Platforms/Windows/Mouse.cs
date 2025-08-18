using System.Runtime.InteropServices;

namespace Platforms.Windows;

public class Mouse
{
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out Point lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }
}