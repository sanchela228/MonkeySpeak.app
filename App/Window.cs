using System.Numerics;
using App.Base;
using Engine;
using Engine.Managers;
using Raylib_cs;

namespace App;

public class Window : IDisposable
{
    protected Header Header = new();
    
    public Window() 
    {
        Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(800, 600, "MonkeySpeak");
        
       
    }
    
    public void Run( Scene startScene ) 
    {
        Engine.Managers.Scenes.Instance.PushScene( startScene );
        
        Image image = Raylib.LoadImage(@"Resources\Textures\Images\LogoMain.png");
        
        Texture2D texture = Raylib.LoadTextureFromImage(image);
        
        Graphics.MainBackground.Instance.SetSettings();
        Platforms.Windows.Window.SetWindowRoundedCorners();
        
        
        
        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            
            Header.Update(deltaTime);
            Engine.Managers.Scenes.Instance.Update(deltaTime);
            Graphics.MainBackground.Instance.Update(deltaTime);
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground( new Color(20, 20, 20, 255));
            Graphics.MainBackground.Instance.Draw();
            
            Header.Draw();
            Engine.Managers.Scenes.Instance.Draw();
            
            Raylib.DrawFPS(10, 10);
            
            Raylib.EndDrawing();
        }
    }

    public void Dispose()
    {
        Engine.Managers.Scenes.Instance.Dispose();
        Raylib.CloseWindow();
    }
}