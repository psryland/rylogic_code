using System;
using System.ComponentModel;
using System.Windows.Controls;
using Rylogic.Container;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Maths;
using Rylogic.Utility;

namespace ADUFO;

public partial class Diagram : UserControl, IDisposable, IDockable, INotifyPropertyChanged, IDiagramCMenuContext, IChartCMenuContext, IView3dCMenuContext
{
	public Diagram()
	{
		InitializeComponent();
		DockControl = new DockControl(this, "Diagram")
		{
			CaptionText = "Diagram",
		};

		Chart = m_ui_diagram;
		Chart.Options = new Diagram_.Options
		{
			Orthographic = false,
			BackgroundColour = 0xFFD9C4AA,
			AreaSelectRequiresShiftKey = true,
			AllowSelection = true,
			AllowElementDragging = true,
			Relink =
			{
				AnchorSharingMode = Node.EAnchorSharing.NoSharing,
			}
		};
		DiagramCMenuContext = new Diagram_.CMenu(Chart);

		DataContext = this;
	}
	public void Dispose()
	{
		ClearDiagram(recycle: false);
		Chart = null!;
		DockControl = null!;
	}

	/// <summary>Provides support for the DockContainer</summary>
	public DockControl DockControl
	{
		get => m_dock_control;
		private set
		{
			if (m_dock_control == value) return;
			Util.Dispose(ref m_dock_control!);
			m_dock_control = value;
		}
	}
	private DockControl m_dock_control = null!;

	/// <summary>The diagram control</summary>
	public ChartControl Chart
	{
		get => m_diagram;
		private set
		{
			if (m_diagram == value) return;
			if (m_diagram != null)
			{
				m_diagram.Selected.ListChanging -= HandleSelectedChanging;
			}
			m_diagram = value;
			if (m_diagram != null)
			{
				m_diagram.Camera.AlignAxis = v4.YAxis;
				m_diagram.Scene.ContextMenu.DataContext = this;
				m_diagram.Scene.Window.LightProperties = View3d.LightInfo.Directional(-v4.ZAxis, specular: 0xFF000000, spec_power: 0, camera_relative: true);
				m_diagram.Selected.ListChanging += HandleSelectedChanging;
			}

			// Handlers
			void HandleSelectedChanging(object? sender, ListChgEventArgs<ChartControl.Element> e)
			{
				if (e.After && e.ChangeType == ListChg.ItemAdded)
				{
					//// Make the selected element the current item
					//if (e.Item is StanceNode node)
					//	MMapView.Stances.MoveCurrentTo(node.Stance);
					//else if (e.Item is MovementConnector conn)
					//	MMapView.Movements.MoveCurrentTo(conn.Movement);
				}
			}
		}
	}
	private ChartControl m_diagram = null!;
	
	/// <summary>Remove all nodes and connectors from the chart</summary>
	private void ClearDiagram(bool recycle)
	{
#if false
		// If recycling the nodes, just remove them all from the diagram
		if (recycle)
		{
			foreach (var node in StanceNodes.Values)
			{
				node.DetachConnectors();
				node.Chart = null;
			}
			foreach (var conn in MovementConnectors.Values)
			{
				conn.DetachNodes();
				conn.Chart = null;
			}
		}
		else
		{
			Util.DisposeRange(StanceNodes.Values);
			Util.DisposeRange(MovementConnectors.Values);
			StanceNodes.Clear();
			MovementConnectors.Clear();
		}
#endif
	}

	/// <inheritdoc/>
	public IView3dCMenu View3dCMenuContext => Chart.View3dCMenuContext;

	/// <inheritdoc/>
	public IChartCMenu ChartCMenuContext => Chart.ChartCMenuContext;

	/// <inheritdoc/>
	public IDiagramCMenu DiagramCMenuContext { get; }

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
}

