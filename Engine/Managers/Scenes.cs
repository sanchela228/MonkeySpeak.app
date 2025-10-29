namespace Engine.Managers; 

public static class Scenes
{
    private static readonly Stack<Scene> _scenes = new();
    private static bool _shouldPopScene = false;
    private static Scene? _sceneToPop = null;
    
    public static Action OnScenePopped;
    public static Action OnScenePushed;

    public static void PushScene(Scene scene)
    {
        _scenes.Push(scene);
        
        OnScenePushed?.Invoke();
    }
    
    public static void PopScene()
    {
        _shouldPopScene = true;
        _sceneToPop = _scenes.Count > 0 ? _scenes.Peek() : null;
        
        OnScenePopped?.Invoke();
    }
    
    private static void ProcessPop()
    {
        if (_shouldPopScene && _sceneToPop != null)
        {
            _scenes.Pop()?.RootDispose();
            _shouldPopScene = false;
            _sceneToPop = null;
        }
    }
    
    public static Scene? PeekScene() 
    {
        if (_scenes.Count == 0) return null;
        return _scenes.Peek();
    }
    
    public static void Update(float deltaTime)
    {
        _scenes.Peek()?.RootUpdate(deltaTime);
        ProcessPop();
    }
    
    public static void Draw() => _scenes.Peek()?.RootDraw();
    
    public static void Dispose()
    {
        while (_scenes.Count > 0)
            PopScene();
        ProcessPop();
    }
    
    public static bool HasPreviousScene() => _scenes.Count > 1;
}