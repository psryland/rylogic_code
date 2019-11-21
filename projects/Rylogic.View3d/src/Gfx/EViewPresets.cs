using Rylogic.Attrib;

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
}
