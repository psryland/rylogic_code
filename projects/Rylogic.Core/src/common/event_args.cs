﻿using System;

namespace Rylogic.Common
{
	// Common event arg types

	/// <summary>Event containing a message string</summary>
	public class MessageEventArgs :EventArgs
	{
		public MessageEventArgs(string msg)
		{
			Message = msg;
		}

		/// <summary>The message</summary>
		public string Message { get; private set; }
	}

	/// <summary>Event containing a single value</summary>
	public class ValueEventArgs :EventArgs
	{
		public ValueEventArgs(object value)
		{
			Value = value;
		}

		/// <summary>The value</summary>
		public object Value { get; private set; }
	}

	/// <summary>Event containing a single value</summary>
	public class ValueEventArgs<T> :EventArgs
	{
		public ValueEventArgs(T value)
		{
			Value = value;
		}

		/// <summary>The value</summary>
		public T Value { get; private set; }
	}

	/// <summary>Event signalled before and then after an event</summary>
	public class PrePostEventArgs :EventArgs
	{
		public PrePostEventArgs(bool after)
		{
			After = after;
		}

		/// <summary>True if this event is logically "before" something is about to happen</summary>
		public bool Before { get { return !After; } }

		/// <summary>True if this event is logically "after" something has happened</summary>
		public bool After { get; private set; }
	}
}