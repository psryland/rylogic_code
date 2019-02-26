using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Rylogic.Extn;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;

namespace Rylogic.Gui.WPF.ChartDetail
{
	internal class ChartFrame
	{
		private readonly ChartControl m_chart;
		private bool m_invalidated;
		private int m_xaxis_hash;
		private int m_yaxis_hash;
		private int m_xaxis_options;
		private int m_yaxis_options;
		private Bitmap m_image;

		public ChartFrame(ChartControl chart)
		{
			m_chart = chart;
			m_image = null;
		}

		/// <summary>Force a repaint of the frame</summary>
		public void Invalidate()
		{
			m_invalidated = true;
		}

		/// <summary>Draw the titles, axis labels, ticks, etc around the chart</summary>
		public void DoPaint(Graphics gfx, ChartControl.ChartDims dims)
		{
			// Drawing the frame can take a while, cache the frame image so if the chart
			// is not moved, we can just blit the cached copy to the screen
			var repaint = true;
			for (; ; )
			{
				// Control size changed?
				if (m_image == null || m_image.Size != dims.Area.Size)
				{
					m_image = new Bitmap((int)dims.Area.Size.Width, (int)dims.Area.Size.Height, gfx);
					break;
				}

				// Manually invalidated?
				if (m_invalidated)
					break;

				// Axes zoomed/scrolled?
				if (m_chart.XAxis.GetHashCode() != m_xaxis_hash)
					break;
				if (m_chart.YAxis.GetHashCode() != m_yaxis_hash)
					break;

				// Rendering options changed?
				if (m_chart.XAxis.Options.ChangeCounter != m_xaxis_options)
					break;
				if (m_chart.YAxis.Options.ChangeCounter != m_yaxis_options)
					break;

				// Cached copy is still good
				repaint = false;
				break;
			}

			// Repaint the frame if the cached version is out of date
			if (repaint)
			{
				var bmp = Graphics.FromImage(m_image);
				var opts = m_chart.Options;
				var xaxis = m_chart.XAxis;
				var yaxis = m_chart.YAxis;
				var title = m_chart.Title;
				var size = dims.Area.Size;
				var chart_area = dims.ChartArea.Shifted(-dims.Area.Left, -dims.Area.Top);

				// This is not enforced in the axis.Min/Max accessors because it's useful
				// to be able to change the min/max independently of each other, set them
				// to float max etc. It's only invalid to render a chart with a negative range
				Debug.Assert(xaxis.Span > 0, "Negative x range");
				Debug.Assert(yaxis.Span > 0, "Negative y range");

				// Clear to the background colour
				bmp.Clear(opts.BkColour);

				// Draw the chart title and labels
				if (title.HasValue())
				{
					using (var bsh = new SolidBrush(opts.TitleColour))
					{
						var r = dims.TitleSize;
						var x = (float)(size.Width - r.Width) * 0.5f;
						var y = (float)(0 + opts.Margin.Top) * 1f;
						bmp.TranslateTransform(x, y);
						bmp.MultiplyTransform(opts.TitleTransform);
						bmp.DrawString(title, opts.TitleFont, bsh, PointF.Empty);
						bmp.ResetTransform();
					}
				}
				if (xaxis.Label.HasValue() && opts.ShowAxes)
				{
					using (var bsh = new SolidBrush(xaxis.Options.LabelColour))
					{
						var r = dims.XLabelSize;
						var x = (float)(size.Width - r.Width) * 0.5f;
						var y = (float)(size.Height - opts.Margin.Bottom - r.Height) * 1f;
						bmp.TranslateTransform(x, y);
						bmp.MultiplyTransform(xaxis.Options.LabelTransform);
						bmp.DrawString(xaxis.Label, xaxis.Options.LabelFont, bsh, PointF.Empty);
						bmp.ResetTransform();
					}
				}
				if (yaxis.Label.HasValue() && opts.ShowAxes)
				{
					using (var bsh = new SolidBrush(yaxis.Options.LabelColour))
					{
						var r = dims.YLabelSize;
						var x = (float)(0 + opts.Margin.Left) * 1f;
						var y = (float)(size.Height + r.Width) * 0.5f;
						bmp.TranslateTransform(x, y);
						bmp.RotateTransform(-90.0f);
						bmp.MultiplyTransform(yaxis.Options.LabelTransform);
						bmp.DrawString(yaxis.Label, yaxis.Options.LabelFont, bsh, PointF.Empty);
						bmp.ResetTransform();
					}
				}

				// Tick marks and labels
				if (opts.ShowAxes)
				{
					if (xaxis.Options.DrawTickLabels || xaxis.Options.DrawTickMarks)
					{
						using (var pen = new System.Drawing.Pen(xaxis.Options.TickColour))
						using (var bsh = new SolidBrush(xaxis.Options.TickColour))
						{
							var lbly = (float)(chart_area.Top + chart_area.Height + xaxis.Options.TickLength + 1);
							xaxis.GridLines(out var min, out var max, out var step);
							for (var x = min; x < max; x += step)
							{
								var X = (int)(chart_area.Left + x * chart_area.Width / xaxis.Span);
								if (xaxis.Options.DrawTickLabels)
								{
									var s = xaxis.TickText(x + xaxis.Min, step);
									bmp.DrawString(s, xaxis.Options.TickFont, bsh, new PointF(X, lbly), new StringFormat { Alignment = StringAlignment.Center });
								}
								if (xaxis.Options.DrawTickMarks)
								{
									bmp.DrawLine(pen, X, chart_area.Top + chart_area.Height, X, chart_area.Top + chart_area.Height + xaxis.Options.TickLength);
								}
							}
						}
					}
					if (yaxis.Options.DrawTickLabels || yaxis.Options.DrawTickMarks)
					{
						using (var pen = new Pen(yaxis.Options.TickColour))
						using (var bsh = new SolidBrush(yaxis.Options.TickColour))
						{
							var lblx = (float)(chart_area.Left - yaxis.Options.TickLength - 1);
							yaxis.GridLines(out var min, out var max, out var step);
							for (var y = min; y < max; y += step)
							{
								var Y = (int)(chart_area.Top + chart_area.Height - y * chart_area.Height / yaxis.Span);
								if (yaxis.Options.DrawTickLabels)
								{
									var s = yaxis.TickText(y + yaxis.Min, step);
									bmp.DrawString(s, yaxis.Options.TickFont, bsh,
										new RectangleF(lblx - dims.YTickLabelSize.Width, Y - dims.YTickLabelSize.Height * 0.5f, dims.YTickLabelSize.Width, dims.YTickLabelSize.Height),
										new StringFormat { Alignment = StringAlignment.Far });
								}
								if (yaxis.Options.DrawTickMarks)
								{
									bmp.DrawLine(pen, chart_area.Left - yaxis.Options.TickLength, Y, chart_area.Left, Y);
								}
							}
						}
					}

					// Axes
					using (var pen = new Pen(xaxis.Options.AxisColour, xaxis.Options.AxisThickness))
					{
						var y = chart_area.Bottom;
						bmp.DrawLine(pen, new PointF(chart_area.Left, y), new PointF(chart_area.Right, y));
					}
					using (var pen = new Pen(yaxis.Options.AxisColour, yaxis.Options.AxisThickness))
					{
						var x = (int)(chart_area.Left - xaxis.Options.AxisThickness * 0.5f);
						bmp.DrawLine(pen, new PointF(x, chart_area.Top), new PointF(x, chart_area.Bottom));
					}

					// Record cache invalidating values
					m_xaxis_hash = xaxis.GetHashCode();
					m_yaxis_hash = yaxis.GetHashCode();
					m_xaxis_options = xaxis.Options.ChangeCounter;
					m_yaxis_options = yaxis.Options.ChangeCounter;
					m_invalidated = false;
				}
			}

			// Blit the cached image to the screen
			gfx.DrawImageUnscaled(m_image, dims.Area.TopLeft().ToPoint());
		}
	}
}
