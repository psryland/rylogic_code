using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;
using NegIdx = System.Int32;
using PosIdx = System.UInt32;

namespace Tradee
{
	/// <summary>The data series for an instrument at a specific time frame</summary>
	[DebuggerDisplay("{SymbolCode},{TimeFrame}")]
	public class Instrument :IDisposable ,IEnumerable<Candle>
	{
		// Notes:
		// - This class is basically an array with an indexer and Count.
		//   However the public interface is for indices that range from (-Count,0].
		//   All methods that deal with indices should expect values in this range.
		// - PosIdx is a positive index in the range [0, Count) where 0 = oldest
		// - NegIdx is a negative index in the range (-Count,0] where 0 = newest

		/// <summary>A database of price data for this instrument</summary>
		private Sqlite.Database m_db;
		private int m_suspend_db_updates;

		/// <summary>A cache of candle data read from the database</summary>
		private List<Candle> m_cache;
		private const int CacheSize = 10000;
		private const int MaxCacheSize = 15000;

		public Instrument(MainModel model, string symbol)
		{
			m_cache = new List<Candle>(CacheSize);

			Model = model;
			SymbolCode = symbol;
			SupportResistLevels = new BindingSource<SnRLevel> { DataSource = new BindingListEx<SnRLevel>() };

			// Load the sqlite database of historic price data
			m_db = new Sqlite.Database(DBFilepath);

			// Tweak some DB settings for performance
			m_db.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
			m_db.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

			// Create a table for the price data info
			m_db.Execute(SqlExpr.PriceDataTable());

			// Ensure tables exist for each of the time frames.
			// Note: timestamp is not the row id. Row Ids should not have any
			// meaning. It's more efficient to let the DB do the sorting
			// and just use 'order by' in queries
			foreach (var tf in Enum<ETimeFrame>.Values.Where(x => x != ETimeFrame.None))
				m_db.Execute(SqlExpr.CandleTable(tf));

			// Create a table for the SnR levels
			m_db.Execute(SqlExpr.SnRLevelsTable());

			// Initialise from the DB, so don't write back to the db
			using (Scope.Create(() => ++m_suspend_db_updates, () => --m_suspend_db_updates))
			{
				// Select the default time frame to begin with
				TimeFrame = Settings.General.DefaultTimeFrame;

				// Load the last known price data for the instrument
				PriceData = m_db.EnumRows<PriceData>(SqlExpr.GetPriceData()).FirstOrDefault() ?? new PriceData();

				// Load the known support and resistance levels
				SupportResistLevels.AddRange(m_db.EnumRows<SnRLevel>(SqlExpr.GetSnRLevelData()));
			}
		}
		public Instrument(Instrument rhs, ETimeFrame time_frame)
			:this(rhs.Model, rhs.SymbolCode)
		{
			TimeFrame = time_frame;
		}
		public virtual void Dispose()
		{
			Debug.Assert(ReferenceCount == 0);
			TimeFrame = ETimeFrame.None;
			SupportResistLevels = null;
			Util.Dispose(ref m_db);
			Model = null;
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.ConnectionChanged -= HandleConnectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.ConnectionChanged += HandleConnectionChanged;
				}
			}
		}
		private MainModel m_model;

		/// <summary>The references to this instrument in Tradee</summary>
		public int ReferenceCount
		{
			[DebuggerStepThrough] get { return m_ref_count; }
			set
			{
				Debug.Assert(value >= 0);
				if (m_ref_count == value) return;
				if (value == 0)
				{
					// Last reference removed
					if (Model.IsConnected)
						StopDataRequest(); // Stop sending data for this instrument
				}
				if (m_ref_count == 0)
				{
					// First reference added
					if (Model.IsConnected)
						StartDataRequest(); // Request updates of this instrument
				}
				m_ref_count = value;
			}
		}
		private int m_ref_count;

		/// <summary>The instrument name (symbol name)</summary>
		public string SymbolCode
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The active time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return m_time_frame; }
			set
			{
				if (m_time_frame == value) return;

				// Set the new time frame and flush any cached data
				m_time_frame = value;
				InvalidateCachedData();

				// Set the last updated time to the timestamp of the last candle
				if (m_time_frame != ETimeFrame.None)
				{
					// Ensure data is being sent for this time frame
					StartDataRequest();
					LastUpdatedUTC = Latest.TimestampUTC;
				}

				// Notify time frame changed
				OnTimeFrameChanged();
			}
		}
		private ETimeFrame m_time_frame;

		/// <summary>Information about the current price, spread, lot sizes, etc</summary>
		public PriceData PriceData
		{
			get { return m_price_data; }
			set
			{
				if (m_price_data == value) return;
				m_price_data = value;
				OnPriceDataUpdated();
			}
		}
		private PriceData m_price_data;

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			get
			{
				if (m_impl_count == null)
				{
					Debug.Assert(TimeFrame != ETimeFrame.None);
					var sql = Str.Build("select count(*) from ",TimeFrame," where [",nameof(Candle.Timestamp),"] <= ?");
					m_impl_count = m_db.ExecuteScalar(sql, 1, new object[] { Model.UtcNow.Ticks });
				}
				return m_impl_count.Value;
			}
		}
		private int? m_impl_count;

		/// <summary>The total number of data points in the cache.</summary>
		private int Total
		{
			// This is different to Count when the simulation time is less than the current time.
			// It allows finding the index of the last candle < Model.UtcNow
			get { return m_impl_total ?? (m_impl_total = m_db.ExecuteScalar(Str.Build("select count(*) from ",TimeFrame))).Value; }
		}
		private int? m_impl_total;

		/// <summary>Index range (-Count, 0]</summary>
		public NegIdx FirstIdx
		{
			get { return -(Count-1); }
		}
		public NegIdx LastIdx
		{
			get { return +1; }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public Candle this[NegIdx neg_idx]
		{
			get { return this[(PosIdx)(neg_idx - FirstIdx)]; }
		}

		/// <summary>The raw data. Idx = 0 is the oldest, Idx = Count is the latest</summary>
		public Candle this[PosIdx pos_idx]
		{
			get
			{
				Debug.Assert(pos_idx >= 0 && pos_idx < Count);

				// Shift the cached range if needed
				if (!m_index_range.Contains(pos_idx))
				{
					// Otherwise, reload the cache centred on the requested index
					var new_range = new Range((long)pos_idx - CacheSize/2, (long)pos_idx + CacheSize/2);
					if (new_range.Begin <   0) new_range = new_range.Shift(0   - new_range.Begin);
					if (new_range.End > Count) new_range = new_range.Shift(Count - new_range.End);
					if (new_range.Begin <   0) new_range.Begin = 0;

					// Populate the cache from the database
					// Order by timestamp so that the oldest is first, and the newest is at the end.
					var sql = Str.Build("select * from ",TimeFrame," order by [",nameof(Candle.Timestamp),"] limit ?,?");
					m_cache.AddRange(m_db.EnumRows<Candle>(sql, 1, new object[] { new_range.Begini, new_range.Sizei }));
					m_index_range = new_range;
				}

				return m_cache[(int)pos_idx - m_index_range.Begini];
			}
		}

		/// <summary>Enumerate all available candles in this instrument</summary>
		IEnumerator<Candle> IEnumerable<Candle>.GetEnumerator()
		{
			for (PosIdx i = 0; i != Count; ++i)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<Candle>)this).GetEnumerator();
		}

		/// <summary>The candle index range covered by 'm_cache'. 0 = Oldest, Count = Latest</summary>
		private Range CachedIndexRange
		{
			[DebuggerStepThrough] get { return m_index_range; }
		}
		private Range m_index_range;

		/// <summary>Return the time range covered by 'm_cache'. Note: Begin == oldest time, End == newest time</summary>
		private Range CachedTimeRange
		{
			get
			{
				if (m_cache.Count == 0) return Range.Zero;
				return new Range(m_cache.Front().Timestamp, m_cache.Back().Timestamp);
			}
		}

		/// <summary>Return the index of the candle at or immediately before 'time_stamp'. Note: the returned indices will be in the range (-Count, 0]</summary>
		public int IndexAt(TFTime time_stamp)
		{
			var ticks = time_stamp.ExactTicks;

			// If the time stamp is within the cached range, binary search the cache for the index position
			if (CachedTimeRange.Contains(ticks))
			{
				var idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(ticks));
				if (idx < 0) idx = ~idx;
				return (m_index_range.Begini + idx) + FirstIdx;
			}
			// Otherwise use database queries to determine the index
			else
			{
				var sql = Str.Build("select count(*)-1 from ",TimeFrame," where [",nameof(Candle.Timestamp),"] <= ? order by [",nameof(Candle.Timestamp),"]");
				var idx = m_db.ExecuteScalar(sql, 1, new object[] { ticks });
				return idx + FirstIdx;
			}
		}

		/// <summary>Clamps the given index range to a valid range within the data. i.e. [-Count,0]</summary>
		public Range IndexRange(NegIdx idx_min, NegIdx idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Maths.Clamp(idx_min, FirstIdx, LastIdx);
			var max = Maths.Clamp(idx_max, min, LastIdx);
			return new Range(min, max);
		}

		/// <summary>Convert a time frame time range to an index range. (Indices in the range (-Count, 0])</summary>
		public Range TimeToIndexRange(TFTime time_min, TFTime time_max)
		{
			Debug.Assert(time_min <= time_max);
			var idx_min = IndexAt(time_min);
			var idx_max = IndexAt(time_max);
			return new Range(idx_min, idx_max);
		}

		/// <summary>The candle with the latest timestamp</summary>
		public Candle Latest
		{
			get
			{
				if (m_latest == null && TimeFrame != ETimeFrame.None)
				{
					// If the cache covers the latest candle, return it from the cache otherwise get it from the DB.
					if (m_index_range.Counti != 0 && m_index_range.Endi == Count)
					{
						m_latest = m_cache.Back();
					}
					else
					{
						var ts = "[" + nameof(Candle.Timestamp) + "]";
						var sql = Str.Build("select * from ",TimeFrame," where ",ts," <= ? order by ",ts," desc limit 1");
						m_latest = m_db.EnumRows<Candle>(sql, 1, new object[] { Model.UtcNow.Ticks }).FirstOrDefault();
					}
				}
				return m_latest ?? Candle.Default;
			}
		}
		private Candle m_latest;

		/// <summary>The candle with the oldest timestamp</summary>
		public Candle Oldest
		{
			get
			{
				if (m_oldest == null && TimeFrame != ETimeFrame.None)
				{
					// If the cache covers the latest candle, return it from the cache otherwise get it from the DB.
					if (m_index_range.Counti != 0 && m_index_range.Begini == 0)
					{
						m_oldest = m_cache.Front();
					}
					else
					{
						var ts = "[" + nameof(Candle.Timestamp) + "]";
						var sql = Str.Build("select * from ",TimeFrame," where ",ts," < ? order by ",ts," asc limit 1");
						m_oldest = m_db.EnumRows<Candle>(sql, 1, new object[] { Model.UtcNow.Ticks }).FirstOrDefault();
					}
				}
				return m_oldest ?? Candle.Default;
			}
		}
		private Candle m_oldest;

		/// <summary>The timestamp of the last time we received data (not necessarily the an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The timestamp of the last time data was received (in local time)</summary>
		public DateTimeOffset LastUpdatedLocal
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(LastUpdatedUTC.DateTime); }
		}

		/// <summary>Add a candle value to the data</summary>
		public void Add(ETimeFrame tf, PriceCandle price_candle)
		{
			// Sanity check
			var candle = new Candle(price_candle);
			Debug.Assert(candle.Valid());
			Debug.Assert(candle.Timestamp != 0);
			Debug.Assert(tf != ETimeFrame.None);

			// If this candle is the newest we've seen, then a new candle has started.
			// Get the latest candle before inserting 'candle' into the database
			var new_candle = tf == TimeFrame && candle.Timestamp > Latest.Timestamp;

			// Insert the candle into the database if the sim isn't running.
			// The sim draws it's data from the database, so there's no point in writing
			// the same data back in. Plus it might in the future do things to emulate sub-candle
			// updates, which I don't want to overwrite the actual data.
			if (!Model.SimActive)
				m_db.Execute(SqlExpr.InsertCandle(tf), 1, SqlExpr.InsertCandleParams(candle));

			// If the candle is for the current time frame and within the
			// cached data range, update the cache to avoid invalidating it.
			if (tf == TimeFrame)
			{
				// Within the currently cached data?
				// Note: this is false if 'candle' is the start of a new candle
				if (!new_candle)
				{
					// Find the index in the cache for 'candle'. If not an existing cache item, then just reset the cache
					var cache_idx = m_cache.BinarySearch(x => x.Timestamp.CompareTo(candle.Timestamp));
					if (cache_idx >= 0)
						m_cache[cache_idx].Update(candle);
					else
						InvalidateCachedData();
				}
				// If the cached range ends at the latest candle (excluding the new candle)
				// then we can preserve the cache and append the new candle to the cache data
				else if (m_index_range.Endi == Count)
				{
					// If adding 'candle' will make the cache too big, just flush
					if (m_cache.Count > MaxCacheSize)
						InvalidateCachedData();

					// Otherwise, append the new candle to the cache
					else
					{
						m_cache.Add(candle);
						m_index_range.End++;
						m_impl_count = null;
						m_impl_total = null;
						m_latest = null;
					}
				}
				// Otherwise the candle is not within the cache, just invalidate
				else
				{
					InvalidateCachedData();
				}
			}

			// Record the last time data was received
			LastUpdatedUTC = Model.UtcNow;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this, tf, candle, new_candle));
		}

		/// <summary>Add a batch of candles</summary>
		public void Add(ETimeFrame tf, PriceCandles candles)
		{
			// Insert the candles into the database
			using (var t = m_db.NewTransaction())
			using (var query = new Sqlite.Query(m_db, SqlExpr.InsertCandle(tf)))
			{
				foreach (var candle in candles.AllCandles.Select(x => new Candle(x)))
				{
					query.Reset();
					query.BindParms(1, SqlExpr.InsertCandleParams(candle));
					query.Run();
				}
				t.Commit();
			}

			// Don't bother maintaining the cache, just invalidate it
			InvalidateCachedData();

			// Record the last time data was received
			LastUpdatedUTC = Model.UtcNow;

			// Notify data added/changed
			DataChanged.Raise(this, new DataEventArgs(this, tf, null, false));
		}

		/// <summary>Raised when 'PriceData' is updated</summary>
		public event EventHandler PriceDataUpdated;
		private void OnPriceDataUpdated()
		{
			PriceDataUpdated.Raise(this);

			// Write the price data into the db
			if (m_suspend_db_updates == 0 && !Equals(PriceData, PriceData.Default))
				m_db.Execute(SqlExpr.UpdatePriceData(), 1, SqlExpr.UpdatePriceDataParams(PriceData));
		}

		/// <summary>Raised whenever the time frame changes</summary>
		public event EventHandler TimeFrameChanged;
		private void OnTimeFrameChanged()
		{
			TimeFrameChanged.Raise(this);
		}

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		private void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>Raise the data changed event for the current time frame</summary>
		public void RaiseDataChanged()
		{
			OnDataChanged(new DataEventArgs(this, TimeFrame, null, false));
		}

		/// <summary>Return the chart X axis value for a given time</summary>
		public double ChartXValue(TFTime time)
		{
			//' X-Axis: -ve --- 0 ---- +ve   '
			//' Idx:    <0      0      >0    '
			//'        past    now     future'
			
			// If the time is a point in the future, return the number of candles assuming no gaps
			if (time.ExactTicks >= Latest.Timestamp)
				return new TFTime(time.ExactTicks - Latest.Timestamp, TimeFrame).ExactTF; // positive number, cos it's in the future

			// If the time is a point in the past beyond our history, return the number of candles assuming no gaps
			if (time.ExactTicks <= Oldest.Timestamp)
				return -(Count + new TFTime(Oldest.Timestamp - time.ExactTicks, TimeFrame).ExactTF); // negative, cos it's in the past

			// Get the index into the past
			var idx = IndexAt(time);

			// Lerp between the timestamps of the candles on either side of 'idx'
			// Candles should exist because we know we're in the time range of the candle data.
			var c0 = this[idx  ]; Debug.Assert(c0 != null);
			var c1 = this[idx+1]; Debug.Assert(c1 != null);
			
			var frac = Maths.Frac(c0.Timestamp, time.ExactTicks, c1.Timestamp);
			Debug.Assert(frac >= 0.0 && frac <= 1.0);
			return idx + frac;
		}

		/// <summary>Enumerate the candles within an index range (i.e. time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(NegIdx idx_min, NegIdx idx_max)
		{
			var r = IndexRange(idx_min, idx_max);
			for (var i = r.Begini; i != r.Endi; ++i)
				yield return this[i];
		}

		/// <summary>Enumerate the candles within a time range (given in time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(TFTime time_min, TFTime time_max)
		{
			var range = TimeToIndexRange(time_min, time_max);
			return CandleRange(range.Begini, range.Endi);
		}

		/// <summary>Invalidate the cached data</summary>
		public void InvalidateCachedData()
		{
			m_cache.Clear();
			m_index_range = Range.Zero;
			m_impl_count  = null;
			m_impl_total  = null;
			m_latest      = null;
			m_oldest      = null;
		}

		/// <summary>The cached price data database file location</summary>
		public string DBFilepath
		{
			get { return CacheDBFilePath(Model.Settings, SymbolCode); }
		}
		public static string CacheDBFilePath(Settings settings, string sym)
		{
			return Path_.CombinePath(settings.General.PriceDataCacheDir, "PriceData_{0}.db".Fmt(sym));
		}

		/// <summary>Start data being sent for this instrument</summary>
		public void StartDataRequest()
		{
			Model.Post(new OutMsg.RequestInstrument(SymbolCode, TimeFrame));

			// Ensure a reasonable history of candle data has been pulled from the trade data source
			var t0 = Model.UtcNow - Misc.TimeFrameToTimeSpan(Settings.General.HistoryLength, TimeFrame);
			var t1 = Model.UtcNow;
			RequestTimeRange(t0, t1, false);
		}

		/// <summary>Stop data being sent for this instrument</summary>
		private void StopDataRequest()
		{
			Model.Post(new OutMsg.RequestInstrumentStop(SymbolCode, TimeFrame));
		}

		/// <summary>
		/// Request candle data for a time range.
		/// 'diff_cache' means only request time ranges that appear missing in the local instrument cache db</summary>
		public void RequestTimeRange(DateTimeOffset t0, DateTimeOffset t1, bool diff_cache)
		{
			// The request message
			var msg = new OutMsg.RequestInstrumentHistory(SymbolCode, TimeFrame);

			// If the DB is currently empty, request the whole range
			if (!diff_cache || Count == 0)
			{
				msg.TimeRanges.Add(t0.Ticks);
				msg.TimeRanges.Add(t1.Ticks);
			}
			// Otherwise, request time ranges that are missing from the DB
			else
			{
				var time_ranges = new List<Range>();

				// One time frame unit
				var one = Misc.TimeFrameToTicks(1.0, TimeFrame);
				var ts = "["+nameof(Candle.Timestamp)+"]";

				// If the oldest candle in the DB is newer than 't0', request from t0 to Oldest
				if (Oldest.TimestampUTC > t0)
				{
					Debug.Assert(!Equals(Oldest, Candle.Default));
					time_ranges.Add(new Range(t0.Ticks, Oldest.Timestamp));
				}

				// Scan the DB for time ranges of missing data.
				var sql = Str.Build( // Returns pairs of timestamps that are the ranges of missing data.
					$"select {ts} from {TimeFrame} where {ts} >= ? and {ts} <= ? and {ts}+? not in (select {ts} from {TimeFrame}) union all ",
					$"select {ts} from {TimeFrame} where {ts} >= ? and {ts} <= ? and {ts}-? not in (select {ts} from {TimeFrame}) ",
					$"order by {ts}");
				var args = new object[] { t0.Ticks, t1.Ticks, one, t0.Ticks, t1.Ticks, one, };

				// Note: ignore the first and last because they are the implicit ranges -inf to first, and last -> +inf
				foreach (var hole in m_db.EnumRows<long>(sql, 1, args).Skip(1).TakeAllBut(1).InPairs())
					time_ranges.Add(new Range(hole.Item1, hole.Item2));

				// If the newest candle in the DB is older than 't1', request from Latest to t1
				if (Latest.TimestampUTC < t1)
				{
					Debug.Assert(!Equals(Latest, Candle.Default));
					time_ranges.Add(new Range(Latest.Timestamp, t1.Ticks));
				}

				// Exclude the known forex non trading hours
				foreach (var time_range in time_ranges)
				{
					foreach (var hole in Model.Sessions.ClipToTradingHours(time_range))
					{
						Debug.Assert(hole.Size != 0);
						msg.TimeRanges.Add(hole.Begin);
						msg.TimeRanges.Add(hole.End);
					}
				}
			}

			// Post the request
			Model.Post(msg);
		}

		/// <summary>Request historic candle data by index range</summary>
		public void RequestIndexRange(NegIdx i_oldest, NegIdx i_newest, bool only_if_missing)
		{
			Debug.Assert(i_oldest <= i_newest);
			var msg = new OutMsg.RequestInstrumentHistory(SymbolCode, TimeFrame);

			// See if we have this range already
			if (only_if_missing)
			{
				var range = IndexRange(i_oldest, i_newest);
				if (i_oldest < range.Begin)
				{
					msg.IndexRanges.Add(i_oldest);
					msg.IndexRanges.Add(Math.Min(range.Begini, i_newest));
				}
				if (i_newest > range.End)
				{
					msg.IndexRanges.Add(Math.Max(range.Endi, i_oldest));
					msg.IndexRanges.Add(i_newest);
				}
			}
			else
			{
				msg.IndexRanges.Add(i_oldest);
				msg.IndexRanges.Add(i_newest);
			}

			// Request if ranges needed
			if (msg.IndexRanges.Count != 0)
				Model.Post(msg);
		}

		/// <summary>Handle the connection to the trade data source changing</summary>
		private void HandleConnectionChanged(object sender, EventArgs e)
		{
			// On connection to a new trade data source, flush any cached data as the
			// latest, oldest, instrument data may have changed.
			if (Model.IsConnected)
			{
				InvalidateCachedData();
			}
		}

		/// <summary>Price levels that are support and resist levels in this instrument</summary>
		public BindingSource<SnRLevel> SupportResistLevels
		{
			[DebuggerStepThrough] get { return m_snr_levels; }
			private set
			{
				if (m_snr_levels == value) return;
				if (m_snr_levels != null)
				{
					m_snr_levels.ListChanging -= HandleSnRLevelsChanging;
				}
				m_snr_levels = value;
				if (m_snr_levels != null)
				{
					m_snr_levels.ListChanging += HandleSnRLevelsChanging;
				}
			}
		}
		private BindingSource<SnRLevel> m_snr_levels;

		/// <summary>Handle SnR levels added/removed</summary>
		private void HandleSnRLevelsChanging(object sender, ListChgEventArgs<SnRLevel> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					// Watch for changes to the SnRLevel so we can update the DB
					e.Item.PropertyChanged += HandleSnRLevelChanged;
					HandleSnRLevelChanged(e.Item);
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Stop watching for changes
					e.Item.PropertyChanged -= HandleSnRLevelChanged;

					// Remove this level from the db.
					// Note: this isn't called during shutdown because this event handler
					// is removed from the binding source before it is disposed.
					m_db.Execute(SqlExpr.RemoveSnRLevel(), 1, SqlExpr.RemoveSnRLevelParams(e.Item));

					break;
				}
			}
		}

		/// <summary>Handle an individual SnR level being updated</summary>
		private void HandleSnRLevelChanged(object sender, PropertyChangedEventArgs e = null)
		{
			if (m_suspend_db_updates != 0)
				return;

			// Update this SnR level in the db
			var snr = (SnRLevel)sender;
			m_db.Execute(SqlExpr.UpdateSnRLevel(), 1, SqlExpr.UpdateSnRLevelParams(snr));
		}
	}
}
