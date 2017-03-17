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
		private static bool DebuggingEnabled { get { return Bot.TickNumber >= m_debug_from_tick || Bot.Instrument.Count >= m_debug_from_candle; } }
		private static int m_debug_from_tick;
		private static int m_debug_from_candle;

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
				}
			}
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
		public static void BreakOnPointOfInterest()
		{
			if (m_ticks_of_interest.Contains(Bot.TickNumber))
			{
				m_ticks_of_interest.Remove(Bot.TickNumber);
				Debugger.Break();
			}
			if (Instrument.NewCandle && m_candles_of_interest.Contains(Instrument.Count))
			{
				m_candles_of_interest.Remove(Instrument.Count);
				Debugger.Break();
			}
		}
		private static List<int> m_candles_of_interest = new List<int>();
		private static List<int> m_ticks_of_interest = new List<int>();

		/// <summary>Output a debug message</summary>
		public static void Trace(string message)
		{
			if (!DebuggingEnabled) return;
			using (var f = new StreamWriter(new FileStream(FP("trace.log"), FileMode.Append, FileAccess.Write, FileShare.Read)))
				f.WriteLine(message);
		}

		#region Dump to Ldr File

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(Trade trade, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("Trade"))
			{
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
						var value = (float)trade.ValueNow() / trade.Volume;
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
						var value = (float)trade.ValueNow() / trade.Volume;
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

		/// <summary>Dump an instrument to an ldr file</summary>
		/// <param name="instr">The instrument to be output</param>
		/// <param name="range_">Optional. Sub range within the instrument data (Idx)</param>
		/// <param name="mcs_range">Optional. The number of candles to use to get the median candle size for candle type classification. (default 50)</param>
		/// <param name="high_res">Optional. Add the sub candle ask/bid line. Value is the number of steps within each candle.</param>
		/// <param name="emas">Optional. Add exponential moving average lines, with periods of the values given.</param>
		public static void Dump(Instrument instr, Range? range_ = null, double? high_res = null, int[] emas = null, int[] smas = null, int? mcs_range = null, bool? ema_slope = null, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			// Get the range of candles to output
			var range = instr.IndexRange(range_ ?? new Range(instr.IdxFirst, instr.IdxLast));
			if (range.Empty)
				return;

			// Colours for lines
			var cols = new[] { 0xFFB9D1EA, 0xFF066896, 0xFF0000A0, 0XFFFF2267, 0xFF9FCD38 };
			var coli = 0;

			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position doesn't change with updates
			using (ldr.Group(instr.SymbolCode))
			{
				// Find the MCS
				var mcs = instr.MedianCandleSize(range.Endi - (mcs_range ?? 50), range.Endi);

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
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , h, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f - 0.4f , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f + 0.4f , o, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f - 0.4f , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f + 0.4f , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , c, -0.01f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.5f        , l, -0.01f, 1f), col));

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
					var price_data = instr.HighResRange(range, step);
					if (price_data.Any())
					{
						ldr.Line("ask", Colour32.Yellow.Lerp(AskColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index, (float)x.Ask, 0.05f, 1)));
						ldr.Line("bid", Colour32.Yellow.Lerp(BidColor, 0.5f), 1, false, price_data.Select(x => new v4((float)x.Index, (float)x.Bid, 0.05f, 1)));
					}
				}
				#endregion

				// Add exponential moving average lines
				#region EMA
				if (emas != null)
				{
					var first = (int)(range.Beg - instr.IdxFirst);
					var last  = (int)(range.End - instr.IdxFirst);
					foreach (var p in emas)
					{
						var col = (Colour32)cols[coli++ % cols.Length];
						var ma = new Indicator(instr, instr.Bot.Indicators.ExponentialMovingAverage(instr.Data.Close, p).Result);
						ldr.Line("ema", col, 5, false, range.Select(i => new v4(i - instr.IdxFirst, (float)ma[(int)i], 0.05f, 1f)));
						coli = (coli + 1) % cols.Length;

						//// And extrapolations
						//var extrap = ma.Extrapolate(1, p);
						//if (extrap != null)
						//	ldr.Line("ema_future", col.Alpha(0x40), 5, false, double_.Range(0,5,0.1).Select(x => new v4((float)(x - (double)instr.IdxFirst - p*0.5), (float)extrap[x], 0.05f, 1f)));
					}
				}
				#endregion

				// Add simple moving average lines
				#region SMA
				if (smas != null)
				{
					var first = (int)(range.Beg - instr.IdxFirst);
					var last  = (int)(range.End - instr.IdxFirst);
					foreach (var p in smas)
					{
						var col = (Colour32)cols[coli++ % cols.Length];
						var ma = new Indicator(instr, instr.Bot.Indicators.SimpleMovingAverage(instr.Data.Close, p).Result);
						ldr.Line("sma", col, 5, false, range.Select(i => new v4(i - instr.IdxFirst - p/2, (float)ma[(int)i], 0.05f, 1f)));

						//// And extrapolations
						//var extrap = ma.Extrapolate(1, p);
						//if (extrap != null)
						//	ldr.Line("sma_future", col.Alpha(0x40), 5, false, double_.Range(0,p/2,0.1).Select(x => new v4((float)(x - (double)instr.IdxFirst - p*0.5), (float)extrap[x], 0.05f, 1f)));
					}
				}
				#endregion

				// Current price levels
				#region Current Price
				{
					ldr.Line("ASK", 0xFF00FF00, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Ask, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.LatestPrice.Ask, 0, 1f));
					ldr.Line("BID", 0xFFFF0000, new v4(0 - (int)instr.IdxFirst, (float)instr.Symbol.Bid, 0, 1f), new v4(20 - (int)instr.IdxFirst, (float)instr.LatestPrice.Bid, 0, 1f));
					ldr.Rect("MCS", 0xFFFF80FF, AxisId.PosZ, 1f, (float)mcs, false, new v4(10 - (int)instr.IdxFirst, (float)instr.LatestPrice.Mid, 0, 1f));
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
						ldr.Line("EMASlope", 0xFFA000A0, 1, false, range.Select(x => new v4((int)x - instr.IdxFirst, offset + (float)(scale*instr.EMASlope((Idx)x)), 0, 1)));
						ldr.Line("Zero", 0xFF800080, new v4(range.Begi - instr.IdxFirst, offset, 0, 1), new v4(range.Endi - instr.IdxFirst, offset, 0, 1));
					}
				}
				#endregion

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
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = pp.Instrument;

			using (ldr.Group("PricePeaks_{0}".Fmt(instr.SymbolCode)))
			{
				foreach (var pk in pp.Peaks.Take(20))
				{
					var name = pk.High ? "high" : "low";
					ldr.Ellipse(name, 0x800000FF, AxisId.PosZ, true, 0.25f, (float)(1f*instr.PipSize), new v4(pk.Index - instr.IdxFirst + 0.5f, (float)pk.Price, 0.01f, 1f));
				}

				// Curve fit the peaks
				var curve_hi = pp.TrendHigh;
				if (curve_hi != null)
					ldr.Line("TrendHigh", 0xFF00FF00, 5, false, double_.Range(-60, +5, 0.1).Select(x => new v4((float)(x - (int)instr.IdxFirst), (float)curve_hi.F(x), 0, 1f)));
				var curve_lo = pp.TrendLow;
				if (curve_lo != null)
					ldr.Line("TrendLow", 0xFFFF0000, 5, false, double_.Range(-60, +5, 0.1).Select(x => new v4((float)(x - (int)instr.IdxFirst), (float)curve_lo.F(x), 0, 1f)));
			}
			if (ldr_ == null)
				ldr.ToFile(FP("price_peaks.ldr"));
		}

		/// <summary>Dump a distribution to an ldr file</summary>
		public static void Dump(Distribution distribution, Idx X, QuoteCurrency Y, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
			var ldr = ldr_ ?? new LdrBuilder();

			var rx = distribution.YRange; // The counts per bucket
			var ry = distribution.XRange.Scale(3.0); // The range of prices
			if (ry.Size == 0)
				return;

			// The position to draw the distribution
			var pt = new v2((float)(X - Instrument.IdxFirst), (float)Y);
			using (ldr.Group("Distribution_{0}".Fmt(distribution.Name)))
			{
				// Draw an axis
				ldr.Line("X", Colour32.Black, new v4(pt.x, pt.y, 0, 1), new v4(pt.x + rx.Sizef, pt.y, 0, 1));
				ldr.Line("Y", Colour32.Black, new v4(pt.x, pt.y + ry.Begf, 0, 1), new v4(pt.x, pt.y + ry.Endf, 0, 1));

				// Plot the distribution vertically 
				ldr.Line("dist", 0xFF0000FF, 1, false, double_.Range(ry, ry.Size*0.01).Select(y =>
				{
					var value = distribution[y];
					return new v4((float)(pt.x + value), (float)(pt.y + y), 0, 1);
				}));
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
			if (!DebuggingEnabled) return;
			var filepath = FP("{0}_report.txt".Fmt(correlator.Name));
			File.WriteAllText(filepath, correlator.Report);
		}

		/// <summary>Dump a slope, coloured to indicate trend</summary>
		public static void Slope(Idx idx, double slope, LdrBuilder ldr_ = null)
		{
			if (!DebuggingEnabled) return;
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

		#endregion

		#region Trade Logging

		/// <summary>The list of logged trades</summary>
		private static List<int> m_trade_ids = new List<int>();

		/// <summary>A ldr builder for outputting trades</summary>
		private static LdrBuilder m_ldr_all_trades;

		/// <summary>The index range of trades</summary>
		private static Range m_trades_range;

		/// <summary>A ldr builder for outputting the instrument</summary>
		private static LdrBuilder m_ldr_instr;

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

		/// <summary>Enable/Disable logging trades to an ldr file</summary>
		public static void LogTrades(Rylobot bot, bool enabled)
		{
			if (m_log_trades == enabled) return;

			// Disabled
			if (m_log_trades)
			{
				Trace("TRADE LOGGING DISABLED\n");

				// Unsubscribe
				bot.Positions.Opened -= LogTradeOpened;
				bot.Positions.Closed -= LogTradeClosed;

				m_candles_of_interest.Clear();
				m_ticks_of_interest  .Clear();
				Bot = null;
			}

			m_log_trades = enabled;

			// Enabled
			if (m_log_trades)
			{
				Bot = bot;

				// Reset the debugging files
				ClearFile("all_trades.ldr");
				ClearFile("candle_patterns.ldr");
				m_ldr_all_trades = new LdrBuilder();
				m_trades_range = Range.Invalid;
				m_trade_ids.Clear();

				// Create the instrument being traded
				m_ldr_instr = new LdrBuilder();

				// Sign up for position created/closed events
				bot.Positions.Closed += LogTradeClosed;
				bot.Positions.Opened += LogTradeOpened;

				// Initial instrument output
				LogInstrument();

				// Load debugging settings
				LoadDebuggingSettings();
	
				// Reset the log file
				using (new FileStream(FP("trace.log"), FileMode.Create, FileAccess.Write, FileShare.Read)) {}
				Trace("\nTRADE LOGGING ENABLED");
			}
		}
		private static bool m_log_trades;

		/// <summary>Handle trade created/closed events</summary>
		private static void LogTradeOpened(PositionOpenedEventArgs args)
		{
			LogTrade(args.Position, live:true, update_instrument:true);
		}
		private static void LogTradeClosed(PositionClosedEventArgs args)
		{
			LogTrade(args.Position, live:false, update_instrument:true);
		}

		/// <summary>Output the live trades to a file</summary>
		private static void LogLiveTrades()
		{
			var sb = new LdrBuilder();
			foreach (var p in Rylobot.Instance.Positions)
				sb.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(p.Id));
			foreach (var p in Rylobot.Instance.PendingOrders)
				sb.Append("#include \"trades\\order_{0}.ldr\"\n".Fmt(p.Id));
			sb.ToFile(FP("live_trades.ldr"));
		}

		/// <summary>Add position 'id' to the list of completed trades</summary>
		private static void LogCompleteTrade(int id)
		{
			if (m_trade_ids.Contains(id))
				return;

			m_ldr_all_trades.Append("#include \"trades\\trade_{0}.ldr\"\n".Fmt(id));
			m_ldr_all_trades.ToFile(FP("all_trades.ldr"));
			m_trade_ids.Add(id);
		}

		/// <summary>Update the details of a trade</summary>
		public static void LogTrade(Position pos, bool live, bool update_instrument)
		{
			if (!DebuggingEnabled)
				return;

			// Output the trade as a separate file
			var ldr = new LdrBuilder();
			using (ldr.Group("trade_{0}".Fmt(pos.Id)))
				Dump(new Trade(Instrument, pos, live:live), ldr_:ldr);
			ldr.ToFile(FP("trades\\trade_{0}.ldr".Fmt(pos.Id)));

			// Encompass the range
			m_trades_range.Beg = Math.Min(m_trades_range.Beg, Instrument.Count - 10);
			m_trades_range.End = Math.Max(m_trades_range.End, Instrument.Count + 10);

			// Add to the 'all trades' or 'live trades' file
			if (live)
				LogLiveTrades();
			else
				LogCompleteTrade(pos.Id);

			// Also update the instrument so that it matches the trade
			if (update_instrument)
				LogInstrument();
		}

		/// <summary>Update the details of a pending order</summary>
		public static void LogOrder(PendingOrder ord, bool update_instrument)
		{
			if (!DebuggingEnabled)
				return;

			// Output the order as a separate file
			var ldr = new LdrBuilder();
			using (ldr.Group("order_{0}".Fmt(ord.Id)))
				Dump(new Trade(Instrument, ord), ldr_:ldr);
			ldr.ToFile(FP("trades\\order_{0}.ldr".Fmt(ord.Id)));

			// Update the 'live trades' file
			LogLiveTrades();

			// Also update the instrument so that it matches the trade
			if (update_instrument)
				LogInstrument();
		}

		/// <summary>Output the instrument to a file as it changes</summary>
		public static void LogInstrument(Range? range_ = null, double? high_res = null, int[] emas = null, int[] smas = null, int? mcs_range = null, bool? ema_slope = null)
		{
			if (!DebuggingEnabled)
				return;

			// Dump the instrument data
			Dump(Instrument
				,range_ ?? new Range(-100, 1)
				,high_res
				,emas
				,smas
				,mcs_range
				,ema_slope
				,ldr_:m_ldr_instr);

			// Write the instrument to 'm_ldr_instr'
			m_ldr_instr.ToFile(FP("{0}.ldr".Fmt(Instrument.SymbolCode)));
			m_ldr_instr.Clear();

			// Update the graphics for active trades and orders
			foreach (var pos in Rylobot.Instance.Positions)
				LogTrade(pos, live:true, update_instrument:false);
			foreach (var ord in Rylobot.Instance.PendingOrders)
				LogOrder(ord, update_instrument:false);

			// Update the live trades file
			LogLiveTrades();
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