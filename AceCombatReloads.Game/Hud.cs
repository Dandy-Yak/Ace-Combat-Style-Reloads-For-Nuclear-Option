using System;
using System.Collections.Generic;
using System.Text;
using AceCombatReloads.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AceCombatReloads.Game;

public static class Hud
{
	private static readonly AccessTools.FieldRef<WeaponStatus, TextMeshProUGUI> AmmoText = AccessTools.FieldRefAccess<WeaponStatus, TextMeshProUGUI>("ammoText");

	private static readonly AccessTools.FieldRef<WeaponStatus, Image> ReloadImage = AccessTools.FieldRefAccess<WeaponStatus, Image>("reloadProgressImage");

	private static readonly AccessTools.FieldRef<WeaponStatus, RectTransform> ReloadBar = AccessTools.FieldRefAccess<WeaponStatus, RectTransform>("reloadProgress");

	private static readonly AccessTools.FieldRef<WeaponStatus, RectTransform> Panel = AccessTools.FieldRefAccess<WeaponStatus, RectTransform>("panel");

	private static readonly AccessTools.FieldRef<WeaponStatus, WeaponStation> StationOf = AccessTools.FieldRefAccess<WeaponStatus, WeaponStation>("weaponStation");

	private static readonly AccessTools.FieldRef<CombatHUD, TextMeshProUGUI> CmAmmoText = AccessTools.FieldRefAccess<CombatHUD, TextMeshProUGUI>("countermeasureAmmo");

	private static GUIStyle _style;

	private static Texture2D _barTex;

	private static readonly StringBuilder Sb = new StringBuilder();

	private static Settings S => Plugin.Settings;

	private static int _localFrame = -1;

	private static AircraftState _localState;

	private static Aircraft _localAircraft;

	// The local aircraft's reload state, looked up once per frame and shared by every HUD element.
	private static AircraftState LocalState(out Aircraft aircraft)
	{
		int frameCount = Time.frameCount;
		if (frameCount != _localFrame)
		{
			_localFrame = frameCount;
			_localState = ReloadState.ForLocalHud(out _localAircraft);
		}
		aircraft = _localAircraft;
		return _localState;
	}

	public static void ApplyPatches(Harmony h)
	{
		Patches.Try(h, "WeaponStatus.UpdateDisplay (HUD reserve)", AccessTools.Method(typeof(WeaponStatus), "UpdateDisplay", new Type[1] { typeof(WeaponStation) }, (Type[])null), null, "UpdateDisplayPostfix", null, typeof(Hud));
		Patches.Try(h, "WeaponStatus.Update (HUD reload bar)", AccessTools.Method(typeof(WeaponStatus), "Update", (Type[])null, (Type[])null), null, "UpdatePostfix", null, typeof(Hud));
		Patches.Try(h, "CombatHUD.DisplayCountermeasureAmmo (HUD flare reserve)", AccessTools.Method(typeof(CombatHUD), "DisplayCountermeasureAmmo", (Type[])null, (Type[])null), null, "CountermeasureAmmoPostfix", null, typeof(Hud));
	}

	private static StationTracker TrackerFor(WeaponStation station)
	{
		if (station == null || !S.Enabled.Value || !S.ShowReserveOnWeaponHud.Value)
		{
			return null;
		}
		AircraftState aircraftState = LocalState(out var aircraft);
		if (aircraftState == null)
		{
			return null;
		}
		int num = ((Unit)aircraft).weaponStations.IndexOf(station);
		if (num < 0 || !aircraftState.Stations.TryGetValue(num, out var value))
		{
			return null;
		}
		return value;
	}

	private static void UpdateDisplayPostfix(WeaponStatus __instance, WeaponStation weaponStation)
	{
		StationTracker stationTracker = TrackerFor(weaponStation);
		if (stationTracker != null)
		{
			TextMeshProUGUI val = AmmoText.Invoke(__instance);
			if ((Object)(object)val != (Object)null)
			{
				string text = ((TMP_Text)val).text ?? "";
				if (text.Contains("/"))
				{
					text = weaponStation.GetAmmoTotal().ToString();
				}
				((TMP_Text)val).text = text.TrimEnd() + "+" + stationTracker.Reserve;
			}
			if (stationTracker.QueuedRounds > 0)
			{
				((Behaviour)__instance).enabled = true;
			}
		}
	}

