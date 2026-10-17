using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace AutoChangeMapIfEmpty;

public sealed class AutoChangeMapIfEmptyConfig : BasePluginConfig
{
    // How long the server must stay empty (no valid players) before the map changes.
    [JsonPropertyName("EmptyMinutesBeforeChange")]
    public int EmptyMinutesBeforeChange { get; set; } = 30;

    // How often (seconds) the plugin re-counts valid players. Lower = more responsive.
    [JsonPropertyName("CheckIntervalSeconds")]
    public float CheckIntervalSeconds { get; set; } = 60.0f;

    // Run tv_stoprecord before changing map. Changing level mid-recording can crash the server.
    [JsonPropertyName("StopTvRecordBeforeChange")]
    public bool StopTvRecordBeforeChange { get; set; } = true;

    // Seconds to wait between tv_stoprecord and the map change (demo flush). 0 = no delay.
    [JsonPropertyName("ChangeDelaySeconds")]
    public float ChangeDelaySeconds { get; set; } = 3.0f;
}
