using System;
using AceCombatReloads.Game;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AceCombatReloads;

[BepInPlugin("AceCombatReloads", "Ace Combat Reloads", "1.2.1")]
public sealed class Plugin : BaseUnityPlugin
{
	public static ManualLogSource Log;

	public static Settings Settings;

	private void Awake()
	{
		Log = Logger;
		Settings = new Settings(((BaseUnityPlugin)this).Config);
		SettingsSync.Init(((BaseUnityPlugin)this).Config);
		Harmony harmony = new Harmony("AceCombatReloads");
		AceCombatReloads.Game.VersionTag.Apply(harmony);
		AceCombatReloads.Game.Patches.Apply(harmony);
		SceneManager.sceneLoaded += (Scene _, LoadSceneMode __) =>
		{
			ReloadState.Clear();
			WeaponRules.ResetCache();
		};
		// The game destroys BepInEx's own manager object (and every plugin on it) unless BepInEx is set to hide it.
		// Run the frame loop on a hidden object of our own, so the mod keeps working with any BepInEx config.
		GameObject gameObject = new GameObject("AceCombatReloadsRunner");
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		Object.DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<Runner>();
		Log.LogInfo((object)"Ace Combat Reloads 1.2.1 loaded");
	}
}

// Drives the reload ticks, the flare bar and the overlay every frame.
public sealed class Runner : MonoBehaviour
{
	private float _nextPoll;

	private float _nextErrorLog;

	private static Settings Settings => Plugin.Settings;

	private void Update()
	{
		try
		{
			Hud.UpdateFlareBar();
		}
		catch (Exception arg)
		{
			LogError("flare bar failed", arg);
		}
		if (!Settings.Enabled.Value || Time.unscaledTime < _nextPoll)
		{
			return;
		}
		_nextPoll = Time.unscaledTime + Mathf.Max(0.05f, Settings.PollInterval.Value);
		try
		{
			SettingsSync.Tick(Settings);
			double now = Time.timeSinceLevelLoad;
			if (ReloadState.IsServer)
			{
				ServerReloader.Tick(now, Settings);
			}
			else
			{
				ClientMirror.Tick(now, Settings);
			}
		}
		catch (Exception arg)
		{
			LogError("reload tick failed", arg);
		}
	}

	private void OnGUI()
	{
		try
		{
			Hud.OnGUI();
		}
		catch (Exception arg)
		{
			LogError("overlay failed", arg);
		}
	}

	private void OnDestroy()
	{
		Plugin.Log.LogWarning((object)"[runner] the mod's update loop was destroyed; reloads will stop until the game restarts");
	}

	// At most one error line every 10 seconds, so a broken frame loop cannot flood the log.
	private void LogError(string what, Exception error)
	{
		if (Time.unscaledTime > _nextErrorLog)
		{
			_nextErrorLog = Time.unscaledTime + 10f;
			Plugin.Log.LogError((object)$"{what}: {error}");
		}
	}
}
