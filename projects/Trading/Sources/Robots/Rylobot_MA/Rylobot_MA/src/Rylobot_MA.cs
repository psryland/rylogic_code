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
	internal class Rylobot_MA :Rylobot
	{
		#region Parameters
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			MA0 = Indicator.EMA("MA0", Instrument, 10);
			MA1 = Indicator.EMA("MA1", Instrument, 20);
			MA2 = Indicator.EMA("MA2", Instrument, 50);
			MA3 = Indicator.EMA("MA3", Instrument, 100);
		}
		protected override void OnStop()
		{
			base.OnStop();
		}

		/// <summary>Moving averages for determining trend sign</summary>
		public Indicator MA0 { get; private set; }
		public Indicator MA1 { get; private set; }
		public Indicator MA2 { get; private set; }
		public Indicator MA3 { get; private set; }

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, indicators:new[] { MA0, MA1, MA2 });
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
		}

		/// <summary>Position opened</summary>
		protected override void OnPositionOpened(Position position)
		{}

		/// <summary>Position closed</summary>
		protected override void OnPositionClosed(Position position)
		{}
	}
}
