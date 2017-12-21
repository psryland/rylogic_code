//***************************************************
// Exception Extensions
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

using System;
using System.Text;

namespace Rylogic.Extn
{
	public static class Exception_
	{
		/// <summary>Returns a message that includes the messages from any inner exceptions as well</summary>
		public static string MessageFull(this Exception e)
		{
			var sb = new StringBuilder(e.Message);
			for (;(e = e.InnerException) != null;)
			{
				sb.AppendLine();
				sb.Append(e.Message);
			}
			return sb.ToString();
		}

		/// <summary>Returns a message that includes the messages from any inner exceptions as well</summary>
		public static string MessageFull(this AggregateException e)
		{
			e = e.Flatten();
			var sb = new StringBuilder();
			for (int i = 0, iend = e.InnerExceptions.Count; i != iend; ++i)
			{
				sb.Append(e.InnerExceptions[i].MessageFull());
				if (i+1 != iend) sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}
