using System.Threading.Tasks;
using pr.util;

namespace CoinFlip
{
	/// <summary>A fake exchange for testing</summary>
	public class TestExchange :Exchange
	{
		public TestExchange(Model model)
			:base(model, 0.002m, 0)
		{
			// Create the coins
			Coins.Add(new Coin("BTC", this));
			Coins.Add(new Coin("LTC", this));
			Coins.Add(new Coin("ETC", this));
			Coins.Add(new Coin("XMR", this));
			Coins.Add(new Coin("DOGE", this));

			// Create trading pairs
			Pairs.Add(new TradePair(Coins["ETC"]  , Coins["BTC"]  , this));
			Pairs.Add(new TradePair(Coins["ETC"]  , Coins["LTC"]  , this));
			Pairs.Add(new TradePair(Coins["XMR"]  , Coins["LTC"]  , this));
			Pairs.Add(new TradePair(Coins["LTC"]  , Coins["BTC"]  , this));
			Pairs.Add(new TradePair(Coins["DOGE"] , Coins["BTC"]  , this));
			Pairs.Add(new TradePair(Coins["DOGE"] , Coins["LTC"]  , this));
			Pairs.Add(new TradePair(Coins["XMR"]  , Coins["DOGE"] , this));

			// Set the price data
			#region Orders
			{
				{
					var pair = Pairs["LTC", "BTC"];
					pair.Ask.Clear();
					pair.Ask.Add(new Order(0.01718697m._("BTC/LTC"),  0.08703017m._("LTC"))); // Volume in LTC
					pair.Ask.Add(new Order(0.01741759m._("BTC/LTC"), 11.19515489m._("LTC"))); // Price in BTC
					pair.Ask.Add(new Order(0.01746114m._("BTC/LTC"),  0.09286685m._("LTC")));
					pair.Bid.Clear();
					pair.Bid.Add(new Order(0.01761355m._("BTC/LTC"), 0.36983197m._("LTC")));
					pair.Bid.Add(new Order(0.01738223m._("BTC/LTC"), 3.25333257m._("LTC")));
					pair.Bid.Add(new Order(0.01703963m._("BTC/LTC"), 0.18067003m._("LTC")));
				}
				{
					var pair = Pairs["ETC", "BTC"];
					pair.Ask.Clear();
					pair.Ask.Add(new Order(0.00705057m._("BTC/ETC"), 0.00207294m._("ETC"))); // Volume in ETC
					pair.Ask.Add(new Order(0.00737168m._("BTC/ETC"), 0.20000000m._("ETC"))); // Price in BTC
					pair.Ask.Add(new Order(0.00739927m._("BTC/ETC"), 1.84008973m._("ETC")));
					pair.Bid.Clear();
					pair.Bid.Add(new Order(0.00723009m._("BTC/ETC"), 3.38670519m._("ETC")));
					pair.Bid.Add(new Order(0.00700004m._("BTC/ETC"), 0.13972610m._("ETC")));
					pair.Bid.Add(new Order(0.00694768m._("BTC/ETC"), 2.08284926m._("ETC")));
				}
				{
					var pair = Pairs["ETC", "LTC"];
					pair.Ask.Clear();
					pair.Ask.Add(new Order(0.44027439m._("LTC/ETC"), 0.00291962m._("ETC")));
					pair.Ask.Add(new Order(0.44104424m._("LTC/ETC"), 0.10505566m._("ETC")));
					pair.Ask.Add(new Order(0.45433037m._("LTC/ETC"), 0.10505566m._("ETC")));
					pair.Bid.Clear();
					pair.Bid.Add(new Order(0.41660278m._("LTC/ETC"), 0.08966687m._("ETC")));
					pair.Bid.Add(new Order(0.40980440m._("LTC/ETC"), 0.00004951m._("ETC")));
					pair.Bid.Add(new Order(0.40177634m._("LTC/ETC"), 0.09052084m._("ETC")));
				}
			}
			#endregion

			// Set the balances
			#region Balance
			{
				Balance[Coins["LTC"]] = new Balance(Coins["LTC"], 1m, 1m, 0m, 0m, 0m);
				Balance[Coins["BTC"]] = new Balance(Coins["BTC"], 1m, 1m, 0m, 0m, 0m);
				Balance[Coins["ETC"]] = new Balance(Coins["ETC"], 1m, 1m, 0m, 0m, 0m);
			}
			#endregion
		}

		/// <summary>Open a trade</summary>
		protected override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			return Misc.CompletedTask;
		}
	}
}

