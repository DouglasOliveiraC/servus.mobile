using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using Servus.Mobile.Services.Push;

namespace Servus.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	// "event" is set both by LocalNotifier and by FCM, which copies the push data into the
	// launch intent's extras when the user taps a notification shown in the background.
	private const string NotificationEventExtra = "event";

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// A recreated activity (rotation, process restore) still carries the original intent;
		// handling it again would reopen the screen the user already left.
		if (savedInstanceState is null)
		{
			NotificationLaunch.Handle(Intent?.GetStringExtra(NotificationEventExtra), appIsRunning: false);
		}
	}

	protected override void OnNewIntent(Intent? intent)
	{
		base.OnNewIntent(intent);
		NotificationLaunch.Handle(intent?.GetStringExtra(NotificationEventExtra), appIsRunning: true);
	}
}
