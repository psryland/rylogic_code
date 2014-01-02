using System;
using System.Windows.Forms;
using pr.gui;
using pr.maths;

namespace TestCS
{
	public class FormView3d : Form
	{
		private readonly View3D m_view3d;
		private readonly View3D.Object m_obj0;
		private readonly View3D.Object m_obj1;

		public FormView3d()
		{
			InitializeComponent();
			m_view3d = new View3D
			{
				Name        = "m_view3d",
				BorderStyle = BorderStyle.FixedSingle,
				Dock        = DockStyle.Fill
			};
			Controls.Add(m_view3d);

			m_obj0 = new View3D.Object("*Box test FF00FF00 {1 2 3}");
			m_view3d.Drawset.AddObject(m_obj0);

			m_obj1 = new View3D.Object("net", 0xFF0000FF, 20, 20,
				(int vcount, int icount, View3D.Vertex[] verts, ushort[] indices, out int new_vcount, out int new_icount, out View3D.EPrim prim_type, out View3D.EGeom geom_type, ref View3D.Material mat, IntPtr ctx) =>
				{
					new_vcount = 0;
					new_icount = 0;
					for (int i = 0; i != 10; ++i)
					{
						verts[new_vcount++] = new View3D.Vertex(new v4(i,0,0,1f));
						verts[new_vcount++] = new View3D.Vertex(new v4(0,0,9-i,1f));
						indices[new_icount++] = (ushort)(new_vcount - 2);
						indices[new_icount++] = (ushort)(new_vcount - 1);
					}
					prim_type = View3D.EPrim.D3D_PRIMITIVE_TOPOLOGY_LINELIST;
					geom_type = View3D.EGeom.Vert;
				});
			m_view3d.Drawset.AddObject(m_obj1);

			m_view3d.CreateDemoScene();
			m_view3d.Drawset.Camera.ResetView();

			m_view3d.Drawset.Camera.SetPosition(new v4(10f,10f,5f,1f), v4.Origin, v4.YAxis);
		}
		protected override void Dispose(bool disposing)
		{
			if (m_obj0 != null) m_obj0.Dispose();
			if (m_obj1 != null) m_obj1.Dispose();
			if (m_view3d != null) m_view3d.Dispose();
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Text = "FormView3d";
		}

		#endregion
	}
}
