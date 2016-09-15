using System;
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

namespace Tradee
{
	public class TrendStrength :IndicatorBase
	{
		public TrendStrength(TrendStrengthSettings settings = null)
			:base(Guid.NewGuid(), "Trend Strength", settings ?? new TrendStrengthSettings())
		{
			m_trend = new List<double>();
		}
		public TrendStrength(XElement node)
			:base(node)
		{
			m_trend = new List<double>();
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
			// Generate the line plot for the visible area of the chart
			
			// Get the visible X axis range, and convert it to positive indices
			var first_idx = Instrument.FirstIdx;
			var rng = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
			rng.Begin = Maths.Clamp(rng.Begin - first_idx, 0, m_trend.Count);
			rng.End   = Maths.Clamp(rng.End   - first_idx, 0, m_trend.Count);
			if (rng.Counti == 0)
			{
				Gfx = null;
				return;
			}

			m_vbuf.Clear();
			m_ibuf.Clear();
			m_nbuf.Clear();

			var bullish_colour = Settings.BullishColour.ToArgbU();
			var bearish_colour = Settings.BearishColour.ToArgbU();
			var y_scale = Chart.YAxis.Span * Settings.Scale;
			for (var i = rng.Begini; i < rng.Endi - 1; ++i)
			{
				var trend0 = m_trend[i  ];
				var trend1 = m_trend[i+1];
				m_vbuf.Add(new View3d.Vertex(new v4(first_idx + i + 0, (float)Math.Abs(trend0 * y_scale), 0, 1f), trend0 >= 0 ? bullish_colour : bearish_colour));
				m_vbuf.Add(new View3d.Vertex(new v4(first_idx + i + 1, (float)Math.Abs(trend1 * y_scale), 0, 1f), trend0 >= 0 ? bullish_colour : bearish_colour));
				m_ibuf.Add((ushort)(m_vbuf.Count - 2));
				m_ibuf.Add((ushort)(m_vbuf.Count - 1));
			}
			var mat = new View3d.Material(shader:View3d.EShader.ThickLineListGS, shader_data:new[] {Settings.Width,0,0,0});
			m_nbuf.Add(new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)m_vbuf.Count, 0, (uint)m_ibuf.Count, false, mat));

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx == null) return;
			Gfx.O2P = m4x4.Translation(0f, (float)Chart.YAxis.Min, ChartUI.Z.Indicators);
			window.AddObject(Gfx);
		}

		/// <summary>Settings for this indicator</summary>
		public TrendStrengthSettings Settings
		{
			[DebuggerStepThrough] get { return (TrendStrengthSettings)IndicatorSettingsInternal; }
		}

		/// <summary>The number of values in this EMA data</summary>
		public int Count
		{
			get { return m_trend.Count; }
		}

		/// <summary>Access this object as an array of EMA data using negative indices (i.e. where 0 = latest)</summary>
		public double this[int idx]
		{
			get
			{
				Debug.Assert(Instrument.Count == m_trend.Count);
				Debug.Assert(idx >= Instrument.FirstIdx && idx < Instrument.LastIdx);

				idx -= Instrument.FirstIdx;
				return m_trend[idx];
			}
		}
		private List<double> m_trend;

		/// <summary>The plot graphics</summary>
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

		/// <summary>Calculate the Trend strength</summary>
		private void Reset()
		{
			m_trend.Clear();
			m_trend.Capacity = Instrument.Count;

			var window_size = Settings.WindowSize;
			for (int i = Instrument.FirstIdx, iend = Instrument.LastIdx; i < iend; ++i)
			{
				var trend = Instrument.MeasureTrend(i - window_size, window_size);
				m_trend.Add(trend);
			}

			// Invalidate the graphics
			Invalidate();
			InvalidateChart();
		}

		/// <summary>Handle data added to the instrument</summary>
		protected override void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			// Check the data is for this instrument
			Debug.Assert(Instrument == e.Instrument);
			if (e.TimeFrame != Instrument.TimeFrame)
				return;

			// If this is a new candle, add to the collection
			if (e.NewCandle)
			{
				var i = Instrument.LastIdx - Settings.WindowSize;
				m_trend.Add(Instrument.MeasureTrend(i, Settings.WindowSize));
			}

			// Otherwise, if this is an update to an existing candle,
			// recalculate the trend for all affected candles
			else if (e.Candle != null)
			{
				Reset(); //hack

				// Find the index of the corresponding value
				//var idx = Instrument.IndexAt(new TFTime(e.Candle.TimestampUTC, Instrument.TimeFrame));
				
				//// Recalculate all EMA values from 'idx' forwards
				//m_ema.Resize(idx);
				//var prev = idx > 0 ? m_ema[idx-1] : new EmaPt((uint)Settings.WindowSize);
				//for (; idx != Instrument.Count; ++idx)
				//{
				//	var ema = new EmaPt(prev);
				//	var candle = Instrument[(uint)idx];
				//	ema.Timestamp = candle.Timestamp;
				//	ema.Add(candle.Close);
				//	m_ema.Add(ema);

				//	prev = ema;
				//}
			}
		}
	}

	#region Settings

	/// <summary>Settings for EMAs</summary>
	[TypeConverter(typeof(TyConv))]
	public class TrendStrengthSettings :SettingsXml<TrendStrengthSettings>
	{
		public TrendStrengthSettings()
		{
			WindowSize    = 5;
			Width         = 5;
			Scale         = 0.2f;
			BullishColour = Color.DarkGreen;
			BearishColour = Color.DarkRed;
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

		/// <summary>The fraction of the vertical size of the chart to draw on</summary>
		public float Scale
		{
			get { return get(x => x.Scale); }
			set { set(x => x.Scale, Maths.Clamp(value, 0f, 1f)); }
		}

		/// <summary>The line colour</summary>
		public Color BullishColour
		{
			get { return get(x => x.BullishColour); }
			set { set(x => x.BullishColour, value); }
		}

		/// <summary>The line colour</summary>
		public Color BearishColour
		{
			get { return get(x => x.BearishColour); }
			set { set(x => x.BearishColour, value); }
		}

		private class TyConv :GenericTypeConverter<TrendStrengthSettings> {}
	}

	#endregion
}
