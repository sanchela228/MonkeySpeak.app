using System.Collections.Generic;

namespace Interface.Room;

public sealed class PlaybackSelectPopup : DeviceSelectPopup
{
    public PlaybackSelectPopup(List<string> devices, int selectedIndex = 0, int initialVolumePercent = 100)
        : base("Playback devices", devices, selectedIndex, initialVolumePercent)
    {
    }
}
