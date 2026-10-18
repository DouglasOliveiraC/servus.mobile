namespace Servus.Mobile.Services.Notifications;

/// <summary>System notification for a realtime event. Only reaches the user while the app
/// process (and so the SignalR connection) is alive — there is no push provider behind it.</summary>
public interface ILocalNotifier
{
    Task EnsurePermissionAsync();

    void Show(string title, string message);
}
