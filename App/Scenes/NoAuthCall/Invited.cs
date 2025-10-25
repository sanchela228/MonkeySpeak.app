using System.Numerics;
using System.Text;
using App.System.Calls.Domain;
using App.System.Managers;
using App.System.Services;
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
    private Action? _onConnected;
    private bool _sendRequestAuth;
    
    public Invited()
    {
        _mainFontBack = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        buttonBack = new Classic(_mainFontBack)
        {
            Position = new Vector2(Raylib.GetScreenWidth()/ 2, Raylib.GetScreenHeight() / 2 + 170),
            Padding = new Vector2(30, 18),
            Text = Language.Get("Back")
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
        
        _onConnected = async () =>
        {
            await Task.Delay(2000);
            Console.WriteLine($"[CallFacade] Connected");
            Engine.Managers.Scenes.Instance.PushScene(new Room());
        };
        
        Context.Instance.CallFacade.OnConnected += _onConnected;
        _onSessionStateChanged = (session, state) =>
        {
            Console.WriteLine($"[CallFacade] Active session change state -> {state}");
        };
        
        Context.Instance.CallFacade.OnSessionStateChanged += _onSessionStateChanged;
    }
    
    protected override void Update(float deltaTime)
    {
        HandleTextInput();
    }

    protected override void Draw()
    {
        Text.DrawWrapped(
            _mainFontBack, 
            Language.Get("Enter the code to connect to the room"), 
            new Vector2(Raylib.GetScreenWidth() / 2 - 125, Raylib.GetScreenHeight() / 2),
            250,
            color: Color.White,
            alignment: TextAlignment.Center
        );
    }
    
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

        if (Input.IsPressed(KeyboardKey.Backspace))
        {
            // NOT WORK
            Console.WriteLine("WDQWDQDW");
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _inputText.Length > 0)
        {
            _inputText.Remove(_inputText.Length - 1, 1);
            _linkInputs.ForEach(x => x.IsFailed = false);
            _linkInputs[_inputText.Length].Symbol = null;

            _sendRequestAuth = false;
        }

        if (Input.IsPressed(KeyboardKey.Enter) && _inputText.Length == maxInputLength && !_sendRequestAuth)
        {
            _sendRequestAuth = true;
            await Context.Instance.CallFacade.ConnectToSessionAsync(_inputText.ToString().ToLower());
        }
    }
    
    protected override void Dispose()
    {
        Context.Instance.CallFacade.OnSessionStateChanged -= _onSessionStateChanged;
        Context.Instance.CallFacade.OnConnected -= _onConnected;

        _onSessionStateChanged = null;
        _onConnected = null;
    }
}