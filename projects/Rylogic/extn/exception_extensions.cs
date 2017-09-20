//***************************************************
// Exception Extensions
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

using System;
using System.Reflection;
using System.Text;

namespace pr.extn
{
	public static class ExceptionExtensions
	{
		/// <summary>Returns a message that includes the messages from any inner exceptions as well</summary>
		public static string MessageFull(this Exception e)
		{
			var sb = new StringBuilder();
			do
			{
				var skip = (e is TargetInvocationException && e.InnerException != null);
				if (!skip) sb.AppendLine(e.Message);
			}
			while ((e = e.InnerException) != null);
			return sb.ToString();
		}

		/// <summary>Returns a message that includes the messages from any inner exceptions as well</summary>
		public static string MessageFull(this AggregateException e)
		{
			var sb = new StringBuilder();
			foreach (var ex in e.InnerExceptions)
			{
				sb.AppendLine(ex.Message);
			}
			return sb.ToString();
		}
	}
}
