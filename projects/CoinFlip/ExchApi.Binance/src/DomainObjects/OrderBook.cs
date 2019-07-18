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
			B2QOffers = new List<Offer>();
			Q2BOffers = new List<Offer>();
			SequenceNo = 0;
		}
		public MarketDepth(MarketDepth rhs)
		{
			Pair = rhs.Pair;
			B2QOffers = new List<Offer>(rhs.B2QOffers);
			Q2BOffers = new List<Offer>(rhs.Q2BOffers);
			SequenceNo = rhs.SequenceNo;
		}

		/// <summary>The pair that this order book is for</summary>
		public CurrencyPair Pair { get; set; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public List<Offer> Q2BOffers { get; private set; }
		[JsonProperty("asks")] private List<string[]> Q2BOffersInternal { set => Q2BOffers = ParseOffers(value); }

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public List<Offer> B2QOffers { get; private set; }
		[JsonProperty("bids")] private List<string[]> B2QOffersInternal { set => B2QOffers = ParseOffers(value); }

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
					double.Parse(order[0], CultureInfo.InvariantCulture),
					double.Parse(order[1], CultureInfo.InvariantCulture)));
			}
			return output;
		}

		/// <summary></summary>
		[DebuggerDisplay("Price={Price} Amount={AmountBase}")]
		public class Offer
		{
			public Offer(double price_q2b, double amount_base)
			{
				PriceQ2B = price_q2b;
				AmountBase = amount_base;
			}

			/// <summary></summary>
			public double PriceQ2B { get; }

			/// <summary></summary>
			public double AmountBase { get; }
		}
	}
}
