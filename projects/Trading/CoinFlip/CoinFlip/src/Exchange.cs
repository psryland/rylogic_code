using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	// Notes:
	//  - Pairs should be given as Base/Quote.
	//  - Orders have the price in Quote currency and the Volume in Base currency.
	//  - Typically, the more common currency is the Quote currency,
	//      e.g NZDUSD => price = 0.72USD/1NZD, volume in NZD
	//      "buy 1000NZD = Base*Price = 720USD"

	/// <summary>Base class for exchanges</summary>
	public abstract class Exchange
	{
		public Exchange(Model model, decimal transaction_fee, int poll_period)
		{
			Model = model;
			TransactionFee = transaction_fee;
			PollPeriod = poll_period;
			Heart = new Timer();
			Coins = new CoinCollection();
			Pairs = new PairCollection();
			Balance = new BalanceCollection(this);
			Positions = new PositionsCollection();
			Status = EStatus.Offline;
		}
		public virtual void Dispose()
		{
			Heart = null;
			Model = null;
		}

		/// <summary>The name of this exchange</summary>
		public string Name
		{
			get { return GetType().Name; }
		}

		/// <summary>The connection status of the exchange</summary>
		public EStatus Status
		{
			get { return m_status; }
			protected set
			{
				if (m_status == value) return;
				m_status = value;
				OnStatusChanged();
			}
		}
		private EStatus m_status;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				SetModel(value);
			}
		}
		protected virtual void SetModel(Model model)
		{
			if (m_model != null)
			{
				m_model.CoinsOfInterest.ListChanging -= HandleCoinsOfInterestedChanging;
			}
			m_model = model;
			if (m_model != null)
			{
				m_model.CoinsOfInterest.ListChanging += HandleCoinsOfInterestedChanging;
			}
		}
		private Model m_model;

		/// <summary>The coins associated with this exchange</summary>
		public CoinCollection Coins { [DebuggerStepThrough] get; private set; }
		public class CoinCollection :BindingDict<string, Coin>
		{
			public CoinCollection()
			{
				KeyFrom = x => x.Symbol;
			}
			public override Coin this[string sym]
			{
				get { return TryGetValue(sym, out var coin) ? coin : null; }
				set { base[sym] = value; }
			}
		}

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs { [DebuggerStepThrough] get; private set; }
		public class PairCollection :BindingDict<string, TradePair>
		{
			public PairCollection()
			{
				KeyFrom = x => x.UniqueKey;
			}

			/// <summary>Return a pair involving the given symbols (in either order)</summary>
			public TradePair this[string sym0, string sym1]
			{
				get
				{
					TradePair pair;
					if (TryGetValue(TradePair.MakeKey(sym0, sym1), out pair)) return pair;
					if (TryGetValue(TradePair.MakeKey(sym1, sym0), out pair)) return pair;
					return null;
				}
			}

			/// <summary>Return a pair involving the given symbol and the two exchanges (in either order)</summary>
			public TradePair this[string sym, Exchange exch0, Exchange exch1]
			{
				get
				{
					TradePair pair;
					if (TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out pair)) return pair;
					if (TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out pair)) return pair;
					return null;
				}
			}
		}

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance { [DebuggerStepThrough] get; private set; }
		public class BalanceCollection :BindingDict<Coin, Balance>
		{
			private readonly Exchange m_exch;
			public BalanceCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.Coin;
				SupportsSorting = false;
			}
			public override Balance this[Coin coin]
			{
				get
				{
					if (coin.Exchange != m_exch) throw new Exception("Currency not associated with this exchange");
					return TryGetValue(coin, out var bal) ? bal : new Balance(coin);
				}
				set
				{
					if (coin.Exchange != m_exch) throw new Exception("Currency not associated with this exchange");
					base[coin] = value;
				}
			}
			public Balance this[string sym]
			{
				get { return base[m_exch.Coins[sym]]; }
				set { base[m_exch.Coins[sym]] = value; }
			}
		}

		/// <summary>Open positions held on this exchange, keyed on order ID</summary>
		public PositionsCollection Positions { [DebuggerStepThrough] get; private set; }
		public class PositionsCollection :BindingDict<ulong, Position>
		{
			public PositionsCollection()
			{
				KeyFrom = x => x.OrderId;
			}
		}

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public decimal TransactionFee
		{
			get;
			private set;
		}

		/// <summary>The number of coins available on this exchange</summary>
		public int CoinsAvailable
		{
			get { return Coins.Count; }
		}

		/// <summary>The number of trading pairs available</summary>
		public int PairsAvailable
		{
			get { return Pairs.Count; }
		}

		/// <summary>Raised when the 'Status' value changes</summary>
		public event EventHandler StatusChanged;
		protected virtual void OnStatusChanged()
		{
			// Status change can happen from any thread
			Model.RunOnGuiThread(() =>
			{
				StatusChanged.Raise(this);
				Model.Exchanges.ResetItem(this, ignore_missing:true);
			});
		}

		/// <summary>The rate that 'Heart' beats at</summary>
		public int PollPeriod
		{
			get { return m_poll_period; }
			set
			{
				SetPollPeriod(value);
				if (Heart != null)
				{
					Heart.Interval = value;
					Heart.Enabled = value != 0;
				}
			}
		}
		protected virtual void SetPollPeriod(int period)
		{
			m_poll_period = period;
		}
		private int m_poll_period;

		/// <summary>Heart beat</summary>
		public Timer Heart
		{
			get { return m_heart; }
			private set
			{
				if (m_heart == value) return;
				if (m_heart != null)
				{
					m_heart.Tick -= HeartBeat;
					Util.Dispose(ref m_heart);
				}
				m_heart = value;
				if (m_heart != null)
				{
					m_heart.Tick += HeartBeat;
					m_heart.Interval = PollPeriod != 0 ? PollPeriod : 60000;
					m_heart.Enabled = PollPeriod != 0;
				}
			}
		}
		private async void HeartBeat(object sender, EventArgs e)
		{
			if (m_heart_beat != 0) return;
			using (Scope.Create(() => ++m_heart_beat, () => --m_heart_beat))
				await UpdateData();
		}
		private int m_heart_beat;
		private Timer m_heart;

		/// <summary>
		/// Open the connection to the exchange and gather data.
		/// This method is expected to be asynchronous.</summary>
		public abstract void Start();

		/// <summary>Place an order to convert 'volume' (base currency) to quote currency at 'price' (Quote/Base)</summary>
		public async Task CreateB2QOrder(TradePair pair, Unit<decimal> volume, Unit<decimal>? price = null)
		{
			// Check units
			if (volume < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair
			price = price ?? pair.Bid.FirstOrDefault().Price;

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so B2Q is a sell of base currency
			await CreateOrder(pair, ETradeType.Sell, volume, price.Value);
		}

		/// <summary>Place an order to convert 'volume' (quote currency) to base currency at 'price' (Base/Quote)</summary>
		public async Task CreateQ2BOrder(TradePair pair, Unit<decimal> volume, Unit<decimal>? price = null)
		{
			// Check units
			if (volume < 0m._(pair.Quote))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume));
			if (price != null && price <= 0m._(pair.Base)/1m._(pair.Quote))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair
			price = price ??  pair.Ask.FirstOrDefault().Price;

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so Q2B is a buy of base currency
			await CreateOrder(pair, ETradeType.Buy, volume * price.Value, 1m / price.Value);
		}

		/// <summary>Place an order on the exchange to buy or sell 'volume' (in base currency). i.e. Sell = Base->Quote, Buy = Quote->Base</summary>
		public async Task CreateOrder(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal>? price = null)
		{
			if (pair.Exchange != this)
				throw new Exception("Pair {0} is not provided by this exchange".Fmt(pair));
			if (volume < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If price isn't given, get it from the pair
			if (price == null)
			{
				if (tt == ETradeType.Sell) price = pair.Ask.FirstOrDefault().Price;
				if (tt == ETradeType.Buy ) price = pair.Bid.FirstOrDefault().Price;
				if (price == null) throw new Exception("Market rate could not be determined");
			}

			// Place the order
			await CreateOrderInternal(pair, tt, volume, price.Value);

			// Log the event
			Model.Log.Write(ELogLevel.Info, "{0} Order Created: {1} {2} -> {3} {4} @ {5}".Fmt(
				tt,
				tt == ETradeType.Sell ? volume    : volume * price.Value,
				tt == ETradeType.Sell ? pair.Base : pair.Quote,
				tt == ETradeType.Buy  ? volume    : volume * price.Value,
				tt == ETradeType.Buy  ? pair.Base : pair.Quote,
				price.Value));

			// Invalidate the pair to prevent accidental use
			pair.Invalidate();
		}
		protected abstract Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate);

		/// <summary>Remove Coins/Pairs that are not associated with coins of interest or open positions</summary>
		protected void Sweep()
		{
			// Get the coins of interest
			var coi = Model.CoinsOfInterestSet.ToHashSet(); // Make a copy

			// Add coins from the open positions
			foreach (var pos in Positions.Values)
			{
				coi.Add(pos.Pair.Base);
				coi.Add(pos.Pair.Quote);
			}

			// Remove all pairs that do not involve coins from 'coi'
			Pairs.RemoveIf(x => !coi.Contains(x.Base) || !coi.Contains(x.Quote));

			// Remove all coins not in 'coi'
			Coins.RemoveIf(x => !coi.Contains(x));

			// Remove all balances for coins not in 'coi'
			Balance.RemoveIf(x => !coi.Contains(x.Coin));
		}

		/// <summary>Add the pairs for this exchange to the model (based on coins of interest)</summary>
		protected virtual Task AddPairsToModel()
		{
			// The set of coins of interest
			var coi = Model.CoinsOfInterestSet;

			// The existing pairs in the model that belong to this exchange
			var existing = Model.Pairs.Where(x => x.Exchange == this).ToHashSet();

			// Add our pairs (involving the coins of interest) that are not already in the model.
			// The model will have already removed pairs that are no longer of interest.
			// The pairs that are associated with open positions are not added to the model.
			var new_pairs = Pairs.Values.Where(x => !existing.Contains(x) && coi.Contains(x.Base) && coi.Contains(x.Quote)).ToArray();
			if (new_pairs.Length != 0)
				using (Model.Pairs.SuspendEvents(reset_bindings_on_resume: true))
					Model.Pairs.AddRange(new_pairs);

			return Misc.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected virtual Task UpdateData()
		{
			return Misc.CompletedTask;
		}

		/// <summary>Get an existing 'Coin' or add one to the 'Coins' collection for 'sym'</summary>
		protected Coin GetOrAddCoin(string sym)
		{
			return Coins.GetOrAdd(sym, k => new Coin(k, this));
		}

		/// <summary>Create a trade pair from currency symbols, adding Coins if necessary</summary>
		protected TradePair CreateTradePair(string base_symbol, string quote_symbol, int? trade_pair_id = null)
		{
			// Get the coins involved in the pair.
			var base_ = GetOrAddCoin(base_symbol);
			var quote = GetOrAddCoin(quote_symbol);

			// Add the trade pair
			return new TradePair(base_, quote, this){ TradePairId = trade_pair_id };
		}

		/// <summary>Handle the coins of interest changing</summary>
		private void HandleCoinsOfInterestedChanging(object sender, ListChgEventArgs<string> e)
		{
			if (e.IsDataChanged)
			{
				Sweep();
				AddPairsToModel();
			}
		}

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}
	}
}

