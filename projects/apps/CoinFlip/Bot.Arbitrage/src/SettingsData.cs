using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinFlip;
using Rylogic.Common;

namespace Bot.Arbitrage
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{ }
		public SettingsData(string filepath)
			: base(filepath)
		{ }

		/// <summary>Performs validation on the settings. Returns null if valid</summary>
		public Exception? Validate(Model model, Fund fund)
		{
			//if (!Exchange.HasValue())
			//	return new Exception("No exchange configured");

			//var exchange = model.Exchanges[Exchange];
			//if (exchange == null)
			//	return new Exception("The configured exchange is not available");

			//if (!Pair.HasValue())
			//	return new Exception("No pair configured");

			//var pair = exchange.Pairs[Pair];
			//if (pair == null)
			//	return new Exception("The configured paid is not available");

			//if (TimeFrame == ETimeFrame.None)
			//	return new Exception("No timeframe configured");

			//var tf = pair.CandleDataAvailable.IndexOf(x => x == TimeFrame) != -1;
			//if (tf == false)
			//	return new Exception("Timeframe not available for the configured pair and exchange");

			//if (fund[pair.Base].Total < 0)
			//	return new Exception("Fund base amount is invalid");
			//if (fund[pair.Quote].Total < 0)
			//	return new Exception("Fund quote amount is invalid");

			//if (fund[pair.Base].Total == 0 && fund[pair.Quote].Total == 0)
			//	return new Exception("Combined holdings must be > 0");

			return null;
		}
	}
}
