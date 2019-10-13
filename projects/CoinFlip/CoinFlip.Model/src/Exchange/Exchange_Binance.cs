using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.API;
using Binance.API.DomainObjects;
using CoinFlip.Settings;
using ExchApi.Common;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Binance :Exchange
	{
		private readonly HashSet<TradePair> m_pairs;

		public Binance(CoinDataList coin_data, CancellationToken shutdown)
			: this(string.Empty, string.Empty, coin_data, shutdown)
		{
			ExchSettings.PublicAPIOnly = true;
		}
		public Binance(string key, string secret, CoinDataList coin_data, CancellationToken shutdown)
			: base(SettingsData.Settings.Binance, coin_data, shutdown)
		{
			m_pairs = new HashSet<TradePair>();
			Api = new BinanceApi(key, secret, shutdown, Model.Log);
		}
		protected override void Dispose(bool _)
		{
			base.Dispose(_);
			Api = null;
		}

		/// <summary>The API interface</summary>
		private BinanceApi Api
		{
			[DebuggerStepThrough]
			get { return m_api; }
			set
			{
				if (m_api == value) return;
				if (m_api != null)
				{
					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					m_api.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
				}
			}
		}
		private BinanceApi m_api;
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// Get all available trading pairs
			// Use 'Shutdown' because updating pairs is independent of the Exchange.UpdateThread
			// and we don't want updating pairs to be interrupted by the update thread stopping
			var server_rules = await Api.ServerRules(cancel: Shutdown.Token);

			// Filter out the coins we don't care about
			server_rules.Symbols.RemoveAll(x =>
				!(coins.Contains(x.BaseAsset) && coins.Contains(x.QuoteAsset)) ||
				x.Status != ESymbolStatus.TRADING);

			// Add an action to integrate the data
			Model.DataUpdates.Add(() =>
			{
				var pairs = new HashSet<TradePair>();

				// Create the trade pairs and associated coins
				foreach (var p in server_rules.Symbols)
				{
					var pair = Pairs.GetOrAdd(p.BaseAsset, p.QuoteAsset);
					pairs.Add(pair);
				}

				// Ensure a 'Balance' object exists for each coin type
				foreach (var c in Coins)
					Balance.GetOrAdd(c);

				// Record the pairs
				lock (m_pairs)
				{
					m_pairs.Clear();
					m_pairs.AddRange(pairs);
				}
			});
		}

		/// <summary>Update account balance data</summary>
		protected override Task UpdateBalancesInternal(HashSet<string> coins) // Worker thread context
		{
			// Request the account data
			var balance_data = Api.UserData.Balances;

			// Ignore coins that aren't in the settings or don't qualify for auto add.
			var auto_add_coins = SettingsData.Settings.AutoAddCoins;
			balance_data.Balances.RemoveAll(x => !(coins.Contains(x.Asset) || (auto_add_coins && x.Total != 0)));

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Ignore out of date data
				if (balance_data.UpdateTime < Balance.LastUpdated)
					return;

				// Update the account balance
				foreach (var b in balance_data.Balances)
				{
					// Find the currency that this balance is for
					var coin = Coins.GetOrAdd(b.Asset);

					// Update the balance
					Balance.ExchangeUpdate(coin, b.Total._(coin), b.Locked._(coin), balance_data.UpdateTime);
				}

				// Notify updated
				Balance.LastUpdated = balance_data.UpdateTime;
			});
			return Task.CompletedTask;
		}

		/// <summary>Update open orders and completed trades</summary>
		protected override Task UpdateOrdersAndHistoryInternal(HashSet<string> coins) // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Get the array of pairs to query for
			List<TradePair> pairs;
			lock (m_pairs)
				pairs = m_pairs.ToList();

			var auto_add_coins = SettingsData.Settings.AutoAddCoins;

			// Request all the existing orders
			var existing_orders = new List<global::Binance.API.DomainObjects.Order>();
			foreach (var pair in pairs)
			{
				if (Shutdown.IsCancellationRequested) return Task.CompletedTask;
				if (!auto_add_coins && (!coins.Contains(pair.Base) || !coins.Contains(pair.Quote))) continue;

				var cp = new CurrencyPair(pair.Base.Symbol, pair.Quote.Symbol);
				var orders = Api.UserData.Orders[cp];
				existing_orders.AddRange(orders);
			}

			// Request all the existing completed orders since 'm_history_last'
			var history_orders = new List<OrderFill>();
			foreach (var pair in pairs)
			{
				if (Shutdown.IsCancellationRequested) return Task.CompletedTask;
				if (!auto_add_coins && (!coins.Contains(pair.Base) || !coins.Contains(pair.Quote))) continue;

				var cp = new CurrencyPair(pair.Base.Symbol, pair.Quote.Symbol);
				var history = Api.UserData.History[cp, m_history_last];
				history_orders.AddRange(history);
			}

			existing_orders.Sort(x => x.Created);
			history_orders.Sort(x => x.Created);

			// Record the time that history has been updated to
			m_history_last = timestamp;

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				//var order_ids = new HashSet<long>();
				var orders = new List<Order>();
				var new_pairs = new HashSet<TradePair>();

				// Update the trade history
				foreach (var exch_order in history_orders)
				{
					// Update the history of completed orders
					var fill = TradeCompletedFrom(exch_order, timestamp);
					AddToTradeHistory(fill);
				}

				// Update the collection of existing orders
				foreach (var exch_order in existing_orders)
				{
					// Get/Add the order
					var order = orders.Add2(OrderFrom(exch_order, timestamp));
					new_pairs.Add(order.Pair);
				}
				SynchroniseOrders(orders, timestamp);

				// Merge pairs that have trades into the 'm_pairs' set
				lock (m_pairs)
					m_pairs.AddRange(new_pairs);

				// Notify updated
				History.LastUpdated = timestamp;
				Orders.LastUpdated = timestamp;
			});
			return Task.CompletedTask;
		}

		/// <summary>Update the market data</summary>
		protected override Task UpdateDataInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Get the array of pairs to query for
			List<TradePair> pairs;
			lock (m_pairs)
				pairs = m_pairs.ToList();

			// Get the ticker info for each pair
			var ticker_updates = new List<PairAndTicker>();
			foreach (var pair in pairs)
			{
				if (Shutdown.IsCancellationRequested) return Task.CompletedTask;
				var cp = new CurrencyPair(pair.Base.Symbol, pair.Quote.Symbol);
				var ticker = Api.TickerData[cp];
				if (ticker != null)
					ticker_updates.Add(new PairAndTicker { Pair = cp, Ticker = ticker });
			}

			// Stop market updates for unreferenced pairs
			var pairs_cached = Api.MarketData.Cached.ToHashSet(0);
			foreach (var pair in pairs)
			{
				var cp = new CurrencyPair(pair.Base.Symbol, pair.Quote.Symbol);
				if (pairs_cached.Contains(cp) && !pair.MarketDepth.IsNeeded)
					Api.MarketData.Forget(cp);
			}

			// For each pair, get the updated market depth info.
			var depth_updates = new List<PairAndMarketData>();
			foreach (var pair in pairs)
			{
				// If no one needs the market data, ignore it.
				if (!pair.MarketDepth.IsNeeded) continue;

				// Get the latest market data for 'pair'
				var cp = new CurrencyPair(pair.Base.Symbol, pair.Quote.Symbol);
				var ob = Api.MarketData[cp];

				// Convert to CoinFlip market data format
				var rate_units = $"{pair.Quote}/{pair.Base}";
				var b2q = ob.B2QOffers.Select(x => new Offer(x.PriceQ2B._(rate_units), x.AmountBase._(pair.Base))).ToArray();
				var q2b = ob.Q2BOffers.Select(x => new Offer(x.PriceQ2B._(rate_units), x.AmountBase._(pair.Base))).ToArray();
				depth_updates.Add(new PairAndMarketData { Pair = cp, B2Q = b2q, Q2B = q2b });
			}

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Update the spot price on each pair
				foreach (var upd in ticker_updates)
				{
					var pair = Pairs[upd.Pair.Base, upd.Pair.Quote];
					if (pair == null)
						continue;

					pair.SpotPrice[ETradeType.Q2B] = upd.Ticker.PriceQ2B._(pair.RateUnits);
					pair.SpotPrice[ETradeType.B2Q] = upd.Ticker.PriceB2Q._(pair.RateUnits);
				}

				// Process the order book data and update the pairs
				foreach (var update in depth_updates)
				{
					// Find the pair to update.
					var pair = Pairs[update.Pair.Base, update.Pair.Quote];
					if (pair == null)
						continue;

					// Update the depth of market data
					pair.MarketDepth.UpdateOrderBooks(update.B2Q, update.Q2B);
				}

				// Notify updated
				Pairs.LastUpdated = timestamp;
			});
			return Task.CompletedTask;
		}

		/// <summary>Return the deposits and withdrawals made on this exchange</summary>
		protected async override Task UpdateTransfersInternal(HashSet<string> coins) // worker thread context
		{
			var timestamp = DateTimeOffset.Now;

			// Request the transfers data
			var deposits = await Api.GetDeposits(beg: m_transfers_last, cancel: Shutdown.Token);
			var withdrawals = await Api.GetWithdrawals(beg: m_transfers_last, cancel: Shutdown.Token);

			// Remove transfers we don't care about
			if (!SettingsData.Settings.AutoAddCoins)
			{
				deposits.RemoveAll(x => !coins.Contains(x.Asset));
				withdrawals.RemoveAll(x => !coins.Contains(x.Asset));
			}

			// Record the time that transfer history has been updated to
			m_transfers_last = timestamp;

			// Queue integration of the transfer history
			Model.DataUpdates.Add(() =>
			{
				// Update the collection of funds transfers
				foreach (var dep in deposits)
				{
					var deposit = TransferFrom(dep);
					AddToTransfersHistory(deposit);
				}
				foreach (var wid in withdrawals)
				{
					var withdrawal = TransferFrom(wid);
					AddToTransfersHistory(withdrawal);
				}

				// Notify updated
				Transfers.LastUpdated = timestamp;
			});
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected override Task<List<Candle>> CandleDataInternal(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			// Get the chart data
			var cp = new CurrencyPair(pair.Base, pair.Quote);
			var data = Api.CandleData[cp, ToMarketPeriod(timeframe), time_beg, time_end, cancel];
			//var data = await Api.GetChartData(cp, ToMarketPeriod(timeframe), time_beg, time_end, cancel);

			// Convert it to candles
			var candles = data.Select(x => new Candle(x.Time.Ticks, x.Open, x.High, x.Low, x.Close, x.Median, x.Volume)).ToList();
			return Task.FromResult(candles);
		}

		/// <summary>Open a trade</summary>
		protected override async Task<OrderResult> CreateOrderInternal(Trade trade, CancellationToken cancel)
		{
			try
			{
				// Create trade parameters
				var cp = new CurrencyPair(trade.Pair.Base, trade.Pair.Quote);
				var p = new OrderParams(trade.TradeType.ToBinanceTT(), trade.OrderType.ToBinanceOT())
				{
					AmountBase = trade.AmountBase,
					PriceQ2B = trade.OrderType != EOrderType.Market ? trade.PriceQ2B : (decimal?)null,
					StopPriceQ2B = trade.OrderType == EOrderType.Stop ? trade.PriceQ2B : (decimal?)null,
				};// shouldn't need to .Canonicalise(cp, Api);

				// Place the trade order
				var res = await Api.SubmitTrade(cp, p, cancel);
				return new OrderResult(trade.Pair, res.OrderId, filled: false);
			}
			catch (Exception ex)
			{
				throw new Exception($"Binance: Submit trade failed. {ex.Message}\n{trade.TradeType} Pair: {trade.Pair.Name}  Amt: {trade.AmountIn.ToString(8, true)} -> {trade.AmountOut.ToString(8, true)} @  {trade.PriceQ2B.ToString(8, true)}", ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected override async Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			try
			{
				var cp = new CurrencyPair(pair.Base, pair.Quote);

				// Look up the Binance ClientOrderId for 'order_id'
				var cid = Api.UserData.Orders[cp].FirstOrDefault(x => x.OrderId == order_id)?.ClientOrderId;
				if (cid == null)
					throw new Exception($"Could not find an order with this order id ({order_id})");

				// Cancel the order
				var res = await Api.CancelTrade(cp, cid, cancel);
				return res.Status == EOrderStatus.CANCELED;
			}
			catch (Exception ex)
			{
				throw new Exception($"Binance: Cancel trade failed. {ex.Message}\nOrder Id: {order_id } Pair: {pair.Name}", ex);
			}
		}

		/// <summary>Enumerate all candle data and time frames provided by this exchange</summary>
		protected override IEnumerable<PairAndTF> EnumAvailableCandleDataInternal(TradePair pair)
		{
			if (pair != null)
			{
				var cp = new CurrencyPair(pair.Base, pair.Quote);
				if (!Pairs.ContainsKey(pair.UniqueKey)) yield break;
				foreach (var mp in Enum<EMarketPeriod>.Values)
					yield return new PairAndTF(pair, ToTimeFrame(mp));
			}
			else
			{
				foreach (var p in Pairs)
					foreach (var mp in Enum<EMarketPeriod>.Values)
						yield return new PairAndTF(p, ToTimeFrame(mp));
			}
		}

		/// <summary>Adjust the values in 'trade' to be within accepted exchange ranges</summary>
		protected override Trade CanonicaliseInternal(Trade trade)
		{
			var p = new OrderParams(trade.TradeType.ToBinanceTT(), trade.OrderType.ToBinanceOT())
			{
				AmountBase = trade.AmountBase,
				PriceQ2B = trade.OrderType != EOrderType.Market ? trade.PriceQ2B : (decimal?)null,
				StopPriceQ2B = trade.OrderType == EOrderType.Stop ? trade.PriceQ2B : (decimal?)null,
			}.Canonicalise(new CurrencyPair(trade.Pair.Base, trade.Pair.Quote), Api);

			// Update 'trade' with canonical values
			trade.PriceQ2B = p.PriceQ2B.Value._(trade.Pair.RateUnits);
			if (trade.TradeType == ETradeType.Q2B)
			{
				trade.AmountOut = p.AmountBase._(trade.Pair.Base);
				trade.AmountIn = (p.AmountBase * p.PriceQ2B.Value)._(trade.Pair.Quote);
			}
			if (trade.TradeType == ETradeType.B2Q)
			{
				trade.AmountIn = p.AmountBase._(trade.Pair.Base);
				trade.AmountOut = (p.AmountBase * p.PriceQ2B.Value)._(trade.Pair.Quote);
			}
			return trade;
		}

		/// <summary>Convert an exchange order into a CoinFlip order</summary>
		private Order OrderFrom(global::Binance.API.DomainObjects.Order order, DateTimeOffset updated)
		{
			var order_id = order.OrderId;
			var fund_id = OrderIdToFund(order_id);
			var ot = Misc.OrderType(order.OrderType, order.IsWorking);
			var tt = Misc.TradeType(order.OrderSide);
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var amount_in = tt.AmountIn(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var amount_out = tt.AmountOut(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var remaining_in = amount_in;
			var created = order.Created;
			return new Order(order_id, fund_id, pair, ot, tt, amount_in, amount_out, remaining_in, created, updated);
		}

		/// <summary>Convert an exchange order into a CoinFlip order completed</summary>
		private TradeCompleted TradeCompletedFrom(OrderFill fill, DateTimeOffset updated)
		{
			var order_id = fill.OrderId;
			var trade_id = fill.TradeId;
			var pair = Pairs.GetOrAdd(fill.Pair.Base, fill.Pair.Quote);
			var tt = fill.Side.TradeType();
			var amount_in = tt.AmountIn(fill.AmountBase._(pair.Base), fill.Price._(pair.RateUnits));
			var amount_out = tt.AmountOut(fill.AmountBase._(pair.Base), fill.Price._(pair.RateUnits));
			var commission = fill.CommissionAsset != null ? fill.Commission._(fill.CommissionAsset) : 0m._(pair.Base);
			var commission_coin = fill.CommissionAsset != null ? Coins[fill.CommissionAsset] : Coins[pair.Base];
			var created = fill.Created;
			return new TradeCompleted(order_id, trade_id, pair, tt, amount_in, amount_out, commission, commission_coin, created, updated);
		}

		/// <summary>Convert an exchange deposit to a CoinFlip transfer</summary>
		private Transfer TransferFrom(Deposit dep)
		{
			var coin = Coins.GetOrAdd(dep.Asset);
			var amount = dep.Amount._(coin);
			return new Transfer(dep.TxId, ETransfer.Deposit, coin, amount, dep.InsertTime, ToTransferStatus(dep.Status));
		}

		/// <summary>Convert an exchange withdrawal to a CoinFlip transfer</summary>
		private Transfer TransferFrom(Withdrawal wit)
		{
			var coin = Coins.GetOrAdd(wit.Asset);
			var amount = wit.Amount._(coin);
			return new Transfer(wit.TxId, ETransfer.Withdrawal, coin, amount, wit.ApplyTime, ToTransferStatus(wit.Status));
		}

		/// <summary>Convert a market period to a time frame</summary>
		private ETimeFrame ToTimeFrame(EMarketPeriod mp)
		{
			switch (mp)
			{
			default: return ETimeFrame.None;
			case EMarketPeriod.Minutes1: return ETimeFrame.Min1;
			case EMarketPeriod.Minutes3: return ETimeFrame.Min3;
			case EMarketPeriod.Minutes5: return ETimeFrame.Min5;
			case EMarketPeriod.Minutes15: return ETimeFrame.Min15;
			case EMarketPeriod.Minutes30: return ETimeFrame.Min30;
			case EMarketPeriod.Hours1: return ETimeFrame.Hour1;
			case EMarketPeriod.Hours2: return ETimeFrame.Hour2;
			case EMarketPeriod.Hours4: return ETimeFrame.Hour4;
			case EMarketPeriod.Hours6: return ETimeFrame.Hour6;
			case EMarketPeriod.Hours8: return ETimeFrame.Hour8;
			case EMarketPeriod.Hours12: return ETimeFrame.Hour12;
			case EMarketPeriod.Day1: return ETimeFrame.Day1;
			case EMarketPeriod.Day3: return ETimeFrame.Day3;
			case EMarketPeriod.Week1: return ETimeFrame.Week1;
			case EMarketPeriod.Month1: return ETimeFrame.Month1;
			}
		}

		/// <summary>Convert a time frame to the nearest market period</summary>
		private EMarketPeriod ToMarketPeriod(ETimeFrame tf)
		{
			switch (tf)
			{
			default:
				return EMarketPeriod.None;
			case ETimeFrame.Tick1:
			case ETimeFrame.Min1:
				return EMarketPeriod.Minutes1;
			case ETimeFrame.Min2:
			case ETimeFrame.Min3:
				return EMarketPeriod.Minutes3;
			case ETimeFrame.Min4:
			case ETimeFrame.Min5:
			case ETimeFrame.Min6:
			case ETimeFrame.Min7:
			case ETimeFrame.Min8:
			case ETimeFrame.Min9:
				return EMarketPeriod.Minutes5;
			case ETimeFrame.Min10:
			case ETimeFrame.Min15:
				return EMarketPeriod.Minutes15;
			case ETimeFrame.Min20:
			case ETimeFrame.Min30:
			case ETimeFrame.Min45:
				return EMarketPeriod.Minutes30;
			case ETimeFrame.Hour1:
				return EMarketPeriod.Hours1;
			case ETimeFrame.Hour2:
				return EMarketPeriod.Hours2;
			case ETimeFrame.Hour3:
			case ETimeFrame.Hour4:
				return EMarketPeriod.Hours4;
			case ETimeFrame.Hour6:
				return EMarketPeriod.Hours6;
			case ETimeFrame.Hour8:
				return EMarketPeriod.Hours8;
			case ETimeFrame.Hour12:
				return EMarketPeriod.Hours12;
			case ETimeFrame.Day1:
				return EMarketPeriod.Day1;
			case ETimeFrame.Day2:
			case ETimeFrame.Day3:
				return EMarketPeriod.Day3;
			case ETimeFrame.Week1:
			case ETimeFrame.Week2:
				return EMarketPeriod.Week1;
			case ETimeFrame.Month1:
				return EMarketPeriod.Month1;
			}
		}

		/// <summary>Convert a deposit status to the nearest transfer status</summary>
		private Transfer.EStatus ToTransferStatus(EDepositStatus ds)
		{
			switch (ds)
			{
			default: throw new Exception($"Unknown deposit status: {ds}");
			case EDepositStatus.Pending: return Transfer.EStatus.Pending;
			case EDepositStatus.Success: return Transfer.EStatus.Complete;
			case EDepositStatus.CreditedButCannotWithdraw: return Transfer.EStatus.Complete;
			}
		}

		/// <summary>Convert a deposit status to the nearest transfer status</summary>
		private Transfer.EStatus ToTransferStatus(EWithdrawalStatus ws)
		{
			switch (ws)
			{
			default: throw new Exception($"Unknown withdrawal status: {ws}");
			case EWithdrawalStatus.EmailSent:
			case EWithdrawalStatus.AwaitingApproval:
			case EWithdrawalStatus.Processing:
				return Transfer.EStatus.Pending;
			case EWithdrawalStatus.Completed:
				return Transfer.EStatus.Complete;
			case EWithdrawalStatus.Cancelled:
				return Transfer.EStatus.Cancelled;
			case EWithdrawalStatus.Rejected:
			case EWithdrawalStatus.Failure:
				return Transfer.EStatus.Failed;
			}
		}

		/// <summary></summary>
		[DebuggerDisplay("{Pair.Id,nq}")]
		struct PairAndTicker
		{
			public CurrencyPair Pair;
			public Ticker Ticker;
		}
		[DebuggerDisplay("{Pair.Id,nq}")]
		struct PairAndMarketData
		{
			public CurrencyPair Pair;
			public Offer[] B2Q;
			public Offer[] Q2B;
		}
	}
}
