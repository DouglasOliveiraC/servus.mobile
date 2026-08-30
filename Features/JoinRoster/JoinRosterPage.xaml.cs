namespace Servus.Mobile.Features.JoinRoster;

public partial class JoinRosterPage : ContentPage
{
    public JoinRosterPage(JoinRosterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
