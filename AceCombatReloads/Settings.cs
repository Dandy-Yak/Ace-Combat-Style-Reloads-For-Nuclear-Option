using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;

namespace AceCombatReloads;

public sealed class Settings
{
	public ConfigEntry<bool> Enabled;

	public ConfigEntry<bool> AffectAI;

	public Synced<bool> AllowNuclear;

	public ConfigEntry<bool> ChargeFunds;

	public Synced<bool> BaseRearmRefillsReserve;

	public ConfigEntry<float> PollInterval;

	public Synced<float> ReloadTimeMultiplier;

	public Synced<float> ReserveMultiplier;

	public ConfigEntry<bool> VerboseLog;

	public Synced<int> UnlistedReserve;

	public Synced<float> UnlistedSeconds;

	public Synced<bool> ConsecutiveOrdnance;

	public Synced<float> GunReserveLoads;

	public Synced<float> GunChunkFraction;

	public Synced<float> GunChunkSeconds;

	public Synced<bool> FlareReloads;

	public Synced<float> FlareReserveLoads;

	public Synced<float> FlareChunkFraction;

	public Synced<float> FlareTime;

	public Synced<string> Overrides;

	// Every setting that clients take from the host (see SettingsSync).
	public readonly System.Collections.Generic.List<ISynced> SyncedSettings = new System.Collections.Generic.List<ISynced>();

	public ConfigEntry<bool> ShowReserveOnWeaponHud;

	public ConfigEntry<bool> ShowOverlay;

	public ConfigEntry<float> OverlayX;

	public ConfigEntry<float> OverlayY;

	public ConfigEntry<bool> HideSelfRearmMessages;

	public ConfigEntry<float> ReserveLineGap;

	public ConfigEntry<bool> FlareReloadBar;

	public ConfigEntry<float> FlareBarOffsetY;

	private readonly Dictionary<string, WeaponEntry> _byName = new Dictionary<string, WeaponEntry>();

	private readonly Dictionary<string, WeaponEntry> _byShortName = new Dictionary<string, WeaponEntry>();

	private readonly HashSet<string> _noReload = new HashSet<string>();

	private Dictionary<string, (int reserve, float time)> _overrides;

	private string _overridesRaw;

