using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	public class SnR :IDisposable
	{
		// Notes:
		// Determine the SnR levels within the given price range
		// - ignore candles that aren't within the range.

		/// <summary>Find support and resistance level data within the given price range</summary>
		/// <param name="instr">The instrument to find levels data in</param>
		/// <param name="iend">The last candle, i.e. look backwards from here</param>
		/// <param name="price">The centre price to centre the SnR level data around</param>
		/// <param name="count">The number of candles that contribute to the SnR levels (only candles intersecting the range)</param>
		/// <param name="range">The range of prices (above and below 'price') to check</param>
		public SnR(Instrument instr, QuoteCurrency? price = null, Idx? iend = null, int? count = null, QuoteCurrency? range = null, QuoteCurrency? bucket_size = null)
		{
			price = price ?? instr.LatestPrice.Mid;
			iend = iend ?? 1;

			// Set the range based on the recent span of candles
			if (range == null)
			{
				var r = instr.MCS * 5;
				foreach (var c in instr.CandleRange(iend.Value - instr.Bot.Settings.LookBackCount, iend.Value))
				{
					r = Math.Max(r, Math.Abs(c.High - price.Value));
					r = Math.Max(r, Math.Abs(price.Value - c.Low));
				}
				range = r;
			};

			SnRLevels = new List<Level>();
			Instrument = instr;
			Beg = iend.Value;
			End = iend.Value;
			Count = count ?? instr.Bot.Settings.SnRHistoryLength;
			Range = range.Value;
			Price = price.Value;
			BucketSize = (double)(bucket_size ?? instr.MCS);

			CalculateLevels();
		}
		public virtual void Dispose()
		{
			Instrument = null;
		}

		/// <summary>The instrument on which the SnR levels have been calculated</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				m_instrument = value;
			}
		}
		private Instrument m_instrument;

		/// <summary>The index of the first candle considered</summary>
		public Idx Beg { get; private set; }

		/// <summary>The index of the end candle considered</summary>
		public Idx End { get; private set; }

		/// <summary>The number of candles that contribute to the SnR levels</summary>
		public int Count { get; private set; }

		/// <summary>The centre price level</summary>
		public QuoteCurrency Price
		{
			get;
			private set;
		}

		/// <summary>The (half) price range about 'Price' that levels have been found for</summary>
		public QuoteCurrency Range
		{
			get;
			private set;
		}

		/// <summary>An ordered list of the support and resistance levels, strongest levels first</summary>
		public List<Level> SnRLevels
		{
			get;
			private set;
		}

		/// <summary>The BucketSize value used</summary>
		public double BucketSize
		{
			get;
			private set;
		}

		/// <summary>Return the price peaks and toughs in the instrument</summary>
		public IEnumerable<StationaryPoint> StationaryPoints
		{
			get
			{
				// Find the stationary points
				var d = 0;
				var count = 0;
				var data = new QuoteCurrency[3];
				for (var i = End; count != Count && i-- != Instrument.IdxFirst;)
				{
					var candle = Instrument[i];
					var price = candle.Close;
					{
						// If the candle doesn't intersect the price range skip it
						if (price < Price - Range || price > Price + Range)
						{
							// Reset the stationary point buffer
							d = 0;
							continue;
						}

						// Add the price to the stationary point detection buffer
						data[d++] = price;
						if (d == data.Length)
						{
							// Maximum
							if (data[0] < data[1] && data[1] > data[2])
								yield return new StationaryPoint(i+1, data[1], true);

							// Minimum
							if (data[0] > data[1] && data[1] < data[2])
								yield return new StationaryPoint(i+1, data[1], false);

							// Shift the prices back
							for (int j = 1; j != d; ++j) data[j-1] = data[j];
							if (++count == Count) break;
							Beg = i;
							--d;
						}
					}
				}
			}
		}

		/// <summary>
		/// Return the nearest significant support level above or below 'price'.
		/// 'sign' is the direction to look for a level. +1 means above, -1 means below, 0 means either side.
		/// 'min_dist' means the nearest SnR level >= price + sign * min_dist (default: 0 if sign=0, 0.25*MCS if sign=±1)
		/// 'range' is a bound on the levels to check (default: no range limit)
		/// 'radius' is a shortcut for Range(price - radius, price + radius). (Default: ignored) </summary>
		public Level Nearest(QuoteCurrency price, int sign, QuoteCurrency? min_dist = null, RangeF? range = null, QuoteCurrency? radius = null, double? min_strength = 0.5)
		{
			// Set the range
			if (radius != null && range == null)
				range = new RangeF((double)(price - radius), (double)(price + radius));

			// Set the minimum if not provided
			min_dist = min_dist ?? (sign == 0 ? 0 : 0.25*Instrument.MCS);

			// Get the threshold price
			var thresh = price + sign * min_dist.Value;

			// Find levels above/below the threshold
			var levels = (IEnumerable<Level>)SnRLevels;
			if (range != null)        levels = levels.Where(x => range.Value.Contains((double)x.Price));
			if (sign != 0)            levels = levels.Where(x => Math.Sign(x.Price - thresh) == sign);
			if (min_strength != null) levels = levels.Where(x => x.Strength >= min_strength.Value);

			// Return the levels closed to 'price'
			return levels.MinByOrDefault(x => Math.Abs(x.Price - price));
		}

		/// <summary>Identify the support and resistance levels</summary>
		private void CalculateLevels()
		{
			SnRLevels.Clear();
			for (var loop = 5; loop-- != 0;)
			{
				// Sort the stationary points into their nearest bucket
				var last_bucket = -1;
				foreach (var sp in StationaryPoints)
				{
					// Find the nearest level to 'sp'
					var idx = SnRLevels.BinarySearch(x => x.Price.CompareTo(sp.Price));
					if (idx < 0) idx = ~idx;

					// Require the price to move away from the last bucket to ensure
					// periods of consolidation don't artificially strengthen an SnR level
					if (idx == last_bucket) continue;
					last_bucket = idx;

					// Find the closest bucket
					// If the nearest bucket is further than the bucket size, insert a bucket
					var d0 = idx >               0 ? (double)(sp.Price - SnRLevels[idx-1].Price) : BucketSize;
					var d1 = idx < SnRLevels.Count ? (double)(SnRLevels[idx  ].Price - sp.Price) : BucketSize;
					if (Math.Min(d0,d1) >= BucketSize)
						SnRLevels.Insert(idx, new Level(sp.Price));
					else
						idx = d0 < d1 ? idx - 1 : idx;

					// Add 'sp' to the average for the bucket
					SnRLevels[idx].Average.Add((double)sp.Price);
				}

				// Adjust the prices to the mean for each bucket and remove empty buckets
				var done = true;
				var pip = Instrument.Symbol.PipSize;
				SnRLevels.RemoveIf(x => x.Average.Count == 0);
				foreach (var lvl in SnRLevels)
				{
					done &= Math.Abs(lvl.Price - lvl.Average.Mean) < pip;
					lvl.Price = lvl.Average.Mean;
					lvl.Strength = lvl.Average.Count;
					lvl.Average.Reset();
				}
				if (done) break;
			}

			// Determine strength values for each SnR level
			if (SnRLevels.Count != 0)
			{
				// Level strength depends on the number of stationary points
				// used to form the level. Not the number of points relative
				// to other levels.
				const int StrongCount = 3;
				foreach (var lvl in SnRLevels)
					lvl.Strength = 0.5 * Maths.Sigmoid(lvl.Strength - StrongCount, StrongCount) + 0.5;

				// Sort the levels by strength
				SnRLevels.Sort(Cmp<Level>.From((l,r) => -l.Strength.CompareTo(r.Strength)));
			}
		}

		/// <summary>A stationary point in the price data</summary>
		[DebuggerDisplay("{Index} {Price}")]
		public class StationaryPoint
		{
			public StationaryPoint(Idx index, QuoteCurrency price, bool maximum)
			{
				Index   = index;
				Price   = price;
				Maximum = maximum;
			}

			/// <summary>The index in the instrument data of the stationary point</summary>
			public Idx Index { get; private set; }

			/// <summary>The price value at the stationary point</summary>
			public QuoteCurrency Price { get; private set; }

			/// <summary>True = A price maximum, otherwise a minimum</summary>
			public bool Maximum { get; private set; }
		}

		/// <summary>An identified price level</summary>
		[DebuggerDisplay("{Price} Str={Strength}")]
		public class Level
		{
			public Level(QuoteCurrency price)
			{
				Price    = price;
				Strength = 0;
				Average  = new Avr();
			}

			/// <summary>The price level</summary>
			public QuoteCurrency Price { get; set; }

			/// <summary>The strength of the SnR level</summary>
			public double Strength { get; set; }

			/// <summary>The number of stationary points at this level</summary>
			public Avr Average { get; set; }
		}
	}
}

