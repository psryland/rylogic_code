using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace pr.extn
{
	public static class ThreadExtensions
	{
		/// <summary>Start this thread and return the thread object</summary>
		public static Thread Run(this Thread thrd)
		{
			thrd.Start();
			return thrd;
		}
	}
}
