using System.Collections.Generic;
using Rylogic.Common;

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
			TransactionFee = 0.0025;
			MarketDepth = 100;
			ServerRequestRateLimit = 10;
			PublicAPIOnly = false;
			OrderDetails = new List<OrderDetails>();
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
		public double TransactionFee
		{
			get { return get<double>(nameof(TransactionFee)); }
			set { set(nameof(TransactionFee), value); }
		}

		/// <summary>The market depth to retrieve</summary>
		public int MarketDepth
		{
			get { return get<int>(nameof(MarketDepth)); }
			set { set(nameof(MarketDepth), value); }
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public double ServerRequestRateLimit
		{
			get { return get<double>(nameof(ServerRequestRateLimit)); }
			set { set(nameof(ServerRequestRateLimit), value); }
		}

		/// <summary>True if only public API calls should be made on this exchange</summary>
		public bool PublicAPIOnly
		{
			get { return get<bool>(nameof(PublicAPIOnly)); }
			set { set(nameof(PublicAPIOnly), value); }
		}

		/// <summary>Persisted details about live orders</summary>
		public List<OrderDetails> OrderDetails
		{
			get { return get<List<OrderDetails>>(nameof(OrderDetails)); }
			set { set(nameof(OrderDetails), value); }
		}
	}
}