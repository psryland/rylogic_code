using System;
using System.ComponentModel;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings associated with the Repo connection</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class BittrexSettings : ExchangeSettings<BittrexSettings>
	{
		public BittrexSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025m;
			MarketDepth = 20;
			ServerRequestRateLimit = 10f;
		}
		private class TyConv : GenericTypeConverter<BittrexSettings> { }
	}
}
