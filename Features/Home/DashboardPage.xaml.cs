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
        // OnAppearing does not fire when the app returns from the background, which is
        // exactly when the greeting is most likely to be stale.
        if (Window is not null)
        {
            Window.Resumed += OnWindowResumed;
        }
        _viewModel.LoadCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Window is not null)
        {
            Window.Resumed -= OnWindowResumed;
        }
    }

    private void OnWindowResumed(object? sender, EventArgs e) => _viewModel.RefreshClockLabels();
}
