using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Rylogic.Common;
using Rylogic.Interop.Win32;
using Rylogic.Windows.Gui;
using HINSTANCE = System.IntPtr;
using HMENU = System.IntPtr;
using HWND = System.IntPtr;

namespace Rylogic.Gui.Native
{
	#region Enumerations
	//todo: unused at the mo
	public enum EUnits
	{
		// X,Y,W,H in pixels
		Pixels,

		// Units are relative to the average size of the window font
		DialogUnits,
	}

	// Auto size anchors
	public enum EAnchor
	{
		None = 0,
		Left = 1 << 0,
		Top = 1 << 1,
		Right = 1 << 2,
		Bottom = 1 << 3,
		TopLeft = Left | Top,
		TopRight = Right | Top,
		BottomLeft = Left | Bottom,
		BottomRight = Right | Bottom,
		LeftTopRight = Left | Top | Right,
		LeftBottomRight = Left | Bottom | Right,
		LeftTopBottom = Left | Top | Bottom,
		RightTopBottom = Right | Top | Bottom,
		All = Left | Top | Right | Bottom,
		allow_bitops = 0,
	}

	// Window docking
	public enum EDock
	{
		None = 0,
		Fill = 1,
		Top = 2,
		Bottom = 3,
		Left = 4,
		Right = 5
	}

	// Dialog result
	public enum EDialogResult
	{
		None     = 0,
		Ok       = Win32.IDOK,
		Cancel   = Win32.IDCANCEL,
		Abort    = Win32.IDABORT,
		Retry    = Win32.IDRETRY,
		Ignore   = Win32.IDIGNORE,
		Yes      = Win32.IDYES,
		No       = Win32.IDNO,
		Close    = Win32.IDCLOSE,
		Help     = Win32.IDHELP,
		TryAgain = Win32.IDTRYAGAIN,
		Continue = Win32.IDCONTINUE,
		Timeout  = Win32.IDTIMEOUT,
	}

	// Window start position
	public enum EStartPosition
	{
		Default,
		CentreParent,
		Manual,
	}

	// Set window position flags
	[Flags] public enum EWindowPos : int
	{
		None           = 0,
		NoSize         = Win32.SWP_NOSIZE,
		NoMove         = Win32.SWP_NOMOVE,
		NoZorder       = Win32.SWP_NOZORDER,
		NoRedraw       = Win32.SWP_NOREDRAW,
		NoActivate     = Win32.SWP_NOACTIVATE,
		FrameChanged   = Win32.SWP_FRAMECHANGED,
		ShowWindow     = Win32.SWP_SHOWWINDOW,
		HideWindow     = Win32.SWP_HIDEWINDOW,
		NoCopyBits     = Win32.SWP_NOCOPYBITS,
		NoOwnerZOrder  = Win32.SWP_NOOWNERZORDER,
		NoSendChanging = Win32.SWP_NOSENDCHANGING,
		DrawFrame      = Win32.SWP_DRAWFRAME,
		NoReposition   = Win32.SWP_NOREPOSITION,
		DeferErase     = Win32.SWP_DEFERERASE,
		AsyncWindowpos = Win32.SWP_ASYNCWINDOWPOS,
		NoClientSize   = 0x0800, // SWP_NOCLIENTSIZE (don't send WM_SIZE)
		NoClientMove   = 0x1000, // SWP_NOCLIENTMOVE (don't send WM_MOVE)
		StateChange    = 0x8000, // SWP_STATECHANGED (minimized, maximised, etc)
	}

	// Control key state
	[Flags] public enum EModifierKey
	{
		None         = 0,
		LShift       = 1 << 0,
		RShift       = 1 << 1,
		Shift        = LShift | RShift,
		LCtrl        = 1 << 2,
		RCtrl        = 1 << 3,
		Ctrl         = LCtrl | RCtrl,
		LAlt         = 1 << 4,
		RAlt         = 1 << 5,
		Alt          = LAlt | RAlt,
	}

