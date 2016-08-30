using System;
using cAlgo.API;
using cAlgo.API.Internals;

namespace Tradee
{
	public static class CAlgo_
	{
		/// <summary>Convert CAlgo time frames to tradee time frames</summary>
		public static ETimeFrame ToTradeeTimeframe(this TimeFrame tf)
		{
			if (tf == TimeFrame.Minute  ) return ETimeFrame.Min1   ;
			if (tf == TimeFrame.Minute2 ) return ETimeFrame.Min2   ;
			if (tf == TimeFrame.Minute3 ) return ETimeFrame.Min3   ;
			if (tf == TimeFrame.Minute4 ) return ETimeFrame.Min4   ;
			if (tf == TimeFrame.Minute5 ) return ETimeFrame.Min5   ;
			if (tf == TimeFrame.Minute6 ) return ETimeFrame.Min6   ;
			if (tf == TimeFrame.Minute7 ) return ETimeFrame.Min7   ;
			if (tf == TimeFrame.Minute8 ) return ETimeFrame.Min8   ;
			if (tf == TimeFrame.Minute9 ) return ETimeFrame.Min9   ;
			if (tf == TimeFrame.Minute10) return ETimeFrame.Min10  ;
			if (tf == TimeFrame.Minute15) return ETimeFrame.Min15  ;
			if (tf == TimeFrame.Minute20) return ETimeFrame.Min20  ;
			if (tf == TimeFrame.Minute30) return ETimeFrame.Min30  ;
			if (tf == TimeFrame.Minute45) return ETimeFrame.Min45  ;
			if (tf == TimeFrame.Hour    ) return ETimeFrame.Hour1  ;
			if (tf == TimeFrame.Hour2   ) return ETimeFrame.Hour2  ;
			if (tf == TimeFrame.Hour3   ) return ETimeFrame.Hour3  ;
			if (tf == TimeFrame.Hour4   ) return ETimeFrame.Hour4  ;
			if (tf == TimeFrame.Hour6   ) return ETimeFrame.Hour6  ;
			if (tf == TimeFrame.Hour8   ) return ETimeFrame.Hour8  ;
			if (tf == TimeFrame.Hour12  ) return ETimeFrame.Hour12 ;
			if (tf == TimeFrame.Daily   ) return ETimeFrame.Day1   ;
			if (tf == TimeFrame.Day2    ) return ETimeFrame.Day2   ;
			if (tf == TimeFrame.Day3    ) return ETimeFrame.Day3   ;
			if (tf == TimeFrame.Weekly  ) return ETimeFrame.Weekly ;
			if (tf == TimeFrame.Monthly ) return ETimeFrame.Monthly;
			return ETimeFrame.None;
		}

		/// <summary>Convert Tradee time frames to CAlgo time frames (Returns null for no corresponding value)</summary>
		public static TimeFrame ToCAlgoTimeframe(this ETimeFrame tf)
		{
			switch (tf) {
			default: return null;
			case ETimeFrame.Min1   : return TimeFrame.Minute  ;
			case ETimeFrame.Min2   : return TimeFrame.Minute2 ;
			case ETimeFrame.Min3   : return TimeFrame.Minute3 ;
			case ETimeFrame.Min4   : return TimeFrame.Minute4 ;
			case ETimeFrame.Min5   : return TimeFrame.Minute5 ;
			case ETimeFrame.Min6   : return TimeFrame.Minute6 ;
			case ETimeFrame.Min7   : return TimeFrame.Minute7 ;
			case ETimeFrame.Min8   : return TimeFrame.Minute8 ;
			case ETimeFrame.Min9   : return TimeFrame.Minute9 ;
			case ETimeFrame.Min10  : return TimeFrame.Minute10;
			case ETimeFrame.Min15  : return TimeFrame.Minute15;
			case ETimeFrame.Min20  : return TimeFrame.Minute20;
			case ETimeFrame.Min30  : return TimeFrame.Minute30;
			case ETimeFrame.Min45  : return TimeFrame.Minute45;
			case ETimeFrame.Hour1  : return TimeFrame.Hour    ;
			case ETimeFrame.Hour2  : return TimeFrame.Hour2   ;
			case ETimeFrame.Hour3  : return TimeFrame.Hour3   ;
			case ETimeFrame.Hour4  : return TimeFrame.Hour4   ;
			case ETimeFrame.Hour6  : return TimeFrame.Hour6   ;
			case ETimeFrame.Hour8  : return TimeFrame.Hour8   ;
			case ETimeFrame.Hour12 : return TimeFrame.Hour12  ;
			case ETimeFrame.Day1   : return TimeFrame.Daily   ;
			case ETimeFrame.Day2   : return TimeFrame.Day2    ;
			case ETimeFrame.Day3   : return TimeFrame.Day3    ;
			case ETimeFrame.Weekly : return TimeFrame.Weekly  ;
			case ETimeFrame.Monthly: return TimeFrame.Monthly ;
			}
		}

