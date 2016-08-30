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
}
