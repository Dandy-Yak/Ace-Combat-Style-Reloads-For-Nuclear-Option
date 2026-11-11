using System.Collections.Generic;
using AceCombatReloads.Core;
using UnityEngine;

namespace AceCombatReloads.Game;

public static class ClientMirror
{
	public static void Tick(double now, Settings s)
	{
		Aircraft val = default;
		if (!GameManager.GetLocalAircraft(out val) || (Object)(object)val == (Object)null || ((Unit)val).disabled)
		{
			return;
		}
		if ((Object)(object)ReloadState.ClientLocalAircraft != (Object)(object)val)
		{
			ReloadState.ClientLocalAircraft = val;
			ReloadState.ClientLocal = new AircraftState();
		}
		AircraftState clientLocal = ReloadState.ClientLocal;
		List<WeaponStation> weaponStations = ((Unit)val).weaponStations;
		for (int i = 0; i < weaponStations.Count; i++)
		{
			WeaponStation val2 = weaponStations[i];
			int num = WeaponRules.FullAmmo(val2);
			StationParams value = WeaponRules.For(val2, num, s);
			if (value.Category != Category.None && num > 0)
			{
				if (!clientLocal.Stations.TryGetValue(i, out var value2))
				{
					value2 = (clientLocal.Stations[i] = new StationTracker(value.Reserve));
				}
				value2.Sync(num, val2.GetAmmoTotal(), now, value.ReloadTime, value.Chunk, value.Consecutive);
			}
		}
		if (s.FlareReloads.Value)
		{
			FlareReloader.Tick(val, clientLocal, now, s, refreshHud: true);
		}
	}

	public static void OnSelfReload(Aircraft aircraft, int[] counts)
	{
		if ((Object)(object)aircraft != (Object)(object)ReloadState.ClientLocalAircraft || ReloadState.ClientLocal == null || counts == null)
		{
			return;
		}
		foreach (KeyValuePair<int, StationTracker> station in ReloadState.ClientLocal.Stations)
		{
			if (station.Key < counts.Length && counts[station.Key] > 0)
			{
				station.Value.ObserveReload(counts[station.Key]);
			}
		}
	}

	public static void OnBaseRearm(Aircraft aircraft, Settings s)
	{
		if (s.BaseRearmRefillsReserve.Value && (Object)(object)aircraft == (Object)(object)ReloadState.ClientLocalAircraft)
		{
			ReloadState.ClientLocal = new AircraftState();
		}
	}
}
