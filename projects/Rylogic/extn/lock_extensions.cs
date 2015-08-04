using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using pr.util;

namespace pr.extn
{
	public static class LockExtensions
	{
		private class SpinLockScope :Scope
		{
			public bool m_got_lock;
		}

		/// <summary>Acquires the lock and safely releases on dispose</summary>
		public static Scope Lock(this SpinLock sl)
		{
			return Scope.Create<SpinLockScope>(
				s =>
				{
					sl.Enter(ref s.m_got_lock);
				},
				s =>
				{
					if (s.m_got_lock)
						sl.Exit();
				});
		}
	}
}
