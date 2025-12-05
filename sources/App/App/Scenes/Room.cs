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
        
        var resizeLeft = new RoomControlIcon(_mainFontStartup, "+80")
        {
            BackgroundColor = new Color(40, 40, 40),
        };

        resizeLeft.OnRelease += (node) =>
        {
            var r = InterlocutorsGrid.Collider;
            float dx = 80f;
            float minWidth = 160f;

            if (r.Width > minWidth)
            {
                var reduce = MathF.Min(dx, r.Width - minWidth);
                r.X += reduce;
                r.Width -= reduce;
                InterlocutorsGrid.Collider = r;
            }
        };
        
        var addOne = new RoomControlIcon(_mainFontStartup, "+1")
        {
            BackgroundColor = new Color(40, 40, 40),
        };
        addOne.OnRelease += (node) =>
        {
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);
            InterlocutorsGrid.Interlocutors.Add(
                new Interlocutor(id, new IPEndPoint(2134, 5000 + InterlocutorsGrid.Interlocutors.Count), CallState.Connected)
            );
        };

        var removeRandom = new RoomControlIcon(_mainFontStartup, "-1")
        {
            BackgroundColor = new Color(40, 40, 40),
        };
        removeRandom.OnRelease += (node) =>
        {
            if (InterlocutorsGrid.Interlocutors.Count <= 0) return;
            int idx = Raylib.GetRandomValue(0, InterlocutorsGrid.Interlocutors.Count - 1);
            var id = InterlocutorsGrid.Interlocutors[idx].Id;
            InterlocutorsGrid.Interlocutors.RemoveAt(idx);
        };

        var restoreLeft = new RoomControlIcon(_mainFontStartup, "-80")
        {
            BackgroundColor = new Color(40, 40, 40),
        };
        restoreLeft.OnRelease += (node) =>
        {
            var r = InterlocutorsGrid.Collider;
            float dx = 80f;
            float minLeft = 50f;

            var grow = MathF.Min(dx, MathF.Max(0f, r.X - minLeft));
            if (grow > 0f)
            {
                r.X -= grow;
                r.Width += grow;
                InterlocutorsGrid.Collider = r;
            }
        };
        
        var testList = new List<RoomControlIcon>()
        {
            microControl,
            hangupControl,
            resizeLeft,
            addOne,
            removeRandom,
            restoreLeft
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

        InterlocutorsGrid = new InterlocutorsGrid()
        {
            Interlocutors = Facade?.CurrentSession().Interlocutors
        };
        
        // InterlocutorsGrid.Interlocutors.Add(new Interlocutor("1", new IPEndPoint(2134, 5062), CallState.Connected));
        // InterlocutorsGrid.Interlocutors.Add(new Interlocutor("2", new IPEndPoint(2134, 5062), CallState.Connected));
        // InterlocutorsGrid.Interlocutors.Add(new Interlocutor("3", new IPEndPoint(2134, 5062), CallState.Connected));
        
        // InterlocutorsGrid.AddChildrens(new List<Avatar>()
        // {
        //     new Avatar(),
        //     new Avatar()
        // });
        
        AddNode(InterlocutorsGrid);

        InterlocutorsGrid.Size = new Vector2(Raylib.GetRenderWidth() - 100, Raylib.GetRenderHeight() - 200);
        InterlocutorsGrid.Position = new Vector2(50, 50);
        InterlocutorsGrid.PointRendering = PointRendering.LeftTop;
    }
    
    private InterlocutorsGrid InterlocutorsGrid;
    
    private readonly Dictionary<string, bool> _muteById = new();
    
    private void HandleCallEnded()
    {
        Engine.Managers.Scenes.PushScene(new StartUp(false));
    }
    
    protected override void Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            // Facade.ToggleDemoDenoise();
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
        
        
        
        
        // var session = Facade.CurrentSession();
        // if (session != null)
        // {
        //     var pos = new Vector2(50, 50);
        //     var audioLevels = Facade.GetAudioLevels();
        //     
        // foreach (var il in session.Interlocutors)
        //     {
        //         var muted = _muteById.TryGetValue(il.Id, out var m) && m;
        //         var audioLevel = audioLevels.TryGetValue(il.Id, out var level) ? level : 0f;
        //         
        //         Text.DrawPro(
        //             _mainFontStartup, 
        //             $"Id: {il.Id.Substring(0, 8)} | Muted: {muted} | Audio: {audioLevel:F2}", 
        //             new Vector2(Raylib.GetRenderWidth() / 2, (int)pos.Y),
        //             color: Color.White
        //         );
        //         
        //         
        //         var barWidth = 200;
        //         var barHeight = 10;
        //         var barX = (int)pos.X + 500;
        //         var barY = (int)pos.Y + 5;
        //         
        //        
        //         Raylib.DrawRectangle(barX, barY, barWidth, barHeight, new Color(50, 50, 50, 255));
        //         
        //         
        //         var fillWidth = (int)(barWidth * audioLevel);
        //         var barColor = audioLevel > 0.05f ? new Color(0, 255, 0, 255) : new Color(100, 100, 100, 255);
        //         if (fillWidth > 0)
        //         {
        //             Raylib.DrawRectangle(barX, barY, fillWidth, barHeight, barColor);
        //         }
        //         
        //         
        //         Raylib.DrawRectangleLines(barX, barY, barWidth, barHeight, Color.White);
        //         
        //         pos.Y += 25;
        //     }
        // }
        
        
        // throw new NotImplementedException();
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
}
