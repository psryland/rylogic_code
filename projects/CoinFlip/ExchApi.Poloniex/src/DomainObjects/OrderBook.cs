using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Poloniex.API.DomainObjects
{
	/// <summary>The Buy(Bid) / Sell(Ask) orders. Typically associated with a pair</summary>
	[DebuggerDisplay("Buys={BuyOffers.Count} Sells={SellOffers.Count}")]
	public class OrderBook
	{
		public OrderBook()
		{
			BuyOffers = new List<Offer>();
			SellOffers = new List<Offer>();
		}

		/// <summary>The currency pair associated with this order book</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>Offers to buy</summary>
		public List<Offer> BuyOffers { get; private set; }
		[JsonProperty("bids")] private List<string[]> BuyOffersInternal
		{
			set { BuyOffers = ParseOffers(value, EOrderType.Buy); }
		}

		/// <summary>Offers to sell</summary>
		public List<Offer> SellOffers { get; private set; }
		[JsonProperty("asks")] private List<string[]> SellOffersInternal
		{
			set { SellOffers = ParseOffers(value, EOrderType.Sell); }
		}

		/// <summary>True if trading is frozen on the owning pair</summary>
		public bool IsFrozen { get; private set; }
		[JsonProperty("isFrozen")] private string IsFrozenInternal
		{
			set { IsFrozen = int.Parse(value) != 0; }
		}

		/// <summary>A sequence number for this update</summary>
		[JsonProperty("seq")]
		public int Seq { get; internal set; }

		/// <summary></summary>
		private static List<Offer> ParseOffers(List<string[]> offers, EOrderType order_type)
		{
			var output = new List<Offer>(offers.Count);
			foreach (var order in offers)
			{
				output.Add(new Offer(
					order_type,
					(decimal)double.Parse(order[0], CultureInfo.InvariantCulture),
					(decimal)double.Parse(order[1], CultureInfo.InvariantCulture)));
			}
			return output;
		}

		/// <summary>A trade offer</summary>
		[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
		public class Offer
		{
			internal Offer()
			{}
			internal Offer(EOrderType type, decimal price, decimal volume)
			{
				Type = type;
				Price = price;
				AmountBase = volume;
			}

			/// <summary>A buy or sell order</summary>
			public EOrderType Type { get; }

			/// <summary>The trade price (in quote currency)</summary>
			[JsonProperty("rate")]
			public decimal Price { get; private set; }

			/// <summary>The trade amount (in base currency)</summary>
			[JsonProperty("amount")]
			public decimal AmountBase { get; private set; }

			/// <summary>The trade amount (in quote currency)</summary>
			public decimal AmountQuote => AmountBase * Price;
		}
	}

	/// <summary>Data type received from the WAMP order book and trade update channel</summary>
	public class OrderBookUpdate
	{
		public enum EUpdateType
		{
			NewTrade,
			Modify,
			Remove,
		}

		/// <summary>The type of update this represents</summary>
		public EUpdateType Type { get; private set; }
		[JsonProperty("type")] internal string TypeInternal
		{
			set
			{
				switch (value) {
				default: throw new PoloniexException(EErrorCode.Failure, $"Unknown update type: {value}");
				case "newTrade":        Type = EUpdateType.NewTrade; break;
				case "orderBookModify": Type = EUpdateType.Modify; break;
				case "orderBookRemove": Type = EUpdateType.Remove; break;
				}
			}
		}

		/// <summary>The updated volume at the given rate</summary>
		[JsonProperty("data")]
		public OrderBook.Offer Offer { get; internal set; }
	}
}
