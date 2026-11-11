using System.Collections.Generic;
using AceCombatReloads.Core;
using UnityEngine;

namespace AceCombatReloads.Game;

public static class WeaponRules
{
	// Per weapon type: whether it is excluded from reloads, and its table entry (null = unlisted).
	private static readonly Dictionary<WeaponInfo, (bool NoReload, WeaponEntry Entry)> _cache = new Dictionary<WeaponInfo, (bool NoReload, WeaponEntry Entry)>();

	private static readonly HashSet<string> _warned = new HashSet<string>();

	private static bool _dumped;

	public static Category Classify(WeaponStation station, Settings s)
	{
		if (station == null || station.Weapons == null || station.Weapons.Count == 0)
		{
			return Category.None;
		}
		WeaponInfo weaponInfo = station.WeaponInfo;
		if ((Object)(object)weaponInfo == (Object)null || station.Cargo)
		{
			return Category.None;
		}
		if (weaponInfo.cargo || weaponInfo.troops || weaponInfo.sling || weaponInfo.jammer || weaponInfo.energy || weaponInfo.massPerRound <= 0f)
		{
			return Category.None;
		}
		if (weaponInfo.nuclear && !s.AllowNuclear.Value)
		{
			return Category.None;
		}
		if (!weaponInfo.gun)
		{
			return Category.Ordnance;
		}
		return Category.Gun;
	}

	public static StationParams For(WeaponStation station, int fullAmmo, Settings s)
	{
		StationParams result = new StationParams
		{
			Category = Classify(station, s),
			Multiple = 1,
			Chunk = 1
		};
		if (result.Category == Category.None)
		{
			return result;
		}
		WeaponInfo weaponInfo = station.WeaponInfo;
		if (!_cache.TryGetValue(weaponInfo, out var rule))
		{
			rule = Resolve(weaponInfo, result.Category, s);
			_cache[weaponInfo] = rule;
		}
		if (rule.NoReload)
		{
			result.Category = Category.None;
			return result;
		}
		WeaponEntry weaponEntry = rule.Entry;
		if (result.Category == Category.Gun)
		{
			result.Multiple = Mathf.Max(1, station.Weapons.Count);
			if (weaponEntry != null)
			{
				result.Reserve = weaponEntry.Reserve.Value;
				result.ReloadTime = weaponEntry.Seconds.Value;
				result.Chunk = result.Multiple;
			}
			else
			{
				result.Reserve = Mathf.RoundToInt((float)fullAmmo * s.GunReserveLoads.Value);
				result.ReloadTime = s.GunChunkSeconds.Value;
				result.Chunk = ReloadMath.GunChunk(fullAmmo, s.GunChunkFraction.Value, result.Multiple);
			}
		}
		else if (weaponEntry != null)
		{
			result.Reserve = weaponEntry.Reserve.Value;
			result.ReloadTime = weaponEntry.Seconds.Value;
		}
		else
		{
			result.Reserve = s.UnlistedReserve.Value;
			result.ReloadTime = s.UnlistedSeconds.Value;
		}
		result.Consecutive = result.Category == Category.Gun || (weaponEntry != null && weaponEntry.Name == WeaponTable.AlwaysConsecutive) || s.ConsecutiveOrdnance.Value;
		if (s.TryGetOverride(weaponInfo.weaponName, weaponInfo.shortName, out var reserve, out var time))
		{
			result.Reserve = ((result.Category == Category.Gun) ? Mathf.RoundToInt((float)fullAmmo * (float)reserve) : reserve);
			result.ReloadTime = time;
		}
		result.Reserve = Mathf.RoundToInt((float)result.Reserve * s.ReserveMultiplier.Value);
		result.ReloadTime = Mathf.Max(0.1f, result.ReloadTime * s.ReloadTimeMultiplier.Value);
		if (result.Reserve <= 0)
		{
			result.Category = Category.None;
		}
		return result;
	}

	private static (bool NoReload, WeaponEntry Entry) Resolve(WeaponInfo info, Category category, Settings s)
	{
		if (s.IsNoReload(info.weaponName, info.shortName))
		{
			return (true, null);
		}
		DumpWeapons(s);
		WeaponEntry value = s.FindWeapon(info.weaponName, info.shortName);
		string key = info.weaponName + "|" + info.shortName;
		if (value == null)
		{
			if (category == Category.Ordnance && _warned.Add(key))
			{
				Plugin.Log.LogWarning((object)($"[weapons] no table entry for '{info.weaponName}' (short '{info.shortName}'); using the unlisted-weapon settings. " + "If this weapon is in the table, its name differs: add an entry under Per-weapon overrides."));
			}
		}
		else if (s.VerboseLog.Value && _warned.Add(key))
		{
			Plugin.Log.LogInfo((object)$"[weapons] '{info.weaponName}' (short '{info.shortName}') -> table entry '{value.Name}'");
		}
		return (false, value);
	}

	private static void DumpWeapons(Settings s)
	{
		if (_dumped || !s.VerboseLog.Value)
		{
			return;
		}
		_dumped = true;
		HashSet<string> hashSet = new HashSet<string>();
		WeaponInfo[] array = Resources.FindObjectsOfTypeAll<WeaponInfo>();
		foreach (WeaponInfo val in array)
		{
			if (!((Object)(object)val == (Object)null) && !(val.massPerRound <= 0f) && !val.cargo && !val.troops && !val.sling && !val.jammer && !val.energy && hashSet.Add(((Object)val).name))
			{
				Plugin.Log.LogInfo((object)($"[weapons] {val.weaponName} ({val.shortName}) cost={val.costPerRound} mass={val.massPerRound} " + $"gun={val.gun} missile={val.missile} bomb={val.bomb} nuclear={val.nuclear}"));
			}
		}
	}

	public static void ResetCache()
	{
		_cache.Clear();
		_dumped = false;
	}

	public static int FullAmmo(WeaponStation station)
	{
		int num = 0;
		foreach (Weapon weapon in station.Weapons)
		{
			if ((Object)(object)weapon != (Object)null)
			{
				num += weapon.GetFullAmmo();
			}
		}
		return num;
	}

	public static bool HasLostMount(WeaponStation station)
	{
		foreach (Weapon weapon in station.Weapons)
		{
			if ((Object)(object)weapon != (Object)null && !weapon.IsAttached() && weapon.GetAmmoTotal() < weapon.GetFullAmmo())
			{
				return true;
			}
		}
		return false;
	}
}
