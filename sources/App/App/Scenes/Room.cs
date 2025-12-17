using System.Collections.ObjectModel;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using App.Scenes.NoAuthCall;
using App.System.Calls.Application.Facade;
using App.System.Calls.Domain;
using App.System.Calls.Media;
using App.System.Services;
using App.System.Utils;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Interface;
using Interface.Buttons;
using Interface.Room;
using Raylib_cs;
using PointRendering = Engine.PointRendering;
using Rectangle = Raylib_cs.Rectangle;


namespace App.Scenes;

public class Room : Scene
{
    private CallFacade Facade;
    private readonly FontFamily _mainFontStartup;
    
    public Room()
    {
        Facade = Context.CallFacade;
        Facade.OnCallEnded += HandleCallEnded;
        
        Task.Run(() => { Facade.StartAudioProcess(); });
        
        _mainFontStartup = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        var volumeOnTextureIcon = Resources.Texture("Images\\Icons\\VolumeOn_Black.png");
        var volumeMuteTextureIcon = Resources.Texture("Images\\Icons\\VolumeMute_White.png");
        var texTest0 = Resources.Texture("Images\\Icons\\MicrophoneDefault_White.png");
        var texTest0_b = Resources.Texture("Images\\Icons\\MicrophoneDefault_Black.png");
        var texTest1 = Resources.Texture("Images\\Icons\\MicrophoneMuted_White.png");
        var texTest2 = Resources.Texture("Images\\Icons\\CallHangup_White.png");

        var microControl = new RoomControlIcon(texTest0_b, new Vector2(28, 28), texTest1, true)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        var volumeControl = new RoomControlIcon(volumeOnTextureIcon, new Vector2(28, 28), volumeMuteTextureIcon, true)
        {
            BackgroundColor = Color.White,
            IsActive = true
        };
        
        volumeControl.OnRelease += (node) =>
        {
            // if (Facade.MicrophoneEnabled)
            // {
            //     Facade.MicrophoneEnabled = false;
            //     volumeControl.IsActive = false;
            // }
            
            Facade.ToggleVolume();
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
            volumeControl,
            microControl,
            hangupControl
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

        Facade.OnRemoteMuteChangedByInterlocutor += HandleRemoteMuteChangedByInterlocutor;

        InterlocutorsGrid = new InterlocutorsGrid()
        {
            Interlocutors = Facade?.CurrentSession().Interlocutors
        };
        
        InterlocutorsGrid.MuteDictionary = _muteById;
        
        AddNode(InterlocutorsGrid);

        InterlocutorsGrid.Size = new Vector2(Raylib.GetRenderWidth() - 100, Raylib.GetRenderHeight() - 200);
        InterlocutorsGrid.Position = new Vector2(50, 50);
        InterlocutorsGrid.PointRendering = PointRendering.LeftTop;
    }
    
    private InterlocutorsGrid InterlocutorsGrid;
    
    private readonly Dictionary<string, bool> _muteById = new();
    
    private void HandleRemoteMuteChangedByInterlocutor(string id, bool isMuted)
    {
        _muteById[id] = isMuted;
    }
    
    private void HandleCallEnded()
    {
        Engine.Managers.Scenes.PushScene(new StartUp(false));
    }
    
    protected override void Update(float dt)
    {

    }

    protected override void Draw()
    {
    }

    protected override void Dispose()
    {
        Logger.Write("[Dispose] Room.cs dispose");
        
        if (Facade != null)
        {
            Facade.OnCallEnded -= HandleCallEnded;
            Facade.OnRemoteMuteChangedByInterlocutor -= HandleRemoteMuteChangedByInterlocutor;
        }
    }
}
