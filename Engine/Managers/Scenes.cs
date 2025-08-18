namespace Engine.Managers;

public class Scenes
{
    static Scenes() => Instance = new();
    public static Scenes Instance { get; private set; }
    
    private readonly Stack<Scene> _scenes = new();
    public void PushScene(Scene scene) => _scenes.Push(scene);
    public void PopScene() => _scenes.Pop()?.RootDispose();
    public Scene? PeekScene() 
    {
        if (_scenes.Count == 0) return null;
        
        return _scenes.Peek();
    }
    public void Update(float deltaTime) => _scenes.Peek()?.RootUpdate(deltaTime);
    public void Draw() => _scenes.Peek()?.RootDraw();
    public void Dispose()
    {
        while (_scenes.Count > 0)
            PopScene();
    }
    public bool HasPreviousScene() => _scenes.Count > 1;
}