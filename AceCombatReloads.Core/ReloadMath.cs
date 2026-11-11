using System;

namespace AceCombatReloads.Core;

public static class ReloadMath
{
	public static int GunChunk(int fullAmmo, float fraction, int weaponCount)
	{
		weaponCount = Math.Max(1, weaponCount);
		int num = (int)Math.Round((float)fullAmmo * fraction);
		return Math.Max(weaponCount, num - num % weaponCount);
	}

	public static int FloorToMultiple(int amount, int multiple)
	{
		if (multiple <= 1)
		{
			return amount;
		}
		return amount - amount % multiple;
	}
}
