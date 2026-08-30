namespace Servus.Mobile.Features.Schedule;

public partial class SubrosterSchedulePage : ContentPage
{
    private readonly SubrosterScheduleViewModel _viewModel;

    public SubrosterSchedulePage(SubrosterScheduleViewModel viewModel)
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
