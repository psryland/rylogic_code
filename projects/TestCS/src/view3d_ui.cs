using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using pr.common;
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
		private View3d.Object m_obj2;
		private View3d.Object m_obj3;
		private View3d.Texture m_tex0;
		private View3d.Texture m_tex2;
		private View3d.Gizmo m_giz;

		#region UI Elements
		private ToolStrip m_ts;
		private ToolStripButton m_btn_translate;
		private ToolStripButton m_btn_rotate;
		private ToolStripButton m_btn_scale;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		#endregion

		static FormView3d()
		{
			Sci.LoadDll(@"..\..\..\..\lib\$(platform)\$(config)\");
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

			m_view3d.Camera.ResetView();
			m_view3d.Camera.SetPosition(new v4(10f,10f,5f,1f), v4.Origin, v4.YAxis);

			// Simple create object
			m_obj0 = new View3d.Object("*Box test FFFFFFFF {1 2 3}", false);
			m_obj0.O2P = m4x4.Rotation(0.5f, -0.3f, 0.2f, new v4(-0.3f, 1.2f, 0.5f, 1f));
			m_view3d.Window.AddObject(m_obj0);

			// Create a texture and assign it to an object
			m_tex0 = new View3d.Texture(100,100);
			using (var tex = new View3d.Texture.Lock(m_tex0))
			{
				tex.Gfx.Clear(Color.DarkBlue);
				tex.Gfx.FillEllipse(Brushes.RoyalBlue, 10,10,80,80);
				tex.Gfx.DrawString("Paul Rulz", SystemFonts.DefaultFont, Brushes.Black, new PointF(30,40));
			}
			m_view3d.Window.RestoreRT();
			m_obj0.SetTexture(m_tex0);

			// Create object via callback
			m_obj1 = new View3d.Object("net", 0xFF0000FF, 20, 20, 1, CreateOnlyCB);
			m_view3d.Window.AddObject(m_obj1);

			// Create an object with a texture from a rendered scene
			m_obj2 = new View3d.Object("*Box Rt FFFFFFFF {0.7 0.9 0.4}", false);
			m_obj2.O2P = m4x4.Rotation(-0.5f, 0.4f, 0.6f, new v4(0.5f, -0.2f, -0.3f, 1f));
			m_view3d.Window.AddObject(m_obj2);

			// Create a texture and use it as a render target
			m_tex2 = new View3d.Texture(200,200,new View3d.TextureOptions
			{
				Format        = View3d.EFormat.DXGI_FORMAT_R8G8B8A8_UNORM,
				Mips          = 1U,
				Filter        = View3d.EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
				AddrU         = View3d.EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP,
				AddrV         = View3d.EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP,
				BindFlags     = View3d.EBindFlags.D3D11_BIND_SHADER_RESOURCE|View3d.EBindFlags.D3D11_BIND_RENDER_TARGET,
				MiscFlags     = View3d.EResMiscFlags.NONE,
				ColourKey     = 0,
				HasAlpha      = false,
				GdiCompatible = false,
				DbgName       = "Test RT",
			});
			m_view3d.Paint += (s,a) =>
			{
				// Make sure 'm_obj' is not rendered (because it uses the texture we're rendering to)
				m_view3d.Window.RemoveObject(m_obj2);
				m_view3d.Window.RenderTo(m_tex2);
				m_view3d.Window.AddObject(m_obj2);
			};
			m_obj2.SetTexture(m_tex2);

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

			// Create an object from buffers created in C#
			{
				var verts = new View3d.Vertex[]
				{
					new View3d.Vertex(new v4(1f, 1f, 0f, 1f), 0xFFFF0000),
					new View3d.Vertex(new v4(2f, 0f, 0f, 1f), 0xFF00FF00),
					new View3d.Vertex(new v4(3f, 1f, 0f, 1f), 0xFF0000FF),
				};
				var indcs = new ushort[]
				{
					0, 1, 2
				};
				var nuggets = new View3d.Nugget[]
				{
					new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr)
				};
				m_obj3 = new View3d.Object("Obj3", 0xFFFFFFFF, 3, 3, 1, verts, indcs, nuggets);
				m_view3d.Window.AddObject(m_obj3);
			}

			//m_view3d.View3d.CreateDemoScene();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_giz   );
			Util.Dispose(ref m_tex0  );
			Util.Dispose(ref m_tex2  );
			Util.Dispose(ref m_obj0  );
			Util.Dispose(ref m_obj1  );
			Util.Dispose(ref m_obj2  );
			Util.Dispose(ref m_obj3  );
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Callback for creating the "net" object</summary>
		private void CreateOnlyCB(int vcount, int icount, int ncount, View3d.Vertex[] verts, ushort[] indices, View3d.Nugget[] nuggets, out int new_vcount, out int new_icount, out int new_ncount, IntPtr ctx)
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
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
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
