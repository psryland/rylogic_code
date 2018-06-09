using System;
using System.Diagnostics;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Pair} vol=({Volume})")]
	public class PortfolioData :IDisposable
	{
		public PortfolioData()
			: this(DateTimeOffset.Now, null, 0, 0, string.Empty)
		{}
		public PortfolioData(DateTimeOffset timestamp, TradePair pair, Unit<decimal> price, Unit<decimal> volume, string notes)
		{
			Timestamp = timestamp;
			Pair = pair;
			Price = price;
			Volume = volume;
			Notes = notes;
		}
		public PortfolioData(PortfolioData rhs)
		{
			Timestamp = rhs.Timestamp;
			Pair = rhs.Pair;
			Price = rhs.Price;
			Volume = rhs.Volume;
			Notes = rhs.Notes;
		}
		public PortfolioData(Model model, XElement node)
		{
			var exch = node.Element(nameof(Exchange)).As<string>();
			var pair = node.Element(nameof(Pair)).As<string>();
			Pair = model.FindPairOnExchange(exch, pair);
			Timestamp = node.Element(nameof(Timestamp)).As<DateTimeOffset>();
			Price = node.Element(nameof(Price)).As<decimal>()._(Pair.RateUnits);
			Volume = node.Element(nameof(Volume)).As<decimal>()._(Pair.Base);
			Notes = node.Element(nameof(Notes)).As<string>();
		}
		public void Dispose()
		{ }
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(Timestamp), Timestamp, false);
			node.Add2(nameof(Exchange), Exchange.Name, false);
			node.Add2(nameof(Pair), Pair.Name, false);
			node.Add2(nameof(Price), Price, false);
			node.Add2(nameof(Volume), Volume, false);
			node.Add2(nameof(Notes), Notes, false);
			return node;
		}

		/// <summary>The exchange that the position is held on</summary>
		public Model Model { get { return Exchange?.Model; } }

		/// <summary>The exchange that the position is held on</summary>
		public Exchange Exchange { get { return Pair?.Exchange; } }

		/// <summary>The date that the trade was made</summary>
		public DateTimeOffset Timestamp { get; set; }

		/// <summary>The pair traded</summary>
		public TradePair Pair { get; set; }

		/// <summary>The price at the time of the trade</summary>
		public Unit<decimal> Price { get; set; }

		/// <summary>The volume traded</summary>
		public Unit<decimal> Volume { get; set; }

		/// <summary>The plan for the trade</summary>
		public string Notes { get; set; }
	}
}
