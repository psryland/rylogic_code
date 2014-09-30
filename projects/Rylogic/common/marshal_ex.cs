using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	/// <summary>Extra methods related to Marshal</summary>
	public static class MarshalEx
	{
		/// <summary>RAII scope for allocated global memory</summary>
		public static Scope<IntPtr> AllocHGlobal(int size_in_bytes)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.AllocHGlobal(size_in_bytes),
				Marshal.FreeHGlobal);
		}

		/// <summary>RAII scope for allocated global memory</summary>
		public static Scope<IntPtr> AllocHGlobal(Type type, int count)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.AllocHGlobal(Marshal.SizeOf(type) * count),
				Marshal.FreeHGlobal);
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to ANSI format if required</summary>
		public static Scope<IntPtr> AllocAnsiString(string str)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.StringToHGlobalAnsi(str),
				Marshal.FreeHGlobal);
		}
	}
}
