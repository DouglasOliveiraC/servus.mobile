namespace Servus.Mobile.Features.Members;

public partial class MembersPage : ContentPage
{
    private readonly MembersViewModel _viewModel;

    public MembersPage(MembersViewModel viewModel)
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
