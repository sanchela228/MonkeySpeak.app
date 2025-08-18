using System.Numerics;
using System.Linq;
using Raylib_cs;

using SceneManager = Engine.Managers.Scenes;

namespace Engine;

public enum PointRendering
{
    Center,
    LeftTop,
}

public enum OverlapsMode
{
    None,
    Exclusive
}

public abstract class Node : IDisposable
{
    public abstract void Update(float deltaTime);
    public abstract void Draw();
    public abstract void Dispose();

    public Node() => Scene = SceneManager.Instance.PeekScene();
    
    public bool IsActive { get; set; } = true;
    
    public Scene Scene { get; set; }
    protected OverlapsMode Overlap { get; set; } = OverlapsMode.Exclusive;

    public bool RecursiveDrawChildren { get; set; } = false;
    public bool RecursiveUpdateChildren { get; set; } = false;
    
    public void RootUpdate(float deltaTime)
    {
        if (IsActive) Update(deltaTime);

        if (_childrens is not null && _childrens.Any())
        {
            if (RecursiveUpdateChildren)
            {
                for (int i = _childrens.Count - 1; i >= 0; i--)
                {
                    var child = _childrens[i];
                    if (child.IsActive) child.RootUpdate(deltaTime);
                }
            }
            else
            {
                foreach (var child in _childrens) 
                    if (child.IsActive) child.RootUpdate(deltaTime);
            }
        }
    }
    
    public void RootDraw()
    {
        if (IsActive) Draw();

        if (_childrens is not null && _childrens.Any())
        {
            if (RecursiveDrawChildren)
            {
                for (int i = _childrens.Count - 1; i >= 0; i--)
                {
                    var child = _childrens[i];
                    if (child.IsActive) child.RootDraw();
                }
            }
            else
            {
                foreach (var child in _childrens) 
                    if (child.IsActive) child.RootDraw();
            }
        }
    }
    
    public void RootDispose()
    {
        Dispose();
        
        if (_childrens is not null && _childrens.Any())
        {
            foreach (var child in _childrens.OrderBy(node => node.Order).ToList()) 
                child.RootDispose();
        }
    }
    
    private Vector2 _position;
    
    public Vector2 Position 
    { 
        get 
        {
            if (_parent == null) return _position;
            return _parent.Position + _position;
        }
        set 
        {
            if (_parent == null) _position = value;
            else _position = value - _parent.Position;
        }
    }
    
    private float _rotation = 0f;
    public float Rotation 
    { 
        get => _parent == null ? _rotation : _parent.Rotation + _rotation;
        set 
        {
            if (_parent == null)
                _rotation = value;
            else
                _rotation = value - _parent.Rotation;
        }
    }

    private Node? _parent = null;
    protected List<Node> _childrens = new();

    public virtual PointRendering PointRendering { get; set; } = PointRendering.Center;
    
    public Node Parent
    {
        get => _parent;
        set => _parent = value;
    }
    
    public event Action<Node> PreventParent;

    public void SetParent(Node newParent, int index = -1, Vector2? position = null)
    {
        if (_parent == newParent)
            return;

        if (_parent != null)
            _parent._childrens.Remove(this);

        _parent = newParent;
        PreventParent?.Invoke(newParent);
        
        if (position is not null)
            Position = position ?? Vector2.Zero;
        
        if (SceneManager.Instance.PeekScene() is not null && SceneManager.Instance.PeekScene().ContainsNode(this))
            SceneManager.Instance.PeekScene().RemoveNode(this);

        if (_parent != null && !_parent._childrens.Contains(this))
        {
            if (index == -1)
                _parent._childrens.Add(this);
            else
                _parent._childrens.Insert(index, this);
        }
    }
    
    public IReadOnlyList<Node> Childrens => _childrens.AsReadOnly();  
    
    public void SetParent(Scene scene)
    {
        if (_parent != null)
            _parent._childrens.Remove(this);

        _parent = null;
        
        if (scene is not null) 
            scene.AddNode(this);
    }