#if false // not sure about this
		/// <summary>
		/// Construct the price density data for a range of candles in 'instr'
		/// 'ibeg' and 'iend' should be negative indices</summary>
		public SnR(Instrument instr, Idx ibeg, Idx iend)
		{
			Instrument = instr;
			Range = Instrument.IndexRange(ibeg, iend);

//			StationaryPoints = new List<StationaryPoint>();
			SnRLevels = new List<Level>();
			BucketSize = 5.0 * Instrument.Symbol.PipSize;

			CalculateLevels();
		}
		public virtual void Dispose()
		{
			Instrument = null;
		}

		/// <summary>The candle range used</summary>
		public Range Range
		{
			get { return m_range; }
			set { m_range = value; }
		}
		private Range m_range;

		/// <summary>Return the price peaks and toughs in the instrument high res data</summary>
		public IEnumerable<StationaryPoint> StationaryPoints
		{
			get
			{
				// Find the stationary points in the high res data
				var sign = 0;
				var prev = new PriceTick();
				var first_idx = Instrument.IdxFirst;
				foreach (var data in Instrument.HighResRange(Range))
				{
					var s = Math.Sign((double)(data.Ask - prev.Ask));
					if (s == sign) continue;
					if (sign != 0) yield return new StationaryPoint(prev.Index + (double)first_idx, prev.Ask);
					sign = s;
					prev = data;
				}
			}
		}

		///// <summary>Points where the derivative of the price is zero</summary>
		//public List<StationaryPoint> StationaryPoints
		//{
		//	get;
		//	private set;
		//}

		/// <summary>An ordered list of the support and resistance levels, strongest levels first</summary>
		public List<Level> SnRLevels
		{
			get;
			private set;
		}

		/// <summary>The BucketSize value used</summary>
		public double BucketSize
		{
			get;
			private set;
		}

		/// <summary>
		/// Return the nearest support level above or below 'price'.
		/// 'sign' is the direction to look for a level. +1 means above, -1 means below.
		/// 'soft' means the nearest SnR level >= price - sign*0.25*mcs</summary>
		public Level Nearest(QuoteCurrency price, int sign, bool soft = true)
		{
			var thresh = price - sign * (soft ? 0.25 * Instrument.MCS_50 : 0);
			var levels = SnRLevels.Where(x => Misc.Sign(x.Price - thresh) == sign);
			return levels.MinByOrDefault(x => sign * (double)(x.Price - thresh));
		}

		/// <summary>
		/// Return the nearest support level within 'range' and on the 'sign' side of 'price'.
		/// 'sign' is the direction to look for a level. +1 means above, -1 means below.</summary>
		public Level Nearest(QuoteCurrency price, int sign, RangeF range)
		{
			var levels = SnRLevels.Where(x =>
				sign * (x.Price - range.Begin) > 0 &&
				sign * (range.End   - x.Price) > 0);

			return levels.MinByOrDefault(x => (double)Misc.Abs(x.Price - price));
		}

		/// <summary>Identify the support and resistance levels</summary>
		private void CalculateLevels()
		{
			SnRLevels.Clear();
			for (var done = false; !done;)
			{
				// Sort the stationary points into their nearest bucket
				foreach (var sp in StationaryPoints)
				{
					// Find the nearest level to 'sp'
					var idx = SnRLevels.BinarySearch(x => x.Price.CompareTo(sp.Price));
					if (idx < 0) idx = ~idx;

					// Find the closest bucket
					// If the nearest bucket is further than the bucket size, insert a bucket
					var d0 = idx >               0 ? (sp.Price - SnRLevels[idx-1].Price) : BucketSize;
					var d1 = idx < SnRLevels.Count ? (SnRLevels[idx  ].Price - sp.Price) : BucketSize;
					if (Misc.Min(d0,d1) >= BucketSize)
						SnRLevels.Insert(idx, new Level(sp.Price));
					else
						idx = d0 < d1 ? idx - 1 : idx;

					// Add 'sp' to the average for the bucket
					SnRLevels[idx].Average.Add((double)sp.Price);
				}

				done = true;

				// Adjust the prices to the mean for each bucket and remove empty buckets
				var pip = Instrument.Symbol.PipSize;
				SnRLevels.RemoveIf(x => x.Average.Count == 0);
				foreach (var lvl in SnRLevels)
				{
					done &= Misc.Abs(lvl.Price - lvl.Average.Mean) < pip;
					lvl.Price = lvl.Average.Mean;
					lvl.Strength = lvl.Average.Count;
					lvl.Average.Reset();
				}
			}

			if (SnRLevels.Count != 0)
			{
				// Normalise the strength values
				var max = SnRLevels.Max(x => x.Strength);
				SnRLevels.ForEach(x => x.Strength /= max);

				// Sort the levels by strength
				SnRLevels.Sort(Cmp<Level>.From((l,r) => -l.Strength.CompareTo(r.Strength)));
			}
		}


		///// <summary>Identify the support and resistance levels</summary>
		//private void CalculateLevels()
		//{
		//	if (Range.Size < 3)
		//		return;

		//	// Use a fraction of the mean candle size as the hysteresis
		//	var mcs = Instrument.MedianCandleSize(Range);
		//	Hysteresis = Math.Max(mcs * 0.25, Instrument.Symbol.PipSize * 5);

		//	// Find the stationary points in the close prices
		//	var price = Instrument.CandleRange(Range).Select(c => c.Close).ToList();
		//	foreach (var sp in FindStationaryPoints(price, Hysteresis))
		//	{
		//		var i0 = Range.Begini + sp.Item1.Begini;
		//		var i1 = Range.Begini + sp.Item1.Endi;
		//		var type = sp.Item2;

		//		// Add the stationary point
		//		var x = (i0 + i1) / 2.0;
		//		var p =
		//			type == EPeakType.InflectionUp || type == EPeakType.InflectionDown ? Instrument.CandleRange(i0+1,i1).Average(c => (double)c.Median) :
		//			type == EPeakType.Minimum ? Instrument.CandleRange(i0,i1).Min(c => c.BodyLimit(-1)) :
		//			type == EPeakType.Maximum ? Instrument.CandleRange(i0,i1).Max(c => c.BodyLimit(+1)) : 0.0;
		//		StationaryPoints.Add(new StationaryPoint(x, p));
		//	}
		//	if (StationaryPoints.Count == 0)
		//		return;

		//	// Find the SnR levels
		//	SnRLevels.Clear();
		//	for (var done = false; !done;)
		//	{
		//		// Sort the stationary points into their nearest bucket
		//		foreach (var sp in StationaryPoints)
		//		{
		//			// Find the nearest level to 'sp'
		//			var idx = SnRLevels.BinarySearch(x => x.Price.CompareTo(sp.Price));
		//			if (idx < 0) idx = ~idx;

		//			// Find the closest bucket
		//			// If the nearest bucket is further than the bucket size, insert a bucket
		//			var d0 = idx >               0 ? (sp.Price - SnRLevels[idx-1].Price) : Hysteresis;
		//			var d1 = idx < SnRLevels.Count ? (SnRLevels[idx  ].Price - sp.Price) : Hysteresis;
		//			if (Misc.Min(d0,d1) >= Hysteresis)
		//				SnRLevels.Insert(idx, new Level(sp.Price));
		//			else
		//				idx = d0 < d1 ? idx - 1 : idx;

		//			// Add 'sp' to the average for the bucket
		//			SnRLevels[idx].Average.Add((double)sp.Price);
		//		}

		//		done = true;
		//		var pip = Instrument.Symbol.PipSize;

		//		// Adjust the prices to the mean for each bucket and remove empty buckets
		//		SnRLevels.RemoveIf(x => x.Average.Count == 0);
		//		foreach (var lvl in SnRLevels)
		//		{
		//			done &= Misc.Abs(lvl.Price - lvl.Average.Mean) < pip;
		//			lvl.Price = lvl.Average.Mean;
		//			lvl.Strength = lvl.Average.Count;
		//			lvl.Average.Reset();
		//		}
		//	}

		//	// Normalise the strength values
		//	var max = SnRLevels.Max(x => x.Strength);
		//	SnRLevels.ForEach(x => x.Strength /= max);

		//	// Sort the levels by strength
		//	SnRLevels.Sort(Cmp<Level>.From((l,r) => -l.Strength.CompareTo(r.Strength)));
		//}

		///// <summary>Returns the index range of the peak and the peak type in the given values</summary>
		//private static IEnumerable<Tuple<Range,EPeakType>> FindStationaryPoints(IList<double> values, double hysteresis)
		//{
		//	// Returns the gradient 'values[i1] - values[i0]'
		//	Func<int, int, double> grad = (i1,i0) => values[i1] - values[i0];

		//	for (int i = 1, iend = values.Count; i != iend;)
		//	{
		//		// Look for where the gradient of the values crosses (or touches) zero
		//		if      (grad(i,i-1) > 0) for (++i; i != iend && grad(i,i-1) > 0; ++i) {}
		//		else if (grad(i,i-1) < 0) for (++i; i != iend && grad(i,i-1) < 0; ++i) {}
		//		else { ++i; continue; }
		//		if (i == iend) break;
		//		--i;

		//		var sign0 = Math.Sign(grad(i,i-1));

		//		// Look for where the values move away from the stationary point by more than 'hystersis'
		//		var j = i + 1;
		//		for (; j != iend && Math.Abs(grad(j,i)) < hysteresis; ++j) {}
		//		if (j == iend) break;

		//		var sign1 = Math.Sign(grad(j,i));

		//		var type =
		//			sign0 < 0 && sign1 > 0 ? EPeakType.Minimum :
		//			sign0 < 0 && sign1 < 0 ? EPeakType.InflectionDown :
		//			sign0 > 0 && sign1 > 0 ? EPeakType.InflectionUp :
		//			EPeakType.Maximum;

		//		// Return the stationary point
		//		yield return Tuple.Create(new Range(i,j), type);
		//		i = j;
		//	}
		//}
		//private enum EPeakType
		//{
		//	Minimum,
		//	Maximum,
		//	InflectionUp,
		//	InflectionDown,
		//}

	}
