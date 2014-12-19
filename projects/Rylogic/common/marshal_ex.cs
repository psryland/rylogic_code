using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	/// <summary>Extra methods related to Marshal</summary>
	public static class MarshalEx
	{
		// Notes:
		//  C# does not support structure alignment (as of 2015)
		//  If you need to pass an aligned structure to a native api it's
		//  probably better if the native api can be made to handle unaligned
		//  data, rather than try to deal with it in C#. Have the native dll
		//  take unaligned types through the interface and internally convert
		//  them to aligned types.

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
