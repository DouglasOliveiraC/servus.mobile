using System.Text.Json.Serialization;

namespace Servus.Mobile.Services.Http;

public class ApiProblemDetails
{
    public int? Status { get; set; }
    public string? Title { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }
}
