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

		private static readonly Color AskColor = Color.Green;
		private static readonly Color BidColor = Color.Red;
		private static readonly Color[] CandleTypeColour = new []
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

		#region Trade Logging
		public static class Trading
		{
			private static int Number = -1;
			private static LdrBuilder m_ldr = new LdrBuilder();
			private static Scope m_group;

			/// <summary>Begin a new trade</summary>
			public static void Begin()
			{
				++Number;

				m_ldr.Clear();
				m_group = m_ldr.Group("Trade_{0}".Fmt(Number), ScaleTxfm);
			}

			public static void Trade(Trade trade)
			{
				trade.Dump(m_ldr, show_snr:true);
			}

			/// <summary>Add a comment to the current trade ldr file</summary>
			public static void Comment(string comment)
			{
				m_ldr.Comment(comment);
			}

			/// <summary>Add a position to the current trade ldr file</summary>
			public static void Position(Position pos, Rylobot bot)
			{
				pos.Dump(bot, m_ldr);
			}

			/// <summary>Add the SnR levels to the current trade ldr file</summary>
			public static void SnrLevels(SnR snr)
			{
				snr.Dump(m_ldr);
			}

			/// <summary>Add the instrument over the range of a trade</summary>
			public static void Instrument(Instrument instr, Position pos, bool diagnostic = false)
			{
				var range = instr.IndexRange(pos);
				range.Begin -= 100;
				instr.Dump(range, diagnostic, m_ldr);
			}

			/// <summary>End a trade group</summary>
			public static void End(bool write = true)
			{
				Util.Dispose(ref m_group);
				WriteFile();
			}

			/// <summary>Write the current trade ldr file to disk</summary>
			public static void WriteFile()
			{
				m_ldr.ToFile(FP("trade_{0}.ldr".Fmt(Number)));
			}
		}
		#endregion

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

				ldr.Box("profit", 0x4000FF00, w, h_tp, 0.001f, new v4(x, y_tp, -0.01f, 1));
				ldr.Box("loss"  , 0x40FF0000, w, h_sl, 0.001f, new v4(x, y_sl, -0.01f, 1));
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trade.ldr"));
		}

		/// <summary>Dump the instrument to an ldr file</summary>
		public static void Dump(this Instrument instr, Range? range_ = null,  bool diagnostic = false, LdrBuilder ldr_ = null)
		{
			// Note: Drawn with x = 0 == oldest (CAlgo indices) so that the X position does change with updates
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group(instr.SymbolCode, ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				var range = instr.IndexRange(range_ ?? new Range(instr.FirstIdx, instr.LastIdx));

				// Just draw bullish/bearish candles
				if (!diagnostic)
				{
					// Using TriList for the bodies, and LineList for the wicks.
					// So: 6 indices for the body, 4 for the wicks
					//   __|__
					//  |\    |
					//  |  \  |
					//  |____\|
					//     |
					// Dividing the index buffer into [bodies, wicks]

					// Buffers
					var vbuf = new List<View3d.Vertex>(8 * range.Counti);
					var body = new List<ushort>(6 * range.Counti);
					var wick = new List<ushort>(4 * range.Counti);

					// Create the geometry
					for (int i = range.Begini; i != range.Endi; ++i)
					{
						var candle = instr[i];
						var x = (float)(i - instr.FirstIdx);
						var o = (float)Math.Max(candle.Open, candle.Close);
						var h = (float)candle.High;
						var l = (float)candle.Low;
						var c = (float)Math.Min(candle.Open, candle.Close);
						var col = candle.Bullish ? AskColor.ToArgbU() : BidColor.ToArgbU();
						var v = vbuf.Count;

						// Prevent degenerate triangles
						if (o == c)
						{
							o *= 1.000005f;
							c *= 0.999995f;
						}

						// Candle verts
						vbuf.Add(new View3d.Vertex(new v4(x        , h, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x        , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x - 0.4f , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.4f , o, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x - 0.4f , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x + 0.4f , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x        , c, 0f, 1f), col));
						vbuf.Add(new View3d.Vertex(new v4(x        , l, 0f, 1f), col));

						// Candle body
						body.Add((ushort)(v + 3));
						body.Add((ushort)(v + 2));
						body.Add((ushort)(v + 4));
						body.Add((ushort)(v + 4));
						body.Add((ushort)(v + 5));
						body.Add((ushort)(v + 3));

						// Candle wick
						wick.Add((ushort)(v + 0));
						wick.Add((ushort)(v + 1));
						wick.Add((ushort)(v + 6));
						wick.Add((ushort)(v + 7));
					}

					ldr.Mesh(string.Empty, 0xFFFFFFFF, View3d.EGeom.Vert|View3d.EGeom.Colr, vbuf, faces:body, lines:wick);
				}
				else
				{
					// Candle candles by type and show detected trend regions
					for (int i = range.Begini; i != range.Endi; ++i)
					{
						var candle = instr[i];
						var mean_candle_size = instr.MeanCandleSize(i-100, i);
						var x = (float)(i - instr.FirstIdx);

						// Candle coloured by type
						var col = CandleTypeColour[(int)candle.Type(mean_candle_size)];
						ldr.Line(col, new v4(x, (float)candle.Low, 0, 1), new v4(x, (float)candle.High, 0, 1));
						ldr.Box(col, 0.8f, (float)candle.BodyLength, 0.0001f, new v4(x, (float)candle.BodyCentre, 0, 1));

						// Mean candle size
						ldr.Line(0xFFFFFF00, new v4(x, (float)(candle.Centre - mean_candle_size/2), -0.001f, 1f), new v4(x, (float)(candle.Centre + mean_candle_size/2), -0.001f, 1f));

						// Trend strength
						var trend = instr.MeasureTrend(i-5,i);
						var trend_col = (trend > 0 ? Color_.FromArgb(0xFF00FF00) : Color_.FromArgb(0xFFFF0000)).Alpha((float)Math.Abs(trend));
						var trend_sz = (float)(mean_candle_size * 0.1);
						var trend_y = (float)(trend > 0 ? candle.High + 4*trend_sz : candle.Low - 4*trend_sz);
						ldr.Box(trend_col, 0.3f, trend_sz, 0.0001f, new v4(x, trend_y, 0, 1));
					}
				}
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("{0}.ldr".Fmt(instr.SymbolCode)));
		}

		/// <summary>Dump a position to an ldr file</summary>
		public static void Dump(this Position pos, Rylobot bot, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (var instr = new Instrument(bot, pos.SymbolCode))
			using (ldr.Group("order_{0}".Fmt(pos.Id), ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				var sign = pos.TradeType.Sign();
				var entry_price = pos.EntryPrice;
				var x = instr.IndexAt(pos.EntryTime) - instr.FirstIdx;
				{
					var p = (float)entry_price;
					ldr.Box("entry", Color.Blue, 0.3f, (float)(instr.Symbol.PipSize*5), 0.3f, new v4(x, p, 0, 1f));
					ldr.Line("entry", Color.Blue, new v4(x, p, 0f, 1f), new v4(x+100f, p, 0f, 1f));
				}
				if (pos.StopLoss != null)
				{
					var p = (float)(entry_price - sign*pos.StopLossRel());
					ldr.Line("sl", BidColor, new v4(x-100f, p, 0f, 1f), new v4(x+100f, p, 0f, 1f));
				}
				if (pos.TakeProfit != null)
				{
					var p = (float)(entry_price + sign*pos.TakeProfitRel());
					ldr.Line("tp", AskColor, new v4(x-100f, p, 0f, 1f), new v4(x+100f, p, 0f, 1f));
				}
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("order_{0}.ldr".Fmt(pos.Id)));
		}

		/// <summary>Dump the order to an ldr file</summary>
		public static void Dump(this Broker br, Instrument instr, TradeType tt, double? sl, double? tp, LdrBuilder ldr_ = null)
		{
			var ldr = new LdrBuilder();
			using (ldr.Group("order", ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				var sign = tt.Sign();
				var current_price = instr.CurrentPrice(sign);
				var x = instr.Count - 1;
				{
					var p = (float)current_price;
					ldr.Box("entry", Color.Blue, (float)(instr.Symbol.PipSize*5), new v4(0f, p, 0, 1f));
					ldr.Line("entry", Color.Blue, new v4(x-50f, p, 0f, 1f), new v4(x+50f, p, 0f, 1f));
				}
				if (sl != null)
				{
					var p1 = (float)(current_price - sign * instr.Symbol.PipsToQuotePrice(sl.Value));
					ldr.Line("sl"   , BidColor  , new v4(x-50f, p1, 0f, 1f), new v4(x+50f, p1, 0f, 1f));
				}
				if (tp != null)
				{
					var p2 = (float)(current_price + sign * instr.Symbol.PipsToQuotePrice(tp.Value));
					ldr.Line("tp"   , AskColor  , new v4(x-50f, p2, 0f, 1f), new v4(x+50f, p2, 0f, 1f));
				}
			}
			Ldr.Write(ldr.ToString(), FP("order.ldr"));
		}

		/// <summary>Dump the SnR data to an ldr file</summary>
		public static void Dump(this SnR snr, LdrBuilder ldr_ = null)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			var instr = snr.Instrument;

			using (ldr.Group("SnR_{0}".Fmt(instr.SymbolCode), ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				// Stationary points
				//' const float scale = 0.25f;
				//' foreach (var sp in snr.StationaryPoints)
				//' 	ldr.Ellipse("pt", Color.Blue, +3, false, 1f*scale, (float)snr.Hysteresis*scale,
				//' 		new v4((float)sp.Index - instr.FirstIdx, (float)sp.Price, 0f, 1f));

				// SnR levels
				for (int i = 0, iend = snr.SnRLevels.Count/3; i != iend; ++i)
					ldr.Line("lvl", Color.Yellow.Alpha(1f - (float)i/iend),
						new v4((float)(snr.Range.Begini - instr.FirstIdx), (float)snr.SnRLevels[i].Price, 0f, 1f),
						new v4((float)(snr.Range.Endi   - instr.FirstIdx), (float)snr.SnRLevels[i].Price, 0f, 1f));
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("snr.ldr"));
		}

		/// <summary>Dump a trade to an ldr file</summary>
		public static void Dump(this Trade trade, LdrBuilder ldr_ = null, bool show_snr = false)
		{
			var ldr = ldr_ ?? new LdrBuilder();
			using (ldr.Group("Trade", ldr_ == null ? ScaleTxfm : m4x4.Identity))
			{
				var sign    = trade.Type.Sign();
				var x0      = (float)(trade.EntryIndex);
				var x1      = (float)(trade.ExitIndex);
				var w       = (float)(trade.ExitIndex - trade.EntryIndex);
				var xmid    = (float)(trade.EntryIndex + w/2);

				ldr.Line("EP", 0xFF0000FF, new v4(x0, (float)trade.EP, 0f, 1f), new v4(x1, (float)trade.EP, 0f, 1f));
				ldr.Line("TP", 0xFF00FF00, new v4(x0, (float)trade.TP, 0f, 1f), new v4(x1, (float)trade.TP, 0f, 1f));
				ldr.Line("SL", 0xFFFF0000, new v4(x0, (float)trade.SL, 0f, 1f), new v4(x1, (float)trade.SL, 0f, 1f));

				var col_pp = trade.Result == Trade.EResult.HitTP ? 0x8000FF00 : 0x4000FF00;
				var col_pl = trade.Result == Trade.EResult.HitSL ? 0x80FF0000 : 0x40FF0000;
				var y_pp = (float)(trade.EP + sign * trade.PeakProfit / 2);
				var y_pl = (float)(trade.EP - sign * trade.PeakLoss   / 2);
				ldr.Rect("peak_profit", col_pp, AxisId.PosZ, w, (float)trade.PeakProfit, true, new v4(xmid, y_pp, -0.01f, 1));
				ldr.Rect("peak_loss"  , col_pl, AxisId.PosZ, w, (float)trade.PeakLoss  , true, new v4(xmid, y_pl, -0.01f, 1));

				if (show_snr)
				{
					var idx_max = trade.Instrument.FirstIdx + trade.EntryIndex;
					var idx_min = idx_max - trade.Bot.Settings.LookBackCount;
					var snr = new SnR(trade.Instrument, idx_min, idx_max);
					foreach (var lvl in snr.SnRLevels)
						ldr.Line(0x40FFFF00,
							new v4(idx_min - trade.Instrument.FirstIdx, (float)lvl.Price, 0f, 1f),
							new v4(idx_max - trade.Instrument.FirstIdx, (float)lvl.Price, 0f, 1f));
				}
			}
			if (ldr_ == null)
				Ldr.Write(ldr.ToString(), FP("trade.ldr"));
		}

		/// <summary>Dump the results of a collection of predictors to a file</summary>
		public static void Dump(this IEnumerable<Predictor> preds)
		{
			// CSV
			var sb = new StringBuilder();
			foreach (var pred in preds)
			{
				sb.AppendLine("Index,RR,TP,SL,RR_sd,TP_sd,SL_sd");
				foreach (var x in pred.Results)
					sb.AppendLine("{0},{1},{2},{3},{4},{5},{6}".Fmt(
						x.Index,
						x.RR.Mean,
						x.TP.Mean,
						x.SL.Mean,
						x.RR.PopStdDev,
						x.TP.PopStdDev,
						x.SL.PopStdDev));

				File.WriteAllText(FP("pred.csv"), sb.ToString());
			}
			
			//var ldr = new LdrBuilder();
			//foreach (var pred in preds)
			//{
			//	ldr.Graph("Pred_{0}".Fmt(pred.Name), AxisId.PosZ, pred.Results.Select(x => new v4(x.Index, (float)x.RR.Mean, 0f, 1f)));
			//}
			//Ldr.Write(ldr.ToString(), FP("pred.ldr"));
		}
	}
}
