using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using pr.util;

namespace pr.extn
{
	public static class GfxExtensions
	{
		/// <summary>Save the tranform and clip state in an RAII object</summary>
		public static Scope<GraphicsContainer> SaveState(this Graphics gfx)
		{
			return Scope<GraphicsContainer>.Create(gfx.BeginContainer, gfx.EndContainer);
		}
	}
}
