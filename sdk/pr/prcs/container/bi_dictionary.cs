using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pr.container
{
	/// <summary>Bi-directional dictionary</summary>
	public class BiDictionary<T0,T1>
	{
		private readonly Dictionary<T0, T1> m_forward = new Dictionary<T0, T1>();
		private readonly Dictionary<T1, T0> m_reverse = new Dictionary<T1, T0>();

		public BiDictionary()
		{
			m_forward = new Dictionary<T0, T1>();
			m_reverse = new Dictionary<T1, T0>();
		}
		public BiDictionary(IDictionary<T0,T1> dict)
		{
			m_forward = new Dictionary<T0, T1>(dict);
			m_reverse = m_forward.ToDictionary(k => k.Value, v => v.Key);
		}
		public BiDictionary(IDictionary<T1,T0> dict)
		{
			m_reverse = new Dictionary<T1, T0>(dict);
			m_forward = m_reverse.ToDictionary(k => k.Value, v => v.Key);
		}

		public int Count
		{
			get { return m_forward.Count; }
		}

		public Dictionary<T0,T1>.KeyCollection Values0
		{
			get { return m_forward.Keys; }
		}
		public Dictionary<T1,T0>.KeyCollection Values1
		{
			get { return m_reverse.Keys; }
		}

		public T1 this[T0 key]
		{
			get { return m_forward[key]; }
			set
			{
				m_forward[key] = value;
				m_reverse[value] = key;
			}
		}
		public T0 this[T1 key]
		{
			get { return m_reverse[key]; }
			set
			{
				m_reverse[key] = value;
				m_forward[value] = key;
			}
		}

		public bool ContainsKey(T0 key)
		{
			return m_forward.ContainsKey(key);
		}
		public bool ContainsKey(T1 key)
		{
			return m_reverse.ContainsKey(key);
		}

		public void Clear()
		{
			m_forward.Clear();
			m_reverse.Clear();
		}

		public void Add(T0 key, T1 value)
		{
			m_forward.Add(key, value);
			m_reverse.Add(value, key);
		}
		public void Add(T1 key, T0 value)
		{
			m_reverse.Add(key, value);
			m_forward.Add(value, key);
		}

		public bool Remove(T0 key)
		{
			T1 value;
			if (m_forward.TryGetValue(key, out value))
			{
				m_forward.Remove(key);
				m_reverse.Remove(value);
				return true;
			}
			return false;
		}
		public bool Remove(T1 key)
		{
			T0 value;
			if (m_reverse.TryGetValue(key, out value))
			{
				m_reverse.Remove(key);
				m_forward.Remove(value);
				return true;
			}
			return false;
		}

		public bool TryGetValue(T0 key, out T1 value)
		{
			return m_forward.TryGetValue(key, out value);
		}
		public bool TryGetValue(T1 key, out T0 value)
		{
			return m_reverse.TryGetValue(key, out value);
		}

		public Dictionary<T0,T1>.Enumerator GetEnumerator()
		{
			return m_forward.GetEnumerator();
		}
	}
}
