using System;
using Rylogic.Maths;

namespace Rylogic.Physics;

/// <summary>Owns one generation-aware native collision shape within an engine.</summary>
public sealed class Shape :IDisposable
{
	private ShapeHandle m_handle;

	/// <summary>Adopt a shape created by the owning engine.</summary>
	internal Shape(Engine engine, ShapeHandle handle)
	{
		Engine = engine;
		m_handle = handle;
	}

	/// <summary>The engine that owns this shape.</summary>
	internal Engine Engine { get; }

	/// <summary>True after this wrapper no longer owns a native shape slot.</summary>
	public bool IsDisposed
	{
		get
		{
			return m_handle.IsNull;
		}
	}

	/// <summary>The stable typed identity used by bodies, compounds, snapshots, and checkpoints.</summary>
	public ShapeHandle Handle
	{
		get
		{
			if (m_handle.IsNull)
				throw new ObjectDisposedException(nameof(Shape));

			return m_handle;
		}
	}

	/// <summary>
	/// Replace this leaf shape's placement within the bodies that use it. Every body sharing the shape sees the change;
	/// compounds built from it keep their own copy. Mass properties are unchanged, and sleeping bodies must be woken by the caller.
	/// </summary>
	public unsafe void SetShapeToRoot(m4x4 shape_to_root)
	{
		// The native engine updates the shape and its packed collision data in place.
		Engine.EnsureOwner();
		Native.Check(Native.Physics_ShapeSetTransform(Engine.Handle, Handle.Value, &shape_to_root));
	}

	/// <summary>Destroy this shape after all bodies referencing it have been destroyed.</summary>
	public void Dispose()
	{
		if (m_handle.IsNull)
			return;

		Engine.EnsureOwner();
		Native.Check(Native.Physics_ShapeDestroy(Engine.Handle, m_handle.Value));
		m_handle = default;
		Engine.Remove(this);
		GC.SuppressFinalize(this);
	}

	/// <summary>Invalidate this wrapper after its owning engine destroys the native shape registry.</summary>
	internal void ReleaseFromEngine()
	{
		if (m_handle.IsNull)
			return;

		m_handle = default;
		Engine.Remove(this);
		GC.SuppressFinalize(this);
	}

}
