using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Services.Realtime;

public class RealtimeClient : IRealtimeClient, IAsyncDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetry;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private HubConnection? _connection;

    public RealtimeClient(ISessionManager sessionManager, ITelemetryService telemetry)
    {
        _sessionManager = sessionManager;
        _telemetry = telemetry;
    }

    public async Task ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                return;
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

            connection.On<SwapRequestUpdate>("SwapRequested", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapResponded", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapApproved", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapCancelled", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapExpired", update => Broadcast(update));
            connection.On<ScheduleUpdate>("SchedulePublished", update => BroadcastSchedule(update));
            connection.On<ScheduleUpdate>("ScheduleUnpublished", update => BroadcastSchedule(update));
            connection.On<ScheduleUpdate>("ScheduleChanged", update => BroadcastSchedule(update));

            connection.Closed += ex =>
            {
                if (ex is not null)
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
