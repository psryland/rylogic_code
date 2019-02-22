using System;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Poloniex.API
{
	/// <summary>A trade offer</summary>
	[DebuggerDisplay("{Type} Price={Price} Vol={VolumeBase}")]
	public class Order
	{
		internal Order()
		{}
		internal Order(EOrderType order_type, decimal price, decimal volume)
		{
			Type = order_type;
			Price = price;
			VolumeBase = volume;
		}

		/// <summary>The pair that this position is held on</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>A unique ID assigned to the order</summary>
		[JsonProperty("orderNumber")]
		public long OrderId { get; internal set; }

		/// <summary>A Buy or Sell order</summary>
		public EOrderType Type { get; private set; }
		[JsonProperty("type")] private string TypeInternal
		{
			set { Type = Misc.ToOrderType(value); }
		}

		/// <summary>The trade price (in quote currency)</summary>
		[JsonProperty("rate")]
		public decimal Price { get; internal set; }

		/// <summary>The trade volume (in base currency)</summary>
		[JsonProperty("startingAmount")]
		public decimal VolumeBase { get; internal set; }

		/// <summary>The trade volume remaining (in base currency)</summary>
		[JsonProperty("amount")]
		public decimal RemainingBase { get; private set; }

		/// <summary>The value of the trade (equal to Price * VolumeBase) (in quote currency)</summary>
		[JsonProperty("total")]
		public decimal Total { get; private set; }

		/// <summary>The order creation time stamp</summary>
		public DateTimeOffset Created { get; private set; }
		[JsonProperty("date")] private string CreatedInternal
		{
			set { Created = DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); }
		}

		/// <summary>Leverage</summary>
		[JsonProperty("margin")]
		public int Margin { get; internal set; }
	}
}
