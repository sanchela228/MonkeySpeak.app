using System.Numerics;
using System.Text;
using App.System.Calls.Domain;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Interface;
using Interface.Buttons;
using Interface.Inputs;
using Raylib_cs;
using PointRendering = Engine.PointRendering;

namespace App.Scenes.NoAuthCall;

public class Invited : Scene
{
    private FontFamily _mainFontBack;
    private Button buttonBack;
    private List<DemoInputInvited> _linkInputs;

    private Action<CallSession, CallState>? _onSessionStateChanged;
    private bool _sendRequestAuth;
    
    public Invited()
    {
        _mainFontBack = new FontFamily()
        {
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        buttonBack = new Classic(_mainFontBack)
        {
            Position = new Vector2(Raylib.GetScreenWidth()/ 2, Raylib.GetScreenHeight() / 2 + 170),
            Padding = new Vector2(30, 18),
            Text = "Back" 
        };
        
        buttonBack.OnClick += (sender) => {
            Engine.Managers.Scenes.Instance.PopScene();
        };
        
        AddNode(buttonBack);
        
        var listInputs = new List<DemoInputInvited>
        {
            new(){PointRendering = PointRendering.LeftTop},
            new(){PointRendering = PointRendering.LeftTop},
            new(){PointRendering = PointRendering.LeftTop},
            new(){PointRendering = PointRendering.LeftTop},
            new(){PointRendering = PointRendering.LeftTop},
            new(){PointRendering = PointRendering.LeftTop}
        };

        _linkInputs = listInputs;
        
        // TODO: FIX PlaceInLine for POINTRENDERING CENTER
        Graphics.Render.PlaceInLine(
            listInputs, 
            new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2 - 90),
            22
        );

        AddNodes(listInputs);
        
        _onSessionStateChanged = (session, state) =>
        {
            Console.WriteLine($"[CallFacade] {session.CallId} -> {state}");
        };
        
        Context.Instance.CallFacade.OnSessionStateChanged += _onSessionStateChanged;
    }
    
    protected override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
        HandleTextInput();
        
    }

    protected override void Draw()
    {
        Text.DrawWrapped(
            _mainFontBack, 
            "Enter the code to connect to the room", 
            new Vector2(Raylib.GetScreenWidth() / 2 - 125, Raylib.GetScreenHeight() / 2),
            250,
            color: Color.White,
            alignment: TextAlignment.Center
        );
    }
    
    // TODO: CREATE INPUTHANDLER AND COMMANDS IN ENGINE
    private StringBuilder _inputText = new();
    private int maxInputLength = 6;
    private async Task HandleTextInput()
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key is >= 32 and <= 125 && _inputText.Length < maxInputLength)
            {
                _inputText.Append((char)key);
                _linkInputs.ForEach(x => x.IsFailed = false);

                _linkInputs[_inputText.Length - 1].Symbol = (char) key;
            }
            
            if ( _inputText.Length == maxInputLength && !_sendRequestAuth )
            {
                _sendRequestAuth = true;
                await Context.Instance.CallFacade.ConnectToSessionAsync( _inputText.ToString().ToLower() );
            }
            
            key = Raylib.GetCharPressed();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _inputText.Length > 0)
        {
            _inputText.Remove(_inputText.Length - 1, 1);
            _linkInputs.ForEach(x => x.IsFailed = false);
            _linkInputs[_inputText.Length].Symbol = null;

            _sendRequestAuth = false;
        }
    }
    
    protected override void Dispose()
    {
        Context.Instance.CallFacade.OnSessionStateChanged -= _onSessionStateChanged;
    }
}