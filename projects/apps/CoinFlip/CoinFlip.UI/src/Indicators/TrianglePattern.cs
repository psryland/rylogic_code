using System;
using System.Linq;
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
using Drawing_ = Rylogic.Extn.Windows.Drawing_;

namespace CoinFlip.UI.Indicators
{
	[Indicator(IsDrawing = true)]
	public class TrianglePattern :Indicator<TrianglePattern>
	{
		// Notes:
		//  - Point labelling:
		//     2 +-.._
		//       |   _ > 1
		//     0 +-''

		public TrianglePattern()
		{
			Type = ETrianglePatternType.Symmetric;
			Time0 = Misc.CryptoCurrencyEpoch.Ticks;
			Time1 = Misc.CryptoCurrencyEpoch.Ticks;
			Price0 = 0.0;
			Price1 = 0.0;
			Price2 = 0.0;
			Width = 1.0;
			LineStyle = ELineStyles.Solid;
		}
		public TrianglePattern(XElement node)
			: base(node)
		{ }
		protected override void OnSettingChange(SettingChangeEventArgs args)
		{
			if (args.After)
			{
				switch (args.Key)
				{
				case nameof(Type):
					{
						switch (Type)
						{
						default: throw new Exception($"Unknown trend type: {Type}");
						case ETrianglePatternType.Asymmetric: break;
						case ETrianglePatternType.Symmetric: Price1 = (Price0 + Price2) / 2; break;
						case ETrianglePatternType.Ascending: Price1 = Price2; break;
						case ETrianglePatternType.Descending: Price1 = Price0; break;
						}
						break;
					}
				case nameof(Price0):
					{
						if (Type == ETrianglePatternType.Symmetric) Price2 = Price0 + 2 * (Price1 - Price0);
						if (Type == ETrianglePatternType.Descending) Price1 = Price0;
						break;
					}
				case nameof(Price1):
					{
						if (Type == ETrianglePatternType.Symmetric)
						{
							var delta = (Price2 - Price0) / 2;
							Price0 = Price1 - delta;
							Price2 = Price1 + delta;
						}
						if (Type == ETrianglePatternType.Ascending) Price2 = Price1;
						if (Type == ETrianglePatternType.Descending) Price0 = Price1;
						break;
					}
				case nameof(Price2):
					{
						if (Type == ETrianglePatternType.Symmetric) Price0 = Price2 + 2 * (Price1 - Price2);
						if (Type == ETrianglePatternType.Ascending) Price1 = Price2;
						break;
					}
				}
			}

			base.OnSettingChange(args);
		}

		/// <summary>The type of triangle pattern</summary>
		public ETrianglePatternType Type
		{
			get => get<ETrianglePatternType>(nameof(Type));
			set => set(nameof(Type), value);
		}

		/// <summary>The start point time (in ticks) of the triangle (wide end)</summary>
		public long Time0
		{
			get => get<long>(nameof(Time0));
			set => set(nameof(Time0), value);
		}

		/// <summary>The end point time (in ticks) of the triangle (narrow end)</summary>
		public long Time1
		{
			get => get<long>(nameof(Time1));
			set => set(nameof(Time1), value);
		}

		/// <summary>The lower bound price at the wide end</summary>
		public double Price0
		{
			get => get<double>(nameof(Price0));
			set => set(nameof(Price0), value);
		}

		/// <summary>The price at the narrow end</summary>
		public double Price1
		{
			get => get<double>(nameof(Price1));
			set => set(nameof(Price1), value);
		}

