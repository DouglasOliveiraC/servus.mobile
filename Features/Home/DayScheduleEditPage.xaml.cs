namespace Servus.Mobile.Features.Home;

public partial class DayScheduleEditPage : ContentPage
{
    private readonly DayScheduleEditViewModel _viewModel;

    public DayScheduleEditPage(DayScheduleEditViewModel viewModel)
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
