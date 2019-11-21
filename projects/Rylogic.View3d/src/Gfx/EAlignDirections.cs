using Rylogic.Attrib;

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
}
