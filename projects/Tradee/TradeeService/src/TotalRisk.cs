using System;
using System.Xml.Linq;
using pr.extn;

namespace Tradee
{
	[Serializable]
	public class TotalRisk
	{
		public TotalRisk()
		{
			PositionRisk = 0.0;
			OrderRisk = 0.0;
			CurrencySymbol = "";
		}
		public TotalRisk(XElement node) :this()
		{
			PositionRisk = node.Element("PositionRisk").As(PositionRisk);
			OrderRisk = node.Element("OrderRisk").As(OrderRisk);
			CurrencySymbol = node.Element("CurrencySymbol").As(CurrencySymbol);
		}
		public XElement ToXml(XElement node)
		{
			node.Add2("PositionRisk", PositionRisk, false);
			node.Add2("OrderRisk", OrderRisk, false);
			node.Add2("CurrencySymbol", CurrencySymbol, false);
			return node;
		}

		/// <summary>Combined risk</summary>
		public double Total { get { return PositionRisk + OrderRisk; } }

		/// <summary>The amount risked with current Positions</summary>
		public double PositionRisk { get; set; }

		/// <summary>The amount risked with pending orders</summary>
		public double OrderRisk { get; set; }

		/// <summary>The currency symbol</summary>
		public string CurrencySymbol { get; set; }
	}



}
