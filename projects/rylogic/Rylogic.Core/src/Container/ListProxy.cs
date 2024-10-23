//***************************************************
// List Proxy
//  Copyright (c) Rylogic Ltd 2022
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;

namespace Rylogic.Container
{
	/// <summary>A wrapper that converts an IList<T> to an IList</summary>
	public class ListProxy<T> : IList, ICollection, IEnumerable
	{
		private readonly IList<T> m_list;
		public ListProxy(IList<T> list) => m_list = list;
		private T AsItem(object? value) => value is T item ? item : throw new InvalidCastException($"Value is not of type: {typeof(T).Name}");

		#region IList
		object? IList.this[int index]
		{
			get => m_list[index]!;
			set => m_list[index] = AsItem(value);
		}
		int IList.Add(object? value) { m_list.Add(AsItem(value)); return m_list.Count - 1; }
		void IList.Clear() => m_list.Clear();
		bool IList.Contains(object? value) => m_list.Contains(AsItem(value));
		int IList.IndexOf(object? value) => m_list.IndexOf(AsItem(value));
		void IList.Insert(int index, object? value) => m_list.Insert(index, AsItem(value));
		void IList.Remove(object? value) => m_list.Remove(AsItem(value));
		void IList.RemoveAt(int index) => m_list.RemoveAt(index);
		bool IList.IsFixedSize => false;
		bool IList.IsReadOnly => m_list.IsReadOnly;
		#endregion
		#region ICollection
		void ICollection.CopyTo(Array array, int index) => m_list.CopyTo((T[])array, index);
		int ICollection.Count => m_list.Count;
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => m_list;
		#endregion
		#region IEnumerator
		IEnumerator IEnumerable.GetEnumerator() => m_list.GetEnumerator();
		#endregion
	}
}
