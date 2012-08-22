using System;
using System.Runtime.InteropServices;

namespace pr.common
{
	/// <summary>RAII wrapper for unmanaged memory allocations</summary>
	public class HGlobalBuffer :IDisposable
	{
		public static HGlobalBuffer New(int size_in_bytes) { return new HGlobalBuffer(size_in_bytes); }
		public static HGlobalBuffer New<T>(int count)      { return new HGlobalBuffer(Marshal.SizeOf(typeof(T)) * count); }

		public IntPtr Ptr                        { get; private set; }
		private HGlobalBuffer(int size_in_bytes) { Ptr = Marshal.AllocHGlobal(size_in_bytes); }
		public void Dispose()                    { Marshal.FreeHGlobal(Ptr); }
	}
}
