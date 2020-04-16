using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Extn.Windows;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		public sealed class TapeMeasure :IDisposable
		{
			private readonly bool m_xhair_enabled;
			public TapeMeasure(ChartControl chart)
			{
				Chart = chart;
				Chart.MouseOperations.Pending[MouseButton.Left] = new DrawTape(this);

				m_xhair_enabled = Chart.ShowCrossHair;
				Chart.ShowCrossHair = true;

				// A slightly darker or lighter shade of the background colour
				var stroke = Chart.Options.BackgroundColour.Lerp(Chart.Options.BackgroundColour.InvertBW(), 0.3);
				var bkgd = Chart.Options.BackgroundColour.Lerp(Chart.Options.BackgroundColour.InvertBW(), 0.1);
				var fill = bkgd.Alpha(0.5);
				Area = new Rectangle
				{
					Stroke = stroke.ToMediaBrush(),
					Fill = fill.ToMediaBrush(),
					StrokeThickness = 0.8,
					IsHitTestVisible = false,
				};
				DeltaX = new Line
				{
					Stroke = stroke.ToMediaBrush(),
					StrokeDashArray = ELineStyles.Dashed.ToStrokeDashArray(),
					StrokeEndLineCap = PenLineCap.Triangle,
					StrokeThickness = 0.8,
					IsHitTestVisible = false,
				};
				DeltaY = new Line
				{
					Stroke = stroke.ToMediaBrush(),
					StrokeDashArray = ELineStyles.Dashed.ToStrokeDashArray(),
					StrokeEndLineCap = PenLineCap.Triangle,
					StrokeThickness = 0.8,
					IsHitTestVisible = false,
				};
				DeltaD = new Line
				{
					Stroke = stroke.ToMediaBrush(),
					StrokeDashArray = ELineStyles.Dashed.ToStrokeDashArray(),
					StrokeEndLineCap = PenLineCap.Triangle,
					StrokeThickness = 1.0,
					IsHitTestVisible = false,
				};
				LabelX = new TextBlock
				{
					FontSize = 10,
					Foreground = fill.InvertBW().ToMediaBrush(),
					Background = bkgd.ToMediaBrush(),
					IsHitTestVisible = false,
					Margin = new Thickness(3),
				};
				LabelY = new TextBlock
				{
					FontSize = 10,
					Foreground = fill.InvertBW().ToMediaBrush(),
					Background = bkgd.ToMediaBrush(),
					IsHitTestVisible = false,
					Margin = new Thickness(3),
				};
				LabelD = new TextBlock
				{
					FontSize = 10,
					Foreground = fill.InvertBW().ToMediaBrush(),
					Background = bkgd.ToMediaBrush(),
					IsHitTestVisible = false,
					Margin = new Thickness(3),
				};
			}
			public void Dispose()
			{
				if (Chart != null)
					Chart.ShowCrossHair = m_xhair_enabled;

				Detach();
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
						if (Beg != null && End != null)
							UpdateGfx();
					}
				}
			}
			private ChartControl m_chart = null!;

			/// <summary>The grab point of the tape measure</summary>
			private Point? Beg
			{
				get => m_beg;
				set
				{
					if (m_beg == value) return;
					m_beg = value;
					if (m_beg == null)
					{
						End = null;
						Detach();
					}
					else
					{
						if (End == null)
							End = value;

						Adopt();
						UpdateGfx();
					}
				}
			}
			private Point? m_beg;

			/// <summary>The drop point of the tape measure</summary>
			private Point? End
			{
				get => m_end;
				set
				{
					if (m_end == value) return;
					m_end = value;
					if (m_end == null)
					{
						Beg = null;
						Detach();
					}
					else
					{
						if (Beg == null)
							Beg = value;

						Adopt();
						UpdateGfx();
					}
				}
			}
			private Point? m_end;

			/// <summary>The measurement area</summary>
			private Rectangle Area { get; }

			/// <summary>The delta X graphics</summary>
			private Line DeltaX { get; }

			/// <summary>The delta Y graphics</summary>
			private Line DeltaY { get; }

			/// <summary>The delta diagonal graphics</summary>
			private Line DeltaD { get; }

			/// <summary>Text description of the measured X value</summary>
			private TextBlock LabelX { get; }

			/// <summary>Text description of the measured X value</summary>
			private TextBlock LabelY { get; }

			/// <summary>Text description of the measured diagonal value</summary>
			private TextBlock LabelD { get; }

			/// <summary>Add the tape measure graphics</summary>
			private void Adopt()
			{
				Chart.Overlay.Adopt(Area);
				Chart.Overlay.Adopt(DeltaX);
				Chart.Overlay.Adopt(DeltaY);
				Chart.Overlay.Adopt(DeltaD);
				Chart.Overlay.Adopt(LabelX);
				Chart.Overlay.Adopt(LabelY);
				Chart.Overlay.Adopt(LabelD);
			}

			/// <summary>Remove the tape measure graphics</summary>
			private void Detach()
			{
				Area.Detach();
				DeltaX.Detach();
				DeltaY.Detach();
				DeltaD.Detach();
				LabelX.Detach();
				LabelY.Detach();
				LabelD.Detach();
			}

			/// <summary></summary>
			private void UpdateGfx()
			{
				if (Beg == null || End == null)
					throw new Exception("UpdateGfx should only be called when both ends are valid");

				var beg = Chart.ChartToClient(Beg.Value);
				var end = Chart.ChartToClient(End.Value);

				Area.Width = Math.Abs(end.X - beg.X);
				Area.Height = Math.Abs(end.Y - beg.Y);
				Canvas.SetLeft(Area, Math.Min(beg.X, end.X));
				Canvas.SetTop(Area, Math.Min(beg.Y, end.Y));

				DeltaX.X1 = beg.X;
				DeltaX.Y1 = (beg.Y + end.Y) / 2;
				DeltaX.X2 = end.X;
				DeltaX.Y2 = (beg.Y + end.Y) / 2;

				DeltaY.X1 = (beg.X + end.X) / 2; 
				DeltaY.Y1 = beg.Y;
				DeltaY.X2 = (beg.X + end.X) / 2;
				DeltaY.Y2 = end.Y;

				DeltaD.X1 = beg.X;
				DeltaD.Y1 = beg.Y;
				DeltaD.X2 = end.X;
				DeltaD.Y2 = end.Y;

				var text = Chart.TapeMeasureStringFormat(Beg.Value, End.Value);

				LabelX.Text = text.LabelX ?? string.Empty;
				LabelY.Text = text.LabelY ?? string.Empty;
				LabelD.Text = text.LabelD ?? string.Empty;
				LabelX.Measure(Size_.Infinity);
				LabelY.Measure(Size_.Infinity);
				LabelD.Measure(Size_.Infinity);

				Canvas.SetLeft(LabelX, end.X > beg.X ? end.X : end.X - LabelX.DesiredSize.Width);
				Canvas.SetTop(LabelX, (beg.Y + end.Y - LabelX.DesiredSize.Height) / 2);

				Canvas.SetLeft(LabelY, (beg.X + end.X - LabelY.DesiredSize.Width) / 2);
				Canvas.SetTop(LabelY, end.Y > beg.Y ? end.Y : end.Y - LabelY.DesiredSize.Height);

				Canvas.SetLeft(LabelD, end.X > beg.X ? end.X + 20 : end.X - LabelD.DesiredSize.Width);
				Canvas.SetTop(LabelD, end.Y > beg.Y ? end.Y + 20 : end.Y - LabelY.DesiredSize.Height);
			}

			/// <summary></summary>
			private class DrawTape :MouseOp
			{
				private readonly TapeMeasure m_owner;
				public DrawTape(TapeMeasure owner)
					:base(owner.Chart, allow_cancel: true)
				{
					m_owner = owner;
				}
				public override void MouseDown(MouseButtonEventArgs? e)
				{
					if (e == null) throw new Exception("This mouse op should start on mouse down");
					m_owner.Beg = GrabChart;
					base.MouseDown(e);
				}
				public override void MouseMove(MouseEventArgs e)
				{
					base.MouseMove(e);
					m_owner.End = DropChart;
				}
				public override void MouseWheel(MouseWheelEventArgs e)
				{
					base.MouseWheel(e);
					e.Handled = false;
				}
			}

			/// <summary></summary>
			public class LabelText
			{
				// Notes:
				//  - Leave properties as null to hide the associated label.

				/// <summary>Text to display for the horizontal measurement</summary>
				public string? LabelX { get; set; }

				/// <summary>Text to display for the vertical measurement</summary>
				public string? LabelY { get; set; }

				/// <summary>Text to display for the diagonal measurement</summary>
				public string? LabelD { get; set; }
			}
		}
	}
}
