using System.Collections.ObjectModel;

namespace ZakYip.Singulation.MauiApp.Services;

/// <summary>
/// Message types for notifications
/// </summary>
public enum MessageType
{
    Success,
    Warning,
    Error,
    Info
}

/// <summary>
/// Notification message model
/// </summary>
public class NotificationMessage
{
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Icon => Type switch
    {
        MessageType.Success => "\uf058", // circle-check
        MessageType.Warning => "\uf071", // triangle-exclamation
        MessageType.Error => "\uf057",   // circle-xmark
        MessageType.Info => "\uf05a",    // circle-info
        _ => "\uf05a"
    };
    
    public Color BackgroundColor => Type switch
    {
        MessageType.Success => Color.FromRgba(76, 175, 80, 230),    // Green with transparency
        MessageType.Warning => Color.FromRgba(255, 152, 0, 230),    // Orange with transparency
        MessageType.Error => Color.FromRgba(244, 67, 54, 230),      // Red with transparency
        MessageType.Info => Color.FromRgba(33, 150, 243, 230),      // Blue with transparency
        _ => Color.FromRgba(158, 158, 158, 230)                     // Gray with transparency
    };
    
    public Color TextColor => Colors.White;
}

/// <summary>
/// Service for showing toast-style notifications
/// </summary>
public class NotificationService
{
    private static NotificationService? _instance;
    public static NotificationService Instance => _instance ??= new NotificationService();
    
    public ObservableCollection<NotificationMessage> Notifications { get; } = new();
    
    private NotificationService() { }
    
    /// <summary>
    /// Show a notification message
    /// </summary>
    public void Show(string message, MessageType type = MessageType.Info, int durationSeconds = 3)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var notification = new NotificationMessage
            {
                Message = message,
                Type = type
            };
            
            Notifications.Add(notification);
            
            // Auto-remove after duration
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
            Notifications.Remove(notification);
        });
    }
    
    /// <summary>
    /// Show success message
    /// </summary>
    public void ShowSuccess(string message, int durationSeconds = 3)
    {
        Show(message, MessageType.Success, durationSeconds);
    }
    
    /// <summary>
    /// Show warning message
    /// </summary>
    public void ShowWarning(string message, int durationSeconds = 4)
    {
        Show(message, MessageType.Warning, durationSeconds);
    }
    
    /// <summary>
    /// Show error message
    /// </summary>
    public void ShowError(string message, int durationSeconds = 5)
    {
        Show(message, MessageType.Error, durationSeconds);
    }
    
    /// <summary>
    /// Show info message
    /// </summary>
    public void ShowInfo(string message, int durationSeconds = 3)
    {
        Show(message, MessageType.Info, durationSeconds);
    }
    
    /// <summary>
    /// Clear all notifications
    /// </summary>
    public void ClearAll()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Notifications.Clear();
        });
    }
}
