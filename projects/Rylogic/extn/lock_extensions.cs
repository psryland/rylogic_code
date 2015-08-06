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
		/// <summary>Acquires the lock and safely releases on dispose</summary>
		public static Scope Lock(this SpinLock sl)
		{
			return Scope.Create(
				() =>
				{
					bool got_lock = false;
					sl.Enter(ref got_lock);
					return got_lock;
				},
				gl =>
				{
					if (gl)
						sl.Exit();
				});
		}
	}
}
