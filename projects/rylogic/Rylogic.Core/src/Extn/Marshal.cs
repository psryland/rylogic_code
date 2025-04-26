using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>Process heaps</summary>
	public enum EHeap
	{
		/// <summary>HGlobal is the is the unmanaged process heap</summary>
		HGlobal,

		/// <summary>CoTaskMem is the COM heap</summary>
		CoTaskMem,
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

		/// <summary>Convenience wrapper for release on IUnknown COM interfaces</summary>
		public static void Release(ref IntPtr iunknown)
		{
			if (iunknown == IntPtr.Zero) return;
			Marshal.Release(iunknown);
			iunknown = IntPtr.Zero;
		}

		/// <summary>RAII scope for allocated global memory</summary>
		public static Scope<UnmanagedBuffer> Alloc(EHeap mem, int size_in_bytes)
		{
			return Alloc(mem, typeof(byte), size_in_bytes);
		}
		public static Scope<UnmanagedBuffer> Alloc<TStruct>(EHeap mem, int count = 1)
			where TStruct : struct
		{
			return Alloc(mem, typeof(TStruct), count);
		}
		public static Scope<UnmanagedBuffer> Alloc<TStruct>(EHeap mem, TStruct init)
			where TStruct : struct
		{
			var buf = Alloc(mem, typeof(TStruct), 1);
			buf.Value.Init(init);
			return buf;
		}
		public static Scope<UnmanagedBuffer> Alloc(EHeap mem, Type type, int count = 1)
		{
			return Scope.Create(
				() => new UnmanagedBuffer(mem, type, count),
				buf => buf.Dispose());
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to UTF8 format if required</summary>
		public static Scope<IntPtr> AllocUTF8String(EHeap mem, string str)
		{
			return Scope.Create(
				() => mem switch
				{
					EHeap.HGlobal => Marshal.StringToHGlobalAnsi(str),
					EHeap.CoTaskMem => Marshal.StringToCoTaskMemAnsi(str),
					_ => throw new Exception($"Unknown global memory type: {mem}"),
				},
				ptr =>
				{
					switch (mem)
					{
					case EHeap.HGlobal: Marshal.FreeHGlobal(ptr); break;
					case EHeap.CoTaskMem: Marshal.FreeCoTaskMem(ptr); break;
					default: throw new Exception($"Unknown global memory type: {mem}");
					}
				});
		}

		/// <summary>Copy a managed string into unmanaged memory, converting to UTF16 format if required</summary>
		public static Scope<IntPtr> AllocUTF16String(EHeap mem, string str)
		{
			return Scope.Create(
				() => mem switch
				{
					EHeap.HGlobal => Marshal.StringToHGlobalUni(str),
					EHeap.CoTaskMem => Marshal.StringToCoTaskMemUni(str),
					_ => throw new Exception($"Unknown global memory type: {mem}"),
				},
				ptr =>
				{
					switch (mem)
					{
					case EHeap.HGlobal: Marshal.FreeHGlobal(ptr); break;
					case EHeap.CoTaskMem: Marshal.FreeCoTaskMem(ptr); break;
					default: throw new Exception($"Unknown global memory type: {mem}");
					}
				});
		}

		/// <summary>Copy an array into non-GC memory. Freeing on dispose</summary>
		public static Scope<UnmanagedBuffer> ArrayToPtr<T>(EHeap mem, T[] arr)
			where T : struct
		{
			var scope = Alloc<T>(mem, arr.Length);
			var elem_sz = Marshal.SizeOf(typeof(T));

			var ptr = scope.Value.Ptr;
			foreach (var elem in arr)
			{
				Marshal.StructureToPtr(elem, ptr, false);
				ptr += elem_sz;
			}

			return scope;
		}
		public static T[] PtrToArray<T>(IntPtr ptr, int count)
			where T : struct
		{
			var r = new T[count];
			var elem_sz = Marshal.SizeOf(typeof(T));
			for (int i = 0; i != count; ++i)
			{
				r[i] = Marshal.PtrToStructure<T>(ptr);
				ptr += elem_sz;
			}
			return r;
		}

		/// <summary>Convert an unmanaged function pointer to a delegate</summary>
		public static TFunc PtrToDelegate<TFunc>(IntPtr ptr)
			where TFunc : class//delegate
		{
			if (!typeof(TFunc).IsSubclassOf(typeof(Delegate)))
				throw new InvalidOperationException($"{typeof(TFunc).Name} is not a delegate type");
			if (ptr == IntPtr.Zero)
				throw new InvalidOperationException($"Pointer cannot be null");
			if (!(Marshal.GetDelegateForFunctionPointer(ptr, typeof(TFunc)) is TFunc func))
				throw new InvalidOperationException($"Cannot convert pointer to {typeof(TFunc).Name} type");

			return func;
		}

		/// <summary>Interpret an (unmanaged) utf8 string pointer to a C# string</summary>
		public static string PtrToStringUTF8(IntPtr ptr, int length)
		{
			if (ptr == IntPtr.Zero)
				return null!;

			#if NETCOREAPP3_1_OR_GREATER
			return Marshal.PtrToStringUTF8(ptr, length);
			#else
			var bytes = new byte[length];
			Marshal.Copy(ptr, bytes, 0, length);
			return Encoding.UTF8.GetString(bytes);
			#endif
		}
		public static string PtrToStringUTF8(IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null!;

			// Read up to the null character
			int len = 0;  for (; Marshal.ReadByte(ptr, len) != 0; ++len) { }
			return PtrToStringUTF8(ptr, len);
		}

		/// <summary>Interpret an (unmanaged) utf16 string pointer to a C# string</summary>
		public static string PtrToStringUTF16(IntPtr ptr, int length)
		{
			if (ptr == IntPtr.Zero)
				return null!;

			#if NETCOREAPP3_1_OR_GREATER
			return Marshal.PtrToStringUni(ptr, length);
			#else
			var bytes = new byte[2 * length];
			Marshal.Copy(ptr, bytes, 0, length);
			return Encoding.Unicode.GetString(bytes);
			#endif
		}
		public static string PtrToStringUTF16(IntPtr ptr)
		{
			if (ptr == IntPtr.Zero)
				return null!;

			// Read up to the null character
			int len = 0;  for (; Marshal.ReadInt16(ptr, len) != 0; ++len) { }
			return PtrToStringUTF16(ptr, len);
		}

		/// <summary>Inverse of PtrToStringUTF8</summary>
		public static PinnedObject<byte[]> StringToPtrUTF8(string str)
		{
			var bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
			return Pin(bytes, GCHandleType.Pinned);
		}

		/// <summary>Pin an object. No copy made. 'type' Pinned only works if 'obj' contains primitive/blittable data</summary>
		public static PinnedObject<T> Pin<T>(T obj, GCHandleType type)
			where T : class
		{
			return new PinnedObject<T>(obj, type);
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

	/// <summary>Unmanaged memory buffer</summary>
	public sealed class UnmanagedBuffer :IDisposable
	{
		public UnmanagedBuffer(EHeap mem, Type ty, int count)
		{
			Type = mem;
			Length = Marshal.SizeOf(ty) * count;
			Ptr = Type switch
			{
				EHeap.CoTaskMem => Marshal.AllocCoTaskMem(Length),
				EHeap.HGlobal => Marshal.AllocHGlobal(Length),
				_ => throw new Exception($"Unknown global memory type: {Type}"),
			};
		}
		public void Dispose()
		{
			if (Ptr == IntPtr.Zero) return;
			switch (Type)
			{
			case EHeap.HGlobal: Marshal.FreeHGlobal(Ptr); break;
			case EHeap.CoTaskMem: Marshal.FreeCoTaskMem(Ptr); break;
			default: throw new Exception($"Unknown global memory type: {Type}");
			}
			Ptr = IntPtr.Zero;
			Length = 0;
			GC.SuppressFinalize(this);
		}

		public EHeap Type;
		public IntPtr Ptr;
		public int Length;

		/// <summary>Init the buffer from a structure</summary>
		public void Init<TStruct>(TStruct init, bool replacing_valid_data = false)
			where TStruct : struct
		{
			if (Ptr == IntPtr.Zero) throw new Exception($"Unmanaged buffer pointer is null");
			if (Length < Marshal.SizeOf<TStruct>()) throw new Exception($"Unmanaged buffer is too small to be this structure type");
			Marshal.StructureToPtr(init, Ptr, replacing_valid_data);
		}

		/// <summary>Interpret the buffer as a structure type</summary>
		public TStruct As<TStruct>()
			where TStruct : struct
		{
			if (Ptr == IntPtr.Zero) throw new Exception($"Unmanaged buffer pointer is null");
			if (Length < Marshal.SizeOf<TStruct>()) throw new Exception($"Unmanaged buffer is too small to be this structure type");
			return Marshal.PtrToStructure<TStruct>(Ptr);
		}
	}

	/// <summary>A helper class for pinning a managed structure so that it is suitable for unmanaged calls.</summary>
	public sealed class PinnedObject<T> :IDisposable
		where T : class
	{
		// Notes:
		//  - A pinned object will not be collected and will not be moved by the GC until explicitly freed.
		//  - GCHandleType.Normal is used to pass a managed object through native callbacks to be accessed later in managed code.
		//  - GCHandleType.Pinned is used to pass a pointer to a managed buffer into unmanaged code.
		// Examples:
		//   GCHandleType.Pinned:
		//      Passing a vertex buffer to view3d:
		//      var verts = new Vertex[1000];
		//      using var vbuf = Marshal_.Pin(verts, GCHandle.Pinned);
		//      View3D_ObjectCreate(..., verts.Length, ..., vbuf.Pointer, ...);
		//
		//   GCHandleType.Normal:
		//      Passing 'this' to WM_CREATE through CreateWindowEx:
		//      using var pin = Marshal_.Pin(this, GCHandleType.Normal);
		//      Win32.CreateWindow(0, atom, title, 0, 0, 0, 1, 1, Win32.HWND_MESSAGE, IntPtr.Zero, HInstance, pin.Pointer);
		//      ...
		//      if (message == Win32.WM_CREATE) // in WndProc
		//      {
		//          var cp = Marshal.PtrToStructure<Win32.CREATESTRUCT>(lparam);
		//          var gc = GCHandle.FromIntPtr(cp.lpCreateParams);
		//          var wnd = (Thing?)gc.Target ?? throw new Exception("'this' pointer must be provided in CreateWindowEx"));
		//      }

		//private readonly T m_managed_object;
		private readonly GCHandleType m_type;
		private readonly GCHandle m_handle;
		private bool m_disposed;

		public PinnedObject(T obj, GCHandleType type)
		{
			m_type = type;
			//m_managed_object = obj;
			m_handle = GCHandle.Alloc(obj, type);
			Pointer = type switch
			{
				GCHandleType.Weak => (IntPtr)m_handle,
				GCHandleType.Normal => (IntPtr)m_handle,
				GCHandleType.Pinned => m_handle.AddrOfPinnedObject(),
				_ => throw new Exception($"Don't know how to get the pointer for pin type {type}"),
			};
		}
		public void Dispose()
		{
			if (m_disposed) return;
			m_handle.Free();
			m_disposed = true;
			GC.SuppressFinalize(this);
		}
		~PinnedObject()
		{
			Dispose();
		}

		/// <summary>
		/// Pointer to the pinned object (if GCHandleType is pinned),
		/// or an pointer to the GCHandle itself (if GCHandleType is normal).</summary>
		public IntPtr Pointer { get; }

		/// <summary>The managed object being pinned</summary>
		public T ManangedObject
		{
			get => (T?)m_handle.Target ?? throw new Exception("Pinned object is null");
			set
			{
				switch (m_type)
				{
					case GCHandleType.Pinned: Marshal.StructureToPtr(value, Pointer, false); break;
					default: throw new Exception("Only 'GCHandle.Pinned' objects can be replaced");
				}
			}
		}
	}
}
