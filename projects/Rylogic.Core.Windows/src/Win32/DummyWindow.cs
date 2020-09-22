using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Common;

namespace Rylogic.Interop.Win32
{
	/// <summary>An invisible window used for receiving notification messages</summary>
	public sealed class DummyWindow :IDisposable
	{
		public const string ClassName = "Rylogic.DummyWindow";
		public static readonly IntPtr HInstance = Marshal.GetHINSTANCE(typeof(DummyWindow).Module);
		private delegate void ThunkFunc(IntPtr hwnd);

		/// <summary>Mapping from HWND to DummyWindow instance</summary>
		private static Dictionary<IntPtr, DummyWindow> m_thunk = new Dictionary<IntPtr, DummyWindow>();

		public DummyWindow()
		{
			EnsureWndClassRegistered();

			var thunk = Marshal.GetFunctionPointerForDelegate(new ThunkFunc((hwnd) => m_thunk[hwnd] = this));
			Handle = Win32.CreateWindow(0, ClassName, string.Empty, 0, 0, 0, 1, 1, Win32.HWND_MESSAGE, IntPtr.Zero, HInstance, thunk);
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
					Win32.DestroyWindow(m_hwnd);
					m_thunk.Remove(m_hwnd);
				}
				m_hwnd = value;
				if (m_hwnd != IntPtr.Zero)
				{
					m_thunk[m_hwnd] = this;
				}
			}
		}
		private IntPtr m_hwnd;

		/// <summary>WndProc hook</summary>
		public event EventHandler<WndProcEventArgs>? Message;

		// Pump the message queue on this window. Returns false if WM_QUIT is received
		public bool Pump()
		{
			for (; Win32.PeekMessage(out var msg, Handle, 0, 0, Win32.EPeekMessageFlags.Remove);)
			{
				if (msg.message == Win32.WM_QUIT) return false;
				Win32.TranslateMessage(ref msg);
				Win32.DispatchMessage(ref msg);
			}
			return true;
		}

		/// <summary>Pump the message queue</summary>
		public async void Run(CancellationToken shutdown)
		{
			using var quit = shutdown.Register(() => Win32.PostMessage(Handle, Win32.WM_QUIT, IntPtr.Zero, IntPtr.Zero));
			for (;;)
			{
				var msg = new Win32.Message { };
				var res = await Task.Run(() => Win32.GetMessage(out msg, Handle, 0, 0));
				if (res == -1) throw new Win32Exception("Dummy Window message queue error");
				if (res == 0) return; // WM_QUIT
				Win32.TranslateMessage(ref msg);
				Win32.DispatchMessage(ref msg);
			}
		}

		/// <summary>Instance wndproc</summary>
		private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam)
		{
			var args = new WndProcEventArgs(hwnd, message, wparam, lparam);
			Message?.Invoke(this, args);
			return !args.Handled
				? Win32.DefWindowProc(hwnd, message, wparam, lparam)
				: IntPtr.Zero; // S_OK
		}
		private static void EnsureWndClassRegistered()
		{
			// Window class is already registered?
			if (Win32.GetClassInfo(HInstance, ClassName) != null)
				return;

			// Register the window class
			var wc = new Win32.WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<Win32.WNDCLASSEX>(),
				style = 0,
				lpfnWndProc = m_static_wndproc = WndProc,
				cbClsExtra = 0,
				cbWndExtra = 0,
				hInstance = IntPtr.Zero,
				hIcon = IntPtr.Zero,
				hCursor = IntPtr.Zero,
				hbrBackground = IntPtr.Zero,
				lpszMenuName = string.Empty,
				lpszClassName = ClassName,
			};
			static IntPtr WndProc(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam)
			{
				if (message == Win32.WM_NCCREATE)
				{
					var cp = Marshal.PtrToStructure<Win32.CREATESTRUCT>(lparam);
					var thunk = Marshal.GetDelegateForFunctionPointer<ThunkFunc>(cp.lpCreateParams);
					thunk(hwnd);
				}
				return m_thunk.TryGetValue(hwnd, out var wnd)
					? wnd.WndProc(hwnd, message, wparam, lparam)
					: Win32.DefWindowProc(hwnd, message, wparam, lparam);
			}
			Win32.RegisterClass(wc);
		}
		private static Win32.WNDPROC? m_static_wndproc;
	}
}