		/// <summary>Convert a CAlgo trade type to a Tradee trade type</summary>
		public static ETradeType ToTradeeTradeType(this TradeType tt)
		{
			switch (tt)
			{
			default:             return ETradeType.None;
			case TradeType.Buy:  return ETradeType.Long;
			case TradeType.Sell: return ETradeType.Short;
			}
		}

		/// <summary>Convert a Tradee trade type to a CAlgo one</summary>
		public static TradeType ToCAlgoTradeType(this ETradeType tt)
		{
			switch (tt)
			{
			default:              throw new Exception("Unknown trade type");
			case ETradeType.None: throw new Exception("Unsupported trade type");
			case ETradeType.Long: return TradeType.Buy;
			case ETradeType.Short: return TradeType.Sell;
			}
		}

		/// <summary>Convert CAlgo error codes to tradee error codes</summary>
		public static EErrorCode ToTradeeErrorCode(this ErrorCode code)
		{
			switch (code)
			{
			default: throw new Exception("Unknown error code");
			case ErrorCode.TechnicalError: return EErrorCode.Failed;
			case ErrorCode.NoMoney:        return EErrorCode.InsufficientFunds;
			case ErrorCode.EntityNotFound: return EErrorCode.EntityNotFound;
			case ErrorCode.BadVolume:      return EErrorCode.InvalidVolume;
			case ErrorCode.MarketClosed:   return EErrorCode.MarketClosed;
			case ErrorCode.Disconnected:   return EErrorCode.Disconnected;
			case ErrorCode.Timeout:        return EErrorCode.Timeout;
			}
		}

		/// <summary>Convert tradee error codes to CAlgo error codes</summary>
		public static ErrorCode ToTradeeErrorCode(this EErrorCode code)
		{
			switch (code)
			{
			default: throw new Exception("Unknown error code");
			case EErrorCode.Failed:            return ErrorCode.TechnicalError;
			case EErrorCode.InsufficientFunds: return ErrorCode.NoMoney;
			case EErrorCode.EntityNotFound:    return ErrorCode.EntityNotFound;
			case EErrorCode.InvalidVolume:     return ErrorCode.BadVolume;
			case EErrorCode.MarketClosed:      return ErrorCode.MarketClosed;
			case EErrorCode.Disconnected:      return ErrorCode.Disconnected;
			case EErrorCode.Timeout:           return ErrorCode.Timeout;
			}
		}

		/// <summary>Return the stop loss as a signed price value relative to the entry price. 0 means no stop loss</summary>
		public static double StopLossRel(this  cAlgo.API.Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.EntryPrice - (pos.StopLoss ?? pos.EntryPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.EntryPrice) - pos.EntryPrice;
			}
		}

		/// <summary>Return the take profit as a signed price value relative to the entry price. 0 means no take profit</summary>
		public static double TakeProfitRel(this  cAlgo.API.Position pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return (pos.TakeProfit ?? pos.EntryPrice) - pos.EntryPrice;
			case TradeType.Sell: return pos.EntryPrice - (pos.TakeProfit ?? pos.EntryPrice);
			}
		}

		/// <summary>Return the stop loss as a signed price value relative to the entry price. 0 means no stop loss</summary>
		public static double StopLossRel(this  cAlgo.API.PendingOrder pos)
		{
			switch (pos.TradeType)
			{
			default: throw new Exception("unknown trade type");
			case TradeType.Buy:  return pos.TargetPrice - (pos.StopLoss ?? pos.TargetPrice);
			case TradeType.Sell: return (pos.StopLoss ?? pos.TargetPrice) - pos.TargetPrice;
			}
		}

		/// <summary>Return the take profit as a signed price value relative to the entry price. 0 means no take profit</summary>
		public static double TakeProfitRel(this  cAlgo.API.PendingOrder pos)
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
	}
}
