using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class PairCollection :IEnumerable<TradePair>, IList, INotifyCollectionChanged
	{
		// Notes:
		//  - Don't allow remove from this collection, other objects hold references to
		//    pair instances. Pairs should live for the life of the application.

		private readonly BindingDict<string, TradePair> m_pairs;
		public PairCollection(Exchange exch)
		{
			m_pairs = new CollectionBase<string, TradePair>(exch) { KeyFrom = x => x.UniqueKey };
			Exchange = exch;
			Updated = new ConditionVariable<DateTimeOffset>(DateTimeOffset.MinValue);
		}

		/// <summary>The exchange that offers these trading pairs</summary>
		public Exchange Exchange { get; }

		/// <summary>Wait-able object for update notification</summary>
		public ConditionVariable<DateTimeOffset> Updated { get; }

		/// <summary>The time when data in this collection was last updated. Note: *NOT* when collection changed, when the elements in the collection changed</summary>
		public DateTimeOffset LastUpdated
		{
			get { return m_last_updated; }
			set
			{
				m_last_updated = value;
				Updated.NotifyAll(value);
			}
		}
		private DateTimeOffset m_last_updated;

		/// <summary></summary>
		public int Count => m_pairs.Count;

		/// <summary>True if 'key' is in this collection</summary>
		public bool ContainsKey(string key)
		{
			return m_pairs.ContainsKey(key);
		}

		/// <summary>Add a pair to this collection</summary>
		public TradePair Add(TradePair pair)
		{
			if (pair.Base.Symbol == "USDC" && pair.Quote.Symbol == "BTC")
				Debugger.Break();

			return m_pairs.Add2(pair);
		}

		/// <summary>Get or add the pair associated with the given symbols on this exchange</summary>
		public TradePair GetOrAdd(string @base, string quote, int? trade_pair_id = null)
		{
			var coinB = Exchange.Coins.GetOrAdd(@base);
			var coinQ = Exchange.Coins.GetOrAdd(quote);
			return this[@base, quote] ?? Add(new TradePair(coinB, coinQ, Exchange, trade_pair_id));
		}

		/// <summary>Get/Set the pair</summary>
		public TradePair this[string key]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return m_pairs.TryGetValue(key, out var pair) ? pair : null;
			}
		}

		/// <summary>Return a pair involving the given symbols (in either order)</summary>
		public TradePair this[string sym0, string sym1]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				if (m_pairs.TryGetValue(TradePair.MakeKey(sym0, sym1), out var pair0)) return pair0;
				if (m_pairs.TryGetValue(TradePair.MakeKey(sym1, sym0), out var pair1)) return pair1;
				return null;
			}
		}

		/// <summary>Return a pair involving the given symbol and the two exchanges (in either order)</summary>
		public TradePair this[string sym, Exchange exch0, Exchange exch1]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				if (m_pairs.TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out var pair0)) return pair0;
				if (m_pairs.TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out var pair1)) return pair1;
				return null;
			}
		}

		/// <summary></summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add => m_pairs.CollectionChanged += value;
			remove => m_pairs.CollectionChanged -= value;
		}

		#region IList
		public bool IsReadOnly => true;
		public bool IsFixedSize => ((IList)m_pairs).IsFixedSize;
		public object SyncRoot => ((IList)m_pairs).SyncRoot;
		public bool IsSynchronized => ((IList)m_pairs).IsSynchronized;
		public object this[int index]
		{
			get => ((IList)m_pairs)[index];
			set => throw new NotSupportedException();
		}
		#endregion

		#region IEnumerable
		public IEnumerator<TradePair> GetEnumerator()
		{
			return m_pairs.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Add(object value)
		{
			return ((IList)m_pairs).Add(value);
		}

		public bool Contains(object value)
		{
			return ((IList)m_pairs).Contains(value);
		}

		public void Clear()
		{
			m_pairs.Clear();
		}

		public int IndexOf(object value)
		{
			return ((IList)m_pairs).IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			((IList)m_pairs).Insert(index, value);
		}

		public void Remove(object value)
		{
			((IList)m_pairs).Remove(value);
		}

		public void RemoveAt(int index)
		{
			m_pairs.RemoveAt(index);
		}

		public void CopyTo(Array array, int index)
		{
			((IList)m_pairs).CopyTo(array, index);
		}
		#endregion
	}
}
