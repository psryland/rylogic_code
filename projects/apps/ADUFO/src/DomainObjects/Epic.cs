using System;
using System.Drawing;
using System.Text.Json;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Rylogic.Gfx;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.LDraw;
using Rylogic.Maths;
using Rylogic.Utility;

namespace ADUFO.DomainObjects;

public class Epic :WorkItemElement
{
	public Epic(WorkItem wi)
		:base(Guid.NewGuid(), wi)
	{
		var ldr = new LdrBuilder();
		ldr.Sphere("epic", Colour32.Orange, 0.5f);
		Gfx = new View3d.Object(ldr, false, null, null);
	}
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	/// <inheritdoc/>
	public override BBox Bounds => new BBox(O2W.pos, new v4(0.5f, 0.5f, 0.5f,0));


	/// <inheritdoc/>
	protected override void UpdateGfxCore()
	{
		base.UpdateGfxCore();
	}
}

