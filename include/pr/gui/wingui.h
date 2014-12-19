//*****************************************************************************************
// Win32 API 
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************************
// A collection of structs that wrap the win32 api and expose an
// interface similar to C# .NET winforms. Inspired by ATL\WTL.
// Specs:
//   - As fast as ATL\WTL. Uses ATL thunks for WNDPROC
//   - Doesn't use macros. Much easier to debug/read
//   - Single file with minimal dependencies. Depends on standard
//     C++ headers, atlbase.h, and other standard headers (no external
//     libraries needed)
//   - Automatic support for resizing
//   - C#.NET style event handlers
//
// Example Use:
//  See the end of this file for the #if 0,#endif
//  block showing basic use.
#pragma once

#include <vector>
#include <string>
#include <unordered_map>
#include <algorithm>
#include <functional>
#include <atomic>
#include <mutex>
#include <thread>
#include <cassert>
#include <tchar.h>
#include <atlbase.h>
#include <atlstdthunk.h>
#include <commctrl.h>
#include <richedit.h>

#pragma comment(lib, "comctl32.lib")

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#define thread_local_defined
#endif

namespace pr
{
	namespace wingui
	{
		// Forwards
		struct Point;
		struct Rect;
		struct Size;
		struct Control;
		template <typename D> struct Form;

		#pragma region Enumerations

		enum class ECommonControl
		{
			ListViewClasses = ICC_LISTVIEW_CLASSES   , // listview, header
			TreeViewClasses = ICC_TREEVIEW_CLASSES   , // treeview, tooltips
			BarClasses      = ICC_BAR_CLASSES        , // toolbar, statusbar, trackbar, tooltips
			TabClasses      = ICC_TAB_CLASSES        , // tab, tooltips
			UpDown          = ICC_UPDOWN_CLASS       , // updown
			Progress        = ICC_PROGRESS_CLASS     , // progress
			Hotkey          = ICC_HOTKEY_CLASS       , // hotkey
			Animate         = ICC_ANIMATE_CLASS      , // animate
			Win95Classes    = ICC_WIN95_CLASSES      , //
			DateClasses     = ICC_DATE_CLASSES       , // month picker, date picker, time picker, updown
			ComboEx         = ICC_USEREX_CLASSES     , // comboex
			Rebar           = ICC_COOL_CLASSES       , // rebar (coolbar) control
			Internet        = ICC_INTERNET_CLASSES   , //
			PageScroller    = ICC_PAGESCROLLER_CLASS , // page scroller
			NativeFontCtrl  = ICC_NATIVEFNTCTL_CLASS , // native font control
			StandardClasses = ICC_STANDARD_CLASSES   ,
			LinkClass       = ICC_LINK_CLASS         ,
		};

		// Autosize anchors
		enum class EAnchor { Left = 1 << 0, Top = 1 << 1, Right = 1 << 2, Bottom = 1 << 3 };
		inline EAnchor operator | (EAnchor lhs, EAnchor rhs) { return EAnchor(int(lhs) | int(rhs)); }
		inline EAnchor operator & (EAnchor lhs, EAnchor rhs) { return EAnchor(int(lhs) & int(rhs)); }

		enum class EDialogResult
		{
			Ok,
			Cancel,
		};

		#pragma endregion

		#pragma region Support Functions

