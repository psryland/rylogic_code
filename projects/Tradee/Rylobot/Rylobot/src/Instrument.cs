using System;
using System.Diagnostics;
using cAlgo.API.Internals;
using pr.extn;
using pr.util;
using NegIdx = System.Int32;

namespace Rylobot
{
	public class Instrument :IDisposable
	{
		public Instrument(RylobotModel model)
		{
			Model = model;
			Settings = new InstrumentSettings(Util.ResolveAppDataPath("Rylogic", "Rylobot", ".\\Instruments\\{0}.xml".Fmt(SymbolCode)));
		}
		public void Dispose()
		{
			Settings = null;
			Model = null;
		}

		/// <summary>Settings for the current instrument</summary>
		public InstrumentSettings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The App logic</summary>
		public RylobotModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				//	m_model.ConnectionChanged -= HandleConnectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
				//	m_model.ConnectionChanged += HandleConnectionChanged;
				}
			}
		}
		private RylobotModel m_model;

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		private void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>The instrument data source</summary>
		private MarketSeries Data
		{
			get { return Model.Robot.MarketSeries; }
		}

		/// <summary>The instrument symbol code</summary>
		public string SymbolCode
		{
			get { return Data.SymbolCode; }
		}

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			get { return Data.OpenTime.Count; }
		}

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
			get
			{
				// CAlgo uses 0 = latest, Count = oldest
				return new Candle(
					Data.OpenTime  [-neg_idx].Ticks,
					Data.Open      [-neg_idx],
					Data.High      [-neg_idx],
					Data.Low       [-neg_idx],
					Data.Close     [-neg_idx],
					Data.Median    [-neg_idx],
					Data.TickVolume[-neg_idx]);
			}
		}

		/// <summary>The candle with the latest timestamp for the current time frame</summary>
		public Candle Latest
		{
			get
			{
				// Cache the latest candle so that we can detect a new candle starting.
				return m_latest ?? (m_latest = this[0]);
			}
		}
		private Candle m_latest;

		/// <summary>The candle with the oldest timestamp for the current time frame</summary>
		public Candle Oldest
		{
			get { return this[FirstIdx]; }
		}

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Called when the instrument data has been updated</summary>
		public void OnTick()
		{
			// Get the latest candle
			var candle = this[0];

			// Check if this is the start of a new candle
			var new_candle = candle.Timestamp > Latest.Timestamp;

			// Apply the new data to the latest candle
			Latest.Update(candle);

			// Record the last time data was received
			LastUpdatedUTC = DateTimeOffset.UtcNow;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this, candle, new_candle));
		}
	}

	#region EventArgs

	/// <summary>Event args for when data is changed</summary>
	public class DataEventArgs :EventArgs
	{
		public DataEventArgs(Instrument instr, Candle candle, bool new_candle)
		{
			Instrument = instr;
			Candle     = candle;
			NewCandle  = new_candle;
		}

		/// <summary>The instrument containing the changes</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>The candle that was added to 'Table'</summary>
		public Candle Candle { get; private set; }

		/// <summary>True if 'Candle' is a new candle and the previous candle as just closed</summary>
		public bool NewCandle { get; private set; }
	}

	#endregion
}
