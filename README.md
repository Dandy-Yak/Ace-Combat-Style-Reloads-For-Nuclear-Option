# Ace Combat Reloads

A BepInEx 5 mod for Nuclear Option that gives aircraft in-flight reload reserves (missiles, bombs, rockets, guns, flares and chaff) with timed reloads, Ace Combat style.

## Features
- Per-weapon reserve and reload time, editable in the config (or with Configuration Manager).
- Guns, flares and chaff reload in chunks, one after another.
- Global multipliers for reload time and reserve size.
- Reserve counts and a reload bar on the HUD.
- A real rearm (airbase, truck, ship) refills the reserves.

## Multiplayer
- Host-authoritative: the host performs the reloads.
- The host and every client must run the mod. The mod adds a tag to the game version, so modded and unmodded players do not see or join each other's games.
- Clients use the host's reload settings while connected (player-hosted lobbies and dedicated servers). HUD settings stay per player.

## Install
1. Install BepInEx 5 for Nuclear Option.
2. Put `AceCombatReloads.dll` in `BepInEx\plugins`.

## Building
`dotnet build -c Release`. Set `GamePath` if the game is not in the default Steam folder:
`dotnet build -c Release -p:GamePath="D:\Games\Nuclear Option"`

## Credits
Created by Dandy Yak, with the first version created by my friend Astrogator. All code was written by AI in both the first and updated versions.
