using System;
using System.Diagnostics;

namespace Rylobot
{
	[DebuggerDisplay("Ask={Ask} Bid={Bid}")]
	public class PriceTick
	{
		public PriceTick()
		{
			Index = 0;
			Ask = 0;
			Bid = 0;
		}
		public PriceTick(double index, QuoteCurrency ask, QuoteCurrency bid)
		{
			if (ask < bid) throw new Exception("Negative spread");

			Index = index;
			Ask = ask;
			Bid = bid;
		}
		public static PriceTick Invalid
		{
			get { return new PriceTick {Ask = -double.MaxValue, Bid = +double.MaxValue }; }
		}

		/// <summary>The fractional CAlgo index</summary>
		public double Index
		{
			get;
			set;
		}

		/// <summary>The ask price (remember: Ask > Bid)</summary>
		public QuoteCurrency Ask
		{
			get;
			set;
		}

		/// <summary>The bid price (remember: Ask > Bid)</summary>
		public QuoteCurrency Bid
		{
			get;
			set;
		}

		/// <summary>The average of ask and bid</summary>
		public QuoteCurrency Mid
		{
			get { return (Ask + Bid) / 2; }
		}

		/// <summary>The buy/sell spread</summary>
		public QuoteCurrency Spread
		{
			get { return Ask - Bid; }
		}
	}
}
