using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.Notifications;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Services.Realtime;

public class RealtimeClient : IRealtimeClient, IAsyncDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetry;
    private readonly ILocalNotifier _notifier;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private HubConnection? _connection;

    public RealtimeClient(ISessionManager sessionManager, ITelemetryService telemetry, ILocalNotifier notifier)
    {
        _sessionManager = sessionManager;
        _telemetry = telemetry;
        _notifier = notifier;
    }

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting)
                {
                    return;
                }

                // Android cuts a backgrounded app's sockets and the automatic reconnect can stay
                // stuck in Reconnecting (or give up into Disconnected) without ever recovering,
                // so a non-live connection is replaced instead of reused.
                await _connection.DisposeAsync();
                _connection = null;
            }

            var token = await _sessionManager.GetValidAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var hubUrl = new Uri(new Uri(ApiConfig.BaseUrl), "hubs/notifications");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // AccessTokenProvider is evaluated again on every automatic reconnect, and
                    // going through the session manager refreshes an expired token first —
                    // reading the stored token directly would reconnect with a dead one.
                    options.AccessTokenProvider = () => _sessionManager.GetValidAccessTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<SwapRequestUpdate>("SwapRequested", update =>
            {
                Broadcast(update);
                _notifier.Show("Nova solicitação de troca", "Um colega pediu para trocar um plantão com você.");
            });
            connection.On<SwapRequestUpdate>("SwapResponded", update =>
            {
                Broadcast(update);
                NotifySwapResponded(update.Status);
            });
            connection.On<SwapRequestUpdate>("SwapApproved", update =>
            {
                Broadcast(update);
                _notifier.Show("Troca aprovada", "A troca de plantão foi aprovada e a escala já está atualizada.");
            });
            connection.On<SwapRequestUpdate>("SwapCancelled", update =>
            {
                Broadcast(update);
                _notifier.Show("Troca cancelada", "O colega cancelou a solicitação de troca.");
            });
            connection.On<SwapRequestUpdate>("SwapExpired", update =>
            {
                Broadcast(update);
                _notifier.Show("Troca expirada", "Uma solicitação de troca expirou sem resposta.");
            });
            connection.On<ScheduleUpdate>("SchedulePublished", update =>
            {
                BroadcastSchedule(update);
                _notifier.Show("Escala publicada", "Seus plantões foram publicados. Toque para conferir.");
            });
            connection.On<ScheduleUpdate>("ScheduleUnpublished", update =>
            {
                BroadcastSchedule(update);
                _notifier.Show("Escala retirada", "Uma escala com plantões seus deixou de estar publicada.");
            });
            // No system notification: this one goes to the whole organization on every edit,
            // and only refreshes what is on screen.
            connection.On<ScheduleUpdate>("ScheduleChanged", update => BroadcastSchedule(update));

            connection.Closed += ex =>
            {
                // OperationCanceledException is ConnectAsync disposing a stuck reconnect on
                // purpose, not a failure.
                if (ex is not null and not OperationCanceledException)
                {
                    _telemetry.TrackException(ex);
                }
                return Task.CompletedTask;
            };

            try
            {
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                // A realtime failure must not break the app or the login flow: the user keeps
                // working over plain HTTP, just without live alerts until the next
                // ConnectAsync.
                _telemetry.TrackException(ex);
                await connection.DisposeAsync();
                return;
            }

            _connection = connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void NotifySwapResponded(string status)
    {
        switch (status)
        {
            case "Accepted":
                _notifier.Show("Troca aceita", "Seu colega aceitou a troca e a escala já está atualizada.");
                break;
            case "AwaitingAdminApproval":
                _notifier.Show("Troca aceita", "Seu colega aceitou a troca. Falta a aprovação do administrador.");
                break;
            case "Declined":
                _notifier.Show("Troca recusada", "Seu colega recusou a solicitação de troca.");
                break;
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                return;
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private static void Broadcast(SwapRequestUpdate update) =>
        WeakReferenceMessenger.Default.Send(new SwapUpdatedMessage(update));

    private static void BroadcastSchedule(ScheduleUpdate update) =>
        WeakReferenceMessenger.Default.Send(new ScheduleUpdatedMessage(update));

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
