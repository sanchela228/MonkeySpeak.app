using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using App.Scenes.NoAuthCall;
using App.System.Calls.Application.Facade;
using App.System.Calls.Media;
using App.System.Services;
using App.System.Utils;
using Engine;
using Engine.Managers;
using Graphics;
using Interface;
using Interface.Buttons;
using Raylib_cs;


namespace App.Scenes;

public class Room : Scene
{
    private CallFacade Facade;
    private readonly FontFamily _mainFontStartup;

    private Avatar _avatar = new Avatar(new Vector2(Raylib.GetScreenWidth() / 2, 260));
    
    public Room()
    {
        Facade = Context.CallFacade;
        
        Facade.OnRemoteMuteChanged += (test) =>
        {
            _avatar.IsMuted = test;
        };
        
        Facade.OnCallEnded += HandleCallEnded;
        
        Task.Run(() => { Facade.StartAudioProcess(); });
        
        _mainFontStartup = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        
        var texTest0 = Resources.Texture("Images\\Icons\\MicrophoneDefault_White.png");
        var texTest0_b = Resources.Texture("Images\\Icons\\MicrophoneDefault_Black.png");
        var texTest1 = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
        var texTest2 = Resources.Texture("Images\\Icons\\CallHangup_White.png");
        
        AddNode(_avatar);
        
        
        var microControl = new RoomControlIcon(texTest0_b, new Vector2(28, 28), texTest1)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        var hangupControl = new RoomControlIcon(texTest2, new Vector2(28, 28), texTest1)
        {
            BackgroundColor = new Color(220, 80, 80),
        };
        
        hangupControl.OnRelease += async (node) =>
        {
            Facade.Hangup();
            HandleCallEnded();
        };
        
        
        var testList = new List<RoomControlIcon>()
        {
            microControl,
            hangupControl,
        };
        
        microControl.OnRelease += (node) =>
        {
            Facade.MicrophoneEnabled = !Facade.MicrophoneEnabled;
        };
        
        AddNodes(testList);
        
        float size = 28 + testList[0].Padding.X + testList[0].CornerWidth;
        
        Render.PlaceInLine(testList, (int) size,
            new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() - 100),
            16
        );



    }
    
    private void HandleCallEnded()
    {
        Engine.Managers.Scenes.Instance.PushScene(new StartUp(false));
    }
    
    protected override void Update(float dt)
    {
        // if (Facade.MicrophoneEnabled)
        // {
        //     ButtonMute.BackgroundColor = Color.Green;
        // }
        // else
        // {
        //     ButtonMute.BackgroundColor = Color.Red;
        // }
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