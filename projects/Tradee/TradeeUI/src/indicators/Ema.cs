using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.ldr;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class EmaIndicator :IndicatorBase
	{
		public EmaIndicator(EmaSettings settings = null)
			:base(Guid.NewGuid(), "EMA", settings ?? new EmaSettings())
		{
			m_ema = new List<EmaPt>();
		}
		public EmaIndicator(XElement node)
			:base(node)
		{
			m_ema = new List<EmaPt>();
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
				Chart.XAxis.Scroll -= Invalidate;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.XAxis.Zoomed += Invalidate;
				Chart.XAxis.Scroll += Invalidate;
			}
		}
		protected override void SetInstrumentCore(Instrument instr)
		{
			base.SetInstrumentCore(instr);
			if (Instrument != null)
				Reset();
		}
		protected override void UpdateGfxCore()
		{
			// Generate the EMA line for the visible area of the chart
			var ema_colour = Settings.ColourEMA.ToArgbU();
			var bol_colour = Settings.ColourBollingerBands.ToArgbU();

			// Get the visible X axis range, and convert it to positive indices
			var first_idx = Instrument.FirstIdx;
			var rng = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
			rng.Begin = Maths.Clamp(rng.Begin - first_idx, 0, m_ema.Count);
			rng.End   = Maths.Clamp(rng.End   - first_idx, 0, m_ema.Count);
			if (rng.Counti == 0)
			{
				Gfx = null;
				return;
			}

			var count = rng.Counti;
			m_vbuf.Resize(count);
			m_ibuf.Resize((count - 1) * 2);
			m_nbuf.Resize(1);

			// EMA model
			var v = 0; var i = 0;
			foreach (var candle_index in rng)
			{
				var ema = m_ema[(int)candle_index];
				m_vbuf[v++] = new View3d.Vertex(new v4(candle_index + first_idx, (float)ema.Mean, ChartUI.Z.Indicators, 1f), ema_colour);
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
				foreach (var candle_index in rng)
				{
					var ema = m_ema[(int)candle_index];
					m_vbuf[v++] = new View3d.Vertex(new v4(candle_index + first_idx, (float)(ema.Mean - Settings.BollingerBands * ema.PopStdDev), ChartUI.Z.Indicators, 1f), bol_colour);
					m_ibuf[i++] = (ushort)(v-1);
				}
				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
				foreach (var candle_index in rng)
				{
					var ema = m_ema[(int)candle_index];
					m_vbuf[v++] = new View3d.Vertex(new v4(candle_index + first_idx, (float)(ema.Mean + Settings.BollingerBands * ema.PopStdDev), ChartUI.Z.Indicators, 1f), bol_colour);
					m_ibuf[i++] = (ushort)(v-1);
				}
				m_nbuf[2] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
			}

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx != null)
				window.AddObject(Gfx);
		}

		/// <summary>Settings for this indicator</summary>
		public EmaSettings Settings
		{
			[DebuggerStepThrough] get { return (EmaSettings)IndicatorSettingsInternal; }
		}

		/// <summary>The number of values in this EMA data</summary>
		public int Count
		{
			get { return m_ema.Count; }
		}

		/// <summary>Access this object as an array of EMA data using negative indices (i.e. where 0 = latest)</summary>
		public double this[int idx]
		{
			get
			{
				Debug.Assert(Instrument.Count == m_ema.Count);
				Debug.Assert(idx >= Instrument.FirstIdx && idx < Instrument.LastIdx);

				idx -= Instrument.FirstIdx;
				return m_ema[idx].Mean;
			}
		}
		private List<EmaPt> m_ema;

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

		/// <summary>Calculate the EMA</summary>
		private void Reset()
		{
			m_ema.Clear();
			m_ema.Capacity = Instrument.Count;

			var ema = new EmaPt((uint)Settings.WindowSize);
			foreach (var candle in Instrument)
			{
				// Candle data must be strictly ordered by timestamp because I'm using binary search
				Debug.Assert(candle.Timestamp >= ema.Timestamp);

				// Calculate the EMA value and save it
				ema.Add(candle.Close);
				ema.Timestamp = candle.Timestamp;
				m_ema.Add(ema);

				// Create the next EMA value
				ema = new EmaPt(ema);
			}

			// Invalidate the graphics
			Invalidate();
			InvalidateChart();
		}

		/// <summary>Handle the instrument time frame changing</summary>
		protected override void HandleTimeFrameChanged(object sender, EventArgs e)
		{
			// Reset the moving average to use the new time frame data
			Reset();
		}

		/// <summary>Handle data added to the instrument</summary>
		protected override void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			// Check the data is for this instrument
			Debug.Assert(Instrument == e.Instrument);
			if (e.TimeFrame != Instrument.TimeFrame)
				return;

			// If this is a new candle, add to the EMA collection
			if (e.NewCandle)
			{
				var ema = m_ema.Count != 0 ? new EmaPt(m_ema.Back()) : new EmaPt((uint)Settings.WindowSize);
				var prev = Instrument[-1];

				ema.Timestamp = prev.Timestamp;
				ema.Add(prev.Close);
				m_ema.Add(ema);
			}

			// Otherwise, if this is an update to an existing candle,
			// recalculate the EMA for this candle and all following candles
			else if (e.Candle != null)
			{
				// Find the index of the corresponding EMA value
				var idx = m_ema.BinarySearch(x => x.Timestamp.CompareTo(e.Candle.Timestamp));
				if (idx < 0) idx = ~idx;

				// Recalculate all EMA values from 'idx' forwards
				m_ema.Resize(idx);
				var prev = idx > 0 ? m_ema[idx-1] : new EmaPt((uint)Settings.WindowSize);
				for (; idx != Instrument.Count; ++idx)
				{
					var ema = new EmaPt(prev);
					var candle = Instrument[(uint)idx];
					ema.Timestamp = candle.Timestamp;
					ema.Add(candle.Close);
					m_ema.Add(ema);

					prev = ema;
				}
			}
		}

		/// <summary>Update the graphics and chart when the settings change</summary>
		protected override void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			base.HandleSettingChanged(sender, e);
		
			if (e.Key == nameof(EmaSettings.WindowSize))
			{
				Name = "EMA_{0}".Fmt(Settings.WindowSize);
				Reset();
			}
		}

		/// <summary>Extend the EMA type to include a timestamp</summary>
		private class EmaPt :ExpMovingAvr
		{
			public EmaPt(uint window_size)
				:base(window_size)
			{}
			public EmaPt(EmaPt rhs)
				:base(rhs)
			{
				Timestamp = rhs.Timestamp;
			}

			/// <summary>The candle timestamp that this EMA data corresponds to</summary>
			public long Timestamp
			{
				get;
				set;
			}
		}
	}

	#region Settings

	/// <summary>Settings for EMAs</summary>
	[TypeConverter(typeof(TyConv))]
	public class EmaSettings :SettingsXml<EmaSettings>
	{
		public EmaSettings()
		{
			Width                = 5;
			WindowSize           = 14;
			ColourEMA            = Color.LightGreen;
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

		/// <summary>The line colour</summary>
		public Color ColourEMA
		{
			get { return get(x => x.ColourEMA); }
			set { set(x => x.ColourEMA, value); }
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

		private class TyConv :GenericTypeConverter<EmaSettings> {}
	}

	#endregion
}
