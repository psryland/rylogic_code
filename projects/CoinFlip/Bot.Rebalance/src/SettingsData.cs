using System;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Common;
using Rylogic.Extn;

namespace Bot.Rebalance
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			Exchange = string.Empty;
			Pair = string.Empty;
			AllInPrice = 0m;
			AllOutPrice = 100_000m;
			BaseCurrencyBalance = 0m;
			QuoteCurrencyBalance = 0m;
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
		public decimal AllInPrice
		{
			get { return get<decimal>(nameof(AllInPrice)); }
			set { set(nameof(AllInPrice), value); }
		}

		/// <summary>The price (in Quote/Base) at which the balance is maximally in the quote currency</summary>
		public decimal AllOutPrice
		{
			get { return get<decimal>(nameof(AllOutPrice)); }
			set { set(nameof(AllOutPrice), value); }
		}

		/// <summary>The current balance of base currency (in Base) </summary>
		public decimal BaseCurrencyBalance
		{
			get { return get<decimal>(nameof(BaseCurrencyBalance)); }
			set { set(nameof(BaseCurrencyBalance), value); }
		}

		/// <summary>The current balance of quote currency (in Quote) </summary>
		public decimal QuoteCurrencyBalance
		{
			get { return get<decimal>(nameof(QuoteCurrencyBalance)); }
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
		public Exception Validate(Model model)
		{
			if (!Exchange.HasValue())
				return new Exception("No exchange configured");
			if (model.Exchanges[Exchange] == null)
				return new Exception("The configured exchange is not available");
			if (!Pair.HasValue())
				return new Exception("No pair configured");
			if (model.Exchanges[Exchange].Pairs[Pair] == null)
				return new Exception("The configured paid is not available");
			if (AllInPrice >= AllOutPrice)
				return new Exception("Price range is invalid");
			if (BaseCurrencyBalance < 0)
				return new Exception("Base holdings is invalid");
			if (QuoteCurrencyBalance < 0)
				return new Exception("Quote holdings is invalid");
			if (BaseCurrencyBalance == 0 && QuoteCurrencyBalance == 0)
				return new Exception("Combined holdings must be > 0");
			if (RebalanceThreshold <= 0)
				return new Exception("Rebalance threshold is invalid");
			return null;
		}
	}
}
