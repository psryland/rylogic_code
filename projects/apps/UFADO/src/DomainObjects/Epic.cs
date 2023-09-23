using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;

namespace UFADO.DomainObjects;

public class Epic : WorkItemElement
{
    public Epic(WorkItem wi)
        : base(Guid.NewGuid(), wi)
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
    public override BBox Bounds => new(O2W.pos, new v4(0.5f, 0.5f, 0.5f, 0));


    /// <inheritdoc/>
    protected override void UpdateGfxCore()
    {
        base.UpdateGfxCore();
    }
}

