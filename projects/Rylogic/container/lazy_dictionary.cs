using System;
using System.Collections.Generic;
using pr.extn;

namespace pr.container
{
	/// <summary>A specialised dictionary that lazy-creates elements as accessed</summary>
	public class LazyDictionary<TKey,TValue> :Dictionary<TKey,TValue>
	{
		private readonly Func<TKey,TValue> m_factory;
		public LazyDictionary(Func<TKey,TValue> factory)
		{
			m_factory = factory ?? (k => Activator.CreateInstance<TValue>());
		}
		public new TValue this[TKey key]
		{
			get { return this.GetOrAdd(key, m_factory); }
			set { base[key] = value; }
		}
	}
}
