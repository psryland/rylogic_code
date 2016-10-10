using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;

namespace Rylobot
{
	/// <summary>Extension methods for CAlgo types</summary>
	public static class CAlgo
	{
		/// <summary>Return the stop loss as a signed price value relative to the entry price. 0 means no stop loss</summary>
		public static double StopLossRel(this Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.EntryPrice - (pos.StopLoss ?? pos.EntryPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.EntryPrice) - pos.EntryPrice;
			}
		}

		/// <summary>Return the take profit as a signed price value relative to the entry price. 0 means no take profit</summary>
		public static double TakeProfitRel(this Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return (pos.TakeProfit ?? pos.EntryPrice) - pos.EntryPrice;
			case TradeType.Sell: return pos.EntryPrice - (pos.TakeProfit ?? pos.EntryPrice);
			}
		}

		/// <summary>Return the stop loss as a signed price value relative to the entry price. 0 means no stop loss</summary>
		public static double StopLossRel(this PendingOrder pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.TargetPrice - (pos.StopLoss ?? pos.TargetPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.TargetPrice) - pos.TargetPrice;
			}
		}

		/// <summary>Return the take profit as a signed price value relative to the entry price. 0 means no take profit</summary>
		public static double TakeProfitRel(this PendingOrder pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return (pos.TakeProfit ?? pos.TargetPrice) - pos.TargetPrice;
			case TradeType.Sell: return pos.TargetPrice - (pos.TakeProfit ?? pos.TargetPrice);
			}
		}

		/// <summary>Convert a price in base currency to pips</summary>
		public static double PriceToPips(this Symbol sym, double price)
		{
			return price / sym.PipSize;
		}

		/// <summary>Return the gradient at 'index'</summary>
		public static double Gradient(this IndicatorDataSeries series, int index)
		{
			if (index == 0) return 0.0;
			return series[index] - series[index-1];
		}

		/// <summary>Return the opposite trade type to this type</summary>
		public static TradeType Opposite(this TradeType tt)
		{
			return tt == TradeType.Buy ? TradeType.Sell : TradeType.Buy;
		}
	}
}
