using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Raylib_cs;

namespace App.Base;

public class Header : IDisposable
{
    private bool _isDragging = false;
    private Vector2 _mouseStartPos = Vector2.Zero;
    private Vector2 _windowStartPos = Vector2.Zero;

    private FontFamily _fontFamily;
    private FontFamily _fontFamilyVersion;
    private bool _isNear = false;
    private const int HeaderHeight = 60;
    private const float ButtonRadius = 10f;

    private bool _showAddFriend = false;
    private string _usernameInput = string.Empty;
    
    public Texture2D _textureMainPic;
    public Header()
    {
        _textureMainPic = Resources.Texture("Images\\Browse.png");

        Engine.Managers.Scenes.OnScenePushed += () =>
        {
            MainBackground.Instance.AnimateSpeedChange(3f, 0.7f);
        };
    }

    public void DemoFriendsUpdate(float deltaTime)
    {
        if (Context.FriendsManager == null)
            return;

        var localMousePos = Raylib.GetMousePosition();

        // Check click on add friend button
        var addBtnPos = new Vector2(50, 80 + Context.FriendsManager.Friends.Count * 30);
        if (Raylib.CheckCollisionPointCircle(localMousePos, addBtnPos, 12) && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            _showAddFriend = !_showAddFriend;
            _usernameInput = string.Empty;
        }

        // Handle username input
        if (_showAddFriend)
        {
            int key = Raylib.GetCharPressed();
            while (key > 0)
            {
                if (key >= 32 && key <= 125)
                {
                    _usernameInput += (char)key;
                }
                key = Raylib.GetCharPressed();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _usernameInput.Length > 0)
            {
                _usernameInput = _usernameInput.Substring(0, _usernameInput.Length - 1);
            }

            // Press Enter to add friend
            if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrWhiteSpace(_usernameInput))
            {
                _ = Context.FriendsManager.AddFriendAsync(_usernameInput);
                _showAddFriend = false;
                _usernameInput = string.Empty;
            }
        }

        // Click on friend to initiate call (TODO: implement later)
        for (int i = 0; i < Context.FriendsManager.Friends.Count; i++)
        {
            var friendPos = new Vector2(50, 80 + i * 30);
            var friendRect = new Raylib_cs.Rectangle(50, friendPos.Y - 10, 200, 25);
            
            if (Raylib.CheckCollisionPointRec(localMousePos, friendRect) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                var friend = Context.FriendsManager.Friends[i];
                System.Services.Logger.Write(System.Services.Logger.Type.Info, $"Clicked on friend: {friend.Username}");
                // TODO: Initiate call
                break;
            }
        }