	// Mouse key state, used in mouse down/up events
	[Flags]
	public enum EMouseKey
	{
		None     = 0,
		Left     = Win32.MK_LBUTTON, // 0x0001
		Right    = Win32.MK_RBUTTON, // 0x0002
		Shift    = Win32.MK_SHIFT,   // 0x0004
		Ctrl     = Win32.MK_CONTROL, // 0x0008
		Middle   = Win32.MK_MBUTTON, // 0x0010
		XButton1 = Win32.MK_XBUTTON1,// 0x0020
		XButton2 = Win32.MK_XBUTTON2,// 0x0040
		Alt      = 0x0080,     // There is not MK_ define for alt, this is tested using GetKeyState
	}
	#endregion

	/// <summary>An Win32 window</summary>
	public sealed class Win32Window<TMessageLoop> :IDisposable where TMessageLoop : MessageLoop, new()
	{
		public class Props
		{
			public EStartPosition StartPosition = EStartPosition.Default;
			public bool HideOnClose = false;
			public int X = Win32.CW_USEDEFAULT;
			public int Y = Win32.CW_USEDEFAULT;
			public int W = Win32.CW_USEDEFAULT;
			public int H = Win32.CW_USEDEFAULT;
			public string Title = string.Empty;
			public int Style = Win32.DS_SETFONT | Win32.DS_FIXEDSYS | Win32.WS_OVERLAPPEDWINDOW | Win32.WS_CLIPCHILDREN;
			public int StyleEx = Win32.WS_EX_APPWINDOW | Win32.WS_EX_WINDOWEDGE;
			public HMENU Menu = HMENU.Zero;
		}

		/// <summary>Registered window class name</summary>
		private const string ClassName = "Rylogic-Win32Window";

		/// <summary>Mapping from HWND to Win32Window instance</summary>
		[ThreadStatic] private static Dictionary<HWND, Win32Window<TMessageLoop>> m_wnd = new();

		/// <summary>The GUI thread</summary>
		private int m_main_thread_id = Environment.CurrentManagedThreadId;

		/// <summary>Create a Win32Window instance</summary>
		public Win32Window(TMessageLoop? msg_loop = null, Props? props = null, HWND? parent = null)
		{
			// Get the instance handle for this process
			var hInstance = Kernel32.GetModuleHandle(null);

			// Ensure the window class has been registered
			var atom = EnsureWndClassRegistered(hInstance);

			// Defaults
			MsgLoop = msg_loop ?? new();
			Properties = props ?? new();
			parent ??= HWND.Zero;

			// Create the window instance
			using var pin = Marshal_.Pin(this, GCHandleType.Normal);
			Handle = User32.CreateWindow(Properties.StyleEx, atom, Properties.Title, Properties.Style, Properties.X, Properties.Y, Properties.W, Properties.H, (HWND)parent, Properties.Menu, hInstance, pin.Pointer);
			if (Handle == IntPtr.Zero)
				throw new Win32Exception();
		}
		public void Dispose()
		{
			Handle = HWND.Zero;
		}

		/// <summary>The window handle</summary>
		public HWND Handle
		{
			get => m_hwnd;
			private set
			{
				if (m_hwnd == value) return;
				if (m_hwnd != HWND.Zero)
				{
					User32.DestroyWindow(m_hwnd);
					m_wnd.Remove(m_hwnd);
				}
				m_hwnd = value;
				if (m_hwnd != HWND.Zero)
				{
					m_wnd[m_hwnd] = this;
				}
			}
		}
		private HWND m_hwnd;

		/// <summary>Window properties</summary>
		public Props Properties { get; }

		/// <summary>Get the current window style</summary>
		public int Style
		{
			get => (int)User32.GetWindowLong(Handle, Win32.GWL_STYLE);
		}

