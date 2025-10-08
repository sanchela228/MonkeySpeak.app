using System.Runtime.InteropServices;
using System.Text;
using App.System.Calls.Media;
using App.System.Utils;
using Concentus.Enums;
using Concentus.Structs;
using Engine;
using Raylib_cs;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace App.Scenes;

public class Room : Scene
{
    public Room()
    {
        Task.Run(() => { Context.Instance.CallFacade.StartAudioProcess(); });
    }
    

    
    protected override unsafe void Update(float dt)
    {
        
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