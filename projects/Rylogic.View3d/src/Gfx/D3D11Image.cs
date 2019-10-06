using System;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Rylogic.Utility;

namespace Rylogic.Gfx
{
	public class D3D11Image : D3DImage, IDisposable
	{
		// Notes:
		//  - This is a re-imaged implementation of Microsoft.Wpf.DirectX.Interop.D3D11Image.
		//  - D3DImage does not own the back buffer it uses. It expects the caller to provide the
		//    back buffer and manage its lifetime. This class *does* manage the back buffer because
		//    it needs to be created as a shared Dx9 render target which is then opened as a Dx11
		//    texture.
		//  - Dx9 cannot be used without a window handle.
		//  - PixelWidth/PixelHeight are the pixel dimensions of the render target. These sizes are
		//    cached in this object so that they can be set before the render target is created.
		//  - The FrontBuffer is a texture with the same dimensions as the target image size.
		//    'RenderTarget' is a texture that may be larger so that anti aliasing can be supported.
		//
		//  Usage:
		//   Assign an instance of this object as the Source of a WPF Image control.
		//   Attach a handler to RenderTargetChanged
		//      Assign the render target to your View3d.Window using SetRT and SaveAsMainRT
		//      This window will be an off-screen window.
		//   Assign the window handle as soon as it's available.
		//      In a window class, use OnSourceInitialized.
		//          d3d_image.WindowOwner = new WindowInteropHelper(this).Handle;
		//      In a custom control, use Loaded/Unloaded with:
		//          var win = System.Windows.Window.GetWindow(this);
		//          d3d_image.WindowOwner = new WindowInteropHelper(win).Handle;
		//  When the Image changes size, use:
		//     var win = PresentationSource.FromVisual(image)?.CompositionTarget as HwndTarget;
		//     var dpi_scale = win?.TransformToDevice.M11 ?? 1.0;
		//     var width  = Math.Max(1, (int)Math.Ceiling(image.Width * dpi_scale));
		//     var height = Math.Max(1, (int)Math.Ceiling(image.Height * dpi_scale));
		//     d3d_image.SetRenderTargetSize(width, height);

		~D3D11Image()
		{
			Dispose(false);
		}
		public D3D11Image()
			: this(96.0, 96.0)
		{ }
		public D3D11Image(double dpiX, double dpiY, int multi_sampling = 4)
			: base(dpiX, dpiY)
		{
			MultiSampling = multi_sampling;
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			RenderTarget = null;
			FrontBuffer = null;
			WindowOwner = IntPtr.Zero;
		}
		protected override Freezable CreateInstanceCore()
		{
			return new D3D11Image();
		}

		/// <summary>The window handle (HWND) of the window that hosts the D3D11Image</summary>
		public IntPtr WindowOwner
		{
			get => (IntPtr)GetValue(WindowOwnerProperty);
			set => SetValue(WindowOwnerProperty, value);
		}
		private static void WindowOwnerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var image = (D3D11Image)sender;
			if (args.OldValue is IntPtr old_hwnd && old_hwnd != IntPtr.Zero)
			{
				// The window handle is changing. Tear-down Dx9
				image.FrontBuffer = null;
				image.RenderTarget = null;
			}
			if (args.NewValue is IntPtr new_hwnd && new_hwnd != IntPtr.Zero)
			{
				// A window handle has been assigned. Setup the Dx9 render target
				image.TryCreateRenderTarget();
			}
		}
		public static DependencyProperty WindowOwnerProperty = DependencyProperty.Register(nameof(WindowOwner), typeof(IntPtr), typeof(D3D11Image), new UIPropertyMetadata(IntPtr.Zero, new PropertyChangedCallback(WindowOwnerChanged)));

		/// <summary>The render target multi-sampling</summary>
		public int MultiSampling
		{
			get => m_multi_sampling;
			set
			{
				if (Equals(m_multi_sampling, value)) return;
				m_multi_sampling = value;
				TryCreateRenderTarget();
			}
		}
		private int m_multi_sampling;

