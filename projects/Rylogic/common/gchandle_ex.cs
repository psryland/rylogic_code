using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	/// <summary>Extra methods for GCHandle</summary>
	public static class GCHandleEx
	{
		/// <summary>RAII scope for an allocated GC handle</summary>
		public static Scope<GCHandle> Alloc(object obj)
		{
			return Scope<GCHandle>.Create(() => GCHandle.Alloc(obj), h => h.Free());
		}

		/// <summary>RAII scope for an allocated GC handle</summary>
		public static Scope<GCHandle> Alloc(object obj, GCHandleType type)
		{
			return Scope<GCHandle>.Create(() => GCHandle.Alloc(obj,type), h => h.Free());
		}
	}
}
