using Microsoft.Extensions.Logging;
using Servus.Mobile.Features.Home;
using Servus.Mobile.Features.Login;
using Servus.Mobile.Features.Register;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.Rosters;
using Syncfusion.Maui.Core.Hosting;

namespace Servus.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		RegisterSyncfusionLicense();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureSyncfusionCore()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
		builder.Services.AddTransient<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		});

		builder.Services.AddHttpClient<IRosterService, RosterService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<DashboardPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterSyncfusionLicense()
	{
		// Chave lida de variável de ambiente em build-time, nunca commitada.
		// Ver docs/tarefas para o passo de configuração do secret local.
		var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
		if (!string.IsNullOrWhiteSpace(licenseKey))
		{
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
		}
	}
}
