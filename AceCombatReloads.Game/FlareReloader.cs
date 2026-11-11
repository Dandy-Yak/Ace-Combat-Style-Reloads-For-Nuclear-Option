using AceCombatReloads.Core;
using UnityEngine;

namespace AceCombatReloads.Game;

public static class FlareReloader
{
	public static void Tick(Aircraft aircraft, AircraftState st, double now, Settings s, bool refreshHud)
	{
		if (!ReloadState.CountermeasureRegistry.TryGetValue(aircraft, out var value))
		{
			return;
		}
		bool flag = false;
		foreach (Countermeasure item in value)
		{
			if ((Object)(object)item == (Object)null)
			{
				continue;
			}
			int num = ReloadState.MaxAmmo(item);
			if (num <= 0)
			{
				continue;
			}
			if (!st.Countermeasures.TryGetValue(item, out var value2))
			{
				value2 = (st.Countermeasures[item] = new StationTracker(Mathf.RoundToInt((float)num * s.FlareReserveLoads.Value * s.ReserveMultiplier.Value)));
			}
			int chunk = Mathf.Max(1, Mathf.RoundToInt((float)num * s.FlareChunkFraction.Value));
			value2.Sync(num, item.ammo, now, Mathf.Max(0.1f, s.FlareTime.Value * s.ReloadTimeMultiplier.Value), chunk, consecutive: true);
			int num2 = value2.DueRounds(now);
			if (num2 <= 0)
			{
				continue;
			}
			int num3 = Mathf.Min(num2, Mathf.Min(num - item.ammo, value2.Reserve));
			value2.CompleteDue(now, num3);
			if (num3 > 0)
			{
				item.ammo += num3;
				flag = true;
				if (s.VerboseLog.Value)
				{
					Plugin.Log.LogInfo((object)$"[flare] {ServerReloader.Name(aircraft)} {item.displayName}: +{num3} -> {item.ammo}/{num} (reserve {value2.Reserve})");
				}
			}
		}
		if (flag & refreshHud)
		{
			aircraft.countermeasureManager.UpdateHUD();
		}
	}
}
