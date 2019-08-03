using System.Xml.Linq;
using Rylogic.Common;

namespace CoinFlip.Settings
{
	public class OrderDetails :SettingsXml<OrderDetails>
	{
		public OrderDetails()
			:this(0, Fund.Default, string.Empty)
		{ }
		public OrderDetails(long order_id, Fund fund, string bot_name)
		{
			OrderId = order_id;
			FundId = fund.Id;
			BotName = bot_name;
		}
		public OrderDetails(XElement node)
			: base(node)
		{ }

		/// <summary>The order id</summary>
		public long OrderId
		{
			get { return get<long>(nameof(OrderId)); }
			set { set(nameof(OrderId), value); }
		}

		/// <summary>The fund the order is associated with</summary>
		public string FundId
		{
			get { return get<string>(nameof(FundId)); }
			set { set(nameof(FundId), value); }
		}

		/// <summary>The name of the bot that created the order</summary>
		public string BotName
		{
			get { return get<string>(nameof(FundId)); }
			set { set(nameof(FundId), value); }
		}
	}
}