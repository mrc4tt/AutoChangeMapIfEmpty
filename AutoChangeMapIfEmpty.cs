using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;

namespace AutoChangeMapIfEmpty;

[MinimumApiVersion(369)]
public sealed class AutoChangeMapIfEmpty : BasePlugin, IPluginConfig<AutoChangeMapIfEmptyConfig>
{
    public override string ModuleName => "AutoChangeMapIfEmpty";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "Miksen";
    public override string ModuleDescription => "Reloads the startup map after the server stays empty (no valid players) for a configurable time.";

    public AutoChangeMapIfEmptyConfig Config { get; set; } = new();

    private CounterStrikeSharp.API.Modules.Timers.Timer? _checkTimer;
    private float _emptySeconds;
    private bool _changePending;

    // Command that reloads the startup map, e.g. "changelevel de_dust2" or "host_workshop_map 3070077001".
    private string? _startupCommand;

    public void OnConfigParsed(AutoChangeMapIfEmptyConfig config)
    {
        Config = config;

        if (Config.CheckIntervalSeconds < 1.0f)
            Config.CheckIntervalSeconds = 1.0f;
        if (Config.EmptyMinutesBeforeChange < 1)
            Config.EmptyMinutesBeforeChange = 1;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(_ => ResetState());

        ResetState();
        _startupCommand = DetectStartupCommand(hotReload);
        if (_startupCommand != null)
            Logger.LogInformation("Startup map detected: {Command}", _startupCommand);
        else
            Logger.LogWarning("Could not detect startup map — will reload the current map instead.");

        _checkTimer = AddTimer(Config.CheckIntervalSeconds, OnCheck, TimerFlags.REPEAT);
    }

    public override void Unload(bool hotReload)
    {
        _checkTimer?.Kill();
        _checkTimer = null;
    }

    private void ResetState()
    {
        _emptySeconds = 0.0f;
        _changePending = false;
    }

    private void OnCheck()
    {
        if (_changePending)
            return;

        int valid = CountValidPlayers();

        // Any real player present -> never change, reset the empty timer.
        if (valid > 0)
        {
            _emptySeconds = 0.0f;
            return;
        }

        // Server empty (bots/CSTV don't count). Accumulate toward the threshold.
        _emptySeconds += Config.CheckIntervalSeconds;

        float threshold = Config.EmptyMinutesBeforeChange * 60.0f;
        if (_emptySeconds < threshold)
            return;

        ChangeMap();
        _emptySeconds = 0.0f;
    }

    // Counts connected humans only. Bots and CSTV/HLTV are excluded.
    private static int CountValidPlayers()
    {
        int count = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, IsBot: false, IsHLTV: false }
                && p.Connected == PlayerConnectedState.Connected)
            {
                count++;
            }
        }
        return count;
    }

    // Finds the map the server was launched with by reading the process command line.
    // Falls back to the map running when the plugin first loaded (not valid on hot reload,
    // since the current map may have changed since startup).
    private static string? DetectStartupCommand(bool hotReload)
    {
        var args = ReadProcessArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            string arg = args[i].ToLowerInvariant();
            string value = args[i + 1];
            if (value.StartsWith('+') || value.StartsWith('-'))
                continue;

            switch (arg)
            {
                case "+map":
                    return $"changelevel {value}";
                case "+host_workshop_map":
                    return $"host_workshop_map {value}";
            }
        }

        if (!hotReload && !string.IsNullOrEmpty(Server.MapName))
            return $"changelevel {Server.MapName}";

        return null;
    }

    private static string[] ReadProcessArgs()
    {
        try
        {
            // The CLR is hosted inside cs2, so read the real process args rather than the host's.
            if (OperatingSystem.IsLinux() && File.Exists("/proc/self/cmdline"))
                return File.ReadAllText("/proc/self/cmdline").Split('\0', StringSplitOptions.RemoveEmptyEntries);

            return Environment.CommandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private void ChangeMap()
    {
        string? command = _startupCommand;
        if (command == null && !string.IsNullOrEmpty(Server.MapName))
            command = $"changelevel {Server.MapName}";

        if (command == null)
        {
            Logger.LogWarning("Server empty but no startup or current map known. Nothing to change to.");
            return;
        }

        Logger.LogInformation("Server empty for {Minutes} min — running '{Command}'.",
            Config.EmptyMinutesBeforeChange, command);

        // Changing level while a GOTV demo is being written can crash the server during
        // shutdown of the old map. Stop the recording first and give it time to flush.
        if (Config.StopTvRecordBeforeChange)
            Server.ExecuteCommand("tv_stoprecord");

        if (Config.ChangeDelaySeconds <= 0.0f)
        {
            Server.ExecuteCommand(command);
            return;
        }

        _changePending = true;
        AddTimer(Config.ChangeDelaySeconds, () =>
        {
            _changePending = false;

            // Someone joined during the delay -> abort.
            if (CountValidPlayers() > 0)
            {
                Logger.LogInformation("Player joined before map change — aborted.");
                return;
            }

            Server.ExecuteCommand(command);
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }
}
