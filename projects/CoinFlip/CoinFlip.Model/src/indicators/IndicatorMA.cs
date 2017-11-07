using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>Moving average indicator</summary>
	public class IndicatorMA :Indicator
	{
		/// <summary>The moving average data. Each point is the average for one candle</summary>
		private List<MAPoint> m_ma;

		public IndicatorMA(SettingsData settings = null)
			: base(Guid.NewGuid(), "MA", settings ?? new SettingsData())
		{
			Init();
		}
		public IndicatorMA(XElement node)
			: base(node)
		{
			Init();
		}
		private void Init()
		{
			m_ma = new List<MAPoint>();
			Cache = new ChartDataSeries.GfxCache(CreatePiece);
		}
		protected override void Dispose(bool disposing)
		{
			Cache = null;
			base.Dispose(disposing);
		}

		/// <summary>Settings for this indicator</summary>
		public SettingsData Settings
		{
			[DebuggerStepThrough] get { return (SettingsData)SettingsInternal; }
		}

		/// <summary>A cache of graphics pieces for this data series</summary>
		private ChartDataSeries.GfxCache Cache
		{
			get { return m_impl_cache; }
			set
			{
				if (m_impl_cache == value) return;
				Util.Dispose(ref m_impl_cache);
				m_impl_cache = value;
			}
		}
		private ChartDataSeries.GfxCache m_impl_cache;

		/// <summary>Create graphics for an X-range spanning 'x'</summary>
		private ChartDataSeries.GfxPiece CreatePiece(double x, RangeF missing)
		{
			// Find the nearest point in the data to 'x'
			var idx = m_ma.BinarySearch(pt => pt.CandleIndex.CompareTo(x), find_insert_position: true);

			// Convert 'missing' to an index range within the data
			var idx_missing = new Range(
				m_ma.BinarySearch(pt => pt.CandleIndex.CompareTo(missing.Beg), find_insert_position:true),
				m_ma.BinarySearch(pt => pt.CandleIndex.CompareTo(missing.End), find_insert_position:true));

			// Limit the size of 'idx_missing' to the block size
			const int PieceBlockSize = 4096;
			var idx_range = new Range(
				Math.Max(idx_missing.Beg, idx - PieceBlockSize),
				Math.Min(idx_missing.End, idx + PieceBlockSize));

			// Resize the geometry buffers
			var count = idx_range.Counti;
			m_vbuf.Resize(count);
			m_ibuf.Resize(count);
			m_nbuf.Resize(1);

			// Get the colours for the MA line
			var ma_colour = Settings.ColourMA.ToArgbU();
			var bol_colour = Settings.ColourBollingerBands.ToArgbU();

			// Create the MA model
			var vert = 0;
			var indx = 0;
			foreach (var i in idx_range.Enumeratei)
			{
				var v = vert;
				var ma = m_ma[i];
				m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)ma.Value, ZOrder.Indicators, 1f), ma_colour);
				m_ibuf[indx++] = (ushort)(v);
			}

			// Create a nugget for the MA model
			{
				var mat = View3d.Material.New();
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineListGS, (float)Settings.Width);
				m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, false, false, mat);
			}

			// Add geometry for Bollinger bands
			if (Settings.ShowBollingerBands && Settings.BollingerBandsStdDev != 0)
			{
				m_vbuf.Resize(m_vbuf.Count + 2*count);
				m_ibuf.Resize(m_ibuf.Count + 2*count);
				m_nbuf.Resize(m_nbuf.Count + 2);

				// Lower band
				foreach (var i in idx_range.Enumeratei)
				{
					var v = vert;
					var ma = m_ma[i];
					m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)(ma.Value - Settings.BollingerBandsStdDev * ma.StdDev), ZOrder.Indicators, 1f), bol_colour);
					m_ibuf[indx++] = (ushort)(v);
				}
				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(vert-count), (uint)vert, (uint)(indx-count), (uint)indx);

				// Upper band
				foreach (var i in idx_range.Enumeratei)
				{
					var v = vert;
					var ma = m_ma[i];
					m_vbuf[vert++] = new View3d.Vertex(new v4((float)ma.CandleIndex, (float)(ma.Value + Settings.BollingerBandsStdDev * ma.StdDev), ZOrder.Indicators, 1f), bol_colour);
					m_ibuf[indx++] = (ushort)(v);
				}
				m_nbuf[2] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(vert-count), (uint)vert, (uint)(indx-count), (uint)indx);
			}

			// Create the graphics
			var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End}]", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
			var x_range = new RangeF(m_ma[idx_range.Begi].CandleIndex, m_ma[idx_range.Endi-1].CandleIndex + 1);
			return new ChartDataSeries.GfxPiece(gfx, x_range);
		}

		/// <summary>Calculate the MA</summary>
		protected override void ResetCore()
		{
			m_ma.Clear();
			Cache.Flush();

			if (Instrument != null)
			{
				// Create a moving average stat that the instrument data is added to.
				var avr = Settings.ExponentialMA
					? (object)new ExpMovingAvr(Settings.WindowSize)
					: (object)new MovingAvr(Settings.WindowSize);

				// Calculate the MA over the cached range of instrument data.
				var range = Instrument.CachedIndexRange;
				m_ma.Capacity = range.Sizei;

				// Calculate the moving average data
				var stat = (IStatSingleVariable)avr;
				var value = (IStatMeanAndVariance)avr;
				var ts = DateTimeOffset.MinValue.Ticks;
				foreach (var i in range.Enumeratei)
				{
					var candle = Instrument[i];

					// Candle data must be strictly ordered by timestamp because I'm using binary search
					Debug.Assert(candle.Timestamp >= ts);
					ts = candle.Timestamp;

					// Calculate the MA value and save it
					stat.Add(candle.Median);
					m_ma.Add(new MAPoint(i, ts, value.Mean, value.PopStdDev));
				}
			}
		}

		/// <summary>Handle settings changing</summary>
		protected override void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			// If the setting affects the underlying data, reset
			switch (e.Key)
			{
			case nameof(SettingsData.WindowSize):
			case nameof(SettingsData.ExponentialMA):
				ResetRequired = true;
				break;
			case nameof(SettingsData.Width):
			case nameof(SettingsData.ColourMA):
			case nameof(SettingsData.ShowBollingerBands):
			case nameof(SettingsData.BollingerBandsStdDev):
			case nameof(SettingsData.ColourBollingerBands):	
				Cache.Flush();
				break;
			}

			base.HandleSettingChanged(sender, e);
		}

		/// <summary>Invalidate if the chart moves beyond the range of the graphics</summary>
		protected override void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
		{
			base.HandleChartMoved(sender, e);
			if (Instrument == null)
				return;

			// Get the visible X axis range
			var rng = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
			if (rng.Count == 0)
				return;

			// If the MA data does not span this range, reset
			if (rng.Begi < m_ma.Front().CandleIndex ||
				rng.Endi > m_ma.Back().CandleIndex)
				ResetRequired = true;
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		protected override void UpdateSceneCore(View3d.Window window)
		{
			// Remove graphics
			window.RemoveObjects(Id);

			// Add the graphics pieces over the visible range
			if (m_ma.Count != 0 && Visible)
			{
				// Get the range required for display
				var range = new RangeF(
					Math.Max(Chart.XAxis.Min, m_ma.Front().CandleIndex),
					Math.Min(Chart.XAxis.Max, m_ma.Back().CandleIndex+1));

				// Add each graphics piece
				foreach (var piece in Cache.Get(range))
				{
					piece.Gfx.O2P = m4x4.Translation(Settings.XOffset, 0f, 0f);
					window.AddObject(piece.Gfx);
				}
			}
		}

		/// <summary>Hit test this indicator</summary>
		public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.Camera cam)
		{
			if (m_ma.Count == 0)
				return null;

			// Test the distance to the MA over the selection distance
			var dist_sq = Maths.Sqr(Chart.Options.MinSelectionDistance);
			var idx_range = new Range(
				m_ma.BinarySearch(pt => pt.CandleIndex.CompareTo(chart_point.X - Chart.Options.MinSelectionDistance/2f), find_insert_position:true),
				m_ma.BinarySearch(pt => pt.CandleIndex.CompareTo(chart_point.X + Chart.Options.MinSelectionDistance/2f), find_insert_position:true));
			var yofs = Settings.ShowBollingerBands && Settings.BollingerBandsStdDev != 0
				? new[] { 0f, -Settings.BollingerBandsStdDev, +Settings.BollingerBandsStdDev }
				: new[] { 0f };

			foreach (var i in idx_range.Enumeratei)
			{
				var ma = m_ma[i];
				foreach (var y in yofs)
				{
					var pt = Chart.ChartToClient(new PointF((float)ma.CandleIndex, (float)(ma.Value + y * ma.StdDev)));
					if (Drawing_.Subtract(client_point, pt).Length2Sq() < dist_sq)
						return new ChartControl.HitTestResult.Hit(this, pt, null);
				}
			}

			return null;
		}

		/// <summary>The number of values in this MA data</summary>
		public int Count
		{
			get { return m_ma.Count; }
		}

		/// <summary>Access this object as an array of EMA data using negative indices (i.e. where 0 = latest)</summary>
		public double this[int idx]
		{
			get
			{
				Debug.Assert(Instrument.Count == m_ma.Count);
				Debug.Assert(idx.Within(0, Instrument.Count));
				return m_ma[idx].Value;
			}
		}

		#region MA Point

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
			public double CandleIndex { get; private set; }

			/// <summary>The value of the moving average at 'Timestamp'</summary>
			public double Value { get; private set; }

			/// <summary>The standard deviation of the moving average at 'Timestamp'</summary>
			public double StdDev { get; private set; }

			/// <summary>The candle timestamp that this MA point corresponds to</summary>
			public long Timestamp { get; private set; }

			/// <summary>Guess at whether double or long values are used</summary>
			private string Description
			{
				get { return $"{CandleIndex} {Value}"; }
			}
		}

		#endregion

		#region Settings
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsXml<SettingsData>
		{
			public SettingsData()
			{
				WindowSize           = 14;
				Width                = 5;
				ExponentialMA        = false;
				ColourMA             = Color.LightGreen;
				ShowBollingerBands   = false;
				BollingerBandsStdDev = 2f;
				ColourBollingerBands = Color.Purple;
				XOffset              = 0f;
			}
			public SettingsData(XElement node)
				: base(node)
			{ }

			/// <summary>The window size of the EMA</summary>
			public int WindowSize
			{
				get { return get<int>(nameof(WindowSize)); }
				set { set(nameof(WindowSize), Maths.Clamp(value, 1, MaxWindowSize)); }
			}
			public const int MaxWindowSize = 500;

			/// <summary>The width of the line to draw</summary>
			public int Width
			{
				get { return get<int>(nameof(Width)); }
				set { set(nameof(Width), Maths.Clamp(value, 1, 50)); }
			}

			/// <summary>True to use an exponential moving average</summary>
			public bool ExponentialMA
			{
				get { return get<bool>(nameof(ExponentialMA)); }
				set { set(nameof(ExponentialMA), value); }
			}

			/// <summary>The line colour</summary>
			public Color ColourMA
			{
				get { return get<Color>(nameof(ColourMA)); }
				set { set(nameof(ColourMA), value); }
			}

			/// <summary>Show Bollinger Bands around the MA</summary>
			public bool ShowBollingerBands
			{
				get { return get<bool>(nameof(ShowBollingerBands)); }
				set { set(nameof(ShowBollingerBands), value); }
			}

			/// <summary>The Bollinger band size in units of the standard deviations</summary>
			public float BollingerBandsStdDev
			{
				get { return get<float>(nameof(BollingerBandsStdDev)); }
				set { set(nameof(BollingerBandsStdDev), Maths.Clamp(value, 0, 5.0f)); }
			}

			/// <summary>The line colour for the Bollinger Bands</summary>
			public Color ColourBollingerBands
			{
				get { return get<Color>(nameof(ColourBollingerBands)); }
				set { set(nameof(ColourBollingerBands), value); }
			}

			/// <summary>Shift the indicator in the X axis direction</summary>
			public float XOffset
			{
				get { return get<float>(nameof(XOffset)); }
				set { set(nameof(XOffset), value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> { }
		}
		#endregion
	}
}
