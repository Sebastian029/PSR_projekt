using System.Text.Json.Serialization;

namespace GrpcService;

public class SettingsRequest
{
    public string type { get; set; } = "settings";

    [JsonPropertyName("depth")]
    public int Depth { get; set; }

    [JsonPropertyName("granulation")]
    public int Granulation { get; set; }

    [JsonPropertyName("isPerformanceTest")]
    public bool IsPerformanceTest { get; set; }

    // Add GameMode setting
    [JsonPropertyName("isPlayerMode")]
    public bool IsPlayerMode { get; set; }
}