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
			private readonly MovingAverage m_ma;
			private readonly List<MAPoint> m_data;
			private IStatMeanAndVarianceSingleVariable<double> m_stat;

			public MAContext(MovingAverage ma, Instrument instrument)
			{
				m_ma = ma;
				m_data = new List<MAPoint>();
				Range = Range.Invalid;
				Instrument = instrument;
			}
			public void Dispose()
			{
				Instrument = null;
			}

			/// <summary>Reset the data and recalculate</summary>
			public void Reset()
			{
				m_data.Clear();
				m_stat = null;
				CalculateMA(Range);
			}

			/// <summary>Basic list access</summary>
			public int Count => m_data.Count;
			public MAPoint this[int i] => m_data[i];

			/// <summary>Returns the nearest index position of 'candle_index' within the data</summary>
			public int IndexOf(double candle_index) => m_data.BinarySearch(pt => pt.CandleIndex.CompareTo(candle_index), find_insert_position: true);

			/// <summary>The candle range spanned by this context</summary>
			public RangeF CandleRange => Count != 0 ? new RangeF(m_data.Front().CandleIndex, m_data.Back().CandleIndex) : Range.Invalid;

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
						Range = Range.Invalid;
					}
					m_instrument = value;
					if (m_instrument != null)
					{
						HandleDataChanged(null, null);
						m_instrument.DataChanged += HandleDataChanged;
					}

					// Handler
					void HandleDataChanged(object sender, DataEventArgs e)
					{
						// Update the range when the instrument changes.
						if (Range == Range.Invalid || Range.End > Instrument.Count)
						{
							// If the range is currently invalid, use a sensible default
							Range = new Range(
								Math.Max(0, Instrument.Count - Instrument.CacheChunkSize),
								Instrument.Count);
						}
						else
						{
							// If the range used to include the latest candle, grow the range as new candles arrive
							var grow = Math.Abs(Instrument.Count - Range.End) <= 1 ? 1 : 0;
							Range = new Range(
								Math_.Clamp(Range.Beg, 0, Instrument.Count),
								Math_.Clamp(Range.End + grow, 0, Instrument.Count));
						}
					}
				}
			}
			private Instrument m_instrument;

			/// <summary>The requested data range</summary>
			public Range Range
			{
				get => m_range;
				set
				{
					if (m_range == value) return;
					CalculateMA(value);
				}
			}
			private Range m_range;

			/// <summary>Calculate the data points over 'range'</summary>
			private void CalculateMA(Range range)
			{
				// Flush the data
				if (range == Range.Invalid || range.Empty || Instrument == null || Instrument.Count == 0)
				{
					m_data.Clear();
					m_stat = null;
					m_range = Range.Invalid;
					DataChanged?.Invoke(this, new DataChangedEventArgs(Range.Invalid));
					return;
				}

				// 'm_stat' does not include the candle at 'Range.End-1'. This candle is typically the 
				// latest candle which is changing all the time. It's simplier to always exclude the last
				// candle from 'm_stat' than to try and track what's happening to Instruction.Count.
				m_data.Capacity = Math.Max(m_data.Capacity, range.Sizei);

				// See if we can incrementally update the range
				if (m_stat != null && Range != Range.Invalid && !Range.Empty && Range.Beg <= range.Beg && range.End > Range.End - 1)
				{
					// Incremental update
					m_data.RemoveToEnd(IndexOf(Range.End - 1));
					m_data.AddRange(CalculateMA(new Range(Range.End - 1, range.End - 1), m_stat));
					m_data.AddRange(CalculateMA(new Range(range.End - 1, range.End), CopyStat(m_stat)));
					DataChanged?.Invoke(this, new DataChangedEventArgs(new Range(Range.End - 1, range.End)));
				}
				else
				{
					m_stat = NewStat();

					// Recalculate all
					m_data.Clear();
					m_data.AddRange(CalculateMA(new Range(range.Beg, range.End - 1), m_stat));
					m_data.AddRange(CalculateMA(new Range(range.End - 1, range.End), CopyStat(m_stat)));
					DataChanged?.Invoke(this, new DataChangedEventArgs(range));
				}

				// Save the new range
				m_range = range;

				// Return the moving average points over the given range
				IEnumerable<MAPoint> CalculateMA(Range r, IStatMeanAndVarianceSingleVariable<double> stat)
				{
					var ts = Misc.CryptoCurrencyEpoch.Ticks;
					foreach (var i in r.Enumeratei)
					{
						var candle = Instrument[i];
						Debug.Assert(candle.Timestamp >= ts, "Candle data must be strictly ordered by timestamp because of binary searches");
						ts = candle.Timestamp;

						// Calculate the MA value and save it
						stat.Add(candle.Close);
						yield return new MAPoint(i, ts, stat.Mean, stat.PopStdDev);
					}
				}
				IStatMeanAndVarianceSingleVariable<double> NewStat()
				{
					// Create a new instance of the stat type
					return m_ma.Exponential
						? (IStatMeanAndVarianceSingleVariable<double>)new ExponentialMovingAverage(m_ma.Periods)
						: (IStatMeanAndVarianceSingleVariable<double>)new SimpleMovingAverage(m_ma.Periods);
				}
				IStatMeanAndVarianceSingleVariable<double> CopyStat(IStatMeanAndVarianceSingleVariable<double> rhs)
				{
					return m_ma.Exponential
						? (IStatMeanAndVarianceSingleVariable<double>)new ExponentialMovingAverage((ExponentialMovingAverage)rhs)
						: (IStatMeanAndVarianceSingleVariable<double>)new SimpleMovingAverage((SimpleMovingAverage)rhs);
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
				// Find the nearest point in the data to 'x'
				var idx = Data.IndexOf(x);

				// Convert 'missing' to an index range within the data
				var idx_missing = new Range(
					Data.IndexOf(missing.Beg),
					Data.IndexOf(missing.End));

				// Limit the size of 'idx_missing' to the block size
				const int PieceBlockSize = 256;
				var idx_range = new Range(
					Math.Max(idx_missing.Beg, idx - PieceBlockSize),
					Math.Min(idx_missing.End, idx + PieceBlockSize));
				Debug.Assert(!idx_range.Empty);

				// Create a piece that spans the missing range
				var data = idx_range.Select(i => Data[(int)i]);
				var piece = new MAPiece(idx_range, MA, data);
				return piece;
			}

			/// <summary>Update when indicator settings change</summary>
			protected override void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				if (e.Before) return;
				switch (e.Key)
				{
				case nameof(Exponential):
				case nameof(Periods):
				case nameof(BBStdDev):
					{
						Data.Reset();
						Cache.Invalidate();
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

			/// <summary>Update the transforms for the graphics model</summary>
			protected override void UpdateSceneCore()
			{
				base.UpdateSceneCore();

				// Add the graphics pieces over the visible range
				if (Data.Count == 0 || !Visible)
				{
					foreach (var piece in Cache.Pieces.OfType<MAPiece>())
						piece.Detach();

					return;
				}

				// Get the range required for display
				var range = Data.CandleRange.Intersect(Chart.XAxis.Range);

				// The line graphics are in chart space, get the transform to client space
				var c2c = Chart.ChartToClientSpace();
				var c2c_2d = new MatrixTransform(c2c.x.x, 0, 0, c2c.y.y, c2c.w.x + MA.XOffset, c2c.w.y);

				// Add each graphics piece. Really, 'Cache.Get' should be
				// called in UpdateGfxCore but this seems to be fast enough.
				foreach (var piece in Cache.Get(range).OfType<MAPiece>())
				{
					piece.Line.Data.Transform = c2c_2d;
					Chart.Overlay.Adopt(piece.Line);

					// Show or hide the glow based on selected/hovered state
					if (Hovered || Selected)
					{
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
				public MAPiece(Range range, MovingAverage ma, IEnumerable<MAContext.MAPoint> data)
				{
					Range = range;

					{// Create the path for the MA
						var path = Geometry_.MakePolyline(data.Select(x => new Point(x.CandleIndex, x.Value)));
						Line = new Path
						{
							Data = path,
							Stroke = ma.Colour.ToMediaBrush(),
							StrokeThickness = ma.Width,
							StrokeDashArray = ma.LineStyle.ToStrokeDashArray(),
							StrokeStartLineCap = PenLineCap.Round,
							StrokeEndLineCap = PenLineCap.Round,
							StrokeLineJoin = PenLineJoin.Round,
						};
						Glow = new Path
						{
							Data = path,
							Stroke = ma.Colour.Alpha(0.25).ToMediaBrush(),
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
