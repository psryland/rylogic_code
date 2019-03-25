using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class OrderBook
	{
		/// <summary>The pair that this order book is for</summary>
		public CurrencyPair Pair { get; set; }

		/// <summary></summary>
		public List<Offer> BuyOffers { get; private set; }
		[JsonProperty("bids")] private List<string[]> BuyOffersInternal
		{
			set { BuyOffers = ParseOffers(value, EOrderSide.BUY); }
		}

		/// <summary></summary>
		public List<Offer> SellOffers { get; private set; }
		[JsonProperty("asks")] private List<string[]> SellOffersInternal
		{
			set { SellOffers = ParseOffers(value, EOrderSide.SELL); }
		}

		/// <summary></summary>
		[JsonProperty("lastUpdateId")]
		public long SequenceNo { get; private set; }

		/// <summary></summary>
		private static List<Offer> ParseOffers(List<string[]> offers, EOrderSide offer_type)
		{
			var output = new List<Offer>(offers.Count);
			foreach (var order in offers)
			{
				output.Add(new Offer(
					offer_type,
					(decimal)double.Parse(order[0], CultureInfo.InvariantCulture),
					(decimal)double.Parse(order[1], CultureInfo.InvariantCulture)));
			}
			return output;
		}

		/// <summary></summary>
		[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
		public class Offer
		{
			public Offer(EOrderSide type, decimal price_q2b, decimal amount_base)
			{
				Type = type;
				PriceQ2B = price_q2b;
				AmountBase = amount_base;
			}

			/// <summary>A buy or sell order</summary>
			public EOrderSide Type { get; }

			/// <summary></summary>
			public decimal PriceQ2B { get; }

			/// <summary></summary>
			public decimal AmountBase { get; }
		}
	}
}
