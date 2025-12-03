using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using App.Scenes.NoAuthCall;
using App.System.Calls.Application.Facade;
using App.System.Calls.Media;
using App.System.Services;
using App.System.Utils;
using Engine;
using Engine.Helpers;
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

    // private Avatar _avatar = new Avatar(new Vector2(Raylib.GetScreenWidth() / 2, 260));
    
    public Room()
    {
        Facade = Context.CallFacade;
        
        Facade.OnRemoteMuteChanged += (test) =>
        {
            // _avatar.IsMuted = test;
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

        Facade.OnRemoteMuteChangedByInterlocutor += (id, isMuted) =>
        {
            _muteById[id] = isMuted;
        };

    }
    
    private readonly Dictionary<string, bool> _muteById = new();
    
    private void HandleCallEnded()
    {
        Engine.Managers.Scenes.PushScene(new StartUp(false));
    }
    
    protected override void Update(float dt)
    {

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            Facade.ToggleDemoDenoise();
        }
        
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
        var session = Facade.CurrentSession();
        if (session != null)
        {
            var pos = new Vector2(50, 50);
            var audioLevels = Facade.GetAudioLevels();
            
            foreach (var il in session.Interlocutors)
            {
                var muted = _muteById.TryGetValue(il.Id, out var m) && m;
                var audioLevel = audioLevels.TryGetValue(il.Id, out var level) ? level : 0f;
                
                // Рисуем текст
                Text.DrawPro(
                    _mainFontStartup, 
                    $"Id: {il.Id.Substring(0, 8)} | Muted: {muted} | Audio: {audioLevel:F2}", 
                    new Vector2(Raylib.GetRenderWidth() / 2, (int)pos.Y),
                    color: Color.White
                );
                
                // Рисуем визуальный индикатор уровня звука
                var barWidth = 200;
                var barHeight = 10;
                var barX = (int)pos.X + 500;
                var barY = (int)pos.Y + 5;
                
                // Фон полоски
                Raylib.DrawRectangle(barX, barY, barWidth, barHeight, new Color(50, 50, 50, 255));
                
                // Уровень звука (зелёный если есть звук, серый если нет)
                var fillWidth = (int)(barWidth * audioLevel);
                var barColor = audioLevel > 0.05f ? new Color(0, 255, 0, 255) : new Color(100, 100, 100, 255);
                if (fillWidth > 0)
                {
                    Raylib.DrawRectangle(barX, barY, fillWidth, barHeight, barColor);
                }
                
                // Рамка
                Raylib.DrawRectangleLines(barX, barY, barWidth, barHeight, Color.White);
                
                pos.Y += 25;
            }
        }
        
        
        // throw new NotImplementedException();
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
}
