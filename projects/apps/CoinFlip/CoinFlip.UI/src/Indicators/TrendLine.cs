using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace CoinFlip.UI.Indicators
{
	[Indicator(IsDrawing = true)]
	public class TrendLine :Indicator<TrendLine>
	{
		public TrendLine()
		{
			Type = ETrendType.Slope;
			Time0 = Misc.CryptoCurrencyEpoch.Ticks;
			Time1 = Misc.CryptoCurrencyEpoch.Ticks;
			Price0 = 0.0;
			Price1 = 0.0;
			Width = 1.0;
			LineStyle = ELineStyles.Solid;
		}
		public TrendLine(XElement node)
			: base(node)
		{ }
		protected override void OnSettingChange(SettingChangeEventArgs args)
		{
			if (args.After)
			{
				switch (args.Key)
				{
				case nameof(Time0):
					{
						if (Type == ETrendType.Vertical)
							Time1 = Time0;
						break;
					}
				case nameof(Time1):
					{
						if (Type == ETrendType.Vertical)
							Time0 = Time1;
						break;
					}
				case nameof(Price0):
					{
						if (Type == ETrendType.Horizontal)
							Price1 = Price0;
						break;
					}
				case nameof(Price1):
					{
						if (Type == ETrendType.Horizontal)
							Price0 = Price1;
						break;
					}
				case nameof(Type):
					{
						switch (Type)
						{
						default: throw new Exception("Unknown trend type");
						case ETrendType.Slope: break;
						case ETrendType.Horizontal: Price0 = Price1 = (Price0 + Price1) / 2; break;
						case ETrendType.Vertical: Time0 = Time1 = (Time0 + Time1) / 2; break;
						}
						break;
					}
				}
			}
			base.OnSettingChange(args);
		}

		/// <summary>The behaviour of this trend line</summary>
		public ETrendType Type
		{
			get => get<ETrendType>(nameof(Type));
			set => set(nameof(Type), value);
		}

		/// <summary>Start point time (in ticks)</summary>
		public long Time0
		{
			get => get<long>(nameof(Time0));
			set => set(nameof(Time0), value);
		}

		/// <summary>End point time (in ticks)</summary>
		public long Time1
		{
			get => get<long>(nameof(Time1));
			set => set(nameof(Time1), value);
		}

		/// <summary>Start point price</summary>
		public double Price0
		{
			get => get<double>(nameof(Price0));
			set => set(nameof(Price0), value);
		}

		/// <summary>End point price</summary>
		public double Price1
		{
			get => get<double>(nameof(Price1));
			set => set(nameof(Price1), value);
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
		public override string Label => $"Trend Line {Name.Surround("(",")")}";

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public override IIndicatorView CreateView(IChartView chart)
		{
			return new View(this, chart);
		}

		/// <summary>Behaivours for this trend line</summary>
		public enum ETrendType
		{
			Slope,
			Horizontal,
			Vertical,
		}

		/// <summary>A view of the indicator</summary>
		public class View :IndicatorView
		{
			public View(TrendLine tl, IChartView chart)
				:base(tl.Id, nameof(TrendLine), chart, tl)
			{
				Line = new Line
				{
					Stroke = TL.Colour.ToMediaBrush(),
					StrokeThickness = TL.Width,
					StrokeDashArray = TL.LineStyle.ToStrokeDashArray(),
					StrokeStartLineCap = PenLineCap.Round,
					StrokeEndLineCap = PenLineCap.Round,
					IsHitTestVisible = false,
				};
				Glow = new Line
				{
					Stroke = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					StrokeThickness = TL.Width + GlowRadius,
					StrokeStartLineCap = PenLineCap.Round,
					StrokeEndLineCap = PenLineCap.Round,
					IsHitTestVisible = false,
				};
				Grab0 = new Ellipse
				{
					Fill = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = TL.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius,
					IsHitTestVisible = false,
				};
				Grab1 = new Ellipse
				{
					Fill = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = TL.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius,
					IsHitTestVisible = false,
				};
			}
			protected override void Dispose(bool disposing)
			{
				Grab0.Detach();
				Grab1.Detach();
				Line.Detach();
				Glow.Detach();
				base.Dispose(disposing);
			}

			/// <summary>The indicator data source</summary>
			private TrendLine TL => (TrendLine)Indicator;

			/// <summary>The trend line</summary>
			private Line Line { get; }

			/// <summary>The glow around the trend line when hovered or selected</summary>
			private Line Glow { get; }

			/// <summary>A grab handle for adjusting the trend line</summary>
			private Ellipse Grab0 { get; }

			/// <summary>A grab handle for adjusting the trend line</summary>
			private Ellipse Grab1 { get; }

			/// <summary>Chart space coordinates of the trend line end points</summary>
			private v4 Pt0
			{
				get
				{
					var fidx = (float)Instrument.FIndexAt(new TimeFrameTime(TL.Time0, Instrument.TimeFrame));
					var price = (float)TL.Price0;
					return new v4(fidx, price, 0, 1f);
				}
				set
				{
					TL.Time0 = Instrument.TimeAtFIndex(value.x);
					TL.Price0 = value.y;
				}
			}
			private v4 Pt1
			{
				get
				{
					var fidx = (float)Instrument.FIndexAt(new TimeFrameTime(TL.Time1, Instrument.TimeFrame));
					var price = (float)TL.Price1;
					return new v4(fidx, price, 0, 1f);
				}
				set
				{
					TL.Time1 = Instrument.TimeAtFIndex(value.x);
					TL.Price1 = value.y;
				}
			}

			/// <summary>Scene space coordinates of the trend line end points</summary>
			private v2 ScnPt0
			{
				get => Chart?.ChartToScene(Pt0) ?? throw new NullReferenceException("Chart is null");
				set => Pt0 = Chart?.SceneToChart(value) ?? throw new NullReferenceException("Chart is null");
			}
			private v2 ScnPt1
			{
				get => Chart?.ChartToScene(Pt1) ?? throw new NullReferenceException("Chart is null");
				set => Pt1 = Chart?.SceneToChart(value) ?? throw new NullReferenceException("Chart is null");
			}

			/// <summary>Update when indicator settings change</summary>
			protected override void HandleSettingChange(object? sender, SettingChangeEventArgs e)
			{
				base.HandleSettingChange(sender, e);
				if (e.After)
				{
					switch (e.Key)
					{
						case nameof(Colour):
						{
							Line.Stroke = TL.Colour.ToMediaBrush();
							Glow.Stroke = TL.Colour.Alpha(0.25).ToMediaBrush();
							Grab0.Stroke = TL.Colour.ToMediaBrush();
							Grab1.Stroke = TL.Colour.ToMediaBrush();
							Grab0.Fill = TL.Colour.Alpha(0.25).ToMediaBrush();
							Grab1.Fill = TL.Colour.Alpha(0.25).ToMediaBrush();
							break;
						}
						case nameof(Width):
						{
							Line.StrokeThickness = TL.Width;
							Glow.StrokeThickness = TL.Width + GlowRadius;
							break;
						}
						case nameof(LineStyle):
						{
							Line.StrokeDashArray = TL.LineStyle.ToStrokeDashArray();
							break;
						}
					}
					Invalidate();
				}
			}

			/// <summary>Update the transforms for the graphics model</summary>
			protected override void UpdateSceneCore(View3d.Window window, View3d.Camera camera)
			{
				base.UpdateSceneCore(window, camera);
				if (Instrument.Count == 0)
				{
					Line.Detach();
					Glow.Detach();
					Grab0.Detach();
					Grab1.Detach();
					return;
				}

				var pt0 = ScnPt0;
				var pt1 = ScnPt1;
				var chart = Chart ?? throw new NullReferenceException("Chart is null");

				if (Visible)
				{
					Line.X1 = pt0.x;
					Line.Y1 = pt0.y;
					Line.X2 = pt1.x;
					Line.Y2 = pt1.y;
					chart.Overlay.Adopt(Line);
				}
				else
				{
					Line.Detach();
				}

				if (Visible && (Hovered || Selected))
				{
					Glow.X1 = pt0.x;
					Glow.X2 = pt1.x;
					Glow.Y1 = pt0.y;
					Glow.Y2 = pt1.y;
					chart.Overlay.Adopt(Glow);
				}
				else
				{
					Glow.Detach();
				}

				if (Visible && Selected)
				{
					Canvas.SetLeft(Grab0, pt0.x - GrabRadius);
					Canvas.SetTop (Grab0, pt0.y - GrabRadius);
					Canvas.SetLeft(Grab1, pt1.x - GrabRadius);
					Canvas.SetTop (Grab1, pt1.y - GrabRadius);
					chart.Overlay.Adopt(Grab0);
					chart.Overlay.Adopt(Grab1);
				}
				else
				{
					Grab0.Detach();
					Grab1.Detach();
				}
			}

			/// <summary>Display the options UI</summary>
			protected override void ShowOptionsUICore()
			{
				if (m_trend_line_ui == null)
				{
					m_trend_line_ui = new TrendLineUI(Window.GetWindow(Chart), TL);
					m_trend_line_ui.Closed += delegate { m_trend_line_ui = null; };
					m_trend_line_ui.Show();
				}
				m_trend_line_ui.Focus();
			}
			private TrendLineUI? m_trend_line_ui;

			/// <summary>Hit test the indicator</summary>
			public override ChartControl.HitTestResult.Hit? HitTest(v4 chart_point, v2 scene_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Instrument.Count == 0 || !Visible)
					return null;

				var chart = Chart ?? throw new NullReferenceException("Chart is null");

				// Find the nearest point to 'scene_point' on the line
				var p0 = ScnPt0;
				var p1 = ScnPt1;
				var pt = scene_point;
				var t = Rylogic.Maths.Geometry.ClosestPoint(p0, p1, pt);
				var closest = p0 * (1f - t) + p1 * (t);

				if ((closest - pt).LengthSq < Math_.Sqr(chart.Options.MinSelectionDistance))
				{
					var closest_cs = Chart.SceneToChart(closest);
					return new ChartControl.HitTestResult.Hit(this, closest_cs, null);
				}

				return null;
			}

			/// <summary>Support dragging</summary>
			protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
			{
				var chart = Chart ?? throw new NullReferenceException("Chart is null");
				switch (args.State)
				{
					case ChartControl.EDragState.Start:
					{
						m_drag_start0 = ScnPt0;
						m_drag_start1 = ScnPt1;
						m_move = EMove.Beg | EMove.End;
						var pos = args.HitResult.ScenePoint;
						if ((pos - m_drag_start0).LengthSq < Math_.Sqr(SettingsData.Settings.Chart.SelectionDistance)) m_move ^= EMove.End;
						if ((pos - m_drag_start1).LengthSq < Math_.Sqr(SettingsData.Settings.Chart.SelectionDistance)) m_move ^= EMove.Beg;
						break;
					}
					case ChartControl.EDragState.Dragging:
					{
						var ofs = chart.ChartToScene(args.Delta);
						if (m_move.HasFlag(EMove.Beg)) ScnPt0 = m_drag_start0 + ofs;
						if (m_move.HasFlag(EMove.End)) ScnPt1 = m_drag_start1 + ofs;
						break;
					}
					case ChartControl.EDragState.Commit:
					{
						break;
					}
					case ChartControl.EDragState.Cancel:
					{
						ScnPt0 = m_drag_start0;
						ScnPt1 = m_drag_start1;
						break;
					}
					default:
					{
						throw new Exception($"Unknown drag state: {args.State}");
					}
				}
				args.Handled = true;
			}
			[Flags] private enum EMove { Beg = 1 << 0, End = 1 << 1 }
			private v2 m_drag_start0;
			private v2 m_drag_start1;
			private EMove m_move;
		}

		/// <summary>Returns a mouse op instance for creating the indicator</summary>
		public static ChartControl.MouseOp Create(CandleChart chart) => new CreateOp(chart);
		private class CreateOp :ChartControl.MouseOp
		{
			private readonly CandleChart m_chart;
			private TrendLine? m_indy;

			public CreateOp(CandleChart chart)
				: base(chart.Chart)
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
				if (Instrument is not Instrument instrument)
					return;

				var time = instrument.TimeAtFIndex(GrabChart.x);
				m_indy = Model.Indicators.Add(instrument.Pair.Name, new TrendLine
				{
					Time0 = time,
					Time1 = time,
					Price0 = GrabChart.y,
					Price1 = GrabChart.y,
				});
			}

			/// <inheritdoc/>
			public override void MouseMove(MouseEventArgs e)
			{
				base.MouseMove(e);
				if (Instrument == null || m_indy == null)
					return;

				var scene_pt = e.GetPosition(Chart.Scene).ToV2();
				var chart_pt = Chart.SceneToChart(scene_pt);
				var delta = scene_pt - GrabScene;

				m_indy.Type =
					!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? ETrendType.Slope :
					Math.Abs(delta.x) > Math.Abs(delta.y) ? ETrendType.Horizontal : ETrendType.Vertical;

				if (m_indy.Type == ETrendType.Horizontal)
					chart_pt.y = GrabChart.y;
				if (m_indy.Type == ETrendType.Vertical)
					chart_pt.x = GrabChart.x;

				m_indy.Time1 = Instrument.TimeAtFIndex(chart_pt.x);
				m_indy.Price1 = chart_pt.y;
			}

			/// <inheritdoc/>
			public override void NotifyCancelled()
			{
				base.NotifyCancelled();
				if (Instrument is not Instrument instrument)
					return;

				if (m_indy != null)
					Model.Indicators.Remove(instrument.Pair.Name, m_indy.Id);
			}
		}
	}
}
