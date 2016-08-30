using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.extn;

namespace Tradee
{
	/// <summary>Handle messages received from Tradee</summary>
	public partial class TradeeBotModel
	{
		/// <summary>Handle messages from Tradee</summary>
		private void DispatchMsg(ITradeeMsg msg)
		{
			switch (msg.ToMsgType())
			{
			default: Debug.WriteLine("Unknown Message Type {0} received".Fmt(msg.GetType().Name)); break;
			case EMsgType.HelloClient:              HandleMsg((OutMsg.HelloClient             )msg); break;
			case EMsgType.RequestAccountStatus:     HandleMsg((OutMsg.RequestAccountStatus    )msg); break;
			case EMsgType.RequestInstrument:        HandleMsg((OutMsg.RequestInstrument       )msg); break;
			case EMsgType.RequestInstrumentStop:    HandleMsg((OutMsg.RequestInstrumentStop   )msg); break;
			case EMsgType.RequestInstrumentHistory: HandleMsg((OutMsg.RequestInstrumentHistory)msg); break;
			case EMsgType.RequestTradeHistory:      HandleMsg((OutMsg.RequestTradeHistory     )msg); break;
			case EMsgType.PlaceMarketOrder:         HandleMsg((OutMsg.PlaceMarketOrder        )msg); break;
			}
		}

		/// <summary>Handle the hello message from Tradee</summary>
		private void HandleMsg(OutMsg.HelloClient msg)
		{
			// snob!
		}

		/// <summary>Handle the request for account data</summary>
		private void HandleMsg(OutMsg.RequestAccountStatus req)
		{
			// Send the account info if the id matches.
			var acct = GetAccount();
			if (!req.AccountId.HasValue() || req.AccountId == acct.Number.ToString())
			{
				SendAccountStatus();
				SendCurrentPositions();
				SendPendingPositions();
			}
		}

		/// <summary>Handle a request to start sending instrument data</summary>
		private void HandleMsg(OutMsg.RequestInstrument req)
		{
			// Add or select the associated transmitter, and ensure the time frames are being sent
			var trans = AddTransmitter(req.SymbolCode);
			trans.TimeFrames = trans.TimeFrames.Concat(req.TimeFrame).Distinct().ToArray();

			// Ensure data is sent, even if it hasn't changed
			trans.ForcePost();
		}

		/// <summary>Handle a request to stop sending instrument data</summary>
		private void HandleMsg(OutMsg.RequestInstrumentStop req)
		{
			// Find the associated transmitter
			var trans = FindTransmitter(req.SymbolCode);
			if (trans == null)
				return;

			// Remove the specified time frame
			if (req.TimeFrame == ETimeFrame.None)
				trans.TimeFrames = new ETimeFrame[0];
			else
				trans.TimeFrames = trans.TimeFrames.Except(req.TimeFrame).ToArray();
		}

		/// <summary>Handle a request to send a range of historic instrument data</summary>
		private void HandleMsg(OutMsg.RequestInstrumentHistory req)
		{
			// Add or select the associated transmitter
			var trans = AddTransmitter(req.SymbolCode);

			// Add the time range requests for historic data
			foreach (var range in req.TimeRanges.InPairs())
				trans.HistoricDataRequests.Add(new HistoricDataRequest(
					req.TimeFrame.ToCAlgoTimeframe(),
					new DateTimeOffset(range.Item1, TimeSpan.Zero),
					new DateTimeOffset(range.Item2, TimeSpan.Zero)));

			// Add the index range requests for historic data
			// Tradee using negative index ranges (i.e. (oldest,newest) = [-Count,0]) but CAlgo uses
			// inverted positive indices, 0 = now, Count = oldest.
			foreach (var range in req.IndexRanges.InPairs())
				trans.HistoricDataRequests.Add(new HistoricDataRequest(
					req.TimeFrame.ToCAlgoTimeframe(),
					-range.Item1,
					-range.Item2));
		}

