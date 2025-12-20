using App.Base;
using App.System.Managers;
using App.System.Services;
using Engine;
using Engine.Managers;
using Raylib_cs;

namespace App;

public class Window : IDisposable
{
    protected Header Header;
    private bool _shutdownRequested;
    
    public Window() 
    {
        Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(800, 600, "MonkeySpeak");
        
        Header = new Header();
    }
    
    public void Run( Scene startScene ) 
    {
        Logger.Write(Logger.Type.Info, "Run application");
        
        Engine.Managers.Scenes.PushScene( startScene );
        
        Image image = Raylib.LoadImage(@"Resources\Textures\Images\LogoMain.png");
        
        Texture2D texture = Raylib.LoadTextureFromImage(image);
        
        Graphics.MainBackground.Instance.SetSettings();
        Platforms.Windows.Window.SetWindowRoundedCorners();
        
        Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor()));
        
        Raylib.InitAudioDevice();
        if (!Raylib.IsAudioDeviceReady())
            return;
        
        while (!Raylib.WindowShouldClose())
        {
            if (_shutdownRequested)
                break;

            float deltaTime = Raylib.GetFrameTime();
            
            try
            {
                Header.Update(deltaTime);
                Input.Update(deltaTime);
                MainThreadDispatcher.ExecutePending();
                Notificator.Update(deltaTime);
                Engine.Managers.Scenes.Update(deltaTime);
                Graphics.MainBackground.Instance.Update(deltaTime);
            
                Raylib.BeginDrawing();
                Raylib.ClearBackground( new Color(20, 20, 20, 255));
                Graphics.MainBackground.Instance.Draw();
            
                Header.Draw();
                Notificator.Draw();
                Engine.Managers.Scenes.Draw();
                
                Raylib.EndDrawing();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _shutdownRequested = true;
                try { Engine.Managers.Scenes.Dispose(); } catch { }
                try { if (Raylib.IsWindowReady()) Raylib.CloseWindow(); } catch { }
                break;
            }
        }
    }

    public void Dispose()
    {
        Logger.Write(Logger.Type.Info, "Close application");
        
        try { Engine.Managers.Scenes.Dispose(); } catch { }
        try { if (Raylib.IsAudioDeviceReady()) Raylib.CloseAudioDevice(); } catch { }
        try { if (Raylib.IsWindowReady()) Raylib.CloseWindow(); } catch { }
    }
}