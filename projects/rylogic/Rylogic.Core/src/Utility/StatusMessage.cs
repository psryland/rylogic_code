using System;
using System.Collections.Generic;
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

	public class StatusMessageStack<TContext>
	{
		// Notes:
		//  - Thread safe, singleton status message stack
		//  - 'TContext' is used to allow multiple global instances
		//  - Status messages are editable to avoid excessive pop/push patterns.
		//
		// Usage:
		//  Create your own specialisations like this:
		//   public class MyStatusStack : StatusMessageStack<MyStatusStack> {}
		// 
		//  Observer 'MyStatusStack.ValueChanged' to update the status.
		//  Then use the factory method to create disposable status message instances
		//   var msg = MyStatusStack.NewStatusMessage("Hello");

		/// <summary>A stack of messages</summary>
		private static List<IStatusMessage> MessageStack { get; } = new List<IStatusMessage>();

		/// <summary>Sync context used to implement time-to-live messages</summary>
		private static SynchronizationContext SyncCtx = new SynchronizationContext();

		/// <summary>The default string to return when the stack is empty</summary>
		public static string DefaultStatusMessage { get; set; } = "Idle";

		/// <summary>The default colour of a status message</summary>
		public static Colour32 DefaultStatusColour { get; set; } = Colour32.Black;

		/// <summary>The current top-of-the-stack status message</summary>
		public static string Message
		{
			get
			{
				lock (MessageStack)
					return MessageStack.LastOrDefault()?.Message ?? DefaultStatusMessage;
			}
		}

		/// <summary>The current top-of-the-stack status message colour</summary>
		public static Colour32 Colour
		{
			get
			{
				lock (MessageStack)
					return MessageStack.LastOrDefault()?.Colour ?? DefaultStatusColour;
			}
		}

		/// <summary>Raised when 'Value' changes. Careful, raised on the thread that changes the top status message</summary>
		public static event EventHandler? ValueChanged;

		/// <summary>Factory for producing status message instances</summary>
		public static IStatusMessage NewStatusMessage(string? message = null, Colour32? colour = null, TimeSpan? ttl = null)
		{
			return new StatusMessage(message, colour, ttl);
		}

		/// <summary>Status message instance</summary>
		[DebuggerDisplay("{Message,nq}")]
		private sealed class StatusMessage : IStatusMessage
		{
			public StatusMessage(string? message, Colour32? colour, TimeSpan? ttl)
			{
				m_sb = new StringBuilder(message ?? string.Empty);
				m_colour = colour ?? DefaultStatusColour;

				// Add the status message to the stack
				lock (MessageStack)
					MessageStack.Add(this);

				// Notify of new status
				ValueChanged?.Invoke(this, EventArgs.Empty);

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
				bool notify;
				lock (MessageStack)
				{
					if (MessageStack.Count == 0) return;
					notify = MessageStack.LastOrDefault() == this;
					MessageStack.Remove(this);
				}
				if (notify)
				{
					ValueChanged?.Invoke(this, EventArgs.Empty);
				}
			}

			/// <summary>The status message</summary>
			public string Message
			{
				get => m_sb.ToString();
				set
				{
					bool notify;
					lock (MessageStack)
					{
						notify = MessageStack.LastOrDefault() == this;
						m_sb.Assign(value);
					}
					if (notify)
					{
						ValueChanged?.Invoke(this, EventArgs.Empty);
					}
				}
			}
			private readonly StringBuilder m_sb;

			/// <summary>The status colour</summary>
			public Colour32 Colour
			{
				get => m_colour;
				set
				{
					bool notify;
					lock (MessageStack)
					{
						notify = MessageStack.LastOrDefault() == this;
						m_colour = value;
					}
					if (notify)
					{
						ValueChanged?.Invoke(this, EventArgs.Empty);
					}
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
		public class MyStatusStack : StatusMessageStack<MyStatusStack>
		{ }

		[Test]
		public void DefaultUse()
		{
			using (var msg1 = MyStatusStack.NewStatusMessage("Status 1"))
			{
				using (var msg2 = MyStatusStack.NewStatusMessage("Status 2"))
				{
					using (var msg3 = MyStatusStack.NewStatusMessage("Status 3"))
					{
						Assert.Equal("Status 3", MyStatusStack.Message);
						msg2.Message = "Changed Status 2";
						Assert.Equal("Status 3", MyStatusStack.Message);
					}
					Assert.Equal("Changed Status 2", MyStatusStack.Message);
				}
				Assert.Equal("Status 1", MyStatusStack.Message);
			}
			Assert.Equal("Idle", MyStatusStack.Message);
		}
	}
}
#endif