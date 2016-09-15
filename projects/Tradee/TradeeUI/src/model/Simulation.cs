using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;
using pr.util;

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

		private Random m_rng;

		public Simulation(MainModel model)
		{
			Model = model;

			m_rng        = new Random();
			Acct         = new Account();
			Positions    = new List<Position>();
			Pending      = new List<PendingOrder>();
			Transmitters = new Dictionary<string, Transmitter>();

			StartTime = DateTimeOffset.UtcNow.Date.As(DateTimeKind.Utc);
			StartingBalance = 1000.0;
			StepSize = Misc.TimeFrameToTimeSpan(1.0, Settings.General.DefaultTimeFrame);
			StepRate = 1.0;

			Reset();
		}
		public virtual void Dispose()
		{
			Util.DisposeAll(Transmitters.Values);
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
		private Dictionary<string, Transmitter> Transmitters
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
			}
		}
		private bool m_running;

		/// <summary>Raised whenever the simulation time changes</summary>
		public event EventHandler SimTimeChanged;
		private void OnSimTimeChanged()
		{
			SimTimeChanged.Raise(this);
		}

		/// <summary>Raised whenever the sim is reset</summary>
		public event EventHandler SimReset;
		private void OnSimReset()
		{
			// Invalidate all instruments
			foreach (var instr in Model.MarketData.Instruments)
				instr.InvalidateCachedData();

			// Invalidate all charts
			foreach (var chart in Model.Charts)
			{
				chart.InvalidateCachedGraphics();
				chart.EnsureLatestPriceDisplayed();
			}

			SimReset.Raise(this);
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
				Reset();
			}
		}
		private DateTimeOffset m_start_time;

		/// <summary>How far to advance the simulation with each step</summary>
		public TimeSpan StepSize
		{
			get;
			set;
		}

		/// <summary>The number of steps per second</summary>
		public double StepRate
		{
			get;
			set;
		}

		/// <summary>Reset the simulation back to the start time</summary>
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

			// Update the simulated account status
			SendAccountStatus();
			SendCurrentPositions();
			SendPendingPositions();

			// Notify reset
			OnSimReset();
		}

		/// <summary>Step the simulation backwards by 'count' step sizes</summary>
		public void StepBack(int count)
		{
			// Go back one extra step, then step forward at the end
			++count;
			for (; count-- != 0; )
				UtcNow -= StepSize;

			// Remove any positions newer than 'UtcNow'
			Positions.RemoveIf(x => x.EntryTime > UtcNow.Ticks);

			// Invalidate all instruments
			foreach (var instr in Model.MarketData.Instruments)
				instr.InvalidateCachedData();

			Step();
		}

		/// <summary>Step the simulation by one 'StepSize'</summary>
		public void Step()
		{
			var time0 = UtcNow;
			var time1 = UtcNow + StepSize;

			// Collect the candle data to send from each transmitter over the step period
			var data = Transmitters.SelectMany(x => x.Value.EnumCandleData(time0, time1)).OrderBy(x => x.Candle.Timestamp);

			// "Post" the candle data to tradee
			foreach (var d in data)
			{
				var candle = d.Candle as Candle;

				// Fake the price data using the candle data
				var trans = Transmitters[d.SymbolCode];
				foreach (var price in new[] { candle.Open, candle.High, candle.Low, candle.Median, candle.Close })
				{
					var pd = new PriceData(trans.PriceData)
					{
						AskPrice = price,
						BidPrice = price + trans.PriceData.AvrSpread,
					};
					Model.DispatchMsg(new InMsg.SymbolData(trans.SymbolCode, pd));
				}

				// Send the candle
				Model.DispatchMsg(d);
			}

			UtcNow = time1;
		}

		/// <summary>
		/// Pretends to send a message over the pipe to the trade data source.
		/// Actually just handles the message in the simulation.</summary>
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
			case EMsgType.PlaceMarketOrder:         HandleMsg((OutMsg.PlaceMarketOrder        )msg); break;
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
			SendAccountStatus();

			// Update the current and pending trades
			SendCurrentPositions();
			SendPendingPositions();
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestInstrument msg)
		{
			// Get or create a transmitter for the requested instrument
			var trans = Transmitters.GetOrAdd(msg.SymbolCode, k => new Transmitter(msg.SymbolCode, Instrument.CacheDBFilePath(Settings, msg.SymbolCode)));

			// Add the requested time frame
			trans.TimeFrames = trans.TimeFrames.Concat(msg.TimeFrame).Distinct().ToArray();
		}

		/// <summary>Handle the hello message</summary>
		private void HandleMsg(OutMsg.RequestInstrumentStop msg)
		{
			// Remove the transmitter for the requested instrument
			Transmitter trans;
			if (!Transmitters.TryGetValue(msg.SymbolCode, out trans))
				return;

			// Remove the unwanted time frame
			if (msg.TimeFrame != ETimeFrame.None)
				trans.TimeFrames = trans.TimeFrames.Except(msg.TimeFrame).ToArray();
			else
				trans.TimeFrames = new ETimeFrame[0];

			// Remove transmitters containing no time frames
			if (trans.TimeFrames.Length == 0)
			{
				Transmitters.Remove(msg.SymbolCode);
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

		/// <summary>Handle a request to place a market order</summary>
		private void HandleMsg(OutMsg.PlaceMarketOrder msg)
		{
			var order = msg.Order;

			// Find the transmitter of the instrument that the order is for
			Transmitter trans;
			if (!Transmitters.TryGetValue(order.SymbolCode, out trans))
			{
				Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.Failed));
				return;
			}

			// Check the volume is valid
			if (order.Volume < trans.PriceData.VolumeMin ||
				order.Volume > trans.PriceData.VolumeMax ||
				order.Volume % trans.PriceData.VolumeStep != 0)
			{
				Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.InvalidVolume));
				return;
			}

			// Place an immediate order
			if (msg.Position != null)
			{
				var res = ActionTrade(msg.Position);
				if (res != EErrorCode.NoError)
				{
					Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, res));
					return;
				}
			}

			// Place a limit order
			else if (msg.PendingLimit != null)
			{
				// Limit orders are rejected if the price is not on the profit side of the order
				if (msg.PendingLimit.TradeType == ETradeType.Long && trans.PriceData.AskPrice < msg.PendingLimit.EntryPrice)
				{
					Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.Failed));
					return;
				}
				if (msg.PendingLimit.TradeType == ETradeType.Short && trans.PriceData.BidPrice > msg.PendingLimit.EntryPrice)
				{
					Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.Failed));
					return;
				}

				// Add the pending order
				Pending.Add(msg.PendingLimit);
			}

			// Place a stop order
			else if (msg.PendingStop != null)
			{
				// Stop orders are rejected if the price is not on the loss side of the order
				if (msg.PendingStop.TradeType == ETradeType.Long && trans.PriceData.AskPrice > msg.PendingStop.EntryPrice)
				{
					Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.Failed));
					return;
				}
				if (msg.PendingStop.TradeType == ETradeType.Short && trans.PriceData.BidPrice < msg.PendingStop.EntryPrice)
				{
					Model.DispatchMsg(new InMsg.MarketOrderChangeResult(false, EErrorCode.Failed));
					return;
				}

				// Add the pending order
				Pending.Add(msg.PendingStop);
			}

			// Send an update of the current/pending positions
			SendCurrentPositions();
			SendPendingPositions();
		}

		/// <summary>Send details of the current account</summary>
		private void SendAccountStatus()
		{
			// Only send differences
			if (Acct.Equals(m_last_account))
				return;

			// Send the account status to Tradee
			Model.DispatchMsg(new InMsg.AccountUpdate(Acct));
			m_last_account = new Account(Acct);
		}
		private Account m_last_account = new Account();

		/// <summary>Send details about the currently active positions held</summary>
		private void SendCurrentPositions()
		{
			// Only send differences
			if (Positions.SequenceEqual(m_last_positions))
				return;

			// Send Update
			var list = Positions.ToArray();
			if (Post(new InMsg.PositionsUpdate(list)))
				m_last_positions = list;
		}
		private Position[] m_last_positions = new Position[0];

		/// <summary>Send details about pending orders</summary>
		private void SendPendingPositions()
		{
			// Only send differences
			if (Pending.SequenceEqual(m_last_pending_orders))
				return;

			// Post update
			var list = Pending.ToArray();
			if (Post(new InMsg.PendingOrdersUpdate(list)))
				m_last_pending_orders = list;
		}
		private PendingOrder[] m_last_pending_orders = new PendingOrder[0];

		/// <summary>Make 'position' active</summary>
		private EErrorCode ActionTrade(Position position)
		{
			// Check the funds are available
			if (position.Volume / Acct.Leverage > Acct.Balance)
				return EErrorCode.InsufficientFunds;

			Positions.Add(position);
			return EErrorCode.NoError;
		}

		/// <summary>Equivalent of the TradeeBot Transmitter</summary>
		private class Transmitter :IDisposable
		{
			private Sqlite.Database m_db;
			public Transmitter(string sym, string db_filepath)
			{
				SymbolCode = sym;
				TimeFrames = new ETimeFrame[0];

				// Load the instrument candle database
				m_db = new Sqlite.Database(db_filepath, Sqlite.OpenFlags.ReadOnly);

				// Read the price data
				PriceData = m_db.EnumRows<PriceData>("select * from PriceData").First();
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

			/// <summary>Instrument price data</summary>
			public PriceData PriceData
			{
				get;
				private set;
			}

			/// <summary>Return all the candles in the given time range</summary>
			public IEnumerable<InMsg.CandleData> EnumCandleData(DateTimeOffset t0, DateTimeOffset t1)
			{
				var ts = "["+nameof(Candle.Timestamp)+"]";
				var sql = Str.Build("select * from {0} where ",ts," >= ? and ",ts," <= ? order by ",ts);
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
