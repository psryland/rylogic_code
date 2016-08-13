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
	}

	public static class SqlExpr
	{
		/// <summary>Insert or replace a candle in table 'time_frame'</summary>
		public static string InsertCandle(ETimeFrame time_frame)
		{
			return Str.Build(
				"insert or replace into ",time_frame," (",
				"[",nameof(Candle.Timestamp),"],",
				"[",nameof(Candle.Open     ),"],",
				"[",nameof(Candle.High     ),"],",
				"[",nameof(Candle.Low      ),"],",
				"[",nameof(Candle.Close    ),"],",
				"[",nameof(Candle.Volume   ),"]) ",
				"values (?,?,?,?,?,?)");
		}

		/// <summary>Return the properties of a candle to match an InsertCandle sql statement</summary>
		public static IEnumerable<object> InsertCandleParams(Candle candle)
		{
			return new object[] { candle.Timestamp, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume };
		}
	}
}
