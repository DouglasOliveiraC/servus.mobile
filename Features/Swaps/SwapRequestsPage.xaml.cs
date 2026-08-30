namespace Servus.Mobile.Features.Swaps;

public partial class SwapRequestsPage : ContentPage
{
    private readonly SwapRequestsViewModel _viewModel;

    public SwapRequestsPage(SwapRequestsViewModel viewModel)
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
}
