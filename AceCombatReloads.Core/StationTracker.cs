using System;
using System.Collections.Generic;

namespace AceCombatReloads.Core;

public sealed class StationTracker
{
	public struct Pending
	{
		public double Start;

		public double Due;

		public int Rounds;
	}

	private readonly List<Pending> _queue = new List<Pending>();

	public int Reserve { get; private set; }

	public int QueuedRounds { get; private set; }

	public StationTracker(int reserve)
	{
		Reserve = Math.Max(0, reserve);
	}

	public void Sync(int full, int current, double now, float reloadTime, int chunk, bool consecutive)
	{
		chunk = Math.Max(1, chunk);
		int num = Math.Max(0, full - current);
		while (QueuedRounds > num && _queue.Count > 0)
		{
			int index = _queue.Count - 1;
			Pending value = _queue[index];
			int num2 = QueuedRounds - num;
			if (value.Rounds <= num2)
			{
				QueuedRounds -= value.Rounds;
				_queue.RemoveAt(index);
			}
			else
			{
				value.Rounds -= num2;
				QueuedRounds -= num2;
				_queue[index] = value;
			}
		}
		int num3 = Math.Min(num, Reserve) - QueuedRounds;
		if (consecutive && num3 > 0 && _queue.Count > 0)
		{
			int tailIndex = _queue.Count - 1;
			Pending tail = _queue[tailIndex];
			int room = Math.Min(chunk - tail.Rounds, num3);
			if (room > 0)
			{
				tail.Rounds += room;
				QueuedRounds += room;
				num3 -= room;
				_queue[tailIndex] = tail;
			}
		}
		while (num3 > 0)
		{
			int num4 = Math.Min(chunk, num3);
			// Consecutive chunks start when the last queued one finishes; the tail is always the latest.
			double num5 = (consecutive && _queue.Count > 0) ? Math.Max(now, _queue[_queue.Count - 1].Due) : now;
			_queue.Add(new Pending
			{
				Start = num5,
				Due = num5 + (double)reloadTime,
				Rounds = num4
			});
			QueuedRounds += num4;
			num3 -= num4;
		}
	}

	public int DueRounds(double now)
	{
		int num = 0;
		foreach (Pending item in _queue)
		{
			if (item.Due <= now)
			{
				num += item.Rounds;
			}
		}
		return num;
	}

	public void CompleteDue(double now, int applied)
	{
		for (int num = _queue.Count - 1; num >= 0; num--)
		{
			if (_queue[num].Due <= now)
			{
				QueuedRounds -= _queue[num].Rounds;
				_queue.RemoveAt(num);
			}
		}
		Reserve = Math.Max(0, Reserve - Math.Max(0, applied));
	}

	public void ObserveReload(int rounds)
	{
		Reserve = Math.Max(0, Reserve - Math.Max(0, rounds));
		int num = rounds;
		while (num > 0 && _queue.Count > 0)
		{
			Pending value = _queue[0];
			if (value.Rounds <= num)
			{
				num -= value.Rounds;
				QueuedRounds -= value.Rounds;
				_queue.RemoveAt(0);
			}
			else
			{
				value.Rounds -= num;
				QueuedRounds -= num;
				_queue[0] = value;
				num = 0;
			}
		}
	}

	public float NextProgress(double now, out double secondsLeft)
	{
		secondsLeft = 0.0;
		if (_queue.Count == 0)
		{
			return -1f;
		}
		Pending pending = _queue[0];
		foreach (Pending item in _queue)
		{
			if (item.Due < pending.Due)
			{
				pending = item;
			}
		}
		double num = Math.Max(0.001, pending.Due - pending.Start);
		secondsLeft = Math.Max(0.0, pending.Due - now);
		return (float)Math.Max(0.0, Math.Min(1.0, (now - pending.Start) / num));
	}
}
