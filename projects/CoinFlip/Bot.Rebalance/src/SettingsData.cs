using System;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Bot.Rebalance
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			Exchange = string.Empty;
			Pair = string.Empty;
			AllInPrice = 0.0;
			AllOutPrice = 100_000.0;
			BaseCurrencyBalance = 0.0;
			QuoteCurrencyBalance = 0.0;
			RebalanceThreshold = 0.1;
			PendingOrders = new MonitoredOrders();

			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The exchange that hosts the pair</summary>
		public string Exchange
		{
			get { return get<string>(nameof(Exchange)); }
			set { set(nameof(Exchange), value); }
		}

		/// <summary>The name of the pair to trade</summary>
		public string Pair
		{
			get { return get<string>(nameof(Pair)); }
			set { set(nameof(Pair), value); }
		}

		/// <summary>The price (in Quote/Base) at which the balance is maximally in the base currency</summary>
		public double AllInPrice
		{
			get { return get<double>(nameof(AllInPrice)); }
			set { set(nameof(AllInPrice), value); }
		}

		/// <summary>The price (in Quote/Base) at which the balance is maximally in the quote currency</summary>
		public double AllOutPrice
		{
			get { return get<double>(nameof(AllOutPrice)); }
			set { set(nameof(AllOutPrice), value); }
		}

		/// <summary>The current balance of base currency (in Base) </summary>
		public double BaseCurrencyBalance
		{
			get { return get<double>(nameof(BaseCurrencyBalance)); }
			set { set(nameof(BaseCurrencyBalance), value); }
		}

		/// <summary>The current balance of quote currency (in Quote) </summary>
		public double QuoteCurrencyBalance
		{
			get { return get<double>(nameof(QuoteCurrencyBalance)); }
			set { set(nameof(QuoteCurrencyBalance), value); }
		}

		/// <summary>The minimum ratio difference before a rebalance is done</summary>
		public double RebalanceThreshold
		{
			get { return get<double>(nameof(RebalanceThreshold)); }
			set { set(nameof(RebalanceThreshold), value); }
		}

		/// <summary>Orders that have been created and are live on an exchange waiting to be filled</summary>
		public MonitoredOrders PendingOrders
		{
			get { return get<MonitoredOrders>(nameof(PendingOrders)); }
			set { set(nameof(PendingOrders), value); }
		}

		/// <summary>Performs validation on the settings. Returns null if valid</summary>
		public Exception Validate(Model model, Fund fund)
		{
			if (!Exchange.HasValue())
				return new Exception("No exchange configured");
			if (model.Exchanges[Exchange] == null)
				return new Exception("The configured exchange is not available");
			if (!Pair.HasValue())
				return new Exception("No pair configured");

			var pair = model.Exchanges[Exchange].Pairs[Pair];
			if (pair == null)
				return new Exception("The configured paid is not available");
			if (AllInPrice >= AllOutPrice)
				return new Exception("Price range is invalid");

			var base_balance = BaseCurrencyBalance._(pair.Base);
			if (base_balance < 0 || base_balance > fund[pair.Base].Available)
				return new Exception("Base holdings is invalid");

			var quote_balance = QuoteCurrencyBalance._(pair.Quote);
			if (quote_balance < 0 || quote_balance > fund[pair.Quote].Available)
				return new Exception("Quote holdings is invalid");

			if (BaseCurrencyBalance == 0 && QuoteCurrencyBalance == 0)
				return new Exception("Combined holdings must be > 0");
			if (RebalanceThreshold <= 0)
				return new Exception("Rebalance threshold is invalid");
			return null;
		}
	}
}
