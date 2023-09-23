using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using UFADO.Utility;

namespace UFADO.DomainObjects;

public class Epic : WorkItemNode
{
	public Epic(ItemId item_id, string title, ItemId? parent_item_id)
		: base(item_id, title, parent_item_id)
	{
		var ldr = new LdrBuilder();
		ldr.Sphere("epic", Colour32.Orange, 0.5f);
		Gfx = new View3d.Object(ldr, false, null, null);
	}

	/// <inheritdoc/>
	public override BBox Bounds => new(O2W.pos, new v4(0.5f, 0.5f, 0.5f, 0));
}