		/// <summary>Is the window visible</summary>
		public bool Visible
		{
			get => User32.IsWindow(Handle) && (Style & Win32.WS_VISIBLE) != 0;
			set
			{
				if (Visible == value) return;
				Show(value ? Win32.SW_SHOW : Win32.SW_HIDE);
			}
		}

		/// <summary>The loop for processing windows messages</summary>
		public TMessageLoop MsgLoop { get; }

		/// <summary>WndProc hook</summary>
		public event EventHandler<WndProcEventArgs>? Message;

		// Display as a non-modal window, creating the window first if necessary
		public void Show(int show = Win32.SW_SHOW)
		{
			// Show the window, non-modally
			User32.ShowWindow(Handle, show);
			User32.UpdateWindow(m_hwnd);
		}

		/// <summary>Instance wndproc</summary>
		private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam)
		{
			var args = new WndProcEventArgs(hwnd, message, wparam, lparam);
			if (Environment.CurrentManagedThreadId != m_main_thread_id)
				throw new Exception("WndProc called on background thread");

			// Custom message handling
			Message?.Invoke(this, args);
			if (args.Handled)
				return S_OK;

			//System.Diagnostics.Debug.WriteLine($"WndProc: {args.Description}");

			// Default message handling
			switch (args.Message)
			{
				case Win32.WM_DESTROY:
				{
					User32.PostQuitMessage(0);
					return User32.DefWindowProc(hwnd, message, wparam, lparam);
				}
				case Win32.WM_CLOSE:
				{
					if (Properties.HideOnClose)
					{
						Visible = false;
						return S_OK;
					}

					// DefWndProc should call DestroyWindow
					return User32.DefWindowProc(hwnd, message, wparam, lparam);
				}
				case Win32.WM_WINDOWPOSCHANGING:
				case Win32.WM_WINDOWPOSCHANGED:
				{
					// todo
					return User32.DefWindowProc(hwnd, message, wparam, lparam);
				}
				default:
				{
					return User32.DefWindowProc(hwnd, message, wparam, lparam);
				}
			}
		}
		private static readonly IntPtr S_OK = IntPtr.Zero;

		/// <summary>Static wndproc - Convert from 'hwnd' to class instance</summary>
		private static IntPtr StaticWndProc(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam)
		{
			if (message == Win32.WM_NCCREATE)
			{
				// Managed issue with this... couldn't figure it out...
				// It's exactly the same as the MessageWindow implementation from the .net reference source
				// but it still didn't work... Only needed if the 'Handle' value is needed during WM_CREATE

				//var gc = GCHandle.FromIntPtr(lparam);
				//var cp = Marshal.PtrToStructure<Win32.CREATESTRUCT>(lparam);
				//var gc = GCHandle.FromIntPtr(cp.lpCreateParams);
				//m_wnd.Add(hwnd, (DummyWindow?)gc.Target ?? throw new Exception("'this' pointer must be provided in CreateWindowEx"));
			}
			return m_wnd.TryGetValue(hwnd, out var wnd)
				? wnd.WndProc(hwnd, message, wparam, lparam)
				: User32.DefWindowProc(hwnd, message, wparam, lparam);
		}
		private static readonly Win32.WNDPROC m_static_wndproc = new(StaticWndProc);

		/// <summary>Register this window class</summary>
		private static int EnsureWndClassRegistered(HINSTANCE hInstance)
		{

			// Window class is already registered?
			if (User32.GetClassInfo(hInstance, ClassName, out var atom) != null)
				return atom;

			// Register the window class
			var wc = new Win32.WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<Win32.WNDCLASSEX>(),
				style = 0,
				lpfnWndProc = m_static_wndproc,
				cbClsExtra = 0,
				cbWndExtra = 0,
				hInstance = hInstance,
				hIcon = IntPtr.Zero,
				hCursor = IntPtr.Zero,
				hbrBackground = IntPtr.Zero,
				lpszMenuName = string.Empty,
				lpszClassName = ClassName,
			};

			atom = User32.RegisterClass(wc);
			return atom;
		}
	}
}














