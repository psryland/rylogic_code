using System;
using System.Linq;
using System.Threading.Tasks;
using pr.container;
using pr.util;

namespace CoinFlip
{
	/// <summary>A meta exchange that facilitates converting currency on one exchange to the same currency on another exchange</summary>
	public class CrossExchange :Exchange
	{
		public CrossExchange(Model model)
			:base(model, 0m, 0)
		{
		}

		/// <summary>Sign up to events on the model</summary>
		protected override void SetModel(Model model)
		{
			if (Model != null)
			{
				Model.Pairs.ListChanging -= HandlePairsListChanging;
			}
			base.SetModel(model);
			if (Model != null)
			{
				Model.Pairs.ListChanging += HandlePairsListChanging;
			}
		}

		/// <summary>Open the connection to the exchange and gather data</summary>
		public override void Start()
		{
			Model.RunOnGuiThread(() =>
			{
				AddPairsToModel();
				Status = EStatus.Connected;
			});
		}

		/// <summary>Open a trade</summary>
		protected override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			return Misc.CompletedTask;
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override Task AddPairsToModel()
		{
			// Remove any existing pairs that don't involve coins of interest
			var coi = Model.CoinsOfInterestSet;
			var old = Pairs.Keys.Where(x => !coi.Contains(x)).ToArray();
			foreach (var pair in old)
				Pairs.Remove(pair);

			// Create cross-exchange pairs for each coin of interest
			foreach (var sym in Model.CoinsOfInterest)
			{
				// Find the exchanges that have this coin
				var exchanges = Model.Exchanges.Where(x => !(x is CrossExchange) && x.Coins.ContainsKey(sym)).ToArray();

				// Create trading pairs between the same currencies on different exchanges
				for (int j = 0; j < exchanges.Length - 1; ++j)
				{
					for (int i = j + 1; i < exchanges.Length; ++i)
					{
						// Check whether the pair already exists
						var exch0 = exchanges[j];
						var exch1 = exchanges[i];
						var pair = Pairs[sym, exch0, exch1];

						// If not, added it
						if (pair == null)
							Pairs.Add(new TradePair(exch0.Coins[sym], exch1.Coins[sym], this));
					}
				}
			}

			// Add the pairs to the model
			return base.AddPairsToModel();
		}

		/// <summary>Update the order books of the cross-exchange pairs</summary>
		protected override Task UpdateData()
		{
			try
			{
				// Update the order book for each pair
				foreach (var pair in Pairs.Values)
				{
					// Get the coins in the pair
					var coin0 = pair.Base;
					var coin1 = pair.Quote;

					// Get the available balance for each coin
					var bal0 = coin0.Exchange.Balance[coin0];
					var bal1 = coin1.Exchange.Balance[coin1];

					// Each order book as one entry for the maximum amount available on each exchange
					var buys  = new[]{ new Order(1m._(pair.RateUnits), bal0.Available) };
					var sells = new[]{ new Order(1m._(pair.RateUnits), bal1.Available) };
					pair.UpdateOrderBook(buys, sells);
				}

				Status = EStatus.Connected;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "Cryptopia UpdateData() failed");
				Status = EStatus.Error;
			}
			return base.UpdateData();
		}

		/// <summary>Handle the collection of Pairs changing</summary>
		private void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
		{
			if (!e.IsDataChanged)
				return;

			AddPairsToModel();
		}
	}
}

