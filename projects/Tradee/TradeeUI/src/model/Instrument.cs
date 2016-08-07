using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>The data series for an instrument at a specific time frame</summary>
	[DebuggerDisplay("{SymbolCode},{TimeFrame}")]
	public class Instrument :IDisposable
	{
		private const int MaxLength = 100000;
		private const int ReduceSize = 1000;

		/// <summary>A database of price data for this instrument</summary>
		private Sqlite.Database m_db;

		public Instrument(MainModel model, string symbol)
		{
			m_price_history = new List<Candle>(MaxLength);
			Model = model;
			SymbolCode = symbol;

			// Load the sqlite database of historic price data
			m_db = new Sqlite.Database(DBFilepath);

			// Tweak some DB settings for performance
			m_db.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
			m_db.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

			// Ensure tables exist for each of the time frames
			var sql = Sqlite.Sql("create table if not exists {0} (\n",
				"[",nameof(Candle.Timestamp),"] integer primary key,\n",
				"[",nameof(Candle.Open     ),"] real,\n",
				"[",nameof(Candle.High     ),"] real,\n",
				"[",nameof(Candle.Low      ),"] real,\n",
				"[",nameof(Candle.Close    ),"] real,\n",
				"[",nameof(Candle.Volume   ),"] real)");
			foreach (var tf in Enum<ETimeFrame>.Values.Skip(1))
				m_db.Execute(sql.Fmt(tf));

			// Select no time frame to begin with
			TimeFrame = ETimeFrame.None;
			PriceData = new PriceData(0,0,0,0,0,0,0,0);
		}
		public virtual void Dispose()
		{
			TimeFrame = ETimeFrame.None;
			Util.Dispose(ref m_db);
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The instrument name (symbol name)</summary>
		public string SymbolCode
		{
			get;
			private set;
		}

		/// <summary>The active time frame</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return m_time_frame; }
			set
			{
				if (m_time_frame == value) return;
				if (m_time_frame != ETimeFrame.None)
				{
					m_price_history.Clear();
				}
				m_time_frame = value;
				if (m_time_frame != ETimeFrame.None)
				{
					// Populate the data with that last 1000 candles
					var count = m_db.ExecuteScalar(Str.Build("select count(*) from ",TimeFrame));
					m_price_history.AddRange(m_db.EnumRows<Candle>(Str.Build("select * from ",TimeFrame," limit ?,1000"), 1, new object[] { Math.Max(0, count - 1000) }));

					// Set the last updated time to the timestamp of the last candle
					LastUpdatedUTC = Latest.TimestampUTC;
				}
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
				PriceDataUpdated.Raise(this);
			}
		}
		private PriceData m_price_data;

		/// <summary>The raw data</summary>
		public IReadOnlyList<Candle> PriceHistory { get { return m_price_history; } }
		private List<Candle> m_price_history;

		/// <summary>The number of data points in the series</summary>
		public int Count
		{
			get { return m_price_history.Count; }
		}

		/// <summary>The last received candle</summary>
		public Candle Latest
		{
			get { return Count != 0 ? m_price_history.Back() : Candle.Default; }
		}

		/// <summary>The timestamp of the last update (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			get;
			private set;
		}

		/// <summary>The timestamp of the last update (in local time)</summary>
		public DateTimeOffset LastUpdatedLocal
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(LastUpdatedUTC.DateTime); }
		}

		/// <summary>The cached price data database file location</summary>
		public string DBFilepath
		{
			get { return CacheDBFilePath(Model.Settings, SymbolCode); }
		}
		public static string CacheDBFilePath(Settings settings, string code)
		{
			return Path_.CombinePath(settings.General.PriceDataCacheDir, "PriceData_{0}.db".Fmt(code));
		}

		/// <summary>Add a candle value to the data</summary>
		public void Add(ETimeFrame tf, Candle candle)
		{
			// Sanity check
			Debug.Assert(candle.Timestamp < (DateTime.UtcNow + TimeSpan.FromDays(100)).Ticks);
			Debug.Assert(candle.High >= candle.Open && candle.High >= candle.Close);
			Debug.Assert(candle.Low  <= candle.Open && candle.Low  <= candle.Close);
			Debug.Assert(candle.High >= candle.Low);
			Debug.Assert(candle.Volume >= 0); // empty candles? cTrader sends them tho...
			Debug.Assert(tf != ETimeFrame.None);

			LastUpdatedUTC = DateTimeOffset.UtcNow;
			var new_candle = false;

			// Insert the candle in the appropriate database
			using (var query = new Sqlite.InsertCmd(tf.ToString(), typeof(Candle), m_db, Sqlite.OnInsertConstraint.Replace))
			{
				query.BindObj(candle);
				query.Run();
			}

			// If the candle is in the currently loaded time frame add it to the data
			if (tf == TimeFrame)
			{
				// Candles are stored in time order
				if (candle.Timestamp > Latest.Timestamp)
				{
					m_price_history.Add(candle);
					new_candle = true;
				}

				// If this candle is an update to the last received candle, update it (don't replace for ref equals sakes)
				else if (candle.Timestamp == Latest.Timestamp)
				{
					Latest.Update(candle);
				}

				// Otherwise this is a historic candle, insert or update
				else
				{
					var idx = m_price_history.BinarySearch(x => x.Timestamp.CompareTo(candle.Timestamp));
					if (idx < 0)
						m_price_history.Insert(~idx, candle);
					else
						m_price_history[idx].Update(candle);

				}

				// Limit the length of the data buffer
				if (m_price_history.Count > MaxLength)
					m_price_history.RemoveRange(0, ReduceSize);
			}

			// Notify data added
			DataAdded.Raise(this, new DataEventArgs(this, tf, candle, new_candle));
		}
		public void Add(ETimeFrame tf, Candles candles)
		{
			using (var t = m_db.NewTransaction())
			{
				foreach (var c in candles.AllCandles)
					Add(tf, c);
				t.Commit();
			}
		}

		/// <summary>Raised when 'PriceData' is updated</summary>
		public event EventHandler PriceDataUpdated;

		/// <summary>Raised whenever data is added to this instrument</summary>
		public event EventHandler<DataEventArgs> DataAdded;

		/// <summary>Return the index of the candle at 'time_stamp'. Returns the two's compliment index if there is no candle at that position</summary>
		public int IndexAt(TFTime time_stamp)
		{
			var ticks = time_stamp.ExactTicks;
			var idx = m_price_history.BinarySearch(x => x.Timestamp.CompareTo(ticks));
			return idx;
		}

		/// <summary>Clamps the given index range to a valid range within the data</summary>
		public Range IndexRange(int idx_min, int idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Maths.Clamp(idx_min, 0, Count);
			var max = Maths.Clamp(idx_max, min, Count);
			return new Range(min, max);
		}

		/// <summary>
		/// Convert a time range (in time frame units) to an index range in 'Data'.
		/// Returns candles with timestamps in the range [time_min, time_max)</summary>
		public Range TimeToIndexRange(TFTime time_min, TFTime time_max)
		{
			Debug.Assert(time_min <= time_max);

			var idx_min = IndexAt(time_min);
			var idx_max = IndexAt(time_max);
			if (idx_min < 0) idx_min = ~idx_min;
			if (idx_max < 0) idx_max = ~idx_max;

			return new Range(idx_min, idx_max);
		}

		/// <summary>Enumerate the candles within an index range (given in time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(int idx_min, int idx_max)
		{
			var r = IndexRange(idx_min, idx_max);
			for (var i = r.Begini; i != r.Endi; ++i)
				yield return m_price_history[i];
		}

		/// <summary>Enumerate the candles within a time range (given in time-frame units)</summary>
		public IEnumerable<Candle> Range(TFTime time_min, TFTime time_max)
		{
			var range = TimeToIndexRange(time_min, time_max);
			return CandleRange(range.Begini, range.Endi);
		}
	}
}