	private static void UpdatePostfix(WeaponStatus __instance)
	{
		WeaponStation val = StationOf.Invoke(__instance);
		StationTracker stationTracker = TrackerFor(val);
		if (stationTracker == null || stationTracker.QueuedRounds == 0 || val.Reloading)
		{
			return;
		}
		float num = stationTracker.NextProgress(Time.timeSinceLevelLoad, out var _);
		if (!(num < 0f))
		{
			RectTransform val2 = ReloadBar.Invoke(__instance);
			RectTransform val3 = Panel.Invoke(__instance);
			Image val4 = ReloadImage.Invoke(__instance);
			if (!((Object)(object)val2 == (Object)null) && !((Object)(object)val3 == (Object)null) && !((Object)(object)val4 == (Object)null))
			{
				((Behaviour)val4).enabled = true;
				val2.sizeDelta = new Vector2((val3.sizeDelta.x - 10f) * num, val2.sizeDelta.y);
				((Behaviour)__instance).enabled = true;
			}
		}
	}

	private static TextMeshProUGUI _cmReserve;

	private static TMP_Text _cmReserveParent;

	private static int _cmReserveShown = int.MinValue;

	private static float _cmLineFontSize = -1f;

	private static float _cmLineHeight;

	private static void HideCmReserve()
	{
		if (_cmReserve != null && _cmReserve.gameObject.activeSelf)
		{
			_cmReserve.gameObject.SetActive(false);
		}
	}

	private static TextMeshProUGUI CreateCmReserve(TMP_Text parent)
	{
		GameObject gameObject = new GameObject("AceCombatReserve", typeof(RectTransform));
		gameObject.layer = parent.gameObject.layer;
		RectTransform rectTransform = (RectTransform)gameObject.transform;
		rectTransform.SetParent((Transform)parent.rectTransform, false);
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.font = parent.font;
		textMeshProUGUI.fontSharedMaterial = parent.fontSharedMaterial;
		textMeshProUGUI.fontStyle = parent.fontStyle;
		textMeshProUGUI.characterSpacing = parent.characterSpacing;
		textMeshProUGUI.enableAutoSizing = false;
		textMeshProUGUI.enableWordWrapping = false;
		textMeshProUGUI.overflowMode = TextOverflowModes.Overflow;
		textMeshProUGUI.raycastTarget = false;
		textMeshProUGUI.richText = true;
		return textMeshProUGUI;
	}

	private static void ShowCmReserve(TMP_Text parent, int reserve)
	{
		if (_cmReserve == null || _cmReserveParent != parent)
		{
			if (_cmReserve != null)
			{
				Object.Destroy(_cmReserve.gameObject);
			}
			_cmReserve = CreateCmReserve(parent);
			_cmReserveParent = parent;
			_cmReserveShown = int.MinValue;
			_cmLineFontSize = -1f;
		}
		if (!_cmReserve.gameObject.activeSelf)
		{
			_cmReserve.gameObject.SetActive(true);
		}
		if (_cmReserveShown != reserve)
		{
			_cmReserve.text = "+" + reserve;
			_cmReserveShown = reserve;
		}
		_cmReserve.color = parent.color;
		_cmReserve.alignment = parent.alignment;
		_cmReserve.fontSize = parent.fontSize * 0.7f;
		// Sit one line below the counter, whichever way the counter's text is anchored in its box.
		// Measuring a line is a text layout pass, so only redo it when the counter's font size changes.
		if (parent.fontSize != _cmLineFontSize)
		{
			_cmLineFontSize = parent.fontSize;
			_cmLineHeight = Mathf.Max(parent.fontSize, parent.GetPreferredValues("A").y);
		}
		float lineHeight = _cmLineHeight;
		int vertical = (int)parent.alignment & 0x3F00;
		float factor = ((vertical == 0x100) ? 1f : ((vertical == 0x400 || vertical == 0x800) ? 0.7f : 0.85f));
		_cmReserve.rectTransform.anchoredPosition = new Vector2(0f, 0f - lineHeight * (factor + S.ReserveLineGap.Value));
	}

	private static void CountermeasureAmmoPostfix(CombatHUD __instance)
	{
		_cmHud = __instance;
		if (!S.Enabled.Value || !S.FlareReloads.Value)
		{
			HideCmReserve();
			return;
		}
		AircraftState aircraftState = LocalState(out var aircraft);
		if (aircraftState == null)
		{
			HideCmReserve();
			return;
		}
		Countermeasure activeCountermeasure = aircraft.countermeasureManager.GetActiveCountermeasure();
		if ((Object)(object)activeCountermeasure == (Object)null)
		{
			HideCmReserve();
			return;
		}
		int num = 0;
		bool flag = false;
		foreach (KeyValuePair<Countermeasure, StationTracker> countermeasure in aircraftState.Countermeasures)
		{
			if ((Object)(object)countermeasure.Key != (Object)null && countermeasure.Key.displayName == activeCountermeasure.displayName)
			{
				num += countermeasure.Value.Reserve;
				flag = true;
			}
		}
		TextMeshProUGUI val = CmAmmoText.Invoke(__instance);
		if (flag && (Object)(object)val != (Object)null)
		{
			ShowCmReserve((TMP_Text)val, num);
		}
		else
		{
			HideCmReserve();
		}
	}

