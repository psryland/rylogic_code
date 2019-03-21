using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;
using CoinFlip.Settings;
using Dapper;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class PriceData :IDisposable
	{
		// Notes:
		// - PriceData represents a single pair and TimeFrame on an exchange.
		// - It reads candle data from the exchange (or from the DB in back
		//   testing) and writes it into a DB (unless back testing).
		// - Use an 'Instrument' to view these data

		public PriceData(TradePair pair, ETimeFrame time_frame, CancellationToken shutdown)
		{
			try
			{
				Pair = pair;
				TimeFrame = time_frame;
				UpdatePollRate = TimeSpan.FromMilliseconds(SettingsData.Settings.PriceDataUpdatePeriodMS);
				MainShutdownToken = shutdown;

				// Set up the ref count
				m_ref = new List<object>();

				// Load the database of historic price data
				var db_filepath = DBFilePath(pair.Exchange.Name, pair.Name);
				DB = new SQLiteConnection($"Data Source={db_filepath};Version=3;journal mode=Memory;synchronous=Off");

				// Ensure a table exists for each time frame
				foreach (var tf in pair.CandleDataAvailable)
				{
					DB.Execute(
						$"create table if not exists {tf} (\n" +
						$"  [{nameof(Candle.Timestamp)}] integer unique primary key,\n" +
						$"  [{nameof(Candle.Open)}] real not null,\n" +
						$"  [{nameof(Candle.High)}] real not null,\n" +
						$"  [{nameof(Candle.Low)}] real not null,\n" +
						$"  [{nameof(Candle.Close)}] real not null,\n" +
						$"  [{nameof(Candle.Median)}] real not null,\n" +
						$"  [{nameof(Candle.Volume)}] real not null\n" +
						$")");
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Util.BreakIf(m_ref.Count != 0, "Price Data still in use");
			UpdateThreadActive = false;
			DB = null;
			//Pair = null;
			//Model = null;
		}

		/// <summary>The pair we're collecting candle data for</summary>
		public TradePair Pair { get; }

		/// <summary>The time frame we're collecting candle data for</summary>
		public ETimeFrame TimeFrame { get; }

		/// <summary>Currency pair name, e.g. ETHBTC</summary>
		public string SymbolCode => Pair.Name;

		/// <summary>Generate a filepath for the given pair name</summary>
		public static string DBFilePath(string exchange_name, string pair_name)
		{
			var dbpath = Misc.ResolveUserPath("PriceData", $"{Path_.SanitiseFileName(pair_name)} - {Path_.SanitiseFileName(exchange_name)}.db");
			Path_.CreateDirs(Path_.Directory(dbpath));
			return dbpath;
		}

		/// <summary>How frequently to update the price data</summary>
		private TimeSpan UpdatePollRate { get; }

		/// <summary>The application shutdown token</summary>
		private CancellationToken MainShutdownToken { get; }

		/// <summary>Database access</summary>
		private SQLiteConnection DB
		{
			[DebuggerStepThrough]
			get { return m_db; }
			set
			{
				if (m_db == value) return;
				if (m_db != null)
				{
					m_db.Close();
					Util.Dispose(ref m_db);
				}
				m_db = value;
				if (m_db != null)
				{
					m_db.Open();
				}
			}
		}
		private SQLiteConnection m_db;

		/// <summary>Create a reference to this price data. When the RefCount != 0, this object collects price data</summary>
		public Scope RefToken(object who)
		{
			return Scope.Create(
				() =>
				{
					if (m_ref.Count == 0) UpdateThreadActive = !Model.BackTesting;
					m_ref.Add(who);
				},
				() =>
				{
					m_ref.Remove(who);
					if (m_ref.Count == 0) UpdateThreadActive = false;
				});
		}
		private readonly List<object> m_ref;

		/// <summary>The total number of candles</summary>
		public int Count
		{
			get
			{
				if (m_total == null)
				{
					if (TimeFrame == ETimeFrame.None) throw new Exception("Invalid time frame");
					m_total = DB.ExecuteScalar<int>($"select count(1) from {TimeFrame}");
				}
				return m_total.Value;
			}
		}
		private int? m_total;

		/// <summary>The number of candles with a timestamp less than 'max_timestamp'</summary>
		public int CountTo(long max_timestamp_ticks)
		{
			if (TimeFrame == ETimeFrame.None) throw new Exception("Invalid time frame");
			return DB.ExecuteScalar<int>(
				$"select count(1) from {TimeFrame}\n" +
				$"where [{nameof(Candle.Timestamp)}] <= @max_timestamp"
				, new { max_timestamp = max_timestamp_ticks });
		}

		/// <summary>The candle with the newest timestamp (or null if there is no data yet)</summary>
		public Candle Newest
		{
			get
			{
				if (m_newest == null)
				{
					m_newest = DB.QuerySingleOrDefault<Candle>(
						$"select * from {TimeFrame}\n"+
						$"order by [{nameof(Candle.Timestamp)}] desc limit 1");
				}
				return m_newest;
			}
		}
		private Candle m_newest;

		/// <summary>The candle with the oldest timestamp (or null if there is no data yet)</summary>
		public Candle Oldest
		{
			get
			{
				if (m_oldest == null)
				{
					m_oldest = DB.QuerySingleOrDefault<Candle>(
						$"select * from {TimeFrame}\n"+
						$"order by [{nameof(Candle.Timestamp)}] asc limit 1");
				}
				return m_oldest;
			}
		}
		private Candle m_oldest;

		/// <summary>The candle with the latest timestamp for the current time. Note: Current != Newest when back testing (null if no data yet)</summary>
		public Candle Current
		{
			get
			{
				if (m_current == null)
				{
					m_current = DB.QuerySingleOrDefault<Candle>(
						$"select * from {TimeFrame}\n"+
						$"where [{nameof(Candle.Timestamp)}] <= @time_stamp\n" +
						$"order by [{nameof(Candle.Timestamp)}] desc limit 1",
						new { time_stamp = Model.UtcNow.Ticks });
				}
				if (m_current != null && Model.BackTesting)
				{
					// Interpolate the latest candle to determine the spot price
					var t = Math_.Frac(m_current.Timestamp, Model.UtcNow.Ticks, m_current.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame));
					return m_current.SubCandle(Math_.Clamp(t, 0.0, 1.0));
				}
				return m_current;
			}
		}
		private Candle m_current;

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC { get; private set; }

		/// <summary>String description of the instrument</summary>
		public string Description => $"{Pair.NameWithExchange}";

		/// <summary>Return the candles over the given time range</summary>
		public IEnumerable<Candle> ReadCandles(Range time_period)
		{
			// Read from the database. Order by timestamp so that the oldest is first, and the newest is at the end.
			return DB.Query<Candle>(
				$"select * from {TimeFrame}\n" +
				$"order by [{nameof(Candle.Timestamp)}] limit @start,@count",
				new { start = time_period.Beg, count = time_period.Size });
		}

		#region Update

		/// <summary>The background thread for performing candle data updates</summary>
		public bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Misc.AssertMainThread());

				// Don't allow the update thread to run during back testing
				if (value && Model.BackTesting)
					throw new Exception("Update thread should not be activated while back-testing");

				// Shutdown the thread if currently running
				if (m_update_thread != null)
				{
					m_update_thread_exit.Set();
					m_update_thread_cancel.Cancel();
					if (m_update_thread.IsAlive)
						m_update_thread.Join();

					Util.Dispose(ref m_update_thread_exit);
					Util.Dispose(ref m_update_thread_cancel);
				}

				// Create a new thread (if active)
				var latest_timestamp = Newest?.Timestamp ?? Misc.CryptoCurrencyEpoch.Ticks;
				m_update_thread = value ? new Thread(() => UpdateThreadEntryPoint(Pair.Exchange, Pair, TimeFrame, UpdatePollRate, latest_timestamp)) : null;

				// Start the new thread
				if (m_update_thread != null)
				{
					m_update_thread_cancel = CancellationTokenSource.CreateLinkedTokenSource(MainShutdownToken);
					m_update_thread_exit = new ManualResetEvent(false);
					m_update_thread.Start();

					// Raise the data syncing event to set the initial state
					DataSyncingChanged?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Thread entry point for the instrument update thread</summary>
				void UpdateThreadEntryPoint(Exchange exch, TradePair pair, ETimeFrame time_frame, TimeSpan poll_rate, long start_time)
				{
					try
					{
						Thread.CurrentThread.Name = $"{SymbolCode} PriceData Update";
						Model.Log.Write(ELogLevel.Debug, $"PriceData {SymbolCode} update thread started");

						// Limit requests to batches of 25K candles
						const int max_candles_per_request = 25_000;
						var max_request_period = Misc.TimeFrameToTicks(max_candles_per_request, time_frame);
						long PeriodEnd(long b) => Math.Min(DateTimeOffset.UtcNow.Ticks, b + max_request_period);

						var beg = start_time;
						var end = PeriodEnd(beg);
						for (;;)
						{
							if (m_update_thread_exit.WaitOne(poll_rate))
								break;

							// Try to request the entire candle range.
							var data = exch.CandleData(pair, time_frame, beg, end, m_update_thread_cancel.Token).Result;
							if (data == null)
							{
								// If that fails, halve the range and try again
								end = beg + (end - beg) / 2;
								continue;
							}
							if (data.Count == 0)
							{
								// If the request returns no data, advance beg to end
								beg = end;
								end = PeriodEnd(beg);
								continue;
							}

							// Update the time range to include the returned data
							beg = data.Back().Timestamp;
							end = PeriodEnd(beg);

							// Add the received data to the database
							Misc.RunOnMainThread(() =>
							{
								if (!UpdateThreadActive)
									return;

								if (data.Count == 1)
									Add(time_frame, data[0]);
								else
									Add(time_frame, data);

								// Raise the event if 'DataSyncing' has changed
								if (m_data_syncing != DataSyncing)
								{
									m_data_syncing = DataSyncing;
									DataSyncingChanged?.Invoke(this, EventArgs.Empty);
								}
							});
						}
						Model.Log.Write(ELogLevel.Debug, $"PriceData {SymbolCode} update thread stopped");
					}
					catch (Exception ex)
					{
						Model.Log.Write(ELogLevel.Error, ex, $"PriceData {SymbolCode} update thread exit\r\n.{ex.StackTrace}");
					}
				}
			}
		}
		private CancellationTokenSource m_update_thread_cancel;
		private ManualResetEvent m_update_thread_exit;
		private Thread m_update_thread;
		private bool m_data_syncing;

		/// <summary>Add a candle value to the database</summary>
		private void Add(ETimeFrame tf, Candle candle)
		{
			// Sanity check
			Debug.Assert(candle.Valid());
			Debug.Assert(candle.Timestamp != 0);
			Debug.Assert(tf != ETimeFrame.None);
			Debug.Assert(tf == TimeFrame);
			if (Model.BackTesting)
				throw new Exception("Should not be adding candles to the DB while back testing");

			// This is a new candle if it's time stamp is >= the TimeFrame period after the Newest candle.
			// This is an update to the latest candle if within a TimeFrame period of the Newest candle.
			var update_type =
				Newest == null ? DataEventArgs.EUpdateType.New :
				candle.Timestamp >= Newest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame) ? DataEventArgs.EUpdateType.New :
				candle.Timestamp >= Newest.Timestamp ? DataEventArgs.EUpdateType.Current :
				DataEventArgs.EUpdateType.Range;

			// Insert the candle into the database if is for 'TimeFrame' and the sim isn't running.
			UpsertCandle(candle);

			// Update/Invalidate cached values
			switch (update_type)
			{
			case DataEventArgs.EUpdateType.New:
				{
					m_newest = candle;
					m_current = m_newest;
					m_total += 1;
					break;
				}
			case DataEventArgs.EUpdateType.Current:
				{
					m_newest.Update(candle);
					m_current = m_newest;
					break;
				}
			default:
				{
					// This must be an update to some random candle in the middle.
					// Just invalidate the cached values. Shouldn't ever happen really.
					m_total = null;
					m_newest = null;
					m_oldest = null;
					m_current = null;
					break;
				}
			}

			// Record the last time data was received
			LastUpdatedUTC = Model.UtcNow;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(update_type, this, new Range(Count-1, Count), candle));
		}

		/// <summary>Add a batch of candles</summary>
		private void Add(ETimeFrame tf, IEnumerable<Candle> candles)
		{
			// 'Upsert' the candles into the table
			using (var transaction = DB.BeginTransaction(IsolationLevel.Serializable))
			{
				foreach (var candle in candles)
					UpsertCandle(candle, transaction);
				
				transaction.Commit();
			}

			// Invalidate cached values
			m_total = null;
			m_newest = null;
			m_oldest = null;
			m_current = null;

			// Record the last time data was received
			LastUpdatedUTC = Model.UtcNow;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this));
		}

		/// <summary>Update or insert a candle</summary>
		private void UpsertCandle(Candle candle, IDbTransaction transaction = null)
		{
			if (candle.Timestamp <= Misc.CryptoCurrencyEpoch.Ticks)
				throw new Exception("Trying to insert an invalid candle");

			DB.Execute(
				$"insert or replace into {TimeFrame} (\n" +
				$"  [{nameof(Candle.Timestamp)}],\n" +
				$"  [{nameof(Candle.Open)}],\n" +
				$"  [{nameof(Candle.High)}],\n" +
				$"  [{nameof(Candle.Low)}],\n" +
				$"  [{nameof(Candle.Close)}],\n" +
				$"  [{nameof(Candle.Median)}],\n" +
				$"  [{nameof(Candle.Volume)}]\n" +
				$") values (\n" +
				$"  @time_stamp, @open, @high, @low, @close, @median, @volume\n" +
				$")\n",
				new
				{
					time_stamp = candle.Timestamp,
					open = candle.Open,
					high = candle.High,
					low = candle.Low,
					close = candle.Close,
					median = candle.Median,
					volume = candle.Volume,
				},
				transaction);
		}

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		protected virtual void OnDataChanged(DataEventArgs args)
		{
			DataChanged?.Invoke(this, args);
		}

		/// <summary>Indicates that the data is out of date and is being updated. False when UtcNow is within [Newest.Timestamp, Newest.Timestamp + TimeFrame)</summary>
		public bool DataSyncing => Newest == null || Model.UtcNow > Newest.TimestampEnd(TimeFrame);

		/// <summary>Raised when the state of the price data changes from up-to-date to syncing or visa versa</summary>
		public event EventHandler DataSyncingChanged;

		#endregion
	}

	#region EventArgs

	/// <summary>Event args for when data is changed</summary>
	public class DataEventArgs :EventArgs
	{
		public DataEventArgs(EUpdateType update_type, PriceData pd, Range index_range, Candle candle)
		{
			UpdateType = update_type;
			PriceData  = pd;
			IndexRange = index_range;
			Candle     = candle;
		}
		public DataEventArgs(PriceData pd, Range index_range)
			:this(EUpdateType.Range, pd, index_range, null)
		{}
		public DataEventArgs(PriceData pd)
			:this(pd, new Range(0, pd.Count))
		{}

		/// <summary>The type of update this event represents.</summary>
		public EUpdateType UpdateType { get; private set; }

		/// <summary>The instrument containing the changes</summary>
		public PriceData PriceData { get; private set; }

		/// <summary>The candle that was added to 'Table'</summary>
		public Candle Candle { get; private set; }

		/// <summary>The index range of candles that have changed</summary>
		public Range IndexRange { get; private set; }

		/// <summary>Update types</summary>
		public enum EUpdateType
		{
			/// <summary>This is an update to a range of candles</summary>
			Range,

			/// <summary>This is an update to the latest candle ('Candle')</summary>
			Current,

			/// <summary>'Candle' is a new candle and the previous candle as just closed</summary>
			New,
		};
	}

	#endregion
}



