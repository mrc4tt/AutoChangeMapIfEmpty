# AutoChangeMapIfEmpty (FSHOST)

CounterStrikeSharp plugin for FSHOST CS2 Pro servers. When the server has been empty (no human players; bots and GOTV don't count) for a configurable time, it reloads the map the server was started with.

The startup map is read from the server's command line:

| Command line                | Map change                   |
|-----------------------------|------------------------------|
| `+map de_dust2`             | `changelevel de_dust2`       |
| `+host_workshop_map <id>`   | `host_workshop_map <id>`     |

If neither is present, the map running when the plugin first loaded is used, and failing that the current map is reloaded.

## Config (`addons/counterstrikesharp/configs/plugins/AutoChangeMapIfEmpty/AutoChangeMapIfEmpty.json`)

| Key                        | Default | Description |
|----------------------------|---------|-------------|
| `EmptyMinutesBeforeChange` | `30`    | Minutes the server must stay empty before the map is reloaded. |
| `CheckIntervalSeconds`     | `60`    | How often players are counted. |
| `StopTvRecordBeforeChange` | `true`  | Run `tv_stoprecord` before the change (changing level mid-recording can crash the server). |
| `ChangeDelaySeconds`       | `3`     | Delay between `tv_stoprecord` and the map change. Aborted if a player joins meanwhile. |

## Install

Download **AutoChangeMapIfEmpty.zip** from the [releases](https://github.com/mrc4tt/AutoChangeMapIfEmpty/releases/latest) and extract it into `game/csgo/`.

## Release zip (`AutoChangeMapIfEmpty.zip`, rooted at `game/csgo/`)

```
addons/counterstrikesharp/plugins/AutoChangeMapIfEmpty/
```

## Releasing

Bump `ModuleVersion`, add a `## X.Y.Z` section to `CHANGELOG.md`, push tag `vX.Y.Z` from `main`. `./package.sh` builds the same zip locally.

## License

[MIT](LICENSE)
