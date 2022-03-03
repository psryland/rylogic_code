using System;
using System.Windows.Controls;
using System.Windows.Shapes;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		public sealed class AreaSelection :IDisposable
		{
			public AreaSelection(ChartControl chart)
			{
				Chart = chart;
				Area = new Rectangle
				{
					Stroke = Chart.Options.SelectionColour.ToMediaBrush(),
					Fill = Chart.Options.SelectionColour.ToMediaBrush(),
					StrokeThickness = 1.0,
					IsHitTestVisible = false,
				};

				Chart.Overlay.Adopt(Area);
			}
			public void Dispose()
			{
				Area.Detach();
				Chart = null!;
			}

			/// <summary>The owning chart</summary>
			private ChartControl Chart
			{
				get => m_chart;
				set
				{
					if (m_chart == value) return;
					if (m_chart != null)
					{
						m_chart.ChartMoved -= HandleMoved;
					}
					m_chart = value;
					if (m_chart != null)
					{
						m_chart.ChartMoved += HandleMoved;
					}

					// Handlers
					void HandleMoved(object? sender, ChartMovedEventArgs e)
					{
						UpdateGfx();
					}
				}
			}
			private ChartControl m_chart = null!;

			/// <summary>The selection area graphic</summary>
			public Rectangle Area { get; }

			/// <summary>The chart-space selection volume</summary>
			public BBox Selection
			{
				get => m_selection;
				set
				{
					if (m_selection == value) return;
					m_selection = value;
					UpdateGfx();
				}
			}
			private BBox m_selection;

			/// <summary>Scale and position the selection rectangle</summary>
			private void UpdateGfx()
			{
				var scene_bbox = Chart.ChartToScene(Selection);
				var pt = scene_bbox.TopLeft;
				var sz = scene_bbox.Size;

				Canvas.SetLeft(Area, pt.X);
				Canvas.SetTop(Area, pt.Y);
				Area.Width = sz.Width;
				Area.Height = sz.Height;
			}
		}
	}
}
