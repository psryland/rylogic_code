using System;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Common;
using Rylogic.Extn;

namespace Bot.HeikinAshiChaser
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			Exchange = string.Empty;
			Pair = string.Empty;
			TimeFrame = ETimeFrame.Day1;
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

		/// <summary>The name of the pair to trade</summary>
		public ETimeFrame TimeFrame
		{
			get => get<ETimeFrame>(nameof(TimeFrame));
			set => set(nameof(TimeFrame), value);
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

			var exchange = model.Exchanges[Exchange];
			if (exchange == null)
				return new Exception("The configured exchange is not available");

			if (!Pair.HasValue())
				return new Exception("No pair configured");

			var pair = exchange.Pairs[Pair];
			if (pair == null)
				return new Exception("The configured paid is not available");

			if (TimeFrame == ETimeFrame.None)
				return new Exception("No timeframe configured");

			var tf = pair.CandleDataAvailable.IndexOf(x => x == TimeFrame) != -1;
			if (tf == false)
				return new Exception("Timeframe not available for the configured pair and exchange");

			if (fund[pair.Base].Total < 0)
				return new Exception("Fund base amount is invalid");
			if (fund[pair.Quote].Total < 0)
				return new Exception("Fund quote amount is invalid");

			if (fund[pair.Base].Total == 0 && fund[pair.Quote].Total == 0)
				return new Exception("Combined holdings must be > 0");

			return null;
		}
	}
}
