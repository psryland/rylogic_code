using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rylogic.Utility;

namespace Rylogic.Common
{
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

	/// <summary>A helper class for pinning a managed structure so that it is suitable for unmanaged calls.</summary>
	public class PinnedObject<T> :IDisposable
	{
		// Notes:
		// A pinned object will not be collected and will not be moved by the GC until explicitly freed.

		protected T        m_managed_object;
		protected GCHandle m_handle;
		protected IntPtr   m_ptr;
		protected bool     m_disposed;

		public PinnedObject(T obj)
			:this(obj, GCHandleType.Pinned)
		{}
		public PinnedObject(T obj, GCHandleType type)
		{
			m_managed_object = obj;
			m_handle = GCHandle.Alloc(obj, type);
			m_ptr = m_handle.AddrOfPinnedObject();
		}
		~PinnedObject()
		{
			Dispose();
		}
		public void Dispose()
		{
			if (!m_disposed)
			{
				m_handle.Free();
				m_ptr = IntPtr.Zero;
				m_disposed = true;
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>The managed object being pinned</summary>
		public T ManangedObject
		{
			[DebuggerStepThrough] get { return (T)m_handle.Target; }
			set { Marshal.StructureToPtr(value, m_ptr, false); }
		}

		/// <summary>Pointer to the pinned object</summary>
		public IntPtr Pointer
		{
			[DebuggerStepThrough] get { return m_ptr; }
		}
	}

	/// <summary>Extra methods for GCHandle</summary>
	public static class GCHandle_
	{
		/// <summary>RAII scope for an allocated GC handle</summary>
		public static Scope Alloc(object obj)
		{
			return Alloc(obj, GCHandleType.Normal);
		}

		/// <summary>RAII scope for an allocated GC handle</summary>
		public static Scope Alloc(object obj, GCHandleType type)
		{
			return new Scope(obj, type);
		}

		/// <summary>RAII wrapper for 'GCHandle_'</summary>
		public class Scope :Utility.Scope
		{
			public GCHandle Handle;
			public Scope(object obj, GCHandleType type)
			{
				Init(
					() => Handle = GCHandle.Alloc(obj, type),
					() => { if (Handle.IsAllocated) Handle.Free(); });
			}
		}
	}
}
