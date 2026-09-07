namespace Servus.Mobile.Services.Telemetry;

/// <summary>
/// Espelha o contrato do servidor (Servus.Application.Telemetry.TelemetryOutcome) para que uma
/// consulta possa agrupar cliente e servidor pela mesma dimensão.
/// </summary>
/// <remarks>
/// Um evento de cliente registra intenção e experiência, nunca prova de alteração: só o
/// servidor confirma que algo mudou.
/// </remarks>
public enum ClientOutcome
{
    Succeeded,
    Failed,
    Cancelled
}