		/// <summary>Handle a request to send the history of trades</summary>
		private void HandleMsg(OutMsg.RequestTradeHistory req)
		{
			SendTradeHistory(req.TimeBegUTC, req.TimeEndUTC);
		}

		/// <summary>Handle a request to place a market order</summary>
		private void HandleMsg(OutMsg.PlaceMarketOrder req)
		{
			// Ignore invalid requests
			if (req.Order.TradeType == ETradeType.None)
				return;

			// Get the symbol to be traded
			var sym = GetSymbol(req.Order.SymbolCode);

			// Place the immediate order
			var result = (TradeResult)null;
			if (req.Position != null)
			{
				// Note: CAlgo does not take an entry price for immediate orders. It must
				// assume the price of the received message and only reject it if the order cannot
				// be filled within 'MarketRangePips' of the price at the time the order is received.
				var position = req.Position;
				result = CAlgo.ExecuteMarketOrder(
					position.TradeType.ToCAlgoTradeType(),
					sym,
					position.Volume,
					position.Label,
					position.StopLossRel   > 0 ? sym.PriceToPips(position.StopLossRel)   : (double?)null,
					position.TakeProfitRel > 0 ? sym.PriceToPips(position.TakeProfitRel) : (double?)null,
					req.MarketRangePips,
					position.Comment);
			}
			// Place the limit order
			else if (req.PendingLimit != null)
			{
				var pending = req.PendingLimit;
				result = CAlgo.PlaceLimitOrder(
					pending.TradeType.ToCAlgoTradeType(),
					sym,
					pending.Volume,
					pending.EntryPrice,
					pending.Label,
					pending.StopLossRel   > 0 ? sym.PriceToPips(pending.StopLossRel  ) : (double?)null,
					pending.TakeProfitRel > 0 ? sym.PriceToPips(pending.TakeProfitRel) : (double?)null,
					pending.ExpirationTime != 0 ? new DateTime(pending.ExpirationTime, DateTimeKind.Utc) : (DateTime?)null,
					pending.Comment);
			}
			// Place the stop order
			else if (req.PendingStop != null)
			{
				var pending = req.PendingStop;
				result = CAlgo.PlaceStopOrder(
					pending.TradeType.ToCAlgoTradeType(),
					sym,
					pending.Volume,
					pending.EntryPrice,
					pending.Label,
					pending.StopLossRel   > 0 ? sym.PriceToPips(pending.StopLossRel  ) : (double?)null,
					pending.TakeProfitRel > 0 ? sym.PriceToPips(pending.TakeProfitRel) : (double?)null,
					pending.ExpirationTime != 0 ? new DateTime(pending.ExpirationTime, DateTimeKind.Utc) : (DateTime?)null,
					pending.Comment);
			}

			// Send the result
			Post(new InMsg.MarketOrderChangeResult(result.IsSuccessful, result.Error != null ? result.Error.Value.ToTradeeErrorCode() : EErrorCode.NoError));

			// Send an update of the current/pending positions
			SendCurrentPositions();
			SendPendingPositions();
		}

		/// <summary>Send details of the current account</summary>
		private void SendAccountStatus()
		{
			var data = GetAccount();
			var acct = new Account();
			acct.AccountId             = data.Number.ToString();
			acct.BrokerName            = data.BrokerName;
			acct.Currency              = data.Currency;
			acct.Balance               = data.Balance;
			acct.Equity                = data.Equity;
			acct.FreeMargin            = data.FreeMargin;
			acct.IsLive                = data.IsLive;
			acct.Leverage              = data.Leverage;
			acct.Margin                = data.Margin;
			acct.UnrealizedGrossProfit = data.UnrealizedGrossProfit;
			acct.UnrealizedNetProfit   = data.UnrealizedNetProfit;

			// Only send differences
			if (acct.Equals(m_last_account))
				return;

			if (Post(new InMsg.AccountUpdate(acct)))
				m_last_account = acct;
		}
		private Account m_last_account = new Account();

