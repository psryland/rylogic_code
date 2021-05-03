using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Maths;

namespace TestWPF
{
	public partial class Diagram2UI :Window, IChartCMenuContext, IView3dCMenuContext, IDiagramCMenuContext
	{
		public Diagram2UI()
		{
			InitializeComponent();

			// Setup the chart for diagram mode
			DiagramOptions = new Diagram_.Options();
			DiagramCMenu = new Diagram_.CMenu(m_diag, DiagramOptions);
			Diagram_.Init(m_diag, DiagramOptions);

			var bs = new NodeStyle
			{
				AutoSize     = true,
				Resizeable   = true,
				Border       = Colour32.Black,
				Fill         = Colour32.LightSteelBlue,
				Hovered      = Colour32.LightGreen,
				Selected     = Colour32.SteelBlue,
				Text         = Colour32.Black,
				TextDisabled = Colour32.Gray,
			};
			var cs = new ConnectorStyle
			{
				Line = Colour32.DarkGreen,
				Dangling = Colour32.Red,
				Selected = Colour32.SteelBlue,
				Smooth = true,
				Width = 10.0,
			};

			var node0 = new QuadNode("Node0", position: m4x4.Translation(-100, -50, 0), style: bs) { Chart = m_diag };
			var node1 = new QuadNode("Node1", position: m4x4.Translation(100, 100, 0), style: bs) { Chart = m_diag };
			var node2 = new QuadNode("Node2", position: m4x4.Translation(200, -150, 0), style: bs) { Chart = m_diag };
			var node3 = new QuadNode("Node3", position: m4x4.Translation(-200, 200, 0), style: bs) { Chart = m_diag };
			var node4 = new QuadNode("Node4", position: m4x4.Translation(50, -200, 0), style: bs) { Chart = m_diag };

			var conn0 = new Connector(node0, node1, type:Connector.EType.BiDir, style:cs) { Chart = m_diag };
			var conn1 = new Connector(node0, node2, type: Connector.EType.BiDir, style:cs) { Chart = m_diag };
			var conn2 = new Connector(node1, node2, type: Connector.EType.BiDir, style: cs) { Chart = m_diag };
			var conn3 = new Connector(node1, node3, type: Connector.EType.Forward, style: cs) { Chart = m_diag };
			var conn4 = new Connector(node4, node2, type: Connector.EType.Back, style: cs) { Chart = m_diag };

			m_diag.Scene.ContextMenu.DataContext = this;
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			Gui_.DisposeChildren(this, EventArgs.Empty);
			base.OnClosed(e);
		}

		private Diagram_.Options DiagramOptions { get; }
		private Diagram_.CMenu DiagramCMenu { get; }

		/// <inheritdoc/>
		public IView3dCMenu View3dCMenuContext => m_diag.Scene.View3dCMenuContext;

		/// <inheritdoc/>
		public IChartCMenu ChartCMenuContext => m_diag.Scene.ChartCMenuContext;

		/// <inheritdoc/>
		public IDiagramCMenu DiagramCMenuContext => DiagramCMenu;
	}
}
