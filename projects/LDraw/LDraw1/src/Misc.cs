
using System;

namespace LDraw
{
	[Flags] public enum ELinkCameras
	{
		None      = 0,
		LeftRight = 1 << 0,
		UpDown    = 1 << 1,
		InOut     = 1 << 2,
		Rotate    = 1 << 3,
		All       = ~None,
	}

	[Flags] public enum ELinkAxes
	{
		None  = 0,
		XAxis = 1 << 0,
		YAxis = 1 << 1,
		All   = ~None,
	}
}