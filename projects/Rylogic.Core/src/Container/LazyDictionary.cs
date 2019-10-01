using System;
using System.Collections.Generic;

namespace Rylogic.Container
{
	/// <summary>A specialised dictionary that lazy-creates elements as accessed</summary>
	public class LazyDictionary<TKey,TValue> :Dictionary<TKey,TValue>
	{
		private readonly Func<TKey, TValue> m_factory;
		public LazyDictionary()
			: this(k => Activator.CreateInstance<TValue>())
		{ }
		public LazyDictionary(Func<TKey, TValue> factory)
		{
			m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}
		public new TValue this[TKey key]
		{
			get { return TryGetValue(key, out var value) ? value : (base[key] = m_factory(key)); }
			set { base[key] = value; }
		}
	}
}
