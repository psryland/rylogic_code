﻿using System;
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
		/// <summary>
		/// Construct the price density data for a range of candles in 'instr'
		/// 'ibeg' and 'iend' should be negative indices</summary>
		public SnR(Instrument instr, NegIdx ibeg, NegIdx iend)
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
				var prev = new Vec4d();
				var first_idx = Instrument.FirstIdx;
				foreach (var data in Instrument.HighResRange(Range))
				{
					var s = Math.Sign(data.y - prev.y);
					if (s == sign) continue;
					if (sign != 0) yield return new StationaryPoint(prev.x + (double)first_idx, prev.y);
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

			// Normalise the strength values
			var max = SnRLevels.Max(x => x.Strength);
			SnRLevels.ForEach(x => x.Strength /= max);

			// Sort the levels by strength
			SnRLevels.Sort(Cmp<Level>.From((l,r) => -l.Strength.CompareTo(r.Strength)));
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

		[DebuggerDisplay("{Index} {Price}")]
		public class StationaryPoint
		{
			public StationaryPoint(double index, QuoteCurrency price)
			{
				Index = index;
				Price = price;
			}

			/// <summary>The NegIdx index in the instrument data of the stationary point</summary>
			public double Index { get; private set; }

			/// <summary>The price value at the stationary point</summary>
			public QuoteCurrency Price { get; private set; }
		}

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
}