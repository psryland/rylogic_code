using System;
using System.Collections.Generic;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class Candles :Buffers
	{
		private const int BatchSize = 1024;

		public Candles(Instrument instrument)
		{
			Cache = new Cache<int, CandleGfx>(100);
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
			get { return m_instrument; }
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
		private Cache<int, CandleGfx> Cache
		{
			get { return m_cache; }
			set
			{
				if (m_cache == value) return;
				Util.Dispose(ref m_cache);
				m_cache = value;
			}
		}
		private Cache<int, CandleGfx> m_cache;

		/// <summary>Returns the candles graphics model containing the data point with index 'cache_idx'</summary>
		private CandleGfx At(int cache_idx)
		{
			// On miss, generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
			return m_cache.Get(cache_idx, i =>
			{
				var colour_bullish = SettingsData.Settings.Chart.Q2BColour.ARGB;
				var colour_bearish = SettingsData.Settings.Chart.B2QColour.ARGB;
				var db_idx_range = new Range((i + 0) * BatchSize, (i + 1) * BatchSize);

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

				// Create the geometry
				var candle_idx = 0;
				foreach (var candle in candles)
				{
					// Use the spot price for the close of the 'Latest' candle.
					var close = candle.Close;
					if (rng.Begi + candle_idx == Instrument.Count - 1)
					{
						var spot = Instrument.SpotPrice(ETradeType.Q2B);
						if (spot != null) close = (double)(decimal)spot.Value;
					}

					// Create the graphics with the first candle at x == 0
					var x = (float)candle_idx++;
					var o = (float)Math.Max(candle.Open, close);
					var h = (float)candle.High;
					var l = (float)candle.Low;
					var c = (float)Math.Min(candle.Open, close);
					var col = close > candle.Open ? colour_bullish : close < candle.Open ? colour_bearish : 0xFFA0A0A0;
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
			});
		}

		/// <summary>Return graphics objects for the candles over the given range</summary>
		public IEnumerable<View3d.Object> Get(Range candle_range)
		{
			// Convert the index range into a cache index range
			var cache0 = candle_range.Begi / BatchSize;
			var cache1 = candle_range.Endi / BatchSize;
			for (int i = cache0; i <= cache1; ++i)
			{
				// Get the graphics model that contains candle 'i'
				var gfx = At(i);
				if (gfx.Gfx != null)
				{
					// Position the graphics object
					gfx.Gfx.O2P = m4x4.Translation(new v4(gfx.DBIndexRange.Begi, 0.0f, CandleChart.ZOrder.Candles, 1.0f));
					yield return gfx.Gfx;
				}
			}
		}

		/// <summary>Invalidate the graphics object that contains the candle at 'candle_index'</summary>
		public void Invalidate(int candle_index)
		{
			var cache_idx = candle_index / BatchSize;
			m_cache.Invalidate(cache_idx);
		}
		public void Invalidate(Range candle_index_range)
		{
			var idx_beg = candle_index_range.Begi / BatchSize;
			var idx_end = candle_index_range.Endi / BatchSize;
			for (var i = idx_beg; i <= idx_end; ++i)
				m_cache.Invalidate(i);
		}

		/// <summary>Invalidate all candle graphics</summary>
		public void Flush()
		{
			m_cache.Flush();
		}

		/// <summary>Graphics object for a batch of candles</summary>
		private class CandleGfx : IDisposable
		{
			public CandleGfx(View3d.Object gfx, Range db_index_range)
			{
				Gfx = gfx;
				DBIndexRange = db_index_range;
			}
			public void Dispose()
			{
				Gfx = Util.Dispose(Gfx);
				DBIndexRange = Range.Zero;
			}

			/// <summary>The graphics object containing 'GfxModelBatchSize' candles</summary>
			public View3d.Object Gfx { get; private set; }

			/// <summary>The index range of candles in this graphic</summary>
			public Range DBIndexRange { get; private set; }
		}
	}
}
