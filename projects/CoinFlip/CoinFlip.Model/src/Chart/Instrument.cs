﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>The candle data for a trading pair</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Instrument :IDisposable ,IEnumerable<Candle>
	{
		// Notes:
		// - An instrument is a read-only view of PriceData with caching.
		// - This class is basically an array with an indexer and Count.
		// - When back-testing is enabled, an Instrument represents a sub-range of PriceData.
		// - There can be multiple instrument instances for the same pair, each having a different TimeFrame.
		//   e.g. a ChartUI has an instrument, indicators have instruments, the Simulation has instruments, Bots can have instruments.
		// - The candle cache size has no upper bound. Potentially all candle data can be loaded into memory.
		//   This only happens on demand however, by default only the last Chunk of candles are loaded.
		// - The candle cache is an ordered collection of candles, sorted by time
		private const int CacheChunkSize = 10_000;

		/// <summary>Create a view of price data that updates as new data arrives</summary>
		public Instrument(string name, PriceData pd)
		{
			try
			{
				Debug.Assert(Misc.AssertMainThread());

				Name = name;
				PriceData = pd;

				// A cache of candle data read from the DB
				m_cache = new List<Candle>();
				CachedIndexRange = new Range(0,0);
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public virtual void Dispose()
		{
			Util.BreakIf(Util.IsGCFinalizerThread, "Leaked Instrument");
			PriceData = null;
		}

		/// <summary>A name for the use of this instrument (mainly for debugging)</summary>
		public string Name { get; }

		/// <summary>The source of price data</summary>
		public PriceData PriceData
		{
			[DebuggerStepThrough] get { return m_price_data; }
			private set
			{
				if (m_price_data == value) return;
				if (m_price_data != null)
				{
					Pair = null;
					m_price_data.DataSyncingChanged -= HandleDataSyncingChanged;
					m_price_data.DataChanged -= HandleDataChanged;
					Util.Dispose(ref m_pd_ref);
				}
				m_price_data = value;
				if (m_price_data != null)
				{
					m_pd_ref = m_price_data.RefToken(this);
					m_price_data.DataChanged += HandleDataChanged;
					m_price_data.DataSyncingChanged += HandleDataSyncingChanged;
					Pair = m_price_data.Pair;
				}

				// Handlers
				void HandleDataChanged(object sender, DataEventArgs e)
				{
					// Update cached items
					switch (e.UpdateType)
					{
					default: throw new Exception($"Unknown update type: {e.UpdateType}");
					case DataEventArgs.EUpdateType.New:
						{
							// A new candle was added. If the candle is the next expected time slot just bump the count
							// Otherwise, invalidate because time might have skipped forward (due to the simulation or a gap in data).
							// The cache doesn't need to be invalidated because "New" means candles we haven't seen before.
							if (Latest?.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame) == e.Candle.Timestamp)
								m_count += 1;
							else
								m_count = null;
							EnsureCached(Count - 1);
							break;
						}
					case DataEventArgs.EUpdateType.Current:
						{
							// Data for the current candle has changed, just update the cached copy
							if (Latest.Timestamp != e.Candle.Timestamp)
								throw new Exception("This is not the current candle");
							if (e.IndexRange.Count != 1)
								throw new Exception("An update of the current candle should be a single value");
							if (e.IndexRange.Begi != Count - 1)
								throw new Exception("The current candle index should be the only value in the update index range");

							this[e.IndexRange.Begi] = e.Candle;
							break;
						}
					case DataEventArgs.EUpdateType.Range:
						{
							// An unknown range of candles changed, invalidate the cache
							InvalidateCachedData();
							EnsureCached(Count-1);
							break;
						}
					}

					// Notify source data changed
					OnDataChanged(e);
				}
				void HandleDataSyncingChanged(object sender, EventArgs e)
				{
					OnDataSyncingChanged();
				}
			}
		}
		private PriceData m_price_data;
		private Scope m_pd_ref;

		/// <summary>The currency pair this instrument represents</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			private set
			{
				if (m_pair == value) return;
				if (m_pair != null)
				{
					m_pair.MarketDepth.Needed -= HandleMDNeeded;
					m_pair.MarketDepth.OrderBookChanged -= HandlePairOrderBookChanged;
				}
				m_pair = value;
				if (m_pair != null)
				{
					m_pair.MarketDepth.OrderBookChanged += HandlePairOrderBookChanged;
					m_pair.MarketDepth.Needed += HandleMDNeeded;
				}

				// Handlers
				void HandlePairOrderBookChanged(object sender, EventArgs e)
				{
					// Notify when the order book of the latest candle changes
					if (Count != 0)
						OnDataChanged(new DataEventArgs(DataEventArgs.EUpdateType.Current, PriceData, new Range(Count-1, Count), Latest));
				}
				void HandleMDNeeded(object sender, HandledEventArgs e)
				{
					e.Handled = true;
				}
			}
		}
		private TradePair m_pair;

		/// <summary>The exchange hosting this instrument</summary>
		public Exchange Exchange => Pair.Exchange;

		/// <summary>The instrument name (symbol name) BASEQUOTE</summary>
		public string SymbolCode => Pair.Name.Strip('/');

		/// <summary>The price units (in Q2B)</summary>
		public string RateUnits => Pair.RateUnits;

		/// <summary>The time frame</summary>
		public ETimeFrame TimeFrame => PriceData.TimeFrame;

		/// <summary>The total number of candles in the database.</summary>
		private int DBCandleCount => PriceData.Count;

		/// <summary>True when the price data is out of date</summary>
		public bool DataSyncing => PriceData.DataSyncing;

		/// <summary>The number of candles available up to the current time</summary>
		public int Count
		{
			get
			{
				// When the simulation is running this will be less than 'DBCandleCount'
				if (m_count == null)
					m_count = PriceData.CountTo(Model.UtcNow.Ticks);

				return m_count.Value;
			}
		}
		private int? m_count;

		/// <summary>The latest candle w.r.t to Model.UtcNow</summary>
		public Candle Latest => Count != 0 ? this[Count - 1] : null;

		/// <summary>The raw data. Idx = 0 is the oldest, Idx = Count is the latest</summary>
		public Candle this[int idx]
		{
			get
			{
				if (!idx.Within(0, Count))
					throw new ArgumentOutOfRangeException(nameof(idx), $"Invalid candle index: {idx}. Range: [0,{Count})");

				EnsureCached(idx);
				return m_cache[idx - CachedIndexRange.Begi];
			}
			private set
			{
				if (!idx.Within(0, Count))
					throw new ArgumentOutOfRangeException(nameof(idx), $"Invalid candle index: {idx}. Range: [0,{Count})");

				EnsureCached(idx);
				m_cache[idx - CachedIndexRange.Begi] = value;
			}
		}

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC => PriceData.LastUpdatedUTC;

		/// <summary>The timestamp of the last time data was received (in local time)</summary>
		public DateTimeOffset LastUpdatedLocal => TimeZone.CurrentTimeZone.ToLocalTime(LastUpdatedUTC.DateTime);

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		protected virtual void OnDataChanged(DataEventArgs args)
		{
			DataChanged?.Invoke(this, args);
		}

		/// <summary>Raised whenever the price data may be out of date, or back into date</summary>
		public event EventHandler DataSyncingChanged;
		protected virtual void OnDataSyncingChanged()
		{
			DataSyncingChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>String description of the instrument</summary>
		public string Description => $"{Name} {Pair.NameWithExchange} {TimeFrame}";

		/// <summary>The current spot price for Quote to Base trades</summary>
		public Unit<double>? SpotPrice(ETradeType tt) => Pair.SpotPrice[tt];

		#region Ranges and Indexing

		/// <summary>Return the index of the candle at or immediately before 'time_stamp'</summary>
		public int IndexAt(TimeFrameTime time_stamp)
		{
			if (TimeFrame == ETimeFrame.None)
				throw new Exception("Time frame is none");

			if (Count == 0)
				return 0;
			if (time_stamp.ExactTicks >= Latest.Timestamp)
				return Count-1;

			// If the time stamp is within the cached range, binary search the cache for the index position
			var time_range = new Range(m_cache.Front().Timestamp, m_cache.Back().Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame));
			if (time_range.Contains(time_stamp.ExactTicks))
			{
				var idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(time_stamp.ExactTicks), find_insert_position:true);
				return idx + CachedIndexRange.Begi;
			}

			// Otherwise, use a database query to determine the index, and grow the cache
			else
			{
				var count = PriceData.CountTo(time_stamp.ExactTicks);
				var idx = count != 0 ? count - 1 : 0;
				EnsureCached(idx);
				return idx;
			}
		}

		/// <summary>Return the fractional index of the candle at 'time_stamp'</summary>
		public double FIndexAt(TimeFrameTime time_stamp)
		{
			var idx = IndexAt(time_stamp);
			var frac = Misc.TicksToTimeFrame(time_stamp.ExactTicks - this[idx].Timestamp, TimeFrame);
			return idx + frac;
		}

		/// <summary>Return the interpolated time (in ticks) a the fractional index 'fidx'</summary>
		public long TimeAtFIndex(double fidx)
		{
			var beg = 0;
			var end = Count - 1;
			var idx = (int)fidx;

			if (Count == 0)
				throw new Exception("TimeAtFIndex is not defined when there is no data");
			if (idx < beg)
				return this[beg].Timestamp + Misc.TimeFrameToTicks(fidx, TimeFrame);
			if (idx > end)
				return this[end].Timestamp + Misc.TimeFrameToTicks(fidx - end, TimeFrame);

			return this[idx].Timestamp + Misc.TimeFrameToTicks(fidx - idx, TimeFrame);
		}

		/// <summary>Clamps the given index range to a valid range within the data. [0, Count]</summary>
		public Range IndexRange(int idx_min, int idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Math_.Clamp(idx_min, 0, Count);
			var max = Math_.Clamp(idx_max, min, Count);
			return new Range(min, max);
		}

		/// <summary>Convert a time frame time range to an index range. [0, Count]</summary>
		public Range TimeToIndexRange(TimeFrameTime time_min, TimeFrameTime time_max)
		{
			Debug.Assert(time_min <= time_max);
			var idx_min = IndexAt(time_min);
			var idx_max = IndexAt(time_max);
			return new Range(idx_min, idx_max);
		}

		/// <summary>Return the candle at or immediately before 'time_stamp'</summary>
		public Candle CandleAt(TimeFrameTime time_stamp)
		{
			var idx = IndexAt(time_stamp);
			return this[idx];
		}

		/// <summary>Enumerate the candles within an index range. [idx_max,idx_max)</summary>
		public IEnumerable<Candle> CandleRange(int idx_min, int idx_max)
		{
			var r = IndexRange(idx_min, idx_max);
			for (var i = r.Begi; i != r.Endi; ++i)
				yield return this[i];
		}

		/// <summary>Enumerate the candles within a time range (given in time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(TimeFrameTime time_min, TimeFrameTime time_max)
		{
			var range = TimeToIndexRange(time_min, time_max);
			return CandleRange(range.Begi, range.Endi);
		}

		#endregion

		#region Candle Cache

		/// <summary>The candle index range covered by 'm_cache'. 0 = Oldest, Count = Latest</summary>
		public Range CachedIndexRange { get; private set; }

		/// <summary>Invalidate the cached data</summary>
		public void InvalidateCachedData()
		{
			m_cache.Clear();
			CachedIndexRange = Range.Zero;
			m_count = null;
		}

		/// <summary>Ensure the cached data contains candle index 'idx'</summary>
		public void EnsureCached(int idx)
		{
			// Instrument should only be accessed from the main thread
			Debug.Assert(Misc.AssertMainThread());

			// Shouldn't be requesting indices outside the range of available data
			// Accept '-1' however because often (Count - 1) is used when Count can be 0.
			Debug.Assert(idx.Within(-1, DBCandleCount));

			// Already cached?
			if (idx == -1 || CachedIndexRange.Contains(idx))
				return;

			// Read from the database of historic price data
			Candle[] ReadCandles(Range read) => PriceData.ReadCandles(read).ToArray();

			// Determine the range of data to read
			if (CachedIndexRange.Empty)
			{
				var read = new Range(Math.Max(0L, idx - CacheChunkSize), Math.Min(DBCandleCount, idx + CacheChunkSize));
				m_cache.AddRange(ReadCandles(read));
				CachedIndexRange = read;
			}
			else if (idx >= CachedIndexRange.End)
			{
				var chunks = (idx+1 - CachedIndexRange.End + CacheChunkSize - 1) / CacheChunkSize;
				var read = new Range(CachedIndexRange.End, Math.Min(DBCandleCount, CachedIndexRange.End + chunks*CacheChunkSize));
				m_cache.AddRange(ReadCandles(read));
				CachedIndexRange = new Range(CachedIndexRange.Beg, read.End);
			}
			else if (idx < CachedIndexRange.Beg)
			{
				var chunks = (CachedIndexRange.Beg - idx + CacheChunkSize - 1) / CacheChunkSize;
				var read = new Range(Math.Max(0L, CachedIndexRange.Beg - chunks*CacheChunkSize), CachedIndexRange.Beg);
				m_cache.InsertRange(0, ReadCandles(read));
				CachedIndexRange = new Range(read.Beg, CachedIndexRange.End);
			}
			else
			{
				throw new Exception();
			}

			// We should now have 'idx' in the cache
			Debug.Assert(idx.Within(CachedIndexRange.Begi, CachedIndexRange.Endi));
		}
		private List<Candle> m_cache;

		#endregion

		#region IEnumerable<Candle>

		/// <summary>Enumerate all available candles in this instrument</summary>
		IEnumerator<Candle> IEnumerable<Candle>.GetEnumerator()
		{
			for (var i = 0; i != Count; ++i)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Candle>)this).GetEnumerator();
		}

		#endregion
	}
}


