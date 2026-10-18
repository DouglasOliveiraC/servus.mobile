using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Realtime;

namespace Servus.Mobile;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		_services = services;
		services.GetRequiredService<ISessionManager>().SessionExpired += OnSessionExpired;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell(_services.GetRequiredService<Features.Menu.AppMenuViewModel>()));
	}

	protected override void OnSleep()
	{
		// ForceFlush is best effort — nothing guarantees it finishes before the OS suspends the
		// process, but it is the mitigation OpenTelemetry .NET documents for this case.
		// TracerProvider is only registered when the connection string is configured (see
		// MauiProgram.ConfigureTelemetry), hence GetService instead of GetRequiredService.
		_services.GetService<TracerProvider>()?.ForceFlush(2000);
	}

	// ConnectAsync is a no-op while the hub is live and without a session, so this only acts
	// when the connection was lost while the app was in the background.
	protected override void OnResume()
	{
		_ = _services.GetRequiredService<IRealtimeClient>().ConnectAsync();
	}

	// Raised from whatever background request found the refresh token rejected; the tokens are
	// already cleared, so all that is left is leaving the authenticated screens.
	private void OnSessionExpired(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			if (Shell.Current is null || Shell.Current.CurrentState.Location.OriginalString.Contains(Routes.Login))
			{
				return;
			}

			await _services.GetRequiredService<IRealtimeClient>().DisconnectAsync();
			await _services.GetRequiredService<INavigationService>().GoToRootAsync(Routes.Login);
			await Shell.Current.DisplayAlertAsync("Sessão expirada", "Entre novamente para continuar.", "OK");
		});
	}
}
