using System.Net;
using System.Numerics;
using App.System.Utils;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Interface;
using Interface.Buttons;
using Raylib_cs;

namespace App.Scenes.NoAuthCall;

public class Creator : Scene
{
    private FontFamily _mainFont;
    private FontFamily _mainFontBack;
    private Button buttonBack;
    
    private string _code;

    public Creator()
    {
        _mainFont = new FontFamily()
        {
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 62),
            Size = 62,
            Spacing = 1,
            Color = Color.White
        };
        
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
        
        Context.Instance.CallFacade.CreateSession();
        Context.Instance.CallFacade.Service.OnSessionCreated += (code) =>
        {
            _code = code;
        };
    }
    
    protected override void Update(float deltaTime)
    {
        // throw new NotImplementedException();
    }

    protected override void Draw()
    {
        Text.DrawPro(
            _mainFont, 
            _code, 
            new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2 - 60),
            color: Color.Red
        );
        
        Text.DrawPro(
            _mainFontBack, 
            "Send this code to your interlocutor", 
            new Vector2(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2),
            color: Color.White
        );
    }

    protected override void Dispose()
    {
        
    }
}