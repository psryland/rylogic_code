using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Common;

namespace Rylogic.Interop.Win32
{
	/// <summary>An invisible window used for receiving notification messages</summary>
	public sealed class DummyWindow :IDisposable
	{
		public const string ClassName = "Rylogic-DummyWindow";
		public static readonly IntPtr HInstance = IntPtr.Zero;// Marshal.GetHINSTANCE(typeof(DummyWindow).Module);

		/// <summary>Mapping from HWND to DummyWindow instance</summary>
		private static Dictionary<IntPtr, DummyWindow> m_wnd = new();

		public DummyWindow(string? diag_name = null)
		{
			var atom = EnsureWndClassRegistered(HInstance);

			using var pin = Marshal_.Pin(this, GCHandleType.Normal);
			Handle = User32.CreateWindow(0, atom, diag_name ?? string.Empty, 0, 0, 0, 1, 1, Win32.HWND_MESSAGE, IntPtr.Zero, HInstance, pin.Pointer);
			if (Handle == IntPtr.Zero)
				throw new Win32Exception();
		}
		public void Dispose()
		{
			Handle = IntPtr.Zero;
		}

		/// <summary>The window handle</summary>
		public IntPtr Handle
		{
			get => m_hwnd;
			set
			{
				if (m_hwnd == value) return;
				if (m_hwnd != IntPtr.Zero)
				{
					User32.DestroyWindow(m_hwnd);
					m_wnd.Remove(m_hwnd);
				}
				m_hwnd = value;
				if (m_hwnd != IntPtr.Zero)
				{
					m_wnd[m_hwnd] = this;
				}
			}
		}
		private IntPtr m_hwnd;

		/// <summary>WndProc hook</summary>
		public event EventHandler<WndProcEventArgs>? Message;

		// Pump the message queue on this window. Returns false if WM_QUIT is received
		public bool Pump()
		{
			for (; User32.PeekMessage(out var msg, Handle, 0, 0, Win32.EPeekMessageFlags.Remove);)
			{
				if (msg.message == Win32.WM_QUIT) return false;
				User32.TranslateMessage(ref msg);
				User32.DispatchMessage(ref msg);
			}
			return true;
		}

		/// <summary>Pump the message queue</summary>
		public async void Run(CancellationToken shutdown)
		{
			using var quit = shutdown.Register(() => User32.PostMessage(Handle, Win32.WM_QUIT, IntPtr.Zero, IntPtr.Zero));
			for (;;)
			{
				var msg = new Win32.Message { };
				var res = await Task.Run(() => User32.GetMessage(out msg, Handle, 0, 0));
				if (res == -1) throw new Win32Exception("Dummy Window message queue error");
				if (res == 0) return; // WM_QUIT
				User32.TranslateMessage(ref msg);
				User32.DispatchMessage(ref msg);
			}
		}

		/// <summary>Instance wndproc</summary>
		private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam)
		{
			var args = new WndProcEventArgs(hwnd, message, wparam, lparam);
			Message?.Invoke(this, args);
			return !args.Handled
				? User32.DefWindowProc(hwnd, message, wparam, lparam)
				: IntPtr.Zero; // S_OK
		}
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

		/// <summary></summary>
		private static int EnsureWndClassRegistered(IntPtr hinstance)
		{
			// Window class is already registered?
			if (User32.GetClassInfo(HInstance, ClassName, out var atom) != null)
				return atom;

			// Register the window class
			var wc = new Win32.WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<Win32.WNDCLASSEX>(),
				style = 0,
				lpfnWndProc = m_static_wndproc,
				cbClsExtra = 0,
				cbWndExtra = 0,
				hInstance = hinstance,
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