        // Handle pending friend request buttons (accept/reject)
        for (int i = 0; i < Context.FriendsManager.PendingRequests.Count; i++)
        {
            var request = Context.FriendsManager.PendingRequests[i];
            var yPos = 80 + i * 35;
            
            // Accept button (checkmark)
            var acceptBtnPos = new Vector2(340, yPos);
            if (Raylib.CheckCollisionPointCircle(localMousePos, acceptBtnPos, 10) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _ = Context.FriendsManager.AcceptFriendAsync(request.FriendshipId);
                break;
            }
            
            // Reject button (X)
            var rejectBtnPos = new Vector2(365, yPos);
            if (Raylib.CheckCollisionPointCircle(localMousePos, rejectBtnPos, 10) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _ = Context.FriendsManager.RejectFriendAsync(request.FriendshipId);
                break;
            }
        }
    }
    
    public void DemoFriendsDraw()
    {
        if (Context.FriendsManager == null)
            return;

        var smallFont = new FontFamily
        {
            Size = 16,
            Font = Resources.FontEx("Midami-Normal.ttf", 16),
            Rotation = 0,
            Spacing = 1f,
            Color = Color.White
        };

        var tinyFont = new FontFamily
        {
            Size = 14,
            Font = Resources.FontEx("Midami-Normal.ttf", 14),
            Rotation = 0,
            Spacing = 1f,
            Color = new Color(150, 150, 150, 255)
        };

        // Draw friends list title
        Text.DrawPro(smallFont, "Friends:", new Vector2(60, 60));

        // Draw friends
        for (int i = 0; i < Context.FriendsManager.Friends.Count; i++)
        {
            var friend = Context.FriendsManager.Friends[i];
            var yPos = 80 + i * 30;
            
            // Online indicator
            var indicatorColor = friend.IsOnline ? Color.Green : Color.Gray;
            Raylib.DrawCircle(65, yPos, 4, indicatorColor);
            
            // Friend name
            var nameColor = friend.IsOnline ? Color.White : new Color(150, 150, 150, 255);
            smallFont.Color = nameColor;
            Text.DrawPro(smallFont, friend.Username, new Vector2(75, yPos - 8));
        }

        // Draw add friend button (+)
        var addBtnY = 80 + Context.FriendsManager.Friends.Count * 30;
        var addBtnPos = new Vector2(60, addBtnY);
        var mousePos = Raylib.GetMousePosition();
        var isHoveringAdd = Raylib.CheckCollisionPointCircle(mousePos, addBtnPos, 12);
        
        var addBtnColor = isHoveringAdd ? Color.Green : new Color(100, 100, 100, 255);
        Raylib.DrawCircle((int)addBtnPos.X, (int)addBtnPos.Y, 12, addBtnColor);
        Raylib.DrawLine((int)addBtnPos.X - 6, (int)addBtnPos.Y, (int)addBtnPos.X + 6, (int)addBtnPos.Y, Color.White);
        Raylib.DrawLine((int)addBtnPos.X, (int)addBtnPos.Y - 6, (int)addBtnPos.X, (int)addBtnPos.Y + 6, Color.White);

        // Draw add friend panel
        if (_showAddFriend)
        {
            // Background panel
            Raylib.DrawRectangle(45, addBtnY + 20, 250, 100, new Color(30, 30, 30, 240));
            Raylib.DrawRectangleLines(45, addBtnY + 20, 250, 100, new Color(100, 100, 100, 255));
            
            // Username input
            Text.DrawPro(tinyFont, "Enter username:", new Vector2(55, addBtnY + 30));
            Raylib.DrawRectangle(50, addBtnY + 50, 240, 30, new Color(50, 50, 50, 255));
            Raylib.DrawRectangleLines(50, addBtnY + 50, 240, 30, new Color(100, 100, 100, 255));
            
            smallFont.Color = Color.White;
            var displayInput = _usernameInput + (DateTime.Now.Millisecond % 1000 < 500 ? "_" : "");
            Text.DrawPro(smallFont, displayInput, new Vector2(55, addBtnY + 58));
            
            // Hint
            tinyFont.Color = new Color(150, 150, 150, 255);
            Text.DrawPro(tinyFont, "Press Enter to send request", new Vector2(55, addBtnY + 90));
        }

        // Draw pending friend requests (right side)
        if (Context.FriendsManager.PendingRequests.Count > 0)
        {
            smallFont.Color = Color.White;
            Text.DrawPro(smallFont, "Requests:", new Vector2(300, 60));
            
            for (int i = 0; i < Context.FriendsManager.PendingRequests.Count; i++)
            {
                var request = Context.FriendsManager.PendingRequests[i];
                var yPos = 80 + i * 35;
                
                // Request username
                smallFont.Color = Color.Yellow;
                Text.DrawPro(smallFont, request.FromUsername, new Vector2(300, yPos - 8));
                
                // Accept button (green checkmark)
                var acceptBtnPos = new Vector2(340, yPos);
                var isHoveringAccept = Raylib.CheckCollisionPointCircle(mousePos, acceptBtnPos, 10);
                var acceptColor = isHoveringAccept ? Color.Green : new Color(0, 150, 0, 255);
                Raylib.DrawCircle((int)acceptBtnPos.X, (int)acceptBtnPos.Y, 10, acceptColor);
                // Draw checkmark
                Raylib.DrawLine((int)acceptBtnPos.X - 4, (int)acceptBtnPos.Y, (int)acceptBtnPos.X - 1, (int)acceptBtnPos.Y + 3, Color.White);
                Raylib.DrawLine((int)acceptBtnPos.X - 1, (int)acceptBtnPos.Y + 3, (int)acceptBtnPos.X + 4, (int)acceptBtnPos.Y - 4, Color.White);
                
                // Reject button (red X)
                var rejectBtnPos = new Vector2(365, yPos);
                var isHoveringReject = Raylib.CheckCollisionPointCircle(mousePos, rejectBtnPos, 10);
                var rejectColor = isHoveringReject ? Color.Red : new Color(150, 0, 0, 255);
                Raylib.DrawCircle((int)rejectBtnPos.X, (int)rejectBtnPos.Y, 10, rejectColor);
                // Draw X
                Raylib.DrawLine((int)rejectBtnPos.X - 4, (int)rejectBtnPos.Y - 4, (int)rejectBtnPos.X + 4, (int)rejectBtnPos.Y + 4, Color.White);
                Raylib.DrawLine((int)rejectBtnPos.X + 4, (int)rejectBtnPos.Y - 4, (int)rejectBtnPos.X - 4, (int)rejectBtnPos.Y + 4, Color.White);
            }
        }
    }
    
    public void Update(float deltaTime)
    {
        Platforms.Windows.Mouse.GetCursorPos(out var globalMousePos);
        
        Font font = Resources.FontEx("Midami-Normal.ttf", 26);
        
        _fontFamily = new()
        {
            Size = 26,
            Font = font,
            Rotation = 0,
            Spacing = 1f,
            Color = Color.White
        };
        
        _fontFamilyVersion = new()
        {
            Size = 20,
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 20),
            Rotation = 0,
            Spacing = 1f,
            Color = new Color(255, 255, 255, 80)
        };
        
        Vector2 mousePos = new Vector2(globalMousePos.X, globalMousePos.Y);
        Vector2 localMousePos = Raylib.GetMousePosition();
        
        _isNear = Vector2.Distance(localMousePos, new Vector2(Raylib.GetRenderWidth() - 40, 22)) < 38;
        
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Vector2 minimizeBtnPos = new Vector2(Raylib.GetRenderWidth() - 55, 25);
            Vector2 closeBtnPos = new Vector2(Raylib.GetRenderWidth() - 30, 25);

            var minimizeBtnPosLocal = Raylib.CheckCollisionPointCircle(localMousePos, minimizeBtnPos, ButtonRadius);
            var closeBtnPosLocal = Raylib.CheckCollisionPointCircle(localMousePos, closeBtnPos, ButtonRadius);
            
            if (localMousePos.Y <= HeaderHeight && (!minimizeBtnPosLocal && !closeBtnPosLocal))
            {
                _isDragging = true;
                _mouseStartPos = mousePos;
                _windowStartPos = new Vector2(Raylib.GetWindowPosition().X, Raylib.GetWindowPosition().Y);
            }
            
            if (minimizeBtnPosLocal)
            {
                Raylib.MinimizeWindow();
            }
            else if (closeBtnPosLocal)
            {
                Environment.Exit(0);
                return;
            }
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            _isDragging = false;
            Raylib.SetMouseCursor(MouseCursor.Default);
        }
            
        if (_isDragging)
        {
            Raylib.SetMouseCursor(MouseCursor.ResizeAll);
            
            Vector2 offset = mousePos - _mouseStartPos;
            Vector2 newWindowPos = _windowStartPos + offset;
            Raylib.SetWindowPosition((int)newWindowPos.X, (int)newWindowPos.Y);
        }
        
        DemoFriendsUpdate(deltaTime);
    }
    
    public void Draw()
    {
        Text.DrawPro(_fontFamily, "MonkeySpeak", new Vector2(Raylib.GetRenderWidth() / 2, 24));
        
        Color yellowColor = _isNear ? Color.Yellow : new Color(70, 70, 70);
        Color redColor = _isNear ? Color.Red : new Color(70, 70, 70);
        
        Raylib.DrawCircle(Raylib.GetRenderWidth() - 55, 25, 6.5f, yellowColor);
        Raylib.DrawCircle(Raylib.GetRenderWidth() - 30, 25, 6.5f, redColor);
        
        Text.DrawPro(
            _fontFamilyVersion, 
            $"e:old:a:{Context.AppConfig.VersionName}.{Context.AppConfig.Version}", 
            new Vector2(Raylib.GetScreenWidth() - 120, Raylib.GetScreenHeight() - 25)
        );
        DemoFriendsDraw();
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(_textureMainPic);
    }
}