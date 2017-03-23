using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyBreakOut :Strategy
	{
		// Notes:
		//  - Identify channels
		//  - When price breaks the channel

		const int MaxPositions = 1;

		public StrategyBreakOut(Rylobot bot, double risk)
			:base(bot, "StrategyBreakOut", risk)
		{
			MA = Indicator.EMA(Instrument, 15);
			Debugging.DumpInstrument += Dump;
		}
		public override void Dispose()
		{
			Debugging.DumpInstrument -= Dump;
			base.Dispose();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return Channel != null ? 1.0 : 0.0; }
		}

		/// <summary>The current price channel (or null)</summary>
		private PriceChannel? Channel
		{
			get
			{
				// When a new candle arrives, look for a channel
				if (Instrument.Count != m_candle_count)
				{
					m_channel = FindChannel(0);
					m_candle_count = Instrument.Count;
				}
				return m_channel;
			}
		}
		private PriceChannel? m_channel;
		private int m_candle_count;

		/// <summary>A MA for validating channels</summary>
		private Indicator MA
		{
			get;
			set;
		}

		/// <summary>Debugging, output current state</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100,1));
			Debugging.Dump("channel.ldr", ldr =>
			{
				if (Channel == null) return;
				var ch = Channel.Value;
				ldr.Rect("channel", 0xFF0000FF, AxisId.PosZ, ch.Idx.Sizei, ch.Price.Sizef, false,
					new v4((float)ch.Idx.Midf, ch.Price.Midf, 0f, 1f));
			});
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (!Instrument.NewCandle)
				return;

			var mcs = Instrument.MCS;

			// If there is an existing position, wait for it to close
			if (Positions.Count() >= MaxPositions)
				return;

			// If there is no price channel
			if (Channel == null)
				return;

			// Cancel any pending orders and replace them with new ones based on this channel
			Broker.CancelAllPendingOrders(Label);

			Dump();

			// The Channel
			var chan = Channel.Value;
			var risk = Risk / MaxPositions;
			var sign = chan.PrecedingTrendSign;

	//		// Create a pending order on the side opposite the preceding trend
	//		var ep = sign > 0 ? chan.Price.End + 0.5*mcs : chan.Price.Beg - 0.5*mcs;
	//		var sl = (QuoteCurrency?)null;//sign > 0 ? chan.Price.Beg : chan.Price.End;
	//		var tp = (QuoteCurrency?)null;//ep + sign * Math.Abs(ep - sl);
	//		var sl_rel = 5*mcs;//Math.Abs(ep - sl);
	//		var vol = Broker.ChooseVolume(Instrument, sl_rel, risk:risk);
	//		var trade = new Trade(Instrument, CAlgo.SignToTradeType(chan.PrecedingTrendSign), Label, ep, sl, tp, vol);
	//		trade.Expiration = Instrument.ExpirationTime(5);
	//		Broker.CreatePendingOrder(trade);

			// Create pending orders on either side of the channel
			{
				var ep = chan.Price.End + 0.5*mcs;
				var sl = chan.Price.Beg;
				var tp = (QuoteCurrency?)null;// ep + chan.Price.Size * 5;
				var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:risk);
				var trade = new Trade(Instrument, TradeType.Buy, Label, ep, sl, tp, vol);
				trade.Expiration = Instrument.ExpirationTime(5);
				Broker.CreatePendingOrder(trade);
			}
			{
				var ep = chan.Price.Beg - 0.5*mcs;
				var sl = chan.Price.End;
				var tp = (QuoteCurrency?)null;// ep - chan.Price.Size * 5;
				var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:risk);
				var trade = new Trade(Instrument, TradeType.Sell, Label, ep, sl, tp, vol);
				trade.Expiration = Instrument.ExpirationTime(5);
				Broker.CreatePendingOrder(trade);
			}

			// Debugging
			{
				var c = chan;
				for (;Channels.Count != 0 && c.Idx.Contains(Channels.Back().Idx);) Channels.PopBack();
				Channels.Add(c);
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
			// When a position opens, cancel any pending orders
			Broker.CancelAllPendingOrders(Label);

			// Manage the position
			//PositionManagers.Add(new PositionManagerNervious(this, position));
			PositionManagers.Add(new PositionManagerCandleFollow(this, position, 7));
			//PositionManagers.Add(new PositionManagerPeakPC(this, position));
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Looks for a price channel in the recent candle history</summary>
		private PriceChannel? FindChannel(Idx iend)
		{
			const int MinCandlesPerChannel = 12;
			const int MaxCandlesPerChannel = 30;
			const double AspectThreshold = 0.5;
			var channel = (PriceChannel?)null;

			// Find the channel with the best aspect ratio
			var count = 0;
			var range = RangeF.Invalid;
			var best_aspect = AspectThreshold;
			for (var i = iend; i != Instrument.IdxFirst; --i, ++count)
			{
				range.Encompass(Instrument[i].Open);
				range.Encompass(Instrument[i].Close);
				if (count < MinCandlesPerChannel) continue;
				if (count > MaxCandlesPerChannel) break;

				// Aspect ratio of the channel (width/height)
				var aspect = count * Instrument.PipSize / range.Size;
				if (aspect > best_aspect)
				{
					var idx_range = new Range(i,1).Shift(-Instrument.IdxFirst);
					
					// Eliminate channels that are not preceded by a trend
					var d = MA[i] - MA[2*i];
					if (Math.Abs(d) < 2*range.Size)
						continue;

				//	// Eliminate channels where price is not evenly distributed within the channel
				//	var c = new Correlation();
				//	int_.Range(i,1).ForEach(x => c.Add(x, Instrument[x].Close));
				//	if (Math.Abs(c.LinearRegression.A) > 0.5 * range.Size / idx_range.Size)
				//		continue;

					// Possible channel
					channel = new PriceChannel(idx_range, range, Math.Sign(d));
					best_aspect = aspect;
				}
			}

			return channel;
		}
		private struct PriceChannel
		{
			public PriceChannel(Range calgo_idx, RangeF price, int preceding_trend_sign)
			{
				Idx                = calgo_idx;
				Price              = price;
				PrecedingTrendSign = preceding_trend_sign;
			}

			/// <summary>CAlgo index range</summary>
			public Range Idx;

			/// <summary>Price range of the channel</summary>
			public RangeF Price;

			/// <summary>The direction of the preceding trend</summary>
			public int PrecedingTrendSign;

			#region Equals
			public static bool operator == (PriceChannel lhs, PriceChannel rhs)
			{
				return lhs.Equals(rhs);
			}
			public static bool operator != (PriceChannel lhs, PriceChannel rhs)
			{
				return !(lhs == rhs);
			}
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (obj.GetType() != typeof(PriceChannel)) return false;
				return Equals((PriceChannel)obj);
			}
			public bool Equals(PriceChannel other)
			{
				return Equals(Idx, other.Idx) && Equals(Price, other.Price);
			}
			public override int GetHashCode()
			{
				return new { Idx, Price }.GetHashCode();
			}
			#endregion

		}

		/// <summary>Watch for bot stopped</summary>
		protected override void OnBotStopping()
		{
			base.OnBotStopping();

			// Log the whole instrument
			Debugging.DebuggingEnabled = true;
			Debugging.ReportEdge(100);
			Debugging.Dump(Debugging.AllTrades.Values);
			Debugging.Dump(Instrument, mas:new[] {Indicator.EMA(Instrument, 5), Indicator.EMA(Instrument, 15), Indicator.EMA(Instrument, 100) });
			Debugging.Dump("channels.ldr", ldr =>
			{
				foreach (var ch in Channels)
					ldr.Rect("channel", 0xFF0000FF, AxisId.PosZ, ch.Idx.Sizei, ch.Price.Sizef, false,
						new v4((float)ch.Idx.Midf, ch.Price.Midf, 0f, 1f));
			});
		}
		List<PriceChannel> Channels = new List<PriceChannel>();
	}
}
