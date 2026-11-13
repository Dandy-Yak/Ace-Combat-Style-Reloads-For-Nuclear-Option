using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AceCombatReloads.Game;

// Appends a mod tag to Application.version so the game's own lobby filter, lobby join check
// and dedicated-server filter only match players running a compatible copy of this mod.
public static class VersionTag
{
	public const string Tag = "_acecombatreloads-v1.2";

	private static string _lastBase;

	private static string _lastTagged;

	public static void Apply(Harmony h)
	{
		MethodInfo getter = AccessTools.PropertyGetter(typeof(Application), "version");
		Patches.Try(h, "Application.version (lobby/join version tag " + Tag + ")", getter, null, "VersionPostfix", null, typeof(VersionTag));
		// Players can only see and join each other when this line matches exactly on both machines.
		Plugin.Log.LogInfo((object)("[version] lobby version: " + Application.version));
	}

	private static void VersionPostfix(ref string __result)
	{
		if (__result == null || __result.EndsWith(Tag, StringComparison.Ordinal))
		{
			return;
		}
		if (!string.Equals(__result, _lastBase, StringComparison.Ordinal))
		{
			_lastBase = __result;
			_lastTagged = __result + Tag;
		}
		__result = _lastTagged;
	}
}
