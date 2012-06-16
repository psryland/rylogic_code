using System;

namespace pr.util
{
	/// <summary>An general purpose RAII scope</summary>
	public class Scope :IDisposable
	{
		private readonly Action m_on_exit;
		public static Scope Create(Action set, Action restore) { return new Scope(set, restore); }
		private     Scope(Action on_enter, Action on_exit)     { m_on_exit = on_exit; if (on_enter != null) on_enter(); }
		public void Dispose()                                  { if (m_on_exit != null) m_on_exit(); }
	}
}
