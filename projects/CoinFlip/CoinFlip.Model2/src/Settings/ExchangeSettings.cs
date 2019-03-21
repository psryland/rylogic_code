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
		decimal TransactionFee { get; set; }

		/// <summary>The market depth to retrieve</summary>
		int MarketDepth { get; set; }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		float ServerRequestRateLimit { get; set; }

		/// <summary>True if only public API calls should be made on this exchange</summary>
		bool PublicAPIOnly { get; set; }

		/// <summary>An event raised before and after a setting is changes value</summary>
		event EventHandler<SettingChangeEventArgs> SettingChange;
	}

	/// <summary></summary>
	public class ExchangeSettings<T> : SettingsSet<T>, IExchangeSettings
		where T : SettingsSet<T>, IExchangeSettings, new()
	{
		public ExchangeSettings()
		{
			Active = true;
			PollPeriod = 500;
			TransactionFee = 0.0025m;
			MarketDepth = 20;
			ServerRequestRateLimit = 10f;
			PublicAPIOnly = false;
		}

		/// <summary>True if the exchange is active</summary>
		public bool Active
		{
			get { return get<bool>(nameof(Active)); }
			set { set(nameof(Active), value); }
		}

		/// <summary>Data polling rate (in ms)</summary>
		public int PollPeriod
		{
			get { return get<int>(nameof(PollPeriod)); }
			set { set(nameof(PollPeriod), value); }
		}

		/// <summary>The fee charged per trade</summary>
		public decimal TransactionFee
		{
			get { return get<decimal>(nameof(TransactionFee)); }
			set { set(nameof(TransactionFee), value); }
		}

		/// <summary>The market depth to retrieve</summary>
		public int MarketDepth
		{
			get { return get<int>(nameof(MarketDepth)); }
			set { set(nameof(MarketDepth), value); }
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit
		{
			get { return get<float>(nameof(ServerRequestRateLimit)); }
			set { set(nameof(ServerRequestRateLimit), value); }
		}

		/// <summary>True if only public API calls should be made on this exchange</summary>
		public bool PublicAPIOnly
		{
			get { return get<bool>(nameof(PublicAPIOnly)); }
			set { set(nameof(PublicAPIOnly), value); }
		}
	}
}