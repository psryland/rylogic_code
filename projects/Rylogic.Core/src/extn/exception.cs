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
			if (e is AggregateException ae)
				e = ae.Flatten();
			
			var sb = new StringBuilder();
			for (;e != null; e = e.InnerException)
			{
				if (e is AggregateException) continue;
				if (sb.Length != 0) sb.AppendLine();
				sb.Append(e.Message);
			}
			return sb.ToString();
		}
	}
}
