namespace Servus.Mobile.Features.Admin;

public partial class RosterMemberProfilePage : ContentPage
{
    private readonly RosterMemberProfileViewModel _viewModel;

    public RosterMemberProfilePage(RosterMemberProfileViewModel viewModel)
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
