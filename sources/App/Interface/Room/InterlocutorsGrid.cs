using App.System.Calls.Domain;
using Engine;
using Raylib_cs;
using System.Numerics;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using App;

namespace Interface.Room;

public class InterlocutorsGrid : Node
{
    private ObservableCollection<Interlocutor>? interlocutors;
    
    public ObservableCollection<Interlocutor> Interlocutors
    {
        get => interlocutors ?? throw new InvalidOperationException("Interlocutors not initialized");
        set
        {
            if (interlocutors != null) 
                interlocutors.CollectionChanged -= OnInterlocutorsChanged;
            
            interlocutors = value;
            
            if (interlocutors != null)
                interlocutors.CollectionChanged += OnInterlocutorsChanged;
            
            SyncInterlocutorsWithChildrenAvatars();
        }
    }
    
    private Dictionary<Interlocutor, Avatar> mapInterlocutorsToAvatars = new();

    public float DefaultRadius = 100f;
    public float MaxRadius = 140f;
    public float MinRadius = 20f;
    public float Spacing = 34f;

    private readonly List<Vector2> _centers = new();
    private float _radius = 0f;

    public InterlocutorsGrid()
    {
        Interlocutors = new ObservableCollection<Interlocutor>();
    }

    private void OnInterlocutorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncInterlocutorsWithChildrenAvatars();
    }
    
    private void SyncInterlocutorsWithChildrenAvatars()
    {
        foreach (var il in Interlocutors)
        {
            if (!mapInterlocutorsToAvatars.ContainsKey(il))
            {
                var av = new Avatar();
                mapInterlocutorsToAvatars.Add(il, av);
                AddChild(av);
            }
        }

        foreach (var ilPair in mapInterlocutorsToAvatars)
        {
            if (!Interlocutors.Contains(ilPair.Key))
            {
                mapInterlocutorsToAvatars.Remove(ilPair.Key);
                RemoveChild(ilPair.Value);
            }
        }
    }

    public override void Update(float deltaTime)
    {
        ComputeLayout();
        HandleDragInput(deltaTime);
        SmoothToTargets(deltaTime);
        
        var audioLevels = Context.CallFacade.GetAudioLevels();
        foreach (var il in Interlocutors)
        {
            if (audioLevels.TryGetValue(il.Id, out var level))
                mapInterlocutorsToAvatars[il].AudioLevel = level;
        }
    }

    public override void Draw()
    {
        // Raylib.DrawRectangle(
        //     (int)Collider.X, 
        //     (int)Collider.Y, 
        //     (int)Collider.Width, 
        //     (int)Collider.Height, 
        //     new Color(190, 20, 110, 150)
        // );

        if (Childrens.Count == 0 || _displayRadius <= 0f) 
            return;

        for (int i = 0; i < Childrens.Count; i++)
        {
            if (_isDragging && i == _dragIndex) 
                continue;
            
            Childrens[i].Size = new Vector2(_displayRadius * 2, _displayRadius * 2);
        }

        foreach (var c in _centers)
        {
            Raylib.DrawCircleV(c, 2f, Color.Red);
        }
    }

    private Vector2 _dragOffset;
    private float _displayRadius = 0f;

    public float LerpSpeed = 10f;
    
    private readonly List<Vector2> _slots = new();
    private List<int> _slotOf = new(); 
    private int _lastCount = -1;
    private bool _isDragging = false;
    private int _dragIndex = -1;
    private Vector2 _dragCurrentPos;
    private Vector2 _dragStartCenter;

    public override void Dispose()
    {
        // throw new NotImplementedException();
    }
    
    private void HandleDragInput(float dt)
    {
        var mouse = Raylib.GetMousePosition();
        float r = _displayRadius > 0 ? _displayRadius : _radius;
        float r2 = r * r;

        if (!_isDragging)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                int hit = -1;
                float best = float.MaxValue;
                for (int i = 0; i < Childrens.Count; i++)
                {
                    float d2 = Vector2.DistanceSquared(mouse, Childrens[i].Position);
                    if (d2 <= r2 && d2 < best)
                    {
                        best = d2;
                        hit = i;
                    }
                }
                if (hit != -1)
                {
                    _isDragging = true;
                    _dragIndex = hit;
                    _dragOffset = Childrens[hit].Position - mouse; 
                    _dragCurrentPos = mouse + _dragOffset;
                    _dragStartCenter = _centers.Count > hit ? _centers[hit] : Childrens[hit].Position;
                }
            }
        }
        else
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                _dragCurrentPos = mouse + _dragOffset;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                if (_centers.Count > _dragIndex)
                {
                    float distSelf = Vector2.Distance(_dragCurrentPos, _centers[_dragIndex]);
                    int bestJ = _dragIndex;
                    float bestDist = distSelf;

                    for (int j = 0; j < _centers.Count; j++)
                    {
                        if (j == _dragIndex) continue;
                        float d = Vector2.Distance(_dragCurrentPos, _centers[j]);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestJ = j;
                        }
                    }
                    
                    if (bestJ != _dragIndex)
                    {
                        var tmp = _slotOf[_dragIndex];
                        _slotOf[_dragIndex] = _slotOf[bestJ];
                        _slotOf[bestJ] = tmp;
                    }
                }

                _isDragging = false;
                _dragIndex = -1;
            }
        }
    }   

    private void SmoothToTargets(float dt)
    {
        float alpha = 1f - MathF.Exp(-LerpSpeed * MathF.Max(0f, dt));

        // if (Childrens.Count < _centers.Count)
        // {
        //     for (int i = Childrens.Count; i < _centers.Count; i++)
        //         _displayCenters.Add(_centers[i]);
        // }
        // else if (Childrens.Count > _centers.Count)
        // {
        //     Childrens.RemoveRange(_centers.Count, Childrens.Count - _centers.Count);
        // }

        for (int i = 0; i < Childrens.Count; i++)
        {
            if (_isDragging && i == _dragIndex)
            {
                Childrens[i].Position = _dragCurrentPos;
                continue;
            }

            Childrens[i].Position = Vector2.Lerp(Childrens[i].Position, _centers[i], alpha);
            if (Vector2.DistanceSquared(Childrens[i].Position, _centers[i]) < 0.5f)
                Childrens[i].Position = _centers[i];
        }

        _displayRadius = _displayRadius + (_radius - _displayRadius) * alpha;
        if (MathF.Abs(_displayRadius - _radius) < 0.1f)
            _displayRadius = _radius;
    }
    
    private void ComputeLayout()
    {
        int count = Childrens?.Count ?? 0;
        if (count <= 0 || Collider.Width <= 0 || Collider.Height <= 0)
        {
            _centers.Clear();
            _radius = 0f;
            return;
        }

        _centers.Clear();

        var c = Collider;
        float s = MathF.Max(0f, Spacing);

        float bestR = 0f;
        int bestRows = 1;
        int[] bestRowCounts = Array.Empty<int>();

        for (int rows = 1; rows <= count; rows++)
        {
            int basePerRow = count / rows;
            int remainder = count % rows;

            int[] rowCounts = new int[rows];
            for (int i = 0; i < rows; i++)
                rowCounts[i] = basePerRow + (i < remainder ? 1 : 0);

            float widthBound = float.PositiveInfinity;
            bool valid = true;
            for (int i = 0; i < rows; i++)
            {
                int k = rowCounts[i];
                if (k == 0) { valid = false; break; }
                float bound = (c.Width - (k - 1) * s) / (2f * k);
                widthBound = MathF.Min(widthBound, bound);
            }
            
            if (!valid) continue;

            float heightBound = (c.Height - (rows - 1) * s) / (2f * rows);

            float rCandidate = MathF.Min(MathF.Min(MaxRadius, DefaultRadius), MathF.Min(widthBound, heightBound));
            if (rCandidate > bestR)
            {
                bestR = rCandidate;
                bestRows = rows;
                bestRowCounts = rowCounts;
            }
        }

        if (bestR <= 0f)
        {
            bestR = MinRadius;
            int bestCols = Math.Max(1, (int)MathF.Floor((c.Width + s) / (2f * bestR + s)));
            bestCols = Math.Min(bestCols, count);
            bestRows = (int)MathF.Ceiling(count / (float)bestCols);

            int basePerRow = count / bestRows;
            int remainder = count % bestRows;
            bestRowCounts = new int[bestRows];
            for (int i = 0; i < bestRows; i++)
                bestRowCounts[i] = basePerRow + (i < remainder ? 1 : 0);
        }

        _radius = MathF.Max(MinRadius, MathF.Min(bestR, MaxRadius));

        float totalHeight = bestRows * 2f * _radius + (bestRows - 1) * s;
        float topY = c.Y + (c.Height - totalHeight) / 2f;

        _slots.Clear();
        int idx = 0;
        for (int row = 0; row < bestRows; row++)
        {
            int k = bestRowCounts[row];
            float rowWidth = k * 2f * _radius + (k - 1) * s;
            float startX = c.X + (c.Width - rowWidth) / 2f;
            float centerY = topY + _radius + row * (2f * _radius + s);

            for (int j = 0; j < k; j++)
            {
                if (idx >= count) break;
                float centerX = startX + _radius + j * (2f * _radius + s);
                _slots.Add(new Vector2(centerX, centerY));
                idx++;
            }
        }

        if (_slotOf.Count != count)
        {
            _slotOf = new List<int>(count);
            for (int i = 0; i < count; i++) _slotOf.Add(i);
        }
        else
        {
            for (int i = 0; i < _slotOf.Count; i++)
            {
                if (_slots.Count == 0) _slotOf[i] = 0;
                else if (_slotOf[i] < 0) _slotOf[i] = 0;
                else if (_slotOf[i] >= _slots.Count) _slotOf[i] = _slots.Count - 1;
            }
        }
        
        _centers.Clear();
        for (int i = 0; i < count; i++)
        {
            int sIdx = _slotOf[i];
            if (sIdx < 0) sIdx = 0;
            else if (sIdx >= _slots.Count) sIdx = _slots.Count - 1;
            _centers.Add(_slots[sIdx]);
        }
    }
}