///// <summary>App logic</summary>
//public Model Model
//{
//	[DebuggerStepThrough] get { return m_model; }
//	private set
//	{
//		if (m_model == value) return;
//		if (m_model != null)
//		{
//			m_model.BackTestingChanging -= HandleBackTestingChanging;
//			m_model.SimReset -= HandleSimReset;
//			m_model.SimStep -= HandleSimStep;
//		}
//		m_model = value;
//		if (m_model != null)
//		{
//			m_model.SimStep += HandleSimStep;
//			m_model.SimReset += HandleSimReset;
//			m_model.BackTestingChanging += HandleBackTestingChanging;
//		}

//		// Handlers
//		void HandleBackTestingChanging(object sender, PrePostEventArgs e)
//		{
//			// If back testing is about to be enabled, turn off the update thread
//			if (!Model.BackTesting && e.Before)
//				UpdateThreadActive = false;

//			// If back testing is just been disabled, turn on the update thread
//			if (!Model.BackTesting && e.After)
//				UpdateThreadActive = m_ref.Count != 0;
//		}
//		void HandleSimReset(object sender, SimResetEventArgs e)
//		{
//			ValidateCurrent(e.StartTime);
//		}
//		void HandleSimStep(object sender, SimStepEventArgs e)
//		{
//			ValidateCurrent(e.Clock);
//		}
//		void ValidateCurrent(DateTimeOffset clock)
//		{
//			// Update 'm_current' for the new clock value
//			if (!clock.Ticks.Within(Current.Timestamp, Current.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame)))
//			{
//				m_current = 
//					clock.Ticks > Newest.Timestamp ? m_newest :
//					clock.Ticks < Oldest.Timestamp ? m_oldest :
//					null;
//			}
//		}
//	}
//}
//private Model m_model;
