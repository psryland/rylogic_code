using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.ldr;
using pr.maths;
using pr.util;

namespace Rylobot
{
	public static class Debugging
	{
		// Notes;
		// Output using CAlgo index positions so that trades can have fixed positions

		private static readonly Colour32 AskColor = Colour32.Green;
		private static readonly Colour32 BidColor = Colour32.Red;
		private static readonly Colour32[] CandleTypeColour = new Colour32 []
		{
			Color_.FromArgb(0xFFA0A0A0), // Unknown

			Color_.FromArgb(0xFF80FF80), // Doji
			Color_.FromArgb(0xFF00FF00), // SpinningTop

			Color_.FromArgb(0xFF0080FF), // Hammer
			Color_.FromArgb(0xFF0080FF), // InvHammer

			Color_.FromArgb(0xFFA00000), // MarubozuWeakening
			Color_.FromArgb(0xFFA00000), // MarubozuStrengthening
			Color_.FromArgb(0xFFFF0000), // Marubozu
		};

		/// <summary>Full path for a dump file</summary>
		public static string FP(string fname)
		{
			const string Dir = @"P:\dump\forex";
			return Path_.CombinePath(Dir, fname);
		}

		/// <summary>Transform that scales Y axis to a decent size</summary>
		public static m4x4 ScaleTxfm
		{
			get { return m4x4.Scale(1f, 1000f, 1f, v4.Origin); }
		}

		/// <summary>Reset a file to empty</summary>
		public static void ClearFile(string fname)
		{
			new LdrBuilder().ToFile(FP(fname));
		}

		/// <summary>Map from position id to ldr builder for that position</summary>
		private static List<int> m_trade_ids = new List<int>();
		private static LdrBuilder m_ldr_all_trades;
		private static Instrument m_trades_instr;
		private static Range m_trades_range;

		/// <summary>True to dump all trades to an ldr file</summary>
		public static void LogTrades(Rylobot bot, bool enabled)
		{
			var currently_enabled = m_ldr_all_trades != null;
			if (currently_enabled == enabled) return;
			if (m_ldr_all_trades != null)
			{
				bot.Positions.Opened -= LogTradeOpened;
				bot.Positions.Closed -= LogTradeClosed;

				// Dump the instrument over the range of trades
				m_trades_range.End   += (int)m_trades_instr.FirstIdx;
				m_trades_range.Begin += (int)m_trades_instr.FirstIdx;
				Dump(m_trades_instr, m_trades_instr.IndexRange(m_trades_range));
			}
			m_ldr_all_trades = enabled ? new LdrBuilder() : null;
			if (m_ldr_all_trades != null)
			{
				// Reset the all_trades file
				m_trade_ids.Clear();
				m_ldr_all_trades.ToFile(FP("all_trades.ldr"));

				m_trades_instr = new Instrument(bot);
				m_trades_range = Range.Invalid;

				bot.Positions.Closed += LogTradeClosed;
				bot.Positions.Opened += LogTradeOpened;
			}
		}

