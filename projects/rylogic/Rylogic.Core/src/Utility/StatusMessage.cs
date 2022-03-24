using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Gfx;

namespace Rylogic.Utility
{
	public interface IStatusMessage : IDisposable
	{
		/// <summary>The status message content</summary>
		string Message { get; set; }

		/// <summary>Status colour</summary>
		Colour32 Colour { get; set; }
	}

	public class StatusMessageStack<TContext> :INotifyPropertyChanged
	{
		// Notes:
		//  - Thread safe.
		//  - Use as a singleton or an instance. 'TContext' is used to allow multiple global instances.
		//  - Status messages are editable to avoid excessive pop/push.
		//
		// Usage:
		//  Create your own specialisation like this:
		//   public class MyStatusStack : StatusMessageStack<MyStatusStack> {}
		// 
		//  Then use the factory method to create disposable status message instances
		//   var msg = MyStatusStack.Instance.Push("Hello", Colour.Red);

		public StatusMessageStack()
		{
			ThreadId = Thread.CurrentThread.ManagedThreadId;
			SyncCtx = SynchronizationContext.Current ?? new SynchronizationContext();
			Default = Push("Idle", Colour32.Black, ttl: null);
			m_instance = this;
		}

		/// <summary>Singleton access</summary>
		public static StatusMessageStack<TContext> Instance => m_instance ??= new StatusMessageStack<TContext>();
		public static StatusMessageStack<TContext> m_instance = null!;

		/// <summary>A stack of messages</summary>
		private List<IStatusMessage> MessageStack { get; } = new List<IStatusMessage>();

		/// <summary>Sync context used to implement time-to-live messages</summary>
		private SynchronizationContext SyncCtx { get; }
		private int ThreadId { get; }

		/// <summary>The default status message</summary>
		public IStatusMessage Default { get; }

		/// <summary>The current top level status message</summary>
		public IStatusMessage Top
		{
			get
			{
				lock (MessageStack)
					return MessageStack.Back();
			}
		}

		/// <summary>The current top-of-the-stack status message</summary>
		public string Message => Top.Message;

		/// <summary>The current top-of-the-stack status message colour</summary>
		public Colour32 Colour => Top.Colour;

		/// <summary>Factory for producing status message instances</summary>
		public IStatusMessage Push(string? message = null, Colour32? colour = null, TimeSpan? ttl = null)
		{
			var sm = new StatusMessage(this, message, colour, ttl);
			lock (MessageStack)
			{
				MessageStack.Add(sm);
			}
			RunOnMainThread(() =>
			{
				NotifyPropertyChanged(nameof(Message));
				NotifyPropertyChanged(nameof(Colour));
			});
			return sm;
		}

		/// <summary>Pop the current status message from the stack</summary>
		public IStatusMessage? Pop()
		{
			var sm = (IStatusMessage?)null;
			lock (MessageStack)
			{
				// Don't allow the default message to be popped
				if (MessageStack.Count == 1) return null;
				sm = MessageStack.PopBack();
			}
			RunOnMainThread(() =>
			{
				NotifyPropertyChanged(nameof(Message));
				NotifyPropertyChanged(nameof(Colour));
			});
			return sm;
		}

		/// <summary>Remove a status message from somewhere in the stack</summary>
		public void Remove(IStatusMessage sm)
		{
			if (sm == Default)
				throw new Exception("The default status message cannot be removed");

			var was_top = false;
			lock (MessageStack)
			{
				var idx = MessageStack.IndexOf(sm);
				if (idx != -1) MessageStack.RemoveAt(idx);
				was_top = idx == MessageStack.Count;
			}
			if (was_top)
			{
				RunOnMainThread(() =>
				{
					NotifyPropertyChanged(nameof(Message));
					NotifyPropertyChanged(nameof(Colour));
				});
			}
		}

		/// <summary></summary>
		internal void RunOnMainThread(Action action)
		{
			if (Thread.CurrentThread.ManagedThreadId != ThreadId)
				SyncCtx.Post(_ => action(), null);
			else
				action();
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		internal void NotifyPropertyChanged(string prop_name)
		{
			if (Thread.CurrentThread.ManagedThreadId != ThreadId)
				throw new Exception("Only notify from the main thread");

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Status message instance</summary>
		[DebuggerDisplay("{Message,nq}")]
		private sealed class StatusMessage : IStatusMessage
		{
			private readonly StatusMessageStack<TContext> m_owner;
			public StatusMessage(StatusMessageStack<TContext> owner, string? message, Colour32? colour, TimeSpan? ttl)
			{
				m_owner = owner;
				m_sb = new StringBuilder(message ?? string.Empty);
				m_colour = colour ?? owner.Default.Colour;

				// If the status expires after a time, set a timer to remove it
				if (ttl != null)
				{
					m_expire_timer = new Timer(HandleExpire, this, (int)ttl.Value.TotalMilliseconds, -1);
					void HandleExpire(object? state)
					{
						m_expire_timer?.Dispose();
						if (state is StatusMessage sm)
							sm.Dispose();
					}
				}
			}
			public void Dispose()
			{
				m_owner.Remove(this);
			}

			/// <summary>The status message</summary>
			public string Message
			{
				get => m_sb.ToString();
				set
				{
					if (Message == value) return;
					m_owner.RunOnMainThread(() =>
					{
						m_sb.Assign(value);
						if (m_owner.Top != this) return;
						m_owner.NotifyPropertyChanged(nameof(StatusMessageStack<TContext>.Message));
					});
				}
			}
			private readonly StringBuilder m_sb;

			/// <summary>The status colour</summary>
			public Colour32 Colour
			{
				get => m_colour;
				set
				{
					if (Colour == value) return;
					m_owner.RunOnMainThread(() =>
					{
						m_colour = value;
						if (m_owner.Top != this) return;
						m_owner.NotifyPropertyChanged(nameof(StatusMessageStack<TContext>.Colour));
					});
				}
			}
			private Colour32 m_colour;

			/// <summary>Timer for TTL</summary>
			private Timer? m_expire_timer;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture]
	public class TestStatusMessageStack
	{
		/// <summary>MyStatusStack instance</summary>
		public class MyStatusStack : StatusMessageStack<MyStatusStack>
		{ }

		[Test]
		public void DefaultUse()
		{
			var status = MyStatusStack.Instance;
			using (var msg1 = status.Push("Status 1"))
			{
				using (var msg2 = status.Push("Status 2"))
				{
					using (var msg3 = status.Push("Status 3"))
					{
						Assert.Equal("Status 3", status.Message);
						msg2.Message = "Changed Status 2";
						Assert.Equal("Status 3", status.Message);
					}
					Assert.Equal("Changed Status 2", status.Message);
				}
				Assert.Equal("Status 1", status.Message);
			}
			Assert.Equal("Idle", status.Message);
		}
	}
}
#endif