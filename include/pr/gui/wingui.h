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
#include <shellapi.h>
#include <uxtheme.h>

#include "pr/common/fmt.h"
#include "pr/gui/messagemap_dbg.h"

#pragma comment(lib, "comctl32.lib")
#pragma comment(lib, "uxtheme.lib")

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#define thread_local_defined
#endif

#pragma warning(push)
#pragma warning(disable: 4351) // new behavior: elements of array will be default initialized

namespace pr
{
	namespace gui
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
		inline ECommonControl operator | (ECommonControl lhs, ECommonControl rhs) { return ECommonControl(int(lhs) | int(rhs)); }
		inline ECommonControl operator & (ECommonControl lhs, ECommonControl rhs) { return ECommonControl(int(lhs) & int(rhs)); }

		// Autosize anchors
		enum class EAnchor { None = 0, Left = 1 << 0, Top = 1 << 1, Right = 1 << 2, Bottom = 1 << 3, All = Left|Top|Right|Bottom };
		inline EAnchor operator | (EAnchor lhs, EAnchor rhs) { return EAnchor(int(lhs) | int(rhs)); }
		inline EAnchor operator & (EAnchor lhs, EAnchor rhs) { return EAnchor(int(lhs) & int(rhs)); }

		// Window docking
		enum class EDock
		{
			None   = 0,
			Fill   = 1,
			Top    = 2,
			Bottom = 3,
			Left   = 4,
			Right  = 5
		};

		// Dialog result
		enum class EDialogResult
		{
			Ok       = IDOK,
			Cancel   = IDCANCEL,
			Abort    = IDABORT,
			Retry    = IDRETRY,
			Ignore   = IDIGNORE,
			Yes      = IDYES,
			No       = IDNO,
			Close    = IDCLOSE,
			Help     = IDHELP,
			TryAgain = IDTRYAGAIN,
			Continue = IDCONTINUE,
			Timeout  = IDTIMEOUT,
		};

		// Control key state
		enum class EControlKey
		{
			None   = 0,
			LShift = 1 << 0,
			RShift = 1 << 1,
			Shift  = LShift|RShift,
			LCtrl  = 1 << 2,
			RCtrl  = 1 << 3,
			Ctrl   = LCtrl | RCtrl,
			LAlt   = 1 << 4,
			RAlt   = 1 << 5,
			Alt    = LAlt | RAlt,
		};
		inline EControlKey operator |  (EControlKey lhs, EControlKey rhs) { return EControlKey(int(lhs) | int(rhs)); }
		inline EControlKey operator &  (EControlKey lhs, EControlKey rhs) { return EControlKey(int(lhs) & int(rhs)); }
		inline EControlKey operator |= (EControlKey& lhs, EControlKey rhs) { return lhs = lhs | rhs; }
		inline EControlKey operator &= (EControlKey& lhs, EControlKey rhs) { return lhs = lhs & rhs; }

		// Mouse key state, used in mouse down/up events
		enum class EMouseKey
		{
			None     = 0,
			Left     = MK_LBUTTON, // 0x0001
			Right    = MK_RBUTTON, // 0x0002
			Shift    = MK_SHIFT,   // 0x0004
			Ctrl     = MK_CONTROL, // 0x0008
			Middle   = MK_MBUTTON, // 0x0010
			XButton1 = MK_XBUTTON1,// 0x0020
			XButton2 = MK_XBUTTON2,// 0x0040
			Alt      = 0x0080,     // There is not MK_ define for alt, this is tested using GetKeyState
		};
		inline EMouseKey operator | (EMouseKey lhs, EMouseKey rhs) { return EMouseKey(int(lhs) | int(rhs)); }
		inline EMouseKey operator & (EMouseKey lhs, EMouseKey rhs) { return EMouseKey(int(lhs) & int(rhs)); }

		// Don't add WS_VISIBLE to the default style. Derived forms should choose when to be visible at the end of their constructors
		// WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX)
		// WS_EX_COMPOSITED adds automatic double buffering, which doesn't work for Dx apps
		enum { DefaultFormStyle = DS_SETFONT | DS_FIXEDSYS | WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN | WS_CLIPSIBLINGS };
		enum { DefaultFormStyleEx = WS_EX_OVERLAPPEDWINDOW | WS_EX_APPWINDOW };

		// WS_POPUPWINDOW = (WS_POPUP|WS_BORDER|WS_SYSMENU)
		enum { DefaultDialogStyle = DefaultFormStyle };
		enum { DefaultDialogStyleEx = DefaultFormStyleEx };

		enum { DefaultControlStyle = WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS };
		enum { DefaultControlStyleEx = 0 };

		#pragma endregion

		#pragma region Support Functions

		// Cast with overflow check
		template <typename TTo, typename TFrom> TTo cast(TFrom from)
		{
			assert(static_cast<TFrom>(static_cast<TTo>(from)) == from && "overflow or underflow in cast");
			return static_cast<TTo>(from);
		}

		// Convert to byte pointer
		template <typename T> byte const* bptr(T const* t) { return reinterpret_cast<byte const*>(t); }
		template <typename T> byte*       bptr(T*       t) { return reinterpret_cast<byte*      >(t); }

		// Append bytes
		template <typename TCont> void append(TCont& cont, void const* x, size_t byte_count)
		{
			cont.insert(end(cont), bptr(x), bptr(x) + byte_count);
		}

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

		// Replace macros from windowsx.h
		inline int GetXLParam(LPARAM lparam) { return int(short(LOWORD(lparam))); } // GET_X_LPARAM
		inline int GetYLParam(LPARAM lparam) { return int(short(HIWORD(lparam))); } // GET_Y_LPARAM
		inline LPARAM MakeLParam(int x, int y) { return LPARAM(short(x) | short(y) << 16); }

