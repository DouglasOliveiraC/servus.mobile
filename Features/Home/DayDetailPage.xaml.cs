namespace Servus.Mobile.Features.Home;

public partial class DayDetailPage : ContentPage
{
    private readonly DayDetailViewModel _viewModel;

    public DayDetailPage(DayDetailViewModel viewModel)
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
