﻿using Rylogic.Common;

namespace CoinFlip.Settings
{
	/// <summary></summary>
	public class ExchangeSettings<T> : SettingsSet<T>, IExchangeSettings
		where T : SettingsSet<T>, IExchangeSettings, new()
	{
		public ExchangeSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025m;
			MarketDepth = 100;
			ServerRequestRateLimit = 10;
			PublicAPIOnly = false;
		}

		/// <summary>True if the exchange is active</summary>
		public bool Active
		{
			get => get<bool>(nameof(Active));
			set => set(nameof(Active), value);
		}

		/// <summary>Data polling rate (in ms)</summary>
		public int PollPeriod
		{
			get => get<int>(nameof(PollPeriod));
			set => set(nameof(PollPeriod), value);
		}

		/// <summary>The fee charged per trade</summary>
		public decimal TransactionFee
		{
			get => get<decimal>(nameof(TransactionFee));
			set => set(nameof(TransactionFee), value);
		}

		/// <summary>The market depth to retrieve</summary>
		public int MarketDepth
		{
			get => get<int>(nameof(MarketDepth));
			set => set(nameof(MarketDepth), value);
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public double ServerRequestRateLimit
		{
			get => get<double>(nameof(ServerRequestRateLimit));
			set => set(nameof(ServerRequestRateLimit), value);
		}

		/// <summary>True if only public API calls should be made on this exchange</summary>
		public bool PublicAPIOnly
		{
			get => get<bool>(nameof(PublicAPIOnly));
			set => set(nameof(PublicAPIOnly), value);
		}
	}
}