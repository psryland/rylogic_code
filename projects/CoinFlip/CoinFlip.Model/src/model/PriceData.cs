using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rylogic.Common;
using Rylogic.Db;
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
		// - It reads candle data from the exchange (or from the DB in back testing)
		//   and writes it into a DB.
		// - Use an 'Instrument' to view these data

		public PriceData(Model model, TradePair pair, ETimeFrame time_frame)
		{
			try
			{
				Model = model;
				Pair = pair;
				TimeFrame = time_frame;

				// Set up the ref count
				m_ref = new List<object>();
			
				// The database filepath
				DBFilepath = Misc.CandleDBFilePath(Pair.NameWithExchange);

				// Load the sqlite database of historic price data
				DB = new Sqlite.Database(DBFilepath);

				// Tweak some DB settings for performance
				DB.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
				DB.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

				// Ensure tables exist for each of the time frames.
				// Note: timestamp is not the row id. Row Ids should not have any
				// meaning. It's more efficient to let the DB do the sorting
				// and just use 'order by' in queries
				foreach (var tf in Enum<ETimeFrame>.Values.Except(ETimeFrame.None))
					DB.Execute(SqlExpr.CandleTable(tf));
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
			Pair = null;
			Model = null;
		}

		/// <summary>Currency pair name, e.g. ETHBTC</summary>
		public string SymbolCode
		{
			get { return Pair.Name; }
		}

		/// <summary>The instrument database file location.</summary>
		public readonly string DBFilepath; // Shared across threads

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

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.BackTestingChanging -= HandleBackTestingChanging;
					m_model.SimReset -= HandleSimReset;
					m_model.SimStep -= HandleSimStep;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SimStep += HandleSimStep;
					m_model.SimReset += HandleSimReset;
					m_model.BackTestingChanging += HandleBackTestingChanging;
				}

				// Handlers
				void HandleBackTestingChanging(object sender, PrePostEventArgs e)
				{
					// If back testing is about to be enabled, turn off the update thread
					if (!Model.BackTesting && e.Before)
						UpdateThreadActive = false;

					// If back testing is just been disabled, turn on the update thread
					if (!Model.BackTesting && e.After)
						UpdateThreadActive = m_ref.Count != 0;
				}
				void HandleSimReset(object sender, SimResetEventArgs e)
				{
					ValidateCurrent(e.StartTime);
				}
				void HandleSimStep(object sender, SimStepEventArgs e)
				{
					ValidateCurrent(e.Clock);
				}
				void ValidateCurrent(DateTimeOffset clock)
				{
					// Update 'm_current' for the new clock value
					if (!clock.Ticks.Within(Current.Timestamp, Current.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame)))
					{
						m_current = 
							clock.Ticks > Newest.Timestamp ? m_newest :
							clock.Ticks < Oldest.Timestamp ? m_oldest :
							null;
					}
				}
			}
		}
		private Model m_model;

		/// <summary>The pair we're collecting candle data for</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			private set
			{
				if (m_pair == value) return;
				if (m_pair != null)
				{
				}
				m_pair = value;
				if (m_pair != null)
				{
				}
			}
		}
		private TradePair m_pair;

		/// <summary>The time frame we're collecting candle data for</summary>
		public ETimeFrame TimeFrame
		{
			[DebuggerStepThrough] get { return m_time_frame; }
			private set
			{
				if (m_time_frame == value) return;
				m_time_frame = value;
			}
		}
		private ETimeFrame m_time_frame;

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

		/// <summary>The total number of candles in the database.</summary>
		public int Count
		{
			get
			{
				if (m_total == null)
				{
					if (TimeFrame == ETimeFrame.None) throw new Exception("Invalid time frame");
					var sql = $"select count(*) from {TimeFrame}";
					m_total = DB.ExecuteScalar(sql);
				}
				return m_total.Value;
			}
		}
		private int? m_total;

		/// <summary>The candle with the newest timestamp in the database</summary>
		public Candle Newest
		{
			get
			{
				if (m_newest == null)
				{
					var sql = $"select * from {TimeFrame} order by [{nameof(Candle.Timestamp)}] desc limit 1";
					m_newest = DB.EnumRows<Candle>(sql).FirstOrDefault();
				}
				return m_newest ?? Candle.Default;
			}
		}
		private Candle m_newest;

		/// <summary>The candle with the oldest timestamp in the database</summary>
		public Candle Oldest
		{
			get
			{
				if (m_oldest == null)
				{
					var sql = $"select * from {TimeFrame} order by [{nameof(Candle.Timestamp)}] asc limit 1";
					m_oldest = DB.EnumRows<Candle>(sql).FirstOrDefault();
				}
				return m_oldest ?? Candle.Default;
			}
		}
		private Candle m_oldest;

		/// <summary>The candle with the latest timestamp for the current time. Note: Current != Newest when back testing</summary>
		public Candle Current
		{
			get
			{
				if (m_current == null)
				{
					var sql = $"select * from {TimeFrame} where [{nameof(Candle.Timestamp)}] <= ? order by [{nameof(Candle.Timestamp)}] desc limit 1";
					m_current = DB.EnumRows<Candle>(sql, 1, new object[] { Model.UtcNow.Ticks }).FirstOrDefault();
				}
				if (m_current != null && Model.BackTesting)
				{
					// Interpolate the latest candle to determine the spot price
					var t = Math_.Frac(m_current.Timestamp, Model.UtcNow.Ticks, m_current.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame));
					return m_current.SubCandle(Math_.Clamp(t, 0.0, 1.0));
				}
				return m_current ?? Candle.Default;
			}
		}
		private Candle m_current;

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>String description of the instrument</summary>
		public string Description
		{
			get { return $"{Pair.NameWithExchange}"; }
		}

		#region Update

		/// <summary>The background thread for performing candle data updates</summary>
		public bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Model.AssertMainThread());

				// Don't allow the update thread to run during back testing
				if (value && Model.BackTesting)
					throw new Exception("Update thread should not be activated while back-testing");

				if (m_update_thread != null)
				{
					m_update_thread_exit.Set();
					m_update_thread_cancel.Cancel();
					if (m_update_thread.IsAlive)
						m_update_thread.Join();

					Util.Dispose(ref m_update_thread_exit);
					Util.Dispose(ref m_update_thread_cancel);
				}
				var parms = new UpdateThreadData
				{
					Pair = Pair,
					Exchange = Pair.Exchange,
					TimeFrame = TimeFrame,
					StartTimestamp = Count != 0 ? Newest.Timestamp : DateTimeOffset_.UnixEpoch.Ticks,
					UpdatePeriod = TimeSpan.FromMilliseconds(Model.Settings.PriceDataUpdatePeriodMS),
					Cancel = m_update_thread_cancel = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown.Token),
				};
				m_update_thread = value ? new Thread(() => UpdateThreadEntryPoint(parms)) : null;
				if (m_update_thread != null)
				{
					m_update_thread_exit = new ManualResetEvent(false);
					m_update_thread.Start();

					// Raise the data syncing event to set the initial state
					DataSyncingChanged?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Thread entry point for the instrument update thread</summary>
				void UpdateThreadEntryPoint(UpdateThreadData args)
				{
					try
					{
						Thread.CurrentThread.Name = $"{SymbolCode} PriceData Update";
						Model.Log.Write(ELogLevel.Debug, $"PriceData {SymbolCode} update thread started");

						var end = args.StartTimestamp;
						for (;;)
						{
							if (m_update_thread_exit.WaitOne(args.UpdatePeriod))
								break;

							// Query for chart data
							var data = args.Exchange.CandleData(args.Pair, args.TimeFrame, end, DateTimeOffset.Now.Ticks, args.Cancel.Token);
							if (data == null || data.Count == 0)
								continue;

							// Update the end time range to include the returned data
							end = data.Back().Timestamp;

							// Add the received data to the database
							Model.RunOnGuiThread(() =>
							{
								if (!UpdateThreadActive)
									return;

								var data_syncing = DataSyncing;
								if (data.Count == 1)
									Add(args.TimeFrame, data[0]);
								else
									Add(args.TimeFrame, data);

								// Raise the event if 'DataSyncing' has changed
								if (data_syncing != DataSyncing)
									DataSyncingChanged?.Invoke(this, EventArgs.Empty);
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
		private struct UpdateThreadData
		{
			public TradePair Pair;
			public Exchange Exchange;
			public ETimeFrame TimeFrame;
			public long StartTimestamp;
			public TimeSpan UpdatePeriod;
			public CancellationTokenSource Cancel;
		}
		private CancellationTokenSource m_update_thread_cancel;
		private ManualResetEvent m_update_thread_exit;
		private Thread m_update_thread;

		/// <summary>Add a candle value to the database</summary>
		private void Add(ETimeFrame tf, Candle candle)
		{
			// Sanity check
			Debug.Assert(candle.Valid());
			Debug.Assert(candle.Timestamp != 0);
			Debug.Assert(tf != ETimeFrame.None);
			Debug.Assert(tf == TimeFrame);
			if (Model.BackTesting)
				throw new Exception("Should not be added candles to the DB while back testing");

			// This is a new candle if it's time stamp is >= the TimeFrame period after the Newest candle.
			// This is an update to the latest candle if within a TimeFrame period of the Newest candle.
			var update_type =
				candle.Timestamp >= Newest.Timestamp + Misc.TimeFrameToTicks(1.0, TimeFrame) ? DataEventArgs.EUpdateType.New :
				candle.Timestamp >= Newest.Timestamp ? DataEventArgs.EUpdateType.Current :
				DataEventArgs.EUpdateType.Range;

			// Insert the candle into the database if is for 'TimeFrame' and the sim isn't running.
			DB.Execute(SqlExpr.InsertCandle(TimeFrame), 1, SqlExpr.InsertCandleParams(candle));

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
			// Insert the candles into the database
			using (var t = DB.NewTransaction())
			using (var query = new Sqlite.Query(DB, SqlExpr.InsertCandle(tf)))
			{
				foreach (var candle in candles)
				{
					query.Reset();
					query.BindParms(1, SqlExpr.InsertCandleParams(candle));
					query.Run();
				}
				t.Commit();
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

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		protected virtual void OnDataChanged(DataEventArgs args)
		{
			DataChanged?.Invoke(this, args);
		}

		/// <summary>Indicates that the data is out of date and is being updated. False when UtcNow is within [Newest.Timestamp, Newest.Timestamp + TimeFrame)</summary>
		public bool DataSyncing
		{
			get { return Newest == null || Model.UtcNow > Newest.TimestampEnd(TimeFrame); }
		}
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
