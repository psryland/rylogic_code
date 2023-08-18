using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;

namespace ADUFO.DomainObjects;

public class WorkStream : WorkItemElement
{
	public WorkStream(WorkItem wi)
		:base(Guid.NewGuid(), wi)
	{
		var title = wi.Fields.TryGetValue("System.Title", out var v) ? (string)v : "WorkStream";

		var ldr = new LdrBuilder();
		using (var g = ldr.Group())
		{
			ldr.Box("workstream", Colour32.Red, 0.5f, 0.5f, 0.5f);
			ldr.Text("title", Colour32.White)
				.Font(30, Colour32.White)
				.String(title)
				.Billboard()
				.NoZTest()
				.End();
		}
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

