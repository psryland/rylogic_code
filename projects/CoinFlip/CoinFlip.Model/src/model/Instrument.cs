using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.common;
using pr.db;
using pr.extn;
using pr.maths;
using pr.util;

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
		//   e.g. a ChartUI has an instrument, indicators has instruments, the Simulation has instruments, Bots can have instruments.
		// - The candle cache size has no upper bound. Potentially all candle data can be loaded into memory.
		//   This only happens on demand however, by default only the last Chunk of candles are loaded.
		// - The candle cache is an ordered collection of candles, sorted by time
		private const int CacheChunkSize = 10_000;

		/// <summary>A cache of candle data read from the database</summary>
		private List<Candle> m_cache;

		/// <summary>The candle index range covered by 'm_cache'. 0 = Oldest, Count = Latest</summary>
		private Range m_index_range;

		public Instrument(string name, PriceData pd)
		{
			try
			{
				Name = name;
				PriceData = pd;
				Model = pd.Model;
				Pair = pd.Pair;

				// Access the sqlite database of historic price data
				DB = new Sqlite.Database(PriceData.DBFilepath);

				// A cache of candle data read from the DB
				m_cache = new List<Candle>();
				m_index_range = new Range(0,0);
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
			DB = null;
			Pair = null;
			Model = null;
			PriceData = null;
		}

		/// <summary>A name for the use of this instrument (mainly for debugging)</summary>
		public string Name { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>The App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.BackTestingChanging -= HandleBackTestingChanged;
					m_model.SimReset -= HandleSimReset;
					m_model.SimStep -= HandleSimStep;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SimStep += HandleSimStep;
					m_model.SimReset += HandleSimReset;
					m_model.BackTestingChanging += HandleBackTestingChanged;
				}

				// Handlers
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					m_count = null;

					// No need to invalidate cache data. The size of the cache does not depend
					// on the back-testing range so whatever is cached is still valid. The 'Count'
					// and 'Latest' will need changing, but that's handled by SimReset.
				}
				void HandleSimReset(object sender, SimResetEventArgs e)
				{
					// On reset, the 'Count' and 'Latest' need updating.
					// It is possible that the simulation time is beyond the available data for this instrument.
					// We need to ensure candle data is cached from 'Simulation.StartTime' to 'DBCandleCount'

					// Ignore if there is no candle data available
					if (DBCandleCount == 0)
						return;

					// Determine the starting candle index from the back testing settings and ensure that it is cached.
					// 'idx' is actually the index value assuming the DB is up to date and has no missing candles. It's
					// possible that the start index is actually newer than this value.
					// Also, back-testing may be using a different time frame to this instrument.
					var ticks_ago = Misc.TimeFrameToTicks(e.StepsAgo, e.TimeFrame);
					var start_idx = Misc.TicksToTimeFrame(ticks_ago, TimeFrame);
					var range = new Range(DBCandleCount - 1 - (int)start_idx, DBCandleCount);
					EnsureCached(range.Begi);
					EnsureCached(range.Endi - 1);

					// Find the position of 'Simulation.StartTime' in the cache ('idx' points to the first "future" candle)
					// Can't use 'IndexAt' here because the sim time may have moved forward and 'IndexAt' is limited by 'Count'
					var time = e.StartTime.Ticks + 1; // +1 so binary search returns the same candle for [time, time + TimeFrame)
					var idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(time), find_insert_position:true) - 1;
					m_count = idx + m_index_range.Begi;

					// Reset the order books to the current price at the new sim time
					// (After the instrument.Count, Latest, etc have been updated)
					CreateFakeOrderBook();

					// Raise data changed so that dependent charts update
					OnDataChanged(new DataEventArgs(PriceData, range));
				}
				void HandleSimStep(object sender, SimStepEventArgs e)
				{
					// Get the candle for the current sim time. Can't use 'IndexAt' because it is limited by 'Count'
					var time = e.Clock.Ticks + 1; // +1 so binary search returns the same candle for [time, time + TimeFrame)
					var idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(time), find_insert_position:true) - 1;
					var candle = idx.Within(0, m_cache.Count) ? m_cache[idx] : null;
					if (candle == null)
						return;

					var candle_type = 
						candle.Timestamp >= Latest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame) ? DataEventArgs.ECandleType.New :
						candle.Timestamp >= Latest.Timestamp ? DataEventArgs.ECandleType.Current :
						DataEventArgs.ECandleType.Other;

					// New candle?
					switch (candle_type) {
					case DataEventArgs.ECandleType.New: m_count += 1; break;
					case DataEventArgs.ECandleType.Current: break;
					default: m_count = null; break;
					}

					// Update the order books to the current price at the new sim time
					CreateFakeOrderBook();

					// Raise data changed to simulate the new data having 'arrived'
					OnDataChanged(new DataEventArgs(PriceData, new Range(Count-1,Count), Latest, candle_type));
				}
			}
		}
		private Model m_model;

		/// <summary>The source of price data</summary>
		public PriceData PriceData
		{
			[DebuggerStepThrough] get { return m_price_data; }
			private set
			{
				if (m_price_data == value) return;
				if (m_price_data != null)
				{
					m_price_data.DataChanged -= HandleDataChanged;
					Util.Dispose(ref m_pd_ref);
				}
				m_price_data = value;
				if (m_price_data != null)
				{
					m_pd_ref = m_price_data.RefToken();
					m_price_data.DataChanged += HandleDataChanged;
				}

				// Handlers
				void HandleDataChanged(object sender, DataEventArgs e)
				{
					// The PriceData update thread should be disabled during back-testing.
					// Instruments handle the SimStep events because emulating the price data is done using the cache.
					if (Model.BackTesting)
						throw new Exception("Should not be getting DataChanged events from a PriceData while back-testing");

					// Update cached items
					if (e.CandleType == DataEventArgs.ECandleType.New)
						m_count += 1;
					else if (e.CandleType != DataEventArgs.ECandleType.Current)
						m_count = null;

					// Notify source data changed
					OnDataChanged(e);
				}
			}
		}
		private PriceData m_price_data;
		private Scope m_pd_ref;

		/// <summary>Database access</summary>
		private Sqlite.Database DB
		{
			[DebuggerStepThrough] get { return m_impl_db; }
			set
			{
				if (m_impl_db == value) return;
				Util.Dispose(ref m_impl_db);
				m_impl_db = value;
			}
		}
		private Sqlite.Database m_impl_db;

		/// <summary>The currency pair this instrument represents</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			private set
			{
				if (m_pair == value) return;
				if (m_pair != null)
				{
					m_pair.OrderBookChanged -= HandlePairOrderBookChanged;
				}
				m_pair = value;
				if (m_pair != null)
				{
					m_pair.OrderBookChanged += HandlePairOrderBookChanged;
				}

				// Handlers
				void HandlePairOrderBookChanged(object sender, EventArgs e)
				{
					// Notify when the order book of the latest candle changes
					OnDataChanged(new DataEventArgs(PriceData, new Range(Count-1, Count), Latest, DataEventArgs.ECandleType.Current));
				}
			}
		}
		private TradePair m_pair;

		/// <summary>The exchange hosting this instrument</summary>
		public Exchange Exchange
		{
			get { return Pair.Exchange; }
		}

		/// <summary>The instrument name (symbol name) BASEQUOTE</summary>
		public string SymbolCode
		{
			[DebuggerStepThrough] get { return Pair.Name.Strip('/'); }
		}

		/// <summary>The time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return PriceData.TimeFrame; }
		}

		/// <summary>The total number of candles in the database.</summary>
		private int DBCandleCount
		{
			// This is different to 'Count' when the simulation time is less than the current time.
			// It allows finding the index of the last candle < Model.UtcNow
			get { return PriceData.Count; }
		}

		/// <summary>The number of candles available</summary>
		public int Count
		{
			get
			{
				if (m_count == null)
				{
					if (TimeFrame == ETimeFrame.None) throw new Exception("Invalid time frame");
					var sql = Str.Build("select count(*) from ",TimeFrame," where [",nameof(Candle.Timestamp),"] <= ?");
					m_count = DB.ExecuteScalar(sql, 1, new object[] { Model.UtcNow.Ticks });
				}
				return m_count.Value;
			}
		}
		private int? m_count;

		/// <summary>The latest candle w.r.t to Model.UtcNow</summary>
		public Candle Latest
		{
			get
			{
				if (Count == 0)
					return Candle.Default;

				// Ensure the latest is cached
				EnsureCached(Count - 1);
				
				// When back testing is enabled m_cache.Back() is not the "latest"
				return m_cache[Count-1 - m_index_range.Begi];
			}
		}

		/// <summary>The raw data. Idx = 0 is the oldest, Idx = Count is the latest</summary>
		public Candle this[int idx]
		{
			get
			{
				Debug.Assert(idx.Within(0, Count));
				EnsureCached(idx);
				return m_cache[idx - m_index_range.Begi];
			}
		}

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			[DebuggerStepThrough] get { return PriceData.LastUpdatedUTC; }
		}

		/// <summary>The timestamp of the last time data was received (in local time)</summary>
		public DateTimeOffset LastUpdatedLocal
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(LastUpdatedUTC.DateTime); }
		}

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		protected virtual void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>String description of the instrument</summary>
		public string Description
		{
			get { return $"{Name} {Pair.NameWithExchange} {TimeFrame}"; }
		}

		#region Price Data

		/// <summary>The current spot price for Quote to Base trades</summary>
		public Unit<decimal> SpotPrice(ETradeType tt)
		{
			return Pair.SpotPrice(tt); 
		}

		/// <summary>The current spot price for Quote to Base trades</summary>
		public Unit<decimal> Q2BPrice
		{
			get { return SpotPrice(ETradeType.Q2B); }
		}

		/// <summary>The current spot price for Base to Quote trades</summary>
		public Unit<decimal> B2QPrice
		{
			get { return SpotPrice(ETradeType.B2Q); }
		}

		/// <summary>Populate the order book for 'Pair' with fake orders based on the Latest candle</summary>
		public void CreateFakeOrderBook()
		{
			if (!Model.BackTesting)
				throw new Exception("Don't create fake orders unless back testing");

			// Interpolate the latest candle to determine the spot price
			var t = Maths.Frac(Latest.Timestamp, Model.Simulation.Clock.Ticks, Latest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame));
			var latest = Latest.SubCandle(Maths.Clamp(t, 0.0, 1.0));
			var spread = latest.Close * Model.Settings.BackTesting.SpreadFrac;
			var spot_price_q2b = ((decimal)(latest.Close         ))._(Pair.RateUnits);
			var spot_price_b2q = ((decimal)(latest.Close + spread))._(Pair.RateUnits);

			// Generate fake buy and sell orders
			var b2q_ffers = new Order[]
			{
				new Order(spot_price_q2b * 1.00m,   0.1m._(Pair.Base)),
				new Order(spot_price_q2b * 0.99m,   1.0m._(Pair.Base)),
				new Order(spot_price_q2b * 0.97m,  10.0m._(Pair.Base)),
				new Order(spot_price_q2b * 0.91m, 100.0m._(Pair.Base)),
			};
			var q2b_offers = new Order[]
			{
				new Order(spot_price_b2q * 1.00m,   0.1m._(Pair.Base)),
				new Order(spot_price_b2q * 1.01m,   1.0m._(Pair.Base)),
				new Order(spot_price_b2q * 1.03m,  10.0m._(Pair.Base)),
				new Order(spot_price_b2q * 1.09m, 100.0m._(Pair.Base)),
			};

			// Don't notify, because we do that in SimStep and SimReset
			Pair.UpdateOrderBook(b2q_ffers, q2b_offers, notify_changed:false);
		}

		#endregion

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
				return m_index_range.Begi + idx;
			}

			// Otherwise, use a database query to determine the index, and grow the cache
			else
			{
				var sql = $"select count(*)-1 from {TimeFrame} where [{nameof(Candle.Timestamp)}] <= ? order by [{nameof(Candle.Timestamp)}]";
				var idx = DB.ExecuteScalar(sql, 1, new object[] { time_stamp.ExactTicks });
				EnsureCached(idx);
				return idx;
			}
		}

		/// <summary>Clamps the given index range to a valid range within the data. [0, Count]</summary>
		public Range IndexRange(int idx_min, int idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Maths.Clamp(idx_min, 0, Count);
			var max = Maths.Clamp(idx_max, min, Count);
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
			return m_cache[idx - m_index_range.Begi];
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

		/// <summary>Invalidate the cached data</summary>
		public void InvalidateCachedData()
		{
			m_cache.Clear();
			m_index_range = Range.Zero;
			m_count = null;
		}

		/// <summary>Ensure the cached data contains candle index 'idx'</summary>
		private void EnsureCached(int idx)
		{
			// Already cached?
			if (m_index_range.Contains(idx))
				return;

			// Shouldn't be requesting indices outside the range of available data
			Debug.Assert(idx.Within(0, DBCandleCount));

			// Read from the database. Order by timestamp so that the oldest is first, and the newest is at the end.
			var sql = Str.Build("select * from ",TimeFrame," order by [",nameof(Candle.Timestamp),"] limit ?,?");

			// Grow the index range in multiples of chunk size
			if (m_index_range.Empty)
			{
				var to_get = new Range(Math.Max(0L, idx - CacheChunkSize), Math.Min(DBCandleCount, idx + CacheChunkSize));
				m_cache.AddRange(DB.EnumRows<Candle>(sql, 1, new object[] { to_get.Begi, to_get.Sizei }));
				m_index_range = to_get;
			}
			if (idx < m_index_range.Beg)
			{
				var chunks = (m_index_range.Beg - idx + CacheChunkSize - 1) / CacheChunkSize;
				var to_get = new Range(Math.Max(0L, m_index_range.Beg - chunks*CacheChunkSize), m_index_range.Beg);
				m_cache.InsertRange(0, DB.EnumRows<Candle>(sql, 1, new object[] { to_get.Begi, to_get.Sizei }));
				m_index_range.Beg = to_get.Beg;
			}
			if (idx >= m_index_range.End)
			{
				var chunks = (idx+1 - m_index_range.End + CacheChunkSize - 1) / CacheChunkSize;
				var to_get = new Range(m_index_range.End, Math.Min(DBCandleCount, m_index_range.End + chunks*CacheChunkSize));
				m_cache.AddRange(DB.EnumRows<Candle>(sql, 1, new object[] { to_get.Begi, to_get.Sizei }));
				m_index_range.End = to_get.End;
			}

			// We should have grown the cache to contain 'idx'
			Debug.Assert(idx.Within(m_index_range.Begi, m_index_range.Endi));
		}

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
