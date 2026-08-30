namespace Servus.Mobile.Features.Admin;

public partial class SubrosterDetailPage : ContentPage
{
    private readonly SubrosterDetailViewModel _viewModel;

    public SubrosterDetailPage(SubrosterDetailViewModel viewModel)
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
