namespace Engine;

public abstract class Scene
{
    protected abstract void Update(float deltaTime);
    protected abstract void Draw();
    protected abstract void Dispose();
    
    private readonly List<Node> _nodes = [];
    
    public void AddNode(Node node)
    {
        node.Scene = this;
        _nodes.Add(node);

        if (_nodes.Count > 1)
            _nodes.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
    }
    
    public void AddNodes(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            AddNode(node);
    }
    
    public void RemoveNode(Node node)
    {
        _nodes.Remove(node);
        node.Scene = null;
    }
    
    public void ClearNodes() => _nodes.Clear();
    public bool ContainsNode(Node node) => _nodes.Contains(node);
    public void SortNodes() => _nodes.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
    public List<Node> GetNodes() => _nodes;
    public int GetNodesCount() => _nodes.Count;

    // Убираем бессмысленный конструктор
    protected Scene()
    {
        // _nodes всегда пустой при создании, сортировка не нужна
    }

    public void RootUpdate(float deltaTime)
    {
        Update(deltaTime);
        NodesUpdate(deltaTime);
    }
    
    public void RootDraw()
    {
        Draw();
        NodesDraw();
    }
    
    public void RootDispose()
    {
        if (_nodes.Count > 0)
        {
            foreach (var node in _nodes.OrderBy(node => node.Order).ToList()) 
                node.RootDispose();
        }
        
        _nodes.Clear();
        Dispose();
    }

    private void NodesUpdate(float deltaTime)
    {
        if (_nodes.Count == 0)
            return;
        
        foreach (var node in _nodes.Where(node => node.IsActive).ToList())
            node.RootUpdate(deltaTime);
    }
    
    private void NodesDraw()
    {
        if (_nodes.Count == 0)
            return;

        foreach (var node in _nodes.Where(node => node.IsActive).ToList())
            node.RootDraw();
    }
}