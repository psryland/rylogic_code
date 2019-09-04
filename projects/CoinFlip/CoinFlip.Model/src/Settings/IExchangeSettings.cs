using System;
using Rylogic.Common;

namespace CoinFlip.Settings
{
	public interface IExchangeSettings
	{
		/// <summary>True if the exchange is active</summary>
		bool Active { get; set; }

		/// <summary>Data polling rate (in ms)</summary>
		int PollPeriod { get; set; }

		/// <summary>The fee charged per trade</summary>
		double TransactionFee { get; set; }

		/// <summary>The market depth to retrieve</summary>
		int MarketDepth { get; set; }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		double ServerRequestRateLimit { get; set; }

		/// <summary>True if only public API calls should be made on this exchange</summary>
		bool PublicAPIOnly { get; set; }

		/// <summary>Save the current settings</summary>
		void Save();

		/// <summary>An event raised before and after a setting is changes value</summary>
		event EventHandler<SettingChangeEventArgs> SettingChange;
	}

}