using System;
using Rylogic.Attrib;
using Rylogic.Maths;

namespace Rylogic.Gfx
{
	/// <summary>Directions to align the camera up axis to</summary>
	public enum EAlignDirection
	{
		[Desc("No Align")] None,
		[Desc("+X Axis")] PosX,
		[Desc("-X Axis")] NegX,
		[Desc("+Y Axis")] PosY,
		[Desc("-Y Axis")] NegY,
		[Desc("+Z Axis")] PosZ,
		[Desc("-Z Axis")] NegZ,
	}

	public static class AlignDirection_
	{
		/// <summary>Convert a vector to an align direction</summary>
		public static EAlignDirection FromAxis(v4 direction)
		{
			if (direction == +v4.XAxis) return EAlignDirection.PosX;
			if (direction == -v4.XAxis) return EAlignDirection.NegX;
			if (direction == +v4.YAxis) return EAlignDirection.PosY;
			if (direction == -v4.YAxis) return EAlignDirection.NegY;
			if (direction == +v4.ZAxis) return EAlignDirection.PosZ;
			if (direction == -v4.ZAxis) return EAlignDirection.NegZ;
			return EAlignDirection.None;
		}

		/// <summary>Convert an align direction to a vector</summary>
		public static v4 ToAxis(EAlignDirection dir)
		{
			switch (dir)
			{
			default: throw new Exception($"Unknown align axis direction: {dir}");
			case EAlignDirection.None: return v4.Zero;
			case EAlignDirection.PosX: return +v4.XAxis;
			case EAlignDirection.NegX: return -v4.XAxis;
			case EAlignDirection.PosY: return +v4.YAxis;
			case EAlignDirection.NegY: return -v4.YAxis;
			case EAlignDirection.PosZ: return +v4.ZAxis;
			case EAlignDirection.NegZ: return -v4.ZAxis;
			}
		}
	}
}
