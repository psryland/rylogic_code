//***************************************************
// Ref Count
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

//#define REFS
//#define STACKTRACES
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>A helper object for maintaining a reference count</summary>
	[DebuggerDisplay("Count={Count}")]
	public class RefCount
	{
		private RefList m_refs = new RefList();
		private RefStacks m_stacks = new RefStacks();

		public RefCount()
			: this(0)
		{ }
		public RefCount(int initial_count)
		{
			m_count = initial_count;
		}

		/// <summary>Returns a disposable object that increases the reference count and decreases it on disposing. Use in a using block</summary>
		public IDisposable RefToken(object who)
		{
			return Scope.Create(() =>
			{
				AddWho(who);
				++Count;
			},
			() =>
			{
				--Count;
				RemoveWho(who);
			});
		}

		/// <summary>The current number of references</summary>
		public int Count
		{
			get => m_count;
			private set
			{
				if (m_count == value) return;
				if (Math.Abs(m_count - value) != 1) throw new Exception("RefCount should only be changed by one reference at a time");
				var beg = m_count == 0 && value == 1;
				var end = m_count == 1 && value == 0; 
				m_count = value;
				if (beg) Referenced?.Invoke(this, EventArgs.Empty);
				if (end) ZeroCount?.Invoke(this, EventArgs.Empty);
				if (m_count < 0) throw new Exception("Ref count mismatch");
			}
		}
		private int m_count;

		/// <summary>Raised when the reference count goes from 0 to non-0</summary>
		public event EventHandler? Referenced;

		/// <summary>Raised when the reference count goes from non-0 to 0</summary>
		public event EventHandler? ZeroCount;

		/// <summary></summary>
		[Conditional("REFS")]
		private void AddWho(object who)
		{
			m_refs.Add(who);
			AddStack(who);
		}
		[Conditional("REFS")]
		private void RemoveWho(object who)
		{
			RemoveStack(who);
			m_refs.Remove(who);
		}
		[Conditional("STACKTRACES")]
		private void AddStack(object who)
		{
			m_stacks[who] = new StackTrace(true).ToString();
		}
		[Conditional("STACKTRACES")]
		private void RemoveStack(object who)
		{
			m_stacks.Remove(who);
		}
		private class RefList :List<object> { }
		private class RefStacks :Dictionary<object, string> { }
	}
}
