using System.Collections.Generic;

namespace Interface.Room;

public sealed class MicrophoneSelectPopup : DeviceSelectPopup
{
    public MicrophoneSelectPopup(List<string> devices, int selectedIndex = 0, int initialVolumePercent = 100)
        : base("Capture devices", devices, selectedIndex, initialVolumePercent)
    {
    }
}
