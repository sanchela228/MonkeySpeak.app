using System.Runtime.InteropServices;
using System.Text;
using App.System.Calls.Media;
using App.System.Utils;
using Engine;


namespace App.Scenes;

public class Room : Scene
{
    public Room()
    {
        Task.Run(() => { Context.Instance.CallFacade.StartAudioProcess(); });
    }
    

    
    protected override void Update(float dt)
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