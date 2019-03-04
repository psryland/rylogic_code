using System;
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

		static D3D11Image()
		{
			WindowOwnerProperty = DependencyProperty.Register(nameof(WindowOwner), typeof(IntPtr), typeof(D3D11Image), new UIPropertyMetadata(IntPtr.Zero, new PropertyChangedCallback(WindowOwnerChanged)));
		}
		public D3D11Image()
			:this(96.0, 96.0)
		{}
		public D3D11Image(double dpiX, double dpiY, int multi_sampling = 1)
			: base(dpiX, dpiY)
		{
			MultiSampling = multi_sampling;
		}
		public virtual void Dispose()
		{
			FrontBuffer = null;
			RenderTarget = null;
			WindowOwner = IntPtr.Zero;
			GC.SuppressFinalize(this);
		}
		~D3D11Image()
		{
			Dispose();
		}
		protected override Freezable CreateInstanceCore()
		{
			return new D3D11Image();
		}

		/// <summary>The window handle (HWND) of the window that hosts the D3D11Image</summary>
		public IntPtr WindowOwner
		{
			get { return (IntPtr)(GetValue(WindowOwnerProperty)); }
			set { SetValue(WindowOwnerProperty, value); }
		}
		public static DependencyProperty WindowOwnerProperty;
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

		/// <summary>The render target scaling factor for multi-sampling. Use 1, 2, 4, or 8</summary>
		public int MultiSampling
		{
			get { return m_multi_sampling; }
			set
			{
				if (m_multi_sampling == value) return;
				m_multi_sampling = value;
				TryCreateRenderTarget();
			}
		}
		private int m_multi_sampling;

		/// <summary>The Dx9 render target that matches the area on screen</summary>
		private View3d.Texture FrontBuffer
		{
			get { return m_front_buffer; }
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
						SetBackBuffer(D3DResourceType.IDirect3DSurface9, ptr);
				}
			}
		}
		private View3d.Texture m_front_buffer;
		
		/// <summary>The Dx11 render target texture</summary>
		public View3d.Texture RenderTarget
		{
			get { return m_render_target ?? FrontBuffer; }
			set
			{
				if (m_render_target == value) return;
				if (value == FrontBuffer)
					throw new Exception("Don't assign the front buffer as the render target");

				Util.Dispose(ref m_render_target);
				m_render_target = value;
				RenderTargetChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private View3d.Texture m_render_target;
		private int m_pixel_width = 16;
		private int m_pixel_height = 16;

		/// <summary>Raised when the render target changes</summary>
		public event EventHandler RenderTargetChanged;

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
			// Copy from the staging resource to the dx9 render target
			if (FrontBuffer != RenderTarget)
			{
				// Copy from the staging render target to the front buffer
				//View3d.Texture.StretchBlt(RenderTarget, FrontBuffer);
			}

			base.AddDirtyRect(area);
		}

		/// <summary>RAII Scope for locking the d3d image</summary>
		public Scope LockScope()
		{
			return Scope.Create(() => Lock(), () => Unlock());
		}

		/// <summary>Create a new render target if possible</summary>
		private void TryCreateRenderTarget()
		{
			// Cannot create the render target until a window handle has been assigned
			if (WindowOwner == IntPtr.Zero)
				return;

			try
			{
				// Create the Dx9 render target
				var opts0 = View3d.TextureOptions.GdiCompat(dbg_name: "D3D11Image RenderTarget FB");
				var rt0 = View3d.Texture.Dx9RenderTarget(WindowOwner, m_pixel_width, m_pixel_height, opts0);
				FrontBuffer = rt0;

				// Create the Dx11 staging render target
				if (MultiSampling <= 1)
				{
					RenderTarget = null;
				}
				else
				{
					var opts1 = View3d.TextureOptions.GdiCompat(dbg_name: "D3D11Image RenderTarget BB");
					var rt1 = new View3d.Texture(m_pixel_width * MultiSampling, m_pixel_height * MultiSampling, opts1);
					RenderTarget = rt1;
				}
			}
			catch { }
		}

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
