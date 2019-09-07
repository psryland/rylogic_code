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
	public class TrendLine :SettingsXml<TrendLine>, IIndicator
	{
		public TrendLine()
		{
			Id = Guid.NewGuid();
			Name = null;
			TrendType = ETrendType.Slope;
			Time0 = Misc.CryptoCurrencyEpoch.Ticks;
			Time1 = Misc.CryptoCurrencyEpoch.Ticks;
			Price0 = 0.0;
			Price1 = 0.0;
			Colour = 0xFF00C000;
			Visible = true;
			Width = 3.0;
			LineStyle = ELineStyles.Solid;
		}
		public TrendLine(XElement node)
			: base(node)
		{ }
		public void Dispose()
		{
		}
		protected override void OnSettingChange(SettingChangeEventArgs args)
		{
			if (args.After)
			{
				switch (args.Key)
				{
				case nameof(Time0):
					{
						if (TrendType == ETrendType.Vertical)
							Time1 = Time0;
						break;
					}
				case nameof(Time1):
					{
						if (TrendType == ETrendType.Vertical)
							Time0 = Time1;
						break;
					}
				case nameof(Price0):
					{
						if (TrendType == ETrendType.Horizontal)
							Price1 = Price0;
						break;
					}
				case nameof(Price1):
					{
						if (TrendType == ETrendType.Horizontal)
							Price0 = Price1;
						break;
					}
				case nameof(TrendType):
					{
						switch (TrendType)
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

		/// <summary>Instance id</summary>
		public Guid Id
		{
			get => get<Guid>(nameof(Id));
			set => set(nameof(Id), value);
		}

		/// <summary>String notes for the indicator</summary>
		public string Name
		{
			get => get<string>(nameof(Name));
			set => set(nameof(Name), value);
		}

		/// <summary>The behaviour of this trend line</summary>
		public ETrendType TrendType
		{
			get => get<ETrendType>(nameof(TrendType));
			set => set(nameof(TrendType), value);
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

		/// <summary>Colour of the indicator line</summary>
		public Colour32 Colour
		{
			get => get<Colour32>(nameof(Colour));
			set => set(nameof(Colour), value);
		}

		/// <summary>Show this indicator</summary>
		public bool Visible
		{
			get => get<bool>(nameof(Visible));
			set => set(nameof(Visible), value);
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
		public string Label => $"Trend Line {Name.Surround("(",")")}";

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public IIndicatorView CreateView(IChartView chart)
		{
			return new View(this, chart);
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
					StrokeEndLineCap = PenLineCap.Round
				};
				Glow = new Line
				{
					Stroke = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					StrokeThickness = TL.Width + GlowRadius,
					StrokeStartLineCap = PenLineCap.Round,
					StrokeEndLineCap = PenLineCap.Round
				};
				Grab0 = new Ellipse
				{
					Fill = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = TL.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius
				};
				Grab1 = new Ellipse
				{
					Fill = TL.Colour.Alpha(0.25f).ToMediaBrush(),
					Stroke = TL.Colour.ToMediaBrush(),
					Height = 2 * GrabRadius,
					Width = 2 * GrabRadius
				};
			}
			public override void Dispose()
			{
				Grab0.Detach();
				Grab1.Detach();
				Line.Detach();
				Glow.Detach();
				base.Dispose();
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
			private Point Pt0
			{
				get => new Point(Instrument.FIndexAt(new TimeFrameTime(TL.Time0, Instrument.TimeFrame)), TL.Price0);
				set
				{
					TL.Time0 = Instrument.TimeAtFIndex(value.X);
					TL.Price0 = value.Y;
				}
			}
			private Point Pt1
			{
				get => new Point(Instrument.FIndexAt(new TimeFrameTime(TL.Time1, Instrument.TimeFrame)), TL.Price1);
				set
				{
					TL.Time1 = Instrument.TimeAtFIndex(value.X);
					TL.Price1 = value.Y;
				}
			}

			/// <summary>Client space coordinates of the trend line end points</summary>
			private Point ScnPt0
			{
				get => Chart.ChartToClient(Pt0);
				set => Pt0 = Chart.ClientToChart(value);
			}
			private Point ScnPt1
			{
				get => Chart.ChartToClient(Pt1);
				set => Pt1 = Chart.ClientToChart(value);
			}

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
			protected override void UpdateSceneCore()
			{
				base.UpdateSceneCore();
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

				if (Visible)
				{
					Line.X1 = pt0.X;
					Line.Y1 = pt0.Y;
					Line.X2 = pt1.X;
					Line.Y2 = pt1.Y;
					Chart.Overlay.Adopt(Line);
				}
				else
				{
					Line.Detach();
				}

				if (Visible && (Hovered || Selected))
				{
					Glow.X1 = pt0.X;
					Glow.X2 = pt1.X;
					Glow.Y1 = pt0.Y;
					Glow.Y2 = pt1.Y;
					Chart.Overlay.Adopt(Glow);
				}
				else
				{
					Glow.Detach();
				}

				if (Visible && Selected)
				{
					Canvas.SetLeft(Grab0, pt0.X - GrabRadius);
					Canvas.SetTop (Grab0, pt0.Y - GrabRadius);
					Canvas.SetLeft(Grab1, pt1.X - GrabRadius);
					Canvas.SetTop (Grab1, pt1.Y - GrabRadius);
					Chart.Overlay.Adopt(Grab0);
					Chart.Overlay.Adopt(Grab1);
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
			private TrendLineUI m_trend_line_ui;

			/// <summary>Hit test the indicator</summary>
			public override ChartControl.HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Instrument.Count == 0 || !Visible)
					return null;

				// Find the nearest point to 'client_point' on the line
				var p0 = Drawing_.ToV2(ScnPt0);
				var p1 = Drawing_.ToV2(ScnPt1);
				var pt = Drawing_.ToV2(client_point);
				var t = Rylogic.Maths.Geometry.ClosestPoint(p0, p1, pt);
				var closest = p0 * (1f - t) + p1 * (t);

				if ((closest - pt).LengthSq < Math_.Sqr(Chart.Options.MinSelectionDistance))
					return new ChartControl.HitTestResult.Hit(this, new Point(closest.x, closest.y), null);

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
						m_drag_start0 = ScnPt0;
						m_drag_start1 = ScnPt1;
						m_move = EMove.Beg | EMove.End;
						var pos = args.HitResult.ClientPoint;
						if ((pos - m_drag_start0).LengthSquared < Math_.Sqr(SettingsData.Settings.Chart.SelectionDistance)) m_move ^= EMove.End;
						if ((pos - m_drag_start1).LengthSquared < Math_.Sqr(SettingsData.Settings.Chart.SelectionDistance)) m_move ^= EMove.Beg;
						break;
					}
				case ChartControl.EDragState.Dragging:
					{
						var ofs = Chart.ChartToClient(args.Delta);
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
				}
				args.Handled = true;
			}
			[Flags] private enum EMove { Beg = 1 << 0, End = 1 << 1 }
			private Point m_drag_start0;
			private Point m_drag_start1;
			private EMove m_move;
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
