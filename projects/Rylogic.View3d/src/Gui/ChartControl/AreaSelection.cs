using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

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
					void HandleMoved(object sender, ChartMovedEventArgs e)
					{
						UpdateGfx();
					}
				}
			}
			private ChartControl m_chart = null!;

			/// <summary>The selection area graphic</summary>
			public Rectangle Area { get; }

			/// <summary>The chart-space area of selection</summary>
			public Rect Selection
			{
				get => m_selection;
				set
				{
					if (m_selection == value) return;
					m_selection = value;
					UpdateGfx();
				}
			}
			private Rect m_selection;

			/// <summary>Scale and position the selection rectangle</summary>
			private void UpdateGfx()
			{
				var pt = Chart.ChartToClient(Selection.Location);
				var sz = Chart.ChartToClient(Selection.Size);

				Canvas.SetLeft(Area, Math.Min(pt.X, pt.X + sz.Width));
				Canvas.SetTop(Area, Math.Min(pt.Y, pt.Y - sz.Height));
				Area.Width = Math.Abs(sz.Width);
				Area.Height = Math.Abs(sz.Height);
			}
		}
	}
}
