using System;
using System.Collections.Generic;

namespace pr.util
{
	/// <summary>An object for managing user data attached to an object</summary>
	public class UserData
	{
		/// <summary>A map from GUID to user data</summary>
		private Dictionary<Guid, object> m_store;

		public UserData()
		{
			m_store = new Dictionary<Guid,object>();
		}
		public UserData(UserData rhs) :this()
		{
			foreach (var pair in rhs.m_store)
				m_store.Add(pair.Key, pair.Value);
		}

		/// <summary>Access the user data by GUID</summary>
		public object this[Guid id]
		{
			get
			{
				object value;
				return m_store.TryGetValue(id, out value) ? value : null;
			}
			set
			{
				if (m_store.ContainsKey(id))
					m_store[id] = value;
				else
					m_store.Add(id, value);
			}
		}
	}
}
