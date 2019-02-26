using System.Diagnostics;
using System.Drawing;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>The calculated areas of the control</summary>
		[DebuggerDisplay("Area={Area}  ChartArea={ChartArea}")]
		public struct ChartDims
		{
			public ChartDims(ChartControl chart)
			{
				Chart = chart;
				//using (var gfx = chart.CreateGraphics())
				{
					var rect = chart.RenderArea();
					var r = SizeF.Empty;

					Area = rect.ToRectF();
					TitleSize = SizeF.Empty;
					XLabelSize = SizeF.Empty;
					YLabelSize = SizeF.Empty;
					XTickLabelSize = SizeF.Empty;
					YTickLabelSize = SizeF.Empty;

					// Add margins
					rect.X += chart.Options.Margin.Left;
					rect.Y += chart.Options.Margin.Top;
					rect.Width -= chart.Options.Margin.Left + chart.Options.Margin.Right;
					rect.Height -= chart.Options.Margin.Top + chart.Options.Margin.Bottom;

					// Add space for the title
					if (chart.Title.HasValue())
					{
						r = new Size(0, 0);//gfx.MeasureString(chart.Title, chart.Options.TitleFont);
						rect.Y += r.Height;
						rect.Height -= r.Height;
						TitleSize = r;
					}

					// Add space for the axes
					if (chart.Options.ShowAxes)
					{
						// Add space for tick marks
						if (chart.YAxis.Options.DrawTickMarks)
						{
							rect.X += chart.YAxis.Options.TickLength;
							rect.Width -= chart.YAxis.Options.TickLength;
						}
						if (chart.XAxis.Options.DrawTickMarks)
						{
							rect.Height -= chart.XAxis.Options.TickLength;
						}

						// Add space for the axis labels
						if (chart.XAxis.Label.HasValue())
						{
							r = new Size(0, 0);//gfx.MeasureString(chart.XAxis.Label, chart.XAxis.Options.LabelFont);
							rect.Height -= r.Height;
							XLabelSize = r;
						}
						if (chart.YAxis.Label.HasValue())
						{
							r = new Size(0, 0);// gfx.MeasureString(chart.YAxis.Label, chart.YAxis.Options.LabelFont);
							rect.X += r.Height; // will be rotated by 90deg
							rect.Width -= r.Height;
							YLabelSize = r;
						}

						// Add space for the tick labels
						// Note: If you're having trouble with the axis jumping around
						// check the 'TickText' callback is returning fixed length strings
						if (chart.XAxis.Options.DrawTickLabels)
						{
							// Measure the height of the tick text
							r = new Size(0, 0);//chart.XAxis.MeasureTickText(gfx);
							rect.Height -= r.Height;
							XTickLabelSize = r;
						}
						if (chart.YAxis.Options.DrawTickLabels)
						{
							// Measure the width of the tick text
							r = new Size(0, 0);//chart.YAxis.MeasureTickText(gfx);
							rect.X += r.Width;
							rect.Width -= r.Width;
							YTickLabelSize = r;
						}
					}

					if (rect.Width < 0) rect.Width = 0;
					if (rect.Height < 0) rect.Height = 0;
					ChartArea = rect.ToRectF();
				}
			}

			/// <summary>The chart that these dimensions were calculated from</summary>
			public ChartControl Chart { get; private set; }

			/// <summary>The size of the control</summary>
			public RectangleF Area { get; private set; }

			/// <summary>The area of the view3d part of the chart</summary>
			public RectangleF ChartArea { get; private set; }

			/// <summary>The measured size of the chart title</summary>
			public SizeF TitleSize { get; private set; }

			/// <summary>The measured size of the X axis label</summary>
			public SizeF XLabelSize { get; private set; }

			/// <summary>The measured size of the Y axis label</summary>
			public SizeF YLabelSize { get; private set; }

			/// <summary>The measured size of the X axis tick labels</summary>
			public SizeF XTickLabelSize { get; private set; }

			/// <summary>The measured size of the Y axis tick labels</summary>
			public SizeF YTickLabelSize { get; private set; }
		}
	}
}
