using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Shapes;
using Servus.Mobile.Features.Home;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Navigation;

namespace Servus.Mobile.Features.Menu;

public partial class AppMenuItem(string title, string subtitle, Geometry icon, string route) : ObservableObject
{
    public string Title { get; } = title;

    public string Subtitle { get; } = subtitle;

    public Geometry Icon { get; } = icon;

    public string Route { get; } = route;

    [ObservableProperty]
    public partial bool IsActive { get; set; }
}

// Singleton behind the Shell flyout: one menu for the whole app, fed by the Dashboard load
// (which already knows the user and whether they administer anything) instead of fetching
// the same data a second time.
public partial class AppMenuViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private string[] _locationSegments = [];

    public AppMenuViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        BuildItems(isAdmin: false);
    }

    [ObservableProperty]
    public partial string? UserEmail { get; set; }

    [ObservableProperty]
    public partial string UserInitial { get; set; } = "?";

    [ObservableProperty]
    public partial string ProfileLabel { get; set; } = "Servidor";

    public ObservableCollection<AppMenuItem> Items { get; } = [];

    public void SetUser(string? email, string initial, bool isAdmin)
    {
        UserEmail = email;
        UserInitial = initial;
        ProfileLabel = isAdmin ? "Administrador" : "Servidor";
        BuildItems(isAdmin);
    }

    // The deepest top-level destination in the stack is the active one, so MemberDetailPage
    // (pushed from Membros) still highlights Membros, and a day detail pushed from the Home
    // keeps Início highlighted.
    public void SyncActiveRoute(string location)
    {
        _locationSegments = location.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var active = _locationSegments.LastOrDefault(segment => Items.Any(i => i.Route == segment));
        foreach (var item in Items)
        {
            item.IsActive = item.Route == active;
        }
    }

    private void BuildItems(bool isAdmin)
    {
        Items.Clear();
        Items.Add(new AppMenuItem("Início", "Visão geral", HomeIcons.LayoutGrid, Routes.Dashboard));
        if (isAdmin)
        {
            Items.Add(new AppMenuItem("Minhas organizações", "Escalas, filas, turnos e publicação", HomeIcons.Building, Routes.OrganizationList));
            Items.Add(new AppMenuItem("Membros", "Cadastro e vínculo às filas", HomeIcons.Users, Routes.Members));
        }

        Items.Add(new AppMenuItem("Trocas de plantão", "Pedidos enviados e recebidos", HomeIcons.Repeat, Routes.SwapRequests));
        Items.Add(new AppMenuItem("Minhas ausências", "Quando não posso ser escalado", HomeIcons.CalendarOff, Routes.Unavailability));
        Items.Add(new AppMenuItem("Entrar com código", "Vincular-me a uma organização", HomeIcons.KeyRound, Routes.JoinOrganization));
        Items.Add(new AppMenuItem("Meu perfil", "Dados da conta e histórico", HomeIcons.User, Routes.Profile));

        SyncActiveRoute(string.Join('/', _locationSegments));
    }

    [RelayCommand]
    private Task OpenAsync(AppMenuItem item) => OpenRouteAsync(item.Route);

    [RelayCommand]
    private Task OpenProfileAsync() => OpenRouteAsync(Routes.Profile);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        CloseFlyout();
        await _authService.LogoutAsync();
        await _navigationService.GoToRootAsync(Routes.Login);
    }

    // Always rebuilt from the Home (//Dashboard/route) rather than pushed onto whatever is open:
    // the menu is top-level navigation, so Back from any menu destination lands on the Home
    // instead of unwinding an ever-growing stack.
    private async Task OpenRouteAsync(string route)
    {
        CloseFlyout();
        if (_locationSegments.LastOrDefault() == route) return;

        await _navigationService.GoToRootAsync(route == Routes.Dashboard
            ? Routes.Dashboard
            : $"{Routes.Dashboard}/{route}");
    }

    private static void CloseFlyout()
    {
        if (Shell.Current is { } shell)
        {
            shell.FlyoutIsPresented = false;
        }
    }
}
