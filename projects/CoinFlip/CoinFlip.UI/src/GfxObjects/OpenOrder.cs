using System;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class OpenOrder : IDisposable
	{
		// Notes:
		//  - A cache of gfx objects for open orders

		private Instrument m_instrument;
		private Cache<Guid, View3d.Object> m_cache;
		public OpenOrder(Instrument instrument)
		{
			m_instrument = instrument;
			m_cache = new Cache<Guid, View3d.Object>();
		}
		public void Dispose()
		{
			Util.Dispose(ref m_cache);
		}

		/// <summary>Get a graphics object for the given open order</summary>
		public View3d.Object Get(Order ord, double xrange, double yrange)
		{
			var gfx = m_cache.Get(ord.UniqueKey, k =>
			{
				var buy = ord.TradeType == ETradeType.Q2B;
				var col = (buy ? SettingsData.Settings.Chart.AskColour : SettingsData.Settings.Chart.BidColour).LerpNoAlpha(Colour32.Black, 0.5f);
				var ldr =
					$"*Group {(buy ? "buy" : "sell")}_{ord.OrderId}\n" +
					$"{{\n" +
					$"	*Line price {col:X8}\n" +
					$"	{{\n" +
					$"		0 0 0 1 0 0\n" +
					$"	}}\n" +
					$"	*CylinderHR {col:X8}\n" +
					$"	{{\n" +
					$"		2 1 {(buy ? 1 : 0)} {(buy ? 0 : 1)}\n" +
					$"		*o2w {{ *pos {{ 0 {(buy ? -0.5 : +0.5)} 0 }} }}\n" +
					$"	}}\n" +
					$"	*Text\n" +
					$"	{{\n" +
					$"		*Font {{ *Name {{\"Tahoma\"}} *Size {{10}} *Colour {{ {col:X8} }} }}\n" +
					$"		*BackColour {{ A0FFFFFF }}\n" +
					$"		*CString {{ \"{(buy ? ord.AmountBase.ToString("G8", true) : ord.AmountQuote.ToString("G8", true))}\\n@ {ord.PriceQ2B.ToString("G8", true)}\" }}\n" +
					$"		*Billboard\n" +
					$"		*o2w {{ *pos {{ 0 {(buy ? -2.2 : +2.2)} 0 }} }}\n" +
					$"	}}\n" +
					$"}}\n";
				return new View3d.Object(ldr, false, CandleChart.CtxId, null);
			});

			// Scale the graphics to be independent of x-axis/y-axis range.
			// Since we're scaling we need the 'gfx' model to be at the origin.
			var x0 = (float)m_instrument.IndexAt(new TimeFrameTime(ord.Created ?? Model.UtcNow, m_instrument.TimeFrame));
			var x1 = (float)m_instrument.IndexAt(new TimeFrameTime(Model.UtcNow, m_instrument.TimeFrame));
			var y = (float)(decimal)ord.PriceQ2B;
			gfx.O2PSet(m4x4.Scale(x1 - x0, 1, 1, v4.Origin), name: "price");
			gfx.O2P = m4x4.Scale((float)(0.01 * xrange), (float)(0.02 * yrange), 1, new v4(x0, y, CandleChart.ZOrder.Trades, 1));
			return gfx;
		}
	}
}
