module prd.gui.wingui;

import std.array;
import std.algorithm;
import std.container;
import std.exception;
import core.sys.windows.windows;

// Base class for all windows
class Control
{
	this()
	{}

protected:

	// Window creation initialisation parameter wrapper
	struct InitParam
	{
		Control m_this;
		void* m_lparam;
		this(Control this_, void* lparam) 
		{
			m_this = this_;
			m_lparam = lparam;
		}
	};

	// The initial wndproc function used in forms, dialogs, and custom controls.
	extern (Windows) static LRESULT InitWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
	{
		//WndProcDebug(hwnd, message, wparam, lparam);
		if (message == WM_NCCREATE)
		{
		//    auto init = reinterpret_cast<InitParam*>(reinterpret_cast<CREATESTRUCT*>(lparam)->lpCreateParams);
		//
		//    // Set the wndproc to the default before replacing it with the thunk in Attach()
		//    assert((WNDPROC)::GetWindowLongPtrW(hwnd, GWLP_WNDPROC) == &InitWndProc);
		//    ::SetWindowLongPtrW(hwnd, GWLP_WNDPROC, LONG_PTR(&::DefWindowProcW));
		//    init->m_this->Attach(hwnd);
		//    return init->m_this->WndProc(message, wparam, lparam);
		}
		if (message == WM_INITDIALOG)
		{
		//    auto init = reinterpret_cast<InitParam*>(lparam);
		//
		//    // The DWLP_DLGPROC is the user wndproc supplied in CreateDialog.
		//    // GWLP_WNDPROC is an internal dialog wndproc (user32::_DefDlgProc != DefDlgProc)
		//    // 'user32::_DefDlgProc' calls the user DLGPROC which, on returning FALSE, then
		//    // handles the message internally (DefWindowProc is never called)
		//    // Restore 'DWLP_DLGPROC' to the default (nullptr) before replacing it with the thunk
		//    assert((WNDPROC)::GetWindowLongPtrW(hwnd, DWLP_DLGPROC) == &InitWndProc);
		//    ::SetWindowLongPtrW(hwnd, DWLP_DLGPROC, LONG_PTR(&::DefDlgProcW));
		//    init->m_this->Attach(hwnd);
		//    return init->m_this->WndProc(message, wparam, LPARAM(init->m_lparam));
		}
		return DefWindowProcW(hwnd, message, wparam, lparam);
	}
	//static LRESULT __stdcall StaticWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
	//{
	//    // 'm_thunk' causes 'hwnd' to actually be the pointer to the control rather than the hwnd
	//    auto& ctrl = *reinterpret_cast<Control*>(hwnd);
	//    assert("Message received for destructed control" && ctrl.m_hwnd != nullptr);
	//    return ctrl.WndProc(message, wparam, lparam);
	//}
	//LRESULT DefWndProc(UINT message, WPARAM wparam, LPARAM lparam)
	//{
	//    if (m_oldproc == &::DefDlgProcW) return FALSE;
	//    if (m_oldproc != nullptr) return ::CallWindowProcW(m_oldproc, m_hwnd, message, wparam, lparam);
	//return ::DefWindowProcW(m_hwnd, message, wparam, lparam);
	//}
	//
	//// Handy debugging method for displaying WM_MESSAGES
	//// Call with 'hwnd' == 0, message = 0/1 to disable/enable trace
	//#if PR_WNDPROCDEBUG
	//static void WndProcDebug(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, char const* name = nullptr)
	//{
	//    if (true
	//        // && (name != nullptr && strncmp(name,"lbl-version",5) == 0)
	//        // && (message == WM_WINDOWPOSCHANGING || message == WM_WINDOWPOSCHANGED || message == WM_ERASEBKGND || message == WM_PAINT)
	//        )
	//    {
	//        auto out = [](char const* s) { OutputDebugStringA(s); };
	//        //auto out = [](char const* s) { std::ofstream("P:\\dump\\wingui.log", std::ofstream::app).write(s,strlen(s)); };
	//
	//        // Display the message
	//        static int msg_idx = 0; ++msg_idx;
	//        auto m = pr::gui::DebugMessage(hwnd, message, wparam, lparam);
	//        if (*m)
	//        {
	//            for (int i = 1; i < wnd_proc_nest(); ++i) out("\t");
	//            out(pr::FmtX<struct X, 256, char>("%5d|%-30s|%s\n", msg_idx, name, m));
	//        }
	//        if (msg_idx == 0) _CrtDbgBreak();
	//    }
	//}
	//static int& wnd_proc_nest()
	//{
	//    static int s_wnd_proc_nest = 0;
	//    return s_wnd_proc_nest;
	//}
	//#else
	//static void WndProcDebug(HWND , UINT , WPARAM , LPARAM , char const* = nullptr) {}
	//#endif
	//static void WndProcDebug(MSG& msg, char const* name = nullptr)
	//{
	//    WndProcDebug(msg.hwnd, msg.message, msg.wParam, msg.lParam, name);
	//}

}

// Base class for main form windows
class Form :Control
{
	this()
	{
		super();
	}
}

// Message Loop
// An interface for types that need to handle messages from the message
// loop before TranslateMessage is called. Typically these are dialog windows
// or windows with keyboard accelerators that need to call 'IsDialogMessage'
// or 'TranslateAccelerator'
interface IMessageFilter
{
	// Implementers should return true to halt processing of the message.
	// Typically, if you're just observing messages as they go past, return false.
	// If you're a dialog return the result of IsDialogMessage()
	// If you're a window with accelerators, return the result of TranslateAccelerator()
	bool TranslateMessage(ref MSG msg);
};

// Base class and basic implementation of a message loop
class MessageLoop :IMessageFilter
{
	// The collection of message filters filtering messages in this loop
	IMessageFilter[] m_filters;

	public this()
	{
		m_filters ~= this;
	}

	// Subclasses should replace this method
	public int Run()
	{
		MSG msg;
		for (int result; (result = GetMessageW(&msg, null, 0, 0)) != 0; )
		{
			// GetMessage returns negative values for errors
			enforce(result > 0, "GetMessage failed");

			// Pass the message to each filter. The last filter is this message loop which always handles the message.
			foreach (filter; m_filters)
				if (filter.TranslateMessage(msg))
					break;
		}
		return cast(int)msg.wParam;
	}

	// Add an instance that needs to handle messages before TranslateMessage is called
	public void AddMessageFilter(ref IMessageFilter filter)
	{
		m_filters.insertInPlace(m_filters.length - 1, filter);
	}

	// Remove a message filter from the chain of filters for this message loop
	public void RemoveMessageFilter(ref IMessageFilter filter)
	{
	//	m_filters.remove(a => a == filter)();
	}

	// The message loop is always the last filter in the chain
	protected bool TranslateMessage(ref MSG msg)
	{
		core.sys.windows.windows.TranslateMessage(&msg);
		core.sys.windows.windows.DispatchMessageW(&msg);
		return true;
	}
}