///// <summary>The App logic</summary>
//public Model Model
//{
//	[DebuggerStepThrough]
//	get { return m_model; }
//	private set
//	{
//		if (m_model == value) return;
//		if (m_model != null)
//		{
//			m_model.BackTestingChanging -= HandleBackTestingChanged;
//			m_model.SimReset -= HandleSimReset;
//			m_model.SimStep -= HandleSimStep;
//		}
//		m_model = value;
//		if (m_model != null)
//		{
//			m_model.SimStep += HandleSimStep;
//			m_model.SimReset += HandleSimReset;
//			m_model.BackTestingChanging += HandleBackTestingChanged;
//		}

//		// Handlers
//		void HandleBackTestingChanged(object sender, PrePostEventArgs e)
//		{
//			m_count = null;

//			// No need to invalidate cache data. The size of the cache does not depend
//			// on the back-testing range so whatever is cached is still valid. The 'Count'
//			// and 'Latest' will need changing, but that's handled by SimReset.
//		}
//		void HandleSimReset(object sender, SimResetEventArgs e)
//		{
//			// On reset, the 'Count' and 'Latest' need updating.
//			// It is possible that the simulation time is beyond the available data for this instrument.
//			// We need to ensure candle data is cached from 'Simulation.StartTime' to 'DBCandleCount'

