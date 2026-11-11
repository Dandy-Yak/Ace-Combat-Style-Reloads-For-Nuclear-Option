using System.Collections.Generic;
using AceCombatReloads.Core;

namespace AceCombatReloads.Game;

public sealed class AircraftState
{
	public readonly Dictionary<int, StationTracker> Stations = new Dictionary<int, StationTracker>();

	public readonly Dictionary<Countermeasure, StationTracker> Countermeasures = new Dictionary<Countermeasure, StationTracker>();
}
