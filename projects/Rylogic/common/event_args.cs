using System;

namespace pr.common
{
	// Common event arg types

	/// <summary></summary>
	public class ValueEventArgs :EventArgs
	{
		public ValueEventArgs(object value)
		{
			Value = value;
		}

		/// <summary>The value</summary>
		public object Value { get; private set; }
	}

	/// <summary></summary>
	public class ValueEventArgs<T> :EventArgs
	{
		public ValueEventArgs(T value)
		{
			Value = value;
		}

		/// <summary>The value</summary>
		public T Value { get; private set; }
	}
}
