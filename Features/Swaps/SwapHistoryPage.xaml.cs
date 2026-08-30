namespace Servus.Mobile.Features.Swaps;

public partial class SwapHistoryPage : ContentPage
{
    private readonly SwapHistoryViewModel _viewModel;

    public SwapHistoryPage(SwapHistoryViewModel viewModel)
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
