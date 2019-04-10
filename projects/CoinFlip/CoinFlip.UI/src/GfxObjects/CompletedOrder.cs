using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class CompletedOrder : IDisposable
	{
		// Notes:
		//  - A cache of gfx objects for completed orders

		private Instrument m_instrument;
		private Cache<Guid, View3d.Object> m_cache;
		public CompletedOrder(Instrument instrument)
		{
			m_instrument = instrument;
			m_cache = new Cache<Guid, View3d.Object>();
		}
		public void Dispose()
		{
			Util.Dispose(ref m_cache);
		}

		/// <summary>Get a graphics object for the given completed order</summary>
		public View3d.Object Get(OrderCompleted his, double xrange, double yrange)
		{
			var gfx = m_cache.Get(his.UniqueKey, k =>
			{
				var buy = his.TradeType == ETradeType.Q2B;
				var col = (buy ? SettingsData.Settings.Chart.AskColour : SettingsData.Settings.Chart.BidColour).LerpNoAlpha(Colour32.Black, 0.5f);
				var ldr =
					$"*Group {(buy ? "buy" : "sell")}_{his.OrderId}\n" +
					$"{{\n" +
					$"	*CylinderHR {col:X8}\n" +
					$"	{{\n" +
					$"		2 1 {(buy ? 1 : 0)} {(buy ? 0 : 1)}\n" +
					$"		*o2w {{ *pos {{ 0 {(buy ? -0.5 : +0.5)} 0 }} }}\n" +
					$"	}}\n" +
					$"	*Text\n" +
					$"	{{\n" +
					$"		*Font {{ *Name {{\"Tahoma\"}} *Size {{10}} *Colour {{ {col:X8} }} }}\n" +
					$"		*BackColour {{ A0FFFFFF }}\n" +
					$"		*CString {{ \"{(buy ? his.AmountBase.ToString("G8", true) : his.AmountQuote.ToString("G8", true))}\\n@ {his.PriceQ2B.ToString("G8", true)}\" }}\n" +
					$"		*Billboard\n" +
					$"		*o2w {{ *pos {{ 0 {(buy ? -2.2 : +2.2)} 0 }} }}\n" +
					$"	}}\n" +
					$"}}\n";
				return new View3d.Object(ldr, false, CandleChart.CtxId, null);
			});

			// Scale the graphics to be independent of x-axis/y-axis range.
			// Since we're scaling we need the 'gfx' model to be at the origin.
			var x = (float)m_instrument.IndexAt(new TimeFrameTime(his.Created, m_instrument.TimeFrame));
			var y = (float)(decimal)his.PriceQ2B;
			gfx.O2P = m4x4.Scale((float)(0.01 * xrange), (float)(0.02*yrange), 1, new v4(x, y, CandleChart.ZOrder.Trades, 1));
			return gfx;
		}
	}
}
