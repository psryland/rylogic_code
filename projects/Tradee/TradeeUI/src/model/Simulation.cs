using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;
using pr.util;

'get the simulation updating the charts in real time
'Write AutoTrade to do simple candle follow

namespace Tradee
{
	public class Simulation :IDisposable
	{
		// The simulation acts as the trade data source, handling orders and price data updates.
		// It replays the stream of price data starting at a point in time. The rest of Tradee
		// should be basically unaware of the simulation.
		// The simulation does not apply to a single instrument, but all available instruments.
		// It emulates the Tradee_StreamData bot.
		// This is so auto trading can work on multiple instruments at a time.

		public Simulation(MainModel model)
		{
			Model = model;

			Acct = new Account();
			Positions = new List<Position>();
			Pending = new List<PendingOrder>();
			Transmitters = new List<Transmitter>();

			StartingBalance = 1000.0;

			StartTime = DateTimeOffset.UtcNow.Date.As(DateTimeKind.Utc);
			UtcNow = StartTime;

			Reset();
		}
		public virtual void Dispose()
		{
			Util.DisposeAll(Transmitters);
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The simulation account</summary>
		public Account Acct
		{
			get;
			private set;
		}

		/// <summary>The simulation account active positions</summary>
		public List<Position> Positions
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The simulation account active positions</summary>
		public List<PendingOrder> Pending
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Instrument transmitters</summary>
		private List<Transmitter> Transmitters
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>True if the simulation is running</summary>
		public bool Running
		{
			[DebuggerStepThrough] get { return m_running; }
			set
			{
				if (m_running == value) return;
				m_running = value;
				UtcNow = StartTime;
			}
		}
		private bool m_running;

		/// <summary>Raised whenever the simulation time changes</summary>
		public event EventHandler SimTimeChanged;
		private void OnSimTimeChanged()
		{
			SimTimeChanged.Raise(this);
		}

		/// <summary>The account balance at the start of the simulation</summary>
		public double StartingBalance
		{
			get;
			set;
		}

		/// <summary>"Now" according to the simulation</summary>
		public DateTimeOffset UtcNow
		{
			get { return m_now; }
			private set
			{
				if (m_now == value) return;
				m_now = value;
				OnSimTimeChanged();
			}
		}
		private DateTimeOffset m_now;

		/// <summary>The time that the simulation starts from</summary>
		public DateTimeOffset StartTime
		{
			get { return m_start_time; }
			set
			{
				if (m_start_time == value) return;
				m_start_time = value;

				// If the sim isn't running, update 'now' to the sim start time
				if (!Running)
					UtcNow = m_start_time;
			}
		}
		private DateTimeOffset m_start_time;

		/// <summary>How far to advance the simulation with each step</summary>
		public TimeSpan StepSize
		{
			get;
			set;
		}

		/// <summary>Reset the simulation but to the start time</summary>
		public void Reset()
		{
			// Reset the account
			Acct.AccountId             = "Simulation";
			Acct.BrokerName            = "Rylogic Ltd";
			Acct.Currency              = "Spon";
			Acct.Balance               = StartingBalance;
			Acct.Equity                = StartingBalance;
			Acct.FreeMargin            = 100.0;
			Acct.IsLive                = false;
			Acct.Leverage              = 500;
			Acct.Margin                = 100.0;
			Acct.UnrealizedGrossProfit = 0.0;
			Acct.UnrealizedNetProfit   = 0.0;

			// Reset trades
			Positions.Clear();
			Pending.Clear();

			// Reset the sim clock
			UtcNow = StartTime;
		}

		/// <summary>Step the simulation by one 'StepSize'</summary>
		public void Step()
		{
			var time0 = UtcNow;
			var time1 = UtcNow + StepSize;

			// Collect the candle data to send from each transmitter over the step period
			var data = Transmitters.SelectMany(x => x.EnumCandleData(time0, time1)).OrderBy(x => x.Candle.Timestamp);

			// "Post" the candle data to tradee
			foreach (var d in data)
			{
				UtcNow = new DateTimeOffset(d.Candle.Timestamp, TimeSpan.Zero);
				Model.DispatchMsg(d);
			}

			UtcNow = time1;
		}

		/// <summary>Send a message over the pipe</summary>
		public bool Post<T>(T m) where T:ITradeeMsg
		{
			var msg = (ITradeeMsg)m;
			switch (msg.ToMsgType())
			{
			case EMsgType.HelloClient:              HandleMsg((OutMsg.HelloClient             )msg); break;
			case EMsgType.RequestAccountStatus:     HandleMsg((OutMsg.RequestAccountStatus    )msg); break;
			case EMsgType.RequestInstrument:        HandleMsg((OutMsg.RequestInstrument       )msg); break;
			case EMsgType.RequestInstrumentStop:    HandleMsg((OutMsg.RequestInstrumentStop   )msg); break;
			case EMsgType.RequestInstrumentHistory: HandleMsg((OutMsg.RequestInstrumentHistory)msg); break;
			case EMsgType.RequestTradeHistory:      HandleMsg((OutMsg.RequestTradeHistory     )msg); break;
			}
			return true;
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.HelloClient msg)
		{
		}

		/// <summary>Update the account status</summary>
		private void HandleMsg(OutMsg.RequestAccountStatus req)
		{
			// Update the account status
			Model.DispatchMsg(new InMsg.AccountUpdate(Acct));

			// Update the current and pending trades
			Model.DispatchMsg(new InMsg.PositionsUpdate(Positions.ToArray()));
			Model.DispatchMsg(new InMsg.PendingOrdersUpdate(Pending.ToArray()));
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestInstrument msg)
		{
			// Get or create a transmitter for the requested instrument
			var idx = Transmitters.IndexOf(x => x.SymbolCode == msg.SymbolCode);
			var trans = idx >= 0 ? Transmitters[idx] : Transmitters.Add2(new Transmitter(msg.SymbolCode, Instrument.CacheDBFilePath(Settings, msg.SymbolCode)));

			// Add the requested time frame
			trans.TimeFrames = trans.TimeFrames.Concat(msg.TimeFrame).ToArray();
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestInstrumentStop msg)
		{
			// Remove the transmitter for the requested instrument
			var trans = Transmitters.FirstOrDefault(x => x.SymbolCode == msg.SymbolCode);
			if (trans == null)
				return;

			// Remove the unwanted time frame
			if (msg.TimeFrame != ETimeFrame.None)
				trans.TimeFrames = trans.TimeFrames.Except(msg.TimeFrame).ToArray();
			else
				trans.TimeFrames = new ETimeFrame[0];

			// Remove transmitters containing no time frames
			if (trans.TimeFrames.Length == 0)
			{
				Transmitters.Remove(trans);
				trans.Dispose();
			}
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestInstrumentHistory msg)
		{
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestTradeHistory msg)
		{
		}

		/// <summary>Equivalent of the TradeeBot Transmitter</summary>
		private class Transmitter :IDisposable
		{
			private Sqlite.Database m_db;
			public Transmitter(string sym, string db_filepath)
			{
				SymbolCode = sym;
				TimeFrames = new ETimeFrame[0];

				// Load the price data database
				m_db = new Sqlite.Database(db_filepath, Sqlite.OpenFlags.ReadOnly);

			}
			public void Dispose()
			{
				Util.Dispose(ref m_db);
			}

			/// <summary>The symbol code of the instrument being transmitted</summary>
			public string SymbolCode
			{
				get;
				private set;
			}

			/// <summary>The time frames to send data for</summary>
			public ETimeFrame[] TimeFrames
			{
				get;
				set;
			}

			/// <summary>Return all the candles in the given time range</summary>
			public IEnumerable<InMsg.CandleData> EnumCandleData(DateTimeOffset t0, DateTimeOffset t1)
			{
				var ts = "["+nameof(Candle.Timestamp)+"]";
				var sql = Str.Build("select * from {0} where ",ts," >= ? && ",ts," <= ? order by ",ts);
				var args = new object[] { t0.Ticks, t1.Ticks };
				foreach (var tf in TimeFrames)
				{
					foreach (var candle in m_db.EnumRows<Candle>(sql.Fmt(tf), 1, args))
						yield return new InMsg.CandleData(SymbolCode, tf, candle);
				}
			}
		}
	}
}
