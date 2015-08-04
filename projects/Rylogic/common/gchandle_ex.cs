using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	public class GCHandleScope :Scope
	{
		public GCHandle Handle;
		public GCHandleScope(object obj, GCHandleType type)
		{
			Init(() =>
				{
					Handle = GCHandle.Alloc(obj, type);
				},
				() =>
				{
					if (Handle.IsAllocated)
						Handle.Free();
				});
		}
	}

	/// <summary>Extra methods for GCHandle</summary>
	public static class GCHandleEx
	{
		/// <summary>RAII scope for an allocated GC handle</summary>
		public static GCHandleScope Alloc(object obj)
		{
			return Alloc(obj, GCHandleType.Normal);
		}

		/// <summary>RAII scope for an allocated GC handle</summary>
		public static GCHandleScope Alloc(object obj, GCHandleType type)
		{
			return new GCHandleScope(obj, type);
		}
	}
}
