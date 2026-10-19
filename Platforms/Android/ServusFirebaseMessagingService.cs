using Android.App;
using Firebase.Messaging;
using Servus.Mobile.Services.Notifications;
using Servus.Mobile.Services.Push;

namespace Servus.Mobile;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public class ServusFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        _ = IPlatformApplication.Current?.Services.GetService<IPushRegistrationService>()?.RegisterAsync(token);
    }

    // Only reached with the app in the foreground: in the background Android shows the
    // notification part of the message by itself, without calling into the app.
    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var notification = message.GetNotification();
        if (notification is null)
        {
            return;
        }

        message.Data.TryGetValue("event", out var eventName);
        IPlatformApplication.Current?.Services.GetService<ILocalNotifier>()?.Show(notification.Title ?? "Servus", notification.Body ?? string.Empty, eventName);
    }
}
