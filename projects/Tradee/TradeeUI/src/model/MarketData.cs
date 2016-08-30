using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using pr.common;
using pr.container;
using pr.extn;

namespace Tradee
{
	/// <summary>A database of market data values</summary>
	public class MarketData :IDisposable
	{
		// Market data contains a set of 'Instruments' (currency pairs).
		// Each pair has a separate database containing tables of candles for
		// each time frame

		public MarketData(MainModel model)
		{
			Model = model;
			Instruments = new BindingSource<Instrument> { DataSource = new BindingListEx<Instrument>(), PerItemClear = true };

			// Ensure the price data cache directory exists
			if (!Path_.DirExists(Model.Settings.General.PriceDataCacheDir))
				Directory.CreateDirectory(Model.Settings.General.PriceDataCacheDir);

			// Add instruments for the price data we know about
			const string price_data_file_pattern = @"PriceData_(\w+)\.db";
			foreach (var fd in Path_.EnumFileSystem(Model.Settings.General.PriceDataCacheDir, regex_filter:price_data_file_pattern))
			{
				var sym = fd.FileName.SubstringRegex(price_data_file_pattern, RegexOptions.IgnoreCase)[0];
				GetOrCreateInstrument(sym);
			}
		}
		public virtual void Dispose()
		{
			Instruments.Clear();
			Model = null;
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

		/// <summary>Get/Create an instrument</summary>
		public Instrument this[string sym]
		{
			get { return GetOrCreateInstrument(sym); }
		}

		/// <summary>Get/Create an instrument named 'sym'</summary>
		public Instrument GetOrCreateInstrument(string sym)
		{
			// Don't request data for this instrument yet.
			// The caller should add a reference to the instrument which will start the data flowing
			var idx = Instruments.IndexOf(x => x.SymbolCode == sym);
			var instr = idx >= 0 ? Instruments[idx] : Instruments.Add2(new Instrument(Model, sym));
			return instr;
		}

		/// <summary>Delete the database file associated with 'code'</summary>
		public void PurgeCachedInstrument(string code)
		{
			Instruments.RemoveIf(x => x.SymbolCode == code);
			var db_filepath = Instrument.CacheDBFilePath(Model.Settings, code);
			File.Delete(db_filepath);
		}

		/// <summary>Raise 'DataChanged' on all instruments</summary>
		public void NotifyAllInstrumentsChanged()
		{
			foreach (var instr in Instruments)
				instr.RaiseDataChanged();
		}

		/// <summary>The currency pairs</summary>
		public BindingSource<Instrument> Instruments
		{
			[DebuggerStepThrough] get { return m_bs_instruments; }
			private set
			{
				if (m_bs_instruments == value) return;
				if (m_bs_instruments != null)
				{
					m_bs_instruments.ListChanging -= HandleInstrumentListChanging;
				}
				m_bs_instruments = value;
				if (m_bs_instruments != null)
				{
					m_bs_instruments.ListChanging += HandleInstrumentListChanging;
				}
			}
		}
		private BindingSource<Instrument> m_bs_instruments;

		/// <summary>An event raised when a new instrument is added</summary>
		public event EventHandler<DataEventArgs> InstrumentAdded;
		protected virtual void OnInstrumentAdded(DataEventArgs args)
		{
			InstrumentAdded.Raise(this, args);
		}

		/// <summary>An event raised when an instrument is removed</summary>
		public event EventHandler<DataEventArgs> InstrumentRemoved;
		protected virtual void OnInstrumentRemoved(DataEventArgs args)
		{
			InstrumentRemoved.Raise(this, args);
		}

		/// <summary>An event raised when data is added to any instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		public void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>Handle the connection to the trade data source changing</summary>
		private void HandleConnectionChanged(object sender, EventArgs e)
		{
		}

		/// <summary>Handle data changing in one of the instruments</summary>
		private void HandleInstrumentDataChanged(object sender, DataEventArgs args)
		{
			OnDataChanged(args);
		}

		/// <summary>Handle changes to the instruments collection</summary>
		private void HandleInstrumentListChanging(object sender, ListChgEventArgs<Instrument> e)
		{
			var instrument = e.Item;
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				OnInstrumentAdded(new DataEventArgs(instrument, ETimeFrame.None, null, false));
				instrument.DataChanged += HandleInstrumentDataChanged;
				break;

			case ListChg.ItemRemoved:
				OnInstrumentRemoved(new DataEventArgs(instrument, ETimeFrame.None, null, false));
				instrument.DataChanged -= HandleInstrumentDataChanged;
				instrument.Dispose();
				break;
			}
		}
	}

	#region EventArgs
	/// <summary>Event args for when data is changed</summary>
	public class DataEventArgs :EventArgs
	{
		public DataEventArgs(Instrument instr, ETimeFrame tf, Candle candle, bool new_candle)
		{
			Instrument = instr;
			TimeFrame  = tf;
			Candle     = candle;
			NewCandle  = new_candle;
		}

		/// <summary>The symbol that changed</summary>
		public string SymbolCode
		{
			get { return Instrument?.SymbolCode ?? string.Empty; }
		}

		/// <summary>The time frame that changed</summary>
		public ETimeFrame TimeFrame { get; private set; }

		/// <summary>The instrument containing the changes</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>The candle that was added to 'Table'</summary>
		public Candle Candle { get; private set; }

		/// <summary>True if 'Candle' is a new candle and the previous candle as just closed</summary>
		public bool NewCandle { get; private set; }
	}
	#endregion
}
