using System;
using System.Threading;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>Wraps an object in a monitor lock</summary>
	public class Synchronised<T>
	{
		private T m_obj;
		private object m_lock;

		public Synchronised()
			:this((T)Activator.CreateInstance(typeof(T)))
		{}
		public Synchronised(T obj)
		{
			m_obj = obj;
			m_lock = new object();
		}

		/// <summary>Obtain the lock on the contained object</summary>
		public Scope<T> Lock
		{
			get
			{
				return Scope.Create(
					() => { Monitor.Enter(m_lock); return m_obj; },
					 x => { Monitor.Exit(m_lock); });
			}
		}
	}
}
