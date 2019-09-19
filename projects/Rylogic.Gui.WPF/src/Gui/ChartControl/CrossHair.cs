using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Helper object for drawing a horizontal and vertical line on the chart, with accompanying labels on the axes</summary>
		public class CrossHair :IDisposable
		{
			private readonly ChartControl Chart;
			public CrossHair(ChartControl chart)
			{
				Chart = chart;
				LineV = new Line
				{
					Stroke = LineColour.ToMediaBrush(),
					StrokeThickness = 1.0,
					StrokeDashArray = ELineStyles.Dashed.ToStrokeDashArray(),
					StrokeStartLineCap = PenLineCap.Flat,
					StrokeEndLineCap = PenLineCap.Flat,
					IsHitTestVisible = false,
				};
				LineH = new Line
				{
					Stroke = LineColour.ToMediaBrush(),
					StrokeThickness = 1.0,
					StrokeDashArray = ELineStyles.Dashed.ToStrokeDashArray(),
					StrokeStartLineCap = PenLineCap.Flat,
					StrokeEndLineCap = PenLineCap.Flat,
					IsHitTestVisible = false,
				};
				LabelX = new TextBlock
				{
					Padding = new Thickness(2),
					TextAlignment = TextAlignment.Center,
					Background = LineColour.ToMediaBrush(),
					Foreground = BkgdColour.ToMediaBrush(),
				};
				LabelY = new TextBlock
				{
					Padding = new Thickness(2),
					TextAlignment = TextAlignment.Center,
					Background = LineColour.ToMediaBrush(),
					Foreground = BkgdColour.ToMediaBrush(),
				};

				LabelX.Typeface(Chart.XAxisPanel.Typeface, Chart.YAxisPanel.FontSize);
				LabelY.Typeface(Chart.YAxisPanel.Typeface, Chart.YAxisPanel.FontSize);

				Chart.Overlay.Adopt(LineV);
				Chart.Overlay.Adopt(LineH);
				Chart.XAxisPanel.Adopt(LabelX);
				Chart.YAxisPanel.Adopt(LabelY);
			}
			public void Dispose()
			{
				LineV.Detach();
				LineH.Detach();
				LabelX.Detach();
				LabelY.Detach();
			}

			/// <summary>The vertical part of the cross hair</summary>
			public Line LineV { get; }

			/// <summary>The horizontal part of the cross hair</summary>
			public Line LineH { get; }

			/// <summary>A text label for the cross hair candle/time</summary>
			public TextBlock LabelX { get; }

			/// <summary>A text label for the cross hair price</summary>
			public TextBlock LabelY { get; }

			/// <summary></summary>
			private Colour32 LineColour => Chart.Scene.BackgroundColor.ToColour32().Intensity < 0.5f ? 0xFFCCCCCC : 0xFF333333;
			private Colour32 BkgdColour => LineColour.Invert();

			/// <summary></summary>
			private View3d.Camera Camera => Chart.Camera;

			/// <summary></summary>
			private OptionsData Options => Chart.Options;

			/// <summary>Set the chart position of the cross hair</summary>
			public void PositionCrossHair(Point client_pt)
			{
				var chart_pt = Chart.ClientToChart(client_pt);
				var scene_bounds = Chart.SceneBounds;

				LineV.X1 = client_pt.X;
				LineV.X2 = client_pt.X;
				LineV.Y1 = scene_bounds.Top;
				LineV.Y2 = scene_bounds.Bottom;

				LineH.X1 = scene_bounds.Left;
				LineH.X2 = scene_bounds.Right;
				LineH.Y1 = client_pt.Y;
				LineH.Y2 = client_pt.Y;

				LabelX.Text = Chart.XAxis.TickText(chart_pt.X);
				switch (Chart.XAxis.Options.Side)
				{
				default: throw new Exception("CrossHair label: Unexpected side for the X axis");
				case Dock.Top:
					Canvas.SetLeft(LabelX, Chart.TransformToDescendant(Chart.XAxisPanel).Transform(client_pt).X - LabelX.RenderSize.Width / 2);
					Canvas.SetTop(LabelY, Chart.XAxisPanel.Height - LabelX.RenderSize.Height);
					break;
				case Dock.Bottom:
					Canvas.SetLeft(LabelX, Chart.TransformToDescendant(Chart.XAxisPanel).Transform(client_pt).X - LabelX.RenderSize.Width / 2);
					Canvas.SetTop(LabelY, 0);
					break;
				}

				LabelY.Text = Chart.YAxis.TickText(chart_pt.Y);
				switch (Chart.YAxis.Options.Side)
				{
				default: throw new Exception("CrossHair label: Unexpected side for the Y axis");
				case Dock.Left:
					Canvas.SetLeft(LabelY, Chart.YAxisPanel.Width - LabelY.RenderSize.Width);
					Canvas.SetTop(LabelY, Chart.TransformToDescendant(Chart.YAxisPanel).Transform(client_pt).Y - LabelY.RenderSize.Height / 2);
					break;
				case Dock.Right:
					Canvas.SetLeft(LabelY, 0);
					Canvas.SetTop(LabelY, Chart.TransformToDescendant(Chart.YAxisPanel).Transform(client_pt).Y - LabelY.RenderSize.Height / 2);
					break;
				}
			}
		}
	}
}