#endif

#if false // doesn't work
	public class SnR :IDisposable
	{
		/// <summary>
		/// Construct the price density data for a range of candles in 'instr'
		/// 'ibeg' and 'iend' should be negative indices</summary>
		public SnR(Instrument instr, int ibeg, int iend, int pips_per_bucket = 5)
		{
			Levels = new List<Bucket>();

			Instrument = instr;
			Range = new Range(ibeg, iend);
			PipsPerBucket = pips_per_bucket;

			CalculateLevels();
		}
		public virtual void Dispose()
		{
			Instrument = null;
		}

		/// <summary>The instrument on which the SnR levels have been calculated</summary>
		public Instrument Instrument
		{
			get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				m_instrument = value;
			}
		}
		private Instrument m_instrument;

		/// <summary>The candle range used</summary>
		public Range Range
		{
			get { return m_range; }
			set { m_range = value; }
		}
		private Range m_range;

		/// <summary>The size of each bucket (in pips)</summary>
		public int PipsPerBucket
		{
			get;
			private set;
		}

		/// <summary>A sorted list of prices</summary>
		public List<Bucket> Levels
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>The price range in each level (bucket)</summary>
		public double BucketSize
		{
			get { return Instrument.Symbol.PipSize * PipsPerBucket; }
		}

		/// <summary>Return a price quantised to the bucket price levels</summary>
		private double QuantisePrice(double price)
		{
			var bucket_size = BucketSize;
			return (long)(price / bucket_size) * bucket_size;
		}

		/// <summary>Return the bucket index for a given price. Note: can exceed the Levels collection</summary>
		private int BucketIndex(double price, List<Bucket> container)
		{
			Debug.Assert(container.Count != 0);
			return (int)((price - container[0].Price) / BucketSize);
		}

		/// <summary>
		/// Return the bucket index for a given price level.
		/// If grow == false, indices can exceed the Levels collection.
		/// If grow == true, successive calls can invalidate indices for lower price levels</summary>
		private Range BucketIndexRange(double price0, double price1, List<Bucket> container, bool grow)
		{
			Debug.Assert(container.Count != 0);
			var idx0 = BucketIndex(price0, container);
			var idx1 = BucketIndex(price1, container);

			// If the 'container' is not large enough, add more levels
			if (grow)
			{
				var bucket_size = BucketSize;
				if (idx0 < 0)
				{
					var cnt = -idx0;
					var p = container.Front().Price - cnt * bucket_size;
					container.InsertRange(0, int_.Range(cnt).Select(x => new Bucket(p + x * bucket_size)));
					idx0 += cnt;
					idx1 += cnt;
				}
				if (idx1 >= container.Count)
				{
					var cnt = idx1 - container.Count + 1;
					var p = container.Back().Price + bucket_size;
					container.InsertRange(container.Count, int_.Range(cnt).Select(x => new Bucket(p + x * bucket_size)));
				}
			}

			return new Range(idx0, idx1);
		}

		/// <summary>Identify the support and resistance levels</summary>
		private void CalculateLevels()
		{
			var container = Levels;

			// Reset the data.
			// The container must have at least one value, because the indices
			// are calculated relative to the first element.
			container.Clear();
			container.Add(new Bucket(QuantisePrice(Instrument[Range.Begini].Median)));

			// Record the maximum values so we can normalise
			var max = new Bucket(0);

			// Add the candle data over the range to 'container'
			var candle_time = Instrument.TimeFrame.ToTicks();
			for (int i = Range.Begini; i != Range.Endi; ++i)
			{
				var candle = Instrument[i];

				// Find the index range (inclusive) spanned by 'candle'.
				var idx = BucketIndexRange(candle.Low, candle.High, container, true);

				// Add to the count of the bucket that contains the median
				var median_idx = BucketIndex(candle.Median, container);
				container[median_idx].Median++;
				max.Median = Math.Max(max.Median, container[median_idx].Median);

				// Scale the values by the range of buckets the candle spans.
				// So, if a candle only covers one bucket, all candle data contributes to that bucket.
				// If the candle spans N buckets, 1/N of the data is added to each bucket.
				var scale = 1.0 / (idx.Count + 1);
				for (int j = idx.Begini, jend = idx.Endi+1; j != jend; ++j)
				{
					container[j].Volume += candle.Volume * scale;
					container[j].Time   += candle_time   * scale;

					// Record the maximum for normalising later
					max.Volume = Math.Max(max.Volume, container[j].Volume);
					max.Time   = Math.Max(max.Time  , container[j].Time);
				}
			}

			// Get the price range
			var price0 = container.Front().Price;
			var price1 = container.Back().Price + BucketSize;
			var levels_range = BucketIndexRange(price0, price1, container, true);

			// Normalise the data
			for (int i = 0; i != container.Count; ++i)
			{
				var level = container[i];
				level.Median = Maths.Div(level.Median, max.Median);
				level.Volume = Maths.Div(level.Volume, max.Volume);
				level.Time   = Maths.Div(level.Time  , max.Time  );
			}
		}

		/// <summary>A single price level (bucket)</summary>
		[DebuggerDisplay("{Price}  {Median}  {Volume}  {Time}")]
		public class Bucket
		{
			public Bucket(double price)
			{
				Price    = price;
				Median   = 0;
				Volume   = 0;
				Time     = 0;
				AvrCount = 0;
			}

			/// <summary>The central price value of this bucket</summary>
			public double Price { get; set; }

			/// <summary>The number of candle median prices that fall within this bucket</summary>
			public double Median { get; set; }

			/// <summary>The volume of trades that occurred within this bucket</summary>
			public double Volume { get; set; }

			/// <summary>The amount of time (in ticks) the price spent within this bucket price range</summary>
			public double Time { get; set; }

			/// <summary>The number of times data has been added to this level (used for averaging)</summary>
			public uint AvrCount { get; set; }

			/// <summary>Returns true if the contained values are zero</summary>
			public bool IsZero
			{
				get
				{
					return
						Maths.FEql(Median, 0.0) &&
						Maths.FEql(Volume, 0.0) &&
						Maths.FEql(Time  , 0.0);
				}
			}
		}
	}
	#endif
