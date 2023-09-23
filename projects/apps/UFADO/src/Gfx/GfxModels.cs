using System;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Utility;

namespace UFADO.Gfx;

internal sealed class GfxModels :IDisposable
{
	// Notes:
	//  - This class is supposed to load the models for the work items
	//    so that the nodes can share instances.

	public GfxModels()
	{
		// Create the work stream model
		{
			var ldr = new LdrBuilder();
			ldr.Box("workstream", Colour32.White, 0.5f, 0.5f, 0.5f);
			WorkStream = new View3d.Object(ldr, false, null, null);
		}
	}
	public void Dispose()
	{
		WorkStream = null!;
		GC.SuppressFinalize(this);
	}

	/// <summary>The graphics model to use for the WorkStream objects</summary>
	public View3d.Object WorkStream
	{
		get => m_work_stream;
		private set
		{
			if (m_work_stream == value) return;
			Util.Dispose(ref m_work_stream!);
			m_work_stream = value;
		}
	}
	private View3d.Object m_work_stream = null!;

}
