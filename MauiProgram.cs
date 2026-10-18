using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Servus.Mobile.Features.Admin;
using Servus.Mobile.Features.CalendarOverrides;
using Servus.Mobile.Features.Home;
using Servus.Mobile.Features.JoinOrganization;
using Servus.Mobile.Features.Login;
using Servus.Mobile.Features.Profile;
using Servus.Mobile.Features.Register;
using Servus.Mobile.Features.Schedule;
using Servus.Mobile.Features.Swaps;
using Servus.Mobile.Features.Unavailability;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.CalendarOverrides;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.MemberClaimCodes;
using Servus.Mobile.Services.Members;
using Servus.Mobile.Services.Navigation;
using Servus.Mobile.Services.Organizations;
using Servus.Mobile.Services.Realtime;
using Servus.Mobile.Services.Rosters;
using Servus.Mobile.Services.SwapHistory;
using Servus.Mobile.Services.SwapRequests;
using Servus.Mobile.Services.Telemetry;
using Servus.Mobile.Services.Unavailabilities;
using Servus.Mobile.Services.Users;
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

		ConfigureBorderlessEntry();
		ConfigureTelemetry(builder);
		builder.Services.AddSingleton<INavigationService, NavigationService>();

		builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
		builder.Services.AddSingleton<ISessionManager, SessionManager>();
		// No AuthHeaderHandler here: the refresh call must not recurse into the handler that
		// triggered it.
		builder.Services.AddHttpClient(SessionManager.HttpClientName, client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		});
		builder.Services.AddTransient<AuthHeaderHandler>();
		builder.Services.AddSingleton<Servus.Mobile.Services.Notifications.ILocalNotifier, Servus.Mobile.Services.Notifications.LocalNotifier>();
		builder.Services.AddSingleton<IRealtimeClient, RealtimeClient>();

		builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		});

		builder.Services.AddHttpClient<IOrganizationService, OrganizationService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IRosterService, RosterService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IMemberService, MemberService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<ISwapRequestService, SwapRequestService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<ISwapHistoryService, SwapHistoryService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IMemberUnavailabilityService, MemberUnavailabilityService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<ICalendarDayOverrideService, CalendarDayOverrideService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IMemberClaimCodeService, MemberClaimCodeService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddHttpClient<IUserService, UserService>(client =>
		{
			client.BaseAddress = new Uri(ApiConfig.BaseUrl);
		}).AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddSingleton<Servus.Mobile.Features.Menu.AppMenuViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<DayDetailViewModel>();
		builder.Services.AddTransient<DayDetailPage>();
		builder.Services.AddTransient<DayScheduleEditViewModel>();
		builder.Services.AddTransient<DayScheduleEditPage>();

		builder.Services.AddTransient<SwapRequestsViewModel>();
		builder.Services.AddTransient<SwapRequestsPage>();
		builder.Services.AddTransient<SwapHistoryViewModel>();
		builder.Services.AddTransient<SwapHistoryPage>();

		builder.Services.AddTransient<UnavailabilityViewModel>();
		builder.Services.AddTransient<UnavailabilityPage>();

		builder.Services.AddTransient<RosterScheduleViewModel>();
		builder.Services.AddTransient<RosterSchedulePage>();

		builder.Services.AddTransient<JoinOrganizationViewModel>();
		builder.Services.AddTransient<JoinOrganizationPage>();

		builder.Services.AddTransient<CalendarOverridesViewModel>();
		builder.Services.AddTransient<CalendarOverridesPage>();

		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<ProfilePage>();

		builder.Services.AddTransient<OrganizationListViewModel>();
		builder.Services.AddTransient<OrganizationListPage>();
		builder.Services.AddTransient<RosterListViewModel>();
		builder.Services.AddTransient<RosterListPage>();
		builder.Services.AddTransient<RosterDetailViewModel>();
		builder.Services.AddTransient<RosterDetailPage>();
		builder.Services.AddTransient<RosterSettingsViewModel>();
		builder.Services.AddTransient<RosterSettingsPage>();
		builder.Services.AddTransient<RosterMemberProfileViewModel>();
		builder.Services.AddTransient<RosterMemberProfilePage>();

		builder.Services.AddTransient<Servus.Mobile.Features.Members.MembersViewModel>();
		builder.Services.AddTransient<Servus.Mobile.Features.Members.MembersPage>();
		builder.Services.AddTransient<Servus.Mobile.Features.Members.MemberDetailViewModel>();
		builder.Services.AddTransient<Servus.Mobile.Features.Members.MemberDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterSyncfusionLicense()
	{
		var licenseKey = ResolveBuildSecret(
			BuildSecrets.SyncfusionLicenseKey,
			"SYNCFUSION_LICENSE_KEY");
		if (!string.IsNullOrWhiteSpace(licenseKey))
		{
			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
		}
	}

	// Only Controls.BorderlessEntry loses the native chrome (Android underline, iOS border); it is
	// what SearchField draws inside its own pill. Plain Entry elsewhere is untouched.
	private static void ConfigureBorderlessEntry()
	{
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
		{
			if (view is not Controls.BorderlessEntry) return;
#if ANDROID
			handler.PlatformView.Background = null;
			handler.PlatformView.SetPadding(0, handler.PlatformView.PaddingTop, 0, handler.PlatformView.PaddingBottom);
#elif IOS || MACCATALYST
			handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
			handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
			handler.PlatformView.Background = null;
#endif
		});
	}

	// Sem connection string, ITelemetryService fica em modo no-op (ILogger<T> null) em vez de
	// derrubar o app — não há Application Insights provisionado em todo ambiente de
	// desenvolvimento local.
	private static void ConfigureTelemetry(MauiAppBuilder builder)
	{
		var connectionString = ResolveBuildSecret(
			BuildSecrets.ApplicationInsightsConnectionString,
			"APPINSIGHTS_CONNECTION_STRING");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
			return;
		}

		builder.Services.AddOpenTelemetry()
			.ConfigureResource(r => r.AddService("Servus.Mobile"))
			.WithTracing(tracing => tracing
				.AddSource("Servus.Mobile")
				.AddHttpClientInstrumentation()
				.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString))
			.WithLogging(logging => logging
				.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString));

		builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
	}

	/// <summary>
	/// Valor gravado no assembly em build-time, com a variável de ambiente do processo como
	/// alternativa.
	/// </summary>
	/// <remarks>
	/// A ordem importa: antes só existia a leitura de ambiente, e ela SEMPRE devolve vazio no
	/// aparelho — variável definida na máquina de build não viaja no APK. O que faz o app
	/// instalado ter telemetria é a constante; o ambiente sobrou para rodar no Windows/desktop
	/// sem recompilar (R4 no relatório 10).
	/// </remarks>
	private static string? ResolveBuildSecret(string compiledValue, string environmentVariableName)
	{
		return !string.IsNullOrWhiteSpace(compiledValue)
			? compiledValue
			: Environment.GetEnvironmentVariable(environmentVariableName);
	}
}