    public void AddChild(Node node)
    {
        _childrens.Add(node);
        node.SetParent(this);

        if (_childrens.Count > 1)
            _childrens.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
    }
    
    public int ChildrenCount => _childrens.Count;
    
    public void AddChildrens(IEnumerable<Node> list)
    {
        if (list is not null && list.Any())
        {
            _childrens.AddRange(list);
            
            foreach (var node in list)
                node.SetParent(this);
                
            // Сортируем всех детей по Order
            if (_childrens.Count > 1)
                _childrens.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
        }
    }

    public void ReplaceChildrens(IEnumerable<Node> list)
    {
        _childrens.Clear();
        _childrens.AddRange(list);
        
        foreach (var node in list)
            node.SetParent(this);
            
        // Сортируем всех детей по Order
        if (_childrens.Count > 1)
            _childrens.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
    }

    public void ClearChildrens()
    {
        _childrens.Clear();
    }

    public void RemoveChild(Node node)
    {
        _childrens.Remove(node);
    }
    
    private Vector2 _size;
    private Vector2 _scale = Vector2.One;
    private Rectangle _bounds => new Rectangle(Position.X, Position.Y, Size.X, Size.Y);
    public Rectangle Bounds
    {
        get
        {
            if (PointRendering == PointRendering.Center)
                return Helpers.Rectangle.CenterShiftRec(Position, Size);

            return _bounds;
        }
    }

    private Rectangle? _collider;
    private Rectangle? _cachedCollider;
    private bool _colliderDirty = true;
    
    public Rectangle Collider {
        get
        {
            if (_colliderDirty || _cachedCollider == null)
            {
                if (_collider is null)
                {
                    if (PointRendering == PointRendering.Center)
                        _cachedCollider = Helpers.Rectangle.CenterShiftRec(_bounds);
                    else
                        _cachedCollider = _bounds;
                }
                else
                {
                    if (PointRendering == PointRendering.Center)
                    {
                        var rect = new Rectangle(
                            _collider.Value.Position.X + Position.X,
                            _collider.Value.Position.Y + Position.Y,
                            _collider.Value.Size.X * _scale.X,
                            _collider.Value.Size.Y * _scale.Y
                        );
                        _cachedCollider = Helpers.Rectangle.CenterShiftRec(rect);
                    }
                    else
                    {
                        _cachedCollider = new Rectangle(
                            _collider.Value.Position.X + Position.X,
                            _collider.Value.Position.Y + Position.Y,
                            _collider.Value.Size.X * _scale.X,
                            _collider.Value.Size.Y * _scale.Y
                        );
                    }
                }
                _colliderDirty = false;
            }
            
            return _cachedCollider.Value;
        }
        set 
        { 
            _collider = value;
            _colliderDirty = true;
        }
    }
    
    public Vector2 Size 
    { 
        get => new Vector2(_size.X * _scale.X, _size.Y * _scale.Y);
        set 
        { 
            _size = value;
            _colliderDirty = true;
        }
    }
    
    public Vector2 Scale
    { 
        get => _scale; 
        set 
        { 
            _scale = value;
            _colliderDirty = true;
        }
    }
    
    private int _order = 100;
    public int Order 
    { 
        get => _order;
        set 
        { 
            _order = value;

            if (_parent != null)
                _parent._childrens.Sort((node1, node2) => node1.Order.CompareTo(node2.Order));
        }
    }
    
    public bool ICollisionWith(Rectangle rect) => Raylib.CheckCollisionRecs(rect, Collider);
    
    public void InvalidateCollider()
    {
        _colliderDirty = true;
    }

    public IEnumerable<Node> GetActiveChildren()
    {
        return _childrens.Where(child => child.IsActive);
    }
    
    public int GetActiveChildrenCount()
    {
        return _childrens.Count(child => child.IsActive);
    }
}