namespace Servus.Mobile.Features.Admin;

public partial class OrganizationListPage : ContentPage
{
    private readonly OrganizationListViewModel _viewModel;

    public OrganizationListPage(OrganizationListViewModel viewModel)
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
