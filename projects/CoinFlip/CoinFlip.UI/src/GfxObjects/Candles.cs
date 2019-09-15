using System;
using System.Collections.Generic;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class Candles :Buffers
	{
		private const int BatchSize = 1024;

		public Candles(Instrument instrument)
		{
			Cache = new ChartGfxCache(CreatePiece);
			Instrument = instrument;
		}
		public override void Dispose()
		{
			Instrument = null;
			Cache = null;
			base.Dispose();
		}

		/// <summary>The instrument for which candles are being created</summary>
		public Instrument Instrument
		{
			get => m_instrument;
			private set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
				}
				m_instrument = value;
				if (m_instrument != null)
				{
				}
			}
		}
		private Instrument m_instrument;

		/// <summary>A cache of candle graphics objects</summary>
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

		/// <summary>Add candles to the scene</summary>
		public void BuildScene(ChartControl chart)
		{
			// Convert the XAxis values into an index range.
			// (indices, not time frame units, because of the gaps in the price data).
			var range = Instrument.IndexRange((int)(chart.XAxis.Min - 1), (int)(chart.XAxis.Max + 1));

			// Add the candles that cover 'range'
			foreach (var gfx in Cache.Get(range).OfType<CandleGfx>())
			{
				if (gfx.Gfx == null) continue;
				gfx.Gfx.O2P = m4x4.Translation(new v4((float)gfx.Range.Beg, 0.0f, CandleChart.ZOrder.Candles, 1.0f));
				chart.Scene.Window.AddObject(gfx.Gfx);
			}
		}

		/// <summary>Create standard candle graphics for the given range</summary>
		private IChartGfxPiece CreatePiece(double x_value, RangeF missing)
		{
			var db_idx_range = new Range(
				(long)(x_value / BatchSize + 0) * BatchSize,
				(long)(x_value / BatchSize + 1) * BatchSize);

			// Generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
			var colour_bullish = SettingsData.Settings.Chart.Q2BColour.ARGB;
			var colour_bearish = SettingsData.Settings.Chart.B2QColour.ARGB;

			// Get the series data over the time range specified
			var rng = Instrument.IndexRange(db_idx_range.Begi, db_idx_range.Endi);
			if (rng.Counti == 0)
				return new CandleGfx(null, rng);

			// Using TriList for the bodies, and LineList for the wicks.
			// So:    6 indices for the body, 4 for the wicks
			//   __|__
			//  |\    |
			//  |  \  |
			//  |____\|
			//     |
			// Dividing the index buffer into [bodies, wicks]
			var candles = Instrument.CandleRange(rng.Begi, rng.Endi);
			var count = rng.Counti;

			// Resize the cache buffers
			m_vbuf.Resize(8 * count);
			m_ibuf.Resize((6 + 4) * count);
			m_nbuf.Resize(2);

			// Index of the first body index and the first wick index.
			var vert = 0;
			var body = 0;
			var wick = 6 * count;
			var nugt = 0;

			// Create the graphics with the first candle at x == 0
			var candle_idx = 0;

			// Create the geometry
			foreach (var candle in candles)
			{
				// Use the spot price for the close of the 'Latest' candle.
				var candle_close = candle.Close;
				if (rng.Begi + candle_idx == Instrument.Count - 1)
				{
					var spot = Instrument.SpotPrice(ETradeType.Q2B);
					if (spot != null) candle_close = spot.Value.ToDouble();
				}

				var open  = candle.Open;
				var high  = candle.High;
				var low   = candle.Low;
				var close = candle_close;

				var col = close > open ? colour_bullish : close < open ? colour_bearish : 0xFFA0A0A0;
				var x = (float)candle_idx++;
				var o = (float)Math.Max(open, close);
				var h = (float)high;
				var l = (float)low;
				var c = (float)Math.Min(open, close);
				var v = vert;

				// Candle verts
				m_vbuf[vert++] = new View3d.Vertex(new v4(x, h, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x, o, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f, o, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f, o, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x - 0.4f, c, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x + 0.4f, c, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x, c, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(x, l, 0f, 1f), col);

				// Candle body
				m_ibuf[body++] = (ushort)(v + 3);
				m_ibuf[body++] = (ushort)(v + 2);
				m_ibuf[body++] = (ushort)(v + 4);
				m_ibuf[body++] = (ushort)(v + 4);
				m_ibuf[body++] = (ushort)(v + 5);
				m_ibuf[body++] = (ushort)(v + 3);

				// Candle wick
				if (o != c)
				{
					m_ibuf[wick++] = (ushort)(v + 0);
					m_ibuf[wick++] = (ushort)(v + 1);
					m_ibuf[wick++] = (ushort)(v + 6);
					m_ibuf[wick++] = (ushort)(v + 7);
				}
				else
				{
					m_ibuf[wick++] = (ushort)(v + 0);
					m_ibuf[wick++] = (ushort)(v + 7);
					m_ibuf[wick++] = (ushort)(v + 2);
					m_ibuf[wick++] = (ushort)(v + 5);
				}
			}

			m_nbuf[nugt++] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)body);
			m_nbuf[nugt++] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, (uint)body, (uint)wick);

			// Create the graphics
			var gfx = new View3d.Object($"Candles-[{db_idx_range.Begi},{db_idx_range.Endi})", 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), CandleChart.CtxId);
			return new CandleGfx(gfx, db_idx_range);
		}

		/// <summary>Invalidate the candle graphics</summary>
		public void Invalidate()
		{
			Cache.Invalidate();
		}
		public void Invalidate(int candle_index)
		{
			Cache.Invalidate(candle_index);
		}
		public void Invalidate(Range candle_index_range)
		{
			Cache.Invalidate(candle_index_range);
		}

		/// <summary>Graphics object for a batch of candles</summary>
		private class CandleGfx :IChartGfxPiece
		{
			public CandleGfx(View3d.Object gfx, Range db_index_range)
			{
				Gfx = gfx;
				Range = db_index_range;
			}
			public void Dispose()
			{
				Util.Dispose(Gfx);
			}

			/// <summary>The graphics object containing 'GfxModelBatchSize' candles</summary>
			public View3d.Object Gfx { get; }

			/// <summary>The index range of candles in this graphic</summary>
			public RangeF Range { get; }
		}
	}
}
