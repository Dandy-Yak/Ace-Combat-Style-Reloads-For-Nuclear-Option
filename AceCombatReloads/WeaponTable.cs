using System.Text;

namespace AceCombatReloads;

public sealed class WeaponEntry
{
	public readonly string Name;

	public readonly Synced<int> Reserve;

	public readonly Synced<float> Seconds;

	public WeaponEntry(string name, Synced<int> reserve, Synced<float> seconds)
	{
		Name = name;
		Reserve = reserve;
		Seconds = seconds;
	}

	public static string Norm(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		foreach (char c in s)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		if (stringBuilder.Length <= 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}
}

public static class WeaponTable
{
	// Extra names the game uses for table weapons: (table entry, name in the game).
	public static readonly (string Entry, string Alias)[] Aliases =
	{
		("76mm Guided Shell", "76mm Cannon"),
		("76mm Guided Shell", "GUN 76MM"),
		("GPO-N", "GPO-N (1.5kt)"),
		("GPO-N", "GPO-N (250kt)"),
		("ALND-4", "ALND-4 (20kt)"),
		("Piledriver TBM", "Piledriver TBM (20kt)"),
		("Piledriver TBM", "TBM 20kt")
	};

	// Weapons the player cannot equip or that are never consumed: the mod never reloads them and gives them no config.
	public static readonly string[] NoReload = new string[7] { "RAM-45", "StratoLance R9", "R9", "GS25", "NL-98", "127mm Guided Shell", "127mm Cannon" };

	// This weapon always reloads one round after another. (Guns always do; missiles and bombs follow the single 'ConsecutiveReload' setting.)
	public const string AlwaysConsecutive = "76mm Guided Shell";

	// Default reload values per weapon: (in-game name, seconds to reload one round, reserve rounds).
	// Source: Nuclear_Option_Munitions_Jono_HardValues table.
	public static readonly (string Name, float Seconds, int Reserve)[] Defaults =
	{
		("IRM-S1", 8f, 64),
		("IRM-S2", 8f, 64),
		("MMR-S3", 12f, 48),
		("AAM-29 Scythe", 16f, 32),
		("AAM-36 Scimitar", 24f, 24),
		("AGR-18 Lynchpin", 6f, 128),
		("AGR-24 Kingpin", 10f, 48),
		("AT-145", 8f, 64),
		("ATP-1", 8f, 64),
		("AGM-48", 8f, 64),
		("AGM-68", 20f, 48),
		("ARAD-116", 24f, 24),
		("AGM-99", 24f, 24),
		("AShM-300", 24f, 24),
		("ALM-C450", 48f, 24),
		("ALND-4", 48f, 24),
		("Tusko-B", 64f, 24),
		("Piledriver TBM", 128f, 8),
		("PAB-80LR", 12f, 48),
		("PAB-125", 8f, 64),
		("PAB-250", 12f, 48),
		("PAB-250LR", 24f, 32),
		("GPO-500", 16f, 32),
		("GBM-500LR", 32f, 16),
		("GPO-2P Auger", 16f, 24),
		("Demolition Bomb", 32f, 12),
		("GPO-N", 128f, 4),
		("76mm Guided Shell", 3f, 500),
		("Eyeball Mk.II", 8f, 32),
	};
}
