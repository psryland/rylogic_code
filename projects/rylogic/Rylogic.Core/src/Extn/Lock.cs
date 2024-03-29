﻿using System.Threading;
using Rylogic.Utility;

namespace Rylogic.Extn
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
