namespace Servus.Mobile.Features.Admin;

public partial class SubrosterListPage : ContentPage
{
    private readonly SubrosterListViewModel _viewModel;

    public SubrosterListPage(SubrosterListViewModel viewModel)
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
