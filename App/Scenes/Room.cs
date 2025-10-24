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

    public Button ButtonMute;
    
    public Room()
    {
        Facade = Context.Instance.CallFacade;
        
        Task.Run(() => { Facade.StartAudioProcess(); });
        
        _mainFontStartup = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        ButtonMute = new Classic(_mainFontStartup)
        {
            Position = new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2 + 160),
            Padding = new Vector2(130, 18),
            Text = Language.Get("micro"),
            IsActive = true,
            HoverBackgroundColor = Facade.MicrophoneEnabled ? Color.Green : Color.Red,
            
        };
        
        ButtonMute.OnClick += (sender) =>
        {
            Facade.MicrophoneEnabled = !Facade.MicrophoneEnabled;
        };
        
        AddNode(ButtonMute);
        
        var texTest0 = Resources.Texture("Images/Icons/MicrophoneDefault_White.png");
        var texTest0_b = Resources.Texture("Images/Icons/MicrophoneDefault_Black.png");
        var texTest1 = Resources.Texture("Images/Icons/MicrophoneMuted_White.png");
        var texTest2 = Resources.Texture("Images/Icons/CallHangup_White.png");
        
        AddNode(new Avatar(new Vector2(Raylib.GetScreenWidth() / 2, 260)));
        
        
        var microControl = new RoomControlIcon(texTest0_b, new Vector2(28, 28), texTest1)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        microControl.OnRelease += (node) =>
        {
            Facade.MicrophoneEnabled = !Facade.MicrophoneEnabled;
        };
        
        
        var testList = new List<RoomControlIcon>()
        {
            new RoomControlIcon(texTest0, new Vector2(28, 28)),
            new RoomControlIcon(texTest1, new Vector2(28, 28)),
            new RoomControlIcon(texTest0, new Vector2(28, 28)),
            microControl,
            new RoomControlIcon(texTest2, new Vector2(28, 28)){BackgroundColor = new Color(220, 80, 80)},
        };
        
        AddNodes(testList);
        
        float size = 28 + testList[0].Padding.X + testList[0].CornerWidth;
        
        Render.PlaceInLine(testList, (int) size,
            new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() - 100),
            16
        );



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