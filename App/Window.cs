using App.Base;
using App.System.Services;
using Engine;
using Raylib_cs;

namespace App;

public class Window : IDisposable
{
    protected Header Header;
    
    public Window() 
    {
        Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(800, 600, "MonkeySpeak");
        
        Header = new Header();
    }
    
    public void Run( Scene startScene ) 
    {
        Logger.Write(Logger.Type.Info, "Run application");
        
        Engine.Managers.Scenes.Instance.PushScene( startScene );
        
        Image image = Raylib.LoadImage(@"Resources\Textures\Images\LogoMain.png");
        
        Texture2D texture = Raylib.LoadTextureFromImage(image);
        
        Graphics.MainBackground.Instance.SetSettings();
        Platforms.Windows.Window.SetWindowRoundedCorners();
        
        Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor()));
        
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            
            try
            {
                Header.Update(deltaTime);
                Engine.Managers.Scenes.Instance.Update(deltaTime);
                Graphics.MainBackground.Instance.Update(deltaTime);
            
                Raylib.BeginDrawing();
                Raylib.ClearBackground( new Color(20, 20, 20, 255));
                Graphics.MainBackground.Instance.Draw();
            
                Header.Draw();
                Engine.Managers.Scenes.Instance.Draw();
                
                Raylib.EndDrawing();
            }
            catch (Exception e)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        Logger.Write(Logger.Type.Info, "Close application");
        
        Engine.Managers.Scenes.Instance.Dispose();
        Raylib.CloseWindow();
    }
}