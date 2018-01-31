using System;
using System.Diagnostics;
using Rylogic.Db;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Basic record of a trade</summary>
	[Sqlite.Table(AllByDefault = false)]
	[DebuggerDisplay("{Description,nq}")]
	public class TradeRecord
	{
		// Notes:
		//  - Used in DB tables

		public TradeRecord()
		{
		}

		/// <summary>The trade id</summary>
		[Sqlite.Column] public ulong TradeId { get; private set; }

		/// <summary>The id of the order that was filled (possible partially) by this trade</summary>
		[Sqlite.Column] public ulong OrderId { get; private set; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public DateTimeOffset TimestampUTC
		{
			get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); }
		}
		[Sqlite.Column] public long Timestamp { get; private set; }

		/// <summary>The name of the pair that was traded</summary>
		public CoinPair CurrencyPair
		{
			get { return new CoinPair(PairName); }
		}
		[Sqlite.Column] public string PairName { get; private set; }

		/// <summary>The direction of the trade</summary>
		[Sqlite.Column] public ETradeType TradeType { get; private set; }

		/// <summary>The price that the trade occurred at</summary>
		public Unit<decimal> PriceQ2B { get; private set; }
		[Sqlite.Column(Name = nameof(PriceQ2B))] private double SqlPriceQ2B
		{
			get { return (double)(decimal)PriceQ2B; }
			set { PriceQ2B = ((decimal)value)._(CurrencyPair.RateUnits); }
		}

		/// <summary>The volume traded (in base currency)</summary>
		public Unit<decimal> VolumeBase { get; private set; }
		[Sqlite.Column(Name = nameof(VolumeBase))] private double SqlVolumeBase
		{
			get { return (double)(decimal)VolumeBase; }
			set { VolumeBase = ((decimal)value)._(CurrencyPair.Base); }
		}

		/// <summary>The amount charged as commission on the trade</summary>
		public Unit<decimal> CommissionQuote { get; private set; }
		[Sqlite.Column(Name = nameof(CommissionQuote))] private double SqlCommissionQuote
		{
			get { return (double)(decimal)CommissionQuote; }
			set { CommissionQuote = ((decimal)value)._(CurrencyPair.Quote); }
		}

		/// <summary>The name of the coin that was sold</summary>
		public string CoinIn
		{
			get { return TradeType.CoinIn(CurrencyPair); }
		}

		/// <summary>The name of the coin that was bought</summary>
		public string CoinOut
		{
			get { return TradeType.CoinOut(CurrencyPair); }
		}

		/// <summary>The volume traded in quote currency (excluding commissions)</summary>
		public Unit<decimal> VolumeQuote
		{
			get { return VolumeBase * PriceQ2B; }
		}

		/// <summary>The volume sold in this trade</summary>
		public Unit<decimal> VolumeIn
		{
			get { return TradeType.VolumeIn(VolumeBase, PriceQ2B); }
		}

		/// <summary>The volume bought in this trade</summary>
		public Unit<decimal> VolumeOut
		{
			get { return TradeType.VolumeOut(VolumeBase, PriceQ2B); }
		}

		/// <summary>The volume bought after commissions (in CoinOut currency)</summary>
		public Unit<decimal> VolumeNett
		{
			get { return VolumeOut - Commission; }
		}

		/// <summary>The commission charged on this trade (in CoinOut currency)</summary>
		public Unit<decimal> Commission
		{
			get { return TradeType.Commission(CommissionQuote, PriceQ2B); }
		}

		/// <summary>String description of the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6", true)} → {VolumeOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}"; }
		}

		/// <summary>Convert this record to an actual 'Historic' instance</summary>
		//public Historic ToHistoric(Exchange exch)
		//{
		//	var pair = exch.Pairs.First
		//	return new Historic(OrderId, TradeId, exch.Pairs.[], Enum<ETradeType>.Parse(rec.TradeType), (decimal)
	
		//		// Populate the history from the database
		//	//var sql = $"select * from {SqlExpr.TradeHistory} order by [{nameof(Historic.Created)}]";
		//	//foreach (var his in HistoryDB.EnumRows<HistoricDBRecord>(sql))
		//	//{
	
		//	//	History.Add(
		//	//}
	
	
	
		//}
	}
}
