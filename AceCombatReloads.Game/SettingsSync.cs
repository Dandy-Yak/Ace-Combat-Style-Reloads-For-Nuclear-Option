using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using NuclearOption.Networking.Lobbies;
using Steamworks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AceCombatReloads.Game;

// The host shares its gameplay settings and clients use them instead of their own while connected,
// so everyone reloads with the host's numbers. HUD and log settings stay local.
// A player-hosted game shares them in its Steam lobby data; a dedicated server in its Steam server rules.
// Only settings the host changed from their defaults are sent, as "index:value", to keep within the
// dedicated server's small rules space. The indexes follow the order of Settings.SyncedSettings, which the
// version tag guarantees is the same on both sides; add new synced settings at the end.
public static class SettingsSync
{
	private const string Format = "v1";

	private const string LobbyKey = "acr_settings";

	// Dedicated server rules: acrc = number of chunks, acr0, acr1, ... = the payload in pieces.
	// The game keeps each rule (key + value) under 127 bytes.
	private const string RuleCountKey = "acrc";

	private const string RulePrefix = "acr";

	private const int RuleChunkChars = 110;

	private const int MaxRuleChunks = 10;

	// Seconds between lobby or rules checks.
	private const float CheckInterval = 1f;

	private static readonly FieldInfo HostedLobbyField = AccessTools.Field(typeof(SteamLobby), "_hostedLobby");

	private static readonly AccessTools.FieldRef<SteamLobby, LobbyInstance> JoinedLobby = AccessTools.FieldRefAccess<SteamLobby, LobbyInstance>("_joinedLobby");

	private static readonly StringBuilder Sb = new StringBuilder();

	private static bool _dirty = true;

	private static string _payload;

	private static float _nextCheck;

	// Host side.
	private static CSteamID _publishedLobby = CSteamID.Nil;

	private static string _publishedLobbyPayload;

	private static string _publishedRulesPayload;

	private static int _publishedRuleChunks;

	private static bool _noGameServer;

	// Client side.
	private static CSteamID _readLobby = CSteamID.Nil;

	private static string _applied;

	private static Aircraft _lastAircraft;

	private static ISteamMatchmakingRulesResponse _rulesResponse;

	private static HServerQuery _rulesQuery;

	private static bool _rulesQueryRunning;

	private static readonly Dictionary<string, string> _rules = new Dictionary<string, string>(StringComparer.Ordinal);

	private static Settings _settings;

	public static void Init(ConfigFile cfg)
	{
		cfg.SettingChanged += (object _, SettingChangedEventArgs __) => _dirty = true;
	}

	public static void Tick(Settings s)
	{
		_settings = s;
		if (ReloadState.IsServer)
		{
			UseLocal(s);
			PublishTick(s);
		}
		else
		{
			ReadTick(s);
		}
	}

	// ---------- host ----------

	private static void PublishTick(Settings s)
	{
		if (!_dirty && Time.unscaledTime < _nextCheck)
		{
			return;
		}
		_nextCheck = Time.unscaledTime + CheckInterval;
		if (_dirty || _payload == null)
		{
			_payload = Encode(s);
			_dirty = false;
		}
		CSteamID cSteamID = HostedLobbyId();
		if (cSteamID != CSteamID.Nil)
		{
			if (cSteamID != _publishedLobby || _payload != _publishedLobbyPayload)
			{
				SteamMatchmaking.SetLobbyData(cSteamID, LobbyKey, _payload);
				_publishedLobby = cSteamID;
				_publishedLobbyPayload = _payload;
				Verbose($"[sync] shared host settings in lobby {cSteamID}: {_payload}");
			}
			return;
		}
		_publishedLobby = CSteamID.Nil;
		if (!_noGameServer && _payload != _publishedRulesPayload)
		{
			PublishRules(_payload);
		}
	}

	private static void PublishRules(string payload)
	{
		int num = (payload.Length + RuleChunkChars - 1) / RuleChunkChars;
		if (num > MaxRuleChunks)
		{
			Plugin.Log.LogWarning((object)"[sync] too many changed settings to share from a dedicated server (the Overrides text is probably long); clients will use the defaults.");
			payload = Format;
			num = 1;
		}
		try
		{
			for (int i = 0; i < num; i++)
			{
				int num2 = i * RuleChunkChars;
				SteamGameServer.SetKeyValue(RulePrefix + i, payload.Substring(num2, Math.Min(RuleChunkChars, payload.Length - num2)));
			}
			for (int j = num; j < _publishedRuleChunks; j++)
			{
				SteamGameServer.SetKeyValue(RulePrefix + j, "");
			}
			SteamGameServer.SetKeyValue(RuleCountKey, num.ToString(CultureInfo.InvariantCulture));
			_publishedRuleChunks = num;
			_publishedRulesPayload = payload;
			Verbose("[sync] shared host settings in the dedicated server rules: " + payload);
		}
		catch (Exception)
		{
			// No Steam game server here (single player or a local host without a lobby): nothing to share.
			_noGameServer = true;
		}
	}

