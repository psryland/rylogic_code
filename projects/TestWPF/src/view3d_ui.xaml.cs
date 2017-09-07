using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace TestWPF
{
	/// <summary>Interaction logic for view3d_ui.xaml</summary>
	public partial class View3dUI :Window ,IDisposable
	{
		private View3d.Object m_obj0;
		private View3d.Object m_obj1;
		private View3d.Texture m_tex0;

		static View3dUI()
		{
			View3d.LoadDll(@"\sdk\lib\$(platform)\$(config)\");
		}
		
		//private View3dControl m_view3d;
		public View3dUI()
		{
			InitializeComponent();

			m_view3d.BorderStyle = BorderStyle.FixedSingle;

			// Simple create object
			m_obj0 = new View3d.Object("*Box test FFFFFFFF {1 2 3}", false);
			m_view3d.Window.AddObject(m_obj0);

			// Create object via callback
			m_obj1 = new View3d.Object("net", 0xFF0000FF, 20, 20, 1,
				(IntPtr ctx, int vcount, int icount, int ncount, View3d.Vertex[] verts, ushort[] indices, View3d.Nugget[] nuggets, out int new_vcount, out int new_icount, out int new_ncount) =>
				{
					new_vcount = 0;
					new_icount = 0;
					new_ncount = 0;
					for (int i = 0; i != 10; ++i)
					{
						verts[new_vcount++] = new View3d.Vertex(new v4(i,0,0,1f));
						verts[new_vcount++] = new View3d.Vertex(new v4(0,0,9-i,1f));
						indices[new_icount++] = (ushort)(new_vcount - 2);
						indices[new_icount++] = (ushort)(new_vcount - 1);
					}
					nuggets[new_ncount++] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert);
				});
			m_view3d.Window.AddObject(m_obj1);

			// Create a texture and assign it to an object
			m_tex0 = new View3d.Texture(100,100);
			using (var tex = new View3d.Texture.Lock(m_tex0, discard:true))
			{
				tex.Gfx.Clear(System.Drawing.Color.DarkBlue);
				tex.Gfx.FillEllipse(System.Drawing.Brushes.RoyalBlue, 10,10,80,80);
				tex.Gfx.DrawString("Paul Rulz", System.Drawing.SystemFonts.DefaultFont, System.Drawing.Brushes.Black, new PointF(30,40));
			}
			m_obj0.SetTexture(m_tex0);

			//m_view3d.View3d.CreateDemoScene();
			
			m_view3d.Camera.ResetView();
			m_view3d.Camera.SetPosition(new v4(10f,10f,5f,1f), v4.Origin, v4.YAxis);
		}
		public void Dispose()
		{
			Util.Dispose(ref m_tex0);
			Util.Dispose(ref m_obj0);
			Util.Dispose(ref m_obj1);
			Util.Dispose(ref m_view3d);
		}
	}
}
