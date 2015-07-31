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
			return Alloc(obj, GCHandleType.Normal);
		}

		/// <summary>RAII scope for an allocated GC handle</summary>
		public static Scope<GCHandle> Alloc(object obj, GCHandleType type)
		{
			return Scope<GCHandle>.Create(
				() =>
				{
					return GCHandle.Alloc(obj, type);
				},
				h =>
				{
					if (h.IsAllocated) h.Free();
				});
		}
	}
}
