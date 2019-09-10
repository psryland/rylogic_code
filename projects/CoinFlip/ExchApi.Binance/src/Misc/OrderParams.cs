using System;
using System.Linq;
using Binance.API.DomainObjects;
using Rylogic.Extn;

namespace Binance.API
{
	public class OrderParams
	{
		public OrderParams(EOrderSide side, EOrderType type)
			: this(side, type, 0, null, null)
		{ }
		public OrderParams(EOrderSide side, decimal amount_base)
			: this(side, EOrderType.MARKET, amount_base, null, null)
		{ }
		public OrderParams(EOrderSide side, decimal amount_base, decimal price_q2b)
			: this(side, EOrderType.LIMIT, amount_base, price_q2b, null)
		{ }
		public OrderParams(EOrderSide side, EOrderType type, decimal amount_base, decimal? price_q2b, decimal? stop_price)
		{
			ClientOrderId = null;
			Side = side;
			Type = type;
			TimeInForce = ETimeInForce.GTC;
			AmountBase = amount_base;
			PriceQ2B = price_q2b;
			StopPriceQ2B = stop_price;
			IcebergAmountBase = null;
		}

		public Guid? ClientOrderId { get; set; }

		/// <summary>The side of the trade</summary>
		public EOrderSide Side { get; set; }

		/// <summary>The order type</summary>
		public EOrderType Type { get; set; }

		/// <summary>How long the order is valid for</summary>
		public ETimeInForce TimeInForce { get; set; }

		/// <summary>The amount to trade</summary>
		public decimal AmountBase { get; set; }

		/// <summary>The price to trade at (null for market orders)</summary>
		public decimal? PriceQ2B { get; set; }

		/// <summary>The spot price (for stop loss/take profit orders)</summary>
		public decimal? StopPriceQ2B { get; set; }

		/// <summary>The size of the amount above water</summary>
		public decimal? IcebergAmountBase { get; set; }

		/// <summary>Round parameters to match the server rules. Throws if the rounding would produce an unexpected result</summary>
		public void Canonicalise(CurrencyPair pair, BinanceApi api)
		{
			// Find the rules for 'cp'. Valid if no rules found
			var rules = api.SymbolRules[pair];
			if (rules == null)
				throw new Exception($"No server rules for symbol: {pair.Id}");

			if (Type == EOrderType.MARKET)
				PriceQ2B = null;
			else if (PriceQ2B == null)
				throw new Exception($"Order type {Type} requires a price parameter");

			if (!Type.IsAlgo())
				StopPriceQ2B = null;
			else if (StopPriceQ2B == null)
				throw new Exception($"Order type {Type} requires a spot price parameter");

			// Round to the tick size
			var price_filter = (ServerRulesData.FilterPrice)rules.Filters.FirstOrDefault(x => x.FilterType == EFilterType.PRICE_FILTER);
			if (price_filter != null)
			{
				if (PriceQ2B != null)
					PriceQ2B = price_filter.Round(PriceQ2B.Value);
				if (StopPriceQ2B != null)
					StopPriceQ2B = price_filter.Round(StopPriceQ2B.Value);
			}

			// Round to the lot size
			var filter_type = Type != EOrderType.MARKET ? EFilterType.LOT_SIZE : EFilterType.MARKET_LOT_SIZE;
			var lot_size_filter = (ServerRulesData.FilterLotSize)rules.Filters.FirstOrDefault(x => x.FilterType == filter_type);
			if (lot_size_filter != null)
			{
				AmountBase = lot_size_filter.Round(AmountBase);
			}
		}

