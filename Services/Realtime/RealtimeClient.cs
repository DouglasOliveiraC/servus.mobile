using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Servus.Mobile.Services.Auth;
using Servus.Mobile.Services.Http;
using Servus.Mobile.Services.Telemetry;

namespace Servus.Mobile.Services.Realtime;

public class RealtimeClient : IRealtimeClient, IAsyncDisposable
{
    private readonly ITokenStore _tokenStore;
    private readonly ITelemetryService _telemetry;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private HubConnection? _connection;

    public RealtimeClient(ITokenStore tokenStore, ITelemetryService telemetry)
    {
        _tokenStore = tokenStore;
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

            var token = await _tokenStore.GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var hubUrl = new Uri(new Uri(ApiConfig.BaseUrl), "hubs/notifications");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // AccessTokenProvider é reavaliado a cada (re)conexão automática do
                    // HubConnection, então uma renovação de token no TokenStore é pega
                    // sem precisar recriar a connection manualmente.
                    options.AccessTokenProvider = () => _tokenStore.GetTokenAsync();
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<SwapRequestUpdate>("SwapRequested", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapResponded", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapApproved", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapCancelled", update => Broadcast(update));
            connection.On<SwapRequestUpdate>("SwapExpired", update => Broadcast(update));
            connection.On<SchedulePublishedUpdate>("SchedulePublished", update =>
                WeakReferenceMessenger.Default.Send(new SchedulePublishedMessage(update)));

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
                // Falha de conexão em tempo real não pode derrubar o app nem o fluxo de
                // login — o usuário continua funcionando via HTTP normal, só sem os
                // alertas ao vivo até a próxima tentativa (ex. próximo ConnectAsync).
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

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
    }
}
