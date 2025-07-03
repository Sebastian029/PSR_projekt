namespace App.Server;
using System.Text.Json.Serialization;

public class SettingsRequest
{
    public string type { get; set; } = "settings";

    [JsonPropertyName("depth")]
    public int Depth { get; set; }

    [JsonPropertyName("granulation")]
    public int Granulation { get; set; }

    [JsonPropertyName("isPerformanceTest")]
    public bool IsPerformanceTest { get; set; }

    [JsonPropertyName("isPlayerMode")]
    public bool IsPlayerMode { get; set; }
}