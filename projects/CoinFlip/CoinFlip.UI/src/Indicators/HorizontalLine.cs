using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI.Indicators
{
	public class HorizontalLine :Indicator<HorizontalLine>
	{
		public HorizontalLine()
		{
			Price = 0.0;
			Visible = true;
			DisplayOrder = 0;
			Width = 1.0;
			LineStyle = ELineStyles.Solid;
		}
		public HorizontalLine(XElement node)
			: base(node)
		{ }

		/// <summary>The horizontal price level</summary>
		public double Price
		{
			get => get<double>(nameof(Price));
			set => set(nameof(Price), value);
		}

		/// <summary>The width of the trend line</summary>
		public double Width
		{
			get => get<double>(nameof(Width));
			set => set(nameof(Width), value);
		}

		/// <summary>The style of line</summary>
		public ELineStyles LineStyle
		{
			get => get<ELineStyles>(nameof(LineStyle));
			set => set(nameof(LineStyle), value);
		}

		/// <summary>The label to use when displaying this indicator</summary>
		public override string Label => $"Hori. Line {Name.Surround("(", ")")}";

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public override IIndicatorView CreateView(IChartView chart)
		{
			return new View(this, chart);
		}

		/// <summary>A view of the indicator</summary>
		public class View :IndicatorView
		{
			public View(HorizontalLine hl, IChartView chart)
				: base(hl.Id, nameof(HorizontalLine), chart, hl)
			{
				Line = new Line
				{
					StrokeThickness = HL.Width,
					StrokeDashArray = HL.LineStyle.ToStrokeDashArray(),
					StrokeStartLineCap = PenLineCap.Flat,
					StrokeEndLineCap = PenLineCap.Flat,
					IsHitTestVisible = false,
				};
				Glow = new Line
				{
					StrokeThickness = HL.Width + GlowRadius,
					StrokeStartLineCap = PenLineCap.Flat,
					StrokeEndLineCap = PenLineCap.Flat,
					IsHitTestVisible = false,
				};
				Price = new TextBlock
				{
					Background = HL.Colour.ToMediaBrush(),
					Foreground = HL.Colour.InvertBW().ToMediaBrush(),
					IsHitTestVisible = false,
				};
				Price.Typeface(Chart.YAxisPanel.Typeface, Chart.YAxisPanel.FontSize);
			}
			public override void Dispose()
			{
				Price.Detach();
				Line.Detach();
				Glow.Detach();
				base.Dispose();
			}

			/// <summary>The indicator data source</summary>
			private HorizontalLine HL => (HorizontalLine)Indicator;

			/// <summary>The horizontal line</summary>
			private Line Line { get; }

			/// <summary>The glow around the line when hovered or selected</summary>
			private Line Glow { get; }

			/// <summary>A price label</summary>
			private TextBlock Price { get; }

			/// <summary>Update when indicator settings change</summary>
			protected override void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				base.HandleSettingChange(sender, e);
				if (e.After)
				{
					switch (e.Key)
					{
					case nameof(Colour):
						{
							Line.Stroke = HL.Colour.ToMediaBrush();
							Glow.Stroke = HL.Colour.Alpha(0.25).ToMediaBrush();
							Price.Background = HL.Colour.ToMediaBrush();
							Price.Foreground = HL.Colour.InvertBW().ToMediaBrush();
							break;
						}
					case nameof(Width):
						{
							Line.StrokeThickness = HL.Width;
							Glow.StrokeThickness = HL.Width + GlowRadius;
							break;
						}
					case nameof(LineStyle):
						{
							Line.StrokeDashArray = HL.LineStyle.ToStrokeDashArray();
							break;
						}
					}
					Invalidate();
				}
			}

			/// <summary>Update the transforms for the graphics model</summary>
			protected override void UpdateSceneCore()
			{
				base.UpdateSceneCore();
				if (Instrument.Count == 0)
				{
					Line.Detach();
					Glow.Detach();
					Price.Detach();
					return;
				}

				var pt0 = Chart.ChartToClient(new Point(Chart.XAxis.Min, HL.Price));
				var pt1 = Chart.ChartToClient(new Point(Chart.XAxis.Max, HL.Price));

				if (Visible)
				{
					Line.X1 = pt0.X;
					Line.Y1 = pt0.Y;
					Line.X2 = pt1.X;
					Line.Y2 = pt1.Y;
					Line.Stroke = HL.Colour.ToMediaBrush();
					Chart.Overlay.Adopt(Line);

					var pt = Chart.TransformToDescendant(Chart.YAxisPanel).Transform(pt1);
					Canvas.SetLeft(Price, 0);
					Canvas.SetTop(Price, pt.Y - Price.RenderSize.Height / 2);
					Price.Text = HL.Price.ToString(8);
					Chart.YAxisPanel.Adopt(Price);

					if (Hovered || Selected)
					{
						Glow.X1 = pt0.X;
						Glow.X2 = pt1.X;
						Glow.Y1 = pt0.Y;
						Glow.Y2 = pt1.Y;
						Glow.Stroke = HL.Colour.Alpha(Selected ? 0.25 : 0.15).ToMediaBrush();
						Chart.Overlay.Adopt(Glow);
					}
					else
					{
						Glow.Detach();
					}
				}
				else
				{
					Line.Detach();
					Glow.Detach();
					Price.Detach();
				}

			}

			/// <summary>Display the options UI</summary>
			protected override void ShowOptionsUICore()
			{
				if (m_horizontal_line_ui == null)
				{
					m_horizontal_line_ui = new HorizontalLineUI(Window.GetWindow(Chart), HL);
					m_horizontal_line_ui.Closed += delegate { m_horizontal_line_ui = null; };
					m_horizontal_line_ui.Show();
				}
				m_horizontal_line_ui.Focus();
			}
			private HorizontalLineUI m_horizontal_line_ui;

			/// <summary>Hit test the indicator</summary>
			public override ChartControl.HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Instrument.Count == 0 || !Visible)
					return null;

				// Find the nearest point to 'client_point' on the line
				var dist = Chart.ChartToClient(new Size(0, Math.Abs(chart_point.Y - HL.Price))).Height;
				if (dist < Chart.Options.MinSelectionDistance)
					return new ChartControl.HitTestResult.Hit(this, new Point(chart_point.X, HL.Price), null);

				return null;
			}

			/// <summary>Support dragging</summary>
			protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
			{
				switch (args.State)
				{
				default: throw new Exception($"Unknown drag state: {args.State}");
				case ChartControl.EDragState.Start:
					{
						m_drag_start = HL.Price;
						break;
					}
				case ChartControl.EDragState.Dragging:
					{
						HL.Price = m_drag_start + args.Delta.Y;
						break;
					}
				case ChartControl.EDragState.Commit:
					{
						break;
					}
				case ChartControl.EDragState.Cancel:
					{
						HL.Price = m_drag_start;
						break;
					}
				}
				args.Handled = true;
			}
			private double m_drag_start;
		}

		/// <summary>Behaivours for this trend line</summary>
		public enum ETrendType
		{
			Slope,
			Horizontal,
			Vertical,
		}
	}
}
