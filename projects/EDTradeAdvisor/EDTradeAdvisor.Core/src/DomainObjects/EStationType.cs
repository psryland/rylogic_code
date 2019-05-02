using Rylogic.Attrib;

namespace EDTradeAdvisor.DomainObjects
{
	public enum EStationType
	{
		[Desc("Unknown")] Unknown = 0,
		[Desc("Civilian Outpost")] CivilianOutpost = 1,
		[Desc("Commercial Outpost")] CommercialOutpost = 2,
		[Desc("Coriolis Starport")] CoriolisStarport = 3,
		[Desc("Industrial Outpost")] IndustrialOutpost = 4,
		[Desc("Military Outpost")] MilitaryOutpost = 5,
		[Desc("Mining Outpost")] MiningOutpost = 6,
		[Desc("Ocellus Starport")] OcellusStarport = 7,
		[Desc("Orbis Starport")] OrbisStarport = 8,
		[Desc("Scientific Outpost")] ScientificOutpost= 9,
		[Desc("Unknown Outpost")] UnknownOutpost = 11,
		[Desc("Planetary Outpost")] PlanetaryOutpost = 13,
		[Desc("Planetary Port")] PlanetaryPort = 14,
		[Desc("Unknown Planetary")] UnknownPlanetary = 15,
		[Desc("Planetary Settlement")] PlanetarySettlement = 16,
		[Desc("Megaship")] Megaship = 19,
		[Desc("Asteroid Base")] AsteroidBase = 20,
		[Desc("Unknown Dockable")] UnknownDockable = 22,
		[Desc("Non-Dockable Orbital")] NonDockableOrbital = 23,
	}
}
