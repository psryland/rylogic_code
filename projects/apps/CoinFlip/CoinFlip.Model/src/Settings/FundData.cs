using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	/// <summary>A balance partition, a.k.a fund</summary>
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	[DebuggerDisplay("{Id,nq}")]
	public class FundData : SettingsSet<FundData>
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

		/// <summary>Access data for an exchange</summary>
		public ExchData this[string exch_name] => Exchanges.FirstOrDefault(x => x.ExchangeName == exch_name) ?? new ExchData(exch_name, new BalData[0]);

		/// <summary>The balances assigned to this fund within each exchange</summary>
		[DebuggerDisplay("{ExchangeName,nq}")]
		public class ExchData
		{
			public ExchData(string name, BalData[] bal_data)
			{
				ExchangeName = name;
				Balances = bal_data;
			}
			public ExchData(XElement node)
			{
				ExchangeName = node.Element(nameof(ExchangeName)).As<string>(string.Empty);
				Balances = node.Element(nameof(Balances)).As<BalData[]>(Array.Empty<BalData>());
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(ExchangeName), ExchangeName, false);
				node.Add2(nameof(Balances), Balances, false);
				return node;
			}

			/// <summary>The name of the exchange</summary>
			public string ExchangeName { get; set; }

			/// <summary>Balances for each currency on this exchange in this fund</summary>
			public BalData[] Balances { get; set; }

			/// <summary>Access data for a currency</summary>
			public BalData this[string symbol] => Balances.FirstOrDefault(x => x.Symbol == symbol) ?? new BalData(symbol, 0);
		}

		/// <summary>The balance of a single coin on an exchange</summary>
		[DebuggerDisplay("{Symbol,nq} {Total}")]
		public class BalData
		{
			public BalData(string sym, decimal total)
			{
				Symbol = sym;
				Total = total;
			}
			public BalData(XElement node)
			{
				Symbol = node.Element(nameof(Symbol)).As<string>(string.Empty);
				Total = node.Element(nameof(Total)).As<decimal>();
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
