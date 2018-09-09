using System;

namespace RyLogViewer
{
	public static class Guard
	{
		/// <summary>Check for null arguments</summary>
		public static void ArgNotNull(object value, string msg)
		{
			if (value != null) return;
			throw new ArgumentNullException(msg, (Exception)null);
		}
	}
}
