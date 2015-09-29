using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace TestCS
{
	public class FormView3d : Form
	{
		private View3dControl m_view3d;
		private View3d.Object m_obj0;
		private View3d.Object m_obj1;
		private View3d.Texture m_tex0;
		private View3d.Gizmo m_giz;

		static FormView3d()
		{
			View3d.LoadDll(@"..\..\..\..\lib\$(platform)\$(config)\");
		}
		public FormView3d()
		{
			InitializeComponent();
			m_view3d = new View3dControl
			{
				Name        = "m_view3d",
				BorderStyle = BorderStyle.FixedSingle,
				Dock        = DockStyle.Fill
			};
			Controls.Add(m_view3d);

			// Simple create object
			m_obj0 = new View3d.Object("*Box test FFFFFFFF {1 2 3}", false);
			m_obj0.O2P = m4x4.Rotation(0.5f, -0.3f, 0.2f, new v4(-0.3f, 1.2f, 0.5f, 1f));
			m_view3d.Window.AddObject(m_obj0);

			// Create object via callback
			m_obj1 = new View3d.Object("net", 0xFF0000FF, 20, 20,
				(int vcount, int icount, View3d.Vertex[] verts, ushort[] indices, out int new_vcount, out int new_icount, out View3d.EPrim prim_type, out View3d.EGeom geom_type, ref View3d.Material mat, IntPtr ctx) =>
				{
					new_vcount = 0;
					new_icount = 0;
					for (int i = 0; i != 10; ++i)
					{
						verts[new_vcount++] = new View3d.Vertex(new v4(i,0,0,1f));
						verts[new_vcount++] = new View3d.Vertex(new v4(0,0,9-i,1f));
						indices[new_icount++] = (ushort)(new_vcount - 2);
						indices[new_icount++] = (ushort)(new_vcount - 1);
					}
					prim_type = View3d.EPrim.D3D_PRIMITIVE_TOPOLOGY_LINELIST;
					geom_type = View3d.EGeom.Vert;
				});
			m_view3d.Window.AddObject(m_obj1);

			// Create a texture and assign it to an object
			m_tex0 = new View3d.Texture(100,100);
			using (var tex = new View3d.Texture.Lock(m_tex0))
			{
				tex.Gfx.Clear(Color.DarkBlue);
				tex.Gfx.FillEllipse(Brushes.RoyalBlue, 10,10,80,80);
				tex.Gfx.DrawString("Paul Rulz", SystemFonts.DefaultFont, Brushes.Black, new PointF(30,40));
			}
			m_obj0.SetTexture(m_tex0);

			// Create a gizmo for moving objects in the scene
			// Position it at the origin of m_obj0 and scale by 2
			m_giz = new View3d.Gizmo(View3d.Gizmo.EMode.Scale, m_obj0.O2P * m4x4.Scale(0.1f, v4.Origin));
			m_giz.Attach(m_obj0);
			m_giz.Moved += (s,a) =>
				{
					m_status.SetStatusMessage(msg:"Gizmo", display_time:TimeSpan.FromSeconds(2));
				};
			m_view3d.Window.AddGizmo(m_giz);

			m_btn_translate.Click += (s,a) => m_giz.Mode = View3d.Gizmo.EMode.Translate;
			m_btn_rotate   .Click += (s,a) => m_giz.Mode = View3d.Gizmo.EMode.Rotate;
			m_btn_scale    .Click += (s,a) => m_giz.Mode = View3d.Gizmo.EMode.Scale;

			//m_view3d.View3d.CreateDemoScene();
			
			m_view3d.Camera.ResetView();
			m_view3d.Camera.SetPosition(new v4(10f,10f,5f,1f), v4.Origin, v4.YAxis);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_giz   );
			Util.Dispose(ref m_tex0  );
			Util.Dispose(ref m_obj0  );
			Util.Dispose(ref m_obj1  );
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private ToolStrip m_ts;
		private ToolStripButton m_btn_translate;
		private ToolStripButton m_btn_rotate;
		private ToolStripButton m_btn_scale;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormView3d));
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_translate = new System.Windows.Forms.ToolStripButton();
			this.m_btn_rotate = new System.Windows.Forms.ToolStripButton();
			this.m_btn_scale = new System.Windows.Forms.ToolStripButton();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_ts.SuspendLayout();
			this.m_ss.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_ts
			// 
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_translate,
            this.m_btn_rotate,
            this.m_btn_scale});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(352, 25);
			this.m_ts.TabIndex = 0;
			this.m_ts.Text = "toolStrip1";
			// 
			// m_btn_translate
			// 
			this.m_btn_translate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.m_btn_translate.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_translate.Image")));
			this.m_btn_translate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_translate.Name = "m_btn_translate";
			this.m_btn_translate.Size = new System.Drawing.Size(59, 22);
			this.m_btn_translate.Text = "Translate";
			// 
			// m_btn_rotate
			// 
			this.m_btn_rotate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.m_btn_rotate.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_rotate.Image")));
			this.m_btn_rotate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_rotate.Name = "m_btn_rotate";
			this.m_btn_rotate.Size = new System.Drawing.Size(45, 22);
			this.m_btn_rotate.Text = "Rotate";
			// 
			// m_btn_scale
			// 
			this.m_btn_scale.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.m_btn_scale.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_scale.Image")));
			this.m_btn_scale.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_scale.Name = "m_btn_scale";
			this.m_btn_scale.Size = new System.Drawing.Size(38, 22);
			this.m_btn_scale.Text = "Scale";
			// 
			// m_ss
			// 
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 320);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(352, 22);
			this.m_ss.TabIndex = 1;
			this.m_ss.Text = "statusStrip1";
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(56, 17);
			this.m_status.Text = "Stay Tuss";
			// 
			// FormView3d
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(352, 342);
			this.Controls.Add(this.m_ss);
			this.Controls.Add(this.m_ts);
			this.Name = "FormView3d";
			this.Text = "FormView3d";
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.m_ss.ResumeLayout(false);
			this.m_ss.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
