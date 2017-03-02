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
			const string Dir = @"P:\dump\trading";
			return Path_.CombinePath(Dir, fname);
		}

		/// <summary>Reset a file to empty</summary>
		public static void ClearFile(string fname)
		{
			new LdrBuilder().ToFile(FP(fname));
		}

		/// <summary>Draw custom ldr script</summary>
		public static void Dump(Action<LdrBuilder> func, string filename, bool append = false)
		{
			if (!append) m_ldr.Clear();
			func(m_ldr);
			Ldr.Write(m_ldr.ToString(), FP(filename));
		}
		private static LdrBuilder m_ldr = new LdrBuilder();

		/// <summary>Break the debugger on the start of certain candles</summary>
		public static void BreakOnCandleOfInterest()
		{
			if (!Instrument.NewCandle) return;
			if (!Path_.FileExists(FP("BreakCandleIndices.txt"))) return;
			var str = File.ReadAllText(FP("BreakCandleIndices.txt"));
			var indices = int_.ParseArray(str);
			if (indices.Contains(Instrument.Count))
				Debugger.Break();
		}

		/// <summary>Output a debug message</summary>
		public static void Trace(string message)
		{
			Debug.WriteLine(message);
		}

		#region Dump to Ldr File

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(Trade trade, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("Trade"))
			{
				var sign    = trade.TradeType.Sign();
				var x0      = (float)(trade.EntryIndex);
				var x1      = (float)Math.Max(trade.ExitIndex, x0 + 5.0);
				var w       = (float)(x1 - x0);
				var xmid    = (float)(trade.EntryIndex + w/2);
				var colr    = trade.Result == Trade.EResult.Pending ? 0x4000007F : 0xFF0000FF;
				var sl_colr = trade.Result == Trade.EResult.Pending ? 0x407F0000 : 0xFFFF0000;
				var tp_colr = trade.Result == Trade.EResult.Pending ? 0x40007F00 : 0xFF00FF00;

				// EP, SL, TP lines
				ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
				if (trade.SL != null) ldr.Line("SL", sl_colr, new v4(x0, (float)trade.SL, 0f, 1f), new v4(x1, (float)trade.SL, +0.001f, 1f));
				if (trade.TP != null) ldr.Line("TP", tp_colr, new v4(x0, (float)trade.TP, 0f, 1f), new v4(x1, (float)trade.TP, +0.001f, 1f));

				// Draw green for profit, red for loss
				switch (trade.Result)
				{
				case Trade.EResult.Open:
					{
						var value = (float)trade.ValueNow() / trade.Volume;
						var h = Math.Abs(value);
						var y = (float)(trade.EP + sign * Math.Sign(value) * h / 2);
						var col = value > 0 ? 0x4000FF00U : 0x40FF0000U;
						ldr.Rect("result", col, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));
						break;
					}
				case Trade.EResult.Pending:
					{
						break;
					}
				case Trade.EResult.HitSL:
					{
						var h = (float)trade.StopLossRel();
						var y = (float)(trade.EP - sign * h / 2);
						ldr.Rect("result", 0x80FF0000, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));
						break;
					}
				case Trade.EResult.HitTP:
					{
						var h = (float)trade.TakeProfitRel();
						var y = (float)(trade.EP + sign * h / 2);
						ldr.Rect("result", 0x8000FF00, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));
						break;
					}
				}

				// Areas showing peaks
				// var col_pp = trade.Result == Trade.EResult.HitTP ? 0x8000FF00 : 0x4000FF00;
				// var col_pl = trade.Result == Trade.EResult.HitSL ? 0x80FF0000 : 0x40FF0000;
				// var y_pp = (float)(trade.EP + sign * trade.PeakProfit / 2);
				// var y_pl = (float)(trade.EP - sign * trade.PeakLoss   / 2);
				// ldr.Rect("peak_profit", col_pp, AxisId.PosZ, w, (float)Misc.Abs(trade.PeakProfit), true, new v4(xmid, y_pp, +0.001f, 1));
				// ldr.Rect("peak_loss"  , col_pl, AxisId.PosZ, w, (float)Misc.Abs(trade.PeakLoss  ), true, new v4(xmid, y_pl, +0.001f, 1));
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trade.ldr"));
		}

		/// <summary>Dump an instrument to an ldr file</summary>
		/// <param name="instr">The instrument to be output</param>
		/// <param name="range_">Optional. Sub range within the instrument data (NegIdx)</param>
		/// <param name="mcs_range">Optional. The number of candles to use to get the median candle size for candle type classification. (default 50)</param>
		/// <param name="high_res">Optional. Add the sub candle ask/bid line. Value is the number of steps within each candle.</param>
		/// <param name="emas">Optional. Add exponential moving average lines, with periods of the values given.</param>
		public static LdrBuilder Dump(Instrument instr, Range? range_ = null, double? high_res = null, int[] emas = null, int? mcs_range = null, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the range of candles to output
			var range = instr.IndexRange(range_ ?? new Range((int)instr.IdxFirst, (int)instr.IdxLast));
			if (range.Empty)
				return ldr;

			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position doesn't change with updates
			using (ldr.Group(instr.SymbolCode))
			{
				// Find the MCS
				var mcs = instr.MedianCandleSize(range.Endi - (mcs_range ?? 50), range.Endi);

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
					for (int i = range.Begi; i != range.Endi; ++i)
					{
						var candle = instr[i];
						if (i == 0) candle.Update(instr.LatestPrice); // Handles the case when 'OnTick' has not been called yet
						var x = (float)(i - instr.IdxFirst);
						var o = (float)Math.Max(candle.Open, candle.Close);
						var h = (float)candle.High;
						var l = (float)candle.Low;
						var c = (float)Math.Min(candle.Open, candle.Close);
						var v = vbuf.Count;
						var col = 
							candle.Type(mcs).IsIndecision() ? Colour32.Yellow :
							candle.Type(mcs).IsTrend() ? (candle.Bullish ? AskColor : BidColor) :
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
						ldr.Line("ask", Colour32.Yellow.Lerp(AskColor, 0.5f), 1, price_data.Select(x => new v4((float)x.Index, (float)x.Ask, 0.05f, 1)));
						ldr.Line("bid", Colour32.Yellow.Lerp(BidColor, 0.5f), 1, price_data.Select(x => new v4((float)x.Index, (float)x.Bid, 0.05f, 1)));
					}
				}

				// Add exponential moving average lines
				if (emas != null)
				{
					var cols = new[] { Colour32.Green, Colour32.Red, Colour32.Blue }; var coli = 0;
					var first = (int)(range.Beg - (double)instr.IdxFirst);
					var last  = (int)(range.End - (double)instr.IdxFirst);
					foreach (var ema in emas)
					{
						var avr = new ExpMovingAvr(ema);
						ldr.Line("ema", cols[coli], 1, int_.Range((int)range.Beg, (int)range.End).Select(i =>
						{
							avr.Add(instr[i].Median);
							return new v4((float)(i - instr.IdxFirst), (float)avr.Mean, 0.05f, 1f);
						}));
						coli = (coli + 1) % cols.Length;
					}
				}

				// Current price levels
				ldr.Line("ASK", 0xFF00FF00, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Ask, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.Symbol.Ask, 0, 1f));
				ldr.Line("BID", 0xFFFF0000, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Bid, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.Symbol.Bid, 0, 1f));

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

		/// <summary>Dump the SnR data to an ldr file</summary>
		public static void Dump(SnR snr, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = snr.Instrument;

			using (ldr.Group("SnR_{0}".Fmt(instr.SymbolCode)))
			{
				// SnR levels
				for (int i = 0, iend = snr.SnRLevels.Count; i != iend; ++i)
				{
					var lvl = snr.SnRLevels[i];
					ldr.Line("lvl", new Colour32(0x40FF0000).Lerp(new Colour32(0xFFFFFF00), (float)lvl.Strength),
						new v4((float)(snr.End - instr.IdxFirst), (float)lvl.Price, 0f, 1f),
						new v4((float)(snr.Beg - instr.IdxFirst), (float)lvl.Price, 0f, 1f));
				}

				// Draw a box around the SNR levels
				var width = snr.End - snr.Beg;
				ldr.Rect("range", 0xFF800080, AxisId.PosZ, width, (float)(2*snr.Range), false,
					new v4((float)(snr.Beg + width/2 - instr.IdxFirst), (float)snr.Price, 0, 1f));
			}
			if (ldr_ == null)
				ldr.ToFile(FP("snr.ldr"));
		}

		/// <summary>Dump the price peaks to an ldr file</summary>
		public static void Dump(PricePeaks pp, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = pp.Instrument;

			using (ldr.Group("PricePeaks_{0}".Fmt(instr.SymbolCode)))
			{
				for (int i = 0, iend = pp.Peaks.Count; i != iend; ++i)
				{
					var pk = pp.Peaks[i];
					var name = pk.High ? "high" : "low";
					ldr.Ellipse(name, 0x800000FF, AxisId.PosZ, true, 0.25f, (float)(3f*instr.PipSize), new v4(pk.Index - instr.IdxFirst + 0.5f, (float)pk.Price, 0.01f, 1f));
				}
			}
			if (ldr_ == null)
				ldr.ToFile(FP("price_peaks.ldr"));
		}

		/// <summary>Dump a slope, coloured to indicate trend</summary>
		public static void Slope(NegIdx idx, double slope, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			var price = Instrument[idx].Close;
			var trend = Instrument.MeasureTrend(slope);
			var colour =
				trend > +0.5f ? 0xFF008000 :
				trend < -0.5f ? 0xFF800000 :
				0xFFC0C0C0;

			const int length = 5;
			ldr.Line("Slope", colour,
				new v4((float)(idx - length - Instrument.IdxFirst), (float)(price - length * slope), 0f, 1f),
				new v4((float)(idx + length - Instrument.IdxFirst), (float)(price + length * slope), 0f, 1f));

			if (ldr_ == null)
				ldr.ToFile(FP("slope.ldr"));
		}

		#endregion

		#region Trade Logging

		/// <summary>Master switch</summary>
		private static bool LoggingEnabled = true;

		/// <summary>The list of logged trades</summary>
		private static List<int> m_trade_ids = new List<int>();

		/// <summary>A ldr builder for outputting trades</summary>
		private static LdrBuilder m_ldr_all_trades;

		/// <summary>The index range of trades</summary>
		private static Range m_trades_range;

		/// <summary>A ldr builder for outputting the instrument</summary>
		private static LdrBuilder m_ldr_instr;

		/// <summary>The instrument being logged</summary>
		private static Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					m_instr.DataChanged -= HandleInstrumentDataChanged;
					Util.Dispose(ref m_instr);
				}
				m_instr = value;
				if (m_instr != null)
				{
					m_instr.DataChanged += HandleInstrumentDataChanged;
				}
			}
		}
		private static Instrument m_instr;

		/// <summary>Enable/Disable logging trades to an ldr file</summary>
		public static void LogTrades(Rylobot bot, bool enabled)
		{
			if (m_log_trades == enabled) return;

			// Disabled
			if (m_log_trades)
			{
				Trace("TRADE LOGGING DISABLED");

				// Unsubscribe
				bot.Positions.Opened -= LogTradeOpened;
				bot.Positions.Closed -= LogTradeClosed;

				Instrument = null;
			}

			m_log_trades = enabled;

			// Enabled
			if (m_log_trades)
			{
				// Reset the debugging files
				ClearFile("all_trades.ldr");
				ClearFile("candle_patterns.ldr");
				m_ldr_all_trades = new LdrBuilder();
				m_trades_range = Range.Invalid;
				m_trade_ids.Clear();

				// Create the instrument being traded
				Instrument = new Instrument(bot);
				m_ldr_instr = new LdrBuilder();

				// Sign up for position created/closed events
				bot.Positions.Closed += LogTradeClosed;
				bot.Positions.Opened += LogTradeOpened;

				// Initial instrument output
				LogInstrument();

				Trace("TRADE LOGGING ENABLED");
			}
		}
		private static bool m_log_trades;

		/// <summary>Handle trade created/closed events</summary>
		private static void LogTradeOpened(PositionOpenedEventArgs args)
		{
			LogTrade(args.Position, true);
		}
		private static void LogTradeClosed(PositionClosedEventArgs args)
		{
			LogTrade(args.Position, true);
		}

		/// <summary>Update the details of a trade</summary>
		public static void LogTrade(Position pos, bool update_instrument)
		{
			if (!LoggingEnabled)
				return;

			// Output the trade as a separate file
			var ldr = new LdrBuilder();
			using (ldr.Group("trade_{0}".Fmt(pos.Id)))
				Dump(new Trade(Instrument, pos), ldr_:ldr);
			ldr.ToFile(FP("trades\\trade_{0}.ldr".Fmt(pos.Id)));

			// Encompass the range
			m_trades_range.Beg = Math.Min(m_trades_range.Beg, Instrument.Count - 10);
			m_trades_range.End = Math.Max(m_trades_range.End, Instrument.Count + 10);

			// Add to the 'all trades' file
			if (!m_trade_ids.Contains(pos.Id))
			{
				m_ldr_all_trades.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(pos.Id));
				m_ldr_all_trades.ToFile(FP("all_trades.ldr"));
				m_trade_ids.Add(pos.Id);
			}

			// Also update the instrument so that it matches the trade
			if (update_instrument)
				LogInstrument();
		}

		/// <summary>Update the details of a pending order</summary>
		public static void LogOrder(PendingOrder ord, bool update_instrument)
		{
			if (!LoggingEnabled)
				return;

			// Output the order as a separate file
			var ldr = new LdrBuilder();
			using (ldr.Group("order_{0}".Fmt(ord.Id)))
				Dump(new Trade(Instrument, ord), ldr_:ldr);
			ldr.ToFile(FP("trades\\order_{0}.ldr".Fmt(ord.Id)));

			// Also update the instrument so that it matches the trade
			if (update_instrument)
				LogInstrument();
		}

		/// <summary>Output the instrument to a file as it changes</summary>
		public static void LogInstrument()
		{
			if (!LoggingEnabled)
				return;

			Dump(Instrument
				,range_:new Range(-500, 1)
				,emas:new[] { 14, 200 }
				,ldr_:m_ldr_instr);

			// Write the instrument to 'm_ldr_instr'
			m_ldr_instr.ToFile(FP("{0}.ldr".Fmt(Instrument.SymbolCode)));
			m_ldr_instr.Clear();

			// Update the graphics for active trades and orders
			foreach (var pos in Rylobot.Instance.Positions)
				LogTrade(pos, false);
			foreach (var ord in Rylobot.Instance.PendingOrders)
				LogOrder(ord, false);

			// Update the live trades file
			{
				var sb = new LdrBuilder();
				foreach (var p in Rylobot.Instance.Positions)
					sb.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(p.Id));
				foreach (var p in Rylobot.Instance.PendingOrders)
					sb.Append("#include \"trades\\order_{0}.ldr\"\n".Fmt(p.Id));
				sb.ToFile(FP("live_trades.ldr"));
			}
		}

		/// <summary>Draw a box around the candles in the range [beg,end]</summary>
		public static void CandlePattern(NegIdx beg, NegIdx end)
		{
			var w = new RangeF((double)(beg - Instrument.IdxFirst), (double)(beg - Instrument.IdxFirst));
			var h = RangeF.Invalid;
			foreach (var c in Instrument.CandleRange(beg, end + 1))
			{
				w.End += 1.0;
				h.Encompass(c.Low);
				h.Encompass(c.High);
			}

			var ldr = new LdrBuilder();
			ldr.Rect("box", 0xFF0000FF, AxisId.PosZ, w.Sizef, h.Sizef, false, new v4(w.Midf, h.Midf, 0f, 1f));
			ldr.ToFile(FP("candle_patterns.ldr"), append:true);
		}

		/// <summary>Write debugging output when the instrument changes</summary>
		private static void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			if (!LoggingEnabled)
				return;

			// Limit dumps to once / second
			if (Environment.TickCount - m_last_instr_dump < 1000) return;
			m_last_instr_dump = Environment.TickCount;

			// Output the instrument
			LogInstrument();
		}
		private static int m_last_instr_dump = 0;

		#endregion
	}
}


#if false

				//// Show S&R levels leading up to this trade
				//if (show_snr)
				//{
				//	var idx_max = (trade.Instrument.FirstIdx + (int)trade.EntryIndex);
				//	var idx_min = (idx_max - trade.Instrument.Bot.Settings.LookBackCount);
				//	var snr = new SnR(trade.Instrument, idx_min, idx_max);
				//	Dump(snr, ldr_:ldr);
				//}
		/// <summary>Dump a trade to an ldr file. Adds to 'ldr_' if provided</summary>
		public static void Dump(double entry_index, double exit_index, double ep, double sl, double tp, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("trade"))
			{
				var w    = (float)(exit_index - entry_index);
				var x    = (float)(entry_index + w/2);
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

		/// <summary>Dump a position to an ldr file</summary>
		public static void Dump(this Position pos, Instrument instr, LdrBuilder ldr_ = null, bool show_snr = false)
		{
			var trade = new Trade(instr, pos);
			trade.Dump(ldr_, show_snr);
		}

#endif