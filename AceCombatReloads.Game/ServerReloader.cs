using System.Collections.Generic;
using AceCombatReloads.Core;
using Mirage;
using Mirage.RemoteCalls;
using NuclearOption.Networking;
using UnityEngine;

namespace AceCombatReloads.Game;

public static class ServerReloader
{
	private static readonly List<Aircraft> Scratch = new List<Aircraft>(64);

	public static void Tick(double now, Settings s)
	{
		ReloadState.PruneDestroyed();
		Scratch.Clear();
		Scratch.AddRange(UnitRegistry.allAircraft);
		foreach (Aircraft item in Scratch)
		{
			if (!((Object)(object)item == (Object)null) && !((Unit)item).disabled && ((NetworkBehaviour)item).IsServer && (!((Object)(object)item.Player == (Object)null) || s.AffectAI.Value))
			{
				AircraftState st = ReloadState.Get(item, create: true);
				TickWeapons(item, st, now, s);
				if (s.FlareReloads.Value)
				{
					FlareReloader.Tick(item, st, now, s, GameManager.IsLocalAircraft(item));
				}
			}
		}
	}

	private static void TickWeapons(Aircraft aircraft, AircraftState st, double now, Settings s)
	{
		List<WeaponStation> weaponStations = ((Unit)aircraft).weaponStations;
		int[] array = null;
		for (int i = 0; i < weaponStations.Count; i++)
		{
			WeaponStation val = weaponStations[i];
			int num = WeaponRules.FullAmmo(val);
			StationParams value = WeaponRules.For(val, num, s);
			if (value.Category == Category.None || num <= 0 || WeaponRules.HasLostMount(val))
			{
				continue;
			}
			if (!st.Stations.TryGetValue(i, out var value2))
			{
				value2 = (st.Stations[i] = new StationTracker(value.Reserve));
				if (s.VerboseLog.Value)
				{
					Plugin.Log.LogInfo((object)$"[track] {Name(aircraft)} st{i} {val.WeaponInfo.weaponName}: full={num} reserve={value.Reserve} time={value.ReloadTime:0.0}s chunk={value.Chunk} cost={val.WeaponInfo.costPerRound}");
				}
			}
			int ammoTotal = val.GetAmmoTotal();
			int queuedRounds = value2.QueuedRounds;
			value2.Sync(num, ammoTotal, now, value.ReloadTime, value.Chunk, value.Consecutive);
			if (s.VerboseLog.Value && queuedRounds == 0 && value2.QueuedRounds > 0)
			{
				Plugin.Log.LogInfo((object)$"[shot] {Name(aircraft)} st{i} {val.WeaponInfo.weaponName}: ammo {ammoTotal}/{num}, queued +{value2.QueuedRounds - queuedRounds} (reserve {value2.Reserve})");
			}
			int num2 = value2.DueRounds(now);
			if (num2 <= 0)
			{
				continue;
			}
			int amount = Mathf.Min(num2, Mathf.Min(num - ammoTotal, value2.Reserve));
			amount = ReloadMath.FloorToMultiple(amount, value.Multiple);
			if (amount > 0 && s.ChargeFunds.Value && !TryCharge(aircraft, val, amount))
			{
				continue;
			}
			value2.CompleteDue(now, amount);
			if (amount > 0)
			{
				(array ?? (array = new int[weaponStations.Count]))[i] = amount;
				if (s.VerboseLog.Value)
				{
					Plugin.Log.LogInfo((object)$"[reload] {Name(aircraft)} st{i} {val.WeaponInfo.weaponName}: +{amount} -> {ammoTotal + amount}/{num} (reserve {value2.Reserve})");
				}
			}
		}
		if (array != null)
		{
			Apply(aircraft, array);
		}
	}

	public static void Apply(Aircraft aircraft, int[] counts)
	{
		RearmEventArgs e = new RearmEventArgs
		{
			Rearmer = (Unit)(object)aircraft,
			Stations = counts
		};
		if (!ClientRpcSender.ShouldInvokeLocally((NetworkBehaviour)(object)aircraft, (RpcTarget)1, (INetworkPlayer)null, false))
		{
			for (int i = 0; i < counts.Length; i++)
			{
				if (counts[i] > 0)
				{
					((Unit)aircraft).weaponStations[i].Rearm(counts[i]);
				}
			}
		}
		((Unit)aircraft).RpcRearm(e);
	}

	private static bool TryCharge(Aircraft aircraft, WeaponStation station, int amount)
	{
		Player player = aircraft.Player;
		if ((Object)(object)player == (Object)null)
		{
			return true;
		}
		float num = (float)amount * station.WeaponInfo.costPerRound;
		if (player.Allocation < num)
		{
			return false;
		}
		player.AddAllocation(0f - num);
		return true;
	}

	public static void OnBaseRearm(Unit unit, Settings s)
	{
		if (s.BaseRearmRefillsReserve.Value)
		{
			Aircraft val = (Aircraft)(object)((unit is Aircraft) ? unit : null);
			if (val != null && ReloadState.Host.Remove(val) && s.VerboseLog.Value)
			{
				Plugin.Log.LogInfo((object)("[rearm] " + Name(val) + " rearmed at base: reload reserve refilled"));
			}
		}
	}

	internal static string Name(Aircraft a)
	{
		if (!((Object)(object)a == (Object)null))
		{
			return $"{((Unit)a).unitName}#{((Object)a).GetInstanceID()}";
		}
		return "?";
	}
}
