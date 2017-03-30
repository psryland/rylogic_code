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
	public class Rylobot_MACD : Rylobot
	{
		#region Parameters
		[Parameter(DefaultValue = 0.3)]
		public double Hysteresis { get; set; }

		[Parameter(DefaultValue = 0.5)]
		public double SlopeThreshold { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();

			MACD = Indicator.MACD("MACD", Instrument, 26, 12, 9);
		}
		protected override void OnStop()
		{
			base.OnStop();
		}

		/// <summary>MACD indicator</summary>
		public Indicator MACD { get; private set; }

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1));
			Debugging.Dump(MACD, range:new Range(-100, 1));
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			var price = Instrument.LatestPrice;
			var mcs = Instrument.MCS;
			var slope = MACD.FirstDerivative() / Instrument.PipSize;

			// Hold a trade in the direction of the MACD slope
			// Use hysteresis to switch
			var position = Positions.FirstOrDefault();
			if (position == null)
			{
				if (Math.Abs(slope) > SlopeThreshold)
				{
					var sign = Math.Sign(slope);
					var vol = Instrument.Symbol.VolumeMin;
					var trade = new Trade(Instrument, CAlgo.SignToTradeType(sign), Label, price.Price(sign), null, null, vol);
					Broker.CreateOrder(trade);
				}
			}
			else if (Instrument.NewCandle)
			{
				var sign = position.Sign();
				if (Math.Abs(slope) < SlopeThreshold - Hysteresis)
				{
					Broker.ClosePosition(position);
				}
			}
		}
	}
}
