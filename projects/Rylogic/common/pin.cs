// (c) 2007 Marc Clifton

using System;
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

		public T ManangedObject
		{
			get { return (T)m_handle.Target; }
			set { Marshal.StructureToPtr(value, m_ptr, false); }
		}

		public IntPtr Pointer
		{
			get { return m_ptr; }
		}

		public PinnedObject(T obj)
		{
			m_managed_object = obj;
			m_handle = GCHandle.Alloc(m_managed_object, GCHandleType.Pinned);
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
			}
		}
	}
}
