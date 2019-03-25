using Rylogic.Attrib;

namespace Binance.API
{
	/// <summary>Error codes to distinguish replies</summary>
	public enum EErrorCode
	{
		Success,
		Failure,
		InvalidParameter,
		TooMuchDataRequested,
	}

	public enum ESymbolStatus
	{
		PRE_TRADING,
		TRADING,
		POST_TRADING,
		END_OF_DAY,
		HALT,
		AUCTION_MATCH,
		BREAK,
	}
	public enum ESymbolType
	{
		SPOT,
	}
	public enum EOrderStatus
	{
		NEW,
		PARTIALLY_FILLED,
		FILLED,
		CANCELED,
		PENDING_CANCEL, // (currently unused)
		REJECTED,
		EXPIRED,
	}
	public enum EOrderTypes
	{
		LIMIT,
		MARKET,
		STOP_LOSS,
		STOP_LOSS_LIMIT,
		TAKE_PROFIT,
		TAKE_PROFIT_LIMIT,
		LIMIT_MAKER,
	}
	public enum EOrderSide
	{
		BUY,
		SELL,
	}
	public enum ETimeInForce
	{
		GTC,
		IOC,
		FOK,
	}

	/// <summary></summary>
	public enum EFilterType
	{
		PRICE_FILTER,
		PERCENT_PRICE,
		LOT_SIZE,
		MIN_NOTIONAL,
		ICEBERG_PARTS,
		MARKET_LOT_SIZE,
		MAX_NUM_ORDERS,
		MAX_NUM_ALGO_ORDERS,
		MAX_NUM_ICEBERG_ORDERS,
		EXCHANGE_MAX_NUM_ORDERS,
		EXCHANGE_MAX_NUM_ALGO_ORDERS,
	}

	public enum ERateLimitType
	{
		REQUEST_WEIGHT,
		ORDERS,
	}

	/// <summary>Represents a time frame of a market.</summary>
	public enum EMarketPeriod
	{
		None = 0,
		[Assoc("tag", "1m")] Minutes1,
		[Assoc("tag", "3m")] Minutes3,
		[Assoc("tag", "5m")] Minutes5,
		[Assoc("tag", "15m")] Minutes15,
		[Assoc("tag", "30m")] Minutes30,
		[Assoc("tag", "1h")] Hours1,
		[Assoc("tag", "2h")] Hours2,
		[Assoc("tag", "4h")] Hours4,
		[Assoc("tag", "6h")] Hours6,
		[Assoc("tag", "8h")] Hours8,
		[Assoc("tag", "12h")] Hours12,
		[Assoc("tag", "1d")] Day1,
		[Assoc("tag", "3d")] Day3,
		[Assoc("tag", "1w")] Week1,
		[Assoc("tag", "1M")] Month1,
	}
}
