using System.Collections.Generic;
using Mirage;
using NuclearOption.Networking;
using UnityEngine;

namespace AceCombatReloads.Game;

public static class ReloadState
{
	public static readonly Dictionary<Aircraft, AircraftState> Host = new Dictionary<Aircraft, AircraftState>();

	public static AircraftState ClientLocal;

	public static Aircraft ClientLocalAircraft;

	public static readonly Dictionary<Aircraft, List<Countermeasure>> CountermeasureRegistry = new Dictionary<Aircraft, List<Countermeasure>>();

	public static bool IsServer
	{
		get
		{
			NetworkManagerNuclearOption i = NetworkManagerNuclearOption.i;
			if ((Object)(object)i != (Object)null && (Object)(object)((NetworkManager)i).Server != (Object)null)
			{
				return ((NetworkManager)i).Server.Active;
			}
			return false;
		}
	}

	public static AircraftState Get(Aircraft aircraft, bool create)
	{
		if (!Host.TryGetValue(aircraft, out var value) && create)
		{
			value = (Host[aircraft] = new AircraftState());
		}
		return value;
	}

	public static AircraftState ForLocalHud(out Aircraft aircraft)
	{
		if (!GameManager.GetLocalAircraft(out aircraft) || (Object)(object)aircraft == (Object)null)
		{
			return null;
		}
		if (IsServer)
		{
			return Get(aircraft, create: false);
		}
		if ((Object)(object)ClientLocalAircraft != (Object)(object)aircraft)
		{
			return null;
		}
		return ClientLocal;
	}

	public static void Clear()
	{
		Host.Clear();
		CountermeasureRegistry.Clear();
		ClientLocal = null;
		ClientLocalAircraft = null;
	}

	public static void PruneDestroyed()
	{
		List<Aircraft> list = null;
		foreach (Aircraft key in Host.Keys)
		{
			if ((Object)(object)key == (Object)null)
			{
				(list ?? (list = new List<Aircraft>())).Add(key);
			}
		}
		if (list != null)
		{
			foreach (Aircraft item in list)
			{
				Host.Remove(item);
			}
		}
		list = null;
		foreach (Aircraft key2 in CountermeasureRegistry.Keys)
		{
			if ((Object)(object)key2 == (Object)null)
			{
				(list ?? (list = new List<Aircraft>())).Add(key2);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Aircraft item2 in list)
		{
			CountermeasureRegistry.Remove(item2);
		}
	}

	public static void RegisterCountermeasure(Countermeasure cm)
	{
		if (!((Object)(object)cm == (Object)null) && !((Object)(object)cm.aircraft == (Object)null))
		{
			if (!CountermeasureRegistry.TryGetValue(cm.aircraft, out var value))
			{
				value = (CountermeasureRegistry[cm.aircraft] = new List<Countermeasure>());
			}
			if (!value.Contains(cm))
			{
				value.Add(cm);
			}
		}
	}

	public static void DeregisterCountermeasure(Countermeasure cm)
	{
		if (!((Object)(object)cm == (Object)null) && !((Object)(object)cm.aircraft == (Object)null) && CountermeasureRegistry.TryGetValue(cm.aircraft, out var value))
		{
			value.Remove(cm);
		}
	}

	public static int MaxAmmo(Countermeasure cm)
	{
		FlareEjector val = (FlareEjector)(object)((cm is FlareEjector) ? cm : null);
		if (val == null)
		{
			ChaffEjector val2 = (ChaffEjector)(object)((cm is ChaffEjector) ? cm : null);
			if (val2 != null)
			{
				return val2.GetMaxAmmo();
			}
			return 0;
		}
		return val.GetMaxAmmo();
	}
}
