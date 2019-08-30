using System;
using System.Collections.Generic;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	public class UndoHistory<TRecord>
	{
		// Notes:
		//  - Creates an undo history of 'TRecord' entries

		public UndoHistory()
		{
			History = new List<TRecord>();
			Capacity = 100;
			End = 0;
		}

		/// <summary>The undo history. The entry at History[End-1] is the current value</summary>
		private List<TRecord> History { get; }

		/// <summary>The end of the history during undo/redo</summary>
		private int End { get; set; }

		/// <summary>Compares to history records for approximately equal. If equal enough, the rhs entry is not added to the history</summary>
		public Func<TRecord, TRecord, bool> IsDuplicate { get; set; }

		/// <summary>Function for applying a history snapshot</summary>
		public Action<TRecord> ApplySnapshot { get; set; }

		/// <summary>The maximum length of the history</summary>
		public int Capacity
		{
			get => m_capacity;
			set
			{
				if (m_capacity == value) return;
				m_capacity = value;
				LimitHistoryLength();
			}
		}
		private int m_capacity;

		/// <summary>The number of values in the history</summary>
		public int Count => Math.Max(0, History.Count - Capacity);

		/// <summary>The current history snapshot</summary>
		public TRecord Current => End != 0 ? History[End - 1] : default;

		/// <summary>Clear all history</summary>
		public void Clear()
		{
			History.Resize(0);
			End = History.Count;
		}

		/// <summary>Add an undo snapshot to the history</summary>
		public void Add(TRecord snapshot)
		{
			if (m_defer_history_add != 0)
				return;

			// If the snapshot is too similar to the previous one, don't bother storing it
			if (IsDuplicate != null && History.Count != 0 && IsDuplicate(History.Back(), snapshot))
				return;

			// Add the snapshot to the history
			History.Resize(End);
			History.Add(snapshot);
			End++;

			// Limit the history length
			LimitHistoryLength();
		}

		/// <summary>Prevent snapshots from being added to the history</summary>
		public IDisposable Defer()
		{
			return Scope.Create(
				() => ++m_defer_history_add,
				() => --m_defer_history_add);
		}
		private int m_defer_history_add;

		/// <summary>Go back one position in the history</summary>
		public void Undo()
		{
			var min = Math.Max(0, History.Count - Capacity);
			End = Math.Max(min, End - 1);
			if (End == min)
				return;

			using (Defer())
			{
				// Apply this history snapshot
				var snapshot = History[End - 1];
				ApplySnapshot(snapshot);
			}
		}

		/// <summary>Go forward one position in the history</summary>
		public void Redo()
		{
			End = Math.Min(History.Count, End + 1);
			if (End == 0)
				return;

			using (Defer())
			{
				var snapshot = History[End - 1];
				ApplySnapshot(snapshot);
			}
		}

		/// <summary>Limit the history length to 'Capacity'</summary>
		private void LimitHistoryLength()
		{
			// Allow 'History' to reach 150% capacity even though the interface of this object
			// presents 'Capacity' as the maximum history length. This is for efficiency, not 
			// have to remove from the start of the list all the time.
			if (History.Count > Capacity * 3 / 2)
			{
				History.RemoveRange(0, 50);
				End = History.Count;
			}
		}
	}
}
