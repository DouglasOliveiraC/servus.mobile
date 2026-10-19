#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
#endif

namespace Servus.Mobile.Services.Notifications;

public class LocalNotifier : ILocalNotifier
{
#if ANDROID
    // Same id the API sends in AndroidNotification.ChannelId (FirebasePushSender).
    private const string ChannelId = "servus_alerts";
    private int _nextNotificationId;
#endif

    public async Task EnsurePermissionAsync()
    {
#if ANDROID
        EnsureChannel(Platform.AppContext);
#endif

        // Runtime grant only exists from Android 13; below that CheckStatus already reports
        // Granted and nothing is shown to the user. A refusal is final for this call: the
        // app works the same without notifications.
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (await Permissions.CheckStatusAsync<Permissions.PostNotifications>() != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Servus] Notification permission request failed: {ex}");
        }
    }

    public void Show(string title, string message, string? eventName = null)
    {
#if ANDROID
        var context = Platform.AppContext;
        var manager = NotificationManagerCompat.From(context);
        if (!manager.AreNotificationsEnabled())
        {
            return;
        }

        EnsureChannel(context);

        var launchIntent = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!)!;
        launchIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        launchIntent.PutExtra("event", eventName);
        var notificationId = Interlocked.Increment(ref _nextNotificationId);
        // Request code per notification: with a shared one, UpdateCurrent would rewrite the
        // extras of every pending notification to the latest event.
        var contentIntent = PendingIntent.GetActivity(context, notificationId, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Resource.Mipmap.appicon)!
            .SetContentTitle(title)!
            .SetContentText(message)!
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))!
            .SetPriority(NotificationCompat.PriorityHigh)!
            .SetAutoCancel(true)!
            .SetContentIntent(contentIntent)!
            .Build();

        manager.Notify(notificationId, notification);
#endif
    }

#if ANDROID
    private static void EnsureChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "Escalas e trocas", NotificationImportance.High);
        ((NotificationManager)context.GetSystemService(Context.NotificationService)!).CreateNotificationChannel(channel);
    }
#endif
}
