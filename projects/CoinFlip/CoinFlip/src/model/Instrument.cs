using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using pr.common;
using pr.db;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>The candle data for a trading pair</summary>
	public class Instrument :IDisposable ,IEnumerable<Candle>
	{
		// Notes:
		// - This class is basically an array with an indexer and Count.

		/// <summary>A database of price data for this instrument</summary>
		private Sqlite.Database m_db;
		private int m_suspend_db_updates;

		/// <summary>A cache of candle data read from the database</summary>
		private List<Candle> m_cache;
		private const int CacheSize = 10000;
		private const int MaxCacheSize = 15000;

		public Instrument(Model model, TradePair pair, ETimeFrame time_frame)
		{
			Model = model;
			Pair = pair;

			// The database filepath
			m_db_filepath = CacheDBFilePath(Pair.NameWithExchange);

			// Load the sqlite database of historic price data
			m_db = new Sqlite.Database(DBFilepath);

			// Tweak some DB settings for performance
			m_db.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
			m_db.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

			// Ensure tables exist for each of the time frames.
			// Note: timestamp is not the row id. Row Ids should not have any
			// meaning. It's more efficient to let the DB do the sorting
			// and just use 'order by' in queries
			foreach (var tf in Enum<ETimeFrame>.Values.Except(ETimeFrame.None))
				m_db.Execute(SqlExpr.CandleTable(tf));

			// A cache of candle data read from the db
			m_cache = new List<Candle>(CacheSize);

			// Initialise from the DB, so don't write back to the db
			using (Scope.Create(() => ++m_suspend_db_updates, () => --m_suspend_db_updates))
			{
				// Select the time frame to begin with
				TimeFrame = time_frame;
			}
		}
		public Instrument(Instrument rhs, ETimeFrame time_frame)
			:this(rhs.Model, rhs.Pair, time_frame)
		{}
		public virtual void Dispose()
		{
			UpdateThreadActive = false;
			TimeFrame = ETimeFrame.None;
			Pair = null;
			Model = null;
			Util.Dispose(ref m_db);
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
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>The currency pair this instrument represents</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			private set
			{
				if (m_pair == value) return;
				m_pair = value;
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

		/// <summary>The active time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return m_time_frame; }
			set
			{
				if (m_time_frame == value) return;

				// Stop the update thread while changing time frame
				UpdateThreadActive = false;

				// Set the new time frame
				m_time_frame = value;

				// Flush any cached data
				InvalidateCachedData();

				// Set the last updated time to the timestamp of the last candle
				LastUpdatedUTC = Latest.TimestampUTC;

				// Notify time frame changed
				OnTimeFrameChanged();

				// Start the update thread for non-none time frames
				UpdateThreadActive = m_time_frame != ETimeFrame.None;
			}
		}
		private ETimeFrame m_time_frame;
		public event EventHandler TimeFrameChanged;
		protected virtual void OnTimeFrameChanged()
		{
			TimeFrameChanged.Raise(this);
		}

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			get
			{
				if (TimeFrame == ETimeFrame.None)
					return 0;

				// Cache the total count value
				if (m_impl_count == null)
				{
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

		/// <summary>The candle with the latest timestamp for the current time frame</summary>
		public Candle Latest
		{
			get
			{
				if (m_latest == null && TimeFrame != ETimeFrame.None)
				{
					// If the cache covers the latest candle, return it from the cache otherwise get it from the DB.
					if (m_index_range.Counti != 0 && m_index_range.Endi == Count)
					{
						// The fast/common case
						m_latest = m_cache.Back();
					}
					else
					{
						var ts = $"[{nameof(Candle.Timestamp)}]";
						var sql = Str.Build("select * from ",TimeFrame," where ",ts," <= ? order by ",ts," desc limit 1");
						m_latest = m_db.EnumRows<Candle>(sql, 1, new object[] { Model.UtcNow.Ticks }).FirstOrDefault();
					}
				}
				return m_latest ?? Candle.Default;
			}
		}
		private Candle m_latest;

		/// <summary>The candle with the oldest timestamp for the current time frame</summary>
		public Candle Oldest
		{
			get
			{
				if (m_oldest == null && TimeFrame != ETimeFrame.None)
				{
					// If the cache covers the latest candle, return it from the cache otherwise get it from the DB.
					if (m_index_range.Counti != 0 && m_index_range.Begi == 0)
					{
						m_oldest = m_cache.Front();
					}
					else
					{
						var ts = $"[{nameof(Candle.Timestamp)}]";
						var sql = Str.Build("select * from ",TimeFrame," where ",ts," < ? order by ",ts," asc limit 1");
						m_oldest = m_db.EnumRows<Candle>(sql, 1, new object[] { Model.UtcNow.Ticks }).FirstOrDefault();
					}
				}
				return m_oldest ?? Candle.Default;
			}
		}
		private Candle m_oldest;

		/// <summary>The raw data. Idx = 0 is the oldest, Idx = Count is the latest</summary>
		public Candle this[int idx]
		{
			get
			{
				Debug.Assert(idx >= 0 && idx < Count);

				// Shift the cached range if needed
				if (!m_index_range.Contains(idx))
				{
					// Reload the cache centred on the requested index
					var new_range = new Range((long)idx - CacheSize/2, (long)idx + CacheSize/2);
					if (new_range.Beg <     0) new_range = new_range.Shift(0     - new_range.Beg);
					if (new_range.End > Count) new_range = new_range.Shift(Count - new_range.End);
					if (new_range.Beg <     0) new_range.Beg = 0;

					// Populate the cache from the database
					// Order by timestamp so that the oldest is first, and the newest is at the end.
					var sql = Str.Build("select * from ",TimeFrame," order by [",nameof(Candle.Timestamp),"] limit ?,?");
					m_cache.AddRange(m_db.EnumRows<Candle>(sql, 1, new object[] { new_range.Begi, new_range.Sizei }));
					m_index_range = new_range;
				}

				return m_cache[idx - m_index_range.Begi];
			}
		}

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

		#region Candle Cache

		/// <summary>The instrument database file location.</summary>
		public string DBFilepath { get { return m_db_filepath; } }
		private readonly string m_db_filepath; // Shared across threads

		/// <summary>Generate a filepath for the given pair name</summary>
		public static string CacheDBFilePath(string pair_name)
		{
			var dbpath = Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", $"PriceData\\{pair_name}.db");
			Directory.CreateDirectory(Path_.Directory(dbpath));
			return dbpath;
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

		/// <summary>The background thread for performing instrument data updates</summary>
		private bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			set
			{
				if (UpdateThreadActive == value) return;
				if (UpdateThreadActive)
				{
					m_update_thread_exit.Set();
					if (m_update_thread.IsAlive)
						m_update_thread.Join();
				}
				Util.Dispose(ref m_update_thread_exit);
				m_update_thread = value ? new Thread(new ThreadStart(UpdateThreadEntryPoint)) : null;
				if (UpdateThreadActive)
				{
					m_update_thread_exit = new ManualResetEvent(false);
					m_update_thread.Start();
				}
			}
		}
		private Thread m_update_thread;
		private ManualResetEvent m_update_thread_exit;
		private void UpdateThreadEntryPoint()
		{
			try
			{
				var exch = Exchange;
				var pair = Pair;
				var tf   = TimeFrame;
				Debug.Assert(tf != ETimeFrame.None);

				using (var db = new Sqlite.Database(DBFilepath))
				{
					// Find the time range of data available in the db
					var beg = DateTimeOffset.MinValue;
					var end = DateTimeOffset.Now;

					for (; !m_update_thread_exit.IsSignalled();)
					{
						// Query for chart data
						{
							var data = exch.ChartData(pair, tf, beg, end).Result;
						}

						// Add the received data to the database
						{
						}

						// Notify new data available
						Model.RunOnGuiThread(() =>
						{
						});

						// Sleep for a bit
						m_update_thread_exit.WaitOne(TimeSpan.FromMilliseconds(500));
					}
				}
			}
			catch (Exception ex)
			{
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else Model.Log.Write(ELogLevel.Error, ex, $"Instrument {SymbolCode} update thread exit\r\n.{ex.StackTrace}");
			}
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

		/// <summary>Sql expression strings</summary>
		public static class SqlExpr
		{
			#region Price Data

			///// <summary>Create a price data table</summary>
			//public static string PriceDataTable()
			//{
			//	// Note: this doesn't store the price data history, only the last received price data
			//	return Str.Build(
			//		"create table if not exists PriceData (\n",
			//		"[",nameof(PriceData.AskPrice  ),"] real,\n",
			//		"[",nameof(PriceData.BidPrice  ),"] real,\n",
			//		"[",nameof(PriceData.AvrSpread ),"] real,\n",
			//		"[",nameof(PriceData.LotSize   ),"] real,\n",
			//		"[",nameof(PriceData.PipSize   ),"] real,\n",
			//		"[",nameof(PriceData.PipValue  ),"] real,\n",
			//		"[",nameof(PriceData.VolumeMin ),"] real,\n",
			//		"[",nameof(PriceData.VolumeStep),"] real,\n",
			//		"[",nameof(PriceData.VolumeMax ),"] real)"
			//		);
			//}

			///// <summary>Sql expression to get the price data from the db</summary>
			//public static string GetPriceData()
			//{
			//	return "select * from PriceData";
			//}

			///// <summary>Update the price data</summary>
			//public static string UpdatePriceData()
			//{
			//	return Str.Build(
			//		"insert or replace into PriceData ( ",
			//		"rowid,",
			//		"[",nameof(PriceData.AskPrice  ),"],",
			//		"[",nameof(PriceData.BidPrice  ),"],",
			//		"[",nameof(PriceData.AvrSpread ),"],",
			//		"[",nameof(PriceData.LotSize   ),"],",
			//		"[",nameof(PriceData.PipSize   ),"],",
			//		"[",nameof(PriceData.PipValue  ),"],",
			//		"[",nameof(PriceData.VolumeMin ),"],",
			//		"[",nameof(PriceData.VolumeStep),"],",
			//		"[",nameof(PriceData.VolumeMax ),"])",
			//		" values (",
			//		"1,", // rowid
			//		"?,", // AskPrice  
			//		"?,", // BidPrice  
			//		"?,", // AvrSpread 
			//		"?,", // LotSize   
			//		"?,", // PipSize   
			//		"?,", // PipValue  
			//		"?,", // VolumeMin 
			//		"?,", // VolumeStep
			//		"?)"  // VolumeMax 
			//		);
			//}

			///// <summary>Return the properties of a price data object to match the update command</summary>
			//public static object[] UpdatePriceDataParams(PriceData pd)
			//{
			//	return new object[]
			//	{
			//		pd.AskPrice  ,
			//		pd.BidPrice  ,
			//		pd.AvrSpread ,
			//		pd.LotSize   ,
			//		pd.PipSize   ,
			//		pd.PipValue  ,
			//		pd.VolumeMin ,
			//		pd.VolumeStep,
			//		pd.VolumeMax ,
			//	};
			//}

			#endregion

			#region Candles

			/// <summary>Create a table of candles for a time frame</summary>
			public static string CandleTable(ETimeFrame time_frame)
			{
				return Str.Build(
					"create table if not exists ",time_frame," (\n",
					"[",nameof(Candle.Timestamp),"] integer unique,\n",
					"[",nameof(Candle.Open     ),"] real,\n",
					"[",nameof(Candle.High     ),"] real,\n",
					"[",nameof(Candle.Low      ),"] real,\n",
					"[",nameof(Candle.Close    ),"] real,\n",
					"[",nameof(Candle.Median   ),"] real,\n",
					"[",nameof(Candle.Volume   ),"] real)"
					);
			}

			/// <summary>Insert or replace a candle in table 'time_frame'</summary>
			public static string InsertCandle(ETimeFrame time_frame)
			{
				return Str.Build(
					"insert or replace into ",time_frame," (",
					"[",nameof(Candle.Timestamp),"],",
					"[",nameof(Candle.Open     ),"],",
					"[",nameof(Candle.High     ),"],",
					"[",nameof(Candle.Low      ),"],",
					"[",nameof(Candle.Close    ),"],",
					"[",nameof(Candle.Median   ),"],",
					"[",nameof(Candle.Volume   ),"])",
					" values (",
					"?,", // Timestamp
					"?,", // Open     
					"?,", // High     
					"?,", // Low      
					"?,", // Close    
					"?,", // Median   
					"?)"  // Volume   
					);
			}

			/// <summary>Return the properties of a candle to match an InsertCandle sql statement</summary>
			public static object[] InsertCandleParams(Candle candle)
			{
				return new object[]
				{
					candle.Timestamp,
					candle.Open,
					candle.High,
					candle.Low,
					candle.Close,
					candle.Median,
					candle.Volume,
				};
			}

			#endregion
		}
	}
}
