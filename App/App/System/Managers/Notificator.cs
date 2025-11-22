using System.Numerics;
using Engine;
using Engine.Helpers;
using Engine.Managers;
using Graphics;
using Raylib_cs;
using Rectangle = Raylib_cs.Rectangle;

namespace App.System.Managers;

public static class Notificator
{
    private static readonly Queue<Notification> _notifications = new Queue<Notification>();
    private static Notification _currentNotification;
    private static float _currentDisplayTime = 0f;
    private static float _currentPauseTime = 0f;
    private static NotificationState _state = NotificationState.Idle;
    
    private static Sound _sound = Resources.Sound("weak-soft-knock-on-wood.wav");
    
    public static float DisplayDuration { get; set; } = 4.0f;
    public static float PauseBetweenNotifications { get; set; } = 0.7f;
    
    public static event Action<Notification> OnNotificationAdded;
    public static event Action<Notification> OnNotificationShow;
    public static event Action OnNotificationHide;
    public static event Action OnAllNotificationsCleared;
    
    private static readonly List<Notification> _notificationHistory = new List<Notification>();
    
    public static void AddNotification(string message, string title, NotificationType type = NotificationType.Info)
    {
        var notification = new Notification(message, title, type);
        _notifications.Enqueue(notification);
        _notificationHistory.Add(notification);
        
        OnNotificationAdded?.Invoke(notification);
        
        if (_state == NotificationState.Idle)
        {
            ShowNextNotification();
        }
    }
    
    private static void ShowNextNotification()
    {
        if (_notifications.Count == 0)
        {
            _state = NotificationState.Idle;
            return;
        }
        
        _currentNotification = _notifications.Dequeue();
        _currentDisplayTime = 0f;
        _state = NotificationState.Showing;


        Raylib.SetSoundVolume(_sound, 0.5f);
        Raylib.PlaySound(_sound);
        
        OnNotificationShow?.Invoke(_currentNotification);
    }
    
    public static void Update(float deltaTime)
    {
        switch (_state)
        {
            case NotificationState.Showing:
                _currentDisplayTime += deltaTime;
                
                if (_currentDisplayTime >= DisplayDuration)
                {
                    // Скрываем уведомление и переходим к паузе
                    OnNotificationHide?.Invoke();
                    _currentPauseTime = 0f;
                    _state = NotificationState.Pausing;
                    _currentNotification = null;
                }
                break;
                
            case NotificationState.Pausing:
                _currentPauseTime += deltaTime;
                
                if (_currentPauseTime >= PauseBetweenNotifications)
                {
                    // Пауза закончилась, показываем следующее уведомление
                    ShowNextNotification();
                }
                break;
        }
    }
    
    private static float _fadeInDuration = 0.2f; 
    private static float _fadeOutDuration = 0.2f;
    public static void Draw()
    {
        if (_state != NotificationState.Showing || _currentNotification == null)
            return;

        float alpha = CalculateAlpha();
        var color = new Color( 40, 40, 40, (int) (alpha * 150));
        
        var width = 280;
        
        Rectangle notificationRect = new Rectangle(
            Raylib.GetScreenWidth() / 2 - width / 2,
            55,
            width,
            68
        );
        
        Raylib.DrawRectangleRounded(notificationRect, 0.2f, 16, color);
        Raylib.DrawRectangleRoundedLinesEx(notificationRect, 0.2f, 16, 1, new Color( 50, 50, 50, (int) (alpha * 255)));

        FontFamily fontFamilyTitle = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 20),
            Size = 20,
            Color = Color.White
        };
        
        FontFamily fontFamilyText = new FontFamily()
        {
            Font = Resources.FontEx("JetBrainsMonoNL-Regular.ttf", 20),
            Size = 20,
            Color = Color.White
        };

        var vec2title = new Vector2(notificationRect.X + notificationRect.Width / 2, (int)notificationRect.Y + 20);
        var vec2text = new Vector2(notificationRect.X + notificationRect.Width / 2, (int)notificationRect.Y + 48);
            
        Text.DrawPro(fontFamilyTitle, _currentNotification.Title, vec2title);
        
        Text.DrawPro(fontFamilyText, _currentNotification.Message, vec2text);
    }
    
    private static float CalculateAlpha()
    {
        float totalTime = DisplayDuration;
        float fadeInTime = _fadeInDuration;
        float fadeOutTime = _fadeOutDuration;
        float stableTime = totalTime - fadeInTime - fadeOutTime;
    
        if (stableTime < 0)
        {
            fadeInTime = totalTime * 0.5f;
            fadeOutTime = totalTime * 0.5f;
            stableTime = 0;
        }
    
        if (_currentDisplayTime < fadeInTime)
        {
            return _currentDisplayTime / fadeInTime;
        }
        else if (_currentDisplayTime > totalTime - fadeOutTime)
        {
            float fadeOutProgress = (_currentDisplayTime - (totalTime - fadeOutTime)) / fadeOutTime;
            return 1f - fadeOutProgress;
        }
        else
        {
            return 1f;
        }
    }
    
    public static Notification GetCurrentNotification()
    {
        return _currentNotification;
    }
    
    public static List<Notification> GetAllNotifications()
    {
        return new List<Notification>(_notificationHistory);
    }
    
    public static void ClearAll()
    {
        _notifications.Clear();
        _currentNotification = null;
        _state = NotificationState.Idle;
        _currentDisplayTime = 0f;
        _currentPauseTime = 0f;
        OnAllNotificationsCleared?.Invoke();
    }
    
    public static void ClearHistory()
    {
        _notificationHistory.Clear();
    }
    
    public static bool HasActiveNotification()
    {
        return _currentNotification != null;
    }
    
    public static int GetQueueCount()
    {
        return _notifications.Count;
    }
    
    public static int GetTotalCount()
    {
        return (_currentNotification != null ? 1 : 0) + _notifications.Count;
    }
    
    public static NotificationState GetState()
    {
        return _state;
    }
    
    public class Notification(
        string message,
        string title = "Уведомление",
        NotificationType type = NotificationType.Info)
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Message { get; } = message;
        public string Title { get; } = title;
        public NotificationType Type { get; } = type;
        public DateTime CreatedAt { get; } = DateTime.Now;
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }
    
    public enum NotificationState
    {
        Idle,
        Showing,
        Pausing
    }
}