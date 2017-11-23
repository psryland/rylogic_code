// (c) 2007 Marc Clifton

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace pr.common
{
	/// <summary>
	/// A helper class for pinning a managed structure so that it is suitable
	/// for unmanaged calls. A pinned object will not be collected and will not be moved
	/// by the GC until explicitly freed.</summary>
	public class PinnedObject<T> :IDisposable
	{
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
}