	private static CombatHUD _cmHud;

	// Look of the game's own missile reload bar, taken from its log readout:
	// half-transparent orange, the built-in rounded UI sprite, additive text material.
	private const float BarLeft = 5f;

	private const float BarBottom = 5f;

	private const float BarHeight = 20.2f;

	private static readonly Color BarColor = new Color(1f, 0.427f, 0f, 0.502f);

	private static Sprite _uiSprite;

	private static Material _barMaterial;

	private static Sprite _roundedSprite;

	private static RectTransform _cmBar;

	private static RectTransform _cmBarPanel;

	private static bool _cmBarVisible;

	private static float _nextFlareCheck;

	private static void HideCmBar()
	{
		_cmBarVisible = false;
		_nextFlareCheck = Time.unscaledTime + 0.2f;
		if (_cmBar != null && _cmBar.gameObject.activeSelf)
		{
			_cmBar.gameObject.SetActive(false);
		}
	}

	private static Sprite GetRoundedSprite()
	{
		if (_roundedSprite != null)
		{
			return _roundedSprite;
		}
		int num = 16;
		int num2 = 4;
		Texture2D texture2D = new Texture2D(num, num, TextureFormat.RGBA32, false);
		texture2D.filterMode = FilterMode.Bilinear;
		texture2D.wrapMode = TextureWrapMode.Clamp;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num3 = (float)j + 0.5f;
				float num4 = (float)i + 0.5f;
				float num5 = Mathf.Clamp(num3, (float)num2, (float)(num - num2));
				float num6 = Mathf.Clamp(num4, (float)num2, (float)(num - num2));
				float num7 = Mathf.Sqrt((num3 - num5) * (num3 - num5) + (num4 - num6) * (num4 - num6));
				texture2D.SetPixel(j, i, new Color(1f, 1f, 1f, Mathf.Clamp01((float)num2 - num7 + 0.5f)));
			}
		}
		texture2D.Apply();
		_roundedSprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)num, (float)num), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, new Vector4((float)num2, (float)num2, (float)num2, (float)num2));
		return _roundedSprite;
	}

	private static void LoadBarAssets()
	{
		if (_uiSprite == null)
		{
			Sprite[] array = Resources.FindObjectsOfTypeAll<Sprite>();
			foreach (Sprite sprite in array)
			{
				if (sprite != null && sprite.name == "UISprite")
				{
					_uiSprite = sprite;
					break;
				}
			}
		}
		if (_barMaterial != null)
		{
			return;
		}
		Material[] array2 = Resources.FindObjectsOfTypeAll<Material>();
		foreach (Material material in array2)
		{
			if (material != null && material.name == "Text_additive")
			{
				_barMaterial = material;
				return;
			}
		}
		Shader shader = Shader.Find("Unlit/AdditiveTextShader");
		if (shader != null)
		{
			_barMaterial = new Material(shader);
		}
	}

	private static RectTransform CreateCmBar(RectTransform panel)
	{
		LoadBarAssets();
		GameObject gameObject = new GameObject("AceCombatReloadBar", typeof(RectTransform));
		gameObject.layer = panel.gameObject.layer;
		RectTransform rectTransform = (RectTransform)gameObject.transform;
		rectTransform.SetParent((Transform)panel, false);
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.zero;
		rectTransform.pivot = Vector2.zero;
		Image image = gameObject.AddComponent<Image>();
		image.raycastTarget = false;
		image.sprite = ((_uiSprite != null) ? _uiSprite : GetRoundedSprite());
		image.type = Image.Type.Sliced;
		image.color = BarColor;
		if (_barMaterial != null)
		{
			image.material = _barMaterial;
		}
		rectTransform.SetAsFirstSibling();
		return rectTransform;
	}

	public static void UpdateFlareBar()
	{
		if (_cmHud == null || (!_cmBarVisible && Time.unscaledTime < _nextFlareCheck))
		{
			return;
		}
		if (!S.Enabled.Value || !S.FlareReloads.Value || !S.FlareReloadBar.Value)
		{
			HideCmBar();
			return;
		}
		TextMeshProUGUI val = CmAmmoText.Invoke(_cmHud);
		if (val == null || !val.gameObject.activeInHierarchy)
		{
			HideCmBar();
			return;
		}
		AircraftState aircraftState = LocalState(out var aircraft);
		if (aircraftState == null)
		{
			HideCmBar();
			return;
		}
		Countermeasure activeCountermeasure = aircraft.countermeasureManager.GetActiveCountermeasure();
		if ((Object)(object)activeCountermeasure == (Object)null)
		{
			HideCmBar();
			return;
		}
		float num = -1f;
		foreach (KeyValuePair<Countermeasure, StationTracker> countermeasure in aircraftState.Countermeasures)
		{
			if ((Object)(object)countermeasure.Key != (Object)null && countermeasure.Key.displayName == activeCountermeasure.displayName && countermeasure.Value.QueuedRounds > 0)
			{
				float num2 = countermeasure.Value.NextProgress(Time.timeSinceLevelLoad, out var _);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		RectTransform panel = ((TMP_Text)val).rectTransform.parent as RectTransform;
		if (num < 0f || panel == null)
		{
			HideCmBar();
			return;
		}
		if (_cmBar == null || _cmBarPanel != panel)
		{
			if (_cmBar != null)
			{
				Object.Destroy(_cmBar.gameObject);
			}
			_cmBar = CreateCmBar(panel);
			_cmBarPanel = panel;
		}
		_cmBarVisible = true;
		if (!_cmBar.gameObject.activeSelf)
		{
			_cmBar.gameObject.SetActive(true);
		}
		_cmBar.sizeDelta = new Vector2(Mathf.Max(0f, panel.rect.width - BarLeft * 2f) * Mathf.Clamp01(num), BarHeight);
		_cmBar.anchoredPosition = new Vector2(BarLeft, BarBottom + S.FlareBarOffsetY.Value);
	}

	public static void OnGUI()
	{
		if (!S.Enabled.Value || !S.ShowOverlay.Value || Event.current.type != EventType.Repaint)
		{
			return;
		}
		AircraftState aircraftState = LocalState(out var aircraft);
		if (aircraftState == null)
		{
			return;
		}
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label)
			{
				fontSize = Mathf.Max(11, Screen.height / 70),
				richText = true
			};
			_style.normal.textColor = new Color(0.6f, 1f, 0.6f, 0.9f);
			_barTex = Texture2D.whiteTexture;
		}
		double now = Time.timeSinceLevelLoad;
		float num = (float)Screen.width * S.OverlayX.Value;
		float num2 = (float)Screen.height * S.OverlayY.Value;
		float num3 = (float)_style.fontSize + 6f;
		float num4 = (float)_style.fontSize * 22f;
		GUI.Label(new Rect(num, num2, num4, num3), "<b>RELOADS</b>", _style);
		num2 += num3;
		foreach (KeyValuePair<int, StationTracker> station in aircraftState.Stations)
		{
			if (station.Key < ((Unit)aircraft).weaponStations.Count)
			{
				WeaponStation val = ((Unit)aircraft).weaponStations[station.Key];
				Row(((Object)(object)val.WeaponInfo != (Object)null) ? (val.WeaponInfo.shortName ?? val.WeaponInfo.weaponName) : "?", val.GetAmmoTotal(), station.Value, now, num, ref num2, num4, num3);
			}
		}
		foreach (KeyValuePair<Countermeasure, StationTracker> countermeasure in aircraftState.Countermeasures)
		{
			if ((Object)(object)countermeasure.Key != (Object)null)
			{
				Row(countermeasure.Key.displayName, countermeasure.Key.ammo, countermeasure.Value, now, num, ref num2, num4, num3);
			}
		}
	}

	private static void Row(string name, int loaded, StationTracker tr, double now, float x, ref float y, float width, float line)
	{
		float num = tr.NextProgress(now, out var secondsLeft);
		Sb.Clear().Append(name).Append("  ")
			.Append(loaded)
			.Append("  <color=#9cf>+")
			.Append(tr.Reserve)
			.Append("</color>");
		if (num >= 0f)
		{
			Sb.Append("  ").Append(secondsLeft.ToString("0.0")).Append('s');
		}
		GUI.Label(new Rect(x, y, width, line), Sb.ToString(), _style);
		if (num >= 0f)
		{
			Color color = GUI.color;
			GUI.color = new Color(0.6f, 1f, 0.6f, 0.6f);
			GUI.DrawTexture(new Rect(x, y + line - 3f, width * 0.6f * num, 2f), (Texture)(object)_barTex);
			GUI.color = color;
		}
		y += line;
	}
}