	private static string Encode(Settings s)
	{
		Sb.Clear().Append(Format);
		List<ISynced> syncedSettings = s.SyncedSettings;
		for (int i = 0; i < syncedSettings.Count; i++)
		{
			if (!syncedSettings[i].LocalIsDefault())
			{
				string text = syncedSettings[i].LocalText();
				if (text.IndexOf('|') >= 0)
				{
					Plugin.Log.LogWarning((object)("[sync] '" + syncedSettings[i].Key + "' contains '|' and cannot be shared; clients use the default."));
					continue;
				}
				Sb.Append('|').Append(i.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
			}
		}
		return Sb.ToString();
	}

	// The game keeps the hosted lobby in a private field; read its Id by reflection.
	private static CSteamID HostedLobbyId()
	{
		try
		{
			if (SteamLobby.instance == null || HostedLobbyField == null)
			{
				return CSteamID.Nil;
			}
			object value = HostedLobbyField.GetValue(SteamLobby.instance);
			if (value == null)
			{
				return CSteamID.Nil;
			}
			Traverse traverse = Traverse.Create(value);
			object obj = traverse.Property("Id").GetValue() ?? traverse.Field("Id").GetValue() ?? traverse.Field("id").GetValue();
			if (obj is CSteamID cSteamID && cSteamID.IsValid())
			{
				return cSteamID;
			}
		}
		catch (Exception)
		{
		}
		return CSteamID.Nil;
	}

	// ---------- client ----------

	private static void ReadTick(Settings s)
	{
		LobbyInstance lobbyInstance = ((SteamLobby.instance == null) ? null : JoinedLobby.Invoke(SteamLobby.instance));
		if (lobbyInstance == null)
		{
			UseLocal(s);
			_readLobby = CSteamID.Nil;
			CancelRulesQuery();
			return;
		}
		CSteamID lobbyId = lobbyInstance.LobbyId;
		bool flag = false;
		if (lobbyId != _readLobby)
		{
			_readLobby = lobbyId;
			UseLocal(s);
			flag = true;
		}
		// Steam only refreshes this data when asked, so ask on joining and at every spawn.
		GameManager.GetLocalAircraft(out var aircraft);
		if ((Object)(object)aircraft != (Object)(object)_lastAircraft)
		{
			_lastAircraft = aircraft;
			flag = true;
		}
		if (lobbyInstance is ServerLobbyInstance server)
		{
			if (flag)
			{
				StartRulesQuery(server);
			}
			return;
		}
		if (flag)
		{
			SteamMatchmaking.RequestLobbyData(lobbyId);
			_nextCheck = 0f;
		}
		if (Time.unscaledTime < _nextCheck)
		{
			return;
		}
		_nextCheck = Time.unscaledTime + CheckInterval;
		string lobbyData = SteamMatchmaking.GetLobbyData(lobbyId, LobbyKey);
		if (!string.IsNullOrEmpty(lobbyData))
		{
			ApplyPayload(s, lobbyData);
		}
	}

	private static void StartRulesQuery(ServerLobbyInstance server)
	{
		CancelRulesQuery();
		if (_rulesResponse == null)
		{
			_rulesResponse = new ISteamMatchmakingRulesResponse(RulesResponded, RulesFailedToRespond, RulesRefreshComplete);
		}
		_rules.Clear();
		servernetadr_t netAdr = server.details.m_NetAdr;
		_rulesQuery = SteamMatchmakingServers.ServerRules(netAdr.GetIP(), netAdr.GetQueryPort(), _rulesResponse);
		_rulesQueryRunning = true;
	}

	private static void CancelRulesQuery()
	{
		if (_rulesQueryRunning)
		{
			_rulesQueryRunning = false;
			SteamMatchmakingServers.CancelServerQuery(_rulesQuery);
		}
	}

	private static void RulesResponded(string key, string value)
	{
		if (key != null && key.StartsWith(RulePrefix, StringComparison.Ordinal))
		{
			_rules[key] = value ?? "";
		}
	}

	private static void RulesFailedToRespond()
	{
		_rulesQueryRunning = false;
		Verbose("[sync] the dedicated server did not answer the rules query; keeping the current settings");
	}

	private static void RulesRefreshComplete()
	{
		_rulesQueryRunning = false;
		if (_settings == null || !_rules.TryGetValue(RuleCountKey, out var value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < result; i++)
		{
			if (!_rules.TryGetValue(RulePrefix + i, out var value2))
			{
				return;
			}
			stringBuilder.Append(value2);
		}
		ApplyPayload(_settings, stringBuilder.ToString());
	}

	private static void ApplyPayload(Settings s, string payload)
	{
		if (payload == _applied || !payload.StartsWith(Format, StringComparison.Ordinal))
		{
			return;
		}
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		string[] array = payload.Split('|');
		for (int i = 1; i < array.Length; i++)
		{
			int num = array[i].IndexOf(':');
			if (num > 0 && int.TryParse(array[i].Substring(0, num), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				dictionary[result] = array[i].Substring(num + 1);
			}
		}
		List<ISynced> syncedSettings = s.SyncedSettings;
		for (int j = 0; j < syncedSettings.Count; j++)
		{
			if (dictionary.TryGetValue(j, out var value))
			{
				syncedSettings[j].SetHost(value);
			}
			else
			{
				syncedSettings[j].SetHostDefault();
			}
		}
		bool firstApply = _applied == null;
		_applied = payload;
		if (firstApply)
		{
			// Rebuild this client's reserves with the host's numbers. Later changes by the host only affect
			// reserves filled after them, the same as on the host.
			ReloadState.ClientLocal = null;
			ReloadState.ClientLocalAircraft = null;
			Plugin.Log.LogInfo((object)"[sync] using the host's reload settings");
		}
		Verbose("[sync] host settings: " + payload);
	}

	private static void UseLocal(Settings s)
	{
		if (_applied == null)
		{
			return;
		}
		_applied = null;
		foreach (ISynced item in s.SyncedSettings)
		{
			item.ClearHost();
		}
	}

	private static void Verbose(string text)
	{
		if (_settings != null && _settings.VerboseLog.Value)
		{
			Plugin.Log.LogInfo((object)text);
		}
	}
}
