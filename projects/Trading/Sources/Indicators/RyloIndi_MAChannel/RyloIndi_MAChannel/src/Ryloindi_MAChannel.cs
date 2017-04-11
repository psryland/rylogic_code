using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using pr.common;
using pr.maths;

namespace Rylobot
{
	[Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
	public class Ryloindi_MAChannel : cAlgo.API.Indicator
	{
		#region Parameters
		[Parameter(DefaultValue = 20)]
		public int Periods { get; set; }

		[Parameter(DefaultValue = 1.0, MinValue = 0.0)]
		public double Distance { get; set; }

		[Parameter(DefaultValue = 1.0)]
		public double SlopeWeight { get; set; }
		#endregion

		protected override void Initialize()
		{
			Instrument = new Instrument(this, MarketSeries);
			MA = Indicator.EMA("ema", Instrument, Periods);
		}

		private Instrument Instrument { get; set; }
		private Indicator MA { get; set; }

		/// <summary></summary>
		[Output("Top", Color = Colors.Red)]
		public IndicatorDataSeries Top { get; set; }

		/// <summary></summary>
		[Output("Main", Color = Colors.MediumPurple)]
		public IndicatorDataSeries MA0 { get; set; }

		/// <summary></summary>
		[Output("Bottom", Color = Colors.Green)]
		public IndicatorDataSeries Bot { get; set; }

		public override void Calculate(int index)
		{
			var idx = index + Instrument.IdxFirst;
			var mcs = Instrument.EMATrueRange(idx - Periods, idx);
			var extrp = MA.Extrapolate(1, Periods, idx_: idx);
			var slope = extrp != null ? ((Monic)extrp.Curve).A : MA.FirstDerivative(idx);
			var ma = MA[idx];

			MA0[index] = ma;
			Top[index] = ma + Distance * mcs + SlopeWeight * slope;
			Bot[index] = ma - Distance * mcs + SlopeWeight * slope;
		}
	}
}
