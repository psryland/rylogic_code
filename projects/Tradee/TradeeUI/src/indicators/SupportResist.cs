using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Finds support and resistance levels in the data</summary>
	public class SnR :IndicatorBase
	{
		public SnR(SnRSettings settings = null)
			:base(Guid.NewGuid(), "SnR", settings ?? new SnRSettings())
		{
			Levels = new List<Bucket>();
			Window = new List<Bucket>();
		}
		public SnR(XElement node)
			:base(node)
		{
			Levels = new List<Bucket>();
			Window = new List<Bucket>();
		}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}
		protected override void SetChartCore(ChartControl chart)
		{
			// Invalidate the graphics whenever the x axis moves
			if (Chart != null)
			{
				Chart.XAxis.Zoomed -= Invalidate;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.XAxis.Zoomed += Invalidate;
			}
		}
		protected override void SetInstrumentCore(Instrument instr)
		{
			// Use a clone of the instrument so we can set
			// the time frame to the SourceTimeFrame.
			if (Instrument != null)
			{
				Util.Dispose(Instrument);
			}

			base.SetInstrumentCore(new Instrument(instr, Settings.SourceTimeFrame));

			// Set the instrument time frame to the source time frame
			if (Instrument != null)
			{
				Instrument.TimeFrame = Settings.SourceTimeFrame;
				CalculateLevels();
			}
		}
		protected override void UpdateGfxCore()
		{
			var snr_colour = Settings.Colour.ToArgbU();
			var width = Settings.GraphicsWidth * Chart.XAxis.Span;
			var attr = Settings.Attribute;
			var power = Settings.Power;
			var count = Levels.Count;
			if (count <= 1)
			{
				Gfx = null;
				return;
			}

			// Create graphics for the support and resistance data
			m_vbuf.Resize(count * 2);
			m_ibuf.Resize(count * 2);
			m_nbuf.Resize(1);

			var values = 
				attr == EAttribute.MedianCount ? Levels.Select(x => x.Median).Normalise() :
				attr == EAttribute.TradeVolume ? Levels.Select(x => x.Volume).Normalise() :
				attr == EAttribute.TimeAtLevel ? Levels.Select(x => x.Time  ).Normalise() :
				null;

			int l = 0, v = 0, i = 0;
			foreach (var value in values)
			{
				var x = (float)(Math.Pow(value, power) * width);
				var y = (float)(Levels[l].Price);
				++l;

				m_ibuf[i++] = (ushort)v;
				m_vbuf[v++] = new View3d.Vertex(new v4(-x,y,0,1));
				m_ibuf[i++] = (ushort)v;
				m_vbuf[v++] = new View3d.Vertex(new v4( 0,y,0,1));
			}
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.TriStrip, View3d.EGeom.Vert, 0, (uint)v, 0, (uint)i, !Bit.AllSet(snr_colour,0xFF000000));

			// Create the graphics
			Gfx = new View3d.Object(Name, snr_colour, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx != null)
			{
				// Graphics are created at the origin, position at XAxis.Max
				Gfx.O2P = m4x4.Translation((float)Chart.XAxis.Max, 0, ChartUI.Z.SnR);
				window.AddObject(Gfx);
			}
		}

		/// <summary>Settings for this indicator</summary>
		public SnRSettings Settings
		{
			[DebuggerStepThrough] get { return (SnRSettings)IndicatorSettingsInternal; }
		}

		/// <summary>Attributes to base the SnR Levels on</summary>
		public enum EAttribute
		{
			MedianCount,
			TradeVolume,
			TimeAtLevel,
		}

		/// <summary>The Ask price line</summary>
		public View3d.Object Gfx
		{
			[DebuggerStepThrough] get { return m_impl_gfx; }
			set
			{
				if (m_impl_gfx == value) return;
				Util.Dispose(ref m_impl_gfx);
				m_impl_gfx = value;
			}
		}
		private View3d.Object m_impl_gfx;

		/// <summary>A sorted list of prices</summary>
		private List<Bucket> Levels
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>A buffer of levels used to find S&R levels over a moving window</summary>
		private List<Bucket> Window
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>The price range in each level (bucket)</summary>
		private double BucketSize
		{
			get { return Instrument.PriceData.PipSize * Settings.PipsPerBucket; }
		}

		/// <summary>Return a price quantised to the bucket price levels</summary>
		private double QuantisePrice(double price)
		{
			var bucket_size = BucketSize;
			return (long)(price / bucket_size) * bucket_size;
		}

		/// <summary>Return the bucket index for a given price. Note: can exceed the Levels collection</summary>
		private int BucketIndex(double price, List<Bucket> container)
		{
			Debug.Assert(container.Count != 0);
			return (int)((price - container[0].Price) / BucketSize);
		}

		/// <summary>
		/// Return the bucket index for a given price level.
		/// If grow == false, indices can exceed the Levels collection.
		/// If grow == true, successive calls can invalidate indices for lower price levels</summary>
		private Range BucketIndexRange(double price0, double price1, List<Bucket> container, bool grow)
		{
			Debug.Assert(container.Count != 0);
			var idx0 = BucketIndex(price0, container);
			var idx1 = BucketIndex(price1, container);

			// If the 'container' is not large enough, add more levels
			if (grow)
			{
				var bucket_size = BucketSize;
				if (idx0 < 0)
				{
					var cnt = -idx0;
					var p = container.Front().Price - cnt * bucket_size;
					container.InsertRange(0, int_.Range(cnt).Select(x => new Bucket(p + x * bucket_size)));
					idx0 += cnt;
					idx1 += cnt;
				}
				if (idx1 >= container.Count)
				{
					var cnt = idx1 - container.Count + 1;
					var p = container.Back().Price + bucket_size;
					container.InsertRange(container.Count, int_.Range(cnt).Select(x => new Bucket(p + x * bucket_size)));
				}
			}

			return new Range(idx0, idx1);
		}

		/// <summary>Add a candle to the levels window</summary>
		private void CalculateLevels(int index, int window_size)
		{
			// Find levels over a window
			Window.Clear();
			Window.Add(new Bucket(QuantisePrice(Instrument[index].Median)));
			var max = new Bucket(0);

			// Scan from [-window_size, index] to determine S&R levels over the window
			var candle_time = Misc.TimeFrameToTicks(1.0, Instrument.TimeFrame);
			var range = Instrument.IndexRange(index - window_size + 1, index + 1);
			for (int i = range.Begi; i != range.Endi; ++i)
			{
				var candle = Instrument[i];

				// Find the index range (inclusive) spanned by 'candle'.
				var idx = BucketIndexRange(candle.Low, candle.High, Window, true);

				// Add to the count of the bucket that contains the median
				var median_idx = BucketIndex(candle.Median, Window);
				Window[median_idx].Median++;
				max.Median = Math.Max(max.Median, Window[median_idx].Median);

				// Scale the values by the range of buckets the candle spans.
				// So, if a candle only covers one bucket, all candle data contributes to that bucket.
				// If the candle spans N buckets, 1/N of the data is added to each bucket.
				// Also, we want to weight latest values as more important than historic values.
				var scale = 1.0 / (idx.Count + 1);
				for (int j = idx.Begi, jend = idx.Endi+1; j != jend; ++j)
				{
					Window[j].Volume += candle.Volume * scale;
					Window[j].Time   += candle_time   * scale;

					max.Volume = Math.Max(max.Volume, Window[j].Volume);
					max.Time   = Math.Max(max.Time  , Window[j].Time);
				}
			}

			// Get the price range covered by the window
			var price0 = Window.Front().Price;
			var price1 = Window.Back().Price + BucketSize;
			var levels_range = BucketIndexRange(price0, price1, Levels, true);

			// Normalise each level value, and then average them into the main 'Levels' data
			for (int i = 0; i != Window.Count; ++i)
			{
				var j = i + levels_range.Begi;
				var level = Levels[j];
				Debug.Assert(level.Price == Window[i].Price);
				level.Median += Maths.Div(Window[i].Median, max.Median);//Avr.Running(Maths.Div(Window[i].Median, max.Median), level.Median, level.AvrCount);
				level.Volume += Maths.Div(Window[i].Volume, max.Volume);//Avr.Running(Maths.Div(Window[i].Volume, max.Volume), level.Volume, level.AvrCount);
				level.Time   += Maths.Div(Window[i].Time  , max.Time  );//Avr.Running(Maths.Div(Window[i].Time  , max.Time  ), level.Time  , level.AvrCount);
				//++level.AvrCount;
			}
		}

		/// <summary>Identify the support and resistance levels</summary>
		private void CalculateLevels()
		{
			var history_length = Math.Max(Settings.HistoryLength, Settings.WindowSize);
			var price = QuantisePrice(Instrument.Latest.Median);
			var window_size = Settings.WindowSize;

			// Reset the Levels data
			Levels.Clear();
			Levels.Add(new Bucket(price));

			// Request the full history length
			Instrument.RequestIndexRange(-history_length, 0, true);

			// Exclude the latest candle, cause it's not finished yet
			var range = Instrument.IndexRange(-history_length, 0);
			for (int i = range.Begi + window_size - 1; i != range.Endi; ++i)
				CalculateLevels(i, window_size);
		}

		/// <summary>Handle data added to the instrument</summary>
		protected override void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			Debug.Assert(e.Instrument.SymbolCode == Instrument.SymbolCode);

			// When a candle closes (signalled by a new candle
			// being created)add it to the levels data.
			if (e.NewCandle && Instrument.Count > 1)
				CalculateLevels(-1, Settings.WindowSize);
		}

		/// <summary>Update the graphics and chart when the settings change</summary>
		protected override void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			base.HandleSettingChanged(sender, e);

			// Reset the levels data when settings that affect the levels change
			if (e.Key == nameof(SnRSettings.SourceTimeFrame) ||
				e.Key == nameof(SnRSettings.HistoryLength) ||
				e.Key == nameof(SnRSettings.WindowSize))
				CalculateLevels();
		}

		/// <summary>A single price level (bucket)</summary>
		[DebuggerDisplay("{Price}  {Median}  {Volume}  {Time}")]
		private class Bucket
		{
			public Bucket(double price)
			{
				Price    = price;
				Median   = 0;
				Volume   = 0;
				Time     = 0;
				AvrCount = 0;
			}

			/// <summary>The central price value of this bucket</summary>
			public double Price { get; set; }

			/// <summary>The number of candle median prices that fall within this bucket</summary>
			public double Median { get; set; }

			/// <summary>The volume of trades that occurred within this bucket</summary>
			public double Volume { get; set; }

			/// <summary>The amount of time (in ticks) the price spent within this bucket price range</summary>
			public double Time { get; set; }

			/// <summary>The number of times data has been added to this level (used for averaging)</summary>
			public uint AvrCount { get; set; }

			/// <summary>Returns true if the contained values are zero</summary>
			public bool IsZero
			{
				get
				{
					return
						Maths.FEql(Median, 0.0) &&
						Maths.FEql(Volume, 0.0) &&
						Maths.FEql(Time  , 0.0);
				}
			}
		}
	}

	#region Settings

	/// <summary>Settings for Support and Resistance</summary>
	[TypeConverter(typeof(TyConv))]
	public class SnRSettings :SettingsXml<SnRSettings>
	{
		public SnRSettings()
		{
			Attribute       = SnR.EAttribute.MedianCount;
			SourceTimeFrame = ETimeFrame.Min10;
			PipsPerBucket   = 5;
			HistoryLength   = 1000;
			WindowSize      = 50;
			Colour          = Color.LightBlue;
			GraphicsWidth   = 0.2f;
			Power           = 1.0f;
		}

		/// <summary>The property to base SnR levels on</summary>
		public SnR.EAttribute Attribute
		{
			get { return get(x => x.Attribute); }
			set { set(x => x.Attribute, value); }
		}

		/// <summary>The time frame to use for the source data</summary>
		public ETimeFrame SourceTimeFrame
		{
			get { return get(x => x.SourceTimeFrame); }
			set { set(x => x.SourceTimeFrame, value); }
		}

		/// <summary>The resolution of the SnR levels</summary>
		public int PipsPerBucket
		{
			get { return get(x => x.PipsPerBucket); }
			set { set(x => x.PipsPerBucket, value); }
		}

		/// <summary>The number of candles to include</summary>
		public int HistoryLength
		{
			get { return get(x => x.HistoryLength); }
			set { set(x => x.HistoryLength, value); }
		}
		public const int MaxHistoryLength = 100000;

		/// <summary></summary>
		public int WindowSize
		{
			get { return get(x => x.WindowSize); }
			set { set(x => x.WindowSize, value); }
		}
		public const int MaxWindowSize = 100;

		/// <summary>Colour of the SnR graphics</summary>
		public Color Colour
		{
			get { return get(x => x.Colour); }
			set { set(x => x.Colour, value); }
		}

		/// <summary>How far across the chart the graphics stretch</summary>
		public float GraphicsWidth
		{
			get { return get(x => x.GraphicsWidth); }
			set { set(x => x.GraphicsWidth, value); }
		}

		/// <summary>How much to saturate the SnR levels</summary>
		public float Power
		{
			get { return get(x => x.Power); }
			set { set(x => x.Power, value); }
		}

		private class TyConv :GenericTypeConverter<SnRSettings> {}
	}

	#endregion
}
