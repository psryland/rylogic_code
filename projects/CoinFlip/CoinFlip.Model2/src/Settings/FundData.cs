using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>A balance partition, a.k.a fund</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	[DebuggerDisplay("{Id}")]
	public class FundData : SettingsXml<FundData>
	{
		public FundData()
			: this(string.Empty, new ExchData[0])
		{ }
		public FundData(string id, ExchData[] exch_data)
		{
			Id = id ?? string.Empty;
			Exchanges = exch_data;
		}
		public FundData(XElement node)
			: base(node)
		{ }

		/// <summary>The unique id for this fund</summary>
		public string Id
		{
			get { return get<string>(nameof(Id)); }
			set { set(nameof(Id), value); }
		}

		/// <summary>The balances on each exchange</summary>
		public ExchData[] Exchanges
		{
			get { return get<ExchData[]>(nameof(Exchanges)); }
			set { set(nameof(Exchanges), value); }
		}

		/// <summary>The balances assigned to this fund within each exchange</summary>
		[DebuggerDisplay("{Name}")]
		public class ExchData
		{
			public ExchData(string name, BalData[] bal_data)
			{
				Name = name;
				Balances = bal_data;
			}
			public ExchData(XElement node)
			{
				Name = node.Element(nameof(Name)).OrDefault(Name);
				Balances = node.Element(nameof(Balances)).OrDefault(Balances);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Name), Name, false);
				node.Add2(nameof(Balances), Balances, false);
				return node;
			}

			/// <summary>The name of the exchange</summary>
			public string Name { get; set; }

			/// <summary>Balances for each currency on this exchange in this fund</summary>
			public BalData[] Balances { get; set; }
		}

		/// <summary>The balance of a single coin on an exchange</summary>
		[DebuggerDisplay("{Symbol} {Total}")]
		public class BalData
		{
			public BalData(string sym, decimal total)
			{
				Symbol = sym;
				Total = total;
			}
			public BalData(XElement node)
			{
				Symbol = node.Element(nameof(Symbol)).OrDefault(Symbol);
				Total = node.Element(nameof(Total)).OrDefault(Total);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Symbol), Symbol, false);
				node.Add2(nameof(Total), Total, false);
				return node;
			}

			/// <summary>The currency that this balance is for</summary>
			public string Symbol { get; set; }

			/// <summary>The total amount of 'Symbol' from this exchange in this fund</summary>
			public decimal Total { get; set; }
		}

		private class TyConv : GenericTypeConverter<FundData> { }
	}
}
