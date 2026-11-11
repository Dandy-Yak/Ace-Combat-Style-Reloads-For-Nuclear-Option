using System;
using AceCombatReloads.Game;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AceCombatReloads;

[BepInPlugin("AceCombatReloads", "Ace Combat Reloads", "1.2.0")]
public sealed class Plugin : BaseUnityPlugin
{
	public static ManualLogSource Log;

	public static Settings Settings;

	private float _nextPoll;

	private float _nextErrorLog;

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
		Log.LogInfo((object)"Ace Combat Reloads 1.2.0 loaded");
	}

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

	// At most one error line every 10 seconds, so a broken frame loop cannot flood the log.
	private void LogError(string what, Exception error)
	{
		if (Time.unscaledTime > _nextErrorLog)
		{
			_nextErrorLog = Time.unscaledTime + 10f;
			Log.LogError((object)$"{what}: {error}");
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
}
