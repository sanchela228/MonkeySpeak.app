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
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
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
    }
    

    
    protected override void Update(float dt)
    {
        if (Facade.MicrophoneEnabled)
        {
            ButtonMute.BackgroundColor = Color.Green;
        }
        else
        {
            ButtonMute.BackgroundColor = Color.Red;
        }
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