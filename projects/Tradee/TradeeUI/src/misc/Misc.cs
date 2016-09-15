using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.extn;

namespace Tradee
{
	public static class Extn
	{
		/// <summary>True if the stop loss level can be changed for an order in this state</summary>
		public static bool CanMoveEntryPrice(this Trade.EState state)
		{
			return state == Trade.EState.Visualising || state == Trade.EState.PendingOrder;
		}

		/// <summary>True if the stop loss level can be changed for an order in this state</summary>
		public static bool CanMoveStopLoss(this Trade.EState state)
		{
			return state == Trade.EState.Visualising || state == Trade.EState.PendingOrder || state == Trade.EState.ActivePosition;
		}

		/// <summary>True if the stop loss level can be changed for an order in this state</summary>
		public static bool CanMoveTakeProfit(this Trade.EState state)
		{
			return state == Trade.EState.Visualising || state == Trade.EState.PendingOrder || state == Trade.EState.ActivePosition;
		}

		/// <summary>True if the stop loss level can be changed for an order in this state</summary>
		public static bool CanMoveEntryTime(this Trade.EState state)
		{
			return state == Trade.EState.Visualising || state == Trade.EState.PendingOrder;
		}

		/// <summary>True if the stop loss level can be changed for an order in this state</summary>
		public static bool CanMoveExpiry(this Trade.EState state)
		{
			return state == Trade.EState.Visualising || state == Trade.EState.PendingOrder;
		}

		/// <summary>True if this is a possible indecision (reversal or continuation) candle type</summary>
		public static bool IsIndecision(this Candle.EType type)
		{
			return type == Candle.EType.Doji || type == Candle.EType.SpinningTop || type == Candle.EType.Hammer || type == Candle.EType.InvHammer;
		}

		/// <summary>True if this is a trend indicating candle type</summary>
		public static bool IsTrend(this Candle.EType type)
		{
			return type == Candle.EType.Marubozu || type == Candle.EType.MarubozuStrengthening || type == Candle.EType.MarubozuWeakening;
		}
	}
}
