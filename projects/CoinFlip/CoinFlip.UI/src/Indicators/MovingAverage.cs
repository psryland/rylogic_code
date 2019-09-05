using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.Indicators
{
	public class MovingAverage :SettingsXml<MovingAverage>, IIndicator
	{
		public MovingAverage()
		{
			Id = Guid.NewGuid();
			Name = null;
			Exponential = false;
			Periods = 50;
			Colour = Colour32.Blue;
			Width = 1.0;
			LineStyle = ELineStyles.Solid;
			ShowBollingerBands = false;
			BBStdDev = 2.0;
			BBColour = Colour32.LightBlue;
			BBWidth = 1.0;
			BBLineStyle = ELineStyles.Solid;
			XOffset = 0.0;
		}
		public MovingAverage(XElement node)
			:base(node)
		{
		}
		public void Dispose()
		{}

		/// <summary>Instance id</summary>
		public Guid Id
		{
			get => get<Guid>(nameof(Id));
			set => set(nameof(Id), value);
		}

		/// <summary>String identifier for the indicator</summary>
		public string Name
		{
			get => get<string>(nameof(Name));
			set => set(nameof(Name), value);
		}

		/// <summary>True if this is an exponential moving average</summary>
		public bool Exponential
		{
			get => get<bool>(nameof(Exponential));
			set => set(nameof(Exponential), value);
		}

		/// <summary>The window size of the EMA</summary>
		public int Periods
		{
			get => get<int>(nameof(Periods));
			set => set(nameof(Periods), value);
		}

		/// <summary>Colour of the indicator line</summary>
		public Colour32 Colour
		{
			get => get<Colour32>(nameof(Colour));
			set => set(nameof(Colour), value);
		}

		/// <summary>The width of the line</summary>
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

		/// <summary>Show Bollinger Bands around the MA</summary>
		public bool ShowBollingerBands
		{
			get { return get<bool>(nameof(ShowBollingerBands)); }
			set { set(nameof(ShowBollingerBands), value); }
		}

		/// <summary>The Bollinger band size in units of the standard deviations</summary>
		public double BBStdDev
		{
			get { return get<double>(nameof(BBStdDev)); }
			set { set(nameof(BBStdDev), Math_.Clamp(value, 0, 5.0f)); }
		}

		/// <summary>The line colour for the Bollinger Bands</summary>
		public Colour32 BBColour
		{
			get { return get<Colour32>(nameof(BBColour)); }
			set { set(nameof(BBColour), value); }
		}

		/// <summary>The width of the Bollinger bands</summary>
		public double BBWidth
		{
			get => get<double>(nameof(BBWidth));
			set => set(nameof(BBWidth), value);
		}

		/// <summary>The style of line</summary>
		public ELineStyles BBLineStyle
		{
			get => get<ELineStyles>(nameof(BBLineStyle));
			set => set(nameof(BBLineStyle), value);
		}

		/// <summary>Shift the indicator in the X axis direction</summary>
		public double XOffset
		{
			get { return get<double>(nameof(XOffset)); }
			set { set(nameof(XOffset), value); }
		}

		/// <summary>The label to use when displaying this indicator</summary>
		public string Label => $"{(Exponential?"E":"")}MA-{Periods} {Name.Surround("(", ")")}";

		/// <summary>Create a view of this indicator for displaying on a chart</summary>
		public IIndicatorView CreateView(IChartView chart)
		{
			return new View(this, chart);
		}

		/// <summary>A MA data set based on an instrument</summary>
		public class MAContext :IDisposable
		{
			// Notes:
			//  - Calculate the MA for the full range (or perhaps limit to 1,000,000).
			//    It's not worth the memory/time/effort limiting the MA data to a range.

			private readonly List<MAPoint> m_data;
			private IStatMeanAndVarianceSingleVariable<double> m_stat;

			public MAContext(MovingAverage ma, Instrument instrument)
			{
				m_data = new List<MAPoint>();
				MA = ma;
				Instrument = instrument;

				Reset();
				CalculateMA();
			}
			public void Dispose()
			{
				Instrument = null;
				MA = null;
			}

			/// <summary>The moving average this context is based on</summary>
			private MovingAverage MA
			{
				get => m_ma;
				set
				{
					if (m_ma == value) return;
					if (m_ma != null)
					{
						m_ma.SettingChange -= HandleSettingChange;
					}
					m_ma = value;
					if (m_ma != null)
					{
						m_ma.SettingChange += HandleSettingChange;
					}

					// Handler
					void HandleSettingChange(object sender, SettingChangeEventArgs e)
					{
						if (e.Before) return;
						switch (e.Key)
						{
						case nameof(MovingAverage.Exponential):
							{
								Reset();
								CalculateMA();
								break;
							}
						case nameof(MovingAverage.Periods):
							{
								Reset();
								CalculateMA();
								break;
							}
						}
					}
				}
			}
			private MovingAverage m_ma;

			/// <summary>Reset the data and recalculate</summary>
			public void Reset()
			{
				m_data.Clear();
				m_stat = MA.Exponential
					? (IStatMeanAndVarianceSingleVariable<double>)new ExponentialMovingAverage(MA.Periods)
					: (IStatMeanAndVarianceSingleVariable<double>)new SimpleMovingAverage(MA.Periods);
			}

			/// <summary>The number of points in the MA</summary>
			public int Count => m_data.Count;

			/// <summary>Basic list access</summary>
			public MAPoint this[int i] => m_data[i];

			/// <summary>Returns the nearest index position of 'candle_index' within the data</summary>
			public int IndexOf(double candle_index) => m_data.BinarySearch(pt => pt.CandleIndex.CompareTo(candle_index), find_insert_position: true);

			/// <summary>The candle range spanned by this context</summary>
			public RangeF CandleRange => Count != 0 ? new RangeF(m_data.Front().CandleIndex, m_data.Back().CandleIndex + 1) : Range.Invalid;

			/// <summary>The instrument to calculate the moving average over</summary>
			public Instrument Instrument
			{
				get => m_instrument;
				set
				{
					if (m_instrument == value) return;
					if (m_instrument != null)
					{
						m_instrument.DataChanged -= HandleDataChanged;
					}
					m_instrument = value;
					if (m_instrument != null)
					{
						m_instrument.DataChanged += HandleDataChanged;
					}

					// Handler
					void HandleDataChanged(object sender, DataEventArgs e)
					{
						CalculateMA();
					}
				}
			}
			private Instrument m_instrument;

			/// <summary>Update the MA to match the current state of 'Instrument'</summary>
			public void CalculateMA()
			{
				// 'm_stat' does not include the latest candle as it changes all the time.

				// No instrument data means no MA
				if (Instrument.Count == 0)
				{
					Reset();
					return;
				}

				// The instrument count can go backwards when back testing
				if (Instrument.Count < m_data.Count)
				{
					Reset();
				}

				// Trim the MA data to exclude the latest candle
				m_data.Resize(Math.Max(0, m_data.Count - 1));

				// Get the range of new MA data
				var range = new Range(m_data.Count, Instrument.Count);

				// Append MA data upto, but excluding, the latest candle
				m_data.AddRange(CalculateMA(m_data.Count, Instrument.Count - 1, m_stat));

				// Append the latest candle
				m_data.AddRange(CalculateMA(m_data.Count, Instrument.Count, CopyStat(m_stat)));

				// Notify
				DataChanged?.Invoke(this, new DataChangedEventArgs(range));

				// Return the moving average points over the given range
				IEnumerable<MAPoint> CalculateMA(int beg, int end, IStatMeanAndVarianceSingleVariable<double> stat)
				{
					Debug.Assert(beg <= end);
					var ts = Misc.CryptoCurrencyEpoch.Ticks;
					for (var i = beg; i != end; ++i)
					{
						var candle = Instrument[i];
						Debug.Assert(candle.Timestamp >= ts, "Candle data must be strictly ordered by timestamp because of binary searches");
						ts = candle.Timestamp;

						// Calculate the MA value and save it
						stat.Add(candle.Close);
						yield return new MAPoint(i, ts, stat.Mean, stat.PopStdDev);
					}
				}
				IStatMeanAndVarianceSingleVariable<double> CopyStat(IStatMeanAndVarianceSingleVariable<double> rhs)
				{
					return 
						rhs is ExponentialMovingAverage ema ? (IStatMeanAndVarianceSingleVariable<double>)new ExponentialMovingAverage(ema) :
						rhs is SimpleMovingAverage sma ? (IStatMeanAndVarianceSingleVariable<double>)new SimpleMovingAverage(sma) :
						throw new Exception("Unknown moving average type");
				}
			}

			/// <summary>Raised when data is changed</summary>
			public event EventHandler<DataChangedEventArgs> DataChanged;

			/// <summary>A single point in the MA curve, representing a single candle</summary>
			[DebuggerDisplay("{Description,nq}")]
			public class MAPoint
			{
				public MAPoint(int candle_index, long ts, double value, double std_dev)
				{
					CandleIndex = candle_index;
					Value = value;
					StdDev = std_dev;
					Timestamp = ts;
				}

				/// <summary>The index in the instrument that this point corresponds to</summary>
				public double CandleIndex { get; }

				/// <summary>The value of the moving average at 'Timestamp'</summary>
				public double Value { get; }

				/// <summary>The standard deviation of the moving average at 'Timestamp'</summary>
				public double StdDev { get; }

				/// <summary>The candle timestamp that this MA point corresponds to</summary>
				public long Timestamp { get; }

				/// <summary>Guess at whether double or long values are used</summary>
				private string Description => $"{CandleIndex} {Value}";
			}
		}

		/// <summary>A view of the indicator</summary>
		public class View :IndicatorView
		{
			public View(MovingAverage ma, IChartView chart)
				: base(ma.Id, ma.Name, chart, ma)
			{
				Cache = new ChartGfxCache(CreatePiece);
				Data = new MAContext(ma, chart.Instrument);
				Data.DataChanged += (s,a) =>
				{
					Cache.Invalidate(a.Range);
				};
			}
			public override void Dispose()
			{
				Util.Dispose(Data);
				Cache = null;
				base.Dispose();
			}

			/// <summary>The indicator data source</summary>
			private MovingAverage MA => (MovingAverage)Indicator;

			/// <summary>The moving average data</summary>
			private MAContext Data { get; }

			/// <summary>A cache of graphics objects than span X-axis ranges</summary>
			private ChartGfxCache Cache
			{
				get => m_cache;
				set
				{
					if (m_cache == value) return;
					Util.Dispose(ref m_cache);
					m_cache = value;
				}
			}
			private ChartGfxCache m_cache;

			/// <summary>Create graphics for an X-range spanning 'x'</summary>
			private MAPiece CreatePiece(double x, RangeF missing)
			{
				// The x-axis range spanned by the data
				var x_range = Data.CandleRange;

				// If there is no data, return a null piece spanning the entire range
				if (x_range == RangeF.Invalid)
					return new MAPiece(missing);

				// If 'x' is before the data range, return a null piece for the range [-inf, Data[0])
				if (x_range.CompareTo(x) > 0)
					return new MAPiece(new RangeF(missing.Beg, x_range.Beg));

				// If 'x' is after the data range, return a null piece for the range [Data[N], +inf)
				if (x_range.CompareTo(x) < 0)
					return new MAPiece(new RangeF(x_range.End, missing.End));

				// Find the nearest point in the data to 'x'
				var idx = Data.IndexOf(x);
				Debug.Assert(idx >= 0 && idx < Data.Count);

				// Generate an index range based on 'missing', limited to the block size
				const int PieceBlockSize = 512;
				var idx0 = Math.Max(Data.IndexOf(missing.Beg), idx - PieceBlockSize);
				var idx1 = Math.Min(Data.IndexOf(missing.End), idx + PieceBlockSize);

				// Set the x-axis range represented by this piece
				x_range.Beg = idx0 == 0          ? missing.Beg : Data[idx0].CandleIndex;
				x_range.End = idx1 == Data.Count ? missing.End : Data[idx1].CandleIndex;

				// Create a piece that spans the index range
				idx1 = Math.Min(idx1 + 1, Data.Count); // Add 1 to connect to the next piece
				var data = new Range(idx0, idx1).Select(i => Data[(int)i]);
				var piece = new MAPiece(x_range, MA, data);
				return piece;
			}

			/// <summary>Update when indicator settings change</summary>
			protected override void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				base.HandleSettingChange(sender, e);
				if (e.After)
				{
					switch (e.Key)
					{
					case nameof(Exponential):
					case nameof(BBStdDev):
						{
							Data.Reset();
							Cache.Invalidate();
							break;
						}
					case nameof(Periods):
						{
							Data.Reset();
							Cache.Invalidate();
							NotifyPropertyChanged(nameof(Label));
							break;
						}
					case nameof(Colour):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.Line.Stroke = MA.Colour.ToMediaBrush();
								piece.Glow.Stroke = MA.Colour.Alpha(0.25).ToMediaBrush();
							}
							break;
						}
					case nameof(BBColour):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.High.Stroke = MA.BBColour.ToMediaBrush();
								piece.Low.Stroke = MA.BBColour.ToMediaBrush();
							}
							break;
						}
					case nameof(Width):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.Line.StrokeThickness = MA.Width;
								piece.Glow.StrokeThickness = MA.Width + GlowRadius;
							}
							break;
						}
					case nameof(BBWidth):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.High.StrokeThickness = MA.BBWidth;
								piece.Low.StrokeThickness = MA.BBWidth;
							}
							break;
						}
					case nameof(LineStyle):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.Line.StrokeDashArray = MA.LineStyle.ToStrokeDashArray();
							}
							break;
						}
					case nameof(BBLineStyle):
						{
							foreach (var piece in Cache.Pieces.OfType<MAPiece>())
							{
								piece.High.StrokeDashArray = MA.BBLineStyle.ToStrokeDashArray();
								piece.Low.StrokeDashArray = MA.BBLineStyle.ToStrokeDashArray();
							}
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

				// Detach all first
				foreach (var piece in Cache.Pieces.OfType<MAPiece>())
					piece.Detach();

				// No data, no MA
				if (Data.Count == 0 || !Visible)
					return;

				// Get the range required for display
				var range = Data.CandleRange.Intersect(Chart.XAxis.Range);

				// The line graphics are in chart space, get the transform to client space
				var c2c = Chart.ChartToClientSpace();
				var c2c_2d = new MatrixTransform(c2c.x.x, 0, 0, c2c.y.y, c2c.w.x + MA.XOffset, c2c.w.y);

				// Add each graphics piece. Really, 'Cache.Get' should be
				// called in UpdateGfxCore but this seems to be fast enough.
				foreach (var piece in Cache.Get(range).OfType<MAPiece>())
				{
					// Ignore null graphics pieces (places where there is no data)
					if (piece.NoGfx)
						continue;

					piece.Line.Stroke = MA.Colour.ToMediaBrush();
					piece.Line.Data.Transform = c2c_2d;
					Chart.Overlay.Adopt(piece.Line);

					// Show or hide the glow based on selected/hovered state
					if (Hovered || Selected)
					{
						piece.Glow.Stroke = MA.Colour.Alpha(Selected ? 0.25 : 0.15).ToMediaBrush();
						piece.Glow.Data.Transform = c2c_2d;
						Chart.Overlay.Adopt(piece.Glow);
					}
					else
					{
						piece.Glow.Detach();
					}

					// Show BollingerBands
					if (MA.ShowBollingerBands && MA.BBStdDev != 0)
					{
						piece.High.Data.Transform = c2c_2d;
						piece.Low.Data.Transform = c2c_2d;
						Chart.Overlay.Adopt(piece.High);
						Chart.Overlay.Adopt(piece.Low);
					}
					else
					{
						piece.High.Detach();
						piece.Low.Detach();
					}
				}
			}

			/// <summary>Display the options UI</summary>
			protected override void ShowOptionsUICore()
			{
				if (m_moving_average_ui == null)
				{
					m_moving_average_ui = new MovingAverageUI(Window.GetWindow(Chart), MA);
					m_moving_average_ui.Closed += delegate { m_moving_average_ui = null; };
					m_moving_average_ui.Show();
				}
				m_moving_average_ui.Focus();
			}
			private MovingAverageUI m_moving_average_ui;

			/// <summary>Hit test this indicator</summary>
			public override ChartControl.HitTestResult.Hit HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
			{
				if (Data.Count == 0)
					return null;

				var pt = client_point.ToV2();
				var dist_sq = Math_.Sqr(Chart.Options.MinSelectionDistance);

				// Get the selection distance in chart space, and the index range that needs testing
				var sel_dist = Chart.ClientToChart(new Size(Chart.Options.MinSelectionDistance, Chart.Options.MinSelectionDistance));
				var idx_range = new Range(
					Data.IndexOf(Math.Floor(chart_point.X - sel_dist.Width) + 0),
					Data.IndexOf(Math.Floor(chart_point.X + sel_dist.Width) + 1));

				// The various y offsets to test
				var yofs = MA.ShowBollingerBands && MA.BBStdDev != 0
					? new[] { 0.0, -MA.BBStdDev, +MA.BBStdDev }
					: new[] { 0.0 };

				// Find the closest point to see if there's a hit
				var hit_pt = (v2?)null;
				for (int i = idx_range.Begi, iend = Math.Min(idx_range.Endi, Data.Count - 1); i < iend; ++i)
				{
					var ma0 = Data[i + 0];
					var ma1 = Data[i + 1];
					foreach (var y in yofs)
					{
						var p0 = Chart.ChartToClient(new Point(ma0.CandleIndex, ma0.Value + y * ma0.StdDev)).ToV2();
						var p1 = Chart.ChartToClient(new Point(ma1.CandleIndex, ma1.Value + y * ma1.StdDev)).ToV2();
						var t = Rylogic.Maths.Geometry.ClosestPoint(p0, p1, pt);
						var closest = p0 * (1f - t) + p1 * (t);

						// Record the closest hit point
						var d_sq = (closest - pt).LengthSq;
						if (d_sq < dist_sq)
						{
							hit_pt = closest;
							dist_sq = d_sq;
						}
					}
				}

				return hit_pt != null
					? new ChartControl.HitTestResult.Hit(this, new Point(hit_pt.Value.x, hit_pt.Value.y), null)
					: null;
			}

			/// <summary>Graphics for a piece of the moving average line</summary>
			private class MAPiece :IChartGfxPiece
			{
				public MAPiece(RangeF range)
				{
					Range = range;
					Line = null;
					Glow = null;
					High = null;
					Low = null;
				}
				public MAPiece(RangeF range, MovingAverage ma, IEnumerable<MAContext.MAPoint> data)
				{
					Range = range;

					{// Create the path for the MA
						var path = Geometry_.MakePolyline(data.Select(x => new Point(x.CandleIndex, x.Value)));
						Line = new Path
						{
							Data = path,
							StrokeThickness = ma.Width,
							StrokeDashArray = ma.LineStyle.ToStrokeDashArray(),
							StrokeStartLineCap = PenLineCap.Round,
							StrokeEndLineCap = PenLineCap.Round,
							StrokeLineJoin = PenLineJoin.Round,
						};
						Glow = new Path
						{
							Data = path,
							StrokeThickness = ma.Width + GlowRadius,
							StrokeStartLineCap = PenLineCap.Round,
							StrokeEndLineCap = PenLineCap.Round,
							StrokeLineJoin = PenLineJoin.Round,
						};
					}
					{// Create the path the high BB
						var path = Geometry_.MakePolyline(data.Select(x => new Point(x.CandleIndex, x.Value + ma.BBStdDev * x.StdDev)));
						High = new Path
						{
							Data = path,
							Stroke = ma.BBColour.ToMediaBrush(),
							StrokeThickness = ma.Width * 0.5,
							StrokeStartLineCap = PenLineCap.Round,
							StrokeEndLineCap = PenLineCap.Round,
							StrokeLineJoin = PenLineJoin.Round,
						};
					}
					{// Create the path the low BB
						var path = Geometry_.MakePolyline(data.Select(x => new Point(x.CandleIndex, x.Value - ma.BBStdDev * x.StdDev)));
						Low = new Path
						{
							Data = path,
							Stroke = ma.BBColour.ToMediaBrush(),
							StrokeThickness = ma.Width * 0.5,
							StrokeStartLineCap = PenLineCap.Round,
							StrokeEndLineCap = PenLineCap.Round,
							StrokeLineJoin = PenLineJoin.Round,
						};
					}
				}
				public void Dispose()
				{
					if (!NoGfx)
						Detach();
				}

				/// <summary>Detach all graphics</summary>
				public void Detach()
				{
					Line.Detach();
					Glow.Detach();
					High.Detach();
					Low.Detach();
				}

				/// <summary>True if this is a null graphics piece</summary>
				public bool NoGfx => Line == null;

				/// <summary>The X-Axis span covered by this piece</summary>
				public RangeF Range { get; }

				/// <summary>The moving average line</summary>
				public Path Line { get; }

				/// <summary>The glow when when the line is hovered</summary>
				public Path Glow { get; }

				/// <summary>The high Bollinger band</summary>
				public Path High { get; }

				/// <summary>The low Bollinger band</summary>
				public Path Low { get; }
			}
		}

		/// <summary>Event args for MAContext.DataChanged</summary>
		public class DataChangedEventArgs
		{
			public DataChangedEventArgs(Range range)
			{
				Range = range;
			}

			/// <summary>The X Range of the data that has changed</summary>
			public Range Range { get; }
		}
	}
}

