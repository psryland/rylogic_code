using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	/// <summary>Extra methods related to Marshal</summary>
	public static class MarshalEx
	{
		/// <summary>RAII scope for allocated global memory</summary>
		public static Scope<IntPtr> AllocHGlobal(int size)
		{
			return Scope<IntPtr>.Create(() => Marshal.AllocHGlobal(size), Marshal.FreeHGlobal);
		}
	}

	//	/// <summary>RAII wrapper for unmanaged memory allocations</summary>
	//public class HGlobalBuffer :IDisposable
	//{
	//	public static HGlobalBuffer New(int size_in_bytes) { return new HGlobalBuffer(size_in_bytes); }
	//	public static HGlobalBuffer New<T>(int count)      { return new HGlobalBuffer(Marshal.SizeOf(typeof(T)) * count); }

	//	public IntPtr Ptr                        { get; private set; }
	//	private HGlobalBuffer(int size_in_bytes) { Ptr = Marshal.AllocHGlobal(size_in_bytes); }
	//	public void Dispose()                    { Marshal.FreeHGlobal(Ptr); }
	//}
}
