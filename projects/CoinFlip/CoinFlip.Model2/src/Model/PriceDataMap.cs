using System;
using System.Collections.Generic;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class PriceDataMap : IDisposable
	{
		public class TPMap : Dictionary<TradePair, TFMap> { }
		public class TFMap : Dictionary<ETimeFrame, PriceData> { }

		public PriceDataMap(CancellationToken shutdown)
		{
			Pairs = new TPMap();
			Shutdown = shutdown;
		}
		public void Dispose()
		{
			foreach (var tf_map in Pairs.Values)
				foreach (var pd in tf_map.Values)
					Util.Dispose(pd);

			Pairs.Clear();
		}

		/// <summary></summary>
		public TPMap Pairs { get; }

		/// <summary></summary>
		public PriceData this[TradePair pair, ETimeFrame time_frame]
		{
			get
			{
				if (pair == null)
					throw new ArgumentNullException(nameof(pair));
				if (time_frame == ETimeFrame.None)
					throw new Exception("TimeFrame is None. No price data available");
				if (!pair.Exchange.Enabled)
					throw new Exception("Requesting a trading pair on an inactive exchange");

				// Get the TimeFrame to PriceData map for the given pair
				var tf_map = Pairs.TryGetValue(pair, out var tf) ? tf : Pairs.Add2(pair, new TFMap());

				// Get the price data for the given time frame
				return tf_map.TryGetValue(time_frame, out var pd) ? pd : tf_map.Add2(time_frame, new PriceData(pair, time_frame, Shutdown));
			}
		}

		/// <summary>Application shutdown token</summary>
		private CancellationToken Shutdown { get; }
	}
}
