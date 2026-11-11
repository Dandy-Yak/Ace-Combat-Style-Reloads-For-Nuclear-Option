using System;
using BepInEx.Configuration;

namespace AceCombatReloads;

public interface ISynced
{
	string Key { get; }

	string LocalText();

	bool LocalIsDefault();

	void SetHostDefault();

	void SetHost(string text);

	void ClearHost();
}

// A gameplay setting that a client takes from the host while in the host's lobby,
// without touching the client's own config file. Outside a lobby it is the local value.
public sealed class Synced<T> : ISynced
{
	public readonly ConfigEntry<T> Entry;

	private bool _hasHost;

	private T _host;

	public Synced(ConfigEntry<T> entry)
	{
		Entry = entry;
		Key = entry.Definition.Section + "." + entry.Definition.Key;
	}

	public string Key { get; }

	public T Value => _hasHost ? _host : Entry.Value;

	public string LocalText()
	{
		return TomlTypeConverter.ConvertToString(Entry.Value, typeof(T));
	}

	public bool LocalIsDefault()
	{
		return LocalText() == TomlTypeConverter.ConvertToString(Entry.DefaultValue, typeof(T));
	}

	// The host left this setting at its default, so the client uses the default too (not its own value).
	public void SetHostDefault()
	{
		_host = (T)Entry.DefaultValue;
		_hasHost = true;
	}

	public void SetHost(string text)
	{
		try
		{
			_host = (T)TomlTypeConverter.ConvertToValue(text, typeof(T));
			_hasHost = true;
		}
		catch (Exception)
		{
			_hasHost = false;
		}
	}

	public void ClearHost()
	{
		_hasHost = false;
	}
}
