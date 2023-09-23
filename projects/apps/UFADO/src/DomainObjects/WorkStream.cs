using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using UFADO.Utility;

namespace UFADO.DomainObjects;

public class WorkStream :WorkItemNode
{
	public WorkStream(ItemId item_id, string title)
		: base(item_id, title, null)
	{
		var ldr = new LdrBuilder();
		ldr.Box("workstream", Colour32.Red, 0.5f, 0.5f, 0.5f);
		Gfx = new View3d.Object(ldr, false, null, null);
	}

	/// <inheritdoc/>
	public override BBox Bounds => new(O2W.pos, new v4(0.5f, 0.5f, 0.5f, 0));
}

