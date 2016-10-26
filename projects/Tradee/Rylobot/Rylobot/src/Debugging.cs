using System.Diagnostics;
using System.Drawing;
using cAlgo.API;
using pr.extn;
using pr.ldr;
using pr.maths;

namespace Rylobot
{
	public static class Debugging
	{
		private const string Dir = "D:\\dump\\forex\\";
		private static readonly Color AskColor = Color.Green;
		private static readonly Color BidColor = Color.Red;

		/// <summary>Dump the instrument to an ldr file</summary>
		public static void Dump(this Instrument instr)
		{
			var ldr = new LdrBuilder();
			using (ldr.Group(instr.SymbolCode, m4x4.Scale(1f, 1000f, 1f, v4.Origin)))
			{
				for (int i = 0, iend = instr.Count; i != iend; ++i)
				{
					var candle = instr[-i];
					var col = candle.Bullish ? AskColor : BidColor;
					ldr.Line(col, new v4(-i, (float)candle.Low, 0, 1), new v4(-i, (float)candle.High, 0, 1));
					ldr.Box(col, 0.8f, (float)candle.BodyLength, 0.0001f, new v4(-i, (float)candle.BodyCentre, 0, 1));
				}
			}
			Ldr.Write(ldr.ToString(), Dir+"{0}.ldr".Fmt(instr.SymbolCode));
		}

		/// <summary>Dump the order to an ldr file</summary>
		public static void DumpOrder(this Broker br, Instrument instr, TradeType tt, double sl, double tp)
		{
			var ldr = new LdrBuilder();
			using (ldr.Group("order", m4x4.Scale(1f, 1000f, 1f, v4.Origin)))
			{
				var sign = tt.Sign();
				var current_price = instr.CurrentPrice(sign);
				var p0 = (float)current_price;
				var p1 = (float)(current_price - sign * instr.Symbol.PipsToQuotePrice(sl));
				var p2 = (float)(current_price + sign * instr.Symbol.PipsToQuotePrice(tp));

				ldr.Line("entry", Color.Blue, new v4(-50f, p0, 0f, 1f), new v4(+50f, p0, 0f, 1f));
				ldr.Line("sl"   , BidColor  , new v4(-50f, p1, 0f, 1f), new v4(+50f, p1, 0f, 1f));
				ldr.Line("tp"   , AskColor  , new v4(-50f, p2, 0f, 1f), new v4(+50f, p2, 0f, 1f));
			}
			Ldr.Write(ldr.ToString(), Dir+"order.ldr");
		}

		/// <summary>Dump the SnR data to an ldr file</summary>
		public static void Dump(this SnR snr)
		{
			var ldr = new LdrBuilder();
			using (ldr.Group("SnR_{0}".Fmt(snr.Instrument.SymbolCode), m4x4.Scale(1f, 1000f, 1f, v4.Origin)))
			{
				// Stationary points
				const float scale = 1f;
				foreach (var sp in snr.StationaryPoints)
					ldr.Ellipse("pt", Color.Blue, +3, false, 1f*scale, (float)snr.Hysteresis*scale,
						new v4((float)sp.Index, (float)sp.Price, 0f, 1f));

				// SnR levels
				for (int i = 0, iend = snr.SnRLevels.Count/3; i != iend; ++i)
					ldr.Line("lvl", Color.Yellow.Alpha(1f - (float)i/iend),
						new v4((float)(snr.Range.Begini), (float)snr.SnRLevels[i].Price, 0f, 1f),
						new v4((float)(snr.Range.Endi  ), (float)snr.SnRLevels[i].Price, 0f, 1f));
			}
			Ldr.Write(ldr.ToString(), Dir+"snr.ldr");
		}
	}
}
