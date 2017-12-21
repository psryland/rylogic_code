using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.scintilla;
using pr.util;
using pr.view3d;
using pr.win32;
using ComboBox = pr.gui.ComboBox;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace TestCS
{
	public class DiagramControlUI :Form
	{
		private DiagramControl m_diag;
		private MenuStrip menuStrip1;
		private ToolStripMenuItem toolsToolStripMenuItem;
		private ToolStripMenuItem m_menu_tools_save;
		private ToolStripMenuItem m_menu_tools_load;
		private ToolStripMenuItem m_menu_tools_clear;
		private ToolStripMenuItem m_menu_tools_loadmmapdiag;
		private ToolStripMenuItem m_menu_tools_load_options;
		private ToolStripMenuItem m_menu_tools_allowediting;
		private ToolStripContainer m_toolstripcont;
		private IMessageFilter m_filter;

		static DiagramControlUI()
		{
			Sci.LoadDll(@"\lib\$(platform)\$(config)\");
			View3d.LoadDll(@"\lib\$(platform)\$(config)\");
		}

		private StatusStrip statusStrip1;
		private ToolStripStatusLabel m_status_mouse_pos;
		private string m_diag_xml;

		public DiagramControlUI()
		{
			InitializeComponent();

			ClientSize = new Size(640,480);

			var node0 = new DiagramControl.BoxNode("Node0"){Position = m4x4.Translation(0,0,0)};
			var node1 = new DiagramControl.BoxNode{Text = "Node1 is really long\nand contains new lines", Position = m4x4.Translation(100,100,0)};
			var node2 = new DiagramControl.BoxNode("Node2 - Paul Rulz"){Position = m4x4.Translation(-100,-50,0)};
			node1.Style = new DiagramControl.NodeStyle{Text = Color.Red};

			m_diag.Elements.Add(node0);
			m_diag.Elements.Add(node1);
			m_diag.Elements.Add(node2);
			
			node2.Enabled = false;

			m_diag.DefaultConnectorStyle.Smooth = true;
			var conn_type = DiagramControl.Connector.EType.BiDir;
			
			m_diag.Elements.Add(new DiagramControl.Connector(node0, node1){Type = conn_type});
			m_diag.Elements.Add(new DiagramControl.Connector(node1, node2){Type = conn_type});
			m_diag.Elements.Add(new DiagramControl.Connector(node2, node0){Type = conn_type});

			var node4 = new DiagramControl.BoxNode("Node4"){PositionXY = new v2(-80, 60)};
			var node5 = new DiagramControl.BoxNode("Node5"){PositionXY = new v2( 80,-60)};
			node4.Diagram = m_diag;
			node5.Diagram = m_diag;
			
			var combo = new ComboBox{DataSource = new[]{"Paul","Was","Here"}};
			node4.EditControl = new DiagramControl.EditingControl(combo
				,(elem,form) => combo.SelectedItem = ((DiagramControl.Node)elem).Text
				,elem => ((DiagramControl.Node)elem).Text = (string)combo.SelectedItem);
			
			var conn4 = new DiagramControl.Connector(node4, node2){Type = conn_type};
			var conn5 = new DiagramControl.Connector(node5, node4){Type = conn_type};
			var conn6 = new DiagramControl.Connector(node1, node4){Type = conn_type};
			conn4.Diagram = m_diag;
			conn5.Diagram = m_diag;
			conn6.Diagram = m_diag;
			
			node5.Dispose();
			node5 = null;

			var lbl1 = new Joypad();
			lbl1.Elem = conn5;
			lbl1.Diagram = m_diag;

			m_diag.ResetView();

			node0.BringToFront();

			m_menu_tools_clear.Click += (s,a) => m_diag.ResetDiagram();
			m_menu_tools_load.Click += (s,a) => m_diag.ImportXml(m_diag_xml, m_diag.Elements.Count != 0);
			m_menu_tools_save.Click += (s,a) => m_diag_xml = m_diag.ExportXml().ToString();
			m_menu_tools_loadmmapdiag.Click += (s,a) => m_diag.ImportXml(XDocument.Load("P:\\dump\\mmap_diag.xml").Root, true);
			m_menu_tools_load_options.Click += (s,a) => m_diag.Options = XDocument.Load("P:\\dump\\diag_options.xml").Root.Element("options").As<DiagramControl.DiagramOptions>();
			m_menu_tools_allowediting.Click += (s,a) => m_menu_tools_allowediting.Checked = m_diag.AllowEditing = !m_diag.AllowEditing;

			m_toolstripcont.TopToolStripPanel.Controls.Add(m_diag.EditToolstrip);
			m_diag.EditToolstrip.Visible = true;
			m_diag.EditToolstrip.ImageScalingSize = new Size(22,22);
			m_diag.EditToolstrip.AutoSize = true;
			
			m_filter = new Filter(this);
			Load += (s,a) => Application.AddMessageFilter(m_filter);
			FormClosed += (s,a) => Application.RemoveMessageFilter(m_filter);

			m_diag.DiagramChanged += (s,a) =>
				{
					//switch (a.ChgType)
					//{
					//case DiagramControl.EDiagramChangeType.MoveLinkBegin:
					//	a.Cancel = true;
					//	break;
					//}
				};
	
			//const string options_filepath = ;
			//var xml = new XDocument();
			//xml.Add2(new XElement("root")).Add2("options", m_diag.Options, false);
			//xml.Save(options_filepath);

			//var tim = new System.Windows.Forms.Timer{Interval = 10};
			//tim.Tick += (s,a) =>
			//	{
			//		try
			//		{
			//			m_diag.Options = XDocument.Load(options_filepath).Root.Element("options").As<DiagramControl.DiagramOptions>();
			//			m_diag.ScatterNodes();
			//		}
			//		catch (Exception)
			//		{}
			//	};
			//tim.Start();

		}
		protected override void Dispose(bool disposing)
		{
			Util.DisposeAll(m_diag.Elements.ToArray());
			Util.Dispose(ref m_diag);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private class Joypad :DiagramControl.Label
		{
			private View3d.Object m_gfx;
			public Joypad()
			{
				m_gfx = new View3d.Object();
				EditControl = null;
			}
			//protected override void RefreshInternal()
			//{
			//	var ldr = new pr.ldr.LdrBuilder();
			//	ldr.Append("*Box b FF00FF00 {20}");
			//	m_gfx.UpdateModel(ldr.ToString(), View3d.EUpdateObject.All ^ View3d.EUpdateObject.Transform);
			//	m_gfx.O2P = Position;
			//}
			public override DiagramControl.HitTestResult.Hit HitTest(v2 point, View3d.Camera cam)
			{
				if ((PositionXY - point).Length2 > 20)
					return null;

				point -= PositionXY;
				return new DiagramControl.HitTestResult.Hit(this, point);
			}

			/// <summary>Add the graphics associated with this element to the drawset</summary>
			protected override void AddToSceneCore(View3d.Window window)
			{
				window.AddObject(m_gfx);
			}
		}

		private class Filter :IMessageFilter
		{
			private readonly DiagramControlUI m_form;
			public Filter(DiagramControlUI form) { m_form = form; }
			public bool PreFilterMessage(ref Message msg)
			{
				switch ((uint)msg.Msg)
				{
				case Win32.WM_MOUSEMOVE:
					if (m_form.m_diag != null)
					{
						var pt = m_form.m_diag.ClientToDiagram(m_form.m_diag.PointToClient(MousePosition));
						m_form.m_status_mouse_pos.Text = "Pos: {0} {1}".Fmt(pt.x.ToString("F3"), pt.y.ToString("F3"));
					}
					break;
				}
				return false;
			}
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			pr.gui.DiagramControl.DiagramOptions diagramOptions1 = new pr.gui.DiagramControl.DiagramOptions();
			pr.gui.DiagramControl.DiagramOptions.NodeOptions nodeOptions1 = new pr.gui.DiagramControl.DiagramOptions.NodeOptions();
			pr.gui.DiagramControl.DiagramOptions.ScatterOptions scatterOptions1 = new pr.gui.DiagramControl.DiagramOptions.ScatterOptions();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.m_status_mouse_pos = new System.Windows.Forms.ToolStripStatusLabel();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_save = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_load = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_clear = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_loadmmapdiag = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_load_options = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tools_allowediting = new System.Windows.Forms.ToolStripMenuItem();
			this.m_toolstripcont = new ToolStripContainer();
			this.m_diag = new pr.gui.DiagramControl();
			this.statusStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.m_toolstripcont.BottomToolStripPanel.SuspendLayout();
			this.m_toolstripcont.ContentPanel.SuspendLayout();
			this.m_toolstripcont.TopToolStripPanel.SuspendLayout();
			this.m_toolstripcont.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).BeginInit();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status_mouse_pos});
			this.statusStrip1.Location = new System.Drawing.Point(0, 0);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(552, 22);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// m_status_mouse_pos
			// 
			this.m_status_mouse_pos.Name = "m_status_mouse_pos";
			this.m_status_mouse_pos.Size = new System.Drawing.Size(118, 17);
			this.m_status_mouse_pos.Text = "toolStripStatusLabel1";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(3, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(152, 24);
			this.menuStrip1.Stretch = false;
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// toolsToolStripMenuItem
			// 
			this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tools_save,
            this.m_menu_tools_load,
            this.m_menu_tools_clear,
            this.m_menu_tools_loadmmapdiag,
            this.m_menu_tools_load_options,
            this.m_menu_tools_allowediting});
			this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
			this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this.toolsToolStripMenuItem.Text = "&Tools";
			// 
			// m_menu_tools_save
			// 
			this.m_menu_tools_save.Name = "m_menu_tools_save";
			this.m_menu_tools_save.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_save.Text = "Save";
			// 
			// m_menu_tools_load
			// 
			this.m_menu_tools_load.Name = "m_menu_tools_load";
			this.m_menu_tools_load.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_load.Text = "Load";
			// 
			// m_menu_tools_clear
			// 
			this.m_menu_tools_clear.Name = "m_menu_tools_clear";
			this.m_menu_tools_clear.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_clear.Text = "Clear";
			// 
			// m_menu_tools_loadmmapdiag
			// 
			this.m_menu_tools_loadmmapdiag.Name = "m_menu_tools_loadmmapdiag";
			this.m_menu_tools_loadmmapdiag.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_loadmmapdiag.Text = "Load mmap_diag";
			// 
			// m_menu_tools_load_options
			// 
			this.m_menu_tools_load_options.Name = "m_menu_tools_load_options";
			this.m_menu_tools_load_options.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_load_options.Text = "Load Options";
			// 
			// m_menu_tools_allowediting
			// 
			this.m_menu_tools_allowediting.Name = "m_menu_tools_allowediting";
			this.m_menu_tools_allowediting.Size = new System.Drawing.Size(166, 22);
			this.m_menu_tools_allowediting.Text = "AllowEditing";
			// 
			// m_toolstripcont
			// 
			// 
			// m_toolstripcont.BottomToolStripPanel
			// 
			this.m_toolstripcont.BottomToolStripPanel.Controls.Add(this.statusStrip1);
			// 
			// m_toolstripcont.ContentPanel
			// 
			this.m_toolstripcont.ContentPanel.Controls.Add(this.m_diag);
			this.m_toolstripcont.ContentPanel.Size = new System.Drawing.Size(527, 520);
			this.m_toolstripcont.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_toolstripcont.Location = new System.Drawing.Point(0, 0);
			this.m_toolstripcont.Margin = new System.Windows.Forms.Padding(0);
			this.m_toolstripcont.Name = "m_toolstripcont";
			this.m_toolstripcont.Size = new System.Drawing.Size(552, 566);
			this.m_toolstripcont.TabIndex = 4;
			this.m_toolstripcont.Text = "toolStripContainer1";
			// 
			// m_toolstripcont.TopToolStripPanel
			// 
			this.m_toolstripcont.TopToolStripPanel.Controls.Add(this.menuStrip1);
			// 
			// m_diag
			// 
			this.m_diag.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_diag.Location = new System.Drawing.Point(0, 0);
			this.m_diag.Name = "m_diag";
			diagramOptions1.BkColour = System.Drawing.SystemColors.ControlDark;
			nodeOptions1.AnchorSharingBias = 70F;
			nodeOptions1.AnchorSpacing = 25F;
			nodeOptions1.AutoRelink = false;
			nodeOptions1.Margin = 30F;
			diagramOptions1.Node = nodeOptions1;
			scatterOptions1.ConnectorScale = 1F;
			scatterOptions1.CoulombConstant = 1000F;
			scatterOptions1.Equilibrium = 0.01F;
			scatterOptions1.MaxIterations = 20;
			scatterOptions1.SpringConstant = 0.01F;
			diagramOptions1.Scatter = scatterOptions1;
			this.m_diag.Options = diagramOptions1;
			this.m_diag.Size = new System.Drawing.Size(527, 520);
			this.m_diag.TabIndex = 0;
			// 
			// DiagramControlUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(552, 566);
			this.Controls.Add(this.m_toolstripcont);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "DiagramControlUI";
			this.Text = "DiagramControl";
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.m_toolstripcont.BottomToolStripPanel.ResumeLayout(false);
			this.m_toolstripcont.BottomToolStripPanel.PerformLayout();
			this.m_toolstripcont.ContentPanel.ResumeLayout(false);
			this.m_toolstripcont.TopToolStripPanel.ResumeLayout(false);
			this.m_toolstripcont.TopToolStripPanel.PerformLayout();
			this.m_toolstripcont.ResumeLayout(false);
			this.m_toolstripcont.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
