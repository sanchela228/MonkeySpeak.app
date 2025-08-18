using System.Numerics;
using App.System.Services;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Interface;
using Raylib_cs;
using PointRendering = Engine.PointRendering;

namespace App.Scenes;

public class StartUp: Scene
{
    private Animator Animator = new();
    private readonly Texture2D _textureMainPic;
    private readonly FontFamily _mainFontStartup;
    private Vector2 centerScreen;
    
    protected Interface.Loader Loader = new( new Vector2(){X = 370, Y = 420} );

    public StartUp()
    {
        _textureMainPic = Resources.Instance.Texture("Images\\LogoMain90.png");
        _mainFontStartup = new FontFamily()
        {
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 24),
            Size = 24,
            Spacing = 1,
            Color = Color.White
        };
        
        Animator.Task((progress) =>
        {
            Color color = Color.White;
            color.A = (byte)(progress * 255);
            
            Texture.DrawEx(_textureMainPic, new Vector2(400, 200), color: color);
            Text.DrawPro(
                _mainFontStartup, 
                "Create your p2p voice chat right now!", 
                new Vector2(400, 280),
                color: color
            );
            
            Text.DrawWrapped(
                _mainFontStartup, 
                "Communication that no one limits", 
                new Vector2(400 - 120, 310), 
                240,
                TextAlignment.Center,
                color: color
            );
           
        }, onComplete: () => {
            Context.Instance.Authorization.Auth();
            // MainBackground.Instance.AnimateSpeedChange(-1f, 2f);
        }, duration: 1.3f, mirror: false, removable: false, repeat: false);



        test = new Button("Text buttons ffff") {
            Position = new Vector2(250, 100),
            Font = new FontFamily()
            {
                Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 28),
                Size = 28,
                Spacing = 0.05f,
                Color = Color.White
            },
            CornerRadius = 0.4f,
            CornerWidth = 2f,
            CornerColor = new Color(30, 30, 30),
            BackgroundColor = new Color(15, 15, 15),
            Padding = new Vector2(40, 20),
        };


        test.OnClick += (sender) =>
        {
            Console.WriteLine("CLICK");
        };
            
        AddNode(test);
        test.IsActive = false;


    }


    public Button test;

    protected override void Update(float deltaTime)
    {
        Loader.Update(deltaTime);
        Animator.Update(deltaTime);
    }

    protected override void Draw()
    {
        Animator.Draw();
        
        if (Context.Instance.Authorization.State == Authorization.AuthState.Pending)
        {
            Loader.Draw();
        }

        if (Context.Instance.Authorization.State == Authorization.AuthState.Error)
        {
            Text.DrawPro(
                _mainFontStartup, 
                Context.Instance.Authorization.ErrorMessage, 
                new Vector2(400, 420),
                color: Color.Red
            );
            
            test.IsActive = true;
        }

        if (Context.Instance.Authorization.State == Authorization.AuthState.Success)
        {
           
        }
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
    
}