		/// <summary>Record a trade as opened</summary>
		public static void LogTrade(Position pos)
		{
			// Output the view of the trade
			var ldr = new LdrBuilder();
			using (ldr.Group("trade_{0}".Fmt(pos.Id), ScaleTxfm))
				new Trade(m_trades_instr, pos).Dump(ldr, show_snr:true);
			ldr.ToFile(FP("trade_{0}.ldr".Fmt(pos.Id)));

			// Encompass the range
			m_trades_range.Begin = Math.Min(m_trades_range.Begin, m_trades_instr.Count - 10);
			m_trades_range.End   = Math.Max(m_trades_range.End  , m_trades_instr.Count + 10);

			// Add to the 'all trades' file
			if (!m_trade_ids.Contains(pos.Id))
			{
				m_ldr_all_trades.Append("#include \"trade_{0}.ldr\"\n".Fmt(pos.Id));
				m_ldr_all_trades.ToFile(FP("all_trades.ldr"));
				m_trade_ids.Add(pos.Id);
			}
		}
		private static void LogTradeOpened(PositionOpenedEventArgs args)
		{
			LogTrade(args.Position);
		}
		private static void LogTradeClosed(PositionClosedEventArgs args)
		{
			LogTrade(args.Position);
		}

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(int entry_index, int exit_index, double ep, double sl, double tp, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("trade", ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				var w = (float)(exit_index - entry_index);
				var x = (float)(entry_index + w/2);
				var h_tp = (float)Math.Abs(tp - ep);
				var h_sl = (float)Math.Abs(sl - ep);
				var y_tp = (float)(ep + tp) / 2;
				var y_sl = (float)(ep + sl) / 2;

				ldr.Box("profit", 0x4000FF00, w, h_tp, 0.001f, pos:new v4(x, y_tp, -0.01f, 1));
				ldr.Box("loss"  , 0x40FF0000, w, h_sl, 0.001f, pos:new v4(x, y_sl, -0.01f, 1));
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trade.ldr"));
		}

		/// <summary>Dump the instrument to an ldr file</summary>
		public static LdrBuilder Dump(this Instrument instr, Range? range_ = null, double? high_res = null, int[] emas = null, int? msc_range = null, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the valid range of candles
			var range = instr.IndexRange(range_ ?? new Range((int)instr.FirstIdx, (int)instr.LastIdx));
			if (range.Empty)
				return ldr;

			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position doesn't change with updates
			using (ldr.Group(instr.SymbolCode))
			{
				var msc = instr.MedianCandleSize(range.Endi - (msc_range ?? 10), range.Endi);

				// Draw bullish/bearish candles
				{
					// Using TriList for the bodies, and LineList for the wicks.
					// So: 6 indices for the body, 4 for the wicks
					//   __|__
					//  |\    |
					//  |  \  |
					//  |____\|
					//     |
					// 0.......1.. -> candle index

					// Buffers
					var vbuf = new List<View3d.Vertex>(8 * range.Counti);
					var faces = new List<ushort>(6 * range.Counti);
					var lines = new List<ushort>(4 * range.Counti);

					// Create the candle geometry
					for (int i = range.Begini; i != range.Endi; ++i)
					{
						var candle = instr[i];
						var x = (float)(i - instr.FirstIdx);
						var o = (float)Math.Max(candle.Open, candle.Close);
						var h = (float)candle.High;
						var l = (float)candle.Low;
						var c = (float)Math.Min(candle.Open, candle.Close);
						var v = vbuf.Count;
						var col = 
							candle.Type(msc).IsIndecision() ? Colour32.Yellow :
							candle.Type(msc).IsTrend() ? (candle.Bullish ? AskColor : BidColor) :
							candle.Bullish ? AskColor.Darken(0.4f) : BidColor.Darken(0.4f);

						// Prevent degenerate triangles
						if (o == c)
						{
							o *= 1.000005f;
							c *= 0.999995f;
						}

						// Candle verts
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , h, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f - 0.4f , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f + 0.4f , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f - 0.4f , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f + 0.4f , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , l, 0f, 1f), col));

						// Candle body
						faces.Add((ushort)(v + 3));
						faces.Add((ushort)(v + 2));
						faces.Add((ushort)(v + 4));
						faces.Add((ushort)(v + 4));
						faces.Add((ushort)(v + 5));
						faces.Add((ushort)(v + 3));

						// Candle wick
						lines.Add((ushort)(v + 0));
						lines.Add((ushort)(v + 1));
						lines.Add((ushort)(v + 6));
						lines.Add((ushort)(v + 7));
					}

					if (lines.Count != 0 || faces.Count != 0)
						ldr.Mesh(string.Empty, 0xFFFFFFFF, View3d.EGeom.Vert|View3d.EGeom.Colr, vbuf, faces:faces, lines:lines);
				}

				// Add the higher resolution price line
				if (high_res != null)
				{
					var step = high_res.Value != 0 ? (double?)1.0/high_res.Value : null;
					var price_data = instr.HighResRange(range, step);
					if (price_data.Any())
					{
						ldr.Line("ask", Colour32.Yellow.Lerp(AskColor, 0.5f), 1, price_data.Select(x => new v4((float)x.x, (float)x.y, 0.05f, 1)));
						ldr.Line("bid", Colour32.Yellow.Lerp(BidColor, 0.5f), 1, price_data.Select(x => new v4((float)x.x, (float)x.z, 0.05f, 1)));
					}
				}

				if (emas != null)
				{
					var cols = new[] { Colour32.Green, Colour32.Red, Colour32.Blue }; var coli = 0;
					foreach (var ema in emas)
					{
						if (!range.Empty)
						{
							var first = (int)(range.Begin - (double)instr.FirstIdx);
							var last  = (int)(range.End   - (double)instr.FirstIdx);
							var series = instr.Bot.Indicators.ExponentialMovingAverage(instr.Data.Close, ema).Result;
							ldr.Line("ema", cols[coli], 1, int_.Range(first,last).Select(i => new v4((float)i, (float)series[i], 0.05f, 1)));
							coli = (coli + 1) % cols.Length;
						}
					}
				}

				//// Candle candles by type and show detected trend regions
				//for (int i = range.Begini; i != range.Endi; ++i)
				//{
				//	var candle = instr[i];
				//	var mean_candle_size = instr.MeanCandleSize(i-100, i);
				//	var x = (float)(i - instr.FirstIdx);

