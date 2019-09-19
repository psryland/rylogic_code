using System;
using System.Xml.Linq;
using Rylogic.Common;

namespace CoinFlip.Settings
{
	public class EquitySettings :SettingsXml<EquitySettings>
	{
		public EquitySettings()
		{
			Since = Misc.CryptoCurrencyEpoch;
			IncludeTransfers = true;
			ShowCompletedOrders = EShowItems.Disabled;
			ShowTransfers = EShowItems.Disabled;
		}
		public EquitySettings(XElement node)
			: base(node)
		{ }

		/// <summary>The start time to display the equity from</summary>
		public DateTimeOffset Since
		{
			get => get<DateTimeOffset>(nameof(Since));
			set => set(nameof(Since), value);
		}

		/// <summary>Include deposits/withdrawals in the equity data</summary>
		public bool IncludeTransfers
		{
			get => get<bool>(nameof(IncludeTransfers));
			set => set(nameof(IncludeTransfers), value);
		}

		/// <summary>Show completed orders</summary>
		public EShowItems ShowCompletedOrders
		{
			get => get<EShowItems>(nameof(ShowCompletedOrders));
			set => set(nameof(ShowCompletedOrders), value);
		}

		/// <summary>Show transfers</summary>
		public EShowItems ShowTransfers
		{
			get => get<EShowItems>(nameof(ShowTransfers));
			set => set(nameof(ShowTransfers), value);
		}
	}
}