		/// <summary>Send details about the currently active positions held</summary>
		private void SendCurrentPositions()
		{
			// Collect the currently active positions
			var list = new List<Position>();
			foreach (var x in CAlgo.Positions)
				list.Add(new Position
				{
					Id            = x.Id,
					SymbolCode    = x.SymbolCode,
					TradeType     = x.TradeType.ToTradeeTradeType(),
					EntryTime     = x.EntryTime.ToUniversalTime().Ticks,
					EntryPrice    = x.EntryPrice,
					Pips          = x.Pips,
					StopLossRel   = x.StopLossRel(),
					TakeProfitRel = x.TakeProfitRel(),
					Quantity      = x.Quantity,
					Volume        = x.Volume,
					GrossProfit   = x.GrossProfit,
					NetProfit     = x.NetProfit,
					Commissions   = x.Commissions,
					Swap          = x.Swap,
					Label         = x.Label,
					Comment       = x.Comment,
				});

			// Only send differences
			if (list.SequenceEqual(m_last_positions))
				return;

			// Send Update
			var arr = list.ToArray();
			if (Post(new InMsg.PositionsUpdate(arr)))
				m_last_positions = arr;
		}
		private Position[] m_last_positions = new Position[0];

		/// <summary>Send details about pending orders</summary>
		private void SendPendingPositions()
		{
			// Collect the pending orders
			var list = new List<PendingOrder>();
			foreach (var x in CAlgo.PendingOrders)
				list.Add(new PendingOrder
				{
					Id             = x.Id,
					SymbolCode     = x.SymbolCode,
					TradeType      = x.TradeType.ToTradeeTradeType(),
					ExpirationTime = x.ExpirationTime != null ? x.ExpirationTime.Value.ToUniversalTime().Ticks : 0,
					EntryPrice     = x.TargetPrice,
					StopLossRel    = x.StopLossRel(),
					TakeProfitRel  = x.TakeProfitRel(),
					Quantity       = x.Quantity,
					Volume         = x.Volume,
					Label          = x.Label,
					Comment        = x.Comment,
				});

			// Only send differences
			if (list.SequenceEqual(m_last_pending_orders))
				return;

			// Post update
			var arr = list.ToArray();
			if (Post(new InMsg.PendingOrdersUpdate(arr)))
				m_last_pending_orders = arr;
		}
		private PendingOrder[] m_last_pending_orders = new PendingOrder[0];

		/// <summary>Send trade history between the given time range</summary>
		private void SendTradeHistory(long time_beg, long time_end)
		{
			var history = GetHistory();
			var old_orders = new List<ClosedOrder>(history.Count);
			for (int i = 0; i != history.Count; ++i)
			{
				var trade = history[i];

				// Only trades that overlap the time range
				if (trade.ClosingTime.Ticks < time_beg || trade.EntryTime.Ticks > time_end)
					continue;

				old_orders.Add(new ClosedOrder
				{
					Id           = trade.PositionId,
					SymbolCode   = trade.SymbolCode,
					TradeType    = trade.TradeType.ToTradeeTradeType(),
					Balance      = trade.Balance,
					EntryTimeUTC = trade.EntryTime.Ticks,
					ExitTimeUTC  = trade.ClosingTime.Ticks,
					Pips         = trade.Pips,
					EntryPrice   = trade.EntryPrice,
					ExitPrice    = trade.ClosingPrice,
					Quantity     = trade.Quantity,
					Volume       = trade.Volume,
					GrossProfit  = trade.GrossProfit,
					NetProfit    = trade.NetProfit,
					Commissions  = trade.Commissions,
					Swap         = trade.Swap,
					Label        = trade.Label,
					Comment      = trade.Comment,
				});
			}

			Post(new InMsg.HistoryData(old_orders.ToArray()));
		}
	}
}
