using System.Numerics;
using System.Text;
using System.Linq;

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
            Scenes.PopScene();
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
            Logger.Write($"[CallFacade] Connected");
            Scenes.PushScene(new Room());
        };

        Context.CallFacade.OnConnected += _onConnected;
        _onSessionStateChanged = (session, state) =>
        {
            Logger.Write($"[CallFacade] Active session change state -> {state}");
            if (state == CallState.Failed)
            {
                _sendRequestAuth = false;
                _ = _inputsRow.MarkErrorAsync();
            }
        };
        
        Context.CallFacade.OnSessionStateChanged += _onSessionStateChanged;

        try
        {
            var clip = Raylib.GetClipboardText_();
            
            if (clip.Length == 6)
                TryAutoPasteFromClipboardOnStart();
        }
        catch (Exception e) { }
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
        if (_inputsRow.IsLocked) return;

        bool ctrlDown = Input.IsDown(KeyboardKey.LeftControl) || Input.IsDown(KeyboardKey.RightControl);
        if (ctrlDown && Input.IsPressed(KeyboardKey.V) && _inputText.Length == 0)
        {
            var clip = string.Empty;
            
            try
            {
                clip = Raylib.GetClipboardText_();
            }
            catch (Exception e) { clip = string.Empty; }
            
            var pasted = SanitizeToCode(clip, maxInputLength);
            if (!string.IsNullOrEmpty(pasted))
            {
                ApplyCodeToUI(pasted);
                if (_inputText.Length == maxInputLength && !_sendRequestAuth)
                {
                    _sendRequestAuth = true;
                    await Context.CallFacade.ConnectToSessionAsync(_inputText.ToString().ToLower());
                }
            }
        }
        
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (_inputText.Length == maxInputLength)
                return;
            
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
            _inputText.Remove(_inputText.Length - 1, 1);
            _inputsRow.ResetStates();
            _inputsRow.SetSymbol(_inputText.Length, null);

            _sendRequestAuth = false;
        }
    }

    private void TryAutoPasteFromClipboardOnStart()
    {
        var clip = string.Empty;
            
        try
        {
            clip = Raylib.GetClipboardText_();
        }
        catch (Exception e) { clip = string.Empty; }
        
        var pasted = SanitizeToCode(clip, maxInputLength);
        if (!string.IsNullOrEmpty(pasted) && pasted.Length == maxInputLength)
        {
            ApplyCodeToUI(pasted);
            _sendRequestAuth = true;
            
            _ = Context.CallFacade.ConnectToSessionAsync(_inputText.ToString().ToLower());
        }
    }

    private string SanitizeToCode(string? src, int take)
    {
        if (string.IsNullOrWhiteSpace(src)) return string.Empty;
        var filtered = new string(src
            .Where(char.IsLetterOrDigit)
            .Take(take)
            .ToArray());
        return filtered;
    }

    private void ApplyCodeToUI(string code)
    {
        _inputText.Clear();
        _inputsRow.ResetStates();
        _inputsRow.ClearAllSymbols();
        for (int i = 0; i < Math.Min(code.Length, maxInputLength); i++)
        {
            _inputText.Append(code[i]);
            _inputsRow.SetSymbol(i, code[i]);
        }
    }
    
    protected override void Dispose()
    {
        Logger.Write("[Dispose] Invited.cs dispose");
        
        Context.CallFacade.OnSessionStateChanged -= _onSessionStateChanged;
        Context.CallFacade.OnConnected -= _onConnected;
        _onSessionStateChanged = null;
        _onConnected = null;
    }
}