		// Convert an error code into an error message
		inline std::string ErrorMessage(HRESULT result)
		{
			char msg[16384];
			DWORD length(_countof(msg));
			if (!FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, result, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), msg, length, NULL))
				sprintf_s(msg, "Unknown error code: 0x%80X", result);
			return msg;
		}

		// Test an hresult and throw on error
		inline void Throw(HRESULT result, std::string message)
		{
			if (SUCCEEDED(result)) return;
			throw std::exception(message.append("\n").append(ErrorMessage(GetLastError())).c_str());
		}
		inline void Throw(BOOL result, std::string message)
		{
			if (result != 0) return;
			auto hr = HRESULT(GetLastError());
			Throw(SUCCEEDED(hr) ? E_FAIL : hr, message);
		}

		// Initialise common controls (makes them look modern)
		inline void InitCtrls(ECommonControl classes = ECommonControl::StandardClasses)
		{
			auto iccx = INITCOMMONCONTROLSEX{sizeof(INITCOMMONCONTROLSEX), DWORD(classes)};
			Throw(::InitCommonControlsEx(&iccx), "Common control initialisation failed");
		}

		#pragma endregion

		#pragma region Unicode conversion

		// A static instance of the locale, because this thing takes ages to construct
		inline std::locale const& locale()
		{
			static std::locale s_locale("");
			return s_locale;
		}

		// Narrow
		inline std::string Narrow(char const* from, std::size_t len = 0)
		{
			if (len == 0) len = strlen(from);
			return std::string(from, from+len);
		}
		inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
		{
			if (len == 0) len = wcslen(from);
			std::vector<char> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from, from + len, '_', &buffer[0]);
			return std::string(&buffer[0], &buffer[len]);
		}
		template <std::size_t Len> inline std::string Narrow(char const (&from)[Len])    { return Narrow(from, Len); }
		template <std::size_t Len> inline std::string Narrow(wchar_t const (&from)[Len]) { return Narrow(from, Len); }

		// Widen
		inline std::wstring Widen(wchar_t const* from, std::size_t len = 0)
		{
			if (len == 0) len = wcslen(from);
			return std::wstring(from, from+len);
		}
		inline std::wstring Widen(char const* from, std::size_t len = 0)
		{
			if (len == 0) len = strlen(from);
			std::vector<wchar_t> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).widen(from, from + len, &buffer[0]);
			return std::wstring(&buffer[0], &buffer[len]);
		}
		template <std::size_t Len> inline std::wstring Widen (wchar_t const (&from)[Len]) { return Widen(from, Len); }
		template <std::size_t Len> inline std::wstring Widen (char    const (&from)[Len]) { return Widen(from, Len); }

		#pragma endregion

		#pragma region Support structures

		// String
		using string = std::basic_string<TCHAR>;

		// Point
		struct Point :POINT
		{
			Point() :POINT() {}
			Point(long x_, long y_) { x = x_; y = y_; }
		};

		// Size
		struct Size :SIZE
		{
			Size() :SIZE() {}
			Size(long sx, long sy) { cx = sx; cy = sy; }
			operator Rect() const;
		};

		// Rect
		struct Rect :RECT
		{
			Rect() :RECT() {}
			Rect(RECT const& r) :RECT(r) {}
			Rect(int l, int t, int r, int b) { left = l; top = t; right = r; bottom = b; }
			explicit Rect(Size s) { left = top = 0; right = s.cx; bottom = s.cy; }
			LONG width() const    { return right - left; }
			LONG height() const   { return bottom - top; }
			Size size() const     { return Size{width(), height()}; }
			void size(Size sz)    { right = left + sz.cx; bottom = top + sz.cy; }
			Point& topleft()      { return *reinterpret_cast<Point*>(&left); }
			Point& bottomright()  { return *reinterpret_cast<Point*>(&right); }
			operator Size() const { return size(); }
		};

		inline Point operator + (Point p, Size s) { return Point(p.x + s.cx, p.y + s.cy); }
		inline Size::operator Rect() const { return Rect(*this); }

		// Range
		struct RangeI
		{
			int beg;
			int end;
			RangeI() :beg() ,end() {}
			RangeI(int b, int e) :beg(b) ,end(e) {}
			int size() const { return end - beg;}
		};

		// Device context
		struct DC
		{
			HDC  m_hdc;
			bool m_owned;
			DC(HDC hdc, bool owned = false) :m_hdc(hdc) ,m_owned(owned) {}
			~DC() { if (m_owned) ::DeleteDC(m_hdc); }
			operator HDC() const { return m_hdc; }
		};
		struct MemDC :DC
		{
			HDC m_hdc_orig;
			HBITMAP m_bmp;
			HBITMAP m_bmp_old;
			Rect m_rect;

			MemDC(HDC hdc, Rect const& rect)
				:DC(::CreateCompatibleDC(hdc), true)
				,m_hdc_orig(hdc)
				,m_bmp(::CreateCompatibleBitmap(m_hdc_orig, rect.width(), rect.height()))
				,m_bmp_old(HBITMAP(::SelectObject(m_hdc, m_bmp)))
				,m_rect(rect)
			{
				assert(m_bmp != nullptr);
				::SetViewportOrgEx(m_hdc, -m_rect.left, -m_rect.top, nullptr);
			}
			~MemDC()
			{
				::BitBlt(m_hdc_orig, m_rect.left, m_rect.top, m_rect.width(), m_rect.height(), m_hdc, m_rect.left, m_rect.top, SRCCOPY);
				::SelectObject(m_hdc, m_bmp_old);
				::DeleteObject(m_bmp);
			}
		};
		struct ClientDC :DC
		{
			HWND m_hwnd;
			ClientDC(HWND hwnd) :DC(::GetDC(hwnd), false) ,m_hwnd(hwnd) {}
			~ClientDC() { ::ReleaseDC(m_hwnd, m_hdc); }
		};

		// Font
		struct Font
		{
			HFONT m_obj;
			bool m_owned;
			Font() :m_obj((HFONT)GetStockObject(DEFAULT_GUI_FONT)) ,m_owned(false) {}
			Font(HFONT obj, bool owned = true) :m_obj(obj) ,m_owned(owned) {}
			Font(int nPointSize, LPCTSTR lpszFaceName, HDC hdc = nullptr, bool bBold = false, bool bItalic = false)
			{
				ClientDC clientdc(nullptr);
				auto hdc_ = hdc ? hdc : clientdc.m_hdc;

				auto lf = LOGFONT{};
				lf.lfCharSet = DEFAULT_CHARSET;
				lf.lfWeight = bBold ? FW_BOLD : FW_NORMAL;
				lf.lfItalic = BYTE(bItalic ? TRUE : FALSE);
				::_tcsncpy_s(lf.lfFaceName, _countof(lf.lfFaceName), lpszFaceName, _TRUNCATE);

				// convert nPointSize to logical units based on hDC
				auto pt = Point(0, ::MulDiv(::GetDeviceCaps(hdc_, LOGPIXELSY), nPointSize, 720)); // 72 points/inch, 10 decipoints/point
				auto ptOrg = Point{};
				::DPtoLP(hdc_, &pt, 1);
				::DPtoLP(hdc_, &ptOrg, 1);
				lf.lfHeight = -abs(pt.y - ptOrg.y);
		
				m_obj = ::CreateFontIndirect(&lf);
				m_owned = true;
			}
			~Font() { if (m_owned) ::DeleteObject(m_obj); }
			operator HFONT() const { return m_obj; }
		};

		// Brush
		struct Brush
		{
			HBRUSH m_obj;
			bool m_owned;
			Brush() :m_obj() ,m_owned() {}
			Brush(HBRUSH obj, bool owned = true) :m_obj(obj) ,m_owned(owned) {}
			Brush(COLORREF col) :m_obj(CreateSolidBrush(col)), m_owned(true) { Throw(m_obj != 0, "Failed to create HBRUSH"); }
			Brush(Brush&& rhs) { std::swap(m_obj, rhs.m_obj); std::swap(m_owned, rhs.m_owned); }
			~Brush() { if (m_owned) DeleteObject(m_obj); }
			operator HBRUSH() const { return m_obj; }
		};

		// Paint
		struct PaintStruct :PAINTSTRUCT
		{
			HWND m_hwnd;
			PaintStruct(HWND hwnd) :m_hwnd(hwnd) { BeginPaint(m_hwnd, this); }
			~PaintStruct()                       { EndPaint(m_hwnd, this); }
		};

		// Basic message loop
		struct MessageLoop
		{
			HACCEL m_accel;

			MessageLoop(HACCEL accel = 0)
				:m_accel(accel)
			{}
			MessageLoop(HINSTANCE hinst, int accel_idd)
				:MessageLoop(::LoadAccelerators(hinst, MAKEINTRESOURCE(accel_idd)))
			{}
			virtual int Run()
			{
				MSG msg;
				for (int result; (result = ::GetMessage(&msg, NULL, 0, 0)) != 0; )
				{
					// GetMessage actually returns negative values for errors
					Throw(result > 0, "GetMessage failed");

					// This is not a typical message loop, we don't call 'TranslateMessage' or
					// 'IsDialogMessage' in the main loop because we want each window to have
					// the option of not translating the message, or of calling 'IsDialogMessage' first.
					if (m_accel && ::TranslateAccelerator(msg.hwnd, m_accel, &msg)) continue;
					::DispatchMessage(&msg);
				}
				return (int)msg.wParam;
			}
		};

		// Helper for changing the state of a variable, restoring it on destruction
		template <typename T> struct RAII
		{
			T* m_var;
			T old_value;
			RAII(T& var, bool new_value) :m_var(&var) ,old_value(var) { *m_var = new_value; }
			~RAII()                                                   { *m_var = old_value; }
			RAII(RAII const&) = delete;
			RAII& operator =(RAII const&) = delete;
		};

		#pragma endregion

		#pragma region EventHandler

		// Returns an identifier for uniquely id'ing event handlers
		using EventHandlerId = unsigned long long;
		inline EventHandlerId GenerateEventHandlerId()
		{
			static auto s_id = std::atomic_uint{};
			auto id = s_id.load();
			for (;!s_id.compare_exchange_weak(id, id + 1);) {}
			return id + 1;
		}

		// Placeholder for events that take no arguments. (Makes the templating consistent)
		struct EmptyArgs {};

		// EventHandler<>
		template <typename Sender, typename Args> struct EventHandler
		{
			using Delegate = std::function<void(Sender,Args)>;
			struct Func
			{
				Delegate m_delegate;
				EventHandlerId m_id;
				Func(Delegate delegate, EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::vector<Func> m_handlers;

			EventHandler() :m_handlers() {}
			EventHandler(EventHandler&& rhs) :m_handlers(std::move(rhs.m_handlers)) {}
			EventHandler(EventHandler const&) = delete;
			EventHandler& operator=(EventHandler const&) = delete;

			// 'Raise' the event notifying subscribed observers
			void operator()(Sender& s, Args const& a) const
			{
				for (auto& h : m_handlers)
					h.m_delegate(s,a);
			}

			// Detach all handlers. NOTE: this invalidates all associated Handler's
			void reset() { m_handlers.clear(); }

			// Number of attached handlers
			size_t count() const { return m_handlers.size(); }

			// Attach/Detach handlers
			EventHandlerId operator += (Delegate func)
			{
				auto handler = Func{func, GenerateEventHandlerId()};
				m_handlers.push_back(handler);
				return handler.m_id;
			}
			EventHandlerId operator = (Delegate func)
			{
				reset();
				return *this += func;
			}
			void operator -= (EventHandlerId handler_id)
			{
				// Note, can't use -= (Delegate func) because std::function<> does not allow operator ==
				auto iter = std::find_if(begin(m_handlers), end(m_handlers), [=](Func const& func){ return func.m_id == handler_id; });
				if (iter != end(m_handlers)) m_handlers.erase(iter);
			}

			// Boolean test for no assigned handlers
			struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
			operator bool_type() const { return !m_handlers.empty() ? &bool_tester::x : static_cast<bool_type>(0); }
		};

		#pragma endregion

		// Special handle value used to indicate that a window is
		// the main application window and that, when closed, the
		// application should exit. Pass as the 'parent' parameter.
		struct ApplicationMainWindowHandle
		{
			operator HWND() const                           { return reinterpret_cast<HWND>(~0ULL); }
			template <typename D> operator Form<D>*() const { return reinterpret_cast<Form<D>*>(~0ULL); }
		} const ApplicationMainWindow;

		// Base class for all windows/controls
		struct Control
		{
			using Controls = std::vector<Control*>;
			static int const IDC_UNUSED = -1;
		
		protected:
			HWND               m_hwnd;        // Window handle for the control
			int                m_id;          // Dialog control id, used to detect windows messages for this control
			TCHAR const*       m_name;        // Debugging name for the control
			Control*           m_parent;      // The parent that contains this control
			Controls           m_child;       // The controls nested with this control
			EAnchor            m_anchor;      // How the control resizes with it's parent
			Rect               m_pos_offset;  // Distances from this control to the edges of the parent client area
			COLORREF           m_colour_fore; // Foreground colour
			COLORREF           m_colour_back; // Background colour
			bool               m_dbl_buffer;  // True if the control is double buffered
			ATL::CStdCallThunk m_thunk;       // WndProc thunk, turns a __stdcall into a __thiscall
			WNDPROC            m_oldproc;     // The window class default wndproc function
			std::thread::id    m_thread_id;   // The thread that this control was created on

			// Hook or unhook the window proc for this control
			void HookWndProc(bool hook)
			{
				if (hook && !m_oldproc)
					m_oldproc = (WNDPROC)::SetWindowLongPtr(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_thunk.GetCodeAddress()));
				else if (!hook && m_oldproc)
					::SetWindowLongPtr(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_oldproc)), m_oldproc = nullptr;
			}
			static LRESULT __stdcall StaticWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				return reinterpret_cast<Control*>(hwnd)->WndProc(message, wparam, lparam);
			}

			// Record the position of the control within the parent
			void RecordPosOffset()
			{
				// Store distances so that this control's position equals
				// parent.left + m_pos_offset.left, parent.right + m_pos_offset.right, etc..
				// (i.e. right and bottom offsets are typically negative)
				if (!m_parent) return;
				auto r = WindowRect();
				auto p = m_parent->WindowRect();
				m_pos_offset = Rect(r.left - p.left, r.top - p.top, r.right - p.right, r.bottom - p.bottom);
			}

			// Adjust the size of this control relative to it's parent
			void ResizeToParent(bool repaint = false)
			{
				if (!m_parent) return;
				auto r = WindowRect();
				auto p = m_parent->WindowRect();
				auto w = r.width(); auto h = r.height();
				if (int(m_anchor & EAnchor::Left) != 0)
				{
					r.left = p.left + m_pos_offset.left;
					if (int(m_anchor & EAnchor::Right ) == 0)
						r.right = r.left + w;
				}
				if (int(m_anchor & EAnchor::Top) != 0)
				{
					r.top = p.top + m_pos_offset.top;
					if (int(m_anchor & EAnchor::Bottom) == 0)
						r.bottom = r.top + h;
				}
				if (int(m_anchor & EAnchor::Right ) != 0)
				{
					r.right = p.right + m_pos_offset.right;
					if (int(m_anchor & EAnchor::Left) == 0)
						r.left = r.right - w;
				}
				if (int(m_anchor & EAnchor::Bottom) != 0)
				{
					r.bottom = p.bottom + m_pos_offset.bottom;
					if (int(m_anchor & EAnchor::Top) == 0)
						r.top = r.bottom - h;
				}
			
				WindowRect(r, repaint);
			}

			// This method is the window procedure for this control.
			// 'ProcessWindowMessage' is used to process messages sent
			// to the parent window that contains this control.
			virtual LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				switch (message)
				{
				case WM_DESTROY:
					{
						HookWndProc(false);
						m_hwnd = nullptr;
						break;
					}
				case WM_ERASEBKGND:
					{
						if (m_dbl_buffer)
							return S_FALSE;
						break;
					}
				case WM_PAINT:
					{
						if (m_dbl_buffer)
						{
							if (wparam != 0) // If wparam != 0 then is should be an existing HDC
							{
								MemDC mem(HDC(wparam), ClientRect());
								DefWndProc(WM_PRINTCLIENT, WPARAM(mem.m_hdc), LPARAM(PRF_CHECKVISIBLE|PRF_NONCLIENT|PRF_CLIENT));
							}
							else
							{
								PaintStruct p(m_hwnd);
								MemDC mem(p.hdc, p.rcPaint);
								::FillRect(mem.m_hdc, &mem.m_rect, ::GetSysColorBrush(DC_BRUSH));
								DefWndProc(WM_PRINTCLIENT, WPARAM(mem.m_hdc), LPARAM(PRF_CHECKVISIBLE|PRF_NONCLIENT|PRF_CLIENT));
							}
							return S_OK;
						}
						break;
					}
				}
				return DefWndProc(message, wparam, lparam);
			}
			LRESULT DefWndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				return m_oldproc != nullptr
					? ::CallWindowProc(m_oldproc, m_hwnd, message, wparam, lparam)
					: ::DefWindowProc(m_hwnd, message, wparam, lparam);
			}

			// Message map function
			// 'hwnd' is the handle of the parent window that contains this control.
			// Messages processed here are the messages sent to the parent window, *not* messages for this window
			// Only change 'result' when specifically returning a result (it defaults to S_OK)
			// Return true to halt message processing, false to allow other controls to process the message
			virtual bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
			{
				// Default handling of parent window messages for all controls
				// Subclassed controls should override this method
				switch (message)
				{
				default:
					{
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_INITDIALOG:
					{
						Attach(::GetDlgItem(hwnd, m_id));
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_SIZE:
					{
						ResizeToParent(true);
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_DESTROY:
					{
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						::DestroyWindow(m_hwnd); // Parent window is being destroy, destroy this window to
						break;
					}
				}

				return false;
			}

			// Forward a windows message to child controls
			bool ForwardToChildren(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
			{
				for (auto& c : m_child)
					if (c->ProcessWindowMessage(hwnd, message, wparam, lparam, result))
						return true;
				return false;
			}

			struct Internal {};
			Control(Internal, int id, Control* parent, TCHAR const* name, EAnchor anchor)
				:m_hwnd()
				,m_id(id)
				,m_name(name)
				,m_parent()
				,m_child()
				,m_anchor(anchor)
				,m_pos_offset()
				,m_colour_fore(CLR_INVALID)
				,m_colour_back(CLR_INVALID)
				,m_dbl_buffer(false)
				,m_thunk()
				,m_oldproc()
				,m_thread_id(std::this_thread::get_id())
			{
				m_thunk.Init(DWORD_PTR(StaticWndProc), this);
				Parent(parent);
			}

		public:

			// Create constructor
			// Use this constructor to create a new instance of a window
			Control(TCHAR const* wndclass_name
				,TCHAR const* text
				,int x, int y, int w, int h
				,int id = IDC_UNUSED
				,HWND hwndparent = 0
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = 0
				,DWORD ex_style = 0
				,TCHAR const* name = nullptr
				,HMENU menu = nullptr
				,void* init_param = nullptr)
				:Control(Internal(), id, parent, name, anchor)
			{
				m_hwnd = ::CreateWindowEx(ex_style, wndclass_name, text, style, x, y, w, h, hwndparent, menu, GetModuleHandle(nullptr), init_param);
				Throw(m_hwnd != 0, "CreateWindowEx failed");

				RecordPosOffset();
				Font(HFONT(GetStockObject(DEFAULT_GUI_FONT)));

				if (style & WS_VISIBLE)
				{
					ShowWindow(m_hwnd, SW_SHOW);
					UpdateWindow(m_hwnd);
				}
			}

			// Attach constructor
			// Use this constructor when you intend to Attach this instance to an existing hwnd
			// Set 'id' != -1 to have the control automatically attach during WM_INITDIALOG
			Control(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(Internal(), id, parent, name, anchor)
			{}
			~Control()
			{
				Parent(nullptr);
			}

			operator HWND() const { return m_hwnd; }

			// Attach/Detach this control wrapper to/form the associated window handle
			void Attach(HWND hwnd)
			{
				assert(m_hwnd == nullptr);
				m_hwnd = hwnd;
				RecordPosOffset();
				HookWndProc(true);
			}
			void Detach()
			{
				if (m_hwnd == nullptr) return;
				HookWndProc(false);
				m_hwnd = nullptr;
			}

			// Get/Set the parent of this control.
			// Note this parent is not necessarily the same parent as ::GetParent()
			// Controls such as GroupBox, Splitter, etc are parents of child controls
			Control* Parent() const { return m_parent; }
			void Parent(Control* parent)
			{
				if (m_parent != parent)
				{
					if (m_parent != nullptr) // Remove from existing parent
					{
						auto& c = m_parent->m_child;
						c.erase(std::remove(c.begin(), c.end(), this), c.end());
					}
					m_parent = parent;
					if (m_parent != nullptr) // Add to new parent
					{
						m_parent->m_child.push_back(this);
					}
				}
			}

			// Get the top level control. This is typically the window containing this control
			Control const* TopLevelControl() const
			{
				if (m_parent == nullptr) return this;
				auto p = m_parent;
				for (;p->m_parent != nullptr; p = p->m_parent) {}
				return p;
			}

			// Get/Set the window style
			LONG_PTR Style() const
			{
				assert(::IsWindow(m_hwnd));
				return ::GetWindowLongPtr(m_hwnd, GWL_STYLE);
			}
			void Style(LONG_PTR style)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowLongPtr(m_hwnd, GWL_STYLE, style);
			}

			// Get/Set the extended window style
			LONG_PTR StyleEx() const
			{
				assert(::IsWindow(m_hwnd));
				return ::GetWindowLongPtr(m_hwnd, GWL_EXSTYLE);
			}
			void StyleEx(LONG_PTR style)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowLongPtr(m_hwnd, GWL_EXSTYLE, style);
			}

			// Get/Set the text in the combo
			string Text() const
			{
				assert(::IsWindow(m_hwnd));
				string s;
				s.resize(::GetWindowTextLength(m_hwnd) + 1, 0);
				if (!s.empty()) s.resize(::GetWindowText(m_hwnd, &s[0], int(s.size())));
				return s;
			}
			void Text(string text)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowText(m_hwnd, text.c_str());
			}

			// Enable/Disable the control
			bool Enabled() const
			{
				assert(::IsWindow(m_hwnd));
				return ::IsWindowEnabled(m_hwnd) != 0;
			}
			void Enabled(bool enabled)
			{
				assert(::IsWindow(m_hwnd));
				::EnableWindow(m_hwnd, enabled);
			}

			// Get/Set visibility of this control
			bool Visible() const
			{
				assert(::IsWindow(m_hwnd));
				return (Style() & WS_VISIBLE) != 0;
			}
			void Visible(bool vis)
			{
				assert(::IsWindow(m_hwnd));
				if (vis)
					Style(Style() |  WS_VISIBLE);
				else
					Style(Style() & ~WS_VISIBLE);
			}

			// Set focus to this control, returning the handle of the previous window with focus
			HWND Focus()
			{
				assert(::IsWindow(m_hwnd));
				return ::SetFocus(m_hwnd);
			}

			// Get/Set the font for the control
			HFONT Font() const
			{
				assert(::IsWindow(m_hwnd));
				return HFONT(::SendMessage(m_hwnd, WM_GETFONT, 0, 0));
			}
			void Font(HFONT font)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessage(m_hwnd, WM_SETFONT, WPARAM(font), TRUE);
			}

			// Invalidate the control for redraw
			bool Invalidate(bool erase = false) throw()
			{
				assert(::IsWindow(m_hwnd));
				return ::InvalidateRect(m_hwnd, nullptr, erase) != 0;
			}
			bool InvalidateRect(Rect* rect = nullptr, bool erase = false) throw()
			{
				assert(::IsWindow(m_hwnd));
				return ::InvalidateRect(m_hwnd, rect, erase) != 0;
			}

			// Get/Set double buffering for the control
			bool DoubleBuffered() const { return m_dbl_buffer; }
			void DoubleBuffered(bool dbl_buffer) { m_dbl_buffer = dbl_buffer; }

			// Get/Set the control's background colour
			COLORREF BackColor() const
			{
				assert(::IsWindow(m_hwnd));
				DC dc(::GetDC(m_hwnd));
				return ::GetBkColor(dc);
			}
			void BackColor(COLORREF col)
			{
				assert(::IsWindow(m_hwnd));
				m_colour_back = col;
				Invalidate();
			}

			// Get/Set the control's foreground colour
			COLORREF ForeColor() const
			{
				assert(::IsWindow(m_hwnd));
				ClientDC dc(m_hwnd);
				return ::GetTextColor(dc);
			}
			void ForeColor(COLORREF col)
			{
				assert(::IsWindow(m_hwnd));
				m_colour_fore = col;
				Invalidate();
			}

			// Get/Set the client rect for the window
			Rect ClientRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect r;
				Throw(::GetClientRect(m_hwnd, &r), "GetClientRect failed.");
				return r;
			}
			void ClientRect(Rect const& r, bool repaint = false)
			{
				assert(::IsWindow(m_hwnd));
				auto rect = r;
				Throw(::AdjustWindowRectEx(&rect, DWORD(Style()), BOOL(::GetMenu(m_hwnd) != nullptr), DWORD(StyleEx())), "AdjustWindowRectEx failed.");
				auto wnd = WindowRect();
				rect.left += wnd.left;
				rect.right += wnd.left;
				rect.top += wnd.top;
				rect.bottom += wnd.top;
				WindowRect(rect, repaint);
			}

			// Get/Set the window rect
			Rect WindowRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect r;
				Throw(::GetWindowRect(m_hwnd, &r), "GetWindowRect failed.");
				return r;
			}
			void WindowRect(Rect r, bool repaint = false)
			{
				assert(::IsWindow(m_hwnd));

				// For a top level window, MoveWindow positions the window relative to the screen.
				// For child windows, MoveWindow positions the window relative to the parent client area
				auto top = TopLevelControl();
				if (top != this)
				{
					::ScreenToClient(top->m_hwnd, &r.topleft());
					::ScreenToClient(top->m_hwnd, &r.bottomright());
				}
				Throw(::MoveWindow(m_hwnd, r.left, r.top, r.width(), r.height(), repaint), "MoveWindow failed.");
			}

			// Set redraw mode on or off
			void Redraw(bool redraw)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessage(m_hwnd, WM_SETREDRAW, WPARAM(redraw ? TRUE : FALSE), 0);
			}
		};

		// Base class for an application or dialog window
		template <typename Derived> struct Form :Control
		{
		protected:
			template <typename D> friend struct Form;

			HINSTANCE m_hinst;           // Module instance
			bool      m_app_main_window; // True if this is the main application window
			int       m_menu_id;         // The id of the main menu
			bool      m_is_dialog;       // True if the form is a modal or modeless dialog
			bool      m_modal;           // True if this is a dialog being display modally

			struct InitParam
			{
				Form<Derived>* m_this;
				LPARAM m_lparam;
				InitParam(Form<Derived>* this_, LPARAM lparam) :m_this(this_) ,m_lparam(lparam) {}
			};

			// A window class is a template from which window instances are created. The WNDCLASS contains a static function
			// for the WndProc. We need a way to set the WndProc of a newly created window to the Control::StaticWndProc which
			// expects the 'HWND' parameter to actually be a pointer to the class instance (thanks to the thunk). Use a WndProc
			// function that handles the WM_NCCREATE message and updates the WndProc for the pointer passed via the CREATESTRUCT
			static ATOM RegisterWndClass(Form<Derived>* this_)
			{
				// Ensure the window class for this form has been registered
				// As of C++11, static initialisation is thread safe
				static ATOM atom = [=]
					{
						// Register the window class if not already registered
						WNDCLASSEX wc;
						auto a = ATOM(::GetClassInfoEx(this_->m_hinst, Derived::WndClassName(this_), &wc));
						if (a != 0) return a;
						
						// Allow 'Derived' to override the WndClassInfo. If the WndProc is not
						// null, assume that Derived has it's own special WndProc.
						wc = Derived::WndClassInfo(this_);
						wc.lpfnWndProc = wc.lpfnWndProc ? wc.lpfnWndProc : &InitWndProc;
						a = ATOM(::RegisterClassEx(&wc));
						if (a != 0) return a;

						Throw(false, "RegisterClassEx failed");
						return ATOM();
					}();
				return atom;
			}
			static LRESULT __stdcall InitWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				if (message == WM_NCCREATE)
				{
					// Only 'Form' derived windows will have this function as the window proc,
					// so we can replace the wndproc with DefWindowProc before calling 'HookWndProc'
					// This prevents an infinite recursion when me->DefWndProc is called.
					::SetWindowLongPtr(hwnd, GWLP_WNDPROC, LONG_PTR(&::DefWindowProc));
					auto init = reinterpret_cast<InitParam*>(reinterpret_cast<CREATESTRUCT*>(lparam)->lpCreateParams);
					init->m_this->Attach(hwnd);
					return init->m_this->WndProc(message, wparam, lparam);
				}
				return ::DefWindowProc(hwnd, message, wparam, lparam);
			}
			static LRESULT __stdcall InitDlgProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				if (message == WM_INITDIALOG)
				{
					// Only 'Form' derived windows will have this function as the window proc,
					// so we can replace the wndproc with DefWindowProc before calling 'HookWndProc'
					// This prevents an infinite recursion when me->DefWndProc is called.
					::SetWindowLongPtr(hwnd, GWLP_WNDPROC, LONG_PTR(&::DefWindowProc));
					auto init = reinterpret_cast<InitParam*>(lparam);
					init->m_this->Attach(hwnd);
					return init->m_this->WndProc(message, wparam, init->m_lparam);
				}
				return ::DefWindowProc(hwnd, message, wparam, lparam);
			}

			// Default wndclass description for windows of this type
			// Derived can override this for custom wndclass definitions
			static WNDCLASSEX WndClassInfo(Form<Derived>* this_)
			{
				WNDCLASSEX wc;
				wc.cbSize        = sizeof(WNDCLASSEX);
				wc.style         = CS_HREDRAW | CS_VREDRAW;
				wc.cbClsExtra    = 0;
				wc.cbWndExtra    = 0;
				wc.hInstance     = this_->m_hinst;
				wc.hIcon         = 0; //::LoadIcon(wc.hInstance, MAKEINTRESOURCE(IDI_ICON));
				wc.hIconSm       = 0; //::LoadIcon(wc.hInstance, MAKEINTRESOURCE(IDI_SMALL));
				wc.hCursor       = 0; //::LoadCursor(NULL, IDC_ARROW);
				wc.hbrBackground = (HBRUSH)(COLOR_WINDOW+1);
				wc.lpszMenuName  = nullptr; //MAKEINTRESOURCE(IDM_MENU);
				wc.lpszClassName = Derived::WndClassName(this_);
				wc.lpfnWndProc   = nullptr; // Leave as null to use the thunk
				return wc;
			}
			static LPCTSTR WndClassName(Form<Derived>* this_)
			{
				// Auto name, derived can overload this function
				thread_local static TCHAR name[64];
				_stprintf_s(name, TEXT("wingui::%p"), this_);
				return name;
			}

			// Window proc
			LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam) override
			{
				// Check if this message is a dialog message
				auto msg = MSG{m_hwnd, message, wparam, lparam, ::GetMessageTime(), ::GetMessagePos()};
				if (m_is_dialog)
				{
					RAII<bool> prevent_recursion(m_is_dialog, false);
					if (::IsDialogMessage(m_hwnd, &msg))
						return S_OK;
				}

				// 'TranslateMessage' doesn't change 'msg' it only adds WM_CHAR,etc
				// messages to the message queue for WK_KEYDOWN,etc events
				::TranslateMessage(&msg);

				// Forward the message to the message map function which will forward
				// the message to nested controls
				LRESULT result = S_OK;
				if (ProcessWindowMessage(m_hwnd, message, wparam, lparam, result))
					return result;

				// If neither this form or any child controls handled the message
				// use the default message handler for this form
				return Control::WndProc(message, wparam, lparam);
			}

			// Message map function
			// 'hwnd' is the handle of the parent window that contains this control.
			// Messages processed here are the messages sent to the parent window, *not* messages for this window
			// Only change 'result' when specifically returning a result (it defaults to S_OK)
			// Return true to halt message processing, false to allow other controls to process the message
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
			{
				switch (message)
				{
				default:
					{
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
				case WM_INITDIALOG:
					{
						// Handle WM_INITDIALOG because we attach to the hwnd in InitWndProc
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						return true;
					}
				case WM_CLOSE:
					{
						Close();
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_DESTROY:
					{
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						if (m_app_main_window)
							::PostQuitMessage(0);
						HookWndProc(false);
						m_hwnd = nullptr;
						return true;
					}
				case WM_COMMAND:
					{
						// Handle commands from the main menu
						if (HIWORD(wparam) == 0 && HandleMenu(LOWORD(wparam))) return true;
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
				}
				return false;
			}

			// Default main menu handler
			virtual bool HandleMenu(WORD)
			{
				return false;
				// Example implementation
				// switch (menu_id)
				// {
				// default: return false;
				// case ID_FILE_EXIT: Close(); return true;
				// }
			}

		public:

			enum { DefaultFormStyle = DS_SETFONT | DS_FIXEDSYS | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_CLIPCHILDREN | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_VISIBLE };
			enum { DefaultFormStyleEx = WS_EX_OVERLAPPEDWINDOW | WS_EX_APPWINDOW | WS_EX_COMPOSITED };

			// Frame Window Constructor
			// Use this constructor to create frame windows (i.e. windows not based on a dialog resource)
			//  - hwnd created in constructor
			//  - use Show() to display the window
			//  - use 'parent == ApplicationMainWindow' to cause the app to exit when this window is closed
			Form(TCHAR const* title
				,HWND parent = nullptr
				,int x = CW_USEDEFAULT, int y = CW_USEDEFAULT
				,int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,DWORD style = DefaultFormStyle
				,DWORD ex_style = DefaultFormStyleEx
				,int menu_id = IDC_UNUSED
				,LPARAM init_param = 0)
				:Control(IDC_UNUSED, nullptr, title)
				,m_hinst(::GetModuleHandle(nullptr))
				,m_app_main_window(parent == ApplicationMainWindow)
				,m_menu_id(menu_id)
				,m_is_dialog(false)
				,m_modal(false)
			{
				parent = !m_app_main_window ? parent : nullptr;
				
				// Ensure the class is registered
				auto atom = RegisterWndClass(this);

				// Load the optional menu
				HMENU menu = menu_id != IDC_UNUSED ? ::LoadMenu(m_hinst, MAKEINTRESOURCE(menu_id)) : nullptr;

				// Create an instance of the window class
				InitParam lparam(this, init_param);
				::CreateWindowEx(ex_style, MAKEINTATOM(atom), title, style, x, y, w, h, parent, menu, m_hinst, &lparam);
				Throw(m_hwnd != 0, "CreateWindowEx failed");
			}

			// Dialog Constructor
			// Use this constructor to create a window from a dialog resource description
			//  - hwnd is not created until Show() or ShowDialog() are called
			Form(int idd, TCHAR const* name = nullptr)
				:Control(idd, nullptr, name)
				,m_hinst(::GetModuleHandle(nullptr))
				,m_app_main_window(false)
				,m_menu_id(IDC_UNUSED)
				,m_is_dialog(true)
				,m_modal(false)
			{}
			~Form()
			{
				Close();
			}

			// Display the form non-modally
			template <typename D> void Show(Form<D>* parent = nullptr, LPARAM init_param = 0)
			{
				m_modal = false;

				// Create the dialog from its dialog resource id if not created yet
				if (m_hwnd == nullptr && m_is_dialog)
				{
					assert(m_id != IDC_UNUSED && "Modeless dialog without a resource id");
					m_app_main_window = parent == ApplicationMainWindow;
					parent = !m_app_main_window ? parent : nullptr;

					InitParam lparam(this, init_param);
					HWND parenthwnd = parent ? parent->m_hwnd : nullptr;
					m_hwnd = ::CreateDialogParam(m_hinst, MAKEINTRESOURCE(m_id), parenthwnd, (DLGPROC)&InitDlgProc, LPARAM(&lparam));
					Throw(m_hwnd != nullptr, "CreateDialogParam failed");
				}
				Throw(m_hwnd != nullptr, "Window not created");

				ShowWindow(m_hwnd, SW_SHOW);
				UpdateWindow(m_hwnd);
			}

			// Display the form modally
			template <typename D> EDialogResult ShowDialog(Form<D>* parent = nullptr, LPARAM init_param = 0)
			{
				assert(m_hwnd == nullptr);
				m_modal = true;

				if (parent != nullptr)
				{
					m_app_main_window = parent == ApplicationMainWindow;
					parent = !m_app_main_window ? parent : nullptr;
				}

				InitParam lparam(this, init_param);
				HWND parenthwnd = parent ? parent->m_hwnd : nullptr;
				return EDialogResult(::DialogBoxParam(m_hinst, MAKEINTRESOURCE(m_id), parenthwnd, (DLGPROC)&InitDlgProc, LPARAM(&lparam)));
			}

			// Close this form
			bool Close(EDialogResult ret_code = EDialogResult::Ok)
			{
				if (m_hwnd == nullptr) return false;
				return (m_modal
					? ::EndDialog(m_hwnd, INT_PTR(ret_code))
					: ::DestroyWindow(m_hwnd)) != 0;
			}
		};

		#pragma region Controls
		struct Label :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			Label(TCHAR const* text
				,int x, int y, int w, int h
				,int id = IDC_UNUSED
				,HWND hwndparent = 0
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = WS_CHILD | WS_VISIBLE | SS_LEFT
				,DWORD ex_style = 0
				,TCHAR const* name = nullptr
				,void* init_param = nullptr)
				:Control(_T("STATIC"), text, x, y, w, h, id, hwndparent, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}

			Label(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_CTLCOLORSTATIC:
					if (m_id == ::GetDlgCtrlID((HWND)lparam))
					{
						if (Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result))
							return true;

						auto hdc = (HDC)wparam;
						if (m_colour_fore != CLR_INVALID) ::SetTextColor(hdc, m_colour_fore);
						if (m_colour_back != CLR_INVALID)
						{
							::SetBkColor(hdc, m_colour_back);
							::SetDCBrushColor(hdc, m_colour_back);
							result = LRESULT(::GetStockObject(DC_BRUSH));
							return true;
						}
					}
					break;
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct Button :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			Button(TCHAR const* text
				,int x, int y, int w, int h
				,int id = IDC_UNUSED
				,HWND hwndparent = 0
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = WS_CHILD | WS_VISIBLE | WS_TABSTOP | BS_PUSHBUTTON | BS_CENTER | BS_TEXT
				,DWORD ex_style = 0
				,TCHAR const* name = nullptr
				,void* init_param = nullptr)
				:Control(_T("BUTTON"), text, x, y, w, h, id, hwndparent, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}

			Button(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Events
			EventHandler<Button&, EmptyArgs const&> Click;

			// Handlers
			virtual void OnClick()
			{
				Click(*this, EmptyArgs());
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_COMMAND:
					if (LOWORD(wparam) != m_id) break;
					switch (HIWORD(wparam))
					{
					case BN_CLICKED:
						OnClick();
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct CheckBox :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			CheckBox(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Get/Set the checked state
			bool Checked() const
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessage(m_hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED;
			}
			void Checked(bool checked)
			{
				assert(::IsWindow(m_hwnd));
				auto is_checked = Checked();
				::SendMessage(m_hwnd, BM_SETCHECK, checked ? BST_CHECKED : BST_UNCHECKED, 0);
				if (is_checked != checked) OnCheckedChanged();
			}

			// Events
			EventHandler<CheckBox&, EmptyArgs const&> Click;
			EventHandler<CheckBox&, EmptyArgs const&> CheckedChanged;

			// Handlers
			virtual void OnClick()
			{
				Click(*this, EmptyArgs());
			}
			virtual void OnCheckedChanged()
			{
				CheckedChanged(*this, EmptyArgs());
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_COMMAND:
					if (LOWORD(wparam) != m_id) break;
					switch (HIWORD(wparam))
					{
					case BN_CLICKED:
						OnClick();
						if (Style() & BS_AUTOCHECKBOX) OnCheckedChanged();
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct TextBox :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			TextBox(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// The number of characters in the text
			int TextLength() const
			{
				assert(::IsWindow(m_hwnd));
				auto len = GETTEXTLENGTHEX{GTL_DEFAULT, CP_ACP};
				return int(::SendMessage(m_hwnd, EM_GETTEXTLENGTHEX, WPARAM(&len), 0));
			}

			// The number of lines of text
			int LineCount() const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessage(m_hwnd, EM_GETLINECOUNT, 0, 0));
			}

			// The length (in characters) of the line containing the character at the given index
			// 'char_index = -1' means the number of *unselected* characters on the lines spanned by the selection.
			int LineLength(int char_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessage(m_hwnd, EM_LINELENGTH, WPARAM(char_index), 0));
			}

			// Gets the character index of the first character on the given line
			// 'line_index = -1' means the current line containing the caret
			int CharFromLine(int line_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessage(m_hwnd, EM_LINEINDEX, WPARAM(line_index), 0));
			}

			// Gets the index of the line that contains 'char_index'
			int LineFromChar(int char_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessage(m_hwnd, EM_EXLINEFROMCHAR, 0, LPARAM(char_index)));
			}

			RangeI Selection() const
			{
				assert(::IsWindow(m_hwnd));
				RangeI r;
				::SendMessage(m_hwnd, EM_GETSEL, WPARAM(&r.beg), LPARAM(&r.end));
				return r;
			}
			void Selection(RangeI range, bool scroll_to_caret = true)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessage(m_hwnd, EM_SETSEL, range.beg, range.end);
				if (scroll_to_caret) ScrollToCaret();
			}
			void SelectAll()
			{
				Selection(RangeI(0, -1));
			}
			void ScrollToCaret()
			{
				assert(::IsWindow(m_hwnd));

				// There is a bug that means scrolling only works if the control has focus
				// There is a workaround however, using hide selection
				auto nohidesel = Style() & ES_NOHIDESEL;
				Style(Style() | ES_NOHIDESEL);
				::SendMessage(m_hwnd, EM_SCROLLCARET, 0, 0);
				Style((Style() & ~ES_NOHIDESEL) | nohidesel);
			}

			// Events
			EventHandler<TextBox&, EmptyArgs const&> TextChanged;

			// Handlers
			virtual void OnTextChanged()
			{
				TextChanged(*this, EmptyArgs());
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_COMMAND:
					{
						if (LOWORD(wparam) != m_id) break;
						switch (HIWORD(wparam)) {
						case EN_CHANGE: OnTextChanged(); break;
						}
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct ComboBox :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			ComboBox(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Get the number of items in the combo box
			int Count() const
			{
				assert(::IsWindow(m_hwnd));
				auto c = int(::SendMessage(m_hwnd, CB_GETCOUNT, 0, 0L));
				Throw(c != CB_ERR, "Error retrieving combo box item count");
				return c;
			}

			// Get the item at index position 'index'
			string Item(int index) const
			{
				assert(::IsWindow(m_hwnd));

				string s;
				auto len = ::SendMessage(m_hwnd, CB_GETLBTEXTLEN, index, 0);
				Throw(len != CB_ERR, std::string("ComboBox: Invalid item index ").append(std::to_string(index)));
				if (len == 0) return s;

				s.resize(len);
				s.resize(::SendMessage(m_hwnd, CB_GETLBTEXT, index, (LPARAM)&s[0]));
				return s;
			}

			// Get/Set the selected index
			int SelectedIndex() const
			{
				assert(::IsWindow(m_hwnd));
				auto index = int(::SendMessage(m_hwnd, CB_GETCURSEL, 0, 0L));
				return index;
			}
			void SelectedIndex(int index)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessage(m_hwnd, CB_SETCURSEL, index, 0L);
			}

			// Get the selected item
			string SelectedItem() const
			{
				return Item(SelectedIndex());
			}

			// Remove all items from the combo dropdown list
			void ResetContent()
			{
				assert(::IsWindow(m_hwnd));
				::SendMessage(m_hwnd, CB_RESETCONTENT, 0, 0L);
			}

			// Add a string to the combo box dropdown list
			int AddItem(LPCTSTR item)
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessage(m_hwnd, CB_ADDSTRING, 0, (LPARAM)item));
			}
			void AddItems(std::initializer_list<LPCTSTR> items)
			{
				for (auto i : items)
					AddItem(i);
			}

			// Events
			EventHandler<ComboBox&, EmptyArgs const&> DropDown;
			EventHandler<ComboBox&, EmptyArgs const&> SelectedIndexChanged;

			// Handlers
			LRESULT OnDropDown()
			{
				DropDown(*this, EmptyArgs());
				return S_OK;
			}
			LRESULT OnSelectedIndexChanged()
			{
				SelectedIndexChanged(*this, EmptyArgs());
				return S_OK;
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override 
			{
				switch (message)
				{
				case WM_COMMAND:
					if (LOWORD(wparam) != m_id) break;
					switch (HIWORD(wparam))
					{
					case CBN_DROPDOWN: result = OnDropDown(); return true;
					case CBN_SELCHANGE: result = OnSelectedIndexChanged(); return true;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct GroupBox :Control
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			GroupBox(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}
		};
		struct RichTextBox :TextBox
		{
			// Note, if you want events from this control is must have an id != IDC_UNUSED
			RichTextBox(int id = IDC_UNUSED, Control* parent = nullptr, TCHAR const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:TextBox(id, parent, name, anchor)
			{}
		};
		#pragma endregion
	}
}

#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

#ifdef thread_local_defined
#undef thread_local_defined
#undef thread_local
#endif

// Example code showing basic use
#if 0
using namespace pr::wingui;

// About dialog
struct About :Form<About>
{
	Button m_btn_ok;

	enum { IDD = IDD_ABOUTBOX };
	About()
		:Form<About>()
		,m_btn_ok(IDOK, this)
	{
		// Hook up an event handler to close the dialog when the ok button is clicked
		m_btn_ok.Click += [&](Button&, EmptyArgs const&){ Close(); };
	}
};

// Application frame window
struct Main :Form<Main>
{
	Label m_lbl;
	Button m_btn;

	// Note: IDD not needed since Main isn't created from a dialog resource
	enum { IDC_BTN = 100, };
	Main()
		:Form<Main>(_T("Demo Window"), ApplicationMainWindow, CW_USEDEFAULT, CW_USEDEFAULT, 320, 200)
		,m_lbl(_T("hello world"), 80, 20, 100, 16, IDC_UNUSED, m_hwnd, this)
		,m_btn(_T("click me!"), 200, 130, 80, 20, IDC_BTN, m_hwnd, this, EAnchor::Right|EAnchor::Bottom)
	{
		// Show a modal dialog when the button is clicked
		m_btn.Click += [&](Button&,EmptyArgs const&)
			{
				About about;
				about.ShowDialog(*this);
			};
	}
};

// WinMain
int __stdcall _tWinMain(HINSTANCE hInstance, HINSTANCE, LPTSTR, int)
{
	InitCtrls();

	Main main;
	MessageLoop loop;
	return loop.Run();
}
#endif