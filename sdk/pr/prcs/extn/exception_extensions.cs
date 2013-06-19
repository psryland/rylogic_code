//***************************************************
// Exception Extensions
//  Copyright © Rylogic Ltd 2013
//***************************************************

using System;
using System.Text;

namespace pr.extn
{
	public static class ExceptionExtensions
	{
		/// <summary>Returns a message that includes the messages from any inner exceptions as well</summary>
		public static string MessageFull(this Exception e)
		{
			var sb = new StringBuilder();
			do { sb.AppendLine(e.Message); }
			while ((e = e.InnerException) != null);
			return sb.ToString();;
		}
	}
}
