using System.Numerics;
using System.Text;
using App;
using App.Scenes;
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

public class Invited : Scene
{
    private FontFamily _mainFontBack;
    private Button buttonBack;
    private DemoInputInvitedRow _inputsRow;

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
            Position = new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2 + 170),
            Padding = new Vector2(30, 18),
            Text = Language.Get("Back")
        };
        
        buttonBack.OnClick += (sender) =>
        {
            Scenes.Instance.PopScene();
        };
        
        AddNode(buttonBack);
        
        _inputsRow = new DemoInputInvitedRow()
        {
            Position = new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2 - 60),
            PointRendering = PointRendering.Center
        };

        AddNode(_inputsRow);
        
        _onConnected = async () =>
        {
            _inputsRow.MarkSuccess();
            await Task.Delay(200);
            Console.WriteLine($"[CallFacade] Connected");
            Scenes.Instance.PushScene(new Room());
        };

        Context.CallFacade.OnConnected += _onConnected;
        _onSessionStateChanged = (session, state) =>
        {
            Console.WriteLine($"[CallFacade] Active session change state -> {state}");
            if (state == CallState.Failed)
            {
                _ = _inputsRow.MarkErrorAsync();
            }
        };
        
        Context.CallFacade.OnSessionStateChanged += _onSessionStateChanged;
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
        if (_sendRequestAuth) return;

        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key is >= 32 and <= 125 && _inputText.Length < maxInputLength)
            {
                _inputText.Append((char)key);
                _inputsRow.ResetStates();
                _inputsRow.SetSymbol(_inputText.Length - 1, (char)key);
            }
            
            if ( _inputText.Length == maxInputLength && !_sendRequestAuth )
            {
                _sendRequestAuth = true;
                await Context.CallFacade.ConnectToSessionAsync( _inputText.ToString().ToLower() );
            }
            
            key = Raylib.GetCharPressed();
        }

        if (Input.IsPressed(KeyboardKey.Backspace) && _inputText.Length > 0)
        {
            Console.WriteLine("LOG");
            _inputText.Remove(_inputText.Length - 1, 1);
            _inputsRow.ResetStates();
            _inputsRow.SetSymbol(_inputText.Length, null);

            _sendRequestAuth = false;
        }
    }
    
    protected override void Dispose()
    {
        Context.CallFacade.OnSessionStateChanged -= _onSessionStateChanged;
        Context.CallFacade.OnConnected -= _onConnected;

        _onSessionStateChanged = null;
        _onConnected = null;
    }
}