namespace Servus.Mobile.Services.Realtime;

public interface IRealtimeClient
{
    Task ConnectAsync();

    Task DisconnectAsync();
}
