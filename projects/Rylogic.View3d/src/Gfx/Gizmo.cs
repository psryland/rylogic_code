//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rylogic.Maths;
using Rylogic.Utility;
using HGizmo = System.IntPtr;
using HObject = System.IntPtr;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>A 3D manipulation gizmo</summary>
		public sealed class Gizmo :IDisposable
		{
			// Use:
			//  Create a gizmo, attach objects or other gizmos to it,
			//  add it to a window to make it visible, enable it to have
			//  it watch for mouse interaction. Moving the gizmo automatically
			//  moves attached objects as well. MouseControl of the gizmo
			//  is provided by the window it's been added to

			public enum EMode { Translate, Rotate, Scale, };
			public enum EEvent { StartManip, Moving, Commit, Revert };

			public HGizmo m_handle; // The handle to the gizmo
			private Callback m_cb;
			private bool m_owned;   // True if 'm_handle' was created with this class

			public Gizmo(EMode mode, m4x4 o2w)
			{
				m_handle = View3D_GizmoCreate(mode, ref o2w);
				if (m_handle == IntPtr.Zero) throw new Exception("View3D.Gizmo creation failed");
				m_owned = true;
				View3D_GizmoAttachCB(m_handle, m_cb = HandleGizmoMoved, IntPtr.Zero);
			}
			public Gizmo(HGizmo handle)
			{
				if (handle == IntPtr.Zero) throw new ArgumentNullException("handle");
				m_handle = handle;
				m_owned = false;
				View3D_GizmoAttachCB(m_handle, m_cb = HandleGizmoMoved, IntPtr.Zero);
			}
			public void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (m_handle == HObject.Zero) return;
				View3D_GizmoDetachCB(m_handle, m_cb);
				if (m_owned) View3D_GizmoDelete(m_handle);
				m_cb = null!;
				m_handle = HObject.Zero;
				GC.SuppressFinalize(this);
			}

			/// <summary>Get/Set whether the gizmo is looking for mouse interaction</summary>
			public bool Enabled
			{
				get => View3D_GizmoEnabled(m_handle);
				set => View3D_GizmoSetEnabled(m_handle, value);
			}

			/// <summary>True while manipulation is in progress</summary>
			public bool Manipulating => View3D_GizmoManipulating(m_handle);

			/// <summary>Get/Set the mode of the gizmo between translate, rotate, scale</summary>
			public EMode Mode
			{
				get => View3D_GizmoGetMode(m_handle);
				set => View3D_GizmoSetMode(m_handle, value);
			}

			/// <summary>Get/Set the scale of the gizmo</summary>
			public float Scale
			{
				get => View3D_GizmoScaleGet(m_handle);
				set => View3D_GizmoScaleSet(m_handle, value);
			}

			/// <summary>Get/Set the gizmo object to world transform (scale is allowed)</summary>
			public m4x4 O2W
			{
				get => View3D_GizmoGetO2W(m_handle);
				set => View3D_GizmoSetO2W(m_handle, ref value);
			}

			/// <summary>
			/// Get the offset transform that represents the difference between the gizmo's
			/// transform at the start of manipulation and now</summary>
			public m4x4 Offset => View3D_GizmoGetOffset(m_handle);

			/// <summary>Raised whenever the gizmo is manipulated</summary>
			public event EventHandler<MovedEventArgs>? Moved;
			public class MovedEventArgs : EventArgs
			{
				/// <summary>The type of movement event this is</summary>
				public EEvent Type { get; private set; }

				[DebuggerStepThrough]
				public MovedEventArgs(EEvent type)
				{
					Type = type;
				}
			}

			/// <summary>Attach an object directly to the gizmo that will move with it</summary>
			public void Attach(Object obj)
			{
				View3D_GizmoAttach(m_handle, obj.Handle);
			}

			/// <summary>Detach an object from the gizmo</summary>
			public void Detach(Object obj)
			{
				View3D_GizmoDetach(m_handle, obj.Handle);
			}

			/// <summary>Handle the callback from the native code for when the gizmo moves</summary>
			private void HandleGizmoMoved(HGizmo ctx, ref Evt_Gizmo args)
			{
				if (args.m_gizmo != m_handle) throw new Exception("Gizmo move event from a different gizmo instance received");
				Moved?.Invoke(this, new MovedEventArgs(args.m_state));
			}

			/// <summary>Callback function type and data from the native gizmo object</summary>
			internal delegate void Callback(IntPtr ctx, ref Evt_Gizmo args);

			[StructLayout(LayoutKind.Sequential)]
			internal struct Evt_Gizmo
			{
				public IntPtr m_gizmo;
				public EEvent m_state;
			}

			#region Equals
			public static bool operator ==(Gizmo? lhs, Gizmo? rhs)
			{
				return ReferenceEquals(lhs, rhs) || Equals(lhs, rhs);
			}
			public static bool operator !=(Gizmo? lhs, Gizmo? rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Gizmo? rhs)
			{
				return rhs != null && m_handle == rhs.m_handle;
			}
			public override bool Equals(object? rhs)
			{
				return Equals(rhs as Gizmo);
			}
			public override int GetHashCode()
			{
				return m_handle.GetHashCode();
			}
			#endregion
		}
	}
}
