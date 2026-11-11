# Ace Combat Reloads

A BepInEx 5 mod for Nuclear Option that gives aircraft in-flight reload reserves (missiles, bombs, rockets, guns, flares and chaff) with timed reloads, Ace Combat style.

## Features
- Per-weapon reserve and reload time, editable in the config (or with Configuration Manager), balance the game however you see fit!
- Each missile, rockets and bomb reloads simultaneously. Guns, flares and chaff reload consecutively in chunks, one after another.
- Global multipliers for reload time and reserve size.
- Reserve counts and a reload bar on the HUD.
- A real rearm (airbase, truck, ship) refills the reserves.

## Multiplayer
- Fully functional in multiplayer
- The host and every client must run the mod.
- Clients use the host's reload settings while connected (player-hosted lobbies and dedicated servers). HUD settings stay per player.

## Mod compatibility
- Compatible with weapon mods, you just need to find the weapons internal name and add it to the overrides section of the config. You can find these by looking in BepInX logOutput.log file and entering WEAPONNAME=RESERVECOUNT,RELOADTIME into the overrides section of the config. e.g AAM45=24,32 for the Sabre-45

## Install
1. Install BepInEx 5 for Nuclear Option.
2. Put `AceCombatReloads.dll` in `BepInEx\plugins`.

## Building
`dotnet build -c Release`. Set `GamePath` if the game is not in the default Steam folder:
`dotnet build -c Release -p:GamePath="D:\Games\Nuclear Option"`

## Credits
Created by Dandy Yak, with the first version created by my friend Astrogator. All code was written by AI in both the first and updated versions.
