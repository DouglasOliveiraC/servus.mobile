namespace Servus.Mobile.Features.JoinOrganization;

public partial class JoinOrganizationPage : ContentPage
{
    public JoinOrganizationPage(JoinOrganizationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
