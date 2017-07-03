using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cryptopia.API;
using Cryptopia.API.DataObjects;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>Cryptopia Exchange</summary>
	public class Cryptopia :Exchange
	{
		private const string ApiKey    = "429162d7138f4275b5e9dd09ff6362fd";
		private const string ApiSecret = "Dt6k0ZC3zqbNCxpDSsHqX7VIR21PEPO7vNeKDbQIKcI=";

		private CryptopiaApiPublic m_pub;
		private CryptopiaApiPrivate m_priv;

		public Cryptopia(Model model)
			:base(model, 0.002m, model.Settings.Cryptopia.PollPeriod)
		{
			m_pub = new CryptopiaApiPublic();
			m_priv = new CryptopiaApiPrivate(ApiKey, ApiSecret);
		}

		/// <summary>Open the connection to the exchange and gather data</summary>
		public override void Start()
		{
			Model.RunOnGuiThread(async () =>
			{
				await AddPairsToModel();
				await UpdateData();

				Heart.Enabled = true;
			});
		}

		/// <summary>Open a trade</summary>
		protected async override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			// Obey the global trade switch
			if (Model.AllowTrades)
			{
				// Place the trade order
				var msg = await m_priv.SubmitTrade(new SubmitTradeRequest(pair.TradePairId.Value, tt.ToCryptopiaTT(), volume, rate));
				if (!msg.Success)
					throw new Exception("Cryptopia: Submit trade failed. {0}".Fmt(msg.Error));
			}

			// Update the balance and positions
			await UpdateData();
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task AddPairsToModel()
		{
			// Get all available trading pairs
			var msg = await m_pub.GetTradePairs();
			if (!msg.Success)
				throw new Exception("Cryptopia: Failed to read available trading pairs. {0}".Fmt(msg.Error));

			// The set of coins of interest
			var coi = Model.CoinsOfInterestSet;

			// Create the trade pairs and associated coins
			var pairs = msg.Data.Where(x => coi.Contains(x.BaseSymbol) && coi.Contains(x.Symbol));
			foreach (var p in pairs)
			{
				// Add the trade pair. Note: Cryptopia has Base/Quote backwards for this data structure
				var pair = CreateTradePair(p.Symbol, p.BaseSymbol, p.Id);
				Pairs[pair.UniqueKey] = pair;
			}

			// Add 'Balance' objects for each coin type
			foreach (var c in Coins.Values)
				Balance.GetOrAdd(c, x => new Balance(c));

			// Add the pairs to the model
			await base.AddPairsToModel();
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateData()
		{
			try
			{
				// Request the order book data for all of the pairs
				var ids = Pairs.Values.Select(x => x.TradePairId.Value).ToArray();
				var order_book = ids.Length != 0
					? m_pub.GetMarketOrderGroups(new MarketOrderGroupsRequest(ids))
					: null;

				// Request the account data
				var balance_data = m_priv.GetBalances(new BalanceRequest());

				// Request the existing orders
				var existing_orders = m_priv.GetOpenOrders(new OpenOrdersRequest());

				// Process the order book data and update the pairs
				#region Order Book
				if (order_book != null)
				{
					var msg = await order_book;
					if (msg == null || !msg.Success)
						throw new Exception("Cryptopia: Failed to update trading pairs. {0}".Fmt(msg?.Error ?? string.Empty));

					// Update the market orders
					foreach (var orders in msg.Data)
					{
						// Get the currencies involved in the pair
						var coin = orders.Market.Split('_');

						// Find the pair to update.
						// The pair may have been removed between the request and the reply.
						// Pair's may have been added as well, we'll get those next heart beat.
						var pair = Pairs[coin[0], coin[1]];
						if (pair == null)
							continue;

						// Update the depth of market data
						var buys  = orders.Buy .Select(x => new Order(x.Price._(pair.RateUnits), x.Volume._(pair.Base))).ToArray();
						var sells = orders.Sell.Select(x => new Order(x.Price._(pair.RateUnits), x.Volume._(pair.Base))).ToArray();
						pair.UpdateOrderBook(buys, sells);
					}
				}
				#endregion

				// Process the account data and update the balances
				#region Balance
				{
					var msg = await balance_data;
					if (!msg.Success)
						throw new Exception("Cryptopia: Failed to update account balances pairs. {0}".Fmt(msg.Error));

					// Update the account balance
					using (Model.Balances.PreservePosition())
					{
						var coi = Model.CoinsOfInterestSet;
						foreach (var b in msg.Data.Where(x => x.Available != 0 || coi.Contains(x.Symbol)))
						{
							// Find the currency that this balance is for. If it's not a coin of interest, ignore
							var coin = GetOrAddCoin(b.Symbol);

							// Update the balance
							var bal = new Balance(coin, b.Total, b.Available, b.Unconfirmed, b.HeldForTrades, b.PendingWithdraw);
							Balance[coin] = bal;
						}
					}
				}
				#endregion

				// Process the existing orders
				#region Current Orders
				{
					var msg = await existing_orders;
					if (!msg.Success)
						throw new Exception("Cryptopia: Failed to received existing orders. {0}".Fmt(msg.Error));

					// Update the collection of existing orders
					var order_ids = new HashSet<ulong>();
					using (Model.Positions.PreservePosition())
					{
						foreach (var order in msg.Data)
						{
							// Get the associated trade pair (add the pair if it doesn't exist)
							var id = unchecked((ulong)order.OrderId);
							var sym = order.Market.Split('/');
							var pair = Pairs[sym[0], sym[1]] ?? Pairs.Add2(CreateTradePair(sym[0], sym[1], order.TradePairId));
							var rate = order.Rate._(pair.RateUnits);
							var volume = order.Amount._(pair.Base);
							var remaining = order.Remaining._(pair.Base);
							var ts = order.TimeStamp.As(DateTimeKind.Utc);

							// Add the position to the collection
							var pos = new Position(id, pair, Misc.TradeType(order.Type), rate, volume, remaining, ts);
							Positions[id] = pos;
							order_ids.Add(id);
						}

						// Remove any positions that are no longer valid
						foreach (var id in Positions.Keys.Where(x => !order_ids.Contains(x)).ToArray())
							Positions.Remove(id);
					}
				}
				#endregion

				Status = EStatus.Connected;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "Cryptopia UpdateData() failed");
				Status = EStatus.Error;
			}

			await base.UpdateData();
		}
	}
}

