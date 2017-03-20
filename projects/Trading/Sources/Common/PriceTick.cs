using System;
using System.Diagnostics;

namespace Rylobot
{
	[DebuggerDisplay("Idx={Index} Ask={Ask} Bid={Bid}")]
	public class PriceTick
	{
		public PriceTick()
		{
			Index     = 0;
			Timestamp = 0;
			Ask       = 0;
			Bid       = 0;
		}
		public PriceTick(double calgo_index, long timestamp, QuoteCurrency ask, QuoteCurrency bid)
		{
			if (ask < bid) throw new Exception("Negative spread");

			Index     = calgo_index;
			Timestamp = timestamp;
			Ask       = ask;
			Bid       = bid;
		}
		public static PriceTick Invalid
		{
			get
			{
				var pt = new PriceTick();
				pt.Ask = -double.MaxValue;
				pt.Bid = +double.MaxValue;
				return pt;
			}
		}

		/// <summary>The fractional CAlgo index</summary>
		public double Index
		{
			get;
			set;
		}

		/// <summary>The server time (in ticks) of this price tick</summary>
		public long Timestamp
		{
			get;
			set;
		}
		public DateTimeOffset TimestampUTC
		{
			get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); }
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
			get { return (Ask + Bid) / 2.0; }
		}

		/// <summary>The buy/sell spread</summary>
		public QuoteCurrency Spread
		{
			get { return Ask - Bid; }
		}

		/// <summary>Return the price in the given direction. Ask(+1)/Bid(-1)/Mid(0)</summary>
		public QuoteCurrency Price(int sign)
		{
			return
				sign > 0 ? Ask :
				sign < 0 ? Bid :
				Mid;
		}
	}
}
