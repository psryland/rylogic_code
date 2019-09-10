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
			RebalanceThreshold = 0.1m;
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
			get => get<string>(nameof(Exchange));
			set => set(nameof(Exchange), value);
		}

		/// <summary>The name of the pair to trade</summary>
		public string Pair
		{
			get => get<string>(nameof(Pair));
			set => set(nameof(Pair), value);
		}

		/// <summary>The price (in Quote/Base) at which the balance is maximally in the base currency</summary>
		public decimal AllInPrice
		{
			get => get<decimal>(nameof(AllInPrice));
			set => set(nameof(AllInPrice), value);
		}

		/// <summary>The price (in Quote/Base) at which the balance is maximally in the quote currency</summary>
		public decimal AllOutPrice
		{
			get => get<decimal>(nameof(AllOutPrice));
			set => set(nameof(AllOutPrice), value);
		}

		/// <summary>The minimum ratio difference before a rebalance is done</summary>
		public decimal RebalanceThreshold
		{
			get => get<decimal>(nameof(RebalanceThreshold));
			set => set(nameof(RebalanceThreshold), value);
		}

		/// <summary>Orders that have been created and are live on an exchange waiting to be filled</summary>
		public MonitoredOrders PendingOrders
		{
			get => get<MonitoredOrders>(nameof(PendingOrders));
			set => set(nameof(PendingOrders), value);
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

			if (fund[pair.Base].Total < 0)
				return new Exception("Fund base amount is invalid");
			if (fund[pair.Quote].Total < 0)
				return new Exception("Fund quote amount is invalid");

			if (fund[pair.Base].Total == 0 && fund[pair.Quote].Total == 0)
				return new Exception("Combined holdings must be > 0");

			if (RebalanceThreshold <= 0)
				return new Exception("Rebalance threshold is invalid");

			return null;
		}
	}
}
