using System;
using Rylogic.Attrib;
using Rylogic.Maths;

namespace Rylogic.Gfx
{
	/// <summary>Pre-set view directions</summary>
	public enum EViewPreset
	{
		[Desc("Current View")] Current,
		[Desc("+X Axis")] PosX,
		[Desc("-X Axis")] NegX,
		[Desc("+Y Axis")] PosY,
		[Desc("-Y Axis")] NegY,
		[Desc("+Z Axis")] PosZ,
		[Desc("-Z Axis")] NegZ,
		[Desc("+X,+Y,+Z Axis")] PosXYZ,
		[Desc("-X,-Y,-Z Axis")] NegXYZ,
	}

	public static class ViewPreset_
	{
		/// <summary>Convert a view preset to a camera forward direction</summary>
		public static v4 ToForward(EViewPreset vp)
		{
			switch (vp)
			{
			default: throw new Exception($"Unknown view pre-set: {vp}");
			case EViewPreset.PosX:   return +v4.XAxis;
			case EViewPreset.NegX:   return -v4.XAxis;
			case EViewPreset.PosY:   return +v4.YAxis;
			case EViewPreset.NegY:   return -v4.YAxis;
			case EViewPreset.PosZ:   return +v4.ZAxis;
			case EViewPreset.NegZ:   return -v4.ZAxis;
			case EViewPreset.PosXYZ: return +v4.XAxis + v4.YAxis + v4.ZAxis;
			case EViewPreset.NegXYZ: return -v4.XAxis - v4.YAxis - v4.ZAxis;
			}
		}
	}
}
