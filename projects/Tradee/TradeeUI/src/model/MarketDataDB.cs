using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.common;
using pr.container;
using pr.extn;

namespace Tradee
{
	/// <summary>A database of market data values</summary>
	public class MarketDataDB :SymbolMap<MarketDataDB.SymbolPriceTable> ,IDisposable
	{
		public MarketDataDB()
		{}
		public virtual void Dispose()
		{
		}

		/// <summary>Add a candle to the data</summary>
		public void Add(Candle candle)
		{
			var table = this[candle.Symbol];
			table.Add(candle);
		}
		public void Add(Candles candles)
		{
			foreach (var c in candles.AllCandles)
				Add(c);
		}

		/// <summary>An event raised when market data is added to a symbol table</summary>
		public event EventHandler<DataAddedEventArgs> DataAdded;

		/// <summary>Handle data changing in one of the symbol tables</summary>
		private void HandleTableDataAdded(object sender, DataAddedEventArgs args)
		{
			DataAdded.Raise(this, args);
		}

		/// <summary>Access the table for a symbol (lazy created)</summary>
		protected override SymbolPriceTable FactoryNew(string sym)
		{
			var table = new SymbolPriceTable(sym);
			table.DataAdded += HandleTableDataAdded;
			return table;
		}

		/// <summary>The data series for an instrument</summary>
		public class SymbolPriceTable :ISymbolData
		{
			private const int MaxLength = 100000;
			private const int ReduceSize = 1000;
			private List<Candle> m_data;

			public SymbolPriceTable(string name)
			{
				m_data = new List<Candle>();
				Symbol = name;
			}

			/// <summary>The instrument name (symbol name)</summary>
			public string Symbol { get; private set; }

			/// <summary>Add a price value to the table</summary>
			public void Add(Candle candle)
			{
				// Price data are stored in time order
				if (m_data.Count == 0 || candle.Timestamp > m_data.Back().Timestamp)
				{
					m_data.Add(candle);
				}
				// If this candle is an update to the last received candles, replace it
				else if (candle.Timestamp == m_data.Back().Timestamp)
				{
					m_data[m_data.Count - 1] = candle;
				}
				// Otherwise this is a historic candle, insert or replace
				else
				{
					Func<Candle,int> ByTime = x => x.Timestamp > candle.Timestamp ? +1 : x.Timestamp < candle.Timestamp ? -1 : 0;
					var idx = m_data.BinarySearch(ByTime);
					if (idx < 0)
						m_data.Insert(~idx, candle);
					else
						m_data[idx] = candle;
				}

				// Limit the length of the data buffer
				if (m_data.Count > MaxLength)
					m_data.RemoveRange(0, ReduceSize);

				// Notify data added
				DataAdded.Raise(this, new DataAddedEventArgs(this, candle));
			}

			/// <summary>Raised whenever data in this table is changed</summary>
			public event EventHandler<DataAddedEventArgs> DataAdded;
		}

		/// <summary>Event args for when data is changed</summary>
		public class DataAddedEventArgs :EventArgs
		{
			public DataAddedEventArgs(SymbolPriceTable table, Candle candle)
			{
				Table  = table;
				Candle = candle;
			}

			/// <summary>The symbol that changed</summary>
			public string Symbol { get { return Table.Symbol; } }

			/// <summary>The table containing the changes</summary>
			public SymbolPriceTable Table { get; private set; }

			/// <summary>The candle that was added to 'Table'</summary>
			public Candle Candle { get; private set; }
		}
	}
}
