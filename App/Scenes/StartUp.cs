using System.Diagnostics;
using System.Numerics;
using App.System.Modules;
using App.System.Services;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Interface;
using Interface.Buttons;
using Raylib_cs;
using PointRendering = Engine.PointRendering;
using Rectangle = Raylib_cs.Rectangle;

namespace App.Scenes;

public class StartUp: Scene
{
    private Animator Animator = new();
    private readonly Texture2D _textureMainPic;
    private readonly FontFamily _mainFontStartup;
    private Vector2 centerScreen;
    
    protected Interface.Loader Loader = new( new Vector2(){X = 370, Y = 420} );
    public Network Network;
    
    public StartUp()
    {

        Network = Context.Instance.Network;
        
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

            Texture.DrawEx(_textureMainPic,
                new Vector2(Raylib.GetRenderWidth() / 2, (Raylib.GetRenderHeight() / 2 - 100) - progress * 16), color: color);
            Text.DrawPro(
                _mainFontStartup,
                "Create your p2p voice chat right now!",
                new Vector2(Raylib.GetRenderWidth() / 2, (Raylib.GetRenderHeight() / 2 - 20) - progress * 16),
                color: color
            );

            Text.DrawWrapped(
                _mainFontStartup,
                "Communication with no limits",
                new Vector2(Raylib.GetRenderWidth() / 2 - 120, (Raylib.GetRenderHeight() / 2 + 10) - progress * 16),
                240,
                TextAlignment.Center,
                color: color
            );

        }, onComplete: () => Network.ConnectServer(), duration: 1f, mirror: false, removable: false, repeat: false);



        
        test2 = new Classic(_mainFontStartup)
        {
            Position = new Vector2(Raylib.GetRenderWidth()/ 2, Raylib.GetRenderHeight() / 2 + 100),
            Padding = new Vector2(70, 18),
            Text = "Create a room",
            IsActive = false
        };
        
        test3 = new Classic(_mainFontStartup)
        {
            Position = new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2 + 160),
            Padding = new Vector2(130, 18),
            Text = "Connect",
            IsActive = false
        };
        
        
        
        AddNode(test2);
        AddNode(test3);
        
        var fontFamilyRetry = new FontFamily
        {
            Font = Resources.Instance.FontEx("JetBrainsMonoNL-Regular.ttf", 22),
            Size = 22,
            Spacing = 0.05f,
            Color = Color.Gray
        };
        
        retryLink = new Link(fontFamilyRetry)
        {
            Position = new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2 + 165),
            Text = "Retry",
            IsActive = false
        };
         
        retryLink.OnClick += async (sender) => Network.ConnectServer();
        
        authLink = new Link(fontFamilyRetry)
        {
            Position = new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2 + 235),
            Text = "Authorization in server",
            IsActive = false,
        };
        
        
        authLink.OnClick += async (sender) => 
        {
            string authUrl = Context.Instance.Network.GenerateAuthorizationUrl();
            Process.Start( new ProcessStartInfo(authUrl) { UseShellExecute = true } );
        };
        
        
        AddNode(authLink);
        AddNode(retryLink);
        _load = true;
        
        Network.OnStateChanged += (net, networkState) =>
        {
            if (networkState == Network.NetworkState.Connecting || networkState == Network.NetworkState.Reconnecting)
            {
                _load = true;
                retryLink.IsActive = false;
                authLink.IsActive = false;
                _drawErrorText = false;
                test2.IsActive = false;
                test3.IsActive = false;
            }
            
            if (networkState == Network.NetworkState.Error || networkState == Network.NetworkState.Disconnected)
            {
                _load = false;
                retryLink.IsActive = true;
                _drawErrorText = true;
            
                authLink.IsActive = false;
                test2.IsActive = false;
                test3.IsActive = false;
            }
        
            if (networkState == Network.NetworkState.Connected)
            {
                _load = false;
                retryLink.IsActive = false;
                _drawErrorText = false;
                authLink.IsActive = true;
            
                test2.IsActive = true;
                test3.IsActive = true;
            }
        };
    }

    public Button test2;
    public Button test3;
    public Link retryLink;
    public Link authLink;
    private bool _drawErrorText = false;
    private bool _load;

    protected override void Update(float deltaTime)
    {
        Loader.Update(deltaTime);
        Animator.Update(deltaTime);
    }

    protected override void Draw()
    {
        Animator.Draw();
        
        if (_load) Loader.Draw();
        
        if (_drawErrorText)
        {
            Text.DrawPro(
                _mainFontStartup, 
                "Connection server error", 
                new Vector2(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2 + 120),
                color: Color.Red
            );
        }
    }

    protected override void Dispose()
    {
        // throw new NotImplementedException();
    }
    
}