		// Replace the MAKEINTATOM macro
		inline wchar_t const* MakeIntAtomW(ATOM atom) { return reinterpret_cast<wchar_t const*>(ULONG_PTR(WORD(atom))); } // MAKEINTATOM

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
			if (!from) return std::string();
			if (len == 0) len = strlen(from);
			return std::string(from, from+len);
		}
		inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
		{
			if (!from) return std::string();
			if (len == 0) len = wcslen(from);
			std::vector<char> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from, from + len, '_', &buffer[0]);
			return std::string(&buffer[0], &buffer[len]);
		}
		inline std::string Narrow(std::string const& from)  { return from; }
		inline std::string Narrow(std::wstring const& from) { return Narrow(from.c_str(), from.size()); }

		// Widen
		inline std::wstring Widen(wchar_t const* from, std::size_t len = 0)
		{
			if (!from) return std::wstring();
			if (len == 0) len = wcslen(from);
			return std::wstring(from, from+len);
		}
		inline std::wstring Widen(char const* from, std::size_t len = 0)
		{
			if (!from) return std::wstring();
			if (len == 0) len = strlen(from);
			std::vector<wchar_t> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).widen(from, from + len, &buffer[0]);
			return std::wstring(&buffer[0], &buffer[len]);
		}
		inline std::wstring Widen(std::wstring const& from) { return from; }
		inline std::wstring Widen(std::string const& from)  { return Widen(from.c_str(), from.size()); }

		#pragma endregion

		#pragma region Win32
		// Template specialised versions of the win32 api functions
		template <typename Char> struct Win32;
		template <> struct Win32<char>
		{
			static int WindowText(HWND hwnd, LPSTR lpString, int nMaxCount)
			{
				return ::GetWindowTextA(hwnd, lpString, nMaxCount);
			}
			static int WindowTextLength(HWND hwnd)
			{
				return ::GetWindowTextLengthA(hwnd);
			}
			static int MenuString(HMENU hMenu, UINT uIDItem, LPSTR lpString, int cchMax, UINT flags)
			{
				return ::GetMenuStringA(hMenu, uIDItem, lpString, cchMax, flags);
			}
		};
		template <> struct Win32<wchar_t>
		{
			static int WindowText(HWND hwnd, LPWSTR lpString, int nMaxCount)
			{
				return ::GetWindowTextW(hwnd, lpString, nMaxCount);
			}
			static int WindowTextLength(HWND hwnd)
			{
				return ::GetWindowTextLengthW(hwnd);
			}
			static int MenuString(HMENU hMenu, UINT uIDItem, LPWSTR lpString, int cchMax, UINT flags)
			{
				return ::GetMenuStringW(hMenu, uIDItem, lpString, cchMax, flags);
			}
		};
		#pragma endregion

		#pragma region Support structures

		// String
		using string = std::wstring;

		// Point
		struct Point :POINT
		{
			Point() :POINT() {}
			Point(long x_, long y_) { x = x_; y = y_; }
			explicit Point(SIZE sz) { x = sz.cx; y = sz.cy; }
			long operator[](int axis) const { return axis == 0 ? x : y; }
		};

		// Size
		struct Size :SIZE
		{
			Size() :SIZE() {}
			Size(long sx, long sy) { cx = sx; cy = sy; }
			explicit Size(POINT pt) { cx = pt.x; cy = pt.y; }
			operator Rect() const;
			long operator[](int axis) const { return axis == 0 ? cx : cy; }
		};

		// Rect
		struct Rect :RECT
		{
			Rect() :RECT()                                {}
			Rect(RECT const& r) :RECT(r)                  {}
			Rect(POINT const& pt, SIZE const& sz) :RECT() { left = pt.x; top = pt.y; right = pt.x + sz.cx; bottom = pt.y + sz.cy; }
			Rect(int l, int t, int r, int b)              { left = l; top = t; right = r; bottom = b; }
			explicit Rect(Size s)                         { left = top = 0; right = s.cx; bottom = s.cy; }
			long width() const                            { return right - left; }
			void width(long w)                            { right = left + w; }
			long height() const                           { return bottom - top; }
			void height(long h)                           { bottom = top + h; }
			Size size() const                             { return Size{width(), height()}; }
			void size(Size sz)                            { right = left + sz.cx; bottom = top + sz.cy; }
			int  size(int axis) const                     { return axis == 0 ? width() : height(); }
			Point centre() const                          { return Point((left + right) / 2, (top + bottom) / 2); }
			void centre(Point pt)                         { long w = width(), h = height(); left = pt.x-w/2; right = left+w; top = pt.y-h/2; bottom = top+h; }
			Point const* points() const                   { return reinterpret_cast<Point const*>(&left); }
			Point const& topleft() const                  { return points()[0]; }
			Point const& bottomright() const              { return points()[1]; }
			Point* points()                               { return reinterpret_cast<Point*>(&left); }
			Point& topleft()                              { return points()[0]; }
			Point& bottomright()                          { return points()[1]; }

			// This functions return false if the result is a zero rect (that's why I'm not using Throw())
			// The returned rect is the the bounding box of the geometric operation (note how that effects subtract)
			bool Contains(Point const& pt, bool incl = false) const
			{
				return incl
					? pt.x >= left && pt.x <= right && pt.y >= top && pt.y <= bottom
					: pt.x >= left && pt.x <  right && pt.y >= top && pt.y <  bottom;
			}
			Rect Offset(int dx, int dy) const                       { auto r = *this; ::OffsetRect(&r, dx, dy); return r; }
			Rect Inflate(int dx, int dy) const                      { auto r = *this; ::InflateRect(&r, dx, dy); return r; }
			Rect Adjust(int dl, int dt, int dr, int db) const       { auto r = *this; r.left += dl; r.top += dt; r.right += dr; r.bottom += db; return r; }
			Rect Intersect(Rect const& rhs) const                   { auto r = *this; ::IntersectRect(&r, this, &rhs); return r; }
			Rect Union(Rect const& rhs) const                       { auto r = *this; ::UnionRect(&r, this, &rhs); return r; }
			Rect Subtract(Rect const& rhs) const                    { auto r = *this; ::SubtractRect(&r, this, &rhs); return r; }
			Rect NormalizeRect() const
			{
				auto r = *this;
				if (r.left > r.right) std::swap(r.left, r.right);
				if (r.top > r.bottom) std::swap(r.top, r.bottom);
				return r;
			}

			operator Size() const { return size(); }
		};

		inline Point operator + (Point p, Size s) { return Point(p.x + s.cx, p.y + s.cy); }
		inline Size  operator + (Size l, Size r) { return Size(l.cx + r.cx, l.cy + r.cy); }
		inline Size  operator - (Point l, Point r) { return Size(l.x + r.x, l.y - r.y); }
		inline Point operator - (Point l, Size r) { return Point(l.x - r.cx, l.y - r.cy); }
		inline bool  operator == (Point const& l, Point const& r) { return l.x == r.x && l.y == r.y; }
		inline bool  operator != (Point const& l, Point const& r) { return !(l == r); }
		inline bool  operator == (Size const& l, Size const& r) { return l.cx == r.cx && l.cy == r.cy; }
		inline bool  operator != (Size const& l, Size const& r) { return !(l == r); }
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

		// Window min/max size and position
		struct MinMaxInfo :MINMAXINFO
		{
			// Set mask bits write to the min/max info
			// Unset bits read from the min/max info
			enum EMask
			{
				MaxSize      = 1 << 0,
				MaxPosition  = 1 << 1,
				MinTrackSize = 1 << 2,
				MaxTrackSize = 1 << 3,
			};
			EMask m_mask;
			
			MinMaxInfo()
				:MINMAXINFO()
				,m_mask()
			{
				ptMaxSize.x      = ::GetSystemMetrics(SM_CXVIRTUALSCREEN);
				ptMaxSize.y      = ::GetSystemMetrics(SM_CYVIRTUALSCREEN);
				ptMaxPosition.x  = ::GetSystemMetrics(SM_CXVIRTUALSCREEN);
				ptMaxPosition.y  = ::GetSystemMetrics(SM_CYVIRTUALSCREEN);
				ptMinTrackSize.x = ::GetSystemMetrics(SM_CXMINTRACK);
				ptMinTrackSize.y = ::GetSystemMetrics(SM_CYMINTRACK);
				ptMaxTrackSize.x = ::GetSystemMetrics(SM_CXMAXTRACK);
				ptMaxTrackSize.y = ::GetSystemMetrics(SM_CYMAXTRACK);
			}
			Rect Bounds() const
			{
				return Rect(0, 0, ptMaxSize.x, ptMaxSize.y);
			}
		};
		inline MinMaxInfo::EMask operator | (MinMaxInfo::EMask lhs, MinMaxInfo::EMask rhs) { return MinMaxInfo::EMask(int(lhs) | int(rhs)); }
		inline MinMaxInfo::EMask operator & (MinMaxInfo::EMask lhs, MinMaxInfo::EMask rhs) { return MinMaxInfo::EMask(int(lhs) & int(rhs)); }

		// Window position information
		struct WindowPos :WINDOWPOS
		{
			enum EFlags
			{
				None           = 0,
				NoSize         = SWP_NOSIZE        ,
				NoMove         = SWP_NOMOVE        ,
				NoZorder       = SWP_NOZORDER      ,
				NoRedraw       = SWP_NOREDRAW      ,
				NoActivate     = SWP_NOACTIVATE    ,
				FrameChanged   = SWP_FRAMECHANGED  ,
				ShowWindow     = SWP_SHOWWINDOW    ,
				HideWindow     = SWP_HIDEWINDOW    ,
				NoCopyBits     = SWP_NOCOPYBITS    ,
				NoOwnerZOrder  = SWP_NOOWNERZORDER ,
				NoSendChanging = SWP_NOSENDCHANGING,
				DrawFrame      = SWP_DRAWFRAME     ,
				NoReposition   = SWP_NOREPOSITION  ,
				DeferErase     = SWP_DEFERERASE    ,
				AsyncWindowpos = SWP_ASYNCWINDOWPOS,
			};
			WindowPos() :WINDOWPOS() {}
			WindowPos(int x_, int y_, int cx_, int cy_, EFlags flags_) :WINDOWPOS()
			{
				x = x_;
				y = y_;
				cx = cx_;
				cy = cy_;
				flags = flags_;
			}
		};
		inline WindowPos::EFlags operator | (WindowPos::EFlags lhs, WindowPos::EFlags rhs) { return WindowPos::EFlags(int(lhs) | int(rhs)); }
		inline WindowPos::EFlags operator & (WindowPos::EFlags lhs, WindowPos::EFlags rhs) { return WindowPos::EFlags(int(lhs) & int(rhs)); }

		// Monitor info
		struct MonitorInfo :MONITORINFO
		{
			MonitorInfo() :MONITORINFO() { cbSize = sizeof(MONITORINFO); }
		};

		// Metrics for the non-client regions of windows
		struct NonClientMetrics :NONCLIENTMETRICSW
		{
			NonClientMetrics() :NONCLIENTMETRICSW()
			{
				cbSize = sizeof(NONCLIENTMETRICSW);
				Throw(::SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(NONCLIENTMETRICS), this, 0), "Failed to read non-client system metrics");
			}
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
			Font(int nPointSize, wchar_t const* lpszFaceName, HDC hdc = nullptr, bool bBold = false, bool bItalic = false)
			{
				ClientDC clientdc(nullptr);
				auto hdc_ = hdc ? hdc : clientdc.m_hdc;

				auto lf = LOGFONTW{};
				lf.lfCharSet = DEFAULT_CHARSET;
				lf.lfWeight = bBold ? FW_BOLD : FW_NORMAL;
				lf.lfItalic = BYTE(bItalic ? TRUE : FALSE);
				::wcsncpy_s(lf.lfFaceName, _countof(lf.lfFaceName), lpszFaceName, _TRUNCATE);

				// convert nPointSize to logical units based on hDC
				auto pt = Point(0, ::MulDiv(::GetDeviceCaps(hdc_, LOGPIXELSY), nPointSize, 720)); // 72 points/inch, 10 decipoints/point
				auto ptOrg = Point{};
				::DPtoLP(hdc_, &pt, 1);
				::DPtoLP(hdc_, &ptOrg, 1);
				lf.lfHeight = -abs(pt.y - ptOrg.y);
		
				m_obj = ::CreateFontIndirectW(&lf);
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
			PaintStruct(HWND hwnd) :m_hwnd(hwnd) { Throw(::BeginPaint(m_hwnd, this) != nullptr, "BeginPaint failed"); }
			~PaintStruct()                       { Throw(::EndPaint(m_hwnd, this), "EndPaint failed"); }
		};

		// Menu
		struct Menu
		{
			HMENU m_menu;
			bool m_owned;
			Menu() :m_menu() ,m_owned() {}
			Menu(HMENU menu, bool owned = true) :m_menu(menu) ,m_owned(owned) {}
			Menu(Menu&& rhs) { std::swap(m_menu, rhs.m_menu); std::swap(m_owned, rhs.m_owned); }
			~Menu() { if (m_owned) DestroyMenu(m_menu); }
			operator HMENU() const { return m_menu; }
		};
		struct MenuItemInfo :MENUITEMINFO
		{
			MenuItemInfo()
				:MENUITEMINFO()
			{
				cbSize = sizeof(MENUITEMINFO);
			}
			MenuItemInfo(MENUITEMINFO const& mii)
				:MENUITEMINFO(mii)
			{}
		};

		// TrackMouseEvent
		struct TrackMouseEvent :TRACKMOUSEEVENT
		{
			TrackMouseEvent()
				:TRACKMOUSEEVENT()
			{
				cbSize = sizeof(TRACKMOUSEEVENT);
			}
			TrackMouseEvent(DWORD flags, HWND wnd_to_track, DWORD hovertime_ms = HOVER_DEFAULT)
				:TrackMouseEvent()
			{
				dwFlags     = flags;
				hwndTrack   = wnd_to_track;
				dwHoverTime = hovertime_ms;
			}
			TrackMouseEvent(TRACKMOUSEEVENT tme)
				:TRACKMOUSEEVENT(tme)
			{}
		};

		// Theme
		struct Theme
		{
			static bool Available() { return ::IsAppThemed() != 0; }

			HTHEME m_htheme;

			// 'class_list' is a semi-colon separated list of class names
			Theme(HWND hwnd, wchar_t const* class_list)
				:m_htheme(::OpenThemeData(hwnd, class_list))
			{}
			~Theme()
			{
				::CloseThemeData(m_htheme);
			}
			operator HTHEME() const
			{
				return m_htheme;
			}

			// 'hdc' = what to draw on
			// 'part_id','state_id' = see https://msdn.microsoft.com/en-us/library/windows/desktop/bb773210(v=vs.85).aspx
			// 'text' = the text to draw
			// 'count' = the number of characters in 'text' to draw, use -1 for all up to null terminator
			// 'flags' = https://msdn.microsoft.com/en-us/library/windows/desktop/bb773199(v=vs.85).aspx
			// 'rect' = where to draw the text (in logical units) (or where it would be drawn)
			// 'opts' = https://msdn.microsoft.com/en-us/library/windows/desktop/bb773236(v=vs.85).aspx
			void Text(HDC hdc, int part_id, int state_id, LPCWSTR text, int count, DWORD flags, RECT* rect, DTTOPTS const* opts)
			{
				Throw(m_htheme != nullptr, "Themes not available");
				Throw(::DrawThemeTextEx(m_htheme, hdc, part_id, state_id, text, count, flags, rect, opts), "Draw theme text failed");
			}

			// 'hdc' = what to draw on
			// 'part_id','state_id' = see https://msdn.microsoft.com/en-us/library/windows/desktop/bb773210(v=vs.85).aspx
			// 'rect' = where to draw (in logical units)
			// 'opts' = https://msdn.microsoft.com/en-us/library/windows/desktop/bb773233(v=vs.85).aspx
			void Bkgd(HDC hdc, int part_id, int state_id, RECT const* rect, DTBGOPTS const* opts)
			{
				Throw(m_htheme != nullptr, "Themes not available");
				Throw(::DrawThemeBackgroundEx(m_htheme, hdc, part_id, state_id, rect, opts), "Draw themed background failed");
			}

			// Retrieves the size of the content area for the background defined by the visual style.
			Rect BkgdContentRect(HDC hdc, int part_id, int state_id, RECT const* bounding_rect)
			{
				Rect res;
				Throw(m_htheme != nullptr, "Themes not available");
				Throw(::GetThemeBackgroundContentRect(m_htheme, hdc, part_id, state_id, bounding_rect, &res), "Get themed background content rect failed");
				return res;
			}
		};

		// Basic message loop
		struct MessageLoop
		{
			HACCEL m_accel;

			MessageLoop(HACCEL accel = nullptr)
				:m_accel(accel)
			{}
			MessageLoop(HINSTANCE hinst, int accel_id)
				:MessageLoop(::LoadAccelerators(hinst, MAKEINTRESOURCE(accel_id)))
			{}
			virtual ~MessageLoop()
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
			RAII(T& var, T new_value) :m_var(&var) ,old_value(var) { *m_var = new_value; }
			~RAII()                                                { *m_var = old_value; }
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
		struct EmptyArgs
		{};

		// Event args used in cancellable operations
		struct CancelEventArgs :EmptyArgs
		{
			bool m_cancel;
			CancelEventArgs(bool cancel = false)
				:m_cancel(cancel)
			{}
		};

		// Event args for paint events
		struct PaintEventArgs :EmptyArgs
		{
			Rect m_update_rect;   // The area needing updating
			bool m_dopaint;       // True if there is a non-zero update rectangle
			HDC  m_alternate_hdc; // If non-null, then paint onto this device context
			PaintEventArgs(HWND hwnd, bool erase, HDC alternate_hdc)
				:m_update_rect()
				,m_dopaint(::GetUpdateRect(hwnd, &m_update_rect, erase) != 0)
				,m_alternate_hdc(alternate_hdc)
			{}
		};

		// Event args for window sizing events
		struct SizeEventArgs :EmptyArgs
		{
			WindowPos m_pos;    // The new position/size info
			Point     m_point;  // Convenience position value
			Size      m_size;   // Convenience size value
			bool      m_before; // True if this event is before the window pos change, false if after
			SizeEventArgs(WindowPos const& pos, bool before)
				:m_pos(pos)
				,m_point(pos.x, pos.y)
				,m_size(pos.cx, pos.cy)
				,m_before(before)
			{}
		};

		// Event args for keyboard key events
		struct KeyEventArgs :EmptyArgs
		{
			UINT m_vk_key;
			bool m_down; // True if this is a key down event, false if key up
			UINT m_repeats;
			UINT m_flags;
			KeyEventArgs(UINT vk_key, bool down, UINT repeats, UINT flags)
				:m_vk_key(vk_key)
				,m_down(down)
				,m_repeats(repeats)
				,m_flags(flags)
			{}
		};

		// Event args for mouse button events
		struct MouseEventArgs :EmptyArgs
		{
			Point     m_point;    // The screen location of the mouse at the button event
			EMouseKey m_button;   // The button that triggered the event
			EMouseKey m_keystate; // The state of all mouse buttons and control keys
			bool      m_down;     // True if the button was a down event, false if an up event
			MouseEventArgs(EMouseKey btn, bool down, Point point, EMouseKey keystate)
				:m_point(point)
				,m_button(btn)
				,m_keystate(keystate)
				,m_down(down)
			{}
		};

		// Event args for mouse wheel events
		struct MouseWheelArgs :EmptyArgs
		{
			short     m_delta;    // The amount the mouse wheel has turned
			Point     m_point;    // The screen location of the mouse at the time of the event
			EMouseKey m_keystate; // The state of all mouse buttons and control keys
			MouseWheelArgs(short delta, Point point, EMouseKey keystate)
				:m_delta(delta)
				,m_point(point)
				,m_keystate(keystate)
			{}
		};

		// Event args for timer events
		struct TimerEventArgs :EmptyArgs
		{
			UINT_PTR m_event_id;
			TimerEventArgs(UINT_PTR event_id)
				:m_event_id(event_id)
			{}
		};

		// Event args for dropped files
		struct DropFilesEventArgs :EmptyArgs
		{
			HDROP               m_drop_info; // The windows drop info
			std::vector<string> m_filepaths; // The file paths dropped
			DropFilesEventArgs(HDROP drop_info)
				:m_drop_info(drop_info)
				,m_filepaths()
			{}
		};

		// EventHandler<>
		template <typename Sender, typename Args> struct EventHandler
		{ // TODO: This isn't threadsafe
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

		#pragma region Application Main Window Handle
		// Special handle value used to indicate that a window is
		// the main application window and that, when closed, the
		// application should exit. Pass as the 'parent' parameter.
		struct ApplicationMainWindowHandle
		{
			operator HWND() const                           { return reinterpret_cast<HWND>(~0ULL); }
			operator Control*() const                       { return reinterpret_cast<Control*>(~0ULL); }
			template <typename D> operator Form<D>*() const { return reinterpret_cast<Form<D>*>(~0ULL); }
		} const ApplicationMainWindow;
		#pragma endregion

		#pragma region Dialog Template
		// A structure for defining a dialog template
		struct DlgTemplate
		{
			enum { DefaultStyle = DefaultDialogStyle };
			enum { DefaultStyleEx = DefaultDialogStyleEx };
			enum { DefaultItemStyle = DefaultControlStyle };

			std::vector<byte> m_mem;

			// Notes:
			//  - if x == CW_USEDEFAULT in CreateWindowEx then the system positions the window, (ignoring y). Only valid for overlapped windows, if used for popup windows will be treated as zero
			//  - if w == CW_USEDEFAULT in CreateWindowEx then the system chooses the width and height. Only valid for overlapped windows, if used for popup windows will be treated as zero
			DlgTemplate() {}
			DlgTemplate(wchar_t const* caption
				,short x = 0, short y = 0, short w = 0, short h = 0
				,DWORD style = DefaultStyle, DWORD ex_style = DefaultStyleEx
				,wchar_t const* font_name = L"MS Shell Dlg", WORD font_size = 8
				,int menu_id = -1, char const* class_name = nullptr)
			{
				// In a standard template for a dialog box, the DLGTEMPLATE structure is always immediately followed
				// by three variable-length arrays that specify the menu, class, and title for the dialog box.
				// When the DS_SETFONT style is specified, these arrays are also followed by a 16-bit value specifying
				// point size and another variable-length array specifying a typeface name. Each array consists of one
				// or more 16-bit elements. The menu, class, title, and font arrays must be aligned on WORD boundaries.

				// If 'style' includes DS_SETFONT then windows expects the header to be followed by
				// font data consisting of a 16-bit font size, and unicode font name string
				if (font_name != nullptr)
					style |= DS_SETFONT;
				else
					style &= ~DS_SETFONT;

				// Add the header
				DLGTEMPLATE hd = {style, ex_style, 0, x, y, w, h};
				append(m_mem, &hd, sizeof(hd));

				// Immediately following the DLGTEMPLATE structure is a menu array that identifies a menu resource for the dialog box.
				// If the first element of this array is 0x0000, the dialog box has no menu and the array has no other elements.
				// If the first element is 0xFFFF, the array has one additional element that specifies the ordinal value of a menu
				// resource in an executable file. If the first element has any other value, the system treats the array as a
				// null-terminated Unicode string that specifies the name of a menu resource in an executable file.
				AddWord(menu_id != -1 ? WORD(MAKEINTRESOURCE(menu_id)) : 0);

				// Following the menu array is a class array that identifies the window class of the control. If the first element
				// of the array is 0x0000, the system uses the predefined dialog box class for the dialog box and the array has no
				// other elements. If the first element is 0xFFFF, the array has one additional element that specifies the ordinal
				// value of a predefined system window class. If the first element has any other value, the system treats the array
				// as a null-terminated Unicode string that specifies the name of a registered window class.
				AddString(class_name);

				// Following the class array is a title array that specifies a null-terminated Unicode string that contains the title
				// of the dialog box. If the first element of this array is 0x0000, the dialog box has no title and the array has no other elements.
				AddString(caption);

				// The 16-bit point size value and the typeface array follow the title array, but only if the style member specifies the
				// DS_SETFONT style. The point size value specifies the point size of the font to use for the text in the dialog box and
				// its controls. The typeface array is a null-terminated Unicode string specifying the name of the typeface for the font.
				// When these values are specified, the system creates a font having the specified size and typeface (if possible) and
				// sends a WM_SETFONT message to the dialog box procedure and the control window procedures as it creates the dialog box and controls.
				if (style & DS_SETFONT)
				{
					append(m_mem, &font_size, sizeof(font_size));
					AddString(font_name);
				}

				// Following the DLGTEMPLATE header in a standard dialog box template are one or more DLGITEMTEMPLATE structures that
				// define the dimensions and style of the controls in the dialog box. The cdit member specifies the number of
				// DLGITEMTEMPLATE structures in the template. These DLGITEMTEMPLATE structures must be aligned on *DWORD* boundaries.
			}

			// Add a control to the template
			void Add(WORD id, wchar_t const* wndclass, wchar_t const* text, short x = 0, short y = 0, short w = 0, short h = 0, DWORD style = DefaultItemStyle, DWORD ex_style = 0, WORD creation_data_size_in_bytes = 0, void* creation_data = nullptr)
			{
				// In a standard template for a dialog box, the DLGITEMTEMPLATE structure is always immediately followed by three
				// variable-length arrays specifying the class, title, and creation data for the control. Each array consists of
				// one or more 16-bit elements.

				// Each DLGITEMTEMPLATE structure in the template must be aligned on a DWORD boundary. The class and title arrays
				// must be aligned on WORD boundaries. The creation data array must be aligned on a WORD boundary.
				auto pad = m_mem.size() & 0x3;
				if (pad)
				{
					byte const padding[4] = {};
					append(m_mem, &padding[0], 4 - pad);
				}

				// Add the dialog item to the header count
				hdr().cdit++;

				// Add a description of the item
				DLGITEMTEMPLATE item = {style, ex_style, x, y, w, h, id};
				append(m_mem, &item, sizeof(item));

				// Immediately following each DLGITEMTEMPLATE structure is a class array that specifies the window class of the control.
				// If the first element of this array is any value other than 0xFFFF, the system treats the array as a null-terminated
				// Unicode string that specifies the name of a registered window class. If the first element is 0xFFFF, the array has
				// one additional element that specifies the ordinal value of a predefined system class. The ordinal can be one of the following atom values.
				enum class EStdCtrlType :WORD { None = 0, BUTTON = 0x0080, EDIT = 0x0081, STATIC = 0x0082, LISTBOX = 0x0083, SCROLLBAR = 0x0084, COMBOBOX = 0x0085};
				EStdCtrlType ctrl_atom = EStdCtrlType::None;
				if      (wndclass == nullptr) {}
				else if (wcscmp(wndclass, L"BUTTON"   ) == 0) ctrl_atom = EStdCtrlType::BUTTON;
				else if (wcscmp(wndclass, L"EDIT"     ) == 0) ctrl_atom = EStdCtrlType::EDIT;
				else if (wcscmp(wndclass, L"STATIC"   ) == 0) ctrl_atom = EStdCtrlType::STATIC;
				else if (wcscmp(wndclass, L"LISTBOX"  ) == 0) ctrl_atom = EStdCtrlType::LISTBOX;
				else if (wcscmp(wndclass, L"SCROLLBAR") == 0) ctrl_atom = EStdCtrlType::SCROLLBAR;
				else if (wcscmp(wndclass, L"COMBOBOX" ) == 0) ctrl_atom = EStdCtrlType::COMBOBOX;
				if (ctrl_atom != EStdCtrlType::None)
					AddWord(WORD(ctrl_atom));
				else
					AddString(wndclass);

				// Following the class array is a title array that contains the initial text or resource identifier of the control.
				// If the first element of this array is 0xFFFF, the array has one additional element that specifies an ordinal value
				// of a resource, such as an icon, in an executable file. You can use a resource identifier for controls, such as
				// static icon controls, that load and display an icon or other resource rather than text. If the first element is any
				// value other than 0xFFFF, the system treats the array as a null-terminated Unicode string that specifies the initial text.
				AddString(text);

				// The creation data array begins at the next WORD boundary after the title array. This creation data can be of any size
				// and format. If the first word of the creation data array is nonzero, it indicates the size, in bytes, of the creation
				// data (including the size word). The control's window procedure must be able to interpret the data. When the system
				// creates the control, it passes a pointer to this data in the lParam parameter of the WM_CREATE message that it sends to the control.
				creation_data_size_in_bytes += (creation_data_size_in_bytes != 0) * sizeof(creation_data_size_in_bytes); // include the size of 'creation_data_size_in_bytes'
				append(m_mem, &creation_data_size_in_bytes, sizeof(creation_data_size_in_bytes));
				if (creation_data_size_in_bytes != 0)
					append(m_mem, creation_data, creation_data_size_in_bytes);
			}

			// True if the template contains a dialog description, false if not
			bool valid() const
			{
				return !m_mem.empty();
			}

			// Access to the template header
			DLGTEMPLATE const& hdr() const
			{
				return *reinterpret_cast<DLGTEMPLATE const*>(m_mem.data());
			}
			DLGTEMPLATE& hdr()
			{
				return *reinterpret_cast<DLGTEMPLATE*>(m_mem.data());
			}
			operator DLGTEMPLATE const*() const
			{
				return &hdr();
			}

		private:

			// Append a string or null terminator to the memory buffer
			template <typename Char> void AddString(Char const* str)
			{
				if (str == nullptr)
				{
					WORD x = 0;
					append(m_mem, &x, sizeof(x));
				}
				else
				{
					auto s = Widen(str); s.push_back(0);
					append(m_mem, s.data(), s.size() * sizeof(wchar_t));
				}
			}

			// Append a {0xffff, val} pattern to the memory buffer
			void AddWord(WORD val)
			{
				if (val == 0)
				{
					WORD m = 0;
					append(m_mem, &m, sizeof(m));
				}
				else
				{
					WORD m[2] = {0xFFFF, val};
					append(m_mem, &m[0], sizeof(m));
				}
			}
		};
		#pragma endregion

		#pragma region Control
		// Base class for all windows/controls
		struct Control
		{
			#pragma region Auto Size
			// Used as a size (w,h) value, means expand w,h to match parent
			// Lower 30 bits are the l,r or t,b margins to fill with.
			static int const Fill = int(0x80000000);
			static int FillM(int left_or_top, int right_or_bottom)
			{
				assert(left_or_top < (1 << 15) && right_or_bottom < (1 << 15));
				return Fill | (left_or_top << 15) | (right_or_bottom << 0);
			}
			static Size FillM(int fill)
			{
				return (fill & Fill) ? Size((fill >> 15) & 0x7fff, (fill >> 0) & 0x7fff) : Size();
			}
			#pragma endregion

			#pragma region Auto Position
			// The mask for auto positioning control bits
			static int const AutoPosMask = 0xFF000000;

			// The mask for the control id
			static int const IdMask = 0x00FFFFFF;

			// The X,Y coord of the control being positioned
			static int const Left    = 0x81000000;
			static int const Right   = 0x82000000;
			static int const CentreH = 0x83000000;
			static int const Top     = Left      ;
			static int const Bottom  = Right     ;
			static int const CentreV = CentreH   ;

			// The X coord of the reference control to align to
			static int const LeftOf    = 0x84000000;
			static int const RightOf   = 0x88000000;
			static int const CentreHOf = 0x8C000000;
			static int const TopOf     = LeftOf    ;
			static int const BottomOf  = RightOf   ;
			static int const CentreVOf = CentreHOf ;

			// Use: e.g. Left|LeftOf|id,
			// read: left edge of this control, aligned to the left of control with id 'id'
			#pragma endregion

			// Special id for controls that don't need an id
			static int const IDC_UNUSED = IdMask;

		protected:
			using Controls = std::vector<Control*>;
			HWND               m_hwnd;         // Window handle for the control
			int                m_id;           // Dialog control id, used to detect windows messages for this control
			char const*        m_name;         // Debugging name for the control
			Control*           m_parent;       // The parent that contains this control
			Controls           m_child;        // The controls nested with this control
			EAnchor            m_anchor;       // How the control resizes with it's parent
			EDock              m_dock;         // Dock style for the control
			Rect               m_pos_offset;   // Distances from this control to the edges of the parent client area
			bool               m_pos_ofs_save; // Enable/Disables the saving of the position offset when the control is moved
			MinMaxInfo         m_min_max_info; // Minimum/Maximum window size/position
			COLORREF           m_colour_fore;  // Foreground colour
			COLORREF           m_colour_back;  // Background colour
			LONG               m_down_at[4];   // Button down timestamp
			bool               m_dbl_buffer;   // True if the control is double buffered
			bool               m_allow_drop;   // Allow drag/drop operations on the control
			ATL::CStdCallThunk m_thunk;        // WndProc thunk, turns a __stdcall into a __thiscall
			WNDPROC            m_oldproc;      // The window class default wndproc function
			int                m_wnd_proc_nest; // Tracks how often WndProc has been called recursively
			std::thread::id    m_thread_id;    // The thread that this control was created on

			struct InitParam
			{
				Control* m_this;
				LPARAM m_lparam;
				InitParam(Control* this_, LPARAM lparam) :m_this(this_) ,m_lparam(lparam) {}
			};
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

			// A window class is a template from which window instances are created. The WNDCLASS contains a static function
			// for the WndProc. We need a way to set the WndProc of a newly created window to the Control::StaticWndProc which
			// expects the 'HWND' parameter to actually be a pointer to the class instance (thanks to the thunk). Use a WndProc
			// function that handles the WM_NCCREATE message and updates the WndProc for the pointer passed via the CREATESTRUCT
			// 'WndType' is the class that implements the Control being registered.
			template <typename WndType> static ATOM RegisterWndClass(HINSTANCE hinst =  GetModuleHandleW(nullptr))
			{
				// Ensure the window class for this form has been registered
				// As of C++11, static initialisation is thread safe
				static ATOM atom = [=]
					{
						// Register the window class if not already registered
						WNDCLASSEXW wc;
						auto a = ATOM(::GetClassInfoExW(hinst, WndType::WndClassName(), &wc));
						if (a != 0) return a;

						// Allow 'Derived' to override the WndClassInfo by calling the static method through 'WndType'.
						// Derived should implement this: static WNDCLASSEXW WndClassInfo(HINSTANCE hinst);
						wc = WndType::WndClassInfo(hinst);

						// If the WndProc is not null, assume that Derived has it's own special WndProc.
						wc.lpfnWndProc = wc.lpfnWndProc ? wc.lpfnWndProc : &InitWndProc;

						// Register the window class
						a = ATOM(::RegisterClassExW(&wc));
						if (a != 0) return a;

						Throw(false, "RegisterClassEx failed");
						return ATOM();
					}();
				return atom;
			}

			// Convenience method that allows derived types to create their WndClassInfo() and
			// WndClassName() functions by instantiating these methods templated with their type
			template <typename WndType> static WNDCLASSEXW MakeWndClassInfo(HINSTANCE hinst)
			{
				// This is a default implementation of WndClassInfo that gets parameters
				// from static functions on 'WndType'. Types that derive from Control
				// will pick up the defaults below unless explicitly defined in the derived type
				WNDCLASSEXW wc;
				wc.cbSize        = sizeof(WNDCLASSEXW);
				wc.style         = CS_HREDRAW | CS_VREDRAW | CS_DBLCLKS;
				wc.cbClsExtra    = 0;
				wc.cbWndExtra    = 0;
				wc.hInstance     = hinst;
				wc.hIcon         = WndType::WndIcon(hinst, true);
				wc.hIconSm       = WndType::WndIcon(hinst, false);
				wc.hCursor       = WndType::WndCursor(hinst);
				wc.hbrBackground = WndType::WndBackground();
				wc.lpszMenuName  = WndType::WndMenu();
				wc.lpszClassName = WndType::WndClassName();
				wc.lpfnWndProc   = nullptr; // Leave as null to use the thunk
				return wc;
			}

			// These methods are provided so that windows that inherit from control
			// can implement their WndClassInfo(), and WndClassName() functions using defaults
			static wchar_t const* WndClassName()
			{
				// Auto name, derived can overload this function
				static thread_local wchar_t name[64];
				_swprintf_c(name, _countof(name), L"wingui::%p", &name[0]);
				return name;
			}
			static HICON WndIcon(HINSTANCE hinst, bool large)
			{
				//::LoadIcon(hinst, MAKEINTRESOURCE(IDI_ICON));
				//::LoadIcon(hinst, MAKEINTRESOURCE(IDI_SMALL));
				(void)hinst,large;
				return nullptr;
			}
			static HCURSOR WndCursor(HINSTANCE hinst)
			{
				(void)hinst;
				auto cur = ::LoadCursor(nullptr, IDC_ARROW); // Load arrow from the system, not this exe image
				Throw(cur != nullptr, "Failed to load default arrow cursor");
				return cur;
			}
			static HBRUSH WndBackground()
			{
				// Returning null forces handling of WM_ERASEBKGND
				return reinterpret_cast<HBRUSH>(COLOR_3DFACE+1);
			}
			static wchar_t const* WndMenu()
			{
				// Typically you might return: MAKEINTRESOURCE(IDM_MENU);
				return nullptr; 
			}

			// A reference implementation alternative to m_thunk objects
			#define PR_HWND_MAP 0
			#if PR_HWND_MAP
			typedef std::unordered_map<HWND, Control*> HwndMap;
			static HwndMap& Hwnd() { static HwndMap map; return map; }
			#endif

			// Hook or unhook the window proc for this control
			void HookWndProc(bool hook)
			{
				#if PR_HWND_MAP
				if (hook)
				{
					Hwnd()[m_hwnd] = this;
					m_oldproc = (WNDPROC)::SetWindowLongPtr(m_hwnd, GWLP_WNDPROC, LONG_PTR(StaticWndProc));
				}
				else
				{
					Hwnd().erase(m_hwnd);
					::SetWindowLongPtr(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_oldproc)), m_oldproc = nullptr;
				}
				#else
				if (hook && !m_oldproc)
					m_oldproc = (WNDPROC)::SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_thunk.GetCodeAddress()));
				else if (!hook && m_oldproc)
					::SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_oldproc)), m_oldproc = nullptr;
				#endif
			}
			static LRESULT __stdcall StaticWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				#if PR_HWND_MAP
				return Hwnd()[hwnd]->WndProc(message, wparam, lparam);
				#else
				// 'm_thunk' causes 'hwnd' to actually be the pointer to the control rather than the hwnd
				return reinterpret_cast<Control*>(hwnd)->WndProc(message, wparam, lparam);
				#endif
			}
			LRESULT DefWndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				return m_oldproc != nullptr
					? ::CallWindowProcW(m_oldproc, m_hwnd, message, wparam, lparam)
					: ::DefWindowProcW(m_hwnd, message, wparam, lparam);
			}

			// Handy debugging method for displaying WM_MESSAGES
			void WndProcDebug(UINT message, WPARAM wparam, LPARAM lparam)
			{
				#if 0
				RAII<int> nest(m_wnd_proc_nest, m_wnd_proc_nest+1);
				if (/*m_name && strcmp(m_name,"ctx-menu") == 0 && message == WM_KEYDOWN*/true)
				{
					static int msg_idx = 0; ++msg_idx;
					auto m = pr::gui::DebugMessage(m_hwnd, message, wparam, lparam);
					if (*m)
					{
						for (int i = 1; i != m_wnd_proc_nest; ++i) OutputDebugStringA("\t");
						OutputDebugStringA(pr::FmtS("%5d|%s|%s\n", msg_idx, m_name, m));
					}
					if (msg_idx == 0) _CrtDbgBreak();
				}
				#else
				(void)message,wparam,lparam;
				#endif
			}

			// This method is the window procedure for this control.
			// 'ProcessWindowMessage' is used to process messages sent
			// to the parent window that contains this control.
			// WndProc is called by windows, Forms forward messages to their child controls using 'ProcessWindowMessage'
			virtual LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				//WndProcDebug(message, wparam, lparam);
				switch (message)
				{
				case WM_DESTROY:
					{
						HookWndProc(false);
						m_hwnd = nullptr;
						break;
					}
				case WM_ACTIVATE:
					{
						UpdateWindow(*this);
						break;
					}
				case WM_ERASEBKGND:
					{
						// Allow subclasses to prevent erase background
						if (OnEraseBkGnd(EmptyArgs()))
							return S_FALSE;

						// If the background colour has been set, fill the client area with it
						if (m_colour_back != CLR_INVALID)
						{
							// Use a trick. By using the ETO_OPAQUE flag in the call to ExtTextOut, and supplying a
							// zero-length string to display, you can quickly and easily fill a rectangular
							// area using the current background colour.
							auto hdc = (HDC)wparam;
							auto rect = ClientRect();
							::ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &rect, L"", 0, 0);
							return S_FALSE;
						}
						break;
					}
				case WM_PAINT:
					{
						// Notes:
						//  Only create a pr::gui::PaintStruct if you intend to do the painting yourself,
						//  otherwise DefWndProc will do it (i.e. most controls are drawn by DefWndProc).
						//  The update rect in the paint args is the area needing painting.
						//  Typical behaviour is to create a PaintStruct, however you can not do this and
						//  use this sequence instead:
						//    ::GetUpdateRect(*this, &r, TRUE); <- sends the WM_ERASEBKGND if needed
						//    Draw(); <- do your drawing
						//    Validate(&r); <- tell windows the update rect has been updated,
						//  Non-client window parts are drawn in DefWndProc
						if (OnPaint(PaintEventArgs(m_hwnd, true, HDC(wparam))))
							return S_OK;

						break;
					}
				case WM_WINDOWPOSCHANGING:
				case WM_WINDOWPOSCHANGED:
					{
						auto& wp = *reinterpret_cast<WindowPos*>(lparam);
						auto before = message == WM_WINDOWPOSCHANGING;
						OnWindowPosChange(SizeEventArgs(wp, before));
						break;
					}
				case WM_GETMINMAXINFO:
					{
						auto& a = *reinterpret_cast<MinMaxInfo*>(lparam);
						auto& b = m_min_max_info;
						if ((b.m_mask & MinMaxInfo::EMask::MaxSize     ) != 0) a.ptMaxSize      = b.ptMaxSize     ; else b.ptMaxSize      = a.ptMaxSize     ;
						if ((b.m_mask & MinMaxInfo::EMask::MaxPosition ) != 0) a.ptMaxPosition  = b.ptMaxPosition ; else b.ptMaxPosition  = a.ptMaxPosition ;
						if ((b.m_mask & MinMaxInfo::EMask::MinTrackSize) != 0) a.ptMinTrackSize = b.ptMinTrackSize; else b.ptMinTrackSize = a.ptMinTrackSize;
						if ((b.m_mask & MinMaxInfo::EMask::MaxTrackSize) != 0) a.ptMaxTrackSize = b.ptMaxTrackSize; else b.ptMaxTrackSize = a.ptMaxTrackSize;
						break;
					}
				case WM_KEYDOWN:
				case WM_KEYUP:
					{
						auto vk_key = UINT(wparam);
						auto repeats = UINT(lparam & 0xFFFF);
						auto flags = UINT((lparam & 0xFFFF0000) >> 16);
						if (OnKey(KeyEventArgs(vk_key, message == WM_KEYDOWN, repeats, flags)))
							return true;
						break;
					}
				case WM_LBUTTONDOWN:
				case WM_RBUTTONDOWN:
				case WM_MBUTTONDOWN:
				case WM_XBUTTONDOWN:
				case WM_LBUTTONUP:
				case WM_RBUTTONUP:
				case WM_MBUTTONUP:
				case WM_XBUTTONUP:
					{
						auto pt = Point(GetXLParam(lparam), GetYLParam(lparam));
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey());
						auto down =
							message == WM_LBUTTONDOWN ||
							message ==  WM_RBUTTONDOWN ||
							message == WM_MBUTTONDOWN ||
							message == WM_XBUTTONDOWN;
						auto btn =
							(message == WM_LBUTTONDOWN || message == WM_LBUTTONUP) ? EMouseKey::Left :
							(message == WM_RBUTTONDOWN || message == WM_RBUTTONUP) ? EMouseKey::Right :
							(message == WM_MBUTTONDOWN || message == WM_MBUTTONUP) ? EMouseKey::Middle :
							(message == WM_XBUTTONDOWN || message == WM_XBUTTONUP) ? EMouseKey::XButton1|EMouseKey::XButton2 :
							EMouseKey();
						
						// Event order is down, click, up
						bool handled = false;
						if (down)
							handled |= OnMouseButton(MouseEventArgs(btn, true, pt, keystate));
						if (IsClick(btn, down))
							handled |= OnMouseClick(MouseEventArgs(btn, true, pt, keystate));
						if (!down)
							handled |= OnMouseButton(MouseEventArgs(btn, false, pt, keystate));
						if (handled) return true;
						break;
					}
				case WM_MOUSEWHEEL:
					{
						auto delta = GET_WHEEL_DELTA_WPARAM(wparam);
						auto pt = Point(GetXLParam(lparam), GetYLParam(lparam));
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						if (OnMouseWheel(MouseWheelArgs(delta, pt, keystate)))
							return true;
						break;
					}
				case WM_MOUSEMOVE:
					{
						auto pt = Point(GetXLParam(lparam), GetYLParam(lparam));
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						OnMouseMove(MouseEventArgs(keystate, false, pt, keystate));
						break;
					}
				}
				return DefWndProc(message, wparam, lparam);
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
						if (m_id != IDC_UNUSED) Attach(::GetDlgItem(hwnd, m_id));
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_DESTROY:
					{
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						::DestroyWindow(m_hwnd); // Parent window is being destroy, destroy this window to
						break;
					}
				case WM_WINDOWPOSCHANGED:
					{
						// WM_WINDOWPOSCHANGED sent to the parent causes this window to resize.
						// The act of resizing causes WM_WINDOWPOSCHANGED to be sent to our WndProc which
						// then calls OnWindowPosChange()
						ResizeToParent();
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						break;
					}
				case WM_TIMER:
					{
						auto event_id = UINT_PTR(wparam);
						OnTimer(TimerEventArgs(event_id));
						break;
					}
				case WM_DROPFILES:
					{
						if (m_allow_drop)
						{
							auto drop_info = HDROP(wparam);
							DropFilesEventArgs drop(drop_info);
							
							int i = 0;
							drop.m_filepaths.resize(::DragQueryFileW(drop_info, 0xFFFFFFFF, nullptr, 0));
							for (auto& path : drop.m_filepaths)
							{
								path.resize(::DragQueryFileW(drop_info, i, 0, 0) + 1, 0);
								Throw(::DragQueryFileW(drop_info, i, &path[0], UINT(path.size())) != 0, "Failed to query file name from dropped files");
								++i;
							}
							OnDropFiles(drop);
						}
						break;
					}
				case WM_MOUSEWHEEL:
					{
						// WM_MOUSEWHEEL is only sent to the focused window, unlike mouse button/move messages
						// which are sent to the control with focus
						
						if (ForwardToChildren(hwnd, message, wparam, lparam, result)) return true;
						auto delta = GET_WHEEL_DELTA_WPARAM(wparam);
						auto pt = Point(GetXLParam(lparam), GetYLParam(lparam));
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						if (OnMouseWheel(MouseWheelArgs(delta, pt, keystate)))
							return true;
						break;
					}
				}

				return false;
			}

			// Forward a windows message to child controls
			bool ForwardToChildren(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
			{
				// This needs thinking about, should we be forwarding all messages to all child controls
				// all the time? or just some? or just to the control with Focus? some to the controls the
				// mouse is over?
				for (auto& c : m_child)
					if (c->ProcessWindowMessage(hwnd, message, wparam, lparam, result))
						return true;
				return false;
			}

			// Handle auto position/size
			void AutoSizePosition(int& x, int& y, int& w, int& h)
			{
				// Get the parent control bounds (in parent space)
				auto parent_rect = m_parent
					? m_parent->ClientRect()
					: MinMaxInfo().Bounds();

				// Return the sibling control with id 'id'
				auto ctrl = [this](int id) -> Control*
					{
						if (id == 0) return nullptr;
						for (auto c : m_parent->m_child) if (c->m_id == id) return c;
						return nullptr;
					};

				// Return the rect for the control with id 'id'
				auto rect = [&](int id) -> Rect
					{
						assert((id == 0 || ctrl(id) != nullptr) && "Sibling control not found");
						return id == 0 ? parent_rect : ctrl(id)->RectRelativeTo(*m_parent);
					};

				// Set the width/height and x/y position
				auto auto_size = [=](int& X, int& W, int i)
				{
					auto margin = Size();
					if (W & Fill)
					{
						margin = FillM(W);
						W = parent_rect.size(i) - margin.cx - margin.cy;
					}
					if (X & AutoPosMask)
					{
						// Get the ref point on the parent. Note, order is important here
						// If the top 4 bits are not '0b1000' then 'X' is just a negative number.
						// Otherwise, the top 8 bits are the auto position bits and the lower 24
						// are the id of the control to position relative to.
						int ref = 0;
						if      ((X & 0xF0000000) != 0x80000000) { ref = parent_rect.bottomright()[i]; }
						else if ((X & CentreHOf ) == CentreHOf ) { ref = rect(X & IdMask).centre()[i]; }
						else if ((X & LeftOf    ) == LeftOf    ) { ref = rect(X & IdMask).topleft()[i]; }
						else if ((X & RightOf   ) == RightOf   ) { ref = rect(X & IdMask).bottomright()[i]; }
						
						// Position the control relative to 'ref'
						if      ((X & 0xF0000000) != 0x80000000) { X = ref - W + X; }
						else if ((X & CentreH   ) == CentreH   ) { X = ref - W/2 + margin.cx; }
						else if ((X & Left      ) == Left      ) { X = ref - 0   + margin.cx; }
						else if ((X & Right     ) == Right     ) { X = ref - W   + margin.cx; }
					}
				};
				auto_size(x, w, 0);
				auto_size(y, h, 1);
			}

			// Record the position of the control within the parent
			void RecordPosOffset()
			{
				// Store distances so that this control's position equals
				// parent.left + m_pos_offset.left, parent.right + m_pos_offset.right, etc..
				// (i.e. right and bottom offsets are typically negative)
				if (!m_parent || !m_hwnd || !m_pos_ofs_save) return;
				auto p = m_parent->ScreenRect();
				auto r = ScreenRect();
				m_pos_offset = Rect(r.left - p.left, r.top - p.top, r.right - p.right, r.bottom - p.bottom);
			}

			// Adjust the size of this control relative to it's parent
			void ResizeToParent(bool repaint = true)
			{
				if (!m_parent || !m_hwnd) return;
				auto p = m_parent->ScreenRect();
				auto r = ScreenRect();
				auto w = r.width(); auto h = r.height();
				if (m_dock == EDock::None)
				{
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
				}
				else
				{
					switch (m_dock)
					{
					default: throw std::exception("Unknown dock style");
					case EDock::Fill:   r = p; break;
					case EDock::Top:    r.left = p.left; r.right = p.right; r.top = 0;           r.bottom = p.top + h; break;
					case EDock::Bottom: r.left = p.left; r.right = p.right; r.bottom = p.bottom; r.top = r.bottom - h; break;
					case EDock::Left:   r.top = p.top; r.bottom = p.bottom; r.left = 0;        r.right = r.left + w; break;
					case EDock::Right:  r.top = p.top; r.bottom = p.bottom; r.right = p.right; r.left = r.right - w; break;
					}
				}
				RAII<bool> no_save_ofs(m_pos_ofs_save, false);
				ScreenRect(r, repaint);
			}

			struct Internal {};
			Control(Internal, int id, Control* parent, char const* name, EAnchor anchor)
				:m_hwnd()
				,m_id(id & IdMask)
				,m_name(name)
				,m_parent()
				,m_child()
				,m_anchor(anchor)
				,m_dock(EDock::None)
				,m_pos_offset()
				,m_pos_ofs_save(true)
				,m_min_max_info()
				,m_colour_fore(CLR_INVALID)
				,m_colour_back(CLR_INVALID)
				,m_down_at()
				,m_dbl_buffer(false)
				,m_allow_drop(false)
				,m_thunk()
				,m_oldproc()
				,m_wnd_proc_nest()
				,m_thread_id(std::this_thread::get_id())
			{
				assert(id == m_id && "Id value too large");
				m_thunk.Init(DWORD_PTR(StaticWndProc), this);
				Parent(parent);
			}

		private:

			// Mouse single click detection.
			// Returns true on mouse up with within the click threshold
			bool IsClick(EMouseKey btn, bool down)
			{
				int idx;
				switch (btn)
				{
				default: throw std::exception("unknown mouse key");
				case EMouseKey::Left:     idx = 0; break;
				case EMouseKey::Right:    idx = 1; break;
				case EMouseKey::Middle:   idx = 2; break;
				case EMouseKey::XButton1: idx = 3; break;
				}
				if (down)
				{
					m_down_at[idx] = ::GetMessageTime();
					return false;
				}
				else
				{
					auto const click_thres = 150;
					auto click = ::GetMessageTime() - m_down_at[idx] < click_thres;
					m_down_at[idx] = 0;
					return click;
				}
			}

		public:

			// Create constructor
			// Use this constructor to create a new instance of a window
			// 'hwndparent' is the hwnd of the top level window
			// 'parent' is the control that contains this control (not necessarily the same as hwndparent, e.g. a group control)
			// Negative values for 'x,y' mean relative to the right,bottom of the parent
			Control(wchar_t const* wndclass_name ,wchar_t const* text
				,int x = 0, int y = 0, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultControlStyle ,DWORD ex_style = DefaultControlStyleEx
				,char const* name = nullptr
				,HMENU menu = nullptr
				,void* init_param = nullptr)
				:Control(Internal(), id, parent, name, anchor)
			{
				auto hwndparent = parent ? HWND(*parent) : HWND(nullptr);
				Create(wndclass_name, text, x, y, w, h, style, ex_style, hwndparent, menu, init_param);
			}

			// Attach or delayed creation constructor
			// Use this constructor when you intend to Attach this instance to an existing hwnd,
			// or when you need to create the control at a later time after a parent window has been created.
			// Set 'id' != -1 to have the control automatically attach to a dialog resouce control during WM_INITDIALOG
			Control(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(Internal(), id, parent, name, anchor)
			{
				// You can call 'Create' once the parent has an HWND. Or, if the parent
				// is based on a resource and 'id' is not IDC_UNUSED, then it will
				// automatically be created and attached to the control from the resource.
			}
			virtual ~Control()
			{
				// Orphan child controls
				for (; !m_child.empty(); )
					m_child.front()->Parent(nullptr);

				// Detach from our parent
				Parent(nullptr);

				// Destroy the window
				if (::IsWindow(m_hwnd))
					::DestroyWindow(m_hwnd);
			}

			// Implicit conversion to HWND
			operator HWND() const { return m_hwnd; }

			// Create the HWND for the control using Ctrl::WndClassName()
			template <typename Ctrl> void Create(wchar_t const* text
				,int x = 0, int y = 0, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,DWORD style = DefaultControlStyle ,DWORD ex_style = DefaultControlStyleEx
				,HWND hwndparent = 0
				,HMENU menu = nullptr
				,void* init_param = nullptr)
			{
				Create(Ctrl::WndClassName(), text, x, y, w, h, style, ex_style, hwndparent, menu, init_param);
			}
			void Create(wchar_t const* wndclass_name, wchar_t const* text
				,int x = 0, int y = 0, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,DWORD style = DefaultControlStyle ,DWORD ex_style = DefaultControlStyleEx
				,HWND hwndparent = 0
				,HMENU menu = nullptr
				,void* init_param = nullptr)
			{
				// Don't call this after using the first constructor
				assert(m_hwnd == 0 && "Window handle already exists");

				// Handle auto position/size
				AutoSizePosition(x,y,w,h);

				// CreateWindowEx failure reasons:
				//  invalid menu handle - if the window style is overlapped or popup, then 'menu' must be null
				//     or a valid menu handle otherwise it is the id of the control being created.
				InitParam init(this, 0);
				if (!init_param) init_param = &init;
				m_hwnd = ::CreateWindowExW(ex_style, wndclass_name, text, style, x, y, w, h, hwndparent, menu, GetModuleHandle(nullptr), init_param);
				Throw(m_hwnd != 0, "CreateWindowEx failed");

				RecordPosOffset();
				Font(HFONT(GetStockObject(DEFAULT_GUI_FONT)));

				if (style & WS_VISIBLE)
				{
					ShowWindow(m_hwnd, SW_SHOW);
					UpdateWindow(m_hwnd);
				}
			}

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

			#pragma region Accessors

			// Get/Set the parent of this control.
			// Note this parent is not necessarily the same parent as ::GetParent()
			// Controls such as GroupBox, Splitter, etc are parents of child controls
			Control* Parent() const { return m_parent; }
			void Parent(Control* parent)
			{
				// Check we're not parenting to ourself or a child
				#ifndef NDEBUG
				if (parent != nullptr)
				{
					std::vector<Control*> stack;
					for (stack.push_back(this); !stack.empty();)
					{
						auto x = stack.back(); stack.pop_back();
						assert(parent != x && "Cannot parent to a child");
						for (auto c : x->m_child)
							stack.push_back(c);
					}
				}
				#endif

				if (m_parent != parent)
				{
					if (m_parent != nullptr) // Remove from existing parent
					{
						auto& c = m_parent->m_child;
						c.erase(std::remove(c.begin(), c.end(), this), c.end());
					}

					m_parent = parent;
					if (::IsWindow(m_hwnd))
					{
						auto hwndparent = m_parent ? HWND(*m_parent) : HWND(nullptr);
						if (hwndparent == nullptr) Style((Style() | WS_POPUP) & ~WS_CHILD);
						Throw(::SetParent(m_hwnd, hwndparent) != nullptr, "SetParent failed");
						if (hwndparent != nullptr) Style((Style() | WS_CHILD) & ~WS_POPUP);
					}

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
				s.resize(::GetWindowTextLengthW(m_hwnd) + 1, 0);
				if (!s.empty()) s.resize(::GetWindowTextW(m_hwnd, &s[0], int(s.size())));
				return s;
			}
			void Text(string text)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowTextW(m_hwnd, text.c_str());
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

			// Returns true if the window is minimised
			bool Minimised() const
			{
				return ::IsIconic(m_hwnd) != 0;
			}

			// Get/Set the anchor mode for the window
			// Note: Dock() overrides this if not == EDock::None
			EAnchor Anchor() const
			{
				return m_anchor;
			}
			void Anchor(EAnchor anchor)
			{
				m_anchor = anchor;
			}

			// Get/Set the dock style for the control
			EDock Dock() const
			{
				return m_dock;
			}
			void Dock(EDock dock)
			{
				m_dock = dock;
				Invalidate();
			}

			// Get/Set Drag/Drop allowed
			bool AllowDrop() const
			{
				return m_allow_drop;
			}
			void AllowDrop(bool allow)
			{
				m_allow_drop = allow;
				::DragAcceptFiles(*this, m_allow_drop);
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
				return HFONT(::SendMessageW(m_hwnd, WM_GETFONT, 0, 0));
			}
			void Font(HFONT font)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, WM_SETFONT, WPARAM(font), TRUE);
			}

			// Invalidate the control for redraw
			virtual void Invalidate(bool erase = false, Rect* rect = nullptr)
			{
				assert(::IsWindow(m_hwnd));
				Throw(::InvalidateRect(m_hwnd, rect, erase), "InvalidateRect failed");
			}

			// Validate a rectangular area of the control
			virtual void Validate(Rect const* rect = nullptr)
			{
				assert(::IsWindow(m_hwnd));
				Throw(::ValidateRect(m_hwnd, rect), "ValidateRect failed");
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
			COLORREF BackColor(COLORREF col)
			{
				assert(::IsWindow(m_hwnd));
				DC dc(::GetDC(m_hwnd));
				return ::SetBkColor(dc, m_colour_back = col);
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

			// Returns a copy of 'rect' increased by the non-client areas of the window
			// Remember, ClientRect is [inclusive,inclusive] (if 'rect' is the client rect)
			Rect AdjRect(Rect const& rect = Rect()) const
			{
				auto r = rect;
				Throw(::AdjustWindowRectEx(&r, DWORD(Style()), BOOL(::GetMenu(m_hwnd) != nullptr), DWORD(StyleEx())), "AdjustWindowRectEx failed.");
				return r;
			}

			// Get the client rect for the window in this controls client space.
			// Note: Menus are part of the non-client area, you don't need to offset
			// the client rect for the menu.
			Rect ClientRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect rect;
				Throw(::GetClientRect(m_hwnd, &rect), "GetClientRect failed.");
				return rect;
			}

			// Get/Set the control bounds in screen space
			Rect ScreenRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect r;
				Throw(::GetWindowRect(m_hwnd, &r), "GetWindowRect failed.");
				return r;
			}
			void ScreenRect(Rect r, bool repaint = true, HWND prev = nullptr, WindowPos::EFlags flags = WindowPos::EFlags::NoZorder|WindowPos::EFlags::NoActivate)
			{
				assert(::IsWindow(m_hwnd));
				if (!repaint) flags = flags | WindowPos::EFlags::NoRedraw;

				// SetWindowPos takes client space coordinates
				auto hwndparent = ::GetParent(m_hwnd);
				::MapWindowPoints(nullptr, hwndparent, r.points(), 2);

				// Use prev = ::GetWindow(m_hwnd, GW_HWNDPREV) for the current z-order
				Throw(::SetWindowPos(m_hwnd, prev, r.left, r.top, r.width(), r.height(), flags), "SetWindowPos failed");
				RecordPosOffset();
			}

			// Get/Set the bounds of this control within it's Control* parent (note: not necessarily the hwndparent)
			Rect ParentRect() const
			{
				// If the control has no parent, then the screen is the parent
				auto parent = Parent();
				if (parent == nullptr)
					return ScreenRect();

				// Return the bounds of this control relative to 'parent'
				auto psr = parent->ScreenRect();
				auto csr = ScreenRect();
				csr.left   -= psr.left;
				csr.top    -= psr.top;
				csr.right  -= psr.left;
				csr.bottom -= psr.top;
				return csr;
			}
			void ParentRect(Rect r, bool repaint = false, HWND prev = nullptr, WindowPos::EFlags flags = WindowPos::EFlags::NoZorder|WindowPos::EFlags::NoActivate)
			{
				auto parent = Parent();
				if (parent != nullptr)
				{
					// Invalidate the previous and new rect on the parent
					if (!repaint)
					{
						auto inv = r.Union(ParentRect());
						parent->Invalidate(false, &inv);
					}

					// Convert 'r' to a screen rect
					auto psr = parent->ScreenRect();
					auto adj = parent->AdjRect();
					r.left   += psr.left - adj.left;
					r.top    += psr.top  - adj.top;
					r.right  += psr.left - adj.left;
					r.bottom += psr.top  - adj.top;
				}
				ScreenRect(r, repaint, prev, flags);
				RecordPosOffset();
				Invalidate();
			}

			// Get/Set the position of this control within it's Control* parent
			Point ParentPos() const
			{
				return ParentRect().topleft();
			}
			void ParentPos(int x, int y, bool repaint = false)
			{
				auto r = ParentRect();
				ParentRect(r.Offset(x - r.left, y - r.top), repaint);
			}

			// Get the bounds of this control relative to another window and optionally
			// with the areas of docked child controls removed. If 'relative_to' != this,
			// then the returned rect is in the client-space coordinates of the 'relative_to' window.
			// If 'relative_to' == nullptr, then the rect is in screen space
			Rect RectRelativeTo(HWND relative_to, bool exclude_docked_children = true) const
			{
				auto rect = ClientRect();
				
				// Exclude the area used by docked child controls
				if (exclude_docked_children)
				{
					for (auto& child : m_child)
					{
						if (child->m_dock == EDock::None) continue;
						if (!child->Visible()) continue;
						rect = rect.Subtract(child->RectRelativeTo(*this, false));
					}
				}

				// Convert the rect to screen space
				if (relative_to != m_hwnd)
				{
					Point pt;
					::MapWindowPoints(m_hwnd, relative_to, &pt, 1);
					rect = rect.Offset(pt.x, pt.y);
				}

				return rect;
			}

			// Return the dimensions of the non-client area for the window
			// WindowRect() - NonClientArea() = ClientRect()
			// Note: Menus are part of the non-client area. pt(0,0) is the top left
			// just below the menu. Status bars aren't though.
			Rect NonClientAreas() const
			{
				// Pass a zero rect to AdjustWindowRectEx to get the non-client dimensions
				return AdjRect(Rect());
			}

			// Convert a screen space point to client window space
			Point PointToClient(Point pt) const
			{
				Throw(::ScreenToClient(m_hwnd, &pt), "ScreenToClient failed");
				return pt;
			}

			// Convert a client window space point to screen space
			Point PointToScreen(Point pt) const
			{
				Throw(::ClientToScreen(m_hwnd, &pt), "ClientToScreen failed");
				return pt;
			}

			// Convert a screen space rectangle to client window space
			Rect RectToClient(Rect rect) const
			{
				return Rect(PointToClient(rect.topleft()), rect.size());
			}

			// Convert a client window space rectangle to screen space
			Rect RectToScreen(Rect rect) const
			{
				return Rect(PointToScreen(rect.topleft()), rect.size());
			}

			// Get/Set the menu. Set returns the previous menu.
			// If replacing a menu, remember to call DestroyMenu on the previous one
			pr::gui::Menu Menu() const
			{
				assert(::IsWindow(m_hwnd));
				return pr::gui::Menu(::GetMenu(m_hwnd), false);
			}
			pr::gui::Menu Menu(pr::gui::Menu menu)
			{
				assert(::IsWindow(m_hwnd));
				auto prev = Menu();
				Throw(::SetMenu(m_hwnd, menu) != 0, "Failed to set menu");
				return prev;
			}

			// Get/Set the control's icon
			HICON Icon(bool big_icon = true) const
			{
				assert(::IsWindow(m_hwnd));
				return HICON(::SendMessageW(m_hwnd, WM_GETICON, WPARAM(big_icon), 0));
			}
			HICON Icon(HICON icon, bool big_icon = true)
			{
				assert(::IsWindow(m_hwnd));
				return HICON(::SendMessageW(m_hwnd, WM_SETICON, WPARAM(big_icon), LPARAM(icon)));
			}

			// Set redraw mode on or off
			void Redraw(bool redraw)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, WM_SETREDRAW, WPARAM(redraw ? TRUE : FALSE), 0);
			}

			// Centre this control within another control or the desktop
			void CenterWindow(HWND centre_hwnd = nullptr)
			{
				assert(::IsWindow(m_hwnd));

				// Determine the owning window to center against
				auto style = Style();
				if (centre_hwnd == nullptr)
					centre_hwnd = (style & WS_CHILD) ? ::GetParent(m_hwnd) : ::GetWindow(m_hwnd, GW_OWNER);

				Rect area, centre;

				// Get the coordinates of the window relative to 'centre_hwnd'
				if (!(style & WS_CHILD))
				{
					// Don't center against invisible or minimized windows
					if (centre_hwnd != nullptr)
					{
						auto parent_state = ::GetWindowLong(centre_hwnd, GWL_STYLE);
						if (!(parent_state & WS_VISIBLE) || (parent_state & WS_MINIMIZE))
							centre_hwnd = nullptr;
					}

					// Center within screen coordinates
					HMONITOR monitor = ::MonitorFromWindow(centre_hwnd ? centre_hwnd : m_hwnd, MONITOR_DEFAULTTONEAREST);
					Throw(monitor != nullptr, "Failed to determine the monitor containing the centre on window");

					MonitorInfo minfo;
					Throw(::GetMonitorInfo(monitor, &minfo), "Failed to get info on monitor containing centre on window");

					area = minfo.rcWork;
					centre = centre_hwnd ? ::GetWindowRect(centre_hwnd, &centre),centre : area;
				}
				else
				{
					// center within parent client coordinates
					auto p = ::GetParent(m_hwnd);

					assert(::IsWindow(p));
					::GetClientRect(p, &area);

					assert(::IsWindow(centre_hwnd));
					::GetClientRect(centre_hwnd, &centre);

					::MapWindowPoints(centre_hwnd, p, (POINT*)&centre, 2);
				}

				auto r = ScreenRect();

				// Find this control's upper left based on centre
				auto l = (centre.left + centre.right - r.width()) / 2;
				auto t = (centre.top + centre.bottom - r.height()) / 2;

				// if the dialog is outside the screen, move it inside
				if (l + r.width() > area.right)   l = area.right - r.width();
				if (l < area.left)                l = area.left;
				if (t + r.height() > area.bottom) t = area.bottom - r.height();
				if (t < area.top)                 t = area.top;

				// Map screen coordinates to child coordinates
				Throw(::SetWindowPos(m_hwnd, ::GetWindow(m_hwnd, GW_HWNDPREV), l, t, -1, -1, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE), "Failed to centre window");
			}

			// Return the mouse location at the time of the last message
			Point MousePosition() const
			{
				auto pos = ::GetMessagePos();
				return Point(GetXLParam(pos), GetYLParam(pos));
			}

			// Return the key state at the time of the last message
			EControlKey KeyState() const
			{
				auto state = EControlKey::None;
				if (::GetKeyState(VK_LSHIFT  ) & 0x8000) state |= EControlKey::LShift;
				if (::GetKeyState(VK_RSHIFT  ) & 0x8000) state |= EControlKey::RShift;
				if (::GetKeyState(VK_LCONTROL) & 0x8000) state |= EControlKey::LCtrl;
				if (::GetKeyState(VK_RCONTROL) & 0x8000) state |= EControlKey::RCtrl;
				if (::GetKeyState(VK_LMENU   ) & 0x8000) state |= EControlKey::LAlt;
				if (::GetKeyState(VK_RMENU   ) & 0x8000) state |= EControlKey::RAlt;
				return state;
			}
			bool KeyState(int vk_key) const
			{
				return (::GetKeyState(vk_key) & 0x8000) != 0;
			}

			#pragma endregion

			#pragma region Events

			// Paint event
			EventHandler<Control&, PaintEventArgs const&> Paint;

			// Erase background
			EventHandler<Control&, EmptyArgs const&> EraseBkGnd;

			// Window position changing or changed
			EventHandler<Control&, SizeEventArgs const&> WindowPosChange;

			// Key down/up
			EventHandler<Control&, KeyEventArgs const&> Key;

			// Mouse button down/up
			EventHandler<Control&, MouseEventArgs const&> MouseButton;

			// Mouse button single click
			EventHandler<Control&, MouseEventArgs const&> MouseClick;

			// Mouse move
			EventHandler<Control&, MouseEventArgs const&> MouseMove;

			// Mouse wheel events
			EventHandler<Control&, MouseWheelArgs const&> MouseWheel;

			// Timer events
			EventHandler<Control&, TimerEventArgs const&> Timer;

			// Dropped files
			EventHandler<Control&, DropFilesEventArgs const&> DropFiles;

			#pragma endregion

			#pragma region Handlers

			// Handle window size changing starting or stopping
			virtual void OnWindowPosChange(SizeEventArgs const& args)
			{
				WindowPosChange(*this, args);
			}

			// Handle the Paint event. Return true, to prevent anything else handling the event
			virtual bool OnPaint(PaintEventArgs const& args)
			{
				Paint(*this, args);

				if (m_dbl_buffer) // This can't be right...
				{
					if (args.m_alternate_hdc != 0) // If wparam != 0 then is should be an existing HDC
					{
						MemDC mem(args.m_alternate_hdc, ClientRect());
						DefWndProc(WM_PRINTCLIENT, WPARAM(mem.m_hdc), LPARAM(PRF_CHECKVISIBLE|PRF_NONCLIENT|PRF_CLIENT));
					}
					else
					{
						PaintStruct p(m_hwnd);
						MemDC mem(p.hdc, p.rcPaint);
						::FillRect(mem.m_hdc, &mem.m_rect, ::GetSysColorBrush(DC_BRUSH));
						DefWndProc(WM_PRINTCLIENT, WPARAM(mem.m_hdc), LPARAM(PRF_CHECKVISIBLE|PRF_NONCLIENT|PRF_CLIENT));
					}
					return true;
				}

				return false;
			}

			// Handle the Erase background event. Return true, to prevent anything else handling the event
			virtual bool OnEraseBkGnd(EmptyArgs const& args)
			{
				EraseBkGnd(*this, args);

				if (m_dbl_buffer) // Check this...
					return S_FALSE;

				return false;
			}

			// Handle keyboard key down/up events. Return true, to prevent anything else handling the event
			virtual bool OnKey(KeyEventArgs const& args)
			{
				Key(*this, args);
				return false;
			}

			// Handle mouse button down/up events. Return true, to prevent anything else handling the event
			virtual bool OnMouseButton(MouseEventArgs const& args)
			{
				MouseButton(*this, args);
				return false;
			}

			// Handle mouse button single click events
			// Single clicks occur between down and up events
			// Return true, to prevent anything else handling the event
			virtual bool OnMouseClick(MouseEventArgs const& args)
			{
				MouseClick(*this, args);
				return false;
			}

			// Handle mouse move events
			virtual void OnMouseMove(MouseEventArgs const& args)
			{
				MouseMove(*this, args);
			}

			// Handle mouse wheel events. Return true, to prevent anything else handling the event
			virtual bool OnMouseWheel(MouseWheelArgs const& args)
			{
				MouseWheel(*this, args);
				return false;
			}

			// Handle timer events
			virtual void OnTimer(TimerEventArgs const& args)
			{
				Timer(*this, args);
			}

			// Handle files dropped onto the control
			virtual void OnDropFiles(DropFilesEventArgs const& args)
			{
				DropFiles(*this, args);
			}

			#pragma endregion
		};
		#pragma endregion

		#pragma region Form
		// Base class for an application or dialog window
		template <typename Derived> struct Form :Control
		{
			// Notes:
			// Neither Form or Control define a load of OnXYZ handlers. This is because it adds a
			// load of potentially unneeded entries to the vtbl. The expected way to use this class
			// is to override ProcessWindowMessage and decode/handle the window messages you actually
			// need. Notice, WM_CREATE is typically not needed, the constructor of your derived type
			// is where OnCreate() code should go.
			using form_t = Form<Derived>;

		protected:
			template <typename D> friend struct Form;
			friend struct Control;

			HINSTANCE   m_hinst;            // Module instance
			bool        m_app_main_window;  // True if this is the main application window
			DlgTemplate m_template;         // The indirect dialog template (if given)
			int         m_menu_id;          // The id of the main menu
			int         m_exit_code;        // The code to return when the form closes
			bool        m_dialog_behaviour; // True if the form is a modal or modeless dialog (determines whether IsDialogMessage() is called)
			bool        m_modal;            // True if this is a dialog being displayed modally

			// Default win class info
			static WNDCLASSEXW WndClassInfo(HINSTANCE hinst)
			{
				return MakeWndClassInfo<form_t>(hinst);
			}

			// Window proc. Derived forms should not override WndProc.
			// All messages are passed to 'ProcessWindowMessage' so use that.
			LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam) override final
			{
				//WndProcDebug(message, wparam, lparam);
				LRESULT result = S_OK;

				// Check if this message is a dialog message
				auto msg = MSG{m_hwnd, message, wparam, lparam, ::GetMessageTime(), ::GetMessagePos()};
				if (m_dialog_behaviour)
				{
					RAII<bool> prevent_recursion(m_dialog_behaviour, false);
					if (::IsDialogMessageW(m_hwnd, &msg))
						return S_OK;
				}

				// 'TranslateMessage' doesn't change 'msg' it only adds WM_CHAR,etc
				// messages to the message queue for WK_KEYDOWN,etc events
				::TranslateMessage(&msg);

				// Forward the message to the message map function
				// which will forward the message to nested controls
				// If the message map doesn't handle the message,
				// pass it to the WndProc for this control/form.
				if (!ProcessWindowMessage(m_hwnd, message, wparam, lparam, result))
					result = Control::WndProc(message, wparam, lparam);
				
				// This is used for DialogProc somehow
				::SetWindowLongPtrW(m_hwnd, DWLP_MSGRESULT, result);
				return result;
			}

			// Message map function
			// 'hwnd' is the handle of the parent window that contains this control.
			// Messages processed here are the messages sent to the parent window, *not* messages for this window
			// Only change 'result' when specifically returning a result (it defaults to S_OK)
			// Return true to halt message processing, false to allow other controls to process the message
			virtual bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				default:
					{
						// All messages for which the handling is the same for
						// both windows and controls should go through here.
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
							::PostQuitMessage(m_exit_code);
						HookWndProc(false);
						m_hwnd = nullptr;
						return true;
					}
				case WM_COMMAND:
					{
						// Handle commands from the main menu
						auto id        = UINT(LOWORD(wparam)); // The menu_item id or accelerator id
						auto src       = UINT(HIWORD(wparam)); // 0 = menu, 1 = accelerator, 2 = control-defined notification code
						auto ctrl_hwnd = HWND(lparam);         // Only valid when src == 2
						if (HandleMenu(id, src, ctrl_hwnd)) return true;
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
				case WM_SYSCOMMAND:
					{
						auto id = UINT(wparam);
						if (m_app_main_window && (id == SC_CLOSE || id == IDCLOSE))
						{
							Close();
							return true;
						}
						break;
					}
				}
				return false;
			}

			// Default main menu handler
			// 'item_id' - the menu item id or accelerator id
			// 'event_source' - 0 = menu, 1 = accelerator, 2 = control-defined notification code
			// 'ctrl_hwnd' - the control that sent the notification. Only valid when src == 2
			// Typically you'll only need 'menu_item_id' unless your accelerator ids
			// overlap your menu ids, in which case you'll need to check 'event_source'
			virtual bool HandleMenu(UINT item_id, UINT event_source, HWND ctrl_hwnd)
			{
				(void)item_id,event_source,ctrl_hwnd;
				return false;
				// Example implementation
				// switch (menu_item_id)
				// {
				// default: return false;
				// case ID_FILE_EXIT: Close(); return true;
				// }
			}

		public:

			// Frame Window Constructor
			// Use this constructor to create frame windows (i.e. windows not based on a dialog resource)
			//  - hwnd created in constructor
			//  - use Show() to display the window
			//  - use 'parent == ApplicationMainWindow' to cause the app to exit when this window is closed
			//  - if x == CW_USEDEFAULT in CreateWindowEx then the system positions the window, (ignoring y). Only valid for overlapped windows, if used for popup windows will be treated as zero
			//  - if w == CW_USEDEFAULT in CreateWindowEx then the system chooses the width and height. Only valid for overlapped windows, if used for popup windows will be treated as zero
			Form(wchar_t const* title
				,HWND parent = nullptr
				,int x = CW_USEDEFAULT, int y = CW_USEDEFAULT
				,int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,DWORD style = DefaultFormStyle
				,DWORD ex_style = DefaultFormStyleEx
				,int menu_id = IDC_UNUSED
				,char const* name = nullptr
				,LPARAM init_param = 0)
				:Control(IDC_UNUSED, nullptr, name)
				,m_hinst(::GetModuleHandle(nullptr))
				,m_app_main_window(parent == ApplicationMainWindow)
				,m_template()
				,m_menu_id(menu_id)
				,m_exit_code()
				,m_dialog_behaviour(false)
				,m_modal(false)
			{
				parent = !m_app_main_window ? parent : nullptr;
				
				// Ensure the class is registered
				auto atom = RegisterWndClass<Form<Derived>>(m_hinst);

				// Load the optional menu
				HMENU menu = menu_id != IDC_UNUSED ? ::LoadMenuW(m_hinst, MAKEINTRESOURCEW(menu_id)) : nullptr;
				
				// Create an instance of the window class
				// Note: the virtual functions called as a result of CreateWindowEx will
				// not call the derived class' overrides because at this point the derived
				// class is not constructed. Derived classes should call 'Show'.
				InitParam lparam(this, init_param);
				::CreateWindowExW(ex_style, MakeIntAtomW(atom), title, style, x, y, w, h, parent, menu, m_hinst, &lparam);
				Throw(m_hwnd != 0, "CreateWindowEx failed");
			}

			// Dialog Resource Constructor
			// Use this constructor to create a window from a dialog resource description
			//  - hwnd is not created until Show() or ShowDialog() are called
			Form(int idd, char const* name = nullptr)
				:Control(idd, nullptr, name)
				,m_hinst(::GetModuleHandle(nullptr))
				,m_app_main_window(false)
				,m_template()
				,m_menu_id(IDC_UNUSED)
				,m_exit_code()
				,m_dialog_behaviour(true)
				,m_modal(false)
			{}

			// Indirect Dialog Constructor
			// Use this constructor to create a window from a dialog template
			//  - hwnd is not created until Show() or ShowDialog() are called
			//  - if 'modal' is true then the window handle is not created until Show() or ShowDialog() is called
			Form(DlgTemplate const& templ, char const* name = nullptr)
				:Control(IDC_UNUSED, nullptr, name)
				,m_hinst(::GetModuleHandle(nullptr))
				,m_app_main_window(false)
				,m_template(templ)
				,m_menu_id(IDC_UNUSED)
				,m_exit_code()
				,m_dialog_behaviour(true)
				,m_modal(false)
			{}

			// Close on destruction
			virtual ~Form()
			{
				Close();
			}

			// Get/Set whether the form uses dialog-like message handling
			bool DialogBehaviour() const
			{
				 return m_dialog_behaviour;
			}
			void DialogBehaviour(bool enabled)
			{
				m_dialog_behaviour = enabled;
			}

			// Display the form non-modally
			// A form created via any constructor can be displayed non-modally.
			// If not created already, the window handle will be created in this method
			void Show(int show, HWND parent, LPARAM init_param = 0)
			{
				m_modal = false;

				// If the window has not been created yet, create it now
				if (m_hwnd == nullptr)
				{
					InitParam lparam(this, init_param);

					// If a template is given, create the window from the template
					if (m_template.valid())
					{
						m_hwnd = ::CreateDialogIndirectParam(m_hinst, m_template, parent, (DLGPROC)&InitDlgProc, LPARAM(&lparam));
						Throw(m_hwnd != nullptr, "CreateDialogIndirectParam failed");
					}
					else
					{
						assert(m_id != IDC_UNUSED && "Modeless dialog without a resource id");
						m_hwnd = ::CreateDialogParam(m_hinst, MAKEINTRESOURCE(m_id), parent, (DLGPROC)&InitDlgProc, LPARAM(&lparam));
						Throw(m_hwnd != nullptr, "CreateDialogParam failed");
					}
				}
				Throw(m_hwnd != nullptr, "Window not created");

				// Show the window, non-modally
				ShowWindow(m_hwnd, show);
				UpdateWindow(m_hwnd);
			}
			void Show(int show, Control* parent, LPARAM init_param = 0)
			{
				// Get the parent hwnd
				// 'm_app_main_window' can only be set to true. A main window can't become not the main window
				m_app_main_window |= parent == ApplicationMainWindow;
				parent = !m_app_main_window ? parent : nullptr;
				auto parenthwnd = parent ? (HWND)*parent : (HWND)nullptr;

				Show(show, parenthwnd, init_param);
			}
			void Show(int show = SW_SHOW)
			{
				Show(show, (HWND)nullptr);
			}

			// Display the form modally
			EDialogResult ShowDialog(HWND parent, LPARAM init_param = 0)
			{
				m_modal = true;

				// Modal dialogs should not have their window handle created yet, the
				// DialogBox() functions create the window and the message loop
				assert(m_hwnd == nullptr);

				InitParam lparam(this, init_param);

				// If a template is given, create the window from the template
				if (m_template.valid())
				{
					return EDialogResult(::DialogBoxIndirectParam(m_hinst, m_template, parent, (DLGPROC)&InitDlgProc, LPARAM(&lparam)));
				}
				else
				{
					assert(m_id != IDC_UNUSED && "Modal dialog without a resource id");
					return EDialogResult(::DialogBoxParam(m_hinst, MAKEINTRESOURCE(m_id), parent, (DLGPROC)&InitDlgProc, LPARAM(&lparam)));
				}
			}
			EDialogResult ShowDialog(Control* parent = nullptr, LPARAM init_param = 0)
			{
				// Get the parent hwnd
				// 'm_app_main_window' can only be set to true. A main window can't become not the main window
				m_app_main_window |= parent == ApplicationMainWindow;
				parent = !m_app_main_window ? parent : nullptr;
				auto parenthwnd = parent ? (HWND)*parent : (HWND)nullptr;

				return ShowDialog(parenthwnd, init_param);
			}
			void ShowDialog()
			{
				ShowDialog();
			}

			// Close this form
			virtual bool Close(int exit_code = 0)
			{
				if (m_hwnd == nullptr) return false;
				m_exit_code = exit_code;
				Parent(nullptr);
				auto r = m_modal
					? ::EndDialog(m_hwnd, INT_PTR(m_exit_code))
					: ::DestroyWindow(m_hwnd);
				m_hwnd = nullptr;
				return r != 0;
			}
			virtual bool Close(EDialogResult dialog_result)
			{
				return Close(int(dialog_result));
			}
		};
		#pragma endregion

		#pragma region Controls
		struct Label :Control
		{
			enum { DefW = 20, DefH = 23 };
			enum { DefaultStyle = (DefaultControlStyle | WS_GROUP | SS_LEFT) & ~WS_TABSTOP };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"STATIC"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			Label(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			Label(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
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
			enum { DefW = 75, DefH = 23 };
			enum { DefaultStyle = DefaultControlStyle | WS_TABSTOP | BS_PUSHBUTTON | BS_CENTER | BS_TEXT };
			enum { DefaultStyleDefBtn = (DefaultStyle | BS_DEFPUSHBUTTON) & ~BS_PUSHBUTTON };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"BUTTON"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			Button(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			Button(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
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
			enum { DefW = 75, DefH = 23 };
			enum { DefaultStyle = DefaultControlStyle | WS_TABSTOP | BS_AUTOCHECKBOX | BS_LEFT | BS_TEXT };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"BUTTON"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			CheckBox(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			CheckBox(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Get/Set the checked state
			bool Checked() const
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessageW(m_hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED;
			}
			void Checked(bool checked)
			{
				assert(::IsWindow(m_hwnd));
				auto is_checked = Checked();
				::SendMessageW(m_hwnd, BM_SETCHECK, checked ? BST_CHECKED : BST_UNCHECKED, 0);
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
			enum { DefW = 80, DefH = 23 };
			enum { DefaultStyle = DefaultControlStyle | WS_TABSTOP | WS_BORDER | ES_AUTOHSCROLL | ES_AUTOVSCROLL | ES_LEFT };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"EDIT"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			TextBox(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			TextBox(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Create the HWND for the control
			void Create(wchar_t const* text
				,int x = CW_USEDEFAULT, int y = CW_USEDEFAULT, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,DWORD style = DefaultControlStyle ,DWORD ex_style = DefaultControlStyleEx
				,HWND hwndparent = 0 // The top level parent window
				,HMENU menu = nullptr
				,void* init_param = nullptr)
			{
				Control::Create<TextBox>(text, x, y, w, h, style, ex_style, hwndparent, menu, init_param);
			}

			// The number of characters in the text
			int TextLength() const
			{
				assert(::IsWindow(m_hwnd));
				auto len = GETTEXTLENGTHEX{GTL_DEFAULT, CP_ACP};
				return int(::SendMessageW(m_hwnd, EM_GETTEXTLENGTHEX, WPARAM(&len), 0));
			}

			// The number of lines of text
			int LineCount() const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, EM_GETLINECOUNT, 0, 0));
			}

			// The length (in characters) of the line containing the character at the given index
			// 'char_index = -1' means the number of *unselected* characters on the lines spanned by the selection.
			int LineLength(int char_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, EM_LINELENGTH, WPARAM(char_index), 0));
			}

			// Gets the character index of the first character on the given line
			// 'line_index = -1' means the current line containing the caret
			int CharFromLine(int line_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, EM_LINEINDEX, WPARAM(line_index), 0));
			}

			// Gets the index of the line that contains 'char_index'
			int LineFromChar(int char_index) const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, EM_EXLINEFROMCHAR, 0, LPARAM(char_index)));
			}

			RangeI Selection() const
			{
				assert(::IsWindow(m_hwnd));
				RangeI r;
				::SendMessageW(m_hwnd, EM_GETSEL, WPARAM(&r.beg), LPARAM(&r.end));
				return r;
			}
			void Selection(RangeI range, bool scroll_to_caret = true)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, EM_SETSEL, range.beg, range.end);
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
				::SendMessageW(m_hwnd, EM_SCROLLCARET, 0, 0);
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
			enum { DefW = 121, DefH = 21 };
			enum { DefaultStyle = DefaultControlStyle | WS_TABSTOP | CBS_DROPDOWN | CBS_AUTOHSCROLL };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"COMBOBOX"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			ComboBox(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			ComboBox(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Get the number of items in the combo box
			int Count() const
			{
				assert(::IsWindow(m_hwnd));
				auto c = int(::SendMessageW(m_hwnd, CB_GETCOUNT, 0, 0L));
				Throw(c != CB_ERR, "Error retrieving combo box item count");
				return c;
			}

			// Get the item at index position 'index'
			string Item(int index) const
			{
				assert(::IsWindow(m_hwnd));

				string s;
				auto len = ::SendMessageW(m_hwnd, CB_GETLBTEXTLEN, index, 0);
				Throw(len != CB_ERR, std::string("ComboBox: Invalid item index ").append(std::to_string(index)));
				if (len == 0) return s;

				s.resize(len);
				s.resize(::SendMessageW(m_hwnd, CB_GETLBTEXT, index, (LPARAM)&s[0]));
				return s;
			}

			// Get/Set the selected index
			int SelectedIndex() const
			{
				assert(::IsWindow(m_hwnd));
				auto index = int(::SendMessageW(m_hwnd, CB_GETCURSEL, 0, 0L));
				return index;
			}
			void SelectedIndex(int index)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, CB_SETCURSEL, index, 0L);
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
				::SendMessageW(m_hwnd, CB_RESETCONTENT, 0, 0L);
			}

			// Add a string to the combo box dropdown list
			int AddItem(wchar_t const* item)
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, CB_ADDSTRING, 0, (LPARAM)item));
			}
			void AddItems(std::initializer_list<wchar_t const*> items)
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
		struct ProgressBar :Control
		{
			enum { DefW = 100, DefH = 23 };
			enum { DefaultStyle = (DefaultControlStyle | PBS_SMOOTH) & ~WS_TABSTOP };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"msctls_progress32"; }

			ProgressBar(int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), L"", x, y, w, h, id, parent, anchor, style, ex_style, name, nullptr, init_param)
			{}
			ProgressBar(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

			// Get/Set the progress bar position
			int Pos() const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, PBM_GETPOS, 0, 0L));
			}
			int Pos(int pos)
			{
				assert(::IsWindow(m_hwnd));
				return int(short(LOWORD(::SendMessageW(m_hwnd, PBM_SETPOS, pos, 0L))));
			}

			// Move the bar position by a delta
			int OffsetPos(int delta)
			{
				assert(::IsWindow(m_hwnd));
				return int(short(LOWORD(::SendMessageW(m_hwnd, PBM_DELTAPOS, delta, 0L))));
			}

			// Get/Set the progress range
			RangeI Range() const
			{
				assert(::IsWindow(m_hwnd));
				PBRANGE range = {};
				::SendMessageW(m_hwnd, PBM_GETRANGE, TRUE, (LPARAM)&range);
				return RangeI(range.iLow, range.iHigh);
			}
			void Range(RangeI rng)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, PBM_SETRANGE32, rng.beg, rng.end);
			}
			void Range(int min, int max)
			{
				Range(RangeI(min,max));
			}

			// Get/Set marquee mode
			bool Marquee() const
			{
				assert(::IsWindow(m_hwnd));
				return (Style() & PBS_MARQUEE) != 0;
			}
			bool Marquee(bool marquee, UINT update_time = 0U)
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessageW(m_hwnd, PBM_SETMARQUEE, WPARAM(marquee), LPARAM(update_time)) != 0;
			}

			// Get/Set the step size
			int StepSize() const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, PBM_GETSTEP, 0, 0L));
			}
			int StepSize(int step_size)
			{
				assert(::IsWindow(m_hwnd));
				return int(short(LOWORD(::SendMessageW(m_hwnd, PBM_SETSTEP, step_size, 0L))));
			}

			// Get/Set the bar colour
			COLORREF BarColor() const
			{
				assert(::IsWindow(m_hwnd));
				return COLORREF(::SendMessageW(m_hwnd, PBM_GETBARCOLOR, 0, 0L));
			}
			COLORREF BarColor(COLORREF clr)
			{
				assert(::IsWindow(m_hwnd));
				return COLORREF(::SendMessageW(m_hwnd, PBM_SETBARCOLOR, 0, (LPARAM)clr));
			}

			// Get/Set the bar background colour
			COLORREF BarBkgdColor() const
			{
				assert(::IsWindow(m_hwnd));
				return COLORREF(::SendMessageW(m_hwnd, PBM_GETBKCOLOR, 0, 0L));
			}
			COLORREF BarBkgdColor(COLORREF clr)
			{
				assert(::IsWindow(m_hwnd));
				return COLORREF(::SendMessageW(m_hwnd, PBM_SETBKCOLOR, 0, (LPARAM)clr));
			}

			// Get/Set the state
			int State() const
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, PBM_GETSTATE, 0, 0L));
			}
			int State(int state)
			{
				assert(::IsWindow(m_hwnd));
				return int(::SendMessageW(m_hwnd, PBM_SETSTATE, state, 0L));
			}

			// Step the bar
			int StepIt()
			{
				assert(::IsWindow(m_hwnd));
				return int(short(LOWORD(::SendMessageW(m_hwnd, PBM_STEPIT, 0, 0L))));
			}

			// Events
			EventHandler<ProgressBar&, EmptyArgs const&> ProgressUpdate;

			// Handlers
			virtual void OnProgressUpdate()
			{
				ProgressUpdate(*this, EmptyArgs());
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				/*switch (message)
				{
				case WM_PROGRESS_UPDATE:
					if (LOWORD(wparam) != m_id) break;
					switch (HIWORD(wparam))
					{
					case BN_CLICKED:
						OnClick();
						break;
					}
				}*/
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}

		};
		struct Panel :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum { DefaultStyle = DefaultControlStyle | WS_BORDER };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"pr::gui::Panel"; }
			static WNDCLASSEXW WndClassInfo(HINSTANCE hinst)
			{
				return MakeWndClassInfo<Panel>(hinst);
			}

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			Panel(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(MakeIntAtomW(RegisterWndClass<Panel>()), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			Panel(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}

		};
		struct GroupBox :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum { DefaultStyle = DefaultControlStyle | BS_GROUPBOX };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"BUTTON"; } // yes, groupbox's use the button window class

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			GroupBox(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
			{}
			GroupBox(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
			{}
		};
		struct RichTextBox :TextBox
		{
			static wchar_t const* WndClassName() { return ::LoadLibraryW(L"msftedit.dll") ? L"RICHEDIT50W" : L"RICHEDIT20W"; }

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			RichTextBox(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:TextBox(id, parent, name, anchor)
			{}
		};
		struct StatusBar :Control
		{
			enum { DefaultStyle = DefaultControlStyle | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | SBARS_SIZEGRIP };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return STATUSCLASSNAMEW; }

			StatusBar(Control* parent, int id, wchar_t const* text, char const* name = nullptr, DWORD style = DefaultStyle)
				:Control(id, parent, name)
			{
				Attach(::CreateStatusWindowW(style, text, *parent, id));
				Throw(IsWindow(m_hwnd), "Failed to create the status bar");
				Dock(EDock::Bottom);
			}
			~StatusBar()
			{
				Detach();
			}

			// Get/Set the parts of the status bar
			int Parts(int count, int* parts) const
			{
				assert(::IsWindow(m_hwnd));
				return (int)::SendMessageW(m_hwnd, SB_GETPARTS, WPARAM(count), LPARAM(parts));
			}
			bool Parts(int count, int* widths)
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessageW(m_hwnd, SB_SETPARTS, WPARAM(count), LPARAM(widths)) != 0;
			}

			// Get/Set the text in a pane in the status bar
			string Text(int pane, int* type = nullptr) const
			{
				assert(::IsWindow(m_hwnd) && pane >= 0 && pane < 256);

				string s;
				s.resize(LOWORD(::SendMessageW(m_hwnd, SB_GETTEXTLENGTH, WPARAM(pane), LPARAM(0))) + 1, 0);
				if (!s.empty())
				{
					auto ret = DWORD(::SendMessageW(m_hwnd, SB_GETTEXT, WPARAM(pane), LPARAM(&s[0])));
					if (type) *type = (int)(short)HIWORD(ret);
					s.resize(LOWORD(ret));
				}
				return s;
			}
			void Text(int pane, string text, int type = 0)
			{
				assert(::IsWindow(m_hwnd) && pane >= 0 && pane < 256);
				Throw(::SendMessageW(m_hwnd, SB_SETTEXT, WPARAM(pane | type), LPARAM(text.c_str())) != 0, "Failed to set status bar pane text");
			}

			// Get the client area of a pane in the status bar
			pr::gui::Rect Rect(int pane) const
			{
				assert(::IsWindow(m_hwnd) && pane >= 0 && pane < 256);
				pr::gui::Rect rect;
				Throw(::SendMessageW(m_hwnd, SB_GETRECT, WPARAM(pane), LPARAM(&rect)) != 0, "Failed to get the client rect for a status bar pane");
				return rect;
			}

			#if 0 // todo
			BOOL GetBorders(int* pBorders) const
			{
				assert(::IsWindow(m_hwnd));
				return (BOOL)::SendMessageW(m_hwnd, SB_GETBORDERS, 0, (LPARAM)pBorders);
			}

			BOOL GetBorders(int& nHorz, int& nVert, int& nSpacing) const
			{
				assert(::IsWindow(m_hwnd));
				int borders[3] = { 0, 0, 0 };
				BOOL bResult = (BOOL)::SendMessageW(m_hwnd, SB_GETBORDERS, 0, (LPARAM)&borders);
				if(bResult)
				{
					nHorz = borders[0];
					nVert = borders[1];
					nSpacing = borders[2];
				}
				return bResult;
			}

			void SetMinHeight(int nMin)
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, SB_SETMINHEIGHT, nMin, 0L);
			}

			BOOL SetSimple(BOOL bSimple = TRUE)
			{
				assert(::IsWindow(m_hwnd));
				return (BOOL)::SendMessageW(m_hwnd, SB_SIMPLE, bSimple, 0L);
			}

			BOOL IsSimple() const
			{
				assert(::IsWindow(m_hwnd));
				return (BOOL)::SendMessageW(m_hwnd, SB_ISSIMPLE, 0, 0L);
			}

			BOOL GetUnicodeFormat() const
			{
				assert(::IsWindow(m_hwnd));
				return (BOOL)::SendMessageW(m_hwnd, SB_GETUNICODEFORMAT, 0, 0L);
			}

			BOOL SetUnicodeFormat(BOOL bUnicode = TRUE)
			{
				assert(::IsWindow(m_hwnd));
				return (BOOL)::SendMessageW(m_hwnd, SB_SETUNICODEFORMAT, bUnicode, 0L);
			}

			void GetTipText(int nPane, LPTSTR lpstrText, int nSize) const
			{
				assert(::IsWindow(m_hwnd));
				assert(nPane < 256);
				::SendMessageW(m_hwnd, SB_GETTIPTEXT, MAKEWPARAM(nPane, nSize), (LPARAM)lpstrText);
			}

			void SetTipText(int nPane, LPCTSTR lpstrText)
			{
				assert(::IsWindow(m_hwnd));
				assert(nPane < 256);
				::SendMessageW(m_hwnd, SB_SETTIPTEXT, nPane, (LPARAM)lpstrText);
			}

			COLORREF SetBkColor(COLORREF clrBk)
			{
				assert(::IsWindow(m_hwnd));
				return (COLORREF)::SendMessageW(m_hwnd, SB_SETBKCOLOR, 0, (LPARAM)clrBk);
			}

			HICON GetIcon(int nPane) const
			{
				assert(::IsWindow(m_hwnd));
				assert(nPane < 256);
				return (HICON)::SendMessageW(m_hwnd, SB_GETICON, nPane, 0L);
			}

			BOOL SetIcon(int nPane, HICON hIcon)
			{
				assert(::IsWindow(m_hwnd));
				assert(nPane < 256);
				return (BOOL)::SendMessageW(m_hwnd, SB_SETICON, nPane, (LPARAM)hIcon);
			}
			#endif
		};
		struct TabControl :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum { DefaultStyle = DefaultControlStyle };
			enum { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"SysTabControl32"; }

			struct Item :TCITEMW
			{
				Item() :TCITEMW() {}
				Item(wchar_t const* label, int image = -1, LPARAM param = 0) :TCITEMW()
				{
					mask    = TCIF_TEXT | (image != -1 ? TCIF_IMAGE : 0) | (param != 0 ? TCIF_PARAM : 0);
					pszText = const_cast<wchar_t*>(label);
					iImage  = image;
					lParam  = param;
				}
			};
			struct TabEventArgs :EmptyArgs
			{
				Control* m_tab;
				int      m_tab_index;
				TabEventArgs(Control* tab, int tab_index) :EmptyArgs() ,m_tab(tab) ,m_tab_index(tab_index) {}
			};
			struct TabSwitchEventArgs :CancelEventArgs
			{
				// True if 'm_tab' is being switched to (cancel ignored in this case)
				// False if 'm_tab' is being switched away from (cancel will stop the switch)
				bool m_activating;

				// The tab being left/entered
				Control* m_tab;
				int      m_tab_index;

				TabSwitchEventArgs(bool activating, Control* tab, int tab_index)
					:CancelEventArgs(false)
					,m_activating(activating)
					,m_tab(tab)
					,m_tab_index(tab_index)
				{}
			};

			std::vector<Control*> m_tabs; // The tab pages. Owned externally
			int m_border_size;
			int m_top_pad;

			// Note, if you want events from this control is must have an id != IDC_UNUSED
			TabControl(wchar_t const* text
				,int x, int y, int w = DefW, int h = DefH
				,int id = IDC_UNUSED
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = DefaultStyle
				,DWORD ex_style = DefaultStyleEx
				,char const* name = nullptr
				,void* init_param = nullptr)
				:Control(WndClassName(), text, x, y, w, h, id, parent, anchor, style, ex_style, name, HMENU(id), init_param)
				,m_tabs()
				,m_border_size(3)
				,m_top_pad(5)
			{}
			TabControl(int id = IDC_UNUSED, Control* parent = nullptr, char const* name = nullptr, EAnchor anchor = EAnchor::Left|EAnchor::Top)
				:Control(id, parent, name, anchor)
				,m_tabs()
				,m_border_size(3)
				,m_top_pad(5)
			{}

			// The number of tabs added
			int TabCount() const
			{
				return int(m_tabs.size());
			}

			// Get a tab by index
			Control& Tab(int index) const
			{
				ValidateTabIndex(index);
				return *m_tabs[index];
			}

			// The active tab
			Control* ActiveTab() const
			{
				auto active_tab_index = SelectedIndex();
				return active_tab_index != -1 ? m_tabs[active_tab_index] : nullptr;
			}

			// Get/Set the active tab by index.
			int SelectedIndex() const
			{
				return int(::SendMessageW(m_hwnd, TCM_GETCURSEL, 0, 0));
			}
			void SelectedIndex(int tab_index)
			{
				auto active_tab_index = SelectedIndex();
				if (tab_index == active_tab_index) return;
				SwitchTab(active_tab_index, tab_index, true);
				Invalidate();
			}

			// Add a tab to the tab control.
			// label - The label to appear on the tab control.
			// tab - The child control to use as the view for the tab.
			// active - true to make the tab active, false to just add the tab.
			// image - The index into the image list for the image to display by the tab label.
			// param - The param value to associate with the tab.
			// Returns the zero based index of the new tab, -1 on failure
			int Insert(wchar_t const* label, Control& tab, int index = -1, bool active = true, int image = -1, LPARAM param = 0)
			{
				// Make sure it's a real window
				assert(::IsWindow(tab));

				// The window must have the WS_CHILD style bit set and the WS_VISIBLE style bit not set.
				tab.Style((tab.Style() | WS_CHILD) & ~WS_VISIBLE);

				// Hide the view window
				tab.Enabled(false);
				tab.Visible(false);

				// Add the tab to the tab control
				Item item(label, image, param);
		
				// Insert the item at the end of the tab control
				index = index != -1 ? index : TabCount();
				index = int(::SendMessageW(m_hwnd, TCM_INSERTITEMW, WPARAM(index), LPARAM(&item)));
				Throw(index != -1, pr::FmtS("Failed to add tab %s", Narrow(label).c_str()));

				// Add the tab
				m_tabs.push_back(&tab);
				tab.Parent(this);
				::SetParent(tab, m_hwnd);

				// Set the position for the window
				tab.ParentRect(CalcViewRect(), true, m_hwnd, WindowPos::EFlags::NoActivate);
		
				// Select the tab that is being added, if desired
				auto last = TabCount() - 1;
				if (active || last == 0)
					SelectedIndex(last);

				OnTabAdded(TabEventArgs(&tab, index));
				return index;
			}

			// Remove a tab by index.
			// tab_index - The index of the tab to remove.
			// Returns the removed tab.
			Control* Remove(int tab_index)
			{
				ValidateTabIndex(tab_index);

				// Save the window handle that is being removed
				auto tab = m_tabs[tab_index];

				// Notify subclasses that a tab was removed
				OnTabRemoved(TabEventArgs(tab, tab_index));

				// Adjust the active tab index if deleting a tab before it
				auto new_tab_count = TabCount() - 1;
				auto active_tab_index = SelectedIndex();
				if (active_tab_index >= new_tab_count)
					active_tab_index = new_tab_count - 1;

				// Remove the item from the view list
				Throw(::SendMessageW(m_hwnd, TCM_DELETEITEM, tab_index, 0) != 0, pr::FmtS("Failed to delete tab %d", tab_index));
				m_tabs.erase(std::begin(m_tabs) + tab_index);
				tab->Parent(nullptr);

				// Adjust the active tab index if deleting a tab before it
				SelectedIndex(active_tab_index);

				return tab;
			}

			// Remove all the tabs from the tab control.
			void RemoveAllTabs()
			{
				// Delete tabs in reverse order to preserve indices
				for (auto i = TabCount(); i-- != 0;)
					Remove(i);
			}

			// Return tab info for a tab by index
			Item TabInfo(int tab_index, int mask = TCIF_TEXT|TCIF_IMAGE|TCIF_PARAM, wchar_t* buf = nullptr, int buf_count = 0) const
			{
				ValidateTabIndex(tab_index);

				Item info;
				info.mask = mask;
				info.pszText = buf;
				info.cchTextMax = buf_count;
				Throw(::SendMessage(m_hwnd, TCM_GETITEMW, tab_index, LPARAM(&info)) != 0, pr::FmtS("Failed to read item info for tab %d", tab_index));
				return info;
			}

			// Return the label of the specified tab.
			std::wstring TabText(int tab_index) const
			{
				// If the TCIF_TEXT flag is set in the mask member of the TCITEM structure, the control may change the pszText
				// member of the structure to point to the new text instead of filling the buffer with the requested text.
				// The control may set the pszText member to NULL to indicate that no text is associated with the item.
				wchar_t buf[128] = {};
				auto info = TabInfo(tab_index, TCIF_TEXT, buf, _countof(buf));
				return info.pszText != nullptr ? info.pszText : L"";
			}

			// Return the image index for a tab by index
			int TabImage(int tab_index) const
			{
				return TabInfo(tab_index, TCIF_IMAGE).iImage;
			}

			// Return the param for a tab by index
			LPARAM TabParam(int tab_index) const
			{
				return TabInfo(tab_index, TCIF_PARAM).lParam;
			}

			// Update the position of all the contained views.
			void UpdateViews()
			{
				auto rect = CalcViewRect();
				for (auto tab : m_tabs)
					tab->ScreenRect(rect, true, nullptr, WindowPos::EFlags::NoZorder);
			}

			// Events
			EventHandler<TabControl&, TabEventArgs const&> TabAdded;
			EventHandler<TabControl&, TabEventArgs const&> TabRemoved;
			EventHandler<TabControl&, TabSwitchEventArgs const&> TabSwitch;

			// Handlers
			virtual void OnTabAdded(TabEventArgs const& args)
			{
				TabAdded(*this, args);
			}
			virtual void OnTabRemoved(TabEventArgs const& args)
			{
				TabRemoved(*this, args);
			}
			virtual void OnTabSwitch(TabSwitchEventArgs& args)
			{
				TabSwitch(*this, args);
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_WINDOWPOSCHANGED:
					{
						UpdateViews();
						break;
					}
				case WM_NOTIFY:
					{
						auto hdr = (NMHDR*)lparam;
						switch (hdr->code)
						{
						case TCN_SELCHANGING:
							{
								auto tab_index = SelectedIndex();
								auto args = TabSwitchEventArgs(false, m_tabs[tab_index], tab_index);
								OnTabSwitch(args);
								if (args.m_cancel) return TRUE;
								SwitchTab(tab_index, -1, false);
								break;
							}
						case TCN_SELCHANGE:
							{
								auto tab_index = SelectedIndex();
								auto args = TabSwitchEventArgs(true, m_tabs[tab_index], tab_index);
								SwitchTab(-1, tab_index, false);
								OnTabSwitch(args);
								break;
							}
						}
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}

		protected:

			// Throw if 'tab_index' is invalid
			void ValidateTabIndex(int tab_index) const
			{
				Throw(tab_index >= 0 && tab_index < TabCount(), pr::FmtS("Tab index (%d) out of range", tab_index));
			}

			// Calculate the client rect for contained views.
			Rect CalcViewRect() const
			{
				auto sr = ScreenRect();

				//auto rect = ClientRect();
				//rect.right  -= 1;
				//rect.bottom -= 1;
				//// Calculate the Height (or Width) of the tab cause it could be Multiline
				//Rect tab_rect;
				//Throw(::SendMessageW(m_hwnd, TCM_GETITEMRECT, 0, LPARAM(&tab_rect)) != 0, "Failed to read the size of the 0th tab");
				//auto row_count   = (int)(::SendMessageW(m_hwnd, TCM_GETROWCOUNT, 0, 0));
				//auto edge_width  = (tab_rect.width()  * row_count) + m_top_pad;
				//auto edge_height = (tab_rect.height() * row_count) + m_top_pad;

				// Set the size based on the style
				// All the same at the mo, need to test each style...
				auto style = Style();
				if ((style & TCS_BOTTOM) && !(style & TCS_VERTICAL)) // Bottom
				{
					sr = sr.Adjust(m_border_size - 4, m_border_size - 2, 2 - m_border_size, 3 - m_border_size);
				}
				else if ((style & TCS_RIGHT) && (style & TCS_VERTICAL)) // Right
				{
					sr = sr.Adjust(m_border_size - 4, m_border_size - 2, 2 - m_border_size, 3 - m_border_size);
				}
				else if (style & TCS_VERTICAL) // Left
				{
					sr = sr.Adjust(m_border_size - 4, m_border_size - 2, 2 - m_border_size, 3 - m_border_size);
				}
				else // Top
				{
					sr = sr.Adjust(m_border_size - 4, m_border_size - 2, 2 - m_border_size, 3 - m_border_size);
				}

				::SendMessageW(m_hwnd, TCM_ADJUSTRECT, FALSE, LPARAM(&sr));
				return sr.height() > 0 && sr.width() > 0 ? sr : Rect();
			}

			// Switch from tab 'old' to tab 'neu'
			void SwitchTab(int old, int neu, bool setcursel)
			{
				// Disable the old tab
				if (old != -1)
				{
					ValidateTabIndex(old);
					auto& old_tab = *m_tabs[old];
					if (::IsWindow(old_tab))
					{
						old_tab.Enabled(false);
						old_tab.Visible(false);
						old_tab.Invalidate();
					}
				}

				// Enable the new tab
				if (neu != -1)
				{
					ValidateTabIndex(neu);
					auto& neu_tab = *m_tabs[neu];
					if (::IsWindow(neu_tab))
					{
						neu_tab.Enabled(true);
						neu_tab.Visible(true);
						neu_tab.Focus();
						neu_tab.Invalidate();
					}
				}

				// Set the new tab index
				if (setcursel)
				{
					::SendMessageW(m_hwnd, TCM_SETCURSEL, neu, 0);
				}
			}
		};
		#pragma endregion
	}
}

#pragma warning(pop)

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
using namespace pr::gui;

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