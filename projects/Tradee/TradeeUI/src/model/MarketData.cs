using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;

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


			{//HACK
				Instruments.RemoveIf(x => x.SymbolCode == "Stairs");
				var instr = GetOrCreateInstrument("Stairs");
				var now = DateTime.UtcNow.Ticks;
				var min = Misc.TimeFrameToTicks(1.0, ETimeFrame.Min1);
				var hr  = Misc.TimeFrameToTicks(1.0, ETimeFrame.Hour1);
				var pip = 0.0001;
				for (int i = 0, j = 1; i != 100; ++i, ++j)
					instr.Add(ETimeFrame.Min1, new Candle(now + i*min, i * pip, j * pip,  i * pip, j * pip, 10));
				for (int i = 0, j = 1; i != 100; ++i, ++j)
					instr.Add(ETimeFrame.Hour1, new Candle(now + i*hr, i * pip, j * pip,  i * pip, j * pip, 10));
			}

		}
		public virtual void Dispose()
		{
			Instruments.Clear();
		}

		/// <summary>The App logic</summary>
		private MainModel Model
		{
			get;
			set;
		}

		/// <summary>Get/Create an instrument</summary>
		public Instrument this[string sym]
		{
			get { return GetOrCreateInstrument(sym); }
		}

		/// <summary>Get/Create an instrument named 'sym'</summary>
		public Instrument GetOrCreateInstrument(string sym)
		{
			// Return the existing instrument if it already exists
			var idx = Instruments.BinarySearch(x => x.SymbolCode.CompareTo(sym));
			if (idx >= 0)
				return Instruments[idx];

			// Otherwise create it
			return Instruments.Insert2(~idx, new Instrument(Model, sym));
		}

		/// <summary>Delete the database file associated with 'code'</summary>
		public void PurgeCachedInstrument(string code)
		{
			Instruments.RemoveIf(x => x.SymbolCode == code);
			var db_filepath = Instrument.CacheDBFilePath(Model.Settings, code);
			File.Delete(db_filepath);
		}

		/// <summary>The currency pairs (sorted alphabetically)</summary>
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
		public event EventHandler<DataEventArgs> DataAdded;

		/// <summary>Handle data changing in one of the instruments</summary>
		private void HandleInstrumentDataAdded(object sender, DataEventArgs args)
		{
			DataAdded.Raise(this, args);
		}

		/// <summary>Handle changes to the instruments collection</summary>
		private void HandleInstrumentListChanging(object sender, ListChgEventArgs<Instrument> e)
		{
			var instrument = e.Item;
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				OnInstrumentAdded(new DataEventArgs(instrument, null, false));
				instrument.DataAdded += HandleInstrumentDataAdded;
				break;

			case ListChg.ItemRemoved:
				OnInstrumentRemoved(new DataEventArgs(instrument, null, false));
				instrument.DataAdded -= HandleInstrumentDataAdded;
				instrument.Dispose();
				break;
			}
		}
	}

	/// <summary>Event args for when data is changed</summary>
	public class DataEventArgs :EventArgs
	{
		public DataEventArgs(Instrument instr, Candle candle, bool new_candle)
		{
			Instrument = instr;
			Candle     = candle;
			NewCandle  = new_candle;
		}

		/// <summary>The symbol that changed</summary>
		public string Symbol
		{
			get { return Instrument.SymbolCode; }
		}

		/// <summary>The time frame that changed</summary>
		public ETimeFrame TimeFrame
		{
			get { return Instrument.TimeFrame; }
		}

		/// <summary>The instrument containing the changes</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>The candle that was added to 'Table'</summary>
		public Candle Candle { get; private set; }

		/// <summary>True if 'Candle' is a new candle and the previous candle as just closed</summary>
		public bool NewCandle { get; private set; }
	}
}
