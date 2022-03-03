using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace CoinFlip.UI.Indicators
{
	[Indicator(IsDrawing = true)]
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
				var chart_ctrl = Chart ?? throw new ArgumentNullException(nameof(Chart));
				Price.Typeface(chart_ctrl.YAxisPanel.Typeface, chart_ctrl.YAxisPanel.FontSize);
			}
			protected override void Dispose(bool disposing)
			{
				Price.Detach();
				Line.Detach();
				Glow.Detach();
				base.Dispose(disposing);
			}

			/// <summary>The indicator data source</summary>
			private HorizontalLine HL => (HorizontalLine)Indicator;

			/// <summary>The horizontal line</summary>
			private Line Line { get; }

			/// <summary>The glow around the line when hovered or selected</summary>
			private Line Glow { get; }

			/// <summary>A price label</summary>
			private TextBlock Price { get; }

			/// <inheritdoc/>
			protected override void HandleSettingChange(object? sender, SettingChangeEventArgs e)
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

			/// <inheritdoc/>
			protected override void UpdateSceneCore(View3d.Window window, View3d.Camera camera)
			{
				base.UpdateSceneCore(window, camera);
				if (Instrument.Count == 0)
				{
					Line.Detach();
					Glow.Detach();
					Price.Detach();
					return;
				}
				if (Chart == null)
					return;

				var pt0 = Chart.ChartToScene(new v4((float)Chart.XAxis.Min, (float)HL.Price, 0, 1f));
				var pt1 = Chart.ChartToScene(new v4((float)Chart.XAxis.Max, (float)HL.Price, 0, 1f));

				if (Visible)
				{
					Line.X1 = pt0.x;
					Line.Y1 = pt0.y;
					Line.X2 = pt1.x;
					Line.Y2 = pt1.y;
					Line.Stroke = HL.Colour.ToMediaBrush();
					Chart.Overlay.Adopt(Line);

					var pt = Chart.TransformToDescendant(Chart.YAxisPanel).Transform(pt1.ToPointD());
					Canvas.SetLeft(Price, 0);
					Canvas.SetTop(Price, pt.Y - Price.RenderSize.Height / 2);
					Price.Text = HL.Price.ToString(8);
					Chart.YAxisPanel.Adopt(Price);

					if (Hovered || Selected)
					{
						Glow.X1 = pt0.x;
						Glow.X2 = pt1.x;
						Glow.Y1 = pt0.y;
						Glow.Y2 = pt1.y;
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

			/// <inheritdoc/>
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
			private HorizontalLineUI? m_horizontal_line_ui;

			/// <inheritdoc/>
			public override ChartControl.HitTestResult.Hit? HitTest(v4 chart_point, v2 scene_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Instrument.Count == 0 || !Visible)
					return null;

				//Todo: Needs updating to 3D
				//// Find the nearest point to 'client_point' on the line
				//var dist = Chart.ChartToClient(new Size(0, Math.Abs(chart_point.Y - HL.Price))).Height;
				//if (dist < Chart.Options.MinSelectionDistance)
				//	return new ChartControl.HitTestResult.Hit(this, new Point(chart_point.X, HL.Price), null);

				return null;
			}

			/// <summary>Support dragging</summary>
			protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
			{
				switch (args.State)
				{
					case ChartControl.EDragState.Start:
					{
						m_drag_start = HL.Price;
						break;
					}
					case ChartControl.EDragState.Dragging:
					{
						HL.Price = m_drag_start + args.Delta.y;
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
					default:
					{
						throw new Exception($"Unknown drag state: {args.State}");
					}
				}
				args.Handled = true;
			}
			private double m_drag_start;
		}

		/// <summary>Returns a mouse op instance for creating the indicator</summary>
		public static ChartControl.MouseOp Create(CandleChart chart) => new CreateOp(chart);
		private class CreateOp :ChartControl.MouseOp
		{
			private readonly CandleChart m_chart;
			public CreateOp(CandleChart chart)
				:base(chart.Chart)
			{
				m_chart = chart;
			}
			
			/// <summary></summary>
			private Model Model => m_chart.Model;
			
			/// <summary></summary>
			private Instrument? Instrument => m_chart.Instrument;

			/// <inheritdoc/>
			public override void MouseDown(MouseButtonEventArgs? e)
			{
				base.MouseDown(e);
				if (Instrument is Instrument instrument)
					Model.Indicators.Add(instrument.Pair.Name, new HorizontalLine { Price = GrabChart.y });
			}
		}
	}
}