		/// <summary>Validate these parameters against the server rules</summary>
		public Exception Validate(CurrencyPair pair, BinanceApi api)
		{
			// Find the rules for 'cp'. Valid if no rules found
			var rules = api.SymbolRules[pair];
			if (rules == null)
				return null;

			// Check the symbol is tradable
			if (rules.Status != ESymbolStatus.TRADING)
				return new Exception($"{pair.Id} is not available for trading");
			if (!rules.IsSpotTradingAllowed)
				return new Exception($"Spot trading {pair.Id} is not allowed");
			if (!rules.OrderTypes.Contains(Type))
				return new Exception($"Order type {Type} is not support for {pair.Id}");

			if (PriceQ2B == null && Type != EOrderType.MARKET)
				return new Exception($"Order type {Type} requires a price parameter");
			if (StopPriceQ2B == null && Type.IsAlgo())
				return new Exception($"Order type {Type} requires a spot price parameter");

			// Validate against the filters
			foreach (var filter in rules.Filters)
			{
				switch (filter.FilterType)
				{
				default: throw new Exception($"Unknown filter type: {filter.FilterType}");
				case EFilterType.PRICE_FILTER:
					{
						var f = (ServerRulesData.FilterPrice)filter;

						if (PriceQ2B != null && PriceQ2B.Value > f.MaxPrice)
							return new Exception($"Trade price ({PriceQ2B.Value}) above maximum ({f.MaxPrice})");
						if (StopPriceQ2B != null && StopPriceQ2B.Value > f.MaxPrice)
							return new Exception($"Stop price ({StopPriceQ2B.Value}) above maximum ({f.MaxPrice})");

						if (PriceQ2B != null && PriceQ2B.Value < f.MinPrice)
							return new Exception($"Trade price ({PriceQ2B.Value}) below minimum ({f.MinPrice})");
						if (StopPriceQ2B != null && StopPriceQ2B.Value < f.MinPrice)
							return new Exception($"Stop price ({StopPriceQ2B.Value}) below minimum ({f.MinPrice})");

						if (PriceQ2B != null && (PriceQ2B.Value - f.MinPrice) % f.TickSize != 0)
							return new Exception($"Trade price ({PriceQ2B.Value}) must be a mulitple of the minimum tick size ({f.TickSize})");
						if (StopPriceQ2B != null && (StopPriceQ2B.Value - f.MinPrice) % f.TickSize != 0)
							return new Exception($"Stop price ({StopPriceQ2B.Value}) must be a mulitple of the minimum tick size ({f.TickSize})");

						break;
					}
				case EFilterType.PERCENT_PRICE:
					{
						var f = (ServerRulesData.FilterPercentPrice)filter;
						var ticker = api.TickerData[pair];
						var lo = (decimal)ticker.WeightedAvgPrice * f.MultiplierDown;
						var hi = (decimal)ticker.WeightedAvgPrice * f.MultiplierUp;

						if (PriceQ2B != null && PriceQ2B.Value < lo)
							return new Exception($"Trade price ({PriceQ2B.Value}) is below the minimum average price band ({lo})");
						if (StopPriceQ2B != null && StopPriceQ2B.Value < lo)
							return new Exception($"Stop price ({StopPriceQ2B.Value}) is below the minimum average price band ({lo})");

						if (PriceQ2B != null && PriceQ2B.Value > hi)
							return new Exception($"Trade price ({PriceQ2B.Value}) is above the maximum average price band ({hi})");
						if (StopPriceQ2B != null && StopPriceQ2B.Value > hi)
							return new Exception($"Stop price ({StopPriceQ2B.Value}) is above the maximum average price band ({hi})");

						break;
					}
				case EFilterType.LOT_SIZE:
				case EFilterType.MARKET_LOT_SIZE:
					{
						var f = (ServerRulesData.FilterLotSize)filter;
						if ((Type == EOrderType.MARKET) == (filter.FilterType == EFilterType.MARKET_LOT_SIZE))
						{
							if (AmountBase > f.MaxQuantity)
								return new Exception($"Trade amount ({AmountBase}) is above the maximum amount ({f.MaxQuantity})");
							if (AmountBase < f.MinQuantity)
								return new Exception($"Trade amount ({AmountBase}) is below the minimum amount ({f.MinQuantity})");
							if ((AmountBase - f.MinQuantity) % f.StepSize != 0)
								return new Exception($"Trade amount ({AmountBase}) must be a multiple of the step size ({f.StepSize})");
						}
						break;
					}
				case EFilterType.MIN_NOTIONAL:
					{
						var f = (ServerRulesData.FilterMinNotional)filter;
						if (Type != EOrderType.MARKET || f.ApplyToMarketOrders)
						{
							var ticker = api.TickerData[pair];
							var price_q2b = Type != EOrderType.MARKET ? PriceQ2B.Value : (decimal)ticker.PriceQ2B;
							var value = price_q2b * AmountBase;

							if (value < f.MinNotional)
								return new Exception($"Trade notional value ({value}) is below the minimum ({f.MinNotional})");
						}
						break;
					}
				case EFilterType.ICEBERG_PARTS:
					{
						var f = (ServerRulesData.FilterLimit)filter;
						if (IcebergAmountBase is decimal iceberg_amount)
						{
							var parts = Math.Ceiling(AmountBase / iceberg_amount);
							if (parts > f.Limit)
								return new Exception($"Number of iceberg parts ({parts}) is above the limit ({f.Limit})");
						}
						break;
					}
				case EFilterType.MAX_NUM_ORDERS:
					{
						var f = (ServerRulesData.FilterLimit)filter;
						var num = api.UserData.Orders[pair].Count;
						if (num > f.Limit)
							return new Exception($"Creating this trade would exceed the number of allowed orders. (Limit = ({f.Limit})");
						break;
					}
				case EFilterType.MAX_NUM_ALGO_ORDERS:
					{
						var f = (ServerRulesData.FilterLimit)filter;
						if (Type.IsAlgo())
						{
							var num = api.UserData.Orders[pair].Count(x => x.OrderType.IsAlgo());
							if (num > f.Limit)
								return new Exception($"Creating this trade would exceed the number of allowed algorithm orders. (Limit = ({f.Limit})");
						}
						break;
					}
				case EFilterType.MAX_NUM_ICEBERG_ORDERS:
					{
						var f = (ServerRulesData.FilterLimit)filter;
						if (IcebergAmountBase > 0)
						{
							var num = api.UserData.Orders[pair].Count(x => x.IcebergAmount > 0);
							if (num > f.Limit)
								return new Exception($"Creating this trade would exceed the number of allowed iceberg orders. (Limit = ({f.Limit})");
						}
						break;
					}
				case EFilterType.EXCHANGE_MAX_NUM_ORDERS:
					{
						// Todo, need the total number of orders on the exchange
						break;
					}
				case EFilterType.EXCHANGE_MAX_NUM_ALGO_ORDERS:
					{
						// Todo, need the total number of "Algo" orders on the exchange
						break;
					}
				}
			}

			// Sweet as bro
			return null;
		}
	}
}
