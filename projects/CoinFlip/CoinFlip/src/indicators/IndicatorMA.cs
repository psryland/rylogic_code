using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
	public class IndicatorMA :Indicator ,IEnumerable<IndicatorMA.MAPoint>
	{
		/// <summary>The moving average data. Each point is the average for one candle</summary>
		private List<MAPoint> m_ma;

		public IndicatorMA(Instrument instrument, SettingsData settings = null)
			:base("MA", settings ?? new SettingsData())
		{
			m_ma = new List<MAPoint>();
			Instrument = instrument;
		}
		public IndicatorMA(Instrument instrument, XElement node)
			:base(node)
		{
			m_ma = new List<MAPoint>();
			Instrument = instrument;
		}
		public override void Dispose()
		{
			Gfx = null;
			base.Dispose();
		}

		/// <summary>Settings for this indicator</summary>
		public SettingsData Settings
		{
			[DebuggerStepThrough] get { return (SettingsData)SettingsInternal; }
		}

		/// <summary>The graphics for the indicator</summary>
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

		/// <summary>Instrument assigned</summary>
		protected override void SetInstrumentCore(Instrument instr)
		{
			if (Instrument != null)
			{
			}
			base.SetInstrumentCore(instr);
			if (Instrument != null)
			{
			}
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		public override void AddToScene(ChartControl.ChartRenderingEventArgs args)
		{
			base.AddToScene(args);

			// Create the indicator graphics
			if (Gfx == null || Dirty.HasFlag(EDirtyFlags.RecreateModel))
			{
				var chart = args.Chart;

				// Get the colours for the MA line
				var ma_colour = Settings.ColourMA.ToArgbU();
				var bol_colour = Settings.ColourBollingerBands.ToArgbU();

				// Get the visible X axis range
				var rng = Instrument.IndexRange((int)(chart.XAxis.Min - 1), (int)(chart.XAxis.Max + 1));
				rng.Beg = Maths.Clamp(rng.Beg, 0, m_ma.Count);
				rng.End = Maths.Clamp(rng.End, 0, m_ma.Count);
				if (rng.Counti == 0)
				{
					Gfx = null;
					return;
				}

				// Resize the cache buffers
				var count = rng.Counti;
				m_vbuf.Resize(count);
				m_ibuf.Resize((count - 1) * 2);
				m_nbuf.Resize(1);

				// Create the MA model
				var v = 0;
				var i = 0;
				foreach (var candle_index in rng.Enumeratei)
				{
					m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)m_ma[candle_index].Value, ChartUI.Z.Indicators, 1f), ma_colour);
				}
				for (var vi = 0; i != m_ibuf.Count; ++vi)
				{
					m_ibuf[i++] = (ushort)(vi  );
					m_ibuf[i++] = (ushort)(vi+1);
				}
				var mat = new View3d.Material(shader:View3d.EShader.ThickLineListGS, shader_data:new[] {Settings.Width,0,0,0});
				m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)v, 0, (uint)i, false, mat);

				// Add geometry for Bollinger bands
				if (Settings.BollingerBands != 0)
				{
					m_vbuf.Resize(m_vbuf.Count + 2*count);
					m_ibuf.Resize(m_ibuf.Count + 2*count);
					m_nbuf.Resize(m_nbuf.Count + 2);

					// Lower/Upper band
					foreach (var candle_index in rng.Enumeratei)
					{
						var ma = m_ma[candle_index];
						m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)(ma.Value - Settings.BollingerBands * ma.StdDev), ChartUI.Z.Indicators, 1f), bol_colour);
						m_ibuf[i++] = (ushort)(v-1);
					}
					m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
					foreach (var candle_index in rng.Enumeratei)
					{
						var ma = m_ma[candle_index];
						m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)(ma.Value + Settings.BollingerBands * ma.StdDev), ChartUI.Z.Indicators, 1f), bol_colour);
						m_ibuf[i++] = (ushort)(v-1);
					}
					m_nbuf[2] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
				}

				// Create the graphics
				Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

				// Clear the dirty flag
				Dirty = Bit.SetBits(Dirty, EDirtyFlags.RecreateModel, false);
			}

			args.AddToScene(Gfx);
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

		/// <summary>Enumerable moving average points</summary>
		public IEnumerator<MAPoint> GetEnumerator()
		{
			return m_ma.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Calculate the MA</summary>
		private void Reset()
		{
			m_ma.Clear();
			m_ma.Capacity = Instrument.Count;

			// Create a moving average stat that the instrument data is added to.
			// Take of a copy of it at each candle.
			var ma = Settings.ExponentialMA
				? (IMA)new ExpMovingAvr(Settings.WindowSize)
				: (IMA)new MovingAvr(Settings.WindowSize);

			var ts = DateTimeOffset.MinValue.Ticks;
			foreach (var candle in Instrument)
			{
				// Candle data must be strictly ordered by timestamp because I'm using binary search
				Debug.Assert(candle.Timestamp >= ts);
				ts = candle.Timestamp;

				// Calculate the MA value and save it
				ma.Add(candle.Close);
				m_ma.Add(new MAPoint(ts, ma.Mean, ma.PopStdDev));
			}

			// Invalidate the graphics
			Invalidate();
		}

		#region MA Point

		/// <summary>Interface for a moving average calculator</summary>
		private interface IMA :IStatMean, IStatVariance, IStatSingleVariable
		{}

		/// <summary>A single point in the MA curve, representing a single candle</summary>
		public class MAPoint
		{
			public MAPoint(long ts, double value, double std_dev)
			{
				Timestamp = ts;
				Value = value;
				StdDev = std_dev;
			}

			/// <summary>The candle timestamp that this MA point corresponds to</summary>
			public long Timestamp { get; private set; }

			/// <summary>The value of the moving average at 'Timestamp'</summary>
			public double Value { get; private set; }

			/// <summary>The standard deviation of the moving average at 'Timestamp'</summary>
			public double StdDev { get; private set; }
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
				BollingerBands       = 2f;
				ColourBollingerBands = Color.Purple;
			}

			/// <summary>The window size of the EMA</summary>
			public int WindowSize
			{
				get { return get(x => x.WindowSize); }
				set { set(x => x.WindowSize, Maths.Clamp(value, 1, MaxWindowSize)); }
			}
			public const int MaxWindowSize = 500;

			/// <summary>The width of the line to draw</summary>
			public int Width
			{
				get { return get(x => x.Width); }
				set { set(x => x.Width, Maths.Clamp(value, 1, 50)); }
			}

			/// <summary>True to use an exponential moving average</summary>
			public bool ExponentialMA
			{
				get { return get(x => x.ExponentialMA); }
				set { set(x => x.ExponentialMA, value); }
			}

			/// <summary>The line colour</summary>
			public Color ColourMA
			{
				get { return get(x => x.ColourMA); }
				set { set(x => x.ColourMA, value); }
			}

			/// <summary>The Bollinger band size in units of the standard deviations (0 = turns them off)</summary>
			public float BollingerBands
			{
				get { return get(x => x.BollingerBands); }
				set { set(x => x.BollingerBands, Maths.Clamp(value, 0, 5.0f)); }
			}

			/// <summary>The line colour for the Bollinger Bands</summary>
			public Color ColourBollingerBands
			{
				get { return get(x => x.ColourBollingerBands); }
				set { set(x => x.ColourBollingerBands, value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
		#endregion
	}
}