	public Settings(ConfigFile cfg)
	{
		Enabled = Bind<bool>(cfg, "General", "Enabled", true, "Master switch.");
		AffectAI = Bind<bool>(cfg, "General", "AffectAI", false, "Give AI aircraft reloads too. Host setting.");
		AllowNuclear = SyncBind<bool>(cfg, "General", "AllowNuclear", false, "Reload nuclear weapons in flight. Host setting.");
		ChargeFunds = Bind<bool>(cfg, "General", "ChargeFunds", false, "Charge the player's funds (costPerRound) for each reload, like a ground rearm. Off = free, Ace Combat style. Host setting.");
		BaseRearmRefillsReserve = SyncBind<bool>(cfg, "General", "BaseRearmRefillsReserve", true, "A real rearm (airbase, truck, ship) refills the reload reserve.");
		PollInterval = Bind<float>(cfg, "General", "PollInterval", 0.25f, "Seconds between reload checks.");
		ReloadTimeMultiplier = SyncBind<float>(cfg, "General", "ReloadTimeMultiplier", 1f, "Scales every reload time: missiles, bombs, rockets, guns, flares and chaff. 2 = twice as slow, 0.5 = twice as fast. Applies to reloads that start after the change. Host setting.", new AcceptableValueRange<float>(0.1f, 5f));
		ReserveMultiplier = SyncBind<float>(cfg, "General", "ReserveMultiplier", 1f, "Scales every reload reserve: missiles, bombs, rockets, guns, flares and chaff. 2 = twice as many spare rounds. Applies from the next spawn or base rearm. Host setting.", new AcceptableValueRange<float>(0.1f, 5f));
		VerboseLog = Bind<bool>(cfg, "General", "VerboseLog", false, "Log every shot and reload to LogOutput.log, plus a list of every weapon the game knows (to check names against the weapon sections).");
		ConsecutiveOrdnance = SyncBind<bool>(cfg, "Missiles, bombs, rockets", "ConsecutiveReload", false, "On = missiles, bombs and rockets reload one round after another. Off = every missing round reloads at the same time. Applies to all of them. Host setting.");
		UnlistedReserve = SyncBind<int>(cfg, "Missiles, bombs, rockets", "UnlistedReserveRounds", 0, "Reload reserve (rounds) for a missile, bomb or rocket that has no 'Weapon' section below (for example a modded weapon). 0 = no reload.");
		UnlistedSeconds = SyncBind<float>(cfg, "Missiles, bombs, rockets", "UnlistedReloadSeconds", 20f, "Reload time per round for such an unlisted weapon.");
		GunReserveLoads = SyncBind<float>(cfg, "Guns", "ReserveFullLoads", 3f, "Reserve in full gun loads, for guns that have no 'Weapon' section. 0 = no gun reload.");
		GunChunkFraction = SyncBind<float>(cfg, "Guns", "ChunkFraction", 0.1f, "Each gun reload puts back this fraction of a full load.", new AcceptableValueRange<float>(0.01f, 1f));
		GunChunkSeconds = SyncBind<float>(cfg, "Guns", "ChunkSeconds", 12f, "Seconds per chunk, for guns that have no 'Weapon' section. Chunks reload one after another, not at the same time.");
		FlareReloads = SyncBind<bool>(cfg, "Flares and chaff", "Enabled", true, "Reload flares and chaff in flight. Host setting.");
		FlareReserveLoads = SyncBind<float>(cfg, "Flares and chaff", "ReserveFullLoads", 4f, "Reserve in full flare/chaff loads. 0 = no flare reload.");
		FlareChunkFraction = SyncBind<float>(cfg, "Flares and chaff", "ChunkFraction", 0.1f, "Each reload puts back this fraction of a full load.", new AcceptableValueRange<float>(0.01f, 1f));
		FlareTime = SyncBind<float>(cfg, "Flares and chaff", "ChunkSeconds", 12f, "Seconds per chunk. Chunks reload one after another, not at the same time.");
		Overrides = SyncBind<string>(cfg, "Per-weapon overrides", "Overrides", "", "Optional. 'Weapon name=reserve,seconds' separated by ';'. Matches weaponName or shortName. Beats the Weapon sections. Example: AAM-4=4,30;Mk82=10,8");
		ShowReserveOnWeaponHud = Bind<bool>(cfg, "HUD", "ReserveOnWeaponBox", true, "Show '+N' reserve and the reload bar in the game's weapon box.");
		ShowOverlay = Bind<bool>(cfg, "HUD", "Overlay", false, "Small corner panel listing every weapon's reserve and timer.");
		OverlayX = Bind<float>(cfg, "HUD", "OverlayX", 0.01f, "Overlay left edge, fraction of screen width.");
		OverlayY = Bind<float>(cfg, "HUD", "OverlayY", 0.35f, "Overlay top edge, fraction of screen height.");
		ReserveLineGap = Bind<float>(cfg, "HUD", "FlareReserveExtraGap", 0f, "Extra space between the flare counter and the reserve number below it, in text heights. Negative moves the reserve up, positive moves it down.");
		FlareReloadBar = Bind<bool>(cfg, "HUD", "FlareReloadBar", true, "Show a reload progress bar on the flare counter, like the one on missiles.");
		FlareBarOffsetY = Bind<float>(cfg, "HUD", "FlareBarOffsetY", 0f, "Move the flare reload bar up (positive) or down (negative), in pixels.");
		HideSelfRearmMessages = Bind<bool>(cfg, "HUD", "HideReloadMessages", true, "Hide the game's 'Rearmed ... by' text for the mod's own reloads.");
		foreach (var (name, seconds, reserve) in WeaponTable.Defaults)
		{
			string section = "Weapon: " + name;
			WeaponEntry weaponEntry = new WeaponEntry(name, SyncBind<int>(cfg, section, "ReserveRounds", reserve, "Reload reserve in rounds. 0 = this weapon does not reload. For guns this is also in rounds."), SyncBind<float>(cfg, section, "ReloadSeconds", seconds, "Seconds to reload one round. For guns: seconds per reload chunk."));
			string text = WeaponEntry.Norm(name);
			if (text != null && !_byName.ContainsKey(text))
			{
				_byName[text] = weaponEntry;
			}
			int num = name.IndexOf(' ');
			if (num > 0)
			{
				string text2 = name.Substring(0, num);
				string text3 = WeaponEntry.Norm(text2);
				if (text3 != null && HasDigit(text2) && !_byShortName.ContainsKey(text3))
				{
					_byShortName[text3] = weaponEntry;
				}
			}
		}
		string[] noReload = WeaponTable.NoReload;
		foreach (string name2 in noReload)
		{
			string text4 = WeaponEntry.Norm(name2);
			if (text4 != null)
			{
				_noReload.Add(text4);
			}
		}
		AddAliases();
	}

