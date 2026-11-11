using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AceCombatReloads.Game;

public static class Patches
{
	[ThreadStatic]
	public static int SelfRearmDepth;

	private static Settings S => Plugin.Settings;

	public static void Apply(Harmony h)
	{
		Try(h, "CountermeasureManager.Rearm (no free flare refill)", AccessTools.Method(typeof(CountermeasureManager), "Rearm", (Type[])null, (Type[])null), "CountermeasureRearmPrefix");
		MethodInfo target = AccessTools.GetDeclaredMethods(typeof(Unit)).FirstOrDefault((MethodInfo m) => m.Name.StartsWith("UserCode_RpcRearm_", StringComparison.Ordinal));
		Try(h, "Unit.UserCode_RpcRearm_* (reload messages, client mirror)", target, "RpcRearmPrefix", null, "RpcRearmFinalizer");
		Try(h, "AircraftActionsReport.ReportText (hide reload messages)", AccessTools.Method(typeof(AircraftActionsReport), "ReportText", (Type[])null, (Type[])null), "ReportTextPrefix");
		Try(h, "Rearmer.ProcessRearmRequest (base rearm refills reserve)", AccessTools.Method(typeof(Rearmer), "ProcessRearmRequest", (Type[])null, (Type[])null), null, "ProcessRearmRequestPostfix");
		Try(h, "CountermeasureManager.RegisterCountermeasure", AccessTools.Method(typeof(CountermeasureManager), "RegisterCountermeasure", (Type[])null, (Type[])null), null, "RegisterCountermeasurePostfix");
		Try(h, "CountermeasureManager.DeregisterCountermeasure", AccessTools.Method(typeof(CountermeasureManager), "DeregisterCountermeasure", (Type[])null, (Type[])null), null, "DeregisterCountermeasurePostfix");
		Hud.ApplyPatches(h);
	}

	internal static void Try(Harmony h, string what, MethodBase target, string prefix = null, string postfix = null, string finalizer = null, Type owner = null)
	{
		if ((object)owner == null)
		{
			owner = typeof(Patches);
		}
		if (target == null)
		{
			Plugin.Log.LogError((object)("[patch] NOT FOUND: " + what + ". The game probably changed; this part of the mod is off."));
			return;
		}
		try
		{
			h.Patch(target, (prefix == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(owner, prefix, (Type[])null, (Type[])null)), (postfix == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(owner, postfix, (Type[])null, (Type[])null)), (HarmonyMethod)null, (finalizer == null) ? ((HarmonyMethod)null) : new HarmonyMethod(AccessTools.Method(owner, finalizer, (Type[])null, (Type[])null)), (HarmonyMethod)null);
			Plugin.Log.LogInfo((object)("[patch] ok: " + what));
		}
		catch (Exception arg)
		{
			Plugin.Log.LogError((object)$"[patch] FAILED: {what}: {arg}");
		}
	}

	private static bool IsSelf(Unit rearmer, Unit unit)
	{
		if ((Object)(object)rearmer != (Object)null && (Object)(object)unit != (Object)null)
		{
			return (Object)(object)rearmer == (Object)(object)unit;
		}
		return false;
	}

	private static bool CountermeasureRearmPrefix(RearmEventArgs e, Aircraft ___aircraft)
	{
		if (S.Enabled.Value)
		{
			return !IsSelf(e.Rearmer, (Unit)(object)___aircraft);
		}
		return true;
	}

	private static void RpcRearmPrefix(Unit __instance, RearmEventArgs args)
	{
		if (!S.Enabled.Value)
		{
			return;
		}
		bool flag = IsSelf(args.Rearmer, __instance);
		if (flag)
		{
			SelfRearmDepth++;
		}
		Aircraft val = (Aircraft)(object)((__instance is Aircraft) ? __instance : null);
		if (val != null && !ReloadState.IsServer)
		{
			if (flag)
			{
				ClientMirror.OnSelfReload(val, args.Stations);
			}
			else
			{
				ClientMirror.OnBaseRearm(val, S);
			}
		}
	}

	private static Exception RpcRearmFinalizer(Unit __instance, RearmEventArgs args, Exception __exception)
	{
		if (S.Enabled.Value && IsSelf(args.Rearmer, __instance) && SelfRearmDepth > 0)
		{
			SelfRearmDepth--;
		}
		return __exception;
	}

	private static bool ReportTextPrefix()
	{
		if (SelfRearmDepth > 0)
		{
			return !S.HideSelfRearmMessages.Value;
		}
		return true;
	}

	private static void ProcessRearmRequestPostfix(Unit unitToRearm, bool __result)
	{
		if (__result && S.Enabled.Value)
		{
			ServerReloader.OnBaseRearm(unitToRearm, S);
		}
	}

	private static void RegisterCountermeasurePostfix(Countermeasure countermeasure)
	{
		ReloadState.RegisterCountermeasure(countermeasure);
	}

	private static void DeregisterCountermeasurePostfix(Countermeasure countermeasure)
	{
		ReloadState.DeregisterCountermeasure(countermeasure);
	}
}
