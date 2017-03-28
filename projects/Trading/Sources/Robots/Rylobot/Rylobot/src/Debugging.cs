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

		/// <summary>Master switch</summary>
		public static bool DebuggingEnabled
		{
			get { return m_debugging_enabled || Bot.TickNumber >= m_debug_from_tick || Bot.Instrument.Count >= m_debug_from_candle; }
			set { m_debugging_enabled = value; }
		}
		private static int m_debug_from_tick;
		private static int m_debug_from_candle;
		private static bool m_debugging_enabled;

		/// <summary>Trace switch</summary>
		public static bool TraceEnabled
		{
			get { return m_trace_enabled || DebuggingEnabled; }
			set { m_trace_enabled = value; }
		}
		private static bool m_trace_enabled;

		/// <summary>Full path for a dump file</summary>
		public static string FP(string fname)
		{
			const string Dir = @"P:\dump\trading";
			return Path_.CombinePath(Dir, fname);
		}

		/// <summary>Read settings from the settings file</summary>
		private static void LoadDebuggingSettings()
		{
			// Set defaults;
			m_debugging_enabled   = false;
			m_debug_from_tick     = int.MaxValue;
			m_debug_from_candle   = int.MaxValue;
			m_candles_of_interest .Clear();
			m_ticks_of_interest   .Clear();

			// Look for candles/ticks of interest
			var filepath = FP("debug_settings.txt");
			if (!Path_.FileExists(filepath))
				return;

			var lines = File.ReadAllLines(filepath);
			for (int i = 0; i != lines.Length; ++i)
			{
				if (lines[i].StartsWith("#"))
					continue;

				var pair = lines[i].SubstringRegex(@"(.*?)\s*=\s*(.*)\s*");
				if (pair.Length != 2)
					continue;

				switch (pair[0])
				{
				case "debug_from_tick":   m_debug_from_tick     = pair[1].HasValue() ? int.Parse(pair[1]) : int.MaxValue; break;
				case "debug_from_candle": m_debug_from_candle   = pair[1].HasValue() ? int.Parse(pair[1]) : int.MaxValue; break;
				case "break_candles":     m_candles_of_interest .AddRange(int_.ParseArray(pair[1])); break;
				case "break_ticks":       m_ticks_of_interest   .AddRange(int_.ParseArray(pair[1])); break;
				case "trace_enabled":     m_trace_enabled       = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				}
			}
		}

		/// <summary>The bot</summary>
		private static Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Instrument.DataChanged -= HandleInstrumentDataChanged;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Instrument.DataChanged += HandleInstrumentDataChanged;
				}
			}
		}
		private static Rylobot m_bot;

		/// <summary>The instrument being logged</summary>
		private static Instrument Instrument
		{
			[DebuggerStepThrough] get { return Bot.Instrument; }
		}

		/// <summary>The set of all trades</summary>
		public static Dictionary<int, Trade> AllTrades { get; private set; }

		/// <summary>The index range of trades</summary>
		public static Range TradesRange { get; private set; }

		/// <summary>Raised to dump the instrument state</summary>
		public static event Action DumpInstrument;

		/// <summary>Reset a file to empty</summary>
		public static void ClearFile(string fname)
		{
			new LdrBuilder().ToFile(FP(fname));
		}

		/// <summary>Break the debugger on the start of certain candles</summary>
		public static void BreakOnPointOfInterest()
		{
			if (m_ticks_of_interest.Contains(Bot.TickNumber))
			{
				Debugger.Break();
				m_ticks_of_interest.Remove(Bot.TickNumber);
			}
			if (Instrument.NewCandle && m_candles_of_interest.Contains(Instrument.Count))
			{
				Debugger.Break();
				m_candles_of_interest.Remove(Instrument.Count);
			}
		}
		private static List<int> m_candles_of_interest = new List<int>();
		private static List<int> m_ticks_of_interest = new List<int>();

		/// <summary>Output a debug message</summary>
		public static void Trace(string message)
		{
			if (!TraceEnabled) return;
			using (var f = new StreamWriter(new FileStream(FP("trace.log"), FileMode.Append, FileAccess.Write, FileShare.Read)))
				f.WriteLine(message);
		}

		/// <summary>Enable/Disable logging trades to an ldr file</summary>
		public static bool LogTrades
		{
			get { return m_log_trades; }
			set
			{
				if (m_log_trades == value) return;

				// Disabled
				if (m_log_trades)
				{
					Trace("TRADE LOGGING DISABLED\n");

					// Unsubscribe
					Bot.Positions.Opened -= LogTradeOpened;
					Bot.Positions.Closed -= LogTradeClosed;
					Bot.Stopping -= OnBotStopping;

					m_candles_of_interest.Clear();
					m_ticks_of_interest  .Clear();
					Bot = null;
				}

				m_log_trades = value;

				// Enabled
				if (m_log_trades)
				{
					Bot = Rylobot.Instance;

					// Reset the debugging files
					ClearFile(FP("all_trades.ldr"));
					ClearFile(FP("candle_patterns.ldr"));
					ClearFile(FP("trace.log"));
					ClearFile(FP("wins.log"));
					ClearFile(FP("losses.log"));

					TradesRange = Range.Invalid;

					// Trades that have been seen before
					AllTrades = new Dictionary<int, Trade>();

					// Sign up for position created/closed events
					Bot.Positions.Closed += LogTradeClosed;
					Bot.Positions.Opened += LogTradeOpened;
					Bot.Stopping += OnBotStopping;

					// Initial instrument output
					LogInstrument();

					// Load debugging settings
					LoadDebuggingSettings();
	
					Trace("\nTRADE LOGGING ENABLED");
				}
			}
		}
		private static bool m_log_trades;

		/// <summary>Handle trade created/closed events</summary>
		private static void LogTradeOpened(PositionOpenedEventArgs args)
		{
			var position = args.Position;
			LogTrade(position, live:true, update_instrument:true);
			Trace("Idx={0},Tick={1} - Position {2} Opened - {3} EP={4} Volume={5} {6}".Fmt(
				Instrument.Count, Bot.TickNumber, position.Id, position.TradeType, position.EntryPrice,
				position.Volume, position.Comment ?? string.Empty));
		}
		private static void LogTradeClosed(PositionClosedEventArgs args)
		{
			var position = args.Position;
			LogTrade(position, live:false, update_instrument:true);
			Trace("Idx={0},Tick={1} - Position {2} Closed - {3} EP={4} Volume={5} Profit=${6} Equity=${7} {8}".Fmt(
				Instrument.Count, Bot.TickNumber, position.Id, position.TradeType, position.EntryPrice,
				position.Volume, position.NetProfit, Bot.Account.Equity, position.Comment));
		}
		private static void OnBotStopping(object sender, EventArgs e)
		{
			foreach (var pos in Bot.Positions)
				LogTrade(pos, live:false, update_instrument:false);
		}

		/// <summary>Update the details of a trade</summary>
		public static void LogTrade(Trade trade, bool update_instrument = true)
		{
			// Record the trade
			var new_trade = !AllTrades.ContainsKey(trade.Id);
			if (!trade.IsPending) AllTrades[trade.Id] = trade;

			if (DebuggingEnabled)
			{
				// Output the trade as a separate file
				var ldr = new LdrBuilder();
				using (ldr.Group(trade.Name)) Dump(trade, ldr_:ldr);
				ldr.ToFile(FP("trades\\{0}.ldr".Fmt(trade.Name)));

				// Encompass the range
				TradesRange.Encompass(Instrument.Count - 10);
				TradesRange.Encompass(Instrument.Count + 10);

				// Add to the 'all trades' or 'live trades' file
				if (trade.IsLive || trade.IsPending)
					LogLiveTrades();
				else // Closed
					LogAllTrades();

				// Also update the instrument so that it matches the trade
				if (update_instrument)
					LogInstrument();
			}
		}

		/// <summary>Update the details of a trade</summary>
		public static void LogTrade(Position pos, bool live, bool update_instrument = true)
		{
			LogTrade(new Trade(Instrument, pos, live?Trade.EResult.Open:Trade.EResult.Closed), update_instrument);
		}

		/// <summary>Update the details of a pending order</summary>
		public static void LogOrder(PendingOrder ord, bool update_instrument = true)
		{
			LogTrade(new Trade(Instrument, ord), update_instrument);
		}

		/// <summary>Update the live trades file</summary>
		private static void LogLiveTrades()
		{
			var ldr = new LdrBuilder();
			//foreach (var t in AllTrades.Values.Where(x => x.IsLive))
			//	ldr.Append("#include \"trades\\{0}.ldr\"\n".Fmt(t.Name));
			foreach (var p in Rylobot.Instance.Positions)     ldr.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(p.Id));
			foreach (var p in Rylobot.Instance.PendingOrders) ldr.Append("#include \"trades\\order_{0}.ldr\"\n".Fmt(p.Id));
			ldr.ToFile(FP("live_trades.ldr"));
		}

		/// <summary>Update the all trades file</summary>
		private static void LogAllTrades()
		{
			var ldr = new LdrBuilder();
			foreach (var t in AllTrades.Values.Where(x => !x.IsLive))
				ldr.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(t.Id));
			ldr.ToFile(FP("all_trades.ldr"));
		}

		/// <summary>Output the instrument to a file as it changes</summary>
		public static void LogInstrument()
		{
			if (DebuggingEnabled)
			{
				// Dump the instrument data
				DumpInstrument.Raise();

				// Update the graphics for active trades and orders
				foreach (var pos in Rylobot.Instance.Positions)     LogTrade(pos, live:true, update_instrument:false);
				foreach (var ord in Rylobot.Instance.PendingOrders) LogOrder(ord, update_instrument:false);

				// Update the live trades file
				LogLiveTrades();
			}
		}

		/// <summary>Write debugging output when the instrument changes</summary>
		private static void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			if (!DebuggingEnabled)
				return;

			// Limit dumps to once / second
			if (Environment.TickCount - m_last_instr_dump < 1000) return;
			m_last_instr_dump = Environment.TickCount;

			// Output the instrument
			LogInstrument();
		}
		private static int m_last_instr_dump = 0;

		#region Dump to Ldr File

		/// <summary>Output the current price levels</summary>
		public static void CurrentPrice(Instrument instr, int? mcs_range = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Find the MCS
			var mcs = instr.MedianCandleSize(-(mcs_range ?? 50), 1);

			ldr.Line("ASK", 0xFF00FF00, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Ask, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.LatestPrice.Ask, 0, 1f));
			ldr.Line("BID", 0xFFFF0000, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Bid, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.LatestPrice.Bid, 0, 1f));
			ldr.Rect("MCS", 0xFFFF80FF, AxisId.PosZ, 1f, (float)mcs, false, new v4(10 - (int)instr.IdxFirst, (float)instr.LatestPrice.Mid, 0, 1f));

			if (ldr_ == null)
				ldr.ToFile(FP("{0}_currentprice.ldr".Fmt(instr.SymbolCode)));
		}

		/// <summary>Dump an instrument to an ldr file</summary>
		/// <param name="instr">The instrument to be output</param>
		/// <param name="range">Optional. Sub range within the instrument data (Idx)</param>
		/// <param name="high_res">Optional. Add the sub candle ask/bid line. Value is the number of steps within each candle.</param>
		/// <param name="mas">Optional. Add moving average indicators.</param>
		/// <param name="mcs_range">Optional. The number of candles to use to get the median candle size for candle type classification. (default 50)</param>
		public static void Dump(Instrument instr, RangeF? range = null, double? high_res = null, Indicator[] mas = null, int? mcs_range = null, bool? ema_slope = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the range of candles to output
			var rng = instr.IndexRange(range ?? new RangeF(instr.IdxFirst, instr.IdxLast));
			if (rng.Empty)
				return;

			// Colours for lines
			var cols = new[] { 0xFFB9D1EA, 0xFF066896, 0xFF0000A0, 0XFFFF2267, 0xFF9FCD38 };
			var coli = 0;

			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position doesn't change with updates
			using (ldr.Group(instr.SymbolCode))
			{
				// Find the MCS
				var mcs = instr.MedianCandleSize(rng.End - (mcs_range ?? 50), rng.End);

				// Draw bullish/bearish candles
				#region Candles
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
					var vbuf = new List<View3d.Vertex>(8 * rng.Counti);
					var faces = new List<ushort>(6 * rng.Counti);
					var lines = new List<ushort>(4 * rng.Counti);

					// Create the candle geometry
					for (int i = rng.Begi; i != rng.Endi; ++i)
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
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f        , h, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f        , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f - 0.4f , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f + 0.4f , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f - 0.4f , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f + 0.4f , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f        , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.0f        , l, -0.01f, 1f), col));

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
				#endregion

				// Add the higher resolution price line
				#region High Res
				if (high_res != null)
				{
					var step = high_res.Value != 0 ? (double?)1.0/high_res.Value : null;
					var price_data = instr.HighResRange(rng, step);
					if (price_data.Any())
					{
						ldr.Line("ask", Colour32.Yellow.Lerp(AskColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index - 0.5f, (float)x.Ask, 0.05f, 1)));
						ldr.Line("bid", Colour32.Yellow.Lerp(BidColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index - 0.5f, (float)x.Bid, 0.05f, 1)));
					}
				}
				#endregion

				// Add moving average lines
				#region MA
				if (mas != null)
				{
					foreach (var ma in mas)
					{
						var col = (Colour32)cols[coli++ % cols.Length];
						coli = (coli + 1) % cols.Length;

						ldr.Line("ma", col, 5, false, rng
							.Select(i => new v4(i            - instr.IdxFirst, (float)ma[i], -0.005f, 1f))
							.Concat(     new v4(instr.IdxNow - instr.IdxFirst, (float)ma[instr.IdxNow], -0.005f, 1f)));

						// Extrapolations
						var extrap = ma.Future;
						if (extrap != null)
							ldr.Line("ma_future", col.Alpha(0x40), 5, false, double_.Range(-5, +5, 0.1)
								.Select(i => new v4(i - instr.IdxFirst, (float)extrap[i], -0.005f, 1f)));
					}
				}
				#endregion

				// EMA slope integral
				#region EMA trend
				if (ema_slope ?? false)
				{
					using (ldr.Group("EMASlope"))
					{
						var scale = 20.0;
						var offset = (float)(instr.LatestPrice.Mid - 3*instr.MCS);
						ldr.Line("EMASlope", 0xFFA000A0, 1, false, rng
							.Select(x => new v4(x - instr.IdxFirst, offset + (float)(scale*instr.EMASlope((Idx)x)), 0, 1)));
						ldr.Line("Zero", 0xFF800080,
							new v4(rng.Beg - instr.IdxFirst, offset, 0, 1),
							new v4(rng.End - instr.IdxFirst, offset, 0, 1));
					}
				}
				#endregion
			}
			if (ldr_ == null)
				ldr.ToFile(FP("{0}.ldr".Fmt(instr.SymbolCode)));
		}

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(Trade trade, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("Trade"))
			{
				var instr   = trade.Instrument;
				var sign    = trade.TradeType.Sign();
				var x0      = (float)(trade.EntryIndex);
				var x1      = (float)Math.Max(trade.ExitIndex, x0 + 5.0);
				var w       = (float)(x1 - x0);
				var xmid    = (float)(trade.EntryIndex + w/2);
				var colr    = trade.Result == Trade.EResult.Pending ? 0x4000007FU : 0x400000FFU;
				var sl_colr = trade.Result == Trade.EResult.Pending ? 0x407F0000U : 0x40FF0000U;
				var tp_colr = trade.Result == Trade.EResult.Pending ? 0x40007F00U : 0x4000FF00U;

				// Draw green for profit, red for loss
				switch (trade.Result)
				{
				default:
					throw new Exception("Unknown trade result {0}".Fmt(trade.Result));
				case Trade.EResult.Open:
				case Trade.EResult.Unknown:
					{
						var value = (float)trade.ValueNow;
						var h = Math.Abs(value);
						var y = (float)(trade.EP + sign * Math.Sign(value) * h / 2);
						var col = value > 0 ? 0x4000FF00U : 0x40FF0000U;
						ldr.Rect("result", col, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));

						// EP, SL, TP lines
						ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
						if (trade.SL != null)
						{
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.SL, +0.001f, 1f));
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.SL, 0.001f, 1f), new v4(x1, (float)trade.SL, +0.001f, 1f));
						}
						if (trade.TP != null)
						{
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.TP, +0.001f, 1f));
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.TP, 0.001f, 1f), new v4(x1, (float)trade.TP, +0.001f, 1f));
						}
						break;
					}
				case Trade.EResult.Closed:
					{
						var value = (float)instr.Symbol.AcctToQuote(trade.NetProfit / trade.Volume);
						var h = Math.Abs(value);
						var y = (float)(trade.EP + sign * Math.Sign(value) * h / 2);
						var col = value > 0 ? 0x8000FF00U : 0x80FF0000U;
						ldr.Rect("result", col, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));
				
						// EP, SL, TP lines
						ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
						break;
					}
				case Trade.EResult.Pending:
					{
						// EP, SL, TP lines
						ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
						if (trade.SL != null)
						{
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.SL, +0.001f, 1f));
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.SL, 0.001f, 1f), new v4(x1, (float)trade.SL, +0.001f, 1f));
						}
						if (trade.TP != null)
						{
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.TP, +0.001f, 1f));
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.TP, 0.001f, 1f), new v4(x1, (float)trade.TP, +0.001f, 1f));
						}
						break;
					}
				case Trade.EResult.HitSL:
					{
						var h = (float)trade.StopLossRel();
						var y = (float)(trade.EP - sign * h / 2);
						ldr.Rect("result", 0x80FF0000, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));

						// EP, SL, TP lines
						ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
						if (trade.SL != null)
						{
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.SL, +0.001f, 1f));
							ldr.Line("SL", sl_colr, new v4(x0, (float)trade.SL, 0.001f, 1f), new v4(x1, (float)trade.SL, +0.001f, 1f));
						}
						break;
					}
				case Trade.EResult.HitTP:
					{
						var h = (float)trade.TakeProfitRel();
						var y = (float)(trade.EP + sign * h / 2);
						ldr.Rect("result", 0x8000FF00, AxisId.PosZ, w, h, true, new v4(xmid, y, +0.001f, 1));

						// EP, SL, TP lines
						ldr.Line("EP", colr, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, +0.001f, 1f));
						if (trade.TP != null)
						{
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.EP, 0.001f, 1f), new v4(x0, (float)trade.TP, +0.001f, 1f));
							ldr.Line("TP", tp_colr, new v4(x0, (float)trade.TP, 0.001f, 1f), new v4(x1, (float)trade.TP, +0.001f, 1f));
						}
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

		/// <summary>Dump a collection of trades</summary>
		public static void Dump(IEnumerable<Trade> trades, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			foreach (var trade in trades)
				Dump(trade, ldr_:ldr);

			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trades.ldr"));
		}

		/// <summary>Dump a collection of candles</summary>
		public static void Dump(IList<Candle> candles, QuoteCurrency mcs, float alpha = 1.0f, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Using TriList for the bodies, and LineList for the wicks.
			// So: 6 indices for the body, 4 for the wicks
			//   __|__
			//  |\    |
			//  |  \  |
			//  |____\|
			//     |
			// 0.......1.. -> candle index

			// Buffers
			var vbuf = new List<View3d.Vertex>(8 * candles.Count);
			var faces = new List<ushort>(6 * candles.Count);
			var lines = new List<ushort>(4 * candles.Count);

			// Create the candle geometry
			for (int i = 0; i != candles.Count; ++i)
			{
				var candle = candles[i];
				var x = (float)(candle.Index + 0.5*(candle.Width - 1.0));
				var w = (float)candle.Width;
				var o = (float)Math.Max(candle.Open, candle.Close);
				var h = (float)candle.High;
				var l = (float)candle.Low;
				var c = (float)Math.Min(candle.Open, candle.Close);
				var v = vbuf.Count;
				var col = (
					candle.Type(mcs).IsIndecision() ? Colour32.Yellow :
					candle.Type(mcs).IsTrend() ? (candle.Bullish ? AskColor : BidColor) :
					candle.Bullish ? AskColor.Darken(0.4f) : BidColor.Darken(0.4f)).Alpha(alpha);

				// Prevent degenerate triangles
				if (o == c)
				{
					o *= 1.000005f;
					c *= 0.999995f;
				}

				// Candle verts
				vbuf.Add(new View3d.Vertex(new v4(x            , h, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x            , o, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x - 0.49f*w  , o, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x + 0.49f*w  , o, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x - 0.49f*w  , c, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x + 0.49f*w  , c, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x            , c, -0.01f, 1f), col));
				vbuf.Add(new View3d.Vertex(new v4(x            , l, -0.01f, 1f), col));

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

			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("candles.ldr"));
		}

		/// <summary>Dump the SnR data to an ldr file</summary>
		public static void Dump(SnR snr, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = snr.Instrument;

			using (ldr.Group("SnR_{0}".Fmt(instr.SymbolCode)))
			{
				// SnR levels
				for (int i = 0, iend = snr.SnRLevels.Count; i != iend; ++i)
				{
					var lvl = snr.SnRLevels[i];
					ldr.Line("lvl", new Colour32(0x40FF0000).Lerp(new Colour32(0xFFFFFF00), (float)lvl.Strength),
						new v4(snr.End - instr.IdxFirst, (float)lvl.Price, 0f, 1f),
						new v4(snr.Beg - instr.IdxFirst, (float)lvl.Price, 0f, 1f));
				}

				// Draw a box around the SNR levels
				var width = snr.End - snr.Beg;
				ldr.Rect("range", 0xFF800080, AxisId.PosZ, width, (float)(2*snr.Range), false,
					new v4(snr.Beg + width/2 - instr.IdxFirst, (float)snr.Price, 0, 1f));

				// Draw the stationary points
				var pt0 = snr.StationaryPoints.First();
				foreach (var pt1 in snr.StationaryPoints.Skip(1))
				{
					ldr.Line("line", 0xFF0000FF, new v4(pt0.Index, (float)pt0.Price, 0, 1), new v4(pt1.Index, (float)pt1.Price, 0, 1));
					//ldr.Ellipse("pt", 0xFF0000FF, 3, true, 0.5f, 0.0001f, new v4(sp.Index, (float)sp.Price, 0f, 1f));
					pt0 = pt1;
				}

				// Batched candles
				//var candles = snr.Instrument.BatchedCandleRange(snr.Beg, snr.End, snr.TimeFrame).ToArray();
				//Dump(candles, snr.Instrument.MCS, alpha:0.2f, ldr_:ldr);
			}
			if (ldr_ == null)
				ldr.ToFile(FP("snr.ldr"));
		}

		/// <summary>Dump the price peaks to an ldr file</summary>
		public static void Dump(PricePeaks pp, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = pp.Instrument;

			using (ldr.Group("PricePeaks_{0}".Fmt(instr.SymbolCode)))
			{
				foreach (var pk in pp.Peaks.Take(20))
				{
					var name = pk.High ? "high" : "low";
					ldr.Ellipse(name, 0x800000FF, AxisId.PosZ, true, 0.25f, (float)(1f*instr.PipSize), new v4(pk.Index - instr.IdxFirst, (float)pk.Price, 0.01f, 1f));
				}

				// Curve fit the peaks
				var curve_hi = pp.TrendHigh;
				if (curve_hi != null)
				{
					var range = double_.Range(pp.Highs.Back().Index, pp.End, 0.1);
					ldr.Line("TrendHigh", 0xFF00FF00, 5, false, range.Select(x => new v4(x - instr.IdxFirst, (float)curve_hi.F(x), 0, 1f)));
				}
				var curve_lo = pp.TrendLow;
				if (curve_lo != null)
				{
					var range = double_.Range(pp.Lows.Back().Index, pp.End, 0.1);
					ldr.Line("TrendLow", 0xFFFF0000, 5, false, range.Select(x => new v4(x - instr.IdxFirst, (float)curve_lo.F(x), 0, 1f)));
				}
			}
			if (ldr_ == null)
				ldr.ToFile(FP("price_peaks.ldr"));
		}

		/// <summary>Dump a distribution to an ldr file</summary>
		public static void Dump(Distribution distribution, Idx X, double[] prob = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			var rx = distribution.YRange; // The counts per bucket
			var ry = distribution.XRange.Scale(3.0); // The range of prices
			if (ry.Size == 0)
				return;

			// The position to draw the distribution
			var pt = new v2((float)(X - Instrument.IdxFirst), (float)0);
			using (ldr.Group("Distribution_{0}".Fmt(distribution.Name)))
			{
				// Draw the axis
				ldr.Line("Y", Colour32.Black, new v4(pt.x, ry.Begf, 0, 1), new v4(pt.x, ry.Endf, 0, 1));

				// Plot the distribution vertically 
				ldr.Line("dist", 0xFF0000FF, 1, false, double_.Range(ry, ry.Size*0.01).Select(y =>
				{
					var value = distribution[y];
					return new v4((float)(pt.x + value), (float)(y), 0, 1);
				}));

				// Show probability levels
				if (prob != null)
				{
					var vals = distribution.Values(prob);
					foreach (var v in vals)
					{
						ldr.Line("prob", 0XFF33C1F3
							,new v4( 5-Instrument.IdxFirst, (float)v, 0, 1)
							,new v4(10-Instrument.IdxFirst, (float)v, 0, 1));
					}
				}
			}

			if (ldr_ == null)
				ldr.ToFile(FP("distribution.ldr"));
		}

		/// <summary>Dump a polynomial to an ldr file</summary>
		public static void Dump(IPolynomial poly, string name, Colour32 colour, RangeF range, double step = 0.1, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			if (poly != null)
				ldr.Line(name, colour, 5, false, double_.Range(range, 0.1).Select(x => new v4((float)(x - (double)Instrument.IdxFirst), (float)poly.F(x), 0.05f, 1f)));

			if (ldr_ == null)
				ldr.ToFile(FP("poly_{0}.ldr".Fmt(name)));
		}

		/// <summary>Dump the results of the correlator</summary>
		public static void Dump(Correlator correlator)
		{
			//if (!DebuggingEnabled) return;
			var filepath = FP("report_{0}.txt".Fmt(correlator.Name));
			File.WriteAllText(filepath, correlator.Report);
		}

		/// <summary>Dump a slope, coloured to indicate trend</summary>
		public static void Slope(Idx idx, double slope, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			var price = Instrument[idx].Close;
			var trend = Instrument.MeasureTrendFromSlope(slope);
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

		/// <summary>Draw a box around the candles in the range [beg,end]</summary>
		public static void CandlePattern(Idx beg, Idx end)
		{
			if (!DebuggingEnabled) return;
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

		/// <summary>Dump arbitrary graphics to a file</summary>
		public static void Dump(string filename, Action<LdrBuilder> func)
		{
			if (!DebuggingEnabled) return;
			var ldr = new LdrBuilder();
			func(ldr);
			ldr.ToFile(FP(filename));
		}

		#endregion

		/// <summary>Output the edge of all trades out to 'periods' candles after the trade entry</summary>
		public static void ReportEdge(int periods)
		{
			Trace("Edge {0} - Based on {1} entry signals".Fmt(periods, AllTrades.Count));

			var mae = Util.NewArray<AvrVar>(periods);
			var mfe = Util.NewArray<AvrVar>(periods);
			foreach (var trade in AllTrades.Values)
			{
				var excursion = trade.MaxExcursion(periods);
				for (int i = 0; i != periods; ++i)
				{
					mae[i].Add(excursion[i].Beg);
					mfe[i].Add(excursion[i].End);
				}
			}

			// Plot mfe/mae vs. periods
			var ldr = new LdrBuilder();
			ldr.Append("Period, Excursion, Excursion Hi, Excursion Lo\n");
			for (int i = 0; i != periods; ++i)
			{
				var f = new v3((float)mfe[i].Mean, (float)(mfe[i].Mean + mfe[i].PopStdDev), (float)(mfe[i].Mean - mfe[i].PopStdDev));
				var a = new v3((float)mae[i].Mean, (float)(mae[i].Mean - mae[i].PopStdDev), (float)(mae[i].Mean + mae[i].PopStdDev));
				var ratio = Maths.Div(f - a, f + a, v3.Zero);
				ldr.Append(i,",",ratio.x,",",ratio.y,",",ratio.z,"\n");
			}
			ldr.ToFile(FP("edge.csv"));
		}
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