				//	// Candle coloured by type
				//	var col = CandleTypeColour[(int)candle.Type(mean_candle_size)];
				//	ldr.Line(col, new v4(x, (float)candle.Low, 0, 1), new v4(x, (float)candle.High, 0, 1));
				//	ldr.Box(col, 0.8f, (float)candle.BodyLength, 0.0001f, new v4(x, (float)candle.BodyCentre, 0, 1));

				//	// Mean candle size
				//	ldr.Line(0xFFFFFF00, new v4(x, (float)(candle.Centre - mean_candle_size/2), -0.001f, 1f), new v4(x, (float)(candle.Centre + mean_candle_size/2), -0.001f, 1f));

				//	// Trend strength
				//	var trend = instr.MeasureTrend(i-5,i);
				//	var trend_col = (trend > 0 ? Color_.FromArgb(0xFF00FF00) : Color_.FromArgb(0xFFFF0000)).Alpha((float)Math.Abs(trend));
				//	var trend_sz = (float)(mean_candle_size * 0.1);
				//	var trend_y = (float)(trend > 0 ? candle.High + 4*trend_sz : candle.Low - 4*trend_sz);
				//	ldr.Box(trend_col, 0.3f, trend_sz, 0.0001f, new v4(x, trend_y, 0, 1));
				//}
			}
			if (ldr_ == null)
				ldr.ToFile(FP("{0}.ldr".Fmt(instr.SymbolCode)));

			return ldr;
		}

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(this Trade trade, LdrBuilder ldr_ = null, bool show_snr = false)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("Trade"))
			{
				var sign    = trade.TradeType.Sign();
				var x0      = (float)(trade.EntryIndex);
				var x1      = (float)Math.Max(trade.ExitIndex, x0 + 1.0);
				var w       = (float)(x1 - x0);
				var xmid    = (float)(trade.EntryIndex + w/2);

				// EP, SL, TP lines
				ldr.Line("EP", 0xFF0000FF, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
				ldr.Line("TP", 0xFF00FF00, new v4(x0, (float)trade.TP, 0f, 1f), new v4(x1, (float)trade.TP, +0.001f, 1f));
				ldr.Line("SL", 0xFFFF0000, new v4(x0, (float)trade.SL, 0f, 1f), new v4(x1, (float)trade.SL, +0.001f, 1f));

				// Areas showing peaks
				var col_pp = trade.Result == Trade.EResult.HitTP ? 0x8000FF00 : 0x4000FF00;
				var col_pl = trade.Result == Trade.EResult.HitSL ? 0x80FF0000 : 0x40FF0000;
				var y_pp = (float)(trade.EP + sign * trade.PeakProfit / 2);
				var y_pl = (float)(trade.EP - sign * trade.PeakLoss   / 2);
				ldr.Rect("peak_profit", col_pp, AxisId.PosZ, w, (float)Misc.Abs(trade.PeakProfit), true, new v4(xmid, y_pp, +0.001f, 1));
				ldr.Rect("peak_loss"  , col_pl, AxisId.PosZ, w, (float)Misc.Abs(trade.PeakLoss  ), true, new v4(xmid, y_pl, +0.001f, 1));

				// Show S&R levels leading up to this trade
				if (show_snr)
				{
					var idx_max = (trade.Instrument.FirstIdx + (int)trade.EntryIndex);
					var idx_min = (idx_max - trade.Instrument.Bot.Settings.LookBackCount);
					var snr = new SnR(trade.Instrument, idx_min, idx_max);
					Dump(snr, ldr_:ldr);
				}
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trade.ldr"));
		}

		/// <summary>Dump a position to an ldr file</summary>
		public static void Dump(this Position pos, Instrument instr, LdrBuilder ldr_ = null, bool show_snr = false)
		{
			var trade = new Trade(instr, pos);
			trade.Dump(ldr_, show_snr);
		}

		/// <summary>Dump the SnR data to an ldr file</summary>
		public static void Dump(this SnR snr, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = snr.Instrument;

			using (ldr.Group("SnR_{0}".Fmt(instr.SymbolCode), ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				// Stationary points
				const float scale = 0.005f;
				foreach (var sp in snr.StationaryPoints)
					ldr.Rect("pt", Color.Blue, +3, 1f*scale, (float)snr.BucketSize*scale, false,
						new v4((float)(sp.Index - (double)instr.FirstIdx), (float)sp.Price, 0f, 1f));

				// SnR levels
				for (int i = 0, iend = snr.SnRLevels.Count; i != iend; ++i)
					ldr.Line("lvl", Color.Yellow.Lerp(Color.Black, (float)i/iend),
						new v4((float)(snr.Range.Begini - instr.FirstIdx), (float)snr.SnRLevels[i].Price, 0f, 1f),
						new v4((float)(snr.Range.Endi   - instr.FirstIdx), (float)snr.SnRLevels[i].Price, 0f, 1f));
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("snr.ldr"));
		}
	}
}
