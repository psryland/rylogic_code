using System;
using System.Threading;
using System.Threading.Tasks;

namespace pr.extn
{
	public static class Task_
	{
		/// <summary>A task to wait fill 'wait' is signalled</summary>
		public static Task WaitUntil(WaitHandle wait)
		{
			return Task.Run(() =>
			{
				wait.WaitOne();
			});
		}

		/// <summary>A task to wait till 'condition' returns false</summary>
		public static Task WaitWhile(Func<bool> condition)
		{
			return Task.Run(() =>
			{
				for (; condition(); )
					Thread.Yield();
			});
		}
	}
}