		/// <summary>The upper bound price at the wide end</summary>
		public double Price2
		{
			get => get<double>(nameof(Price2));
			set => set(nameof(Price2), value);
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
		public override string Label => $"Triangle {Name.Surround("(", ")")}";

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public override IIndicatorView CreateView(IChartView chart)
		{
			return new View(this, chart);
		}

		/// <summary>Triangle pattern types</summary>
		public enum ETrianglePatternType
		{
			/// <summary>The slope of the top and bottom edges are not related</summary>
			Asymmetric,

			/// <summary>The slope of the top and bottom edges have antisymmetric signs</summary>
			Symmetric,

			/// <summary>Top edge is horizontal</summary>
			Ascending,

			/// <summary>Bottom edge is horizontal</summary>
			Descending,
		}

		/// <summary>A view of the indicator</summary>
		public class View :IndicatorView
		{
			public View(TrianglePattern tri, IChartView chart)
				:base(tri.Id, nameof(TrianglePattern), chart, tri)
			{
				var geom = Geometry_.MakePolygon(true, Pt0.xy.ToPointD(), Pt1.xy.ToPointD(), Pt2.xy.ToPointD());
				Line = new Path
				{
					Data = geom,
					Fill = Brushes.Transparent,
					Stroke = Tri.Colour.ToMediaBrush(),
					StrokeThickness = Tri.Width,
					StrokeDashArray = Tri.LineStyle.ToStrokeDashArray(),
					StrokeLineJoin = PenLineJoin.Round,
					IsHitTestVisible = false,
				};
				Glow = new Path
				{
					Data = geom,
					Fill = Brushes.Transparent,
					Stroke = Tri.Colour.Alpha(0.25f).ToMediaBrush(),
					StrokeThickness = Tri.Width + GlowRadius,
					StrokeLineJoin = PenLineJoin.Round,
					IsHitTestVisible = false,
				};
				Grab0 = new Ellipse
				{
					Fill = Tri.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = Tri.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius,
					IsHitTestVisible = false,
				};
				Grab1 = new Ellipse
				{
					Fill = Tri.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = Tri.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius,
					IsHitTestVisible = false,
				};
				Grab2 = new Ellipse
				{
					Fill = Tri.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = Tri.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius,
					IsHitTestVisible = false,
				};
			}
			protected override void Dispose(bool disposing)
			{
				Line.Detach();
				Glow.Detach();
				Grab0.Detach();
				Grab1.Detach();
				Grab2.Detach();
				base.Dispose(disposing);
			}

			/// <summary>The indicator data source</summary>
			private TrianglePattern Tri => (TrianglePattern)Indicator;

			/// <summary>The trend line</summary>
			private Path Line { get; }

			/// <summary>The glow around the trend line when hovered or selected</summary>
			private Path Glow { get; }

			/// <summary>A grab handle for adjusting the trend line</summary>
			private Ellipse Grab0 { get; }

			/// <summary>A grab handle for adjusting the trend line</summary>
			private Ellipse Grab1 { get; }

			/// <summary>A grab handle for adjusting the trend line</summary>
			private Ellipse Grab2 { get; }

			/// <summary>Chart space coordinates of the trend line end points</summary>
			private v4 Pt0
			{
				get
				{
					var tft = new TimeFrameTime(Tri.Time0, Instrument.TimeFrame);
					var x = Instrument.Count != 0 ? Instrument.FIndexAt(tft) : 0;
					return new v4((float)x, (float)Tri.Price0, 0, 1f);
				}
				set
				{
					Tri.Time0 = Instrument.TimeAtFIndex(value.x);
					Tri.Price0 = value.y;
				}
			}
			private v4 Pt1
			{
				get
				{
					var tft = new TimeFrameTime(Tri.Time1, Instrument.TimeFrame);
					var x = Instrument.Count != 0 ? Instrument.FIndexAt(tft) : 0;
					return new v4((float)x, (float)Tri.Price1, 0, 1f);
				}
				set
				{
					Tri.Time1 = Instrument.TimeAtFIndex(value.x);
					Tri.Price1 = value.y;
				}
			}
			private v4 Pt2
			{
				get
				{
					var tft = new TimeFrameTime(Tri.Time0, Instrument.TimeFrame);
					var x = Instrument.Count != 0 ? Instrument.FIndexAt(tft) : 0;
					return new v4((float)x, (float)Tri.Price2, 0, 1f);
				}
				set
				{
					Tri.Time0 = Instrument.TimeAtFIndex(value.x);
					Tri.Price2 = value.y;
				}
			}

			/// <summary>Client space coordinates of the trend line end points</summary>
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
			private v2 ScnPt2
			{
				get => Chart?.ChartToScene(Pt2) ?? throw new NullReferenceException("Chart is null");
				set => Pt2 = Chart?.SceneToChart(value) ?? throw new NullReferenceException("Chart is null");
			}

			/// <summary>Update when indicator settings change</summary>
			protected override void HandleSettingChange(object? sender, SettingChangeEventArgs e)
			{
				base.HandleSettingChange(sender, e);
				if (e.After)
				{
					switch (e.Key)
					{
						case nameof(Time0):
						case nameof(Time1):
						case nameof(Price2):
						case nameof(Price0):
						case nameof(Price1):
						{
							var geom = Geometry_.MakePolygon(true, Pt0.xy.ToPointD(), Pt1.xy.ToPointD(), Pt2.xy.ToPointD());
							Line.Data = geom;
							Glow.Data = geom;
							break;
						}
						case nameof(Colour):
						{
							Line.Stroke = Tri.Colour.ToMediaBrush();
							Glow.Stroke = Tri.Colour.Alpha(0.25).ToMediaBrush();
							Grab0.Stroke = Tri.Colour.ToMediaBrush();
							Grab1.Stroke = Tri.Colour.ToMediaBrush();
							Grab2.Stroke = Tri.Colour.ToMediaBrush();
							Grab0.Fill = Tri.Colour.Alpha(0.25).ToMediaBrush();
							Grab1.Fill = Tri.Colour.Alpha(0.25).ToMediaBrush();
							Grab2.Fill = Tri.Colour.Alpha(0.25).ToMediaBrush();
							break;
						}
						case nameof(Width):
						{
							Line.StrokeThickness = Tri.Width;
							Glow.StrokeThickness = Tri.Width + GlowRadius;
							break;
						}
						case nameof(LineStyle):
						{
							Line.StrokeDashArray = Tri.LineStyle.ToStrokeDashArray();
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
				var chart = Chart ?? throw new NullReferenceException("Chart is null");

				// The line graphics are in chart space, get the transform to client space
				var c2c = chart.ChartToSceneSpace();
				var c2c_2d = new MatrixTransform(c2c.x.x, 0, 0, c2c.y.y, c2c.w.x, c2c.w.y);

				if (Visible)
				{
					Line.Data.Transform = c2c_2d;
					Chart.Overlay.Adopt(Line);
				}
				else
				{
					Line.Detach();
				}

				if (Visible && (Hovered || Selected))
				{
					Glow.Stroke = Tri.Colour.Alpha(Selected ? 0.25 : 0.15).ToMediaBrush();
					Glow.Data.Transform = c2c_2d;
					Chart.Overlay.Adopt(Glow);
				}
				else
				{
					Glow.Detach();
				}

				if (Visible && Selected)
				{
					var pt0Hi = ScnPt2;
					var pt0Lo = ScnPt0;
					var pt1 = ScnPt1;

					Canvas.SetLeft(Grab0, pt0Hi.x - GrabRadius);
					Canvas.SetTop(Grab0, pt0Hi.y - GrabRadius);
					Canvas.SetLeft(Grab1, pt0Lo.x - GrabRadius);
					Canvas.SetTop(Grab1, pt0Lo.y - GrabRadius);
					Canvas.SetLeft(Grab2, pt1.x - GrabRadius);
					Canvas.SetTop(Grab2, pt1.y - GrabRadius);
					Chart.Overlay.Adopt(Grab0);
					Chart.Overlay.Adopt(Grab1);
					Chart.Overlay.Adopt(Grab2);
				}
				else
				{
					Grab0.Detach();
					Grab1.Detach();
					Grab2.Detach();
				}
			}

			/// <inheritdoc/>
			protected override void ShowOptionsUICore()
			{
				if (m_ui == null)
				{
					m_ui = new TrianglePatternUI(Window.GetWindow(Chart), Tri);
					m_ui.Closed += delegate { m_ui = null; };
					m_ui.Show();
				}
				m_ui.Focus();
			}
			private TrianglePatternUI? m_ui;

			/// <summary>Hit test the indicator</summary>
			public override ChartControl.HitTestResult.Hit? HitTest(v4 chart_point, v2 scene_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Instrument.Count == 0 || !Visible)
					return null;

				var scn_p0 = ScnPt0;
				var scn_p1 = ScnPt1;
				var scn_p2 = ScnPt2;
				var chart = Chart ?? throw new NullReferenceException("Chart is null");

				// Test the grab points
				if ((scn_p0 - scene_point).LengthSq < chart.Options.MinSelectionDistanceSq)
					return new ChartControl.HitTestResult.Hit(this, Pt0, EMove.Pt0);
				if ((scn_p1 - scene_point).LengthSq < chart.Options.MinSelectionDistanceSq)
					return new ChartControl.HitTestResult.Hit(this, Pt1, EMove.Pt1);
				if ((scn_p2 - scene_point).LengthSq < chart.Options.MinSelectionDistanceSq)
					return new ChartControl.HitTestResult.Hit(this, Pt2, EMove.Pt2);

				// Test the three edges if the triangle
				if (HitEdge(scn_p0, scn_p1) is v2 top_edge)
					return new ChartControl.HitTestResult.Hit(this, new v4(top_edge, 0, 1), EMove.Pt0 | EMove.Pt1);
				if (HitEdge(scn_p1, scn_p2) is v2 bottom_edge)
					return new ChartControl.HitTestResult.Hit(this, new v4(bottom_edge, 0, 1), EMove.Pt2 |EMove.Pt1);
				if (HitEdge(scn_p2, scn_p0) is v2 side_edge)
					return new ChartControl.HitTestResult.Hit(this, new v4(side_edge, 0, 1), EMove.Pt0 | EMove.Pt2);

				return null;

				// Find the nearest point to 'scene_point' on the line
				v2? HitEdge(v2 pt0, v2 pt1)
				{
					var t = Rylogic.Maths.Geometry.ClosestPoint(pt0, pt1, scene_point);
					var closest = pt0 * (1f - t) + pt1 * (t);
					if ((closest - scene_point).LengthSq < Chart.Options.MinSelectionDistanceSq)
						return closest;

					return null;
				}
			}

			/// <summary>Support dragging</summary>
			protected override void HandleDragged(ChartControl.ChartDraggedEventArgs args)
			{
				var chart = Chart ?? throw new NullReferenceException("Chart is null");
				switch (args.State)
				{
					case ChartControl.EDragState.Start:
					{
						var hit = args.HitResult.Hits.FirstOrDefault(x => x.Element is View);
						m_drag_start0 = ScnPt0;
						m_drag_start1 = ScnPt1;
						m_drag_start2 = ScnPt2;
						m_move = (EMove?)hit?.Context ?? EMove.None;
						break;
					}
					case ChartControl.EDragState.Dragging:
					{
						var delta = chart.ChartToScene(args.Delta);
						if (m_move == EMove.Pt0) ScnPt0 = m_drag_start0 + delta;
						else if (m_move == EMove.Pt1) ScnPt1 = m_drag_start1 + delta;
						else if (m_move == EMove.Pt2) ScnPt2 = m_drag_start2 + delta;
						else if (m_move != EMove.None)
						{
							ScnPt0 = m_drag_start0 + delta;
							ScnPt1 = m_drag_start1 + delta;
							ScnPt2 = m_drag_start2 + delta;
						}
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
						ScnPt2 = m_drag_start2;
						break;
					}
					default:
					{
						throw new Exception($"Unknown drag state: {args.State}");
					}
				}
				args.Handled = true;
			}
			[Flags] private enum EMove
			{
				None = 0,
				Pt0 = 1 << 0,
				Pt1 = 1 << 1,
				Pt2 = 1 << 2,
			}
			private v2 m_drag_start0;
			private v2 m_drag_start1;
			private v2 m_drag_start2;
			private EMove m_move;
		}

		/// <summary>Returns a mouse op instance for creating the indicator</summary>
		public static ChartControl.MouseOp Create(CandleChart chart) => new CreateOp(chart);
		private class CreateOp :ChartControl.MouseOp
		{
			private readonly CandleChart m_chart;
			private TrianglePattern? m_indy;

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
				if (Instrument == null)
					return;

				var time = Instrument.TimeAtFIndex(GrabChart.x);
				m_indy = Model.Indicators.Add(Instrument.Pair.Name, new TrianglePattern
				{
					Type = ETrianglePatternType.Symmetric,
					Time0 = time,
					Time1 = time,
					Price0 = GrabChart.y,
					Price1 = GrabChart.y,
					Price2 = GrabChart.y,
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
				var delta = chart_pt - GrabChart;

				m_indy.Type =
					Keyboard.Modifiers == ModifierKeys.None ? ETrianglePatternType.Symmetric :
					Keyboard.Modifiers == ModifierKeys.Control ? ETrianglePatternType.Asymmetric :
					delta.y > 0 ? ETrianglePatternType.Ascending : ETrianglePatternType.Descending;

				m_indy.Time1 = Instrument.TimeAtFIndex(chart_pt.x);
				m_indy.Price1 = chart_pt.y;
				if (m_indy.Type == ETrianglePatternType.Symmetric)
				{
					if (delta.y > 0)
						m_indy.Price0 = GrabChart.y - Math.Abs(delta.y);
					else
						m_indy.Price2 = GrabChart.y + Math.Abs(delta.y);
				}
				if (m_indy.Type == ETrianglePatternType.Asymmetric)
				{
					m_indy.Price0 = GrabChart.y - Math.Abs(delta.y);
					m_indy.Price2 = GrabChart.y + Math.Abs(delta.y);
				}
			}
			public override void NotifyCancelled()
			{
				base.NotifyCancelled();
				if (Instrument != null && m_indy != null)
					Model.Indicators.Remove(Instrument.Pair.Name, m_indy.Id);
			}
		}
	}
}
