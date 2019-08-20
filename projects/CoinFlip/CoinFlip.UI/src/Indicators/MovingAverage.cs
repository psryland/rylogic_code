using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
			Name = nameof(MovingAverage);
			Exponential = false;
			Periods = 50;
			Colour = Colour32.Blue;
			Width = 1.0;
			LineStyle = ELineStyles.Solid;
			ShowBollingerBands = false;
			BollingerBandsStdDev = 2.0;
			ColourBollingerBands = Colour32.LightBlue;
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
		public double BollingerBandsStdDev
		{
			get { return get<double>(nameof(BollingerBandsStdDev)); }
			set { set(nameof(BollingerBandsStdDev), Math_.Clamp(value, 0, 5.0f)); }
		}

		/// <summary>The line colour for the Bollinger Bands</summary>
		public Colour32 ColourBollingerBands
		{
			get { return get<Colour32>(nameof(ColourBollingerBands)); }
			set { set(nameof(ColourBollingerBands), value); }
		}

		/// <summary>Shift the indicator in the X axis direction</summary>
		public double XOffset
		{
			get { return get<double>(nameof(XOffset)); }
			set { set(nameof(XOffset), value); }
		}

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
						// Calculate the MA over the cached range of instrument data.
						Range = m_instrument.CachedIndexRange;
						m_instrument.DataChanged += HandleDataChanged;
					}

					// Handler
					void HandleDataChanged(object sender, DataEventArgs e)
					{
						// Update the data when the instrument changes
						Range = m_instrument.CachedIndexRange;
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
					// Flush the data
					if (value == Range.Invalid || Instrument == null || Instrument.Count == 0)
					{
						m_data.Clear();
						m_range = Range.Invalid;
						return;
					}

					// Do not include 'Instrument.Count-1' in 'm_stat' because it is changing all the time.
					var new_range = value;
					var old_range = m_range;
					if (new_range.End == Instrument.Count) --new_range.End;
					if (old_range.End == Instrument.Count) --old_range.End;

					// Populate the data over 'range'
					if (old_range == Range.Invalid)
					{
						m_stat = NewStat();
						m_data.Capacity = new_range.Sizei;
						m_data.Assign(CalculateMA(new_range, m_stat));
					}
					else
					{
						// If the end of the range has been extended, append to the data
						if (new_range.End > old_range.End)
						{
							var r = new Range(old_range.End, new_range.End);
							m_data.RemoveToEnd(IndexOf(r.Beg));
							m_data.AddRange(CalculateMA(r, m_stat));
						}

						// If start of the range has been extended, prepend to the data
						if (new_range.Beg < old_range.Beg)
						{
							// Overlap the existing range by 'Periods' because of the warm up period with moving averages
							var r = new Range(new_range.Beg, Math.Min(old_range.Beg + m_ma.Periods, Instrument.Count - 1));
							m_data.RemoveRange(0, IndexOf(r.End));
							m_data.InsertRange(0, CalculateMA(r, NewStat()));
						}
					}

					// If the requested range includes the latest candle, add one more data point to the data
					// using a copy of 'm_stat' since the latest candle changes.
					if (value.End == Instrument.Count)
					{
						var r = new Range(Instrument.Count - 1, Instrument.Count);
						m_data.AddRange(CalculateMA(r, CopyStat(m_stat)));
					}

					// Save the new range
					m_range = value;

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
			}
			private Range m_range;

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
			}
			public override void Dispose()
			{
				Util.Dispose(Data);
				Cache = null;
				base.Dispose();
			}
			protected override void HandleSettingChange(object sender, SettingChangeEventArgs e)
			{
				//switch (e.Key)
				//{
				//case nameof(Colour):
				//	{
				//		Line.Stroke = Data.Colour.ToMediaBrush();
				//		Glow.Stroke = Data.Colour.Alpha(0.25).ToMediaBrush();
				//		Grab0.Stroke = Data.Colour.ToMediaBrush();
				//		Grab1.Stroke = Data.Colour.ToMediaBrush();
				//		Grab0.Fill = Data.Colour.Alpha(0.25).ToMediaBrush();
				//		Grab1.Fill = Data.Colour.Alpha(0.25).ToMediaBrush();
				//		break;
				//	}
				//case nameof(Width):
				//	{
				//		Line.StrokeThickness = Data.Width;
				//		Glow.StrokeThickness = Data.Width + GrabRadius * 2;
				//		break;
				//	}
				//case nameof(LineStyle):
				//	{
				//		Line.StrokeDashArray = Data.LineStyle.ToStrokeDashArray();
				//		break;
				//	}
				//}
				Invalidate();
			}
			protected override void UpdateSceneCore(View3d.Window window)
			{
				base.UpdateSceneCore(window);

				// Remove graphics
				window.RemoveObjects(new[] { Id }, 1, 0);

				// Add the graphics pieces over the visible range
				if (Data.Count != 0 && Visible)
				{
					// Get the range required for display
					var range = Data.CandleRange.Intersect(Chart.XAxis.Range);

					// Add each graphics piece. Really, 'Cache.Get' should be
					// called in UpdateGfxCore but this seems to be fast enough.
					foreach (var piece in Cache.Get(range))
					{
						// Show or hide the glow based on selected/hovered state
					//	var flags = (Hovered || Selected ? View3d.ENuggetFlag.None : View3d.ENuggetFlag.Hidden) | View3d.ENuggetFlag.TintHasAlpha;
					//	piece.Gfx.NuggetFlagsSet(flags, null, 1);

						piece.Gfx.O2P = m4x4.Translation((float)MA.XOffset, 0f, 0f);
						window.AddObject(piece.Gfx);
					}
				}
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
			private ChartGfxPiece CreatePiece(double x, RangeF missing)
			{
				// Find the nearest point in the data to 'x'
				var idx = Data.IndexOf(x);

				// Convert 'missing' to an index range within the data
				var idx_missing = new Range(
					Data.IndexOf(missing.Beg),
					Data.IndexOf(missing.End));

				// Limit the size of 'idx_missing' to the block size
				const int PieceBlockSize = 4096;
				var idx_range = new Range(
					Math.Max(idx_missing.Beg, idx - PieceBlockSize),
					Math.Min(idx_missing.End, idx + PieceBlockSize));

				// Resize the geometry buffers
				var count = idx_range.Counti;
				m_vbuf.Resize(count);
				m_ibuf.Resize(count);
				m_nbuf.Resize(2);

				// Get the colours for the MA line
				var ma_colour = MA.Colour;
				var bol_colour = MA.ColourBollingerBands;

				// Create the MA model
				var vert = 0;
				var indx = 0;
				foreach (var i in idx_range.Enumeratei)
				{
					var v = vert;
					var ma = Data[i];
					m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)ma.Value, 0, 1f), ma_colour);
					m_ibuf[indx++] = (ushort)(v);
				}

				{// Create a nugget for the MA model
					var mat = View3d.Material.New();
					mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineListGS, $"*LineWidth {{{MA.Width}}}");
					m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, View3d.ENuggetFlag.None, false, mat);
				}

				{// Create a nugget for the hover glow
					var mat = View3d.Material.New();
					mat.m_tint = ma_colour.Alpha(0.25);
					mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineStripGS, $"*LineWidth {{{MA.Width + GlowRadius * 2}}}");
					m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, View3d.ENuggetFlag.TintHasAlpha, true, mat);
				}

				// Add geometry for Bollinger bands
				if (MA.ShowBollingerBands && MA.BollingerBandsStdDev != 0)
				{
					m_vbuf.Resize(m_vbuf.Count + 2 * count);
					m_ibuf.Resize(m_ibuf.Count + 2 * count);
					m_nbuf.Resize(m_nbuf.Count + 2);

					// Lower band
					foreach (var i in idx_range.Enumeratei)
					{
						var v = vert;
						var ma = Data[i];
						m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)(ma.Value - MA.BollingerBandsStdDev * ma.StdDev), 0, 1f), bol_colour);
						m_ibuf[indx++] = (ushort)(v);
					}
					m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, (uint)(vert - count), (uint)vert, (uint)(indx - count), (uint)indx);

					// Upper band
					foreach (var i in idx_range.Enumeratei)
					{
						var v = vert;
						var ma = Data[i];
						m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)(ma.Value + MA.BollingerBandsStdDev * ma.StdDev), 0, 1f), bol_colour);
						m_ibuf[indx++] = (ushort)(v);
					}
					m_nbuf[2] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, (uint)(vert - count), (uint)vert, (uint)(indx - count), (uint)indx);
				}

				// Create the graphics
				var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End}]", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
				var x_range = new RangeF(Data[idx_range.Begi].CandleIndex, Data[idx_range.Endi - 1].CandleIndex + 1);
				return new ChartGfxPiece(gfx, x_range);
			}

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
				var yofs = MA.ShowBollingerBands && MA.BollingerBandsStdDev != 0
					? new[] { 0.0, -MA.BollingerBandsStdDev, +MA.BollingerBandsStdDev }
					: new[] { 0.0 };

				// Find the closest point to see if there's a hit
				var hit_pt = (v2?)null;
				for (int i = idx_range.Begi, iend = idx_range.Endi - 1; i < iend; ++i)
				{
					var ma0 = Data[i + 0];
					var ma1 = Data[i + 1];
					foreach (var y in yofs)
					{
						var p0 = Chart.ChartToClient(new Point(ma0.CandleIndex, ma0.Value + y * ma0.StdDev)).ToV2();
						var p1 = Chart.ChartToClient(new Point(ma1.CandleIndex, ma1.Value + y * ma1.StdDev)).ToV2();
						var t = Geometry.ClosestPoint(p0, p1, pt);
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
		}
	}
}
