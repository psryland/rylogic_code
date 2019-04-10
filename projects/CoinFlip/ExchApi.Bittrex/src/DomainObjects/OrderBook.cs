using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	[DebuggerDisplay("{Pair} Buys={BuyOffers.Count} Sells={SellOffers.Count}")]
	public class OrderBook
	{
		public OrderBook()
		{
			Nonce = 0;
			BuyOffers = new List<Offer>();
			SellOffers = new List<Offer>();
		}
		public OrderBook(CurrencyPair pair, long nonce)
			:this()
		{
			Pair = pair;
			Nonce = nonce;
		}
		public OrderBook(OrderBook rhs)
		{
			Pair = rhs.Pair;
			Nonce = rhs.Nonce;
			BuyOffers = rhs.BuyOffers.ToList();
			SellOffers = rhs.SellOffers.ToList();
		}

		/// <summary>The trading pair that this is an order book for</summary>
		public CurrencyPair Pair { get; set; }

		/// <summary>The nonce value at the time of the last update</summary>
		public long Nonce { get; set; }

		/// <summary>(First number is a maximum)</summary>
		[JsonProperty("buy")]
		public List<Offer> BuyOffers { get; set; }

		/// <summary>(First number is a minimum)</summary>
		[JsonProperty("sell")]
		public List<Offer> SellOffers { get; set; }

		[DebuggerDisplay("{Description,nq}")]
		public struct Offer
		{
			public Offer(decimal price, decimal amount_base)
			{
				Price = price;
				AmountBase = amount_base;
			}

			/// <summary></summary>
			[JsonProperty("Rate")]
			public decimal Price { get; set; }

			/// <summary></summary>
			[JsonProperty("Quantity")]
			public decimal AmountBase { get; set; }

			/// <summary></summary>
			public decimal AmountQuote => Price + AmountBase;

			/// <summary></summary>
			public string Description => $"Price={Price} Amount={AmountBase}";
		}
	}
}
