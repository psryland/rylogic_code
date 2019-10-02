using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	public interface IStatusMessage : IDisposable
	{
		/// <summary>The status message content</summary>
		string Message { get; set; }
	}

	public class StatusMessageStack<TContext>
	{
		// Notes:
		//  - Thread safe, status message stack
		// Usage:
		//  Create your own specialisations like this:
		//   public class MyStatusStack : StatusMessageStack<MyStatusStack> {}
		//  Observer 'MyStatusStack.ValueChanged' to update the status.
		//  Then use the factory method to create disposable status message instances
		//   var msg = MyStatusStack.NewStatusMessage("Hello");

		static StatusMessageStack()
		{
			DefaultStatusMessage = "Idle";
			MessageStack = new List<StatusMessage>();
		}

		/// <summary>A stack of messages</summary>
		private static List<StatusMessage> MessageStack { get; }

		/// <summary>The default string to return when the stack is empty</summary>
		public static string DefaultStatusMessage { get; set; }

		/// <summary>The current top-of-the-stack status message</summary>
		public static string Value
		{
			get
			{
				lock (MessageStack)
					return MessageStack.LastOrDefault()?.Message ?? DefaultStatusMessage;
			}
		}

		/// <summary>Raised when 'Value' changes. Careful, raised on the thread that changes the top status message</summary>
		public static event EventHandler? ValueChanged;

		/// <summary>Factory for producing status message instances</summary>
		public static IStatusMessage NewStatusMessage(string? message = null)
		{
			return new StatusMessage(message);
		}

		/// <summary>Status message instance</summary>
		[DebuggerDisplay("{m_sb.ToString()}")]
		private class StatusMessage : IStatusMessage
		{
			private readonly StringBuilder m_sb;
			public StatusMessage(string? message)
			{
				m_sb = new StringBuilder(message ?? string.Empty);
				lock (MessageStack)
					MessageStack.Add(this);

				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
			public virtual void Dispose()
			{
				bool notify;
				lock (MessageStack)
				{
					if (MessageStack.Count == 0) return;
					notify = MessageStack.Back() == this;
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
				get
				{
					lock (MessageStack)
						return m_sb.ToString();
				}
				set
				{
					bool notify;
					lock (MessageStack)
					{
						notify = MessageStack.Back() == this;
						m_sb.Assign(value);
					}
					if (notify)
					{
						ValueChanged?.Invoke(this, EventArgs.Empty);
					}
				}
			}
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
						Assert.Equal("Status 3", MyStatusStack.Value);
						msg2.Message = "Changed Status 2";
						Assert.Equal("Status 3", MyStatusStack.Value);
					}
					Assert.Equal("Changed Status 2", MyStatusStack.Value);
				}
				Assert.Equal("Status 1", MyStatusStack.Value);
			}
			Assert.Equal("Idle", MyStatusStack.Value);
		}
	}
}
#endif