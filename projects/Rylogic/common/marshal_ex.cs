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

		/// <summary>Convenience wrapper for release</summary>
		public static void Release(ref IntPtr ptr)
		{
			if (ptr == IntPtr.Zero) return;
			Marshal.Release(ptr);
			ptr = IntPtr.Zero;
		}

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
		public static Scope<IntPtr> AllocHGlobal<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocHGlobal(typeof(TStruct), count);
		}

		/// <summary>RAII scope for allocated co-task memory</summary>
		public static Scope<IntPtr> AllocCoTaskMem(int size_in_bytes)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.AllocCoTaskMem(size_in_bytes),
				 Marshal.FreeCoTaskMem);
		}

		/// <summary>RAII scope for allocated co-task memory</summary>
		public static Scope<IntPtr> AllocCoTaskMem(Type type, int count)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.AllocCoTaskMem(Marshal.SizeOf(type) * count),
				Marshal.FreeCoTaskMem);
		}
		public static Scope<IntPtr> AllocCoTaskMem<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocCoTaskMem(typeof(TStruct), count);
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to ANSI format if required</summary>
		public static Scope<IntPtr> AllocAnsiString(string str)
		{
			return Scope<IntPtr>.Create(
				() => Marshal.StringToHGlobalAnsi(str),
				Marshal.FreeHGlobal);
		}

		/// <summary>Copy a structure into non-gc memory. Freeing on dispose</summary>
		public static Scope<IntPtr> StructureToPtr<TStruct>(TStruct s) where TStruct:struct
		{
			return Scope<IntPtr>.Create(
				() =>
					{
						var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(s));
						Marshal.StructureToPtr(s, ptr, false);
						return ptr;
					},
				p =>
					{
						Marshal.FreeCoTaskMem(p);
					});
		}

		/// <summary>Convert an IntPtr to a 'TStruct'</summary>
		public static TStruct PtrToStructure<TStruct>(IntPtr ptr) where TStruct:struct
		{
			return (TStruct)Marshal.PtrToStructure(ptr, typeof(TStruct));
		}

		/// <summary>Convert an unmanaged function pointer to a delegate</summary>
		public static TFunc PtrToDelegate<TFunc>(IntPtr ptr) where TFunc:class//delegate
		{
			if (!typeof(TFunc).IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException(typeof(TFunc).Name + " is not a delegate type");

			return Marshal.GetDelegateForFunctionPointer(ptr, typeof(TFunc)) as TFunc;
		}
	}
}
