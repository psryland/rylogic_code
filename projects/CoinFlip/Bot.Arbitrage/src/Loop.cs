using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinFlip;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Bot.Arbitrage
{
	/// <summary>A closed loop series of trades</summary>
	[DebuggerDisplay("{CoinsString(Direction),nq}")]
	public class Loop
	{
		// Note: a closed loop is one that starts and ends at the same currency
		// but not necessarily on the same exchange.

		public Loop(TradePair pair)
		{
			Pairs = new List<TradePair>();
			Rate = null;
			Direction = 0;
			TradeScale = 1m;
			TradeVolume = 0m._(pair.Base);
			Profit = 0m._(pair.Base);
			ProfitRatioFwd = 0;
			ProfitRatioBck = 0;

			Pairs.Add(pair);
		}
		public Loop(Loop rhs)
		{
			Pairs = rhs.Pairs.ToList();
			Rate = rhs.Rate;
			Direction = rhs.Direction;
			TradeScale = rhs.TradeScale;
			TradeVolume = rhs.TradeVolume;
			Profit = rhs.Profit;
			ProfitRatioFwd = rhs.ProfitRatioFwd;
			ProfitRatioBck = rhs.ProfitRatioBck;
		}
		public Loop(Loop loop, OrderBook rate, int direction)
			: this(loop)
		{
			Rate = rate;
			Direction = direction;
		}

		/// <summary>An ordered sequence of trading pairs</summary>
		public List<TradePair> Pairs { get; private set; }

		/// <summary>The first coin in the (possibly partial) loop</summary>
		public Coin Beg => Direction >= 0 ? BegInternal : EndInternal;
		private Coin BegInternal => Pairs.Count > 1 ? Pairs.Front().OtherCoin(TradePair.CommonCoin(Pairs.Front(0), Pairs.Front(1))) : Pairs.Front().Base;

		/// <summary>The last coin in the (possibly partial) loop</summary>
		public Coin End => Direction >= 0 ? EndInternal : BegInternal;
		private Coin EndInternal => Pairs.Count > 1 ? Pairs.Back().OtherCoin(TradePair.CommonCoin(Pairs.Back(0), Pairs.Back(1))) : Pairs.Back().Quote;

		/// <summary>The accumulated exchange rate when cycling forward/backward around this loop (indicated by Direction)</summary>
		public OrderBook Rate { get; private set; }

		/// <summary>The direction to go around the loop</summary>
		public int Direction { get; private set; }

		/// <summary>A scaling factor for this loop based on the available balances</summary>
		public decimal TradeScale { get; set; }

		/// <summary>The maximum profitable trade volume (in the starting currency)</summary>
		public Unit<decimal> TradeVolume { get; set; }

		/// <summary>The gain in volume of this loop</summary>
		public Unit<decimal> Profit { get; set; }

		/// <summary>The profitability of this loop as a ratio when going forward around the loop (volume final / volume initial)</summary>
		public decimal ProfitRatioFwd { get; set; }

		/// <summary>The profitability of this loop as a ratio when going backward around the loop (volume final / volume initial)</summary>
		public decimal ProfitRatioBck { get; set; }

		/// <summary>The maximum profit ratio</summary>
		public decimal ProfitRatio => Math.Max(ProfitRatioFwd, ProfitRatioBck);

		/// <summary>The trading pair that limits the volume </summary>
		public Coin LimitingCoin { get; set; }

		/// <summary>The loop described as a string</summary>
		public string Description => CoinsString(Direction);

		/// <summary>A string description of why this loop is trade-able or not</summary>
		public string Tradeability { get; set; }

		/// <summary>Return a string describing the loop</summary>
		public string CoinsString(int direction)
		{
			return string.Join("→", EnumCoins(direction).Select(x => x.Symbol));
		}

		/// <summary>Enumerate the pairs forward or backward around the loop</summary>
		public IEnumerable<TradePair> EnumPairs(int direction)
		{
			if (direction >= 0)
				for (int i = 0; i != Pairs.Count; ++i)
					yield return Pairs[i];
			else
				for (int i = Pairs.Count; i-- != 0;)
					yield return Pairs[i];
		}

		///// <summary>Enumerate the coins forward or backward around the loop</summary>
		public IEnumerable<Coin> EnumCoins(int direction)
		{
			var c = direction >= 0 ? BegInternal : EndInternal;
			yield return c;
			foreach (var pair in EnumPairs(direction))
			{
				c = pair.OtherCoin(c);
				yield return c;
			}
		}

		/// <summary>Generate a unique key for all distinct loops</summary>
		public int HashKey
		{
			get
			{
				var hash0 = FNV1a.Hash(0);
				var hash1 = FNV1a.Hash(0);

				// Closed loops start and finish with the same coin on the same exchange.
				// "Open" loops start and finish with the same currency, but not on the same exchange.
				// We test for profitability in both directions around the loop.
				// Actually, all loops are closed because all open loops have an implicit cross-exchange
				// pair that links End -> Beg. This means all loops with the same order, in either direction
				// are equivalent.

				var closed = Beg == End;

				// Loop equivalents: [A→B→C] == [B→C→A] == [C→B→A]
				var len = Pairs.Count + (closed ? 0 : 1);
				var coins = EnumCoins(+1).Take(len).Select(x => x.SymbolWithExchange).ToArray();

				// Find the lexicographically lowest coin name
				var idx = coins.IndexOfMinBy(x => x);

				// Calculate the hash going in both directions and choose the lowest
				for (int i = 0; i != len; ++i)
				{
					var s0 = coins[(idx + i + 2 * len) % len];
					var s1 = coins[(idx - i + 2 * len) % len];
					hash0 = FNV1a.Hash(s0, hash0);
					hash1 = FNV1a.Hash(s1, hash1);
				}

				return Math.Min(hash0, hash1);
			}
		}

		/// <summary>
		/// Determine if executing trades in 'loop' should result in a profit.
		/// Returns true if profitable and a copy of this loop in 'loop'</summary>
		public bool IsProfitable(bool forward, Fund fund, out Loop loop)
		{
			// How to think about this:
			// - We want to see what happens if we convert some currency to each of the coins
			//   in the loop, ending up back at the initial currency. If the result is more than
			//   we started with, then it's a profitable loop.
			// - We can go in either direction around the loop.
			// - We want to execute each trade around a profitable loop at the same time, so we're
			//   limited to the smallest balance for the coins in the loop.
			// - The rate by volume does not depend on our account balance. We calculate the effective
			//   rate at each of the offered volumes then determine if any of those volumes are profitable
			//   and whether we have enough balance for the given volumes.
			// - The 'Bid' table contains the amounts of base currency people want to buy, ordered by price.
			// - The 'Ask' table contains the amounts of base currency people want to sell, ordered by price.
			loop = null;

			// Construct an "order book" of volumes and complete-loop prices (e.g. BTC to BTC price for each volume)
			var dir = forward ? +1 : -1;
			var coin = forward ? Beg : End;
			var tt = forward ? ETradeType.B2Q : ETradeType.Q2B;
			var obk = new OrderBook(coin, coin, tt) { new Offer(1m, decimal.MaxValue._(coin.Symbol)) };
			foreach (var pair in EnumPairs(dir))
			{
				// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
				Unit<decimal> bal = 0m;
				OrderBook b2q = null, q2b = null;
				using (Task_.NoSyncContext())
				{
					Misc.RunOnMainThread(() =>
					{
						bal = coin.Balances[fund].Available;
						b2q = new OrderBook(pair.MarketDepth.B2Q);
						q2b = new OrderBook(pair.MarketDepth.Q2B);
					}).Wait();
				}

				// Note: the trade prices are in quote currency
				if (pair.Base == coin)
					obk = MergeRates(obk, b2q, bal, invert: false);
				else if (pair.Quote == coin)
					obk = MergeRates(obk, q2b, bal, invert: true);
				else
					throw new Exception($"Pair {pair} does not include Coin {coin}. Loop is invalid.");

				// Get the next coin in the loop
				coin = pair.OtherCoin(coin);
			}
			if (obk.Count == 0)
				return false;

			// Save the best profit ratio for this loop (as an indication)
			if (forward)
				ProfitRatioFwd = obk[0].PriceQ2B;
			else
				ProfitRatioBck = obk[0].PriceQ2B;

			// Look for any volumes that have a nett gain
			var amount_gain = obk.Where(x => x.PriceQ2B > 1).Sum(x => x.PriceQ2B * x.AmountBase);
			if (amount_gain == 0)
				return false;

			// Create a copy of the loop for editing (with the direction set)
			loop = new Loop(this, obk, dir);

			// Find the maximum profitable volume to trade
			var amount = 0m._(loop.Beg);
			foreach (var ordr in loop.Rate.Where(x => x.PriceQ2B > 1))
				amount += ordr.AmountBase;

			// Calculate the effective fee in initial coin currency.
			// Do all trades assuming no fee, but accumulate the fee separately
			var fee = 0m._(loop.Beg);
			var initial_volume = amount;

			// Trade each pair in the loop (in the given direction) to check 
			// that the trade is still profitable after fees. Record each trade
			// so that we can determine the trade scale
			coin = loop.Beg;
			var trades = new List<Trade>();
			foreach (var pair in loop.EnumPairs(loop.Direction))
			{
				// If we trade 'volume' using 'pair' that will result in a new volume
				// in the new currency. There will also be a fee charged (in quote currency).
				// If we're trading to quote currency, the new volume is reduced by the fee.
				// If we're trading to base currency, the cost is increased by the fee.

				// Calculate the result of the trade
				var new_coin = pair.OtherCoin(coin);
				var trade = pair.Base == coin
					? pair.BaseToQuote(fund, amount)
					: pair.QuoteToBase(fund, amount);

				// Record the trade amount.
				trades.Add(trade);

				// Convert the fee so far to the new coin using the effective rate,
				// and add on the fee for this trade.
				var rate = trade.AmountOut / trade.AmountIn;
				fee = fee * rate + trade.AmountOut * pair.Fee;

				// Advance to the next pair
				coin = new_coin;
				amount = trade.AmountOut;
			}

			// Record the volume to trade, the scale, and the expected profit.
			// If the new volume is greater than the initial volume, WIN!
			// Update the profitability of the loop now we've accounted for fees.
			loop.TradeScale = 1m;
			loop.Tradeability = string.Empty;
			loop.TradeVolume = initial_volume;
			loop.Profit = (amount - fee) - initial_volume;
			if (forward) loop.ProfitRatioFwd = ProfitRatioFwd = (amount - fee) / initial_volume;
			else loop.ProfitRatioBck = ProfitRatioBck = (amount - fee) / initial_volume;
			if (loop.ProfitRatio <= 1m)
				return false;

			// Determine the trade scale based on the available balances
			foreach (var trade in trades)
			{
				var pair = trade.Pair;

				// Get the balance available for this trade and determine a trade scaling factor.
				// Increase the required volume to allow for the fee
				// Reduce the available balance slightly to deal with rounding errors
				var bal = trade.CoinIn.Balances[fund].Available * 0.999m;
				var req = trade.AmountIn * (1 + pair.Fee);
				var scale = Math_.Clamp((decimal)(bal / req), 0m, 1m);
				if (scale < loop.TradeScale)
				{
					loop.TradeScale = Math_.Clamp(scale, 0, loop.TradeScale);
					loop.LimitingCoin = trade.CoinIn;
				}
			}

			// Check that all traded volumes are within the limits
			var all_trades_valid = EValidation.Valid;
			foreach (var trade in trades)
			{
				// Check the unscaled amount, if that's too small we'll ignore this loop
				var validation0 = trade.Validate();
				all_trades_valid |= validation0;

				// Record why the base trade isn't valid
				if (validation0 != EValidation.Valid)
					loop.Tradeability += $"{trade.Description} - {validation0}\n";

				// If the volume to trade, multiplied by the trade scale, is outside the
				// allowed range of trading volume, set the scale to zero. This is to prevent
				// loops being traded where part of the loop would be rejected.
				var validation1 = new Trade(trade, loop.TradeScale).Validate();
				if (validation1.HasFlag(EValidation.AmountInOutOfRange))
					loop.Tradeability += $"Not enough {trade.CoinIn} to trade\n";
				if (validation1.HasFlag(EValidation.AmountOutOutOfRange))
					loop.Tradeability += $"Trade result volume of {trade.CoinOut} is too small\n";
				if (validation1 != EValidation.Valid)
					loop.TradeScale = 0m;
			}

			// Return the profitable loop (even if scaled to 0)
			return all_trades_valid == EValidation.Valid;
		}

		/// <summary>Determine the exchange rate based on volume</summary>
		private static OrderBook MergeRates(OrderBook rates, OrderBook offers, Unit<decimal> balance, bool invert) // Worker thread context
		{
			// 'rates' is a table of volumes in the current coin currency (i.e. the current
			// coin in the loop) along with the accumulated exchange rate for each volume.
			// 'orders' is a table of 'Base' currency volumes and the offer prices for
			// converting those volumes to 'Quote' currency.
			// If 'invert' is true, the 'orders' table is the offers for converting Quote
			// currency to Base currency, however the volumes and prices are still in Base
			// and Quote respectively.
			var new_coin = invert ? offers.Base : offers.Quote;
			var ret = new OrderBook(new_coin, new_coin, rates.TradeType);

			// Volume accumulators for the 'rates' and 'orders' order books.
			var R_vol = 0m._(rates.Base.Symbol);
			var O_vol = 0m._(rates.Base.Symbol);

			// The maximum volume available to trade is the minimum of the 'rates' and 'orders'
			// volumes, hence this loop ends when the last order in either set is reached.
			for (int r = 0, o = 0; r != rates.Count && o != offers.Count;)
			{
				var rate = rates[r];
				var offr = offers[o];

				// Get the offer price and volume to convert to the current coin currency
				var price = invert ? 1m / offr.PriceQ2B : offr.PriceQ2B;
				var volume = invert ? offr.PriceQ2B * offr.AmountBase : offr.AmountBase;

				// Get the volume available to be traded at 'price'
				var vol0 = rate.AmountBase < volume ? rate.AmountBase : volume;

				// Convert this volume to the new currency using 'price'
				var vol1 = vol0 * price;

				// Record the volume and the combined rate
				ret.Add(new Offer(rate.PriceQ2B * price, vol1), validate: false);

				// Move to the next order in the set with the lowest accumulative volume.
				// If the same accumulative volume is in both sets, move to the next order in both sets.
				// Need to be careful with overflow, because special case values use decimal.MaxValue.
				// Only advance to the next order if the accumulative volume is less than MaxValue.
				var adv_R =
					(R_vol < decimal.MaxValue - rate.AmountBase) && // if 'R_vol + rate.AmountBase' does not overflow
					(R_vol - O_vol <= volume - rate.AmountBase);    // and 'R_vol + rate.AmountBase' <= 'O_vol + volume'
				var adv_O =
					(O_vol < decimal.MaxValue - volume) &&          // if 'O_vol + volume' does not overflow
					(R_vol - O_vol >= volume - rate.AmountBase);    // and 'R_vol + rate.AmountBase' >= 'O_vol + volume'
				if (adv_R) { ++r; R_vol += rate.AmountBase; }
				if (adv_O) { ++o; O_vol += volume; }
				if (!adv_R && !adv_O)
					break;

				// Don't bother calculating for volumes that exceed the current balance
				if (rates.Count > 5 && R_vol > balance)
					break;
			}

			return ret;
		}
	}

	[DebuggerDisplay("{Coin} {Msg}")]
	public class InsufficientCoin
	{
		public InsufficientCoin(Coin coin, string msg)
		{
			Coin = coin;
			Msg = msg;
		}

		/// <summary>The coin that there isn't enough of if the TradeScale is 0</summary>
		public Coin Coin { get; set; }

		/// <summary>The reason the volume is insufficient</summary>
		public string Msg { get; set; }
	}
}
