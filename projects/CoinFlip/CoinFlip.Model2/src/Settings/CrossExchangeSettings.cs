using System;
using System.ComponentModel;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings associated with the Repo connection</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class CrossExchangeSettings : ExchangeSettings<CrossExchangeSettings>
	{
		public CrossExchangeSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0;
			MarketDepth = 100;
			ServerRequestRateLimit = 1_000_000f;
		}
		private class TyConv : GenericTypeConverter<CrossExchangeSettings> { }
	}

}