		/// <summary>The Dx11 render target texture</summary>
		public View3d.Texture? RenderTarget
		{
			get => m_render_target;
			set
			{
				if (m_render_target == value) return;
				Util.Dispose(ref m_render_target);
				m_render_target = value;

				// Notify of a new render target
				RenderTargetChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private View3d.Texture? m_render_target;
		private int m_pixel_width = 16;
		private int m_pixel_height = 16;

		/// <summary>The Dx9 render target that matches the area on screen</summary>
		private View3d.Texture? FrontBuffer
		{
			get => m_front_buffer;
			set
			{
				if (m_front_buffer == value) return;
				if (m_front_buffer != null)
				{
					// Remove the render target
					using (LockScope())
						SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);

					Util.Dispose(ref m_front_buffer);
				}
				m_front_buffer = value;
				if (m_front_buffer != null)
				{
					var ptr = m_front_buffer.PrivateDataPointer[View3d.Texture.UserData.Surface0Pointer];

					// Set the render target as the Dx9 surface
					using (LockScope())
						SetBackBuffer(D3DResourceType.IDirect3DSurface9, ptr, true);
				}
			}
		}
		private View3d.Texture? m_front_buffer;

		/// <summary>Raised when the render target changes</summary>
		public event EventHandler? RenderTargetChanged;

		/// <summary>Set the dimensions of the render target</summary>
		public void SetRenderTargetSize(int pixel_width, int pixel_height)
		{
			// Cache these values so that the render target can be created at a later time.
			m_pixel_width = pixel_width;
			m_pixel_height = pixel_height;

			// If the render target size has changed, recreate it
			if (FrontBuffer == null ||
				PixelWidth != m_pixel_width ||
				PixelHeight != m_pixel_height)
				TryCreateRenderTarget();
		}

		/// <summary>Invalidate the entire surface</summary>
		public void Invalidate()
		{
			Invalidate(new Int32Rect(0, 0, m_pixel_width, m_pixel_height));
		}
		public void Invalidate(Int32Rect area)
		{
			if (FrontBuffer == null || RenderTarget == null)
				return;

			using (LockScope())
			{
				View3d.Texture.ResolveAA(FrontBuffer, RenderTarget);
				View3d.Flush();
				base.AddDirtyRect(area);
			}
		}

		/// <summary>RAII Scope for locking the d3d image</summary>
		private Scope LockScope()
		{
			return Scope.Create(() => Lock(), () => Unlock());
		}

		/// <summary>Create a new render target if possible</summary>
		private void TryCreateRenderTarget()
		{
			// Cannot create the render target until a window handle
			// has been assigned because Dx9 requires a window handle.
			if (WindowOwner == IntPtr.Zero)
				return;

			try
			{
				var opts = View3d.TextureOptions.New(
					format: View3d.EFormat.DXGI_FORMAT_B8G8R8A8_UNORM,
					mips: 1,
					bind_flags: View3d.EBindFlags.D3D11_BIND_RENDER_TARGET | View3d.EBindFlags.D3D11_BIND_SHADER_RESOURCE,
					dbg_name: "D3D11Image RenderTarget FB");

				// Create the Dx9 render target of the required size;
				var rt0 = View3d.Texture.Dx9RenderTarget(WindowOwner, m_pixel_width, m_pixel_height, opts);
				FrontBuffer = rt0;

				// Add multi-sampling for the main render target
				opts.MultiSamp = (uint)MultiSampling;
				opts.DbgName = "D3D11Image RenderTarget BB";

				// Create the Dx11 staging render target
				var rt1 = new View3d.Texture(m_pixel_width, m_pixel_height, opts);
				RenderTarget = rt1;
			}
			catch { }
		}

		/// <summary>True if the front buffer is available</summary>
		public new bool IsFrontBufferAvailable => base.IsFrontBufferAvailable || (bool)m_is_sw_fallback_enabled.GetValue(this)!;
		private static readonly FieldInfo m_is_sw_fallback_enabled = typeof(D3DImage).GetField("_isSoftwareFallbackEnabled", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException("'D3DImage._isSoftwareFallbackEnabled' is missing");

		/// <summary></summary>
		private new void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer, bool enableSoftwareFallback)
		{
			// Hide the back buffer method because this object controls setting the back buffer
			base.SetBackBuffer(backBufferType, backBuffer, enableSoftwareFallback);
		}
		private new void SetBackBuffer(D3DResourceType backBufferType, IntPtr backBuffer)
		{
			// Hide the back buffer method because this object controls setting the back buffer
			base.SetBackBuffer(backBufferType, backBuffer);
		}
	}
}
