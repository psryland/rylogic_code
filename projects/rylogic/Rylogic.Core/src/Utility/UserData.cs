using System;
using System.Collections;
using System.Collections.Generic;

namespace Rylogic.Utility
{
	/// <summary>An object for managing user data attached to an object</summary>
	public class UserData :IEnumerable<KeyValuePair<Guid,object?>>
	{
		/// <summary>A map from GUID to user data</summary>
		private Dictionary<Guid, object?> m_store;

		public UserData()
		{
			m_store = new Dictionary<Guid, object?>();
		}
		public UserData(UserData rhs)
			:this()
		{
			foreach (var pair in rhs.m_store)
				m_store.Add(pair.Key, pair.Value);
		}

		/// <summary>Access the user data by GUID. Returns null if not found</summary>
		public object? this[Guid id]
		{
			get => m_store.TryGetValue(id, out var value) ? value : null;
			set
			{
				if (m_store.ContainsKey(id))
					m_store[id] = value;
				else
					m_store.Add(id, value);
			}
		}

		/// <inheritdoc/>
		public IEnumerator<KeyValuePair<Guid, object?>> GetEnumerator() => m_store.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
