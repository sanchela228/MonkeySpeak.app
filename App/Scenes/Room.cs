using System.Runtime.InteropServices;
using App.System.Calls.Media;
using Concentus.Enums;
using Concentus.Structs;
using Engine;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using System.Collections.Generic;
using System.Net.Sockets;

namespace App.Scenes;

public class Room : Scene
{
    public Room()
    {
        Context.Instance.CallFacade.StartAudioProcess();
    }
    
    protected override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
    }

    protected override void Draw()
    {
        // throw new NotImplementedException();
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
}