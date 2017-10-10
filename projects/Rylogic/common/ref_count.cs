//***************************************************
// Ref Count
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

using System;
using System.Diagnostics;
using pr.extn;
using pr.util;

namespace pr.common
{
	/// <summary>A helper object for maintaining a reference count</summary>
	public class RefCount
	{
		public RefCount()
			:this(0)
		{ }
		public RefCount(int initial_count)
		{
			m_count = initial_count;
		}

		/// <summary>Returns a disposable object that increases the reference count and decreases it on disposing. Use in a using block</summary>
		public Scope Scope()
		{
			// Not a property so references aren't added by the debugger
			return util.Scope.Create(() => ++Count, () => --Count);
		}

		/// <summary>Explicitly add a reference</summary>
		public void AddRef()
		{
			++Count;
		}

		/// <summary>Explicitly drop a reference</summary>
		public void ReleaseRef()
		{
			--Count;
		}

		/// <summary>The current number of references</summary>
		public int Count
		{
			get { return m_count; }
			private set
			{
				if (m_count == value) return;
				Debug.Assert(Math.Abs(m_count - value) == 1);
				m_count = value;
				if (m_count == 1) Referenced.Raise(this);
				if (m_count == 0) ZeroCount.Raise(this);
				if (m_count < 0) throw new Exception("Ref count mismatch");
			}
		}
		private int m_count;

		/// <summary>Raised when the reference count goes from 0 to non-0</summary>
		public event EventHandler Referenced;

		/// <summary>Raised when the reference count goes from non-0 to 0</summary>
		public event EventHandler ZeroCount;
	}
}
