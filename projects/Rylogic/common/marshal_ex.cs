using System;
using System.Runtime.InteropServices;
using pr.util;

namespace pr.common
{
	/// <summary>Unmanaged memory buffer</summary>
	public struct UnmanagedBuffer
	{
		public int Length;
		public IntPtr Ptr;

		public UnmanagedBuffer(Type ty, int count)
		{
			Length = Marshal.SizeOf(ty) * count;
			Ptr = Marshal.AllocHGlobal(Length);
		}
		public void Release()
		{
			Marshal.FreeHGlobal(Ptr);
			Ptr = IntPtr.Zero;
			Length = 0;
		}
	}

	/// <summary>Extra methods related to Marshal</summary>
	public static class Marshal_
	{
		// Notes:
		// HGlobal vs. CoTaskMem:
		//  HGlobal is allocated from the process heap (i.e. same place a malloc, new, etc)
		//  CoTaskMem is allocated from the heap used for COM
		//
		// Alignment:
		//  C# does not support structure alignment (as of 2015)
		//  If you need to pass an aligned structure to a native API it's
		//  probably better if the native API can be made to handle unaligned
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
		public static Scope<UnmanagedBuffer> AllocHGlobal(int size_in_bytes)
		{
			return AllocHGlobal(typeof(byte), size_in_bytes);
		}
		public static Scope<UnmanagedBuffer> AllocHGlobal<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocHGlobal(typeof(TStruct), count);
		}
		public static Scope<UnmanagedBuffer> AllocHGlobal(Type type, int count)
		{
			return Scope.Create(
				() => new UnmanagedBuffer(type, count),
				buf => buf.Release());
		}

		/// <summary>RAII scope for allocated co-task memory</summary>
		public static Scope<IntPtr> AllocCoTaskMem(int size_in_bytes)
		{
			return AllocCoTaskMem(typeof(byte), size_in_bytes);
		}
		public static Scope<IntPtr> AllocCoTaskMem<TStruct>(int count = 1) where TStruct:struct
		{
			return AllocCoTaskMem(typeof(TStruct), count);
		}
		public static Scope<IntPtr> AllocCoTaskMem(Type type, int count)
		{
			return Scope.Create(
				() => Marshal.AllocCoTaskMem(Marshal.SizeOf(type) * count),
				ptr => Marshal.FreeCoTaskMem(ptr));
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to ANSI format if required</summary>
		public static Scope<IntPtr> AllocAnsiString(string str)
		{
			return Scope.Create(
				() => Marshal.StringToHGlobalAnsi(str),
				ptr => Marshal.FreeHGlobal(ptr));
		}

		/// <summary>Convert to/from a structure to non-GC memory. Freeing on dispose</summary>
		public static Scope<IntPtr> StructureToPtr<TStruct>(TStruct strukt) where TStruct:struct
		{
			return Scope.Create(
				() =>
					{
						var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(strukt));
						Marshal.StructureToPtr(strukt, ptr, false);
						return ptr;
					},
				ptr =>
					{
						Marshal.FreeCoTaskMem(ptr);
					});
		}
		public static TStruct PtrToStructure<TStruct>(IntPtr ptr) where TStruct:struct
		{
			return (TStruct)Marshal.PtrToStructure(ptr, typeof(TStruct));
		}

		/// <summary>Copy an array into non-GC memory. Freeing on dispose</summary>
		public static Scope<UnmanagedBuffer> ArrayToPtr<T>(T[] arr) where T:struct
		{
			var scope = AllocHGlobal<T>(arr.Length);
			var elem_sz = Marshal.SizeOf(typeof(T));

			var ptr = scope.Value.Ptr;
			foreach (var elem in arr)
			{
				Marshal.StructureToPtr(elem, ptr, false);
				ptr += elem_sz;
			}

			return scope;
		}
		public static T[] PtrToArray<T>(IntPtr ptr, int count) where T:struct
		{
			var r = new T[count];
			var elem_sz = Marshal.SizeOf(typeof(T));
			for (int i = 0; i != count; ++i)
			{
				r[i] = PtrToStructure<T>(ptr);
				ptr += elem_sz;
			}
			return r;
		}

		/// <summary>Convert an unmanaged function pointer to a delegate</summary>
		public static TFunc PtrToDelegate<TFunc>(IntPtr ptr) where TFunc:class//delegate
		{
			if (!typeof(TFunc).IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException(typeof(TFunc).Name + " is not a delegate type");

			return Marshal.GetDelegateForFunctionPointer(ptr, typeof(TFunc)) as TFunc;
		}

		/// <summary>Pin an object. No copy made</summary>
		public static PinnedObject<T> Pin<T>(T obj)
		{
			return new PinnedObject<T>(obj);
		}
	}
}
