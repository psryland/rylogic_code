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
			:base(Guid.NewGuid(), "MA", settings ?? new SettingsData())
		{
			m_ma = new List<MAPoint>();
		}
		public IndicatorMA(XElement node)
			:base(node)
		{
			m_ma = new List<MAPoint>();
		}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}

		/// <summary>Settings for this indicator</summary>
		public SettingsData Settings
		{
			[DebuggerStepThrough] get { return (SettingsData)SettingsInternal; }
		}

		/// <summary>The graphics for the indicator</summary>
		public View3d.Object Gfx
		{
			[DebuggerStepThrough] get { return m_gfx; }
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;
		private Range m_gfx_range;

		/// <summary>Calculate the MA</summary>
		protected override void ResetCore()
		{
			m_ma.Clear();

			if (Instrument != null)
			{
				// Create a moving average stat that the instrument data is added to.
				var avr = Settings.ExponentialMA
					? (object)new ExpMovingAvr(Settings.WindowSize)
					: (object)new MovingAvr(Settings.WindowSize);

				// Ensure the visible range is cached (plus a bit)
				var xmin = (int)Maths.Clamp(Chart.XAxis.Min - Chart.XAxis.Span, 0, Instrument.Count-1);
				var xmax = (int)Maths.Clamp(Chart.XAxis.Max + Chart.XAxis.Span, 0, Instrument.Count-1);
				Instrument.EnsureCached(xmin);
				Instrument.EnsureCached(xmax);

				// Load the
				var ma = new List<MAPoint>(xmax - xmin);
				var stat = (IStatSingleVariable)avr;
				var value = (IStatMeanAndVariance)avr;
				var ts = DateTimeOffset.MinValue.Ticks;
				for (var i = xmin; i != xmax; ++i)
				{
					var candle = Instrument[i];

					// Candle data must be strictly ordered by timestamp because I'm using binary search
					Debug.Assert(candle.Timestamp >= ts);
					ts = candle.Timestamp;

					// Calculate the MA value and save it
					stat.Add(candle.Close);
					ma.Add(new MAPoint(ts, value.Mean, value.PopStdDev));
				}
				
				m_ma = ma;
			}
		}

		/// <summary>Handle settings changing</summary>
		protected override void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			// If the setting affects the underlying data, reset
			switch (e.Key) {
			case nameof(SettingsData.ExponentialMA):
			case nameof(SettingsData.WindowSize):
				ResetRequired = true;
				break;
			}

			base.HandleSettingChanged(sender, e);
		}

		/// <summary>Invalidate if the chart moves beyond the range of the graphics</summary>
		protected override void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
		{
			base.HandleChartMoved(sender, e);

			// Get the visible X axis range
			var rng = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
			rng.Beg = Maths.Clamp(rng.Beg, 0, m_ma.Count);
			rng.End = Maths.Clamp(rng.End, 0, m_ma.Count);

			// Check that the graphics model spans the required range
			if ((m_gfx_range.Beg != 0 && rng.Beg < m_gfx_range.Beg) ||
				(m_gfx_range.End != m_ma.Count && rng.End > m_gfx_range.End))
				Invalidate();

			// IF the graphics are way bigger than necessary, recreate to free up memory
			if (m_gfx_range.Count > 3 * rng.Count)
				Invalidate();
		}

		/// <summary>Update the graphics model for this indicator</summary>
		protected override void UpdateGfxCore()
		{
			base.UpdateGfxCore();

			// Get the visible X axis range
			var rng = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
			rng.Beg = Maths.Clamp(rng.Beg, 0, m_ma.Count);
			rng.End = Maths.Clamp(rng.End, 0, m_ma.Count);
			if (rng.Counti == 0)
			{
				// Indicator isn't visible
				Gfx = null;
				return;
			}

			// Resize the cache buffers
			var count = rng.Counti;
			m_vbuf.Resize(count);
			m_ibuf.Resize((count - 1) * 2);
			m_nbuf.Resize(1);

			// Get the colours for the MA line
			var ma_colour = Settings.ColourMA.ToArgbU();
			var bol_colour = Settings.ColourBollingerBands.ToArgbU();

			// Create the MA model
			var v = 0;
			var i = 0;
			foreach (var candle_index in rng.Enumeratei)
			{
				m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)m_ma[candle_index].Value, ZOrder.Indicators, 1f), ma_colour);
			}
			for (var vi = 0; i != m_ibuf.Count; ++vi)
			{
				m_ibuf[i++] = (ushort)(vi  );
				m_ibuf[i++] = (ushort)(vi+1);
			}
			var mat = new View3d.Material(shader:View3d.EShader.ThickLineListGS, shader_data:new[] {Settings.Width,0,0,0});
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, (uint)v, 0, (uint)i, false, mat);

			// Add geometry for Bollinger bands
			if (Settings.ShowBollingerBands && Settings.BollingerBandsStdDev != 0)
			{
				m_vbuf.Resize(m_vbuf.Count + 2*count);
				m_ibuf.Resize(m_ibuf.Count + 2*count);
				m_nbuf.Resize(m_nbuf.Count + 2);

				// Lower/Upper band
				foreach (var candle_index in rng.Enumeratei)
				{
					var ma = m_ma[candle_index];
					m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)(ma.Value - Settings.BollingerBandsStdDev * ma.StdDev), ZOrder.Indicators, 1f), bol_colour);
					m_ibuf[i++] = (ushort)(v-1);
				}
				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
				foreach (var candle_index in rng.Enumeratei)
				{
					var ma = m_ma[candle_index];
					m_vbuf[v++] = new View3d.Vertex(new v4(candle_index, (float)(ma.Value + Settings.BollingerBandsStdDev * ma.StdDev), ZOrder.Indicators, 1f), bol_colour);
					m_ibuf[i++] = (ushort)(v-1);
				}
				m_nbuf[2] = new View3d.Nugget(View3d.EPrim.LineStrip, View3d.EGeom.Vert|View3d.EGeom.Colr, (uint)(v-count), (uint)v, (uint)(i-count), (uint)i);
			}

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			// Record the range that the graphics covers
			m_gfx_range = rng;
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		protected override void AddToSceneCore(View3d.Window window)
		{
			base.AddToSceneCore(window);

			// Add to the scene
			if (Gfx != null)
			{
				Gfx.O2P = m4x4.Translation(Settings.XOffset, 0f, 0f);
				window.AddObject(Gfx);
			}
		}

		/// <summary>Hit test this indicator</summary>
		public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.Camera cam)
		{
			// Test the distance to the MA over the selection distance
			var dist_sq = Maths.Sqr(Chart.Options.MinSelectionDistance);
			var xbeg = (int)Maths.Clamp(chart_point.X - Chart.Options.MinSelectionDistance/2f, 0, m_ma.Count);
			var xend = (int)Maths.Clamp(chart_point.X + Chart.Options.MinSelectionDistance/2f, 0, m_ma.Count);
			var yofs = Settings.ShowBollingerBands && Settings.BollingerBandsStdDev != 0
				? new [] { 0f, -Settings.BollingerBandsStdDev, +Settings.BollingerBandsStdDev }
				: new [] { 0f };

			for (var x = xbeg; x != xend; ++x)
			{
				var ma = m_ma[x];
				foreach (var y in yofs)
				{
					var pt = Chart.ChartToClient(new PointF(x, (float)(ma.Value + y * ma.StdDev)));
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
				ShowBollingerBands   = false;
				BollingerBandsStdDev = 2f;
				ColourBollingerBands = Color.Purple;
				XOffset              = 0f;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

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

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
		#endregion
	}
}
