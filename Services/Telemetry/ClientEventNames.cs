namespace Servus.Mobile.Services.Telemetry;

/// <summary>
/// Nomes dos eventos de uso do app. Constantes pelo mesmo motivo do catálogo do servidor: um
/// typo não quebra o build, só faz o evento sumir do painel.
/// </summary>
public static class ClientEventNames
{
    public const string ScreenViewed = "screen.viewed";

    /// <summary>Tela com os DADOS prontos, não só navegada — é a medida que responde "quanto o usuário espera".</summary>
    public const string ScreenReady = "screen.ready";

    public const string UiActionCompleted = "ui.action.completed";
}
