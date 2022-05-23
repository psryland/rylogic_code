﻿//#define PR_VIEW3D_CREATE_STACKTRACE
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>Namespace for the camera controls</summary>
		[DebuggerDisplay("{Description,nq}")]
		public class Camera
		{
			private readonly Window m_window;
			public Camera(Window window)
			{
				m_window = window;
			}
			public Camera(Window window, XElement node)
				: this(window)
			{
				Load(node);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(O2W), O2W, false);
				node.Add2(nameof(FocusDist), FocusDist, false);
				node.Add2(nameof(Orthographic), Orthographic, false);
				node.Add2(nameof(Aspect), Aspect, false);
				node.Add2(nameof(FovY), FovY, false);
				node.Add2(nameof(AlignAxis), AlignAxis, false);
				return node;
			}
			public void Load(XElement node)
			{
				O2W = node.Element(nameof(O2W)).As<m4x4>(m4x4.Identity);
				FocusDist = node.Element(nameof(FocusDist)).As<float>(FocusDist);
				Orthographic = node.Element(nameof(Orthographic)).As<bool>(Orthographic);
				Aspect = node.Element(nameof(Aspect)).As<float>(Aspect);
				FovY = node.Element(nameof(FovY)).As<float>(FovY);
				AlignAxis = node.Element(nameof(AlignAxis)).As<v4>(AlignAxis);
				Commit();
			}

			/// <summary>Return the world space size of the camera view area at 'dist' in front of the camera</summary>
			public v2 ViewArea(float dist)
			{
				return View3D_ViewArea(m_window.Handle, dist);
			}
			public v2 ViewArea()
			{
				return ViewArea(FocusDist);
			}

			/// <summary>Get/Set Orthographic projection mode</summary>
			public bool Orthographic
			{
				get => View3D_CameraOrthographicGet(m_window.Handle);
				set => View3D_CameraOrthographicSet(m_window.Handle, value);
			}

			/// <summary>Get/Set the camera align axis (camera up axis). Zero vector means no align axis is set</summary>
			public v4 AlignAxis
			{
				get => View3D_CameraAlignAxisGet(m_window.Handle);
				set => View3D_CameraAlignAxisSet(m_window.Handle, value);
			}

			/// <summary>Get/Set the camera view aspect ratio = Width/Height</summary>
			public float Aspect
			{
				get => View3D_CameraAspectGet(m_window.Handle);
				set
				{
					if (value <= 0 || value > 1.0e10) throw new Exception("Aspect must be > 0 && < 1.0e10");
					View3D_CameraAspectSet(m_window.Handle, value);
				}
			}

			/// <summary>Get/Set the camera horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa</summary>
			public float FovX
			{
				get => View3D_CameraFovXGet(m_window.Handle);
				set
				{
					if (value <= 0 || value > Math_.TauBy2 - Math_.TinyF) throw new Exception("FovX must be < tau/2");
					View3D_CameraFovXSet(m_window.Handle, value);
				}
			}

			/// <summary>Get/Set the camera vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
			public float FovY
			{
				get => View3D_CameraFovYGet(m_window.Handle);
				set
				{
					if (value <= 0 || value > Math_.TauBy2 - Math_.TinyF) throw new Exception("FovY must be < tau/2");
					View3D_CameraFovYSet(m_window.Handle, value);
				}
			}

			/// <summary>Set both the X and Y field of view (i.e. change the aspect ratio)</summary>
			public void SetFov(float fovX, float fovY)
			{
				if (fovX <= 0 || fovX > Math_.TauBy2 - Math_.TinyF) throw new Exception("FovX must be < tau/2");
				if (fovY <= 0 || fovY > Math_.TauBy2 - Math_.TinyF) throw new Exception("FovY must be < tau/2");
				View3D_CameraFovSet(m_window.Handle, fovX, fovY);
			}

			/// <summary>Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'</summary>
			public void BalanceFov(float fov)
			{
				View3D_CameraBalanceFov(m_window.Handle, fov);
			}

			/// <summary>Get/Set the camera near plane distance. Focus relative</summary>
			public float NearPlane
			{
				get => ClipPlanes(true).Near;
				set => ClipPlanes(value, FarPlane, true);
			}

			/// <summary>Get/Set the camera far plane distance. Focus relative</summary>
			public float FarPlane
			{
				get => ClipPlanes(true).Far;
				set => ClipPlanes(NearPlane, value, true);
			}

			/// <summary>Get/Set the camera clip plane distances</summary>
			public (float Near, float Far) ClipPlanes(bool focus_relative)
			{
				View3D_CameraClipPlanesGet(m_window.Handle, out var near, out var far, focus_relative);
				return (near, far);
			}
			public void ClipPlanes(float near, float far, bool focus_relative)
			{
				View3D_CameraClipPlanesSet(m_window.Handle, near, far, focus_relative);
			}

			/// <summary>Get/Set the position of the camera focus point (in world space, relative to the world origin)</summary>
			public v4 FocusPoint
			{
				get { View3D_CameraFocusPointGet(m_window.Handle, out var pos); return pos; }
				set => View3D_CameraFocusPointSet(m_window.Handle, value);
			}

			/// <summary>Get/Set the distance to the camera focus point</summary>
			public float FocusDist
			{
				get => View3D_CameraFocusDistanceGet(m_window.Handle);
				set => View3D_CameraFocusDistanceSet(m_window.Handle, value >= 0 ? value : throw new Exception("Focus distance cannot be negative"));
			}

			/// <summary>Get/Set the camera to world transform. Note: use SetPosition to set the focus distance at the same time</summary>
			public m4x4 O2W
			{
				get { View3D_CameraToWorldGet(m_window.Handle, out var c2w); return c2w; }
				set => View3D_CameraToWorldSet(m_window.Handle, ref value);
			}
			public m4x4 W2O => Math_.InvertFast(O2W);

			/// <summary>Set the current O2W transform as the reference point</summary>
			public void Commit()
			{
				View3D_CameraCommit(m_window.Handle);
			}

			/// <summary>Set the camera to world transform and focus distance.</summary>
			public void Lookat(v4 position, v4 lookat, v4 up)
			{
				View3D_CameraPositionSet(m_window.Handle, position, lookat, up);
			}

			/// <summary>Set the camera position such that it's still looking at the current focus point</summary>
			public void SetPosition(v4 position)
			{
				var up = AlignAxis;
				if (up.LengthSq == 0f) up = v4.YAxis;
				Lookat(position, FocusPoint, up);
			}

			/// <summary>Set the camera fields of view (H and V) and focus distance such that a rectangle (w/h) exactly fills the view</summary>
			public void SetView(float width, float height, float dist)
			{
				View3D_CameraViewRectSet(m_window.Handle, width, height, dist);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera directions 'forward' and 'up'</summary>
			public void ResetView(v4? forward = null, v4? up = null, float dist = 0f, bool preserve_aspect = true, bool commit = true)
			{
				up ??= AlignAxis;
				if (up.Value.LengthSq == 0f)
					up = v4.YAxis;

				forward ??= up.Value.z > up.Value.y ? -v4.YAxis : -v4.ZAxis;
				if (Math_.Parallel(up.Value, forward.Value))
					up = Math_.Perpendicular(forward.Value);

				View3D_ResetView(m_window.Handle, forward.Value, up.Value, dist, preserve_aspect, commit);
			}

			/// <summary>Reset the camera to view a bbox</summary>
			public void ResetView(BBox bbox, v4? forward = null, v4? up = null, float dist = 0f, bool preserve_aspect = true, bool commit = true)
			{
				up ??= AlignAxis;
				if (up.Value.LengthSq == 0f)
					up = v4.YAxis;

				forward ??= up.Value.z > up.Value.y ? -v4.YAxis : -v4.ZAxis;
				if (Math_.Parallel(up.Value, forward.Value))
					up = Math_.Perpendicular(forward.Value);

				View3D_ResetViewBBox(m_window.Handle, bbox, forward.Value, up.Value, dist, preserve_aspect, commit);
			}

			/// <summary>Reset the zoom factor to 1f</summary>
			public void ResetZoom()
			{
				View3D_CameraResetZoom(m_window.Handle);
			}

			/// <summary>Get/Set the camera FOV zoom</summary>
			public float Zoom
			{
				get { return View3D_CameraZoomGet(m_window.Handle); }
				set { View3D_CameraZoomSet(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera movement lock mask</summary>
			public ECameraLockMask LockMask
			{
				get { return View3D_CameraLockMaskGet(m_window.Handle); }
				set { View3D_CameraLockMaskSet(m_window.Handle, value); }
			}

			/// <summary>
			/// Return a point in world space corresponding to a normalised screen space point.
			/// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
			/// The z component should be the world space distance from the camera.</summary>
			public v4 NSSPointToWSPoint(v4 screen)
			{
				return View3D_NSSPointToWSPoint(m_window.Handle, screen);
			}
			public v4 SSPointToWSPoint(v2 screen)
			{
				var nss = SSPointToNSSPoint(screen);
				return NSSPointToWSPoint(new v4(nss.x, nss.y, FocusDist, 1.0f));
			}

			/// <summary>Return the normalised screen space point corresponding to a screen space point</summary>
			public v2 SSPointToNSSPoint(v2 screen)
			{
				return m_window.SSPointToNSSPoint(screen);
			}

			/// <summary>
			/// Return a point in normalised screen space corresponding to 'world'
			/// The returned 'z' component will be the world space distance from the camera.</summary>
			public v4 WSPointToNSSPoint(v4 world)
			{
				return View3D_WSPointToNSSPoint(m_window.Handle, world);
			}
			public v2 WSPointToSSPoint(v4 world)
			{
				var nss = WSPointToNSSPoint(world);
				return m_window.ScreenSpacePoint(nss.xy);
			}

			/// <summary>
			/// Return a screen space vector that is the world space line a->b
			/// projected onto the screen.</summary>
			public v2 WSVecToSSVec(v4 s, v4 e)
			{
				return WSPointToSSPoint(e) - WSPointToSSPoint(s);
			}

			/// <summary>
			/// Return a world space vector that is the screen space line a->b
			/// at the focus depth from the camera.</summary>
			public v4 SSVecToWSVec(v2 s, v2 e)
			{
				var nss_s = SSPointToNSSPoint(s);
				var nss_e = SSPointToNSSPoint(e);
				var z = FocusDist;
				return
					NSSPointToWSPoint(new v4(nss_e.x, nss_e.y, z, 1.0f)) -
					NSSPointToWSPoint(new v4(nss_s.x, nss_s.y, z, 1.0f));
			}

			/// <summary>
			/// Convert a normalised screen space point into a ray from the camera in world space.
			/// 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left to upper right).
			/// The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive).
			/// 'ws_point' will be the camera position in world space.
			/// 'ws_direction' will be the world space direction vector that passes through 'screen' at the depth 'screen.z'.</summary>
			public void NSSPointToWSRay(v4 screen, out v4 ws_point, out v4 ws_direction)
			{
				View3D_NSSPointToWSRay(m_window.Handle, screen, out ws_point, out ws_direction);
			}

			/// <summary>
			/// Convert a screen space point into a world space ray from the camera.
			/// The ray will pass though the screen space point at the focus distance of the camera.</summary>
			public void SSPointToWSRay(PointF screen, out v4 ws_point, out v4 ws_direction)
			{
				var nss = SSPointToNSSPoint(screen);
				NSSPointToWSRay(new v4(nss.x, nss.y, FocusDist, 1.0f), out ws_point, out ws_direction);
			}

			/// <summary>A ray cast from the camera into the scene through client space point 'point_cs'</summary>
			public HitTestRay RaySS(v2 point_ss)
			{
				var ray = new HitTestRay();
				SSPointToWSRay(point_ss, out ray.m_ws_origin, out ray.m_ws_direction);
				return ray;
			}

			/// <summary>Convert a mouse button to the default navigation operation</summary>
			public static ENavOp MouseBtnToNavOp(EMouseBtns mk)
			{
				return View3D_MouseBtnToNavOp(mk);
			}

			/// <summary></summary>
			private string Description => $"{O2W.pos} FPoint={FocusPoint} FDist={FocusDist}";

			#region Equals
			public static bool operator ==(Camera? lhs, Camera? rhs)
			{
				return ReferenceEquals(lhs, rhs) || Equals(lhs, rhs);
			}
			public static bool operator !=(Camera? lhs, Camera? rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Camera? rhs)
			{
				return rhs != null && m_window == rhs.m_window;
			}
			public override bool Equals(object? obj)
			{
				return Equals(obj as Camera);
			}
			public override int GetHashCode()
			{
				return m_window.GetHashCode();
			}
			#endregion
		}
	}
}
