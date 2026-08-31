namespace Servus.Mobile.Features.Schedule;

public partial class RosterSchedulePage : ContentPage
{
    private readonly RosterScheduleViewModel _viewModel;

    public RosterSchedulePage(RosterScheduleViewModel viewModel)
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
