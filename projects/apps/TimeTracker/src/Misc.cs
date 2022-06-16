using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker
{
	public static class Misc
	{
		/// <summary>True if 'lhs' and 'rhs' are equivalent task names</summary>
		public static bool SameTaskName(string? lhs, string? rhs)
		{
			return (lhs == null && rhs == null) || (lhs != null && rhs != null && string.Compare(lhs, rhs, true) == 0);
		}
	}
}
