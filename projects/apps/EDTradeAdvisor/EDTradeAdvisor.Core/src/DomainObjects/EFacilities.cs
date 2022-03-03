using System;

namespace EDTradeAdvisor.DomainObjects
{
	[Flags]
	public enum EFacilities : int
	{
		None        = 0,
		Market      = 1 << 0,
		BlackMarket = 1 << 1,
		Refuel      = 1 << 2,
		Repair      = 1 << 3,
		Rearm       = 1 << 4,
		Outfitting  = 1 << 5,
		Shipyard    = 1 << 6,
		Docking     = 1 << 7,
		Commodities = 1 << 8,
	}
}
