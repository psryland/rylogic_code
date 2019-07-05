using CoinFlip;
using Rylogic.Common;

namespace Bot.Rebalance
{
	public class SettingsData : SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			FundId = Fund.Main;
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
	}
}
