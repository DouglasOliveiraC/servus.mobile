namespace Servus.Mobile.Services.Navigation;

public interface INavigationService
{
    Task GoToAsync(string route);

    Task GoToAsync(string route, IDictionary<string, object> parameters);

    Task GoToRootAsync(string route);

    Task GoBackAsync();
}
