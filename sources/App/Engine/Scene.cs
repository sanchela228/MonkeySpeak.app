using System.Linq;

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
        UpdatePointerCapture();
        Update(deltaTime);
        NodesUpdate(deltaTime);
    }

    private void UpdatePointerCapture()
    {
        var nodes = _nodes;
        if (nodes.Count == 0)
        {
            Engine.Managers.Pointer.SetHoveredNode((Node?)null);
            return;
        }

        var mousePos = Raylib_cs.Raylib.GetMousePosition();

        if (Engine.Managers.Pointer.PressedNode != null)
        {
            Engine.Managers.Pointer.SetHoveredNode(Engine.Managers.Pointer.PressedNode);
            return;
        }

        var all = CollectActiveNodesWithTraversalIndex();

        Node? top = null;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var n = all[i];
            if (!Raylib_cs.Raylib.CheckCollisionPointRec(mousePos, n.Bounds))
                continue;

            top = n;
            if (n.Overlap == OverlapsMode.Exclusive)
                break;
        }

        Engine.Managers.Pointer.SetHoveredNode(top);

        if (Raylib_cs.Raylib.IsMouseButtonPressed(Raylib_cs.MouseButton.Left))
        {
            Engine.Managers.Pointer.SetPressedNode(top);
        }
    }

    private List<Node> CollectActiveNodesWithTraversalIndex()
    {
        var collected = new List<(Node node, int order, int seq)>();
        int seq = 0;

        void Visit(Node n)
        {
            if (n == null || !n.IsActive)
                return;

            collected.Add((n, n.Order, seq++));

            if (n.Childrens == null)
                return;

            foreach (var child in n.Childrens)
                Visit(child);
        }

        foreach (var root in _nodes)
            Visit(root);

        collected.Sort((a, b) =>
        {
            int byOrder = a.order.CompareTo(b.order);
            return byOrder != 0 ? byOrder : a.seq.CompareTo(b.seq);
        });

        return collected.Select(x => x.node).ToList();
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