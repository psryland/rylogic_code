using System;
using System.ComponentModel;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>Settings associated with the Repo connection</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	public class PoloniexSettings : ExchangeSettings<PoloniexSettings>
	{
		public PoloniexSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025m;
			MarketDepth = 20;
			ServerRequestRateLimit = 6f;
		}
		private class TyConv : GenericTypeConverter<PoloniexSettings> { }
	}
}