//			// Ignore if there is no candle data available
//			if (DBCandleCount == 0)
//				return;

//			// Determine the starting candle index from the back testing settings and ensure that it is cached.
//			// 'idx' is actually the index value assuming the DB is up to date and has no missing candles. It's
//			// possible that the start index is actually newer than this value.
//			// Also, back-testing may be using a different time frame to this instrument.
//			var ticks_ago = Misc.TimeFrameToTicks(e.StepsAgo, e.TimeFrame);
//			var start_idx = Misc.TicksToTimeFrame(ticks_ago, TimeFrame);
//			var range = new Range(DBCandleCount - 1 - (int)start_idx, DBCandleCount);
//			EnsureCached(range.Begi);
//			EnsureCached(range.Endi - 1);

//			// Find the position of 'Simulation.StartTime' in the cache ('idx' points to the first "future" candle)
//			// Can't use 'IndexAt' here because the sim time may have moved forward and 'IndexAt' is limited by 'Count'
//			var time = e.StartTime.Ticks + 1; // +1 so binary search returns the same candle for [time, time + TimeFrame)
//			var idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(time), find_insert_position: true) - 1;
//			m_count = idx + m_index_range.Begi;

//			// Raise data changed so that dependent charts update
//			OnDataChanged(new DataEventArgs(PriceData, range));
//		}
//		void HandleSimStep(object sender, SimStepEventArgs e)
//		{
//			var update_type = DataEventArgs.EUpdateType.Current;

//			// Update 'Latest' for the new clock value
//			if (!e.Clock.Ticks.Within(Latest.Timestamp, Latest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame)))
//			{
//				if (Count - m_index_range.Begi < m_index_range.Endi)
//					++m_count;

//				update_type = DataEventArgs.EUpdateType.New;
//				if (!e.Clock.Ticks.Within(Latest.Timestamp, Latest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame)))
//				{
//					m_count = null;
//					update_type = DataEventArgs.EUpdateType.Range;
//				}
//			}

//			// Raise data changed to simulate the new data having 'arrived'
//			OnDataChanged(new DataEventArgs(update_type, PriceData, new Range(Count - 1, Count), Latest));
//		}
//	}
//}
//private Model m_model;