using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class PriceDataMap : IDisposable, IEnumerable<PriceData>
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

		/// <summary>Return the price data for 'pair' and 'time_frame'</summary>
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

		/// <summary>Return the best matching price data for 'pair' and 'time_frame'</summary>
		public PriceData Find(string pair_name, ETimeFrame time_frame, string preferred_exchange = null)
		{
			if (pair_name == null)
				throw new ArgumentNullException(nameof(pair_name));
			if (time_frame == ETimeFrame.None)
				throw new Exception("TimeFrame is None. No price data available");

			// Search for pairs that match 'pair_name'
			var pairs = Pairs.Where(x => x.Key.Name == pair_name && x.Key.Exchange.Enabled).ToList();
			if (pairs.Count == 0)
				return null;

			// Look for price data from the preferred exchange
			if (preferred_exchange != null)
			{
				var pair = pairs.FirstOrDefault(x => x.Key.Exchange.Name == preferred_exchange);
				if (pair.Key != null && pair.Value.TryGetValue(time_frame, out var data))
					return data;
			}

			// Look for price data in any of the pairs
			foreach (var pair in pairs)
			{
				if (pair.Value.TryGetValue(time_frame, out var data))
					return data;
			}

			return null;
		}

		/// <summary>Application shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>Enumerate all price data instances</summary>
		public IEnumerator<PriceData> GetEnumerator()
		{
			foreach (var pair in Pairs.Values)
				foreach (var pd in pair.Values)
					yield return pd;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
