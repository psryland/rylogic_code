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
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.maths;
using pr.gfx;
using pr.gui;
using pr.util;
using pr.win32;

namespace TestWPF
{
	/// <summary>Interaction logic for diagram_ui.xaml</summary>
	public partial class DiagramUI :Window ,IDisposable
	{
		private string m_diag_xml;

		static DiagramUI()
		{
			View3d.LoadDll(@"\sdk\lib\$(platform)\$(config)\");
		}

		public DiagramUI()
		{
			InitializeComponent();

			var node0 = new DiagramControl.BoxNode("Node0"){Position = m4x4.Translation(0,0,0)};
			var node1 = new DiagramControl.BoxNode{Text = "Node1 is really long\nand contains new lines", Position = m4x4.Translation(100,100,0)};
			var node2 = new DiagramControl.BoxNode("Node2 - Paul Rulz"){Position = m4x4.Translation(-100,-50,0)};
			node1.Style = new DiagramControl.NodeStyle{Text = System.Drawing.Color.Red};

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
			
			var combo = new System.Windows.Forms.ComboBox{DataSource = new[]{"Paul","Was","Here"}};
			node4.EditControl = new DiagramControl.EditingControl(combo
				,(elem,form) => combo.SelectedItem = elem.As<DiagramControl.Node>().Text
				,elem => elem.As<DiagramControl.Node>().Text = combo.SelectedItem.As<string>());
			
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

			m_menu_file_clear.Click += (s,a) => m_diag.ResetDiagram();
			m_menu_file_load.Click += (s,a) => m_diag.ImportXml(m_diag_xml, m_diag.Elements.Count != 0);
			m_menu_file_save.Click += (s,a) => m_diag_xml = m_diag.ExportXml().ToString();
			m_menu_file_load_options.Click += (s,a) => m_diag.Options = XDocument.Load("P:\\dump\\diag_options.xml").Root.Element("options").As<DiagramControl.DiagramOptions>();
			m_menu_file_editable.Click += (s,a) => m_menu_file_editable.IsChecked = m_diag.AllowEditing = !m_diag.AllowEditing;

		//	m_toolstripcont.Controls.Add(m_diag.EditToolstrip);
			m_diag.EditToolstrip.Visible = true;
			m_diag.EditToolstrip.ImageScalingSize = new System.Drawing.Size(22,22);
			m_diag.EditToolstrip.AutoSize = true;

			m_diag.DiagramChanged += (s,a) =>
				{
					//switch (a.ChgType)
					//{
					//case DiagramControl.EDiagramChangeType.MoveLinkBegin:
					//	a.Cancel = true;
					//	break;
					//}
				};
		}
		public void Dispose()
		{
			Util.Dispose(ref m_diag);
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			ComponentDispatcher.ThreadFilterMessage += HandleThreadMessage;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			ComponentDispatcher.ThreadFilterMessage -= HandleThreadMessage;
		}

		private void HandleThreadMessage(ref MSG msg, ref bool handled)
		{
			switch ((uint)msg.message)
			{
			case Win32.WM_MOUSEMOVE:
				if (m_diag != null)
				{
					var mouse = Mouse.GetPosition(this);
					var pt = m_diag.ClientToDiagram(new System.Drawing.Point((int)mouse.X,(int)mouse.Y));
					m_status_mouse_pos.Text = "Pos: {0} {1}".Fmt(pt.x.ToString("F3"), pt.y.ToString("F3"));
				}
				break;
			}
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
	}
}
