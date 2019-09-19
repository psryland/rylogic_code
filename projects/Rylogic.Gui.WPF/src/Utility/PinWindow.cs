using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>Anchor points on the pin target</summary>
	public enum EPin
	{
		TopLeft,
		TopCentre,
		TopRight,
		BottomLeft,
		BottomCentre,
		BottomRight,
		CentreLeft,
		Centre,
		CentreRight,
	}

	/// <summary>The window supports pinning</summary>
	public interface IPinnable
	{
		PinData PinState { get; }
	}

	/// <summary>Provides the functionality for pinning a window to another</summary>
	public class PinData :IDisposable
	{
		// Notes:
		//  - Dispose is called when 'window' is closed 
		//
		// Usage:
		//  In your window class:
		//    ctor()
		//    {
		//        // Dispose is handled by 'PinData'
		//        PinState = new PinData(this, EPin.Centre);
		//    }
		//
		//    /// <summary>Support pinning this window</summary>
		//    private PinData PinState { get; }
		//

		public PinData(Window window, EPin pin_site = EPin.Centre, bool pinned = true)
		{
			PinSite = pin_site;
			PinWindow = window;
			Pinned = pinned;
		}
		public void Dispose()
		{
			Pinned = false;
			PinTarget = null;
			PinWindow = null;
		}

		/// <summary>Pin or Unpin the window from the target</summary>
		public bool Pinned
		{
			get { return m_pinned; }
			set
			{
				if (m_pinned == value) return;
				m_pinned = value;
				RecordOffset();
				UpdatePinMenuCheckState();
			}
		}
		private bool m_pinned;

		/// <summary>The point on the target window that we move relative to</summary>
		public EPin PinSite
		{
			get { return m_pin_site; }
			set
			{
				if (m_pin_site == value) return;
				m_pin_site = value;
			}
		}
		private EPin m_pin_site;

		/// <summary>The window whose position we're controlling</summary>
		private Window PinWindow
		{
			get { return m_pin_window; }
			set
			{
				if (m_pin_window == value) return;
				if (m_pin_window != null)
				{
					m_pin_window.Loaded -= HandleLoaded;
					m_pin_window.LocationChanged -= HandleMoved;
					m_pin_window.SizeChanged -= HandleMoved;
					m_pin_window.Closed -= HandleClosed;

					// Remove the menu item from the system menu
					PinOptionInSysMenu(false);
				}
				m_pin_window = value;
				if (m_pin_window != null)
				{
					// Add a menu item to the system menu
					PinOptionInSysMenu(true);

					m_pin_window.Closed += HandleClosed;
					m_pin_window.SizeChanged += HandleMoved;
					m_pin_window.LocationChanged += HandleMoved;
					m_pin_window.Loaded += HandleLoaded;
				}

				// Handlers
				void HandleLoaded(object sender, EventArgs e)
				{
					// On first load, set the initial position based on the pin location
					switch (PinSite)
					{
					default: throw new Exception($"Unknown pin location '{PinSite}'");
					case EPin.TopLeft:      PinOffset = new Vector(-PinWindow.ActualWidth    , 0); break;
					case EPin.TopCentre:    PinOffset = new Vector(-PinWindow.ActualWidth / 2, 0); break;
					case EPin.TopRight:     PinOffset = new Vector(0                         , 0); break;
					case EPin.BottomLeft:   PinOffset = new Vector(-PinWindow.ActualWidth    , -PinWindow.ActualHeight); break;
					case EPin.BottomCentre: PinOffset = new Vector(-PinWindow.ActualWidth / 2, -PinWindow.ActualHeight); break;
					case EPin.BottomRight:  PinOffset = new Vector(0                         , -PinWindow.ActualHeight); break;
					case EPin.CentreLeft:   PinOffset = new Vector(-PinWindow.ActualWidth    , -PinWindow.ActualHeight / 2); break;
					case EPin.Centre:       PinOffset = new Vector(-PinWindow.ActualWidth / 2, -PinWindow.ActualHeight / 2); break;
					case EPin.CentreRight:  PinOffset = new Vector(0                         , -PinWindow.ActualHeight / 2); break;
					}
				}
				void HandleMoved(object sender, EventArgs e)
				{
					// When the location of the controlled window changes,
					// record the offset from the target window
					RecordOffset();
				}
				void HandleClosed(object sender, EventArgs e)
				{
					Dispose();
				}
			}
		}
		private Window m_pin_window;

		/// <summary>The window to position relative to (defaults to the Owner of PinWindow)</summary>
		public Window PinTarget
		{
			get
			{
				// If a target has not been set explicitly, use the PinWindow's owner
				if (m_pin_target == null)
					PinTarget = PinWindow.Owner;

				return m_pin_target;
			}
			set
			{
				if (m_pin_target == value) return;
				if (m_pin_target != null)
				{
					m_pin_target.LocationChanged -= HandleMoved;
					m_pin_target.SizeChanged -= HandleMoved;
				}
				m_pin_target = value;
				if (m_pin_target != null)
				{
					m_pin_target.SizeChanged += HandleMoved;
					m_pin_target.LocationChanged += HandleMoved;
				}

				UpdatePinMenuCheckState();

				// Handler
				void HandleMoved(object sender, EventArgs e)
				{
					// When the target window moves, update the position of the pinned window.
					SignalUpdateLocation();
				}
			}
		}
		private Window m_pin_target;

		/// <summary>The offset from the PinTarget</summary>
		public Vector PinOffset
		{
			get { return m_pin_offset; }
			set
			{
				if (m_pin_offset == value) return;
				m_pin_offset = value;
				UpdateLocation();
			}
		}
		private Vector m_pin_offset;

		/// <summary>The window handle of the pinned window</summary>
		private IntPtr PinWindowHandle { get; set; }

		/// <summary>Record the offset from the target window</summary>
		private void RecordOffset()
		{
			if (!Pinned || PinWindow == null || PinTarget == null)
				return;

			if (m_block_updates != 0) return;
			using (Scope.Create(() => ++m_block_updates, () => --m_block_updates))
			{
				// Measure the offset from the reference point
				var rect = new Rect(PinTarget.Left, PinTarget.Top, PinTarget.ActualWidth, PinTarget.ActualHeight);
				var pt = new Point(PinWindow.Left, PinWindow.Top);
				switch (PinSite)
				{
				default: throw new Exception($"Unknown pin site '{PinSite}'");
				case EPin.TopLeft:      PinOffset = new Vector(pt.X - rect.Left                    , pt.Y - rect.Top); break;
				case EPin.TopCentre:    PinOffset = new Vector(pt.X - (rect.Left + rect.Right) / 2 , pt.Y - rect.Top); break;
				case EPin.TopRight:     PinOffset = new Vector(pt.X - rect.Right                   , pt.Y - rect.Top); break;
				case EPin.BottomLeft:   PinOffset = new Vector(pt.X - rect.Left                    , pt.Y - rect.Bottom); break;
				case EPin.BottomCentre: PinOffset = new Vector(pt.X - (rect.Left + rect.Right) / 2 , pt.Y - rect.Bottom); break;
				case EPin.BottomRight:  PinOffset = new Vector(pt.X - rect.Right                   , pt.Y - rect.Bottom); break;
				case EPin.CentreLeft:   PinOffset = new Vector(pt.X - rect.Left                    , pt.Y - (rect.Top + rect.Bottom) / 2); break;
				case EPin.Centre:       PinOffset = new Vector(pt.X - (rect.Left + rect.Right) / 2 , pt.Y - (rect.Top + rect.Bottom) / 2); break;
				case EPin.CentreRight:  PinOffset = new Vector(pt.X - rect.Right                   , pt.Y - (rect.Top + rect.Bottom) / 2); break;
				}
			}
		}

		/// <summary>Update the position of the window relative to the reference point on the target</summary>
		private void UpdateLocation()
		{
			m_location_update_pending = false;
			if (!Pinned || PinWindow == null || PinTarget == null)
				return;

			if (m_block_updates != 0) return;
			using (Scope.Create(() => ++m_block_updates, () => --m_block_updates))
			{
				var pt = Point_.Zero;
				var rect = new Rect(PinTarget.Left, PinTarget.Top, PinTarget.ActualWidth, PinTarget.ActualHeight);
				switch (PinSite)
				{
				default: throw new Exception($"Unknown pin location '{PinSite}'");
				case EPin.TopLeft:      pt = new Point(rect.Left                    , rect.Top) + PinOffset; break;
				case EPin.TopCentre:    pt = new Point((rect.Left + rect.Right) / 2 , rect.Top) + PinOffset; break;
				case EPin.TopRight:     pt = new Point(rect.Right                   , rect.Top) + PinOffset; break;
				case EPin.BottomLeft:   pt = new Point(rect.Left                    , rect.Bottom) + PinOffset; break;
				case EPin.BottomCentre: pt = new Point((rect.Left + rect.Right) / 2 , rect.Bottom) + PinOffset; break;
				case EPin.BottomRight:  pt = new Point(rect.Right                   , rect.Bottom) + PinOffset; break;
				case EPin.CentreLeft:   pt = new Point(rect.Left                    , (rect.Top + rect.Bottom) / 2) + PinOffset; break;
				case EPin.Centre:       pt = new Point((rect.Left + rect.Right) / 2 , (rect.Top + rect.Bottom) / 2) + PinOffset; break;
				case EPin.CentreRight:  pt = new Point(rect.Right                   , (rect.Top + rect.Bottom) / 2) + PinOffset; break;
				}

				// Keep the window on-screen
				pt = Gui_.OnScreen(pt, PinWindow.RenderSize);
				PinWindow.SetLocation(pt);
			}
		}
		private int m_block_updates;

		/// <summary>BeginInvoke a location update</summary>
		private void SignalUpdateLocation()
		{
			if (m_location_update_pending) return;
			m_location_update_pending = true;
			PinWindow.Dispatcher.BeginInvoke(new Action(UpdateLocation));
		}
		private bool m_location_update_pending;

		/// <summary>Handle for the pin pop-up menu</summary>
		private void PinOptionInSysMenu(bool add)
		{
			var wih = new WindowInteropHelper(m_pin_window);
			if (add)
			{
				// Get the win32 HWND for 'PinWindow'
				PinWindowHandle = wih.EnsureHandle();
				if (PinWindowHandle == IntPtr.Zero)
					throw new Exception("The pinned window does not yet have a window handle");

				// Get the system menu handle for the window
				var sys_menu_handle = Win32.GetSystemMenu(PinWindowHandle, false);
				
				// Insert the 'Pin Window' option
				Win32.InsertMenu(sys_menu_handle, 5, Win32.MF_BYPOSITION | Win32.MF_SEPARATOR, 0, string.Empty);
				Win32.InsertMenu(sys_menu_handle, 6, Win32.MF_BYPOSITION | Win32.MF_STRING, MenuCmd_Pinned, "&Pin Window");

				// Set the checked state
				UpdatePinMenuCheckState();

				// Install a handler for the system menu item
				var src = HwndSource.FromHwnd(PinWindowHandle);
				src.AddHook(HandlePinMenuOption);
			}
			else
			{
				// Remove the handler for the system menu item
				var src = HwndSource.FromHwnd(PinWindowHandle);
				src.RemoveHook(HandlePinMenuOption);

				// Reset the system menu for the window
				Win32.GetSystemMenu(PinWindowHandle, true);

				PinWindowHandle = IntPtr.Zero;
			}

			// Handler
			IntPtr HandlePinMenuOption(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				if (msg == Win32.WM_SYSCOMMAND && wParam.ToInt32() == MenuCmd_Pinned)
				{
					Pinned = !Pinned;
					handled = true;
				}
				return IntPtr.Zero;
			}
		}

		/// <summary>Update the check mark next to the Pin Window menu option</summary>
		private void UpdatePinMenuCheckState()
		{
			var sys_menu_handle = Win32.GetSystemMenu(PinWindowHandle, false);
			Win32.CheckMenuItem(sys_menu_handle, MenuCmd_Pinned, Win32.MF_BYCOMMAND | (Pinned ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
		}

		/// <summary>The command id for the Pin menu item</summary>
		private const int MenuCmd_Pinned = 1000;
	};
}
