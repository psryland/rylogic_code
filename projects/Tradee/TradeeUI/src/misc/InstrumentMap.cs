using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;

namespace Tradee
{
	/// <summary>A data type associated with a single instrument at a time frame</summary>
	public interface IInstrument
	{
		/// <summary>The string name of the instrument</summary>
		string Symbol { get; }

		/// <summary>The time frame that the data is quantised to</summary>
		ETimeFrame TimeFrame { get; }
	}

	/// <summary>A map that lazily creates entries on request</summary>
	public abstract class InstrumentMap<TInstrument> where TInstrument :class, IInstrument
	{
		/// <summary>A list sorted by symbol, then by time frame, of the instruments</summary>
		private List<TInstrument> m_instruments;
		 
		protected InstrumentMap()
		{
			m_instruments = new List<TInstrument>();
		}

		/// <summary>Get/Create the 'instrument' associated with 'sym' and 'tf'</summary>
		public TInstrument this[string sym, ETimeFrame tf]
		{
			get
			{
				Debug.Assert(tf != ETimeFrame.None);
				var idx = m_instruments.BinarySearch(x => CompareTo(x, sym, tf));
				return idx >= 0 ? m_instruments[idx] : m_instruments.Insert2(~idx, FactoryNew(sym, tf));
			}
		}

		/// <summary>Enumerate the unique symbols in this set</summary>
		public IEnumerable<string> Symbols
		{
			get
			{
				var sym = string.Empty;
				foreach (var instr in m_instruments)
				{
					if (instr.Symbol == sym) continue;
					sym = instr.Symbol;
					yield return sym;
				}
			}
		}

		/// <summary>All instruments</summary>
		public IReadOnlyList<TInstrument> Instruments()
		{
			return m_instruments;
		}

		/// <summary>Enumerate the available instruments matching the symbol name 'sym'</summary>
		public IEnumerable<TInstrument> Instruments(string sym)
		{
			var idx = m_instruments.BinarySearch(x => CompareTo(x, sym, ETimeFrame.None)); Debug.Assert(idx < 0);
			for (idx = ~idx; idx < m_instruments.Count && m_instruments[idx].Symbol == sym; ++idx)
				yield return m_instruments[idx];
		}

		/// <summary>Return the first instrument to match 'sym' and the given time-frames in priority order</summary>
		public TInstrument Instrument(string sym, params ETimeFrame[] time_frames)
		{
			var instruments  = Instruments(sym).ToArray();
			var available_tf = instruments.ToHashSet(x => x.TimeFrame);
			var tf           = time_frames.FirstOrDefault(x => available_tf.Contains(x));
			return tf != ETimeFrame.None ? this[sym, tf] : null;
		}

		/// <summary>Create a new instrument for 'sym' and 'tf'</summary>
		protected abstract TInstrument FactoryNew(string sym, ETimeFrame tf);

		/// <summary>Sorting predicate used to order the list</summary>
		private int CompareTo(TInstrument x, string sym, ETimeFrame tf)
		{
			int i;
			if ((i = x.Symbol.CompareTo(sym)) != 0) return i;
			return x.TimeFrame.CompareTo(tf);
		}
	}
}
