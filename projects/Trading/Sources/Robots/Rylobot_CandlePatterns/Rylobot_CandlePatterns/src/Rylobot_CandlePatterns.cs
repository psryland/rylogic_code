using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using pr.common;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot_CandlePatterns :Rylobot
	{
		#region Parameters
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
		}
		protected override void OnStop()
		{
			// Log the whole instrument
			Debugging.Dump(Instrument);

			var colours = new uint[]
			{
				0x7F0080FF,// Stall1,
				0x7F008080,// Stall2,
				0x7F0000FF,// Reversal1,
				0x7F0000A0,// Reversal2,
				0x7F000080,// Reversal3,
				0x7FFF0000,// Engulfing1,
				0x7FA00000,// Engulfing2,
				0x7F00FF00,// Spike,
			};

			// Show the identified candle pattern
			Debugging.ClearFile(Debugging.FP("area_of_interest.ldr"));
			for (var i = Instrument.IdxFirst + 100; i < Instrument.IdxLast; ++i)
			{
				CandlePattern patn;
				var pattern = Instrument.IsCandlePattern(out patn, idx_:i);
				if (pattern != null)
				{
					Debugging.AreaOfInterest(patn.Range, col:colours[(int)patn.Pattern], solid:true);
				}
			}

			base.OnStop();
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new RangeF(-100,1), high_res:20.0);
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			if (!Positions.Any() && !PendingOrders.Any())
			{
				// Look for a candle pattern
				CandlePattern patn;
				var pattern = Instrument.IsCandlePattern(out patn);
			//	if (pattern == ECandlePattern.Reversal1 ||
			//		pattern == ECandlePattern.Reversal2 ||
			//		pattern == ECandlePattern.Reversal3 ||
			//		false)
			//	{
			//		Debugging.AreaOfInterest(patn.Range);
			//		Dump();
			//
			//		var exit = Instrument.ChooseTradeExit(patn.TT, patn.EP, risk:Risk);
			//		var trade = new Trade(Instrument, patn.TT, Label, patn.EP, exit.SL, null, exit.Volume) { Expiration = Instrument.ExpirationTime(2) };
			//		Broker.CreatePendingOrder(trade);
			//	}
			}
		}

		/// <summary>Position opened</summary>
		protected override void OnPositionOpened(Position position)
		{
			PositionManagers.Add(new PositionManagerCandlePattern(this, position));
		}

		/// <summary>Position closed</summary>
		protected override void OnPositionClosed(Position position)
		{}
	}

	/// <summary>Exit at a reversal candle pattern</summary>
	public class PositionManagerCandlePattern :PositionManager
	{
		public PositionManagerCandlePattern(Rylobot bot, Position position)
			:base(bot, position)
		{}

		/// <summary></summary>
		protected override void StepCore()
		{
			// Look for a candle pattern that indicators trend in the other direction
			CandlePattern patn;
			var pattern = Instrument.IsCandlePattern(out patn);
			if (pattern != null && patn.TT != Position.TradeType)
				Broker.ClosePosition(Position);
		}
	}
}
