using System;
using System.Collections.Generic;
using Rylogic.Gfx;

namespace CoinFlip.UI.GfxObjects
{
	/// <summary>Base class for graphics V,I,N buffers</summary>
	public class Buffers : IDisposable
	{
		/// <summary>Buffers for creating the graphics</summary>
		protected List<View3d.Vertex> m_vbuf;
		protected List<ushort> m_ibuf;
		protected List<View3d.Nugget> m_nbuf;

		public Buffers()
		{
			m_vbuf = new List<View3d.Vertex>();
			m_ibuf = new List<ushort>();
			m_nbuf = new List<View3d.Nugget>();
		}
		public virtual void Dispose()
		{
			m_vbuf = null!;
			m_ibuf = null!;
			m_nbuf = null!;
			GC.Collect();
		}
	}
}
