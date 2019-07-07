using System;
using System.Windows.Controls;
using CoinFlip;
using Rylogic.Common;
using Rylogic.Extn;

namespace Bot.Rebalance
{
	public class SettingsData : SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			FundId = Fund.Main;
			Exchange = string.Empty;
			Pair = string.Empty;
			AllInPrice = 0m;
			AllOutPrice = 100_000m;
			BaseCurrencyBalance = 0m;
			QuoteCurrencyBalance = 0m;

			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The fund to use for this bot</summary>
		public string FundId
		{
			get { return get<string>(nameof(FundId)); }
			set { set(nameof(FundId), value); }
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

		/// <summary>The price at which the balance is maximally in the base currency</summary>
		public decimal AllInPrice
		{
			get { return get<decimal>(nameof(AllInPrice)); }
			set { set(nameof(AllInPrice), value); }
		}

		/// <summary>The price at which the balance is maximally in the quote currency</summary>
		public decimal AllOutPrice
		{
			get { return get<decimal>(nameof(AllOutPrice)); }
			set { set(nameof(AllOutPrice), value); }
		}

		/// <summary>The current balance of base currency</summary>
		public decimal BaseCurrencyBalance
		{
			get { return get<decimal>(nameof(BaseCurrencyBalance)); }
			set { set(nameof(BaseCurrencyBalance), value); }
		}

		/// <summary>The current balance of quote currency</summary>
		public decimal QuoteCurrencyBalance
		{
			get { return get<decimal>(nameof(QuoteCurrencyBalance)); }
			set { set(nameof(QuoteCurrencyBalance), value); }
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
			if (model.Funds[FundId] == null)
				return new Exception("Fund is invalid");
			return null;
		}
	}
}
