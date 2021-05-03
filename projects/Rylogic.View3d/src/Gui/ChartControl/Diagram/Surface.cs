using System;
using System.Diagnostics;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	public sealed class Surface :IDisposable
	{
		// Notes:
		//  - A wrapper around a view3d texture
		public Surface(int sx, int sy, double texture_scale = 2, string? dbg_name = null)
		{
			Debug.Assert(texture_scale > 0);
			TextureScale = texture_scale;

			sx = (int)Math.Max(1.0, sx * TextureScale + 0.5);
			sy = (int)Math.Max(1.0, sy * TextureScale + 0.5);
			var opts = new View3d.TextureOptions
			{
				GdiCompatible = true,
				DbgName = dbg_name ?? string.Empty
			};
			Surf = new View3d.Texture(sx, sy, opts);// D3D11_FILTER_ANISOTROPIC});
		}
		public Surface(XElement node)
		{
			TextureScale = node.Element(XmlTag.TextureScale).As<int>();
			var sz = node.Element(XmlTag.Size).As<v2>();
			var sx = (int)Math.Max(1.0, sz.x + 0.5);
			var sy = (int)Math.Max(1.0, sz.y + 0.5);
			var opts = new View3d.TextureOptions
			{
				GdiCompatible = true,
			};
			Surf = new View3d.Texture(sx, sy, opts);// D3D11_FILTER_ANISOTROPIC});
		}
		public void Dispose()
		{
			Surf = null!;
		}

		/// <summary>Export to XML</summary>
		public XElement ToXml(XElement node)
		{
			node.Add2(XmlTag.TextureScale, TextureScale, false);
			node.Add2(XmlTag.Size, Size, false);
			return node;
		}

		/// <summary>Texture scaling factor</summary>
		public double TextureScale { get; }

		/// <summary>Get/Set the size of the texture</summary>
		public v2 Size
		{
			get => v2.From(Surf.Size) / TextureScale;
			set
			{
				// Get the size in pixels
				var tex_size = (value * TextureScale).ToSize();
				if (Surf == null || Surf.Size == tex_size)
					return;

				// Ensure positive dimensions
				if (tex_size.Width <= 0) tex_size.Width = 1;
				if (tex_size.Height <= 0) tex_size.Height = 1;
				Surf.Size = tex_size;
			}
		}

		/// <summary>The texture surface</summary>
		public View3d.Texture Surf
		{
			get => m_surf;
			private set
			{
				if (m_surf == value) return;
				Util.Dispose(ref m_surf!);
				m_surf = value;
			}
		}
		private View3d.Texture m_surf = null!;

		/// <summary>
		/// Lock the texture for drawing on.
		/// Draw in logical surface area units, ignore texture scale</summary>
		public View3d.Texture.Lock LockSurface(bool discard)
		{
			var lck = Surf.LockSurface(discard);
			lck.Gfx.ScaleTransform((float)TextureScale, (float)TextureScale);
			return lck;
		}
	}
}
