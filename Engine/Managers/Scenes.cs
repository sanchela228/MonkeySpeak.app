namespace Engine.Managers; 

public class Scenes
{
    static Scenes() => Instance = new();
    public static Scenes Instance { get; private set; }
    
    private readonly Stack<Scene> _scenes = new();
    private bool _shouldPopScene = false;
    private Scene? _sceneToPop = null;
    
    public Action OnScenePopped;
    public Action OnScenePushed;

    public void PushScene(Scene scene)
    {
        _scenes.Push(scene);
        
        OnScenePushed?.Invoke();
    }
    
    public void PopScene()
    {
        _shouldPopScene = true;
        _sceneToPop = _scenes.Count > 0 ? _scenes.Peek() : null;
        
        OnScenePopped?.Invoke();
    }
    
    private void ProcessPop()
    {
        if (_shouldPopScene && _sceneToPop != null)
        {
            _scenes.Pop()?.RootDispose();
            _shouldPopScene = false;
            _sceneToPop = null;
        }
    }
    
    public Scene? PeekScene() 
    {
        if (_scenes.Count == 0) return null;
        return _scenes.Peek();
    }
    
    public void Update(float deltaTime)
    {
        _scenes.Peek()?.RootUpdate(deltaTime);
        ProcessPop();
    }
    
    public void Draw() => _scenes.Peek()?.RootDraw();
    
    public void Dispose()
    {
        while (_scenes.Count > 0)
            PopScene();
        ProcessPop();
    }
    
    public bool HasPreviousScene() => _scenes.Count > 1;
}