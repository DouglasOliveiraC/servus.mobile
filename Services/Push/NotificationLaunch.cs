using Servus.Mobile.Services.Navigation;

namespace Servus.Mobile.Services.Push;

/// <summary>
/// Where a tapped push notification should take the user. On a cold start the tap arrives
/// before login and the dashboard exist, so the route is parked here and the dashboard takes it
/// once loaded; with the app already running it is followed right away.
/// </summary>
public static class NotificationLaunch
{
    private static string? _pendingRoute;

    public static void Handle(string? eventName, bool appIsRunning)
    {
        var route = RouteFor(eventName);
        if (route is null)
        {
            return;
        }

        if (!appIsRunning)
        {
            _pendingRoute = route;
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var navigation = IPlatformApplication.Current?.Services.GetService<INavigationService>();
            if (navigation is not null && Shell.Current?.CurrentState.Location.OriginalString.Contains(Routes.Login) == false)
            {
                await navigation.GoToAsync(route);
            }
        });
    }

    public static string? TakePendingRoute() => Interlocked.Exchange(ref _pendingRoute, null);

    // Schedule events land on the dashboard, which already opens there; only swaps have a
    // screen of their own to go to.
    private static string? RouteFor(string? eventName) =>
        eventName is not null && eventName.StartsWith("Swap", StringComparison.Ordinal) ? Routes.SwapRequests : null;
}
