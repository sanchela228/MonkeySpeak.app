using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace Engine.Managers;

public static class Pointer
{
    private static Node? _pressedNode;
    private static Node? _hoveredNode;
    
    public static Node? PressedNode => _pressedNode;
    public static bool Pressed => _pressedNode != null; 
    public static void Clear() => _pressedNode = null;
    public static void SetPressedNode(Node? node) => _pressedNode = node;
    
    public static bool Hovered => _hoveredNode != null;
    public static void ClearHovered() => _hoveredNode = null;
    public static Node? HoveredNode => _hoveredNode;
    public static void SetHoveredNode(Node? node) => _hoveredNode = node;
}
