namespace Servus.Mobile.Services.Notifications;

/// <summary>Shows a push notification that arrived while the app was in the foreground — in the
/// background Android displays it by itself, on the same channel.</summary>
public interface ILocalNotifier
{
    /// <summary>Also creates the notification channel, which has to exist before the first push
    /// arrives with the app closed.</summary>
    Task EnsurePermissionAsync();

    /// <summary><paramref name="eventName"/> rides along in the tap intent, the same extra a
    /// background push carries, so both open the same screen.</summary>
    void Show(string title, string message, string? eventName = null);
}