	public bool IsNoReload(string weaponName, string shortName)
	{
		string text = WeaponEntry.Norm(weaponName);
		string text2 = WeaponEntry.Norm(shortName);
		if (text == null || !_noReload.Contains(text))
		{
			return text2 != null && _noReload.Contains(text2);
		}
		return true;
	}

	private static ConfigEntry<T> Bind<T>(ConfigFile cfg, string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues = null)
	{
		return cfg.Bind<T>(section, key, defaultValue, new ConfigDescription(description, acceptableValues, new ConfigurationManagerAttributes
		{
			DispName = "    " + key
		}));
	}

	private Synced<T> SyncBind<T>(ConfigFile cfg, string section, string key, T defaultValue, string description, AcceptableValueBase acceptableValues = null)
	{
		Synced<T> synced = new Synced<T>(Bind<T>(cfg, section, key, defaultValue, description, acceptableValues));
		SyncedSettings.Add(synced);
		return synced;
	}

	private void AddAliases()
	{
		foreach (var (entry, alias) in WeaponTable.Aliases)
		{
			string key = WeaponEntry.Norm(entry);
			string key2 = WeaponEntry.Norm(alias);
			if (key != null && key2 != null && _byName.TryGetValue(key, out var value) && !_byName.ContainsKey(key2))
			{
				_byName[key2] = value;
			}
		}
	}

	private static bool HasDigit(string s)
	{
		foreach (char c in s)
		{
			if (char.IsDigit(c))
			{
				return true;
			}
		}
		return false;
	}

	public WeaponEntry FindWeapon(string weaponName, string shortName)
	{
		string text = WeaponEntry.Norm(weaponName);
		string text2 = WeaponEntry.Norm(shortName);
		WeaponEntry value;
		if (text != null && _byName.TryGetValue(text, out value))
		{
			return value;
		}
		if (text2 != null && _byName.TryGetValue(text2, out value))
		{
			return value;
		}
		if (text != null && _byShortName.TryGetValue(text, out value))
		{
			return value;
		}
		if (text2 != null && _byShortName.TryGetValue(text2, out value))
		{
			return value;
		}
		return null;
	}

	public bool TryGetOverride(string weaponName, string shortName, out int reserve, out float time)
	{
		string text = Overrides.Value ?? "";
		if (_overrides == null || text != _overridesRaw)
		{
			_overridesRaw = text;
			_overrides = new Dictionary<string, (int, float)>(StringComparer.OrdinalIgnoreCase);
			string[] array = text.Split(';');
			foreach (string text2 in array)
			{
				int num = text2.IndexOf('=');
				if (num > 0)
				{
					string[] array2 = text2.Substring(num + 1).Split(',');
					if (array2.Length == 2 && int.TryParse(array2[0].Trim(), out var result) && float.TryParse(array2[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
					{
						_overrides[text2.Substring(0, num).Trim()] = (result, result2);
					}
				}
			}
		}
		if ((weaponName != null && _overrides.TryGetValue(weaponName, out (int, float) value)) || (shortName != null && _overrides.TryGetValue(shortName, out value)))
		{
			(reserve, time) = value;
			return true;
		}
		reserve = 0;
		time = 0f;
		return false;
	}
}
