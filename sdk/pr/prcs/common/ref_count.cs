//***************************************************
// Ref Count
//  Copyright © Rylogic Ltd 2013
//***************************************************

using System;
using pr.util;

namespace pr.common
{
	/// <summary>A helper object for maintaining a reference count</summary>
	public class RefCount
	{
		/// <summary>Explicitly add a reference</summary>
		public void AddRef()     { ++Count; }

		/// <summary>Explicitly drop a reference</summary>
		public void ReleaseRef() { --Count; }

		/// <summary>The current number of references</summary>
		public int Count
		{
			get { return m_count; }
			private set
			{
				m_count = value;
				if (m_count == 0 && ZeroCount != null) ZeroCount();
				if (m_count < 0) throw new Exception("Ref count mismatch");
			}
		}
		private int m_count;

		/// <summary>A event raised when the reference count reaches 0</summary>
		public event Action ZeroCount;

		/// <summary>Returns a disposable object that increases the reference count and decreases it on disposing. Use in a using block</summary>
		public Scope Reference { get { return Scope.Create(() => ++Count, () => --Count); } }

		public RefCount() :this(0) {}
		public RefCount(int initial_count)
		{
			m_count = initial_count;
		}
	}
}
