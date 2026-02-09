using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Common
{
	public interface IUndoRedo
	{
		/// <summary>Undo the last operation</summary>
		void Undo(object context);

		/// <summary>Redo the last operation</summary>
		void Redo(object context);
	}

	public class UndoStack :INotifyPropertyChanged, IUndoRedo
	{
		// Notes:
		//  - The UndoStack is a recursive structure. An Undo group is just a nested UndoStack

		private readonly List<IUndoRedo> m_undos = new();
		private readonly List<IUndoRedo> m_redos = new();
		private readonly Stack<Batch> m_batches = new();

		public UndoStack()
		{
			HistoryLength = 1024;
		}

		/// <summary>The maximum length of the undo history</summary>
		public int HistoryLength
		{
			get;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), "History length must be >= 0");
				if (GroupInScope)
					throw new InvalidOperationException($"Cannot change the undo history length within a change group");

				if (field == value)
					return;

				field = value;
				NotifyPropertyChanged(nameof(HistoryLength));
				EnforceLimits();
			}
		}

		/// <summary>Clear the entire stack</summary>
		public void Clear()
		{
			ClearUndos();
			ClearRedos();
		}

		/// <summary>Drop any redo operations</summary>
		public void ClearUndos()
		{
			if (m_undos.Count == 0) return;
			m_undos.Clear();
			NotifyPropertyChanged(nameof(CanUndo));
		}

		/// <summary>Drop any redo operations</summary>
		public void ClearRedos()
		{
			if (m_redos.Count == 0) return;
			m_redos.Clear();
			NotifyPropertyChanged(nameof(CanRedo));
		}

		/// <summary>True if undo is possible</summary>
		public bool CanUndo => m_undos.Count != 0;

		/// <summary>True if redo is possible</summary>
		public bool CanRedo => m_redos.Count != 0;

		/// <summary>Undo the last operation</summary>
		public void Undo(object context)
		{
			if (GroupInScope)
				throw new InvalidOperationException($"Cannot apply undo while a group is in scope");
			if (!CanUndo)
				return;

			var action = m_undos.PopBack();
			m_redos.Add(action);

			using (UndoRedoScope())
				action.Undo(context);

			if (m_redos.Count == 1)
				NotifyPropertyChanged(nameof(CanRedo));
			if (m_undos.Count == 0)
				NotifyPropertyChanged(nameof(CanUndo));
		}

		/// <summary>Redo the last operation</summary>
		public void Redo(object context)
		{
			if (GroupInScope)
				throw new InvalidOperationException($"Cannot apply redo while a group is in scope");
			if (!CanRedo)
				return;

			var action = m_redos.PopBack();
			m_undos.Add(action);

			using (UndoRedoScope()) 
				action.Redo(context);

			if (m_undos.Count == 1)
				NotifyPropertyChanged(nameof(CanUndo));
			if (m_redos.Count == 0)
				NotifyPropertyChanged(nameof(CanRedo));
		}

		/// <summary>Push an undoable action onto the stack</summary>
		public T Push<T>(T action) where T : IUndoRedo
		{
			// Ignore changes to the undo stack while undo/redo are in progress
			if (InProgress)
				return action;

			// If a group of changes is in scope, add the action to the group
			if (GroupInScope)
			{
				var batch = m_batches.Peek();
				batch.Actions.Add(action);
				return action;
			}

			var first = m_undos.Count == 0;

			// Otherwise, add the action to the main undo history.
			ClearRedos();
			m_undos.Add(action);
			EnforceLimits();

			if (first && m_undos.Count != 0)
				NotifyPropertyChanged(nameof(CanUndo));

			return action;
		}

		/// <summary>Create a group of actions that correspond to a single undo/redo operation</summary>
		public IDisposable Group()
		{
			return Scope.Create(
				() => m_batches.Push(new Batch()),
				() => Push(m_batches.Pop()));
		}

		/// <summary>Blocks modification of the undo stack while actions are being undone or redone</summary>
		private IDisposable UndoRedoScope()
		{
			return Scope.Create(
				() => ++m_in_progress,
				() => --m_in_progress);
		}

		/// <summary>True while actions are being grouped</summary>
		public bool GroupInScope => m_batches.Count != 0;

		/// <summary>True when an undo or redo action is being applied</summary>
		public bool InProgress => m_in_progress != 0;
		private int m_in_progress;

		/// <summary>Enforce the maximum length of undo history</summary>
		private void EnforceLimits()
		{
			// Only apply history limit when at the top of the undo stack
			if (CanRedo)
				return;

			m_undos.RemoveRange(0, Math.Max(0, m_undos.Count - HistoryLength));
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary>A group of undoable actions</summary>
		private class Batch :IUndoRedo
		{
			public List<IUndoRedo> Actions { get; } = new List<IUndoRedo>();

			/// <inheritdoc/>
			public void Redo(object context)
			{
				for (int i = 0; i != Actions.Count; ++i)
					Actions[i].Redo(context);
			}

			/// <inheritdoc/>
			public void Undo(object context)
			{
				for (int i = Actions.Count; i-- != 0; )
					Actions[i].Undo(context);
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Rylogic.Common;

	[TestFixture]
	public class TestUndoStack
	{
		public class StringChange :IUndoRedo
		{
			public StringChange(EOp op, string text, int index)
			{
				Op = op;
				Text = text;
				Index = index;
			}

			/// <summary>The text operation</summary>
			public EOp Op;
			public enum EOp { Inserted, Removed }

			/// <summary>The Text added, or removed</summary>
			public string Text;

			/// <summary>The index at the start of the text operation</summary>
			public int Index;

			/// <inheritdoc/>
			public void Redo(object context)
			{
				if (!(context is StringBuilder sb)) return;
				switch (Op)
				{
					case EOp.Inserted:
						sb.Insert(Index, Text);
						break;
					case EOp.Removed:
						sb.Remove(Index, Text.Length);
						break;
					default:
						throw new NotImplementedException();
				}
			}

			/// <inheritdoc/>
			public void Undo(object context)
			{
				if (!(context is StringBuilder sb)) return;
				switch (Op)
				{
					case EOp.Removed:
						sb.Insert(Index, Text);
						break;
					case EOp.Inserted:
						sb.Remove(Index, Text.Length);
						break;
					default:
						throw new NotImplementedException();
				}
			}
		}

		[Test]
		public void Basic()
		{
			var stack = new UndoStack();
			var sb = new StringBuilder();

			var chg0 = new StringChange(StringChange.EOp.Inserted, "Hello ", 0);
			stack.Push(chg0).Redo(sb);
			Assert.Equal("Hello ", sb.ToString());

			var chg1 = new StringChange(StringChange.EOp.Inserted, "World", 6);
			stack.Push(chg1).Redo(sb);
			Assert.Equal("Hello World", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("Hello ", sb.ToString());

			var chg2 = new StringChange(StringChange.EOp.Inserted, "Bob", 6);
			stack.Push(chg2).Redo(sb);
			Assert.Equal("Hello Bob", sb.ToString());

			var chg3 = new StringChange(StringChange.EOp.Removed, "o ", 4);
			stack.Push(chg3).Redo(sb);
			Assert.Equal("HellBob", sb.ToString());

			using (var scope = stack.Group())
			{
				var chg4 = new StringChange(StringChange.EOp.Removed, "b", 6);
				stack.Push(chg4).Redo(sb);

				var chg5 = new StringChange(StringChange.EOp.Inserted, "y", 6);
				stack.Push(chg5).Redo(sb);
			}
			Assert.Equal("HellBoy", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("HellBob", sb.ToString());

			stack.Redo(sb);
			Assert.Equal("HellBoy", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("HellBob", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("Hello Bob", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("Hello ", sb.ToString());

			stack.Undo(sb);
			Assert.Equal("", sb.ToString());

			Assert.True(!stack.CanUndo);
			Assert.True(stack.CanRedo);

			stack.Redo(sb);
			Assert.Equal("Hello ", sb.ToString());

			var chg6 = new StringChange(StringChange.EOp.Inserted, "Bitches!", 6);
			stack.Push(chg6).Redo(sb);

			Assert.Equal("Hello Bitches!", sb.ToString());
			Assert.True(stack.CanUndo);
			Assert.True(!stack.CanRedo);
		}
	}
}
#endif
