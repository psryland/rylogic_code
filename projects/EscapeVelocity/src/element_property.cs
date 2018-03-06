using System;

namespace EscapeVelocity
{
	[Flags] public enum EElemProp
	{
		Existence         = 1 << 0,
		Name              = 1 << 1,
		AtomicNumber      = 1 << 2,
		MeltingPoint      = 1 << 3,
		BoilingPoint      = 1 << 4,
		ValenceElectrons  = 1 << 5,
		ElectroNegativity = 1 << 6,
		AtomicRadius      = 1 << 7,
	}

	public enum EPhase
	{
		Solid,
		Liquid,
		Gas,
	}
}