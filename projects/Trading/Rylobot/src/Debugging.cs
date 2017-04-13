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
	public class Debugging :IDisposable
	{
		// Notes;
		// Cannot be static, the bot runs multiple instances during optimisation
		// Output using CAlgo index positions so that trades can have fixed positions

		#region Constants
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
		#endregion

		public Debugging(Rylobot bot)
		{
			Bot = bot;
		}
		public virtual void Dispose()
		{
			Bot = null;
		}

		/// <summary>The bot</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Stopping -= HandleBotStopping;
					m_bot.Instrument.DataChanged -= HandleInstrumentDataChanged;
					DumpInstrument -= m_bot.Dump;
				}
				m_bot = value;
				if (m_bot != null)
				{
					LoadDebuggingSettings();
					m_bot.Instrument.DataChanged += HandleInstrumentDataChanged;
					m_bot.Stopping += HandleBotStopping;
					DumpInstrument += m_bot.Dump;
				}
			}
		}
		public Rylobot m_bot;

		/// <summary>Master switch</summary>
		public bool DebuggingEnabled
		{
			get { return m_debugging_enabled || Bot.TickNumber >= m_debug_from_tick || Bot.Instrument.Count >= m_debug_from_candle; }
			set
			{
				if (m_debugging_enabled == value) return;
				m_debugging_enabled = value;
				if (m_debugging_enabled)
				{
					// Reset the debugging files
					ClearFile(FP("all_trades.ldr"));
					ClearFile(FP("live_trades.ldr"));
					ClearFile(FP("trades.ldr"));
					ClearFile(FP("area_of_interest.ldr"));
					ClearFile(FP("snr.ldr"));
					ClearFile(FP("distribution.ldr"));
					ClearFile(FP("wins.log"));
					ClearFile(FP("losses.log"));
				}
			}
		}
		private int m_debug_from_tick;
		private int m_debug_from_candle;
		private bool m_debugging_enabled;
		private bool m_extrap_indicators;

		/// <summary>Trace switch</summary>
		public bool TraceEnabled
		{
			get { return m_trace_enabled || DebuggingEnabled; }
			set
			{
				if (m_trace_enabled == value) return;
				m_trace_enabled = value;
				if (m_trace_enabled)
				{
					ClearFile(FP("trace.log"));
				}
			}
		}
		private bool m_trace_enabled;

		/// <summary>Report edge switch</summary>
		public bool ReportsEnabled
		{
			get { return m_reports_enabled; }
			set { m_reports_enabled = value; }
		}
		private bool m_reports_enabled;

		/// <summary>Dump graphics at the end</summary>
		private bool m_bot_stopping;
		private bool m_at_end_dump_instrument;
		private bool m_at_end_dump_trades;

		/// <summary>Full path for a dump file</summary>
		public string FP(string fname)
		{
			const string Dir = @"P:\dump\trading";
			return Path_.CombinePath(Dir, fname);
		}

		/// <summary>Read settings from the settings file</summary>
		public void LoadDebuggingSettings()
		{
			// Set defaults;
			LogTrades                = false;
			TraceEnabled             = false;
			ReportsEnabled           = false;
			m_debugging_enabled      = false;
			m_extrap_indicators      = false;
			m_bot_stopping           = false;
			m_at_end_dump_instrument = false;
			m_at_end_dump_trades     = false;

			m_debug_from_tick   = int.MaxValue;
			m_debug_from_candle = int.MaxValue;
			m_candles_of_interest .Clear();
			m_ticks_of_interest   .Clear();

			// Look for candles/ticks of interest
			var filepath = FP("debug_settings.txt");
			if (!Path_.FileExists(filepath))
				return;

			// Parse settings
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
				case "log_trades":             LogTrades                = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "debugging_enabled":      DebuggingEnabled         = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "trace_enabled":          TraceEnabled             = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "report_enabled":         ReportsEnabled           = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "extrap_indicators":      m_extrap_indicators      = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "at_end_dump_instrument": m_at_end_dump_instrument = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "at_end_dump_trades":     m_at_end_dump_trades     = pair[1].HasValue() ? bool.Parse(pair[1]) : false; break;
				case "debug_from_tick":        m_debug_from_tick        = pair[1].HasValue() ? int.Parse(pair[1]) : int.MaxValue; break;
				case "debug_from_candle":      m_debug_from_candle      = pair[1].HasValue() ? int.Parse(pair[1]) : int.MaxValue; break;
				case "break_candles":          m_candles_of_interest .AddRange(int_.ParseArray(pair[1])); break;
				case "break_ticks":            m_ticks_of_interest   .AddRange(int_.ParseArray(pair[1])); break;
				}
			}
		}

		/// <summary>The instrument being logged</summary>
		private Instrument Instrument
		{
			[DebuggerStepThrough] get { return Bot.Instrument; }
		}

		/// <summary>The set of all trades</summary>
		public Dictionary<int, Trade> AllTrades { get; private set; }

		/// <summary>The index range of trades</summary>
		public Range TradesRange { get; private set; }

		/// <summary>Raised to dump the instrument state</summary>
		public event Action DumpInstrument;

		/// <summary>Reset a file to empty</summary>
		public void ClearFile(string fname)
		{
			new LdrBuilder().ToFile(FP(fname));
		}

		/// <summary>Break the debugger on the start of certain candles</summary>
		public void BreakOnPointOfInterest()
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
		private List<int> m_candles_of_interest = new List<int>();
		private List<int> m_ticks_of_interest = new List<int>();

		/// <summary>Output a debug message</summary>
		public void Trace(string message)
		{
			if (!TraceEnabled) return;
			using (var f = new StreamWriter(new FileStream(FP("trace.log"), FileMode.Append, FileAccess.Write, FileShare.Read)))
				f.WriteLine(message);
		}

		/// <summary>Enable/Disable logging trades to an ldr file</summary>
		public bool LogTrades
		{
			get { return m_log_trades; }
			set
			{
				if (m_log_trades == value) return;
				if (m_log_trades)
				{
					// Unsubscribe
					Bot.PositionOpened -= LogTradeOpened;
					Bot.PositionClosed -= LogTradeClosed;
					m_bot.Stopping -= LogTradeBotStopping;

					Trace("TRADE LOGGING DISABLED\n");
				}
				m_log_trades = value;
				if (m_log_trades)
				{
					Trace("\nTRADE LOGGING ENABLED");

					TradesRange = Range.Invalid;

					// Trades that have been seen before
					AllTrades = new Dictionary<int, Trade>();

					// Sign up for position created/closed events
					Bot.PositionClosed += LogTradeClosed;
					Bot.PositionOpened += LogTradeOpened;
					m_bot.Stopping += LogTradeBotStopping;

					// Initial instrument output
					LogInstrument();
				}
			}
		}
		private bool m_log_trades;

		/// <summary>Handle trade created/closed events</summary>
		private void LogTradeOpened(object sender, PositionOpenedEventArgs args)
		{
			var position = args.Position;
			LogTrade(position, live:true, update_instrument:true);
			Trace("Idx={0},Tick={1} - Position {2} Opened - {3} EP={4} Volume={5} {6}".Fmt(
				Instrument.Count, Bot.TickNumber, position.Id, position.TradeType, position.EntryPrice,
				position.Volume, position.Comment ?? string.Empty));
		}
		private void LogTradeClosed(object sender, PositionClosedEventArgs args)
		{
			var position = args.Position;
			LogTrade(position, live:false, update_instrument:true);
			Trace("Idx={0},Tick={1} - Position {2} Closed - {3} EP={4} Volume={5} Profit=${6} Equity=${7} {8}".Fmt(
				Instrument.Count, Bot.TickNumber, position.Id, position.TradeType, position.EntryPrice,
				position.Volume, position.NetProfit, Bot.Account.Equity, position.Comment));
		}
		private void LogTradeBotStopping(object sender, EventArgs e)
		{
			foreach (var pos in Bot.Positions)
				LogTrade(pos, live:false, update_instrument:false);
		}

		/// <summary>Update the details of a trade</summary>
		public void LogTrade(Trade trade, bool update_instrument = true)
		{
			if (!LogTrades && !ReportsEnabled && !DebuggingEnabled)
				return;

			// Record the trade
			var new_trade = !AllTrades.ContainsKey(trade.Id);
			if (!trade.IsPending)
				AllTrades[trade.Id] = trade;

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
		public void LogTrade(Position pos, bool live, bool update_instrument = true)
		{
			LogTrade(new Trade(Instrument, pos, live?Trade.EResult.Open:Trade.EResult.Closed), update_instrument);
		}

		/// <summary>Update the details of a pending order</summary>
		public void LogOrder(PendingOrder ord, bool update_instrument = true)
		{
			LogTrade(new Trade(Instrument, ord), update_instrument);
		}

		/// <summary>Update the live trades file</summary>
		private void LogLiveTrades()
		{
			var ldr = new LdrBuilder();
			//foreach (var t in AllTrades.Values.Where(x => x.IsLive))
			//	ldr.Append("#include \"trades\\{0}.ldr\"\n".Fmt(t.Name));
			foreach (var p in Bot.Positions)     ldr.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(p.Id));
			foreach (var p in Bot.PendingOrders) ldr.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(p.Id));
			ldr.ToFile(FP("live_trades.ldr"));
		}

		/// <summary>Update the all trades file</summary>
		private void LogAllTrades()
		{
			var ldr = new LdrBuilder();
			foreach (var t in AllTrades.Values.Where(x => !x.IsLive))
				ldr.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(t.Id));
			ldr.ToFile(FP("all_trades.ldr"));
		}

		/// <summary>Output the instrument to a file as it changes</summary>
		public void LogInstrument()
		{
			if (DebuggingEnabled)
			{
				// Dump the instrument data
				DumpInstrument.Raise();

				// Update the graphics for active trades and orders
				foreach (var pos in Bot.Positions)     LogTrade(pos, live:true, update_instrument:false);
				foreach (var ord in Bot.PendingOrders) LogOrder(ord, update_instrument:false);

				// Update the live trades file
				LogLiveTrades();
			}
		}

		/// <summary>Output the edge of all trades out to 'periods' candles after the trade entry</summary>
		public void ReportEdge(int periods)
		{
			if (!ReportsEnabled)
				return;

			Trace("Edge {0} - Based on {1} entry signals".Fmt(periods, AllTrades.Count));

			// Measure the excursion for 'periods' candles after trade entry
			var mae = Util.NewArray<AvrVar>(periods);
			var mfe = Util.NewArray<AvrVar>(periods);
			foreach (var trade in AllTrades.Values)
			{
				var excursion = trade.MaxExcursionNormalised(periods);
				for (int i = 0; i != periods; ++i)
				{
					mae[i].Add(excursion[i].Beg);
					mfe[i].Add(excursion[i].End);
				}
			}

			// Plot MFE/MAE vs. periods
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

		/// <summary>Dump the results of the correlator</summary>
		public void ReportStats(TradeStats stats)
		{
			if (!ReportsEnabled)
				return;

			// Output the stats summary
			{
				var filepath = FP("trade_stats.txt");
				File.WriteAllText(filepath, stats.Report);
			}

			// Output CSV data for each predictor
			foreach (var p in Bot.TradeStats.Predictors.Values)
				p.Distribution().ToCSV().ToFile(FP("Stats\\{0}.csv".Fmt(p.Name)));

			//foreach (var f in stats.Predictors)
			//{
			//	var factor_name = f.Key;
			//	var trade_id_to_value_map = f.Value;

			//	// Compile the stats for this factor
			//	var results = new Dictionary<double, TradeStats.ResultStats>();
			//	foreach (var g in trade_id_to_value_map)
			//	{
			//		var trade_id = g.Key;
			//		var value    = Maths.Quantise(g.Value, Instrument.PipSize);

			//		var s = results.GetOrAdd(value);
			//		var rec = stats.Records[trade_id];
			//		if (rec.Success > 0) { ++s.Wins;   s.WinAmount  += (double)rec.NetProfit; }
			//		if (rec.Success	< 0) { ++s.Losses; s.LossAmount += (double)rec.NetProfit; }
			//	}

			//	// Output the results as CSV data
			//	var sb = new StringBuilder();
			//	sb.AppendLine("Factor Value, Win Ratio, Avr $/Trade");
			//	foreach (var r in results.OrderBy(x => x.Key))
			//	{
			//		var value = r.Key;
			//		var s = r.Value;
			//		sb.AppendLine("{0},{1},{2}".Fmt(value, s.WinRatioPC, s.AvrAmountPerTrade));
			//	}
			//	var filepath = FP("Stats\\{0}.csv".Fmt(factor_name));
			//	sb.ToFile(filepath);
			//}
		}

		#region Dump to Ldr File

		/// <summary>Output the current price levels</summary>
		public void CurrentPrice(Instrument instr, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			var mcs = instr.MCS;

			ldr.Line("ASK", 0xFF00FF00, new v4(instr.IdxNow - instr.IdxFirst, (float)instr.Symbol.Ask, 0, 1f), new v4(20.0 - instr.IdxFirst, (float)instr.LatestPrice.Ask, 0, 1f));
			ldr.Line("BID", 0xFFFF0000, new v4(instr.IdxNow - instr.IdxFirst, (float)instr.Symbol.Bid, 0, 1f), new v4(20.0 - instr.IdxFirst, (float)instr.LatestPrice.Bid, 0, 1f));
			ldr.Rect("MCS", 0xFFFF80FF, AxisId.PosZ, 1f, (float)mcs, true, new v4(10.0 - instr.IdxFirst, (float)instr.LatestPrice.Mid, 0, 1f));
			
			if (ldr_ == null)
				ldr.ToFile(FP("{0}_currentprice.ldr".Fmt(instr.SymbolCode)));
		}

		/// <summary>Dump an instrument to an ldr file</summary>
		/// <param name="instr">The instrument to be output</param>
		/// <param name="range">Optional. Sub range within the instrument data (Idx)</param>
		/// <param name="high_res">Optional. Add the sub candle ask/bid line. Value is the number of steps within each candle.</param>
		/// <param name="indicators">Optional. Add moving average indicators.</param>
		/// <param name="mcs_range">Optional. The number of candles to use to get the median candle size for candle type classification. (default 50)</param>
		public void Dump(Instrument instr, RangeF? range = null, double? high_res = null, Indicator[] indicators = null, bool? ema_slope = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the range of candles to output
			range = (range != null && !m_bot_stopping) ? range : new RangeF(instr.IdxFirst, instr.IdxLast);
			var rng = instr.IndexRange(range.Value);
			if (rng.Empty)
				return;

			// Colours for lines
			var cols = new[]
			{
				0xFF001b30, 0xFF013861, 0xFF0071c1, 0xFF66a8d8, 0xFFcce2f0, // Blues
				0xFF371851, 0xFF542478, 0xFFaa83c6, 0xFFc6acd9, 0xFFe3d5ec, // Purples
				0xFF40e0d0, 0xFF0070c0, 0xFF7030a0, 0XFFFF2267, 0xFF9FCD38,
			};
			var coli = 0;

			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position doesn't change with updates
			using (ldr.Group(instr.SymbolCode))
			{
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
					const int MaxVertsPerModel = 65520;
					const int MaxCandlesPerModel = MaxVertsPerModel/8;
					var vbuf = new List<View3d.Vertex>(MaxCandlesPerModel * 8);
					var faces = new List<ushort>(MaxCandlesPerModel * 6);
					var lines = new List<ushort>(MaxCandlesPerModel * 4);

					// Break the model up into 65535 vert blocks
					for (int i = rng.Begi; i != rng.Endi;)
					{
						vbuf .Clear();
						faces.Clear();
						lines.Clear();

						// Create the candle geometry
						for (int iend = Math.Min(i + MaxCandlesPerModel, rng.Endi); i != iend; ++i)
						{
							var candle = instr[i];
							var mcs = instr.EMATrueRange(i - 20, i);
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
				}
				#endregion

				// Volume
				#region Volume
				{

				}
				#endregion

				// Add the higher resolution price line
				#region High Res
				if (high_res != null)
				{
					var step = high_res.Value != 0 ? (double?)1.0/high_res.Value : null;
					var price_data = instr.HighRes.Range(rng, step);
					if (price_data.Any())
					{
						ldr.Line("ask", Colour32.Yellow.Lerp(AskColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index - 0.5f, (float)x.Ask, 0.05f, 1)));
						ldr.Line("bid", Colour32.Yellow.Lerp(BidColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index - 0.5f, (float)x.Bid, 0.05f, 1)));
					}

					// Extrapolate high res data
					var extrap = instr.HighRes.Extrapolate(5);
					if (extrap != null)
						ldr.Line("future", 0xFF804000, 3, false, double_.Range(-5, +5, 0.1)
							.Select(i => new v4(i - 0.5 - instr.IdxFirst, (float)extrap[i], 0.05f, 1f)));
				}
				#endregion

				// Add indicator lines
				#region Indicators
				if (indicators != null)
				{
					foreach (var indicator in indicators)
					{
						for (int s = 0; s != indicator.SourceCount; ++s)
						{
							var col = (Colour32)cols[coli++ % cols.Length];
							coli = (coli + 1) % cols.Length;

							// Draw the indicator
							ldr.Line("indi", col, 3, false, rng
								.Where(i => Maths.IsFinite(indicator[i,series:s]))
								.Select(i => new v4(i - instr.IdxFirst, (float)indicator[i,series:s], -0.005f, 1f)));

							// Extrapolations
							if (m_extrap_indicators)
							{
								var extrap = indicator.Extrapolate(2, indicator.ExtrapHistory, series:s);
								if (extrap != null)
								{
									ldr.Line("ma_future", col.Alpha(0x40), 3, false, double_
										.Range(-indicator.ExtrapHistory, +5, 0.1)
										.Where(i => Maths.IsFinite(extrap[i]))
										.Select(i => new v4(i - instr.IdxFirst, (float)extrap[i], -0.005f, 1f)));
								}
							}
						}
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
		public void Dump(Trade trade, LdrBuilder ldr_ = null)
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
				var colr    = trade.Result == Trade.EResult.Open ? 0x400000FFU : 0x4000007FU;
				var sl_colr = trade.Result == Trade.EResult.Open ? 0x40FF0000U : 0x407F0000U;
				var tp_colr = trade.Result == Trade.EResult.Open ? 0x4000FF00U : 0x40007F00U;

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
				case Trade.EResult.LimitOrder:
				case Trade.EResult.StopEntryOrder:
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
		public void Dump(IEnumerable<Trade> trades, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			foreach (var trade in trades)
				Dump(trade, ldr_:ldr);

			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trades.ldr"));
		}

		/// <summary>Dump a collection of candles</summary>
		public void Dump(IList<Candle> candles, QuoteCurrency mcs, float alpha = 1.0f, LdrBuilder ldr_ = null)
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
		public void Dump(SnR snr, LdrBuilder ldr_ = null)
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
				var candles = snr.Instrument.BatchedCandleRange(snr.Beg, snr.End, snr.TimeFrame).ToArray();
				Dump(candles, snr.Instrument.MCS, alpha:0.2f, ldr_:ldr);
			}
			if (ldr_ == null)
				ldr.ToFile(FP("snr.ldr"));
		}

		/// <summary>Dump the price peaks to an ldr file</summary>
		public void Dump(PricePeaks pp, LdrBuilder ldr_ = null)
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
		public void Dump(Distribution distribution, Idx X, double[] prob = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			var rx = distribution.YRange; // The counts per bucket
			var ry = distribution.XRange.Inflate(3.0); // The range of prices
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
		public void Dump(IPolynomial poly, string name, Colour32 colour, RangeF range, double step = 0.1, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			if (poly != null)
				ldr.Line(name, colour, 5, false, double_.Range(range, 0.1).Select(x => new v4((float)(x - (double)Instrument.IdxFirst), (float)poly.F(x), 0.05f, 1f)));

			if (ldr_ == null)
				ldr.ToFile(FP("poly_{0}.ldr".Fmt(name)));
		}

		/// <summary>Dump an indicator</summary>
		public void Dump(Indicator indi, Range? range = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the range to output
			var rng = range ?? new Range((int)indi.IdxFirst, (int)indi.IdxLast);
			if (rng.Empty)
				return;

			// Colours for lines
			var cols = new[] { 0xFFB9D1EA, 0xFF066896, 0xFF0000A0, 0XFFFF2267, 0xFF9FCD38 };
			var coli = 0;

			var instr = indi.Instrument;
			var yrange = RangeF.Invalid;
			for (var s = 0; s != indi.SourceCount; ++s)
			{
				var col = (Colour32)cols[coli++ % cols.Length];
				coli = (coli + 1) % cols.Length;

				ldr.Line("indi_{0}".Fmt(s), col, 5, false, rng
					.Select(i => new v4(i - indi.IdxFirst, (float)indi[i,s], 0, 1f))
					.Concat(     new v4(instr.IdxNow - instr.IdxFirst, (float)indi[instr.IdxNow, s], 0, 1f)));

				yrange.Encompass(rng.Min(i => indi[i,s]));
				yrange.Encompass(rng.Max(i => indi[i,s]));

				//// Extrapolations
				//var extrap = ma.Future;
				//if (extrap != null)
				//	ldr.Line("ma_future", col.Alpha(0x40), 5, false, double_.Range(-5, +5, 0.1)
				//		.Select(i => new v4(i - instr.IdxFirst, (float)extrap[i], -0.005f, 1f)));
			}

			// Axes
			ldr.Line("XAxis", 0xFF000000, new v4(rng.Beg - indi.IdxFirst, 0, 0, 1), new v4(rng.End - indi.IdxFirst, 0, 0, 1));
			ldr.Line("YAxis", 0xFF000000, new v4(0, yrange.Begf, 0, 1), new v4(0, yrange.Endf, 0, 1));

			if (ldr_ == null)
				ldr.ToFile(FP("indicator_{0}.ldr".Fmt(indi.Name)));
		}

		/// <summary>Dump a slope, coloured to indicate trend</summary>
		public void Slope(Idx idx, double slope, LdrBuilder ldr_ = null)
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

		/// <summary>Draw a box around the candles in the range [beg,end)</summary>
		public void AreaOfInterest(Idx beg, Idx end, Colour32? col = null, bool? solid = null, bool? append = null)
		{
			if (!DebuggingEnabled)
				return;

			var w = new RangeF(beg - Instrument.IdxFirst, beg - Instrument.IdxFirst);
			var h = RangeF.Invalid;
			foreach (var c in Instrument.CandleRange(beg, end))
			{
				h.Encompass(c.Low);
				h.Encompass(c.High);
				w.End += 1.0;
			}

			col = col ?? 0xFF0000FF;

			var ldr = new LdrBuilder();
			ldr.Rect("box", col.Value, AxisId.PosZ, w.Sizef, h.Sizef, solid ?? false, new v4(w.Midf, h.Midf, 0f, 1f));
			ldr.ToFile(FP("area_of_interest.ldr"), append:append ?? true);
		}
		public void AreaOfInterest(RangeF rng, Colour32? col = null, bool? solid = null, bool? append = null)
		{
			AreaOfInterest(rng.Beg, rng.End, col, solid, append);
		}

		/// <summary>Dump arbitrary graphics to a file</summary>
		public void Dump(string filename, Action<LdrBuilder> func)
		{
			if (!DebuggingEnabled) return;
			var ldr = new LdrBuilder();
			func(ldr);
			ldr.ToFile(FP(filename));
		}

		#endregion

		/// <summary>Write debugging output when the instrument changes</summary>
		private void HandleInstrumentDataChanged(object sender, DataEventArgs e)
		{
			if (!DebuggingEnabled)
				return;

			// Limit dumps to once / second
			if (Environment.TickCount - m_last_instr_dump < 1000) return;
			m_last_instr_dump = Environment.TickCount;

			// Output the instrument
			LogInstrument();
		}
		private int m_last_instr_dump = 0;

		/// <summary>Handle the bot stopping</summary>
		private void HandleBotStopping(object sender, EventArgs e)
		{
			m_bot_stopping = true;
			if (ReportsEnabled)
			{
				ReportEdge(100);
				ReportStats(Bot.TradeStats);
			}
			if (m_at_end_dump_instrument)
			{
				DebuggingEnabled = true;
				DumpInstrument.Raise();
			}
			if (m_at_end_dump_trades)
			{
				DebuggingEnabled = true;
				Dump(AllTrades.Values);
			}
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
		public void Dump(double entry_index, double exit_index, double ep, double sl, double tp, LdrBuilder ldr_ = null)
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
		public void Dump(this Position pos, Instrument instr, LdrBuilder ldr_ = null, bool show_snr = false)
		{
			var trade = new Trade(instr, pos);
			trade.Dump(ldr_, show_snr);
		}

#endif