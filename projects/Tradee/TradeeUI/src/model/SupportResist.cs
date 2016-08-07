using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.container;
using pr.extn;
using pr.maths;

namespace Tradee
{
	/// <summary>Finds support and resistance levels in the data</summary>
	public class SupportResist :InstrumentMap<SupportResist.SnRLevels>, IDisposable
	{
		public SupportResist(MainModel model)
		{
			Data = model.MarketData;
		}
		public virtual void Dispose()
		{
			Data = null;
		}

		/// <summary>The market data</summary>
		public MarketData Data
		{
			get { return m_impl_data; }
			set
			{
				if (m_impl_data == value) return;
				if (m_impl_data != null)
				{
					m_impl_data.DataAdded -= HandleDataAdded;
				}
				m_impl_data = value;
				if (m_impl_data != null)
				{
					m_impl_data.DataAdded += HandleDataAdded;
				}
			}
		}
		private MarketData m_impl_data;

		/// <summary>Handle new market data arriving</summary>
		private void HandleDataAdded(object sender, DataEventArgs args)
		{
			var snr = this[args.Symbol, args.TimeFrame];
			snr.HandleDataAdded(args);
		}

		/// <summary>Create an instance of a support and resistance detector</summary>
		protected override SnRLevels FactoryNew(string sym, ETimeFrame tf)
		{
			return new SnRLevels(sym, tf);
		}

		/// <summary>Sorted list of price levels and number of orders at that level</summary>
		public class SnRLevels :IInstrument
		{
			private Candle m_last_candle;

			public SnRLevels(string sym, ETimeFrame tf)
			{
				Levels        = new List<Level>();
				m_last_candle = Candle.Default;
				Symbol        = sym;
				TimeFrame     = tf;
			}

			/// <summary>The symbol these levels belong to</summary>
			public string Symbol { get; private set; }

			/// <summary>The time frame these levels are based on</summary>
			public ETimeFrame TimeFrame { get; private set; }

			/// <summary>A sorted list of prices</summary>
			public List<Level> Levels { get; private set; }

			/// <summary>Handle data </summary>
			internal void HandleDataAdded(DataEventArgs args)
			{
				// Depending on the time-frame, 'candle' could be an updated to
				// the last received candle. We only want to add a level when the
				// candle has stopped changing.
				var candle = args.Candle;
				if (candle == null)
					return;

				// When the time stamp changes, commit the last candle
				if (m_last_candle.Timestamp != candle.Timestamp)
					AddCandle(m_last_candle);

				// Update to the new data
				m_last_candle = candle;
			}

			/// <summary>Add a candle to the levels</summary>
			private void AddCandle(Candle candle)
			{
				// Ignore the sentinel candle
				if (candle == Candle.Default)
					return;

				// Insert the level into the ordered list
				var lvl = new Level(candle);
				var idx = Levels.BinarySearch(x => x.Price.CompareTo(lvl.Price));
				if (idx < 0)
					Levels.Insert(~idx, lvl);
				else
					Levels[idx].Update(lvl);
			}
		}

		/// <summary>A single price level</summary>
		public class Level
		{
			public Level(Candle candle)
			{
				// Get the median price
				const double scale = 10000.0;
				Price  = (int)(scale * (candle.High + candle.Low)) / (2.0 * scale);
				Volume = candle.Volume;
			}

			/// <summary>The price level</summary>
			public double Price { get; private set; }

			/// <summary>The number of orders at this price level</summary>
			public double Volume { get; private set; }

			/// <summary>Accumulate data from another level</summary>
			public void Update(Level rhs)
			{
				Debug.Assert(Price == rhs.Price);
				Volume = rhs.Volume;
			}
		}
	}
}
