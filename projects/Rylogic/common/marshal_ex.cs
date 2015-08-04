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
		public static IntPtrScope AllocHGlobal(int size_in_bytes)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = Marshal.AllocHGlobal(size_in_bytes),
				s => Marshal.FreeHGlobal(s.Ptr));
		}

		/// <summary>RAII scope for allocated global memory</summary>
		public static IntPtrScope AllocHGlobal(Type type, int count)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(type) * count),
				s => Marshal.FreeHGlobal(s.Ptr));
		}
		public static IntPtrScope AllocHGlobal<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocHGlobal(typeof(TStruct), count);
		}

		/// <summary>RAII scope for allocated co-task memory</summary>
		public static IntPtrScope AllocCoTaskMem(int size_in_bytes)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = Marshal.AllocCoTaskMem(size_in_bytes),
				s => Marshal.FreeCoTaskMem(s.Ptr));
		}

		/// <summary>RAII scope for allocated co-task memory</summary>
		public static IntPtrScope AllocCoTaskMem(Type type, int count)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(type) * count),
				s => Marshal.FreeCoTaskMem(s.Ptr));
		}
		public static IntPtrScope AllocCoTaskMem<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocCoTaskMem(typeof(TStruct), count);
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to ANSI format if required</summary>
		public static IntPtrScope AllocAnsiString(string str)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = Marshal.StringToHGlobalAnsi(str),
				s => Marshal.FreeHGlobal(s.Ptr));
		}

		/// <summary>Copy a structure into non-gc memory. Freeing on dispose</summary>
		public static IntPtrScope StructureToPtr<TStruct>(TStruct strukt) where TStruct:struct
		{
			return Scope.Create<IntPtrScope>(
				s =>
					{
						s.Ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(strukt));
						Marshal.StructureToPtr(strukt, s.Ptr, false);
					},
				s =>
					{
						Marshal.FreeCoTaskMem(s.Ptr);
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
