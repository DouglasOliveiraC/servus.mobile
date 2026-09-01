using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Servus.Mobile;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		_services = services;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override void OnSleep()
	{
		// ForceFlush é best-effort — não há garantia de conclusão antes do OS suspender o
		// processo, mas é a mitigação documentada pelo OpenTelemetry .NET para esse cenário.
		// TracerProvider só está registrado no DI quando a connection string está configurada
		// (ver MauiProgram.ConfigureTelemetry) — daí o GetService em vez de GetRequiredService.
		_services.GetService<TracerProvider>()?.ForceFlush(2000);
	}
}