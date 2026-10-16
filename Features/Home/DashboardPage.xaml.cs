using Servus.Mobile.Services.Navigation;

namespace Servus.Mobile.Features.Home;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }

    // Android back closes an open drawer first, as on any native app, instead of leaving the app.
    protected override bool OnBackButtonPressed()
    {
        if (Drawer.DrawerSettings.IsOpen)
        {
            Drawer.DrawerSettings.IsOpen = false;
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void OnMenuTapped(object? sender, TappedEventArgs e) => Drawer.ToggleDrawer();

    private void OnHomeItemTapped(object? sender, TappedEventArgs e) => Drawer.DrawerSettings.IsOpen = false;

    // Drawer item handlers live here, not in the view model: the drawer must close before
    // navigating, otherwise it is still open when the user comes back to the Home.
    private async void OnShortcutTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not HomeShortcut shortcut) return;

        Drawer.DrawerSettings.IsOpen = false;
        await _viewModel.GoToQuickActionCommand.ExecuteAsync(shortcut.Route);
    }

    private async void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        Drawer.DrawerSettings.IsOpen = false;
        await _viewModel.GoToQuickActionCommand.ExecuteAsync(Routes.Profile);
    }

    private async void OnLogoutTapped(object? sender, TappedEventArgs e)
    {
        Drawer.DrawerSettings.IsOpen = false;
        await _viewModel.LogoutCommand.ExecuteAsync(null);
    }
}
