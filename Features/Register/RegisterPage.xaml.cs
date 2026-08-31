namespace Servus.Mobile.Features.Register;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterViewModel _viewModel;

    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.RegisterSucceeded += OnRegisterSucceeded;
    }

    private async void OnRegisterSucceeded(object? sender, EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync("Conta criada", "Sua conta foi criada. Entre com o e-mail e a senha que você acabou de definir.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
