namespace Servus.Mobile.Features.Members;

public partial class MemberDetailPage : ContentPage
{
    private readonly MemberDetailViewModel _viewModel;

    public MemberDetailPage(MemberDetailViewModel viewModel)
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
