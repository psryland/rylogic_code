using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace Rylobot
{
	/// <summary>A template for a bot</summary>
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	internal class Rylobot_Donchian :Rylobot
	{
		#region Parameters
		/// <summary>Overall risk allowed by this bot</summary>
		[Parameter("Risk", DefaultValue = 0.1)]
		public override double Risk { get; set; }

		/// <summary>The maximum number of open position sets</summary>
		[Parameter("Max Position Sets", DefaultValue = 1)]
		public override int MaxPositionSets { get; set; }

		/// <summary>The number of periods for the Donchian channel</summary>
		[Parameter("Periods", DefaultValue = 20)]
		public int Periods { get; set; }

		/// <summary>The stop loss multiple</summary>
		[Parameter("Stop Loss Fraction", DefaultValue = 1.0)]
		public double SLFrac { get; set; }

		/// <summary>Close positions after a break of this many periods</summary>
		[Parameter("Close Breakout Periods", DefaultValue = 8)]
		public int CloseBreakoutPeriods { get; set; }

		/// <summary>Close positions if a new profit peak is not set within this many periods</summary>
		[Parameter("Close TopDrop Count", DefaultValue = 8)]
		public int CloseTopDropCount { get; set; }

		/// <summary>Close positions if a sequence of this many adverse candles are detected</summary>
		[Parameter("Close Adverse Count", DefaultValue = 4)]
		public int CloseAdverseCount { get; set; }

		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			Donchian = Indicator.Donchian("Donchian", Instrument, Periods);
		}
		protected override void OnStop()
		{
			base.OnStop();
		}

		/// <summary>The indicator</summary>
		public Indicator Donchian { get; private set; }
		private const int Top = 0;
		private const int Mid = 1;
		private const int Bot = 2;

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, indicators:new[] { Donchian });
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			// Look for trade entry
			var sets_count = PositionSets.Count + PendingOrders.Count();
			if (sets_count < MaxPositionSets && EntryCooldown == 0)
			{
				var mcs = Instrument.MCS;
				{
					var sign = -1;
					var ep = Donchian[0, Bot];
					var tt = CAlgo.SignToTradeType(sign);
					var sl = ep - sign * mcs * SLFrac;
					var tp = (QuoteCurrency?)null;
					var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol, comment:Guid.NewGuid().ToString()) { Expiration = Instrument.ExpirationTime(1) };
					Broker.CreatePendingOrder(trade);
				}
				{
					var sign = +1;
					var ep = Donchian[0, Top];
					var tt = CAlgo.SignToTradeType(sign);
					var sl = ep - sign * mcs * SLFrac;
					var tp = (QuoteCurrency?)null;
					var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol, comment:Guid.NewGuid().ToString()) { Expiration = Instrument.ExpirationTime(1) };
					Broker.CreatePendingOrder(trade);
				}
				EntryCooldown = 1;
			}
		}

		/// <summary>Position opened</summary>
		protected override void OnPositionOpened(Position position)
		{
			EntryCooldown = 5;

			// Close positions if there is a breakout in the wrong direction
			PositionManagers.Add(new PositionManagerBreakOut(this, position, CloseBreakoutPeriods, only_if_in_profit: true));

			// Close positions when they fail to make new peaks
			PositionManagers.Add(new PositionManagerTopDrop(this, position, CloseTopDropCount, only_if_in_profit: true));

			//// Close positions when there's a steady stream of adverse candles
			//PositionManagers.Add(new PositionManagerAdverseCandles(this, position, CloseAdverseCount));
		}

		/// <summary>Position closed</summary>
		protected override void OnPositionClosed(Position position)
		{}
	}
}
