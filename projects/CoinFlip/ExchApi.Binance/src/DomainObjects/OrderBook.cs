using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class MarketDepth
	{
		public MarketDepth()
			:this(default(CurrencyPair))
		{}
		public MarketDepth(CurrencyPair pair)
		{
			Pair = pair;
			BuyOffers = new List<Offer>();
			SellOffers = new List<Offer>();
			SequenceNo = 0;
		}
		public MarketDepth(MarketDepth rhs)
		{
			Pair = rhs.Pair;
			BuyOffers = new List<Offer>(rhs.BuyOffers);
			SellOffers = new List<Offer>(rhs.SellOffers);
			SequenceNo = rhs.SequenceNo;
		}

		/// <summary>The pair that this order book is for</summary>
		public CurrencyPair Pair { get; set; }

		/// <summary>Buy orderbook</summary>
		public List<Offer> BuyOffers { get; private set; }
		[JsonProperty("bids")] private List<string[]> BuyOffersInternal { set => BuyOffers = ParseOffers(value); }

		/// <summary>Sell orderbook</summary>
		public List<Offer> SellOffers { get; private set; }
		[JsonProperty("asks")] private List<string[]> SellOffersInternal { set => SellOffers = ParseOffers(value); }

		/// <summary></summary>
		[JsonProperty("lastUpdateId")]
		public long SequenceNo { get; set; }

		/// <summary></summary>
		private static List<Offer> ParseOffers(List<string[]> offers)
		{
			var output = new List<Offer>(offers.Count);
			foreach (var order in offers)
			{
				output.Add(new Offer(
					(decimal)double.Parse(order[0], CultureInfo.InvariantCulture),
					(decimal)double.Parse(order[1], CultureInfo.InvariantCulture)));
			}
			return output;
		}

		/// <summary></summary>
		[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
		public class Offer
		{
			public Offer(decimal price_q2b, decimal amount_base)
			{
				PriceQ2B = price_q2b;
				AmountBase = amount_base;
			}

			/// <summary></summary>
			public decimal PriceQ2B { get; }

			/// <summary></summary>
			public decimal AmountBase { get; }
		}
	}
}
