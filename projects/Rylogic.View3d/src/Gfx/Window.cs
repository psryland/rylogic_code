//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Rylogic.Attrib;
using Rylogic.Common;
using Rylogic.Extn.Windows;
using Rylogic.Maths;
using Rylogic.Utility;
using HWindow = System.IntPtr;
using HObject = System.IntPtr;
using HWND = System.IntPtr;
using System.Linq;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>Binds a 3D scene to a window</summary>
		public sealed class Window :IDisposable
		{
			private readonly WindowOptions m_opts;              // The options used to create the window (contains references to the user provided error call back)
			private readonly SettingsChangedCB m_settings_cb;   // A local reference to prevent the callback being garbage collected
			private readonly InvalidatedCB m_invalidated_cb;    // A local reference to prevent the callback being garbage collected
			private readonly RenderCB m_render_cb;              // A local reference to prevent the callback being garbage collected
			private readonly SceneChangedCB m_scene_changed_cb; // A local reference to prevent the callback being garbage collected
			private readonly AnimationCB m_animation_cb;        // A local reference to prevent the callback being garbage collected

			public Window(View3d view, HWND hwnd, bool? gdi_compatible_backbuffer = null, int? multi_sampling = null, string? dbg_name = null)
			{
				View = view;
				Hwnd = hwnd;
				Diag = new Diagnostics(this);
				m_opts = new WindowOptions
				{
					ErrorCB = HandleError,
					ErrorCBCtx = IntPtr.Zero,
					GdiCompatibleBackBuffer = gdi_compatible_backbuffer ?? false,
					Multisampling = multi_sampling ?? 4,
					DbgName = dbg_name ?? string.Empty,
				};

				// Create the window
				Handle = View3D_WindowCreate(hwnd, ref m_opts);
				if (Handle == null) throw new Exception("Failed to create View3D window");
				void HandleError(IntPtr ctx, string msg, string filepath, int line, long pos) => Error?.Invoke(this, new ErrorEventArgs(msg, filepath, line, pos));

				// Set up a callback for when settings are changed
				View3D_WindowSettingsChangedCB(Handle, m_settings_cb = HandleSettingsChanged, IntPtr.Zero, true);
				void HandleSettingsChanged(IntPtr ctx, HWindow wnd, ESettings setting) => OnSettingsChanged?.Invoke(this, new SettingChangeEventArgs(setting));

				// Set up a callback for when the window is invalidated
				View3D_WindowInvalidatedCB(Handle, m_invalidated_cb = HandleInvalidated, IntPtr.Zero, true);
				void HandleInvalidated(IntPtr ctx, HWindow wnd) => OnInvalidated?.Invoke(this, EventArgs.Empty);

				// Set up a callback for when a render is about to happen
				View3D_WindowRenderingCB(Handle, m_render_cb = HandleRendering, IntPtr.Zero, true);
				void HandleRendering(IntPtr ctx, HWindow wnd) => OnRendering?.Invoke(this, EventArgs.Empty);

				// Set up a callback for when the object store for this window changes
				View3d_WindowSceneChangedCB(Handle, m_scene_changed_cb = HandleSceneChanged, IntPtr.Zero, true);
				void HandleSceneChanged(IntPtr ctx, HWindow wnd, ref View3DSceneChanged args) => OnSceneChanged?.Invoke(this, new SceneChangedEventArgs(args));

				// Set up a callback for animation events
				View3D_WindowAnimEventCBSet(Handle, m_animation_cb = HandleAnimationEvent, IntPtr.Zero, true);
				void HandleAnimationEvent(IntPtr ctx, HWindow wnd, EAnimCommand command, double clock) => OnAnimationEvent?.Invoke(this, new AnimationEventArgs(command, clock));

				// Set up the light source
				SetLightSource(v4.Origin, -v4.ZAxis, true);

				// Display the focus point
				FocusPointVisible = true;

				// Position the camera
				Camera = new Camera(this);
				Camera.Lookat(new v4(0f, 0f, -2f, 1f), v4.Origin, v4.YAxis);
			}
			public void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (Handle == HWindow.Zero) return;
				View3D_WindowAnimControl(Handle, EAnimCommand.Stop, 0.0);
				View3D_WindowAnimEventCBSet(Handle, m_animation_cb, IntPtr.Zero, false);
				View3d_WindowSceneChangedCB(Handle, m_scene_changed_cb, IntPtr.Zero, false);
				View3D_WindowRenderingCB(Handle, m_render_cb, IntPtr.Zero, false);
				View3D_WindowInvalidatedCB(Handle, m_invalidated_cb, IntPtr.Zero, false);
				View3D_WindowSettingsChangedCB(Handle, m_settings_cb, IntPtr.Zero, false);
				View3D_WindowDestroy(Handle);
				Handle = HWindow.Zero;
			}

			/// <summary>Event call on errors. Note: can be called in a background thread context</summary>
			public event EventHandler<ErrorEventArgs>? Error;

			/// <summary>Event notifying whenever rendering settings have changed</summary>
			public event EventHandler<SettingChangeEventArgs>? OnSettingsChanged;

			/// <summary>Raised when Invalidate is called</summary>
			public event EventHandler? OnInvalidated;

			/// <summary>Event notifying of a render about to happen</summary>
			public event EventHandler? OnRendering;

			/// <summary>Event notifying whenever objects are added/removed from the scene (Not raised during Render however)</summary>
			public event EventHandler<SceneChangedEventArgs>? OnSceneChanged;

			/// <summary>Event notifying of state changes for animation</summary>
			public event EventHandler<AnimationEventArgs>? OnAnimationEvent;

			/// <summary>Triggers the 'OnInvalidated' event on the first call. Future calls are ignored until 'Present' or 'Validate' are called</summary>
			public void Invalidate()
			{
				View3D_Invalidate(Handle, false);
			}
			public void Validate()
			{
				View3D_Validate(Handle);
			}

			/// <summary>The associated view3d object</summary>
			public View3d View { get; }

			/// <summary>Get the view3d native handle for the window (Note: this is not the HWND)</summary>
			public HWindow Handle { get; private set; }

			/// <summary>Get the windows handle associated with this window (Note: Null if using off-screen rendering only)</summary>
			public HWND Hwnd { get; }

			/// <summary>Camera controls</summary>
			public Camera Camera { get; }

			/// <summary>
			/// Mouse navigation and/or object manipulation.
			/// 'point' is a point in client space.
			/// 'btns' is the state of the mouse buttons (MK_LBUTTON etc)
			/// 'nav_op' is the logical navigation operation to perform.
			/// 'nav_beg_or_end' should be true on mouse button down or up, and false during mouse movement
			/// Returns true if the scene requires refreshing</summary>
			public bool MouseNavigate(PointF point, EMouseBtns btns, ENavOp nav_op, bool nav_beg_or_end)
			{
				// This function is not in the CameraControls object because it is not solely used
				// for camera navigation. It can also be used to manipulate objects in the scene.
				if (m_in_mouse_navigate != 0) return false;
				using (Scope.Create(() => ++m_in_mouse_navigate, () => --m_in_mouse_navigate))
				{
					// Notify of navigating, allowing client code to make
					// changes or optionally handle the mouse event.
					var args = new MouseNavigateEventArgs(point, btns, nav_op, nav_beg_or_end);
					MouseNavigating?.Invoke(this, args);
					if (args.Handled)
						return false;

					// The mouse event wasn't handled, so forward to the window for navigation
					return View3D_MouseNavigate(Handle, v2.From(args.Point), args.NavOp, args.NavBegOrEnd);
				}
			}
			public bool MouseNavigate(PointF point, EMouseBtns btns, bool nav_beg_or_end)
			{
				var op = Camera.MouseBtnToNavOp(btns);
				return MouseNavigate(point, btns, op, nav_beg_or_end);
			}
			private int m_in_mouse_navigate;

			/// <summary>
			/// Zoom using the mouse.
			/// 'point' is a point in client rect space.
			/// 'delta' is the mouse wheel scroll delta value (i.e. 120 = 1 click)
			/// 'along_ray' is true if the camera should move along a ray cast through 'point'
			/// Returns true if the scene requires refreshing</summary>
			public bool MouseNavigateZ(PointF point, EMouseBtns btns, float delta, bool along_ray)
			{
				if (m_in_mouse_navigate != 0) return false;
				using (Scope.Create(() => ++m_in_mouse_navigate, () => --m_in_mouse_navigate))
				{
					// Notify of navigating, allowing client code to make
					// changes or optionally handle the mouse event.
					var args = new MouseNavigateEventArgs(point, btns, delta, along_ray);
					MouseNavigating?.Invoke(this, args);
					if (args.Handled)
						return false;

					// The mouse event wasn't handled, so forward to the window for navigation
					return View3D_MouseNavigateZ(Handle, v2.From(args.Point), args.Delta, args.AlongRay);
				}
			}

			/// <summary>
			/// Direct camera relative navigation or manipulation.
			/// Returns true if the scene requires refreshing</summary>
			public bool Navigate(float dx, float dy, float dz)
			{
				// This function is not in the CameraControls object because it is not solely used
				// for camera navigation. It can also be used to manipulate objects in the scene.
				return View3D_Navigate(Handle, dx, dy, dz);
			}

			/// <summary>Raised just before a mouse navigation happens</summary>
			public event EventHandler<MouseNavigateEventArgs>? MouseNavigating;

			/// <summary>Perform a hit test in the scene</summary>
			public HitTestResult HitTest(HitTestRay ray, float snap_distance, EHitTestFlags flags)
			{
				return HitTest(ray, snap_distance, flags, new Guid[0], 0, 0);
			}
			public HitTestResult HitTest(HitTestRay ray, float snap_distance, EHitTestFlags flags, Guid[] guids, int include_count, int exclude_count)
			{
				Debug.Assert(include_count + exclude_count == guids.Length);
				var rays = new HitTestRay[1] { ray };
				var hits = new HitTestResult[1];

				using var rays_buf = Marshal_.Pin(rays, GCHandleType.Pinned);
				using var hits_buf = Marshal_.Pin(hits, GCHandleType.Pinned);
				using var guids_buf = Marshal_.Pin(guids, GCHandleType.Pinned);
				View3D_WindowHitTestByCtx(Handle, rays_buf.Pointer, hits_buf.Pointer, 1, snap_distance, flags, guids_buf.Pointer, include_count, exclude_count);

				return hits[0];
			}
			public HitTestResult HitTest(HitTestRay ray, float snap_distance, EHitTestFlags flags, IEnumerable<Object> objects)
			{
				var rays = new HitTestRay[1] { ray };
				var hits = new HitTestResult[1];
				var insts = objects.Select(x => x.Handle).ToArray();

				using var rays_buf = Marshal_.Pin(rays, GCHandleType.Pinned);
				using var hits_buf = Marshal_.Pin(hits, GCHandleType.Pinned);
				using var insts_buf = Marshal_.Pin(insts, GCHandleType.Pinned);
				View3D_WindowHitTestObjects(Handle, rays_buf.Pointer, hits_buf.Pointer, 1, snap_distance, flags, insts, insts.Length);

				return hits[0];
			}

			/// <summary>Get the render target texture</summary>
			public Texture RenderTarget => new Texture(View3D_TextureRenderTarget(Handle), owned:false);

			/// <summary>The size of the render target (in pixels)</summary>
			public Size RenderTargetSize => RenderTarget?.Info is ImageInfo info ? new Size((int)info.m_width, (int)info.m_height) : Size.Empty;

			/// <summary>The DPI of the monitor this window is on</summary>
			public PointF DpiScale => View3D_WindowDpiScale(Handle).ToPointF();

			/// <summary>Import/Export a settings string</summary>
			public string Settings
			{
				get => View3D_WindowSettingsGet(Handle) ?? string.Empty;
				set => View3D_WindowSettingsSet(Handle, value);
			}

			/// <summary>Enumerate the guids associated with this window</summary>
			public void EnumGuids(Action<Guid> cb)
			{
				EnumGuids(guid => { cb(guid); return true; });
			}
			public void EnumGuids(Func<Guid, bool> cb)
			{
				View3D_WindowEnumGuids(Handle, (c,guid) => cb(guid), IntPtr.Zero);
			}

			/// <summary>Enumerate the objects associated with this window</summary>
			public void EnumObjects(Action<Object> cb)
			{
				EnumObjects(obj => { cb(obj); return true; });
			}
			public void EnumObjects(Func<Object, bool> cb)
			{
				View3D_WindowEnumObjects(Handle, (c,obj) => cb(new Object(obj)), IntPtr.Zero);
			}
			public void EnumObjects(Action<Object> cb, Guid context_id, bool all_except = false)
			{
				EnumObjects(obj => { cb(obj); return true; }, context_id, all_except);
			}
			public void EnumObjects(Func<Object, bool> cb, Guid context_id, bool all_except = false)
			{
				EnumObjects(cb, new [] { context_id }, all_except?0:1, all_except?1:0);
			}
			public void EnumObjects(Action<Object> cb, Guid[] context_ids, int include_count, int exclude_count)
			{
				EnumObjects(obj => { cb(obj); return true; }, context_ids, include_count, exclude_count);
			}
			public void EnumObjects(Func<Object, bool> cb, Guid[] context_ids, int include_count, int exclude_count)
			{
				Debug.Assert(include_count + exclude_count == context_ids.Length);
				EnumObjectsCB enum_cb = (c,obj) => cb(new Object(obj));
				using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
				View3D_WindowEnumObjectsById(Handle, enum_cb, IntPtr.Zero, ids.Pointer, include_count, exclude_count);
			}

			/// <summary>Return the objects associated with this window</summary>
			public Object[] Objects
			{
				get
				{
					var objs = new List<Object>();
					EnumObjects(x => objs.Add(x));
					return objs.ToArray();
				}
			}

			/// <summary>Add an object to the window</summary>
			public void AddObject(Object obj)
			{
				Util.BreakIf(!Math_.FEql(obj.O2P.w.w, 1f), "Invalid instance transform");
				Util.BreakIf(!Math_.IsFinite(obj.O2P), "Invalid instance transform");
				View3D_WindowAddObject(Handle, obj.Handle);
			}

			/// <summary>Add a gizmo to the window</summary>
			public void AddGizmo(Gizmo giz)
			{
				Util.BreakIf(!Math_.FEql(giz.O2W.w.w, 1f), "Invalid instance transform");
				Util.BreakIf(!Math_.IsFinite(giz.O2W), "Invalid instance transform");
				View3D_WindowAddGizmo(Handle, giz.m_handle);
			}

			/// <summary>Add multiple objects, filtered by 'context_ids</summary>
			public void AddObjects(Guid context_id)
			{
				AddObjects(new[] { context_id }, 1, 0);
			}
			public void AddObjects(Guid[] context_ids, int include_count, int exclude_count)
			{
				Debug.Assert(include_count + exclude_count == context_ids.Length);
				using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
				View3D_WindowAddObjectsById(Handle, ids.Pointer, include_count, exclude_count);
			}

			/// <summary>Add a collection of objects to the window</summary>
			public void AddObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					AddObject(obj);
			}

			/// <summary>Remove an object from the window</summary>
			public void RemoveObject(Object obj)
			{
				View3D_WindowRemoveObject(Handle, obj.Handle);
			}

			/// <summary>Remove a gizmo from the window</summary>
			public void RemoveGizmo(Gizmo giz)
			{
				View3D_WindowRemoveGizmo(Handle, giz.m_handle);
			}

			/// <summary>Remove a collection of objects from the window</summary>
			public void RemoveObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					RemoveObject(obj);
			}

			/// <summary>Remove multiple objects, filtered by 'context_ids'</summary>
			public void RemoveObjects(Guid[] context_ids, int include_count, int exclude_count)
			{
				Debug.Assert(include_count + exclude_count == context_ids.Length);
				using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
				View3D_WindowRemoveObjectsById(Handle, ids.Pointer, include_count, exclude_count);
			}

			/// <summary>Remove all instances from the window</summary>
			public void RemoveAllObjects()
			{
				View3D_WindowRemoveAllObjects(Handle);
			}

			/// <summary>Return the number of objects in a window</summary>
			public int ObjectCount => View3D_WindowObjectCount(Handle);

			/// <summary>True if 'obj' is a member of this window</summary>
			public bool HasObject(Object obj, bool search_children)
			{
				return View3D_WindowHasObject(Handle, obj.Handle, search_children);
			}
			[Obsolete("Use HasObject")] public bool Contains(Object obj, bool search_children) => HasObject(obj, search_children);

			/// <summary>Return a bounding box of the objects in this window</summary>
			public BBox SceneBounds(ESceneBounds bounds, Guid[]? except = null)
			{
				return View3D_WindowSceneBounds(Handle, bounds, except?.Length ?? 0, except);
			}

			/// <summary>Show/Hide the focus point</summary>
			public bool FocusPointVisible
			{
				get => View3D_FocusPointVisibleGet(Handle);
				set => View3D_FocusPointVisibleSet(Handle, value);
			}

			/// <summary>Set the size of the focus point graphic</summary>
			public float FocusPointSize
			{
				set => View3D_FocusPointSizeSet(Handle, value);
			}

			/// <summary>Show/Hide the origin point</summary>
			public bool OriginPointVisible
			{
				get => View3D_OriginVisibleGet(Handle);
				set => View3D_OriginVisibleSet(Handle, value);
			}

			/// <summary>Set the size of the origin graphic</summary>
			public float OriginPointSize
			{
				set => View3D_OriginSizeSet(Handle, value);
			}

			/// <summary>Get/Set whether the selection box is visible</summary>
			public bool SelectionBoxVisible
			{
				get => View3D_SelectionBoxVisibleGet(Handle);
				set => View3D_SelectionBoxVisibleSet(Handle, value);
			}

			/// <summary>Set the position of the selection box</summary>
			public void SelectionBoxPosition(BBox box, m4x4 o2w)
			{
				View3D_SelectionBoxPosition(Handle, ref box, ref o2w);
			}

			/// <summary>Set the size and position of the selection box to bound the selected objects in this view</summary>
			public void SelectionBoxFitToSelected()
			{
				View3D_SelectionBoxFitToSelected(Handle);
			}

			/// <summary>Get/Set the render mode</summary>
			public EFillMode FillMode
			{
				get => View3D_FillModeGet(Handle);
				set => View3D_FillModeSet(Handle, value);
			}

			/// <summary>Get/Set the face culling mode</summary>
			public ECullMode CullMode
			{
				get => View3D_CullModeGet(Handle);
				set => View3D_CullModeSet(Handle, value);
			}

			/// <summary>Get/Set the multi-sampling level for the window</summary>
			public int MultiSampling
			{
				get => View3D_MultiSamplingGet(Handle);
				set => View3D_MultiSamplingSet(Handle, value);
			}

			/// <summary>Get/Set the light properties. Note returned value is a value type</summary>
			public LightInfo LightProperties
			{
				get => View3D_LightPropertiesGet(Handle, out var light) ? light : default;
				set => View3D_LightPropertiesSet(Handle, ref value);
			}

			/// <summary>Show the lighting dialog</summary>
			public void ShowLightingDlg()
			{
				View3D_LightShowDialog(Handle);
			}

			/// <summary>Set the single light source</summary>
			public void SetLightSource(v4 position, v4 direction, bool camera_relative)
			{
				View3D_LightSource(Handle, position, direction, camera_relative);
			}

			/// <summary>Show/Hide the measuring tool</summary>
			public bool ShowMeasureTool
			{
				get => View3D_MeasureToolVisible(Handle);
				set => View3D_ShowMeasureTool(Handle, value);
			}

			/// <summary>Show/Hide the angle tool</summary>
			public bool ShowAngleTool
			{
				get => View3D_AngleToolVisible(Handle);
				set => View3D_ShowAngleTool(Handle, value);
			}

			/// <summary>The background colour for the window</summary>
			public Colour32 BackgroundColour
			{
				get => new Colour32(View3D_BackgroundColourGet(Handle));
				set => View3D_BackgroundColourSet(Handle, value.ARGB);
			}

			/// <summary>Get/Set the animation clock</summary>
			public double AnimTime
			{
				get => View3D_WindowAnimTimeGet(Handle);
				set => View3D_WindowAnimTimeSet(Handle, value);
			}

			/// <summary>Is animation currently running</summary>
			public bool Animating => View3D_WindowAnimating(Handle);

			/// <summary>Control animation</summary>
			public void AnimControl(EAnimCommand command, double time_s = 0.0)
			{
				View3D_WindowAnimControl(Handle, command, time_s);
			}

			/// <summary>Cause the window to be rendered. Remember to call Present when done</summary>
			public void Render()
			{
				View3D_Render(Handle);
			}

			/// <summary>Called to flip the back buffer to the screen after all window have been rendered</summary>
			public void Present()
			{
				View3D_Present(Handle);
			}

			/// <summary>Get/Set the size of the backbuffer (in pixels)</summary>
			[Browsable(false)]
			public Size BackBufferSize
			{
				get
				{
					// You might be after RenderTargetSize instead...
					Util.BreakIf(Hwnd == IntPtr.Zero, "There is no back buffer when used in window-less mode");
					View3D_BackBufferSizeGet(Handle, out var w, out var h);
					return new Size(w, h);
				}
				set
				{
					Util.BreakIf(Hwnd == IntPtr.Zero, "There is no back buffer when used in window-less mode");
					Util.BreakIf(value.Width == 0 || value.Height == 0, "Invalid back buffer size");
					Util.BreakIf(!Math_.IsFinite(value.Width) || !Math_.IsFinite(value.Height), "Invalid back buffer size");
					View3D_BackBufferSizeSet(Handle, value.Width, value.Height);
				}
			}

			/// <summary>Restore the render target as the main output</summary>
			public void RestoreRT()
			{
				View3D_RenderTargetRestore(Handle);
			}

			/// <summary>
			/// Render the current scene into 'render_target'. If no 'depth_buffer' is given a temporary one will be created.
			/// Note: Make sure the render target is not used as a texture for an object in the scene to be rendered.
			/// Either remove that object from the scene, or detach the texture from the object. 'render_target' cannot be
			/// a source and destination texture at the same time</summary>
			public void SetRT(Texture? render_target, Texture? depth_buffer, bool is_new_main_rt)
			{
				View3D_RenderTargetSet(Handle, render_target?.Handle ?? IntPtr.Zero, depth_buffer?.Handle ?? IntPtr.Zero, is_new_main_rt);
			}

			/// <summary>Get/Set the size/position of the viewport within the render target</summary>
			public Viewport Viewport
			{
				get => View3D_Viewport(Handle);
				set
				{
					Util.BreakIf(value.Width == 0 || value.Height == 0, "Invalid viewport size");
					Util.BreakIf(!Math_.IsFinite(value.Width) || !Math_.IsFinite(value.Height), "Invalid viewport size");
					View3D_SetViewport(Handle, value);
				}
			}

			/// <summary>Get/Set whether the depth buffer is enabled</summary>
			public bool DepthBufferEnabled
			{
				get => View3D_DepthBufferEnabledGet(Handle);
				set => View3D_DepthBufferEnabledSet(Handle, value);
			}

			/// <summary>Convert a screen space point to a normalised point</summary>
			public v2 SSPointToNSSPoint(v2 screen)
			{
				return View3D_SSPointToNSSPoint(Handle, screen);
			}

			/// <summary>Convert a normalised point into a screen space point</summary>
			public v2 ScreenSpacePoint(v2 pt)
			{
				// Notes:
				//  - What does screen space mean if there is no back buffer?
				//    For WPF, or other window-less modes of use, there is no back buffer. I'm assuming here that the render target
				//    is the psuedo-back buffer. In the case were there is a back buffer but a temporary render target is in use, I'm
				//    using the back buffer still as I'm assuming that is still the screen size.
				//  - The render target dimension is in pixels however, and WPF uses device independent units (i.e. 96dpi).
				//    So to return points in device-independent screen space, the render target size needs to be adjusted
				//    by the DPI.
				var dpi_scale = DpiScale;
				var da = Hwnd != IntPtr.Zero ? BackBufferSize : RenderTargetSize;
				return new v2(
					(pt.x + 1f) * da.Width / (2f * dpi_scale.X),
					(1f - pt.y) * da.Height / (2f * dpi_scale.Y));
			}

			/// <summary>Standard keyboard shortcuts. 'key_code' corresponds to VK_KEY</summary>
			public bool TranslateKey(EKeyCodes key_code)
			{
				return View3D_TranslateKey(Handle, key_code);
			}

			/// <summary>Handy method for creating random objects</summary>
			public Guid CreateDemoScene()
			{
				return View3D_DemoSceneCreate(Handle);

#if false
				{// Create an object using ldr script
				    HObject obj = ObjectCreate("*Box ldr_box FFFF00FF {1 2 3}");
				    DrawsetAddObject(obj);
				}

				{// Create a box model, an instance for it, and add it to the window
				    HModel model = CreateModelBox(new v4(0.3f, 0.2f, 0.4f, 0f), m4x4.Identity, 0xFFFF0000);
				    HInstance instance = CreateInstance(model, m4x4.Identity);
				    AddInstance(window, instance);
				}

				{// Create a mesh
				    // Mesh data
				    Vertex[] vert = new Vertex[]
				    {
				        new Vertex(new v4( 0f, 0f, 0f, 1f), v4.ZAxis, 0xFFFF0000, v2.Zero),
				        new Vertex(new v4( 0f, 1f, 0f, 1f), v4.ZAxis, 0xFF00FF00, v2.Zero),
				        new Vertex(new v4( 1f, 0f, 0f, 1f), v4.ZAxis, 0xFF0000FF, v2.Zero),
				    };
				    ushort[] face = new ushort[]
				    {
				        0, 1, 2
				    };

				    HModel model = CreateModel(vert.Length, face.Length, vert, face, EPrimType.D3DPT_TRIANGLELIST);
				    HInstance instance = CreateInstance(model, m4x4.Identity);
				    AddInstance(window, instance);
				}
#endif
			}

			/// <summary>Show a window containing and example ldr script file</summary>
			public void ShowExampleScript()
			{
				View3D_DemoScriptShow(Handle);
			}

			/// <summary>Show/Hide the object manager UI</summary>
			public void ShowObjectManager(bool show)
			{
				View3D_ObjectManagerShow(Handle, show);
			}

			/// <summary>Diagnostic settings</summary>
			public Diagnostics Diag { get; }

			/// <summary>Namespace for diagnostic settings</summary>
			public class Diagnostics
			{
				private readonly Window m_window;
				internal Diagnostics(Window window)
				{
					m_window = window;
				}

				/// <summary>Get/Set whether object bounding boxes are visible</summary>
				public bool BBoxesVisible
				{
					get => View3D_DiagBBoxesVisibleGet(m_window.Handle);
					set => View3D_DiagBBoxesVisibleSet(m_window.Handle, value);
				}

				/// <summary>Get/Set the length of vertex normals (when visible)</summary>
				public float NormalsLength
				{
					get => View3D_DiagNormalsLengthGet(m_window.Handle);
					set => View3D_DiagNormalsLengthSet(m_window.Handle, value);
				}

				/// <summary>Get/Set the length of vertex normals (when visible)</summary>
				public Colour32 NormalsColour
				{
					get => View3D_DiagNormalsColourGet(m_window.Handle);
					set => View3D_DiagNormalsColourSet(m_window.Handle, value);
				}

				/// <summary>Get/Set the size of the points in EFillMode::Points mode</summary>
				public v2 FillModePointsSize
				{
					get => View3D_DiagFillModePointsSizeGet(m_window.Handle);
					set => View3D_DiagFillModePointsSizeSet(m_window.Handle, value);
				}
			}

			#region Equals
			public static bool operator == (Window? lhs, Window? rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Window? lhs, Window? rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Window? rhs)
			{
				return rhs != null && Handle == rhs.Handle;
			}
			public override bool Equals(object? rhs)
			{
				return Equals(rhs as Window);
			}
			public override int GetHashCode()
			{
				return Handle.GetHashCode();
			}
			#endregion
		}
	}
}
