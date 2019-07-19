using System;
using System.ComponentModel;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings associated with the Repo connection</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class BinanceSettings : ExchangeSettings<BinanceSettings>
	{
		public BinanceSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025;
			MarketDepth = 100;
			ServerRequestRateLimit = 6f;
		}
		private class TyConv : GenericTypeConverter<BinanceSettings> { }
	}
}
