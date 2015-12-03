//*****************************************************************************************
// Win32 API 
//  Copyright (c) Rylogic Ltd 2014
//*****************************************************************************************
// A collection of structs that wrap the win32 API and expose an
// interface similar to C# .NET win forms. Inspired by ATL\WTL.
// Specs:
//   - As fast as ATL\WTL. Uses ATL thunks for WNDPROC
//   - Doesn't use macros. Much easier to debug/read
//   - Single file with minimal dependencies. Depends on standard
//     C++ headers, atlbase.h, and other standard headers (no external
//     libraries needed)
//   - Automatic support for resizing
//   - C#.NET style event handlers
//
#pragma once

#include <vector>
#include <string>
#include <fstream>
#include <unordered_map>
#include <algorithm>
#include <functional>
#include <atomic>
#include <mutex>
#include <thread>
#include <type_traits>
#include <cassert>
#include <tchar.h>
#include <atlbase.h>
#include <atlstdthunk.h>
#include <commctrl.h>
#include <commdlg.h>
#include <richedit.h>
#include <shellapi.h>
#include <shobjidl.h>
#include <shlguid.h>
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
#pragma warning(disable: 4351) // new behaviour: elements of array will be default initialized

namespace pr
{
	namespace gui
	{
		static_assert(_WIN32_WINNT >= 0x0500, "Minimum windows version is 0x0500");
		using namespace std::placeholders;

		// Forwards
		struct DlgTemplate;
		struct Control;
		struct Form;

		// Special id for controls that don't need an id
		static int const ID_UNUSED = 0x00FFFFFF;

		// A user windows message the returns the Control* associated with a given hwnd
		static int const WM_GETCTRLPTR = WM_USER + 0x6569;

		// Tree/List constants
		using HLISTITEM = int; // A typedef for symmetry with TreeView
		static HTREEITEM const INVALID_TREE_ITEM =  0;
		static HLISTITEM const INVALID_LIST_ITEM = -1;

		#pragma region Enumerations
		// True (true_type) if 'T' has '_bitwise_operators_allowed' as a static member
		template <typename T> struct has_bitwise_operations_allowed
		{
		private:
			template <typename U> static std::true_type  check(decltype(U::_bitwise_operators_allowed)*);
			template <typename>   static std::false_type check(...);
		public:
			using type = decltype(check<T>(0));
			static bool const value = type::value;
		};
		template <typename T> struct support_bitwise_operators :has_bitwise_operations_allowed<T>::type {};
		template <typename T, typename = std::enable_if_t<support_bitwise_operators<T>::value>> struct bitwise_operators_enabled;

		// The common control classes
		enum class ECommonControl
		{
			None            = 0,
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
			All             = ~None,
			_bitwise_operators_allowed,
		};

		// Auto size anchors
		enum class EAnchor
		{
			None            = 0,
			Left            = 1 << 0,
			Top             = 1 << 1,
			Right           = 1 << 2,
			Bottom          = 1 << 3,
			TopLeft         = Left|Top,
			TopRight        = Right|Top,
			BottomLeft      = Left|Bottom,
			BottomRight     = Right|Bottom,
			LeftTopRight    = Left|Top|Right,
			LeftBottomRight = Left|Bottom|Right,
			All             = Left|Top|Right|Bottom,
			_bitwise_operators_allowed,
		};

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

		// Window start position
		enum class EStartPosition
		{
			Default,
			CentreParent,
			Manual,
		};

		// Set window position flags
		enum class EWindowPos :UINT
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
			_bitwise_operators_allowed,
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
			_bitwise_operators_allowed,
		};

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
			_bitwise_operators_allowed,
		};

		// Don't add WS_VISIBLE to the default style. Derived forms should choose when to be visible at the end of their constructors
		// WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX)
		// WS_POPUPWINDOW = (WS_POPUP|WS_BORDER|WS_SYSMENU)
		// WS_EX_COMPOSITED adds automatic double buffering, which doesn't work for Dx apps
		enum :DWORD { DefaultFormStyle = DS_SETFONT | DS_FIXEDSYS | WS_OVERLAPPEDWINDOW | WS_CLIPCHILDREN | WS_CLIPSIBLINGS };
		enum :DWORD { DefaultFormStyleEx = WS_EX_APPWINDOW | WS_EX_WINDOWEDGE };

		enum :DWORD { DefaultDialogStyle = (DefaultFormStyle & ~WS_OVERLAPPED) | DS_MODALFRAME | WS_POPUPWINDOW };
		enum :DWORD { DefaultDialogStyleEx = (DefaultFormStyleEx & ~WS_EX_APPWINDOW) };

		enum :DWORD { DefaultControlStyle = WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS };
		enum :DWORD { DefaultControlStyleEx = 0 };

		// Construction window creation flag
		enum class ECreate
		{
			Create, // Create the hwnd during construction
			Defer,  // Don't create the hwnd
			Auto
		};

		// Flags for Control Rect methods
		enum class ERectFlags
		{
			ExcludeDockedChildren = 1 << 0,
			_bitwise_operators_allowed,
		};
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

		#pragma region Support Functions

		// Cast with overflow check
		template <typename TTo, typename TFrom> inline TTo cast(TFrom from)
		{
			assert(static_cast<TFrom>(static_cast<TTo>(from)) == from && "overflow or underflow in cast");
			return static_cast<TTo>(from);
		}

		// Convert to byte pointer
		template <typename T> byte const* bptr(T const* t) { return reinterpret_cast<byte const*>(t); }
		template <typename T> byte*       bptr(T*       t) { return reinterpret_cast<byte*      >(t); }

		// Enum bitwise operators
		template <typename TEnum, typename UT = std::underlying_type<TEnum>::type, typename = bitwise_operators_enabled<TEnum>> inline bool operator == (TEnum lhs, UT rhs)
		{
			return UT(lhs) == rhs;
		}
		template <typename TEnum, typename UT = std::underlying_type<TEnum>::type, typename = bitwise_operators_enabled<TEnum>> inline bool operator != (TEnum lhs, UT rhs)
		{
			return UT(lhs) != rhs;
		}
		template <typename TEnum, typename UT = std::underlying_type<TEnum>::type, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator | (TEnum lhs, TEnum rhs)
		{
			return TEnum(UT(lhs) | UT(rhs));
		}
		template <typename TEnum, typename UT = std::underlying_type<TEnum>::type, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator & (TEnum lhs, TEnum rhs)
		{
			return TEnum(UT(lhs) & UT(rhs));
		}
	
		// Append bytes
		template <typename TCont> void append(TCont& cont, void const* x, size_t byte_count)
		{
			cont.insert(end(cont), bptr(x), bptr(x) + byte_count);
		}

		// Convert an error code into an error message
		inline std::string ErrorMessage(HRESULT result)
		{
			char msg[8192];
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
		// Must be called before creating any controls
		inline void InitCtrls(ECommonControl classes = ECommonControl::StandardClasses)
		{
			auto iccx = INITCOMMONCONTROLSEX{sizeof(INITCOMMONCONTROLSEX), DWORD(classes)};
			Throw(::InitCommonControlsEx(&iccx), "Common control initialisation failed");
		}

		// Replace macros from windowsx.h
		inline WORD MakeWord(DWORD_PTR lo, DWORD_PTR hi) { return WORD((lo &   0xff) | ((hi &   0xff) <<  8)); }
		inline LONG MakeLong(DWORD_PTR lo, DWORD_PTR hi) { return LONG((lo & 0xffff) | ((hi & 0xffff) << 16)); }
		inline WORD HiWord(DWORD_PTR l) { return WORD((l >> 16) & 0xffff); }
		inline BYTE HiByte(DWORD_PTR w) { return BYTE((w >>  8) &   0xff); }
		inline WORD LoWord(DWORD_PTR l) { return WORD(l & 0xffff); }
		inline BYTE LoByte(DWORD_PTR w) { return BYTE(w &   0xff); }
		inline int GetXLParam(LPARAM lparam) { return int(short(LoWord(lparam))); } // GET_X_LPARAM
		inline int GetYLParam(LPARAM lparam) { return int(short(HiWord(lparam))); } // GET_Y_LPARAM
		inline LPARAM MakeLParam(int x, int y) { return LPARAM(short(x) | short(y) << 16); }

		// Replace the MAKEINTATOM macro
		inline wchar_t const* MakeIntAtomW(ATOM atom) { return reinterpret_cast<wchar_t const*>(ULONG_PTR(WORD(atom))); } // MAKEINTATOM

		// Return the window class name that 'hwnd' is an instance of
		inline std::wstring WndClassName(HWND hwnd)
		{
			assert(::IsWindow(hwnd));

			std::wstring cn(64, 0);
			for (int len = 0; len == 0;)
			{
				len = ::GetClassNameW(hwnd, &cn[0], int(cn.size()));
				cn.resize(len == 0 ? cn.size() * 2 : len);
			}
			return cn;
		}

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

		template <typename THandle> struct IdOrHandle
		{
			THandle m_handle;
			int     m_id;

			IdOrHandle() :m_handle() ,m_id(ID_UNUSED) {}
			IdOrHandle(THandle handle) :m_handle(handle) ,m_id(ID_UNUSED) {}
			IdOrHandle(int id) :m_handle() ,m_id(id) {}
			bool operator == (nullptr_t) const { return m_handle == nullptr && m_id == ID_UNUSED; }
			bool operator != (nullptr_t) const { return !(*this == nullptr); }
		};
		#pragma endregion

		#pragma region Win32 Structure Wrappers

		struct Point;
		struct Rect;
		struct Size;

		// String
		using string = std::wstring;

		// Point
		struct Point :POINT
		{
			Point() :POINT() {}
			Point(long x_, long y_) { x = x_; y = y_; }
			explicit Point(SIZE sz) { x = sz.cx; y = sz.cy; }
			explicit Point(LPARAM lparam) :Point(GetXLParam(lparam), GetYLParam(lparam)) {}
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
			bool empty() const                            { return left == right && top == bottom; }
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
			Rect Shifted(int dx, int dy) const
			{
				auto r = *this;
				::OffsetRect(&r, dx, dy);
				return r;
			}
			Rect Inflate(int dx, int dy) const
			{
				auto r = *this;
				::InflateRect(&r, dx, dy);
				return r;
			}
			Rect Adjust(int dl, int dt, int dr, int db) const
			{
				auto r = *this;
				r.left += dl;
				r.top += dt;
				r.right += dr;
				r.bottom += db;
				return r;
			}
			Rect Adjust(Rect const& adj) const
			{
				auto r = *this;
				r.left += adj.left;
				r.top += adj.top;
				r.right += adj.right;
				r.bottom += adj.bottom;
				return r;
			}
			Rect Intersect(Rect const& rhs) const
			{
				auto r = *this;
				::IntersectRect(&r, this, &rhs);
				return r;
			}
			Rect Union(Rect const& rhs) const
			{
				auto r = *this;
				if (*this != rhs) // UnionRect has a bug if these are equal, (returns [0x0])
					::UnionRect(&r, this, &rhs);
				return r;
			}
			Rect Subtract(Rect const& rhs) const
			{
				auto r = *this;
				::SubtractRect(&r, this, &rhs);
				return r;
			}
			Rect NormalizeRect() const
			{
				auto r = *this;
				if (r.left > r.right) std::swap(r.left, r.right);
				if (r.top > r.bottom) std::swap(r.top, r.bottom);
				return r;
			}

			Rect operator +() const { return Rect(+left, +top, +right, +bottom); }
			Rect operator -() const { return Rect(-left, -top, -right, -bottom); }
			bool operator == (Rect const& rhs) const { return left == rhs.left && top == rhs.top && right == rhs.right && bottom == rhs.bottom; }
			bool operator != (Rect const& rhs) const { return !(*this == rhs); }
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
				_bitwise_operators_allowed,
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

		// Window position information
		struct WindowPos :WINDOWPOS
		{
			WindowPos(HWND hwnd_ = nullptr)
				:WindowPos(hwnd_, 0, 0, 0, 0, EWindowPos::NoMove|EWindowPos::NoSize|EWindowPos::NoZorder)
			{}
			WindowPos(HWND hwnd_, Rect const& rect, EWindowPos flags_ = EWindowPos::NoZorder)
				:WindowPos(hwnd_, rect.left, rect.top, rect.width(), rect.height(), flags_)
			{}
			WindowPos(HWND hwnd_, int x_, int y_, int cx_, int cy_, EWindowPos flags_ = EWindowPos::NoZorder) :WINDOWPOS()
			{
				hwnd = hwnd_;
				x = x_;
				y = y_;
				cx = cx_;
				cy = cy_;
				flags = (UINT)flags_;
			}
			Rect Bounds() const
			{
				return Rect(x, y, x + cx, y + cy);
			}
		};

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
			// Never cache a DC. If you have an expensive initialisation of a DC
			// use a ClassDC (CS_CLASSDC) or PrivateDC (CS_OWNDC) instead.
			HDC  m_hdc;
			bool m_owned;
			DC(HDC hdc, bool owned = false) :m_hdc(hdc) ,m_owned(owned) {}
			~DC() { if (m_owned) ::DeleteDC(m_hdc); }
			operator HDC() const { return m_hdc; }
		};
		struct MemDC :DC
		{
			HDC m_hdc_orig;
			Rect m_rect;
			HBITMAP m_bmp;
			HBITMAP m_bmp_old;
			bool m_owns_bmp;

			MemDC(HDC hdc, Rect const& rect, HBITMAP bmp)
				:DC(::CreateCompatibleDC(hdc), true)
				,m_hdc_orig(hdc)
				,m_rect(rect)
				,m_bmp(bmp ? bmp : ::CreateCompatibleBitmap(hdc, rect.width(), rect.height()))
				,m_bmp_old(HBITMAP(::SelectObject(m_hdc, m_bmp)))
				,m_owns_bmp(bmp == nullptr)
			{
				assert(m_bmp != nullptr);
				::SetViewportOrgEx(m_hdc, -m_rect.left, -m_rect.top, nullptr);
			}
			~MemDC()
			{
				::BitBlt(m_hdc_orig, m_rect.left, m_rect.top, m_rect.width(), m_rect.height(), m_hdc, m_rect.left, m_rect.top, SRCCOPY);
				::SelectObject(m_hdc, m_bmp_old);
				if (m_owns_bmp) ::DeleteObject(m_bmp);
			}
		};
		struct ClientDC :DC
		{
			// A ClientDC is restricted to the client area of the window
			// The DC returned by PaintStruct is just a ClientDC with the
			// clipping region set to the update region.
			HWND m_hwnd;
			ClientDC(HWND hwnd) :DC(::GetDC(hwnd), false) ,m_hwnd(hwnd) {}
			~ClientDC() { ::ReleaseDC(m_hwnd, m_hdc); }
		};
		struct WindowDC :DC
		{
			// A WindowDC can access both client and non-client areas of a window
			HWND m_hwnd;
			WindowDC(HWND hwnd) :DC(::GetWindowDC(hwnd), false) ,m_hwnd(hwnd) {}
			~WindowDC() { ::ReleaseDC(m_hwnd, m_hdc); }
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
		// Note: ownership is lost with copying
		// Controls/Forms don't own menus. Menu ownership is a convenience for callers
		// to automatically destroy menus, almost all other use should be with non-owned menus.
		// Note: implicit conversion constructors are delibrate
		struct Brush
		{
			HBRUSH m_obj;
			bool m_owned;

			~Brush()
			{
				if (m_owned)
					::DeleteObject(m_obj);
			}
			Brush()
				:m_obj(nullptr)
				,m_owned(false)
			{}
			Brush(HBRUSH obj, bool owned = false)
				:m_obj(obj)
				,m_owned(owned)
			{}
			Brush(COLORREF col)
				:m_obj(CreateSolidBrush(col)),
				m_owned(true)
			{
				Throw(m_obj != 0, "Failed to create HBRUSH");
			}
			Brush(Brush&& rhs)
				:m_obj(rhs.m_obj)
				,m_owned(rhs.m_owned)
			{
				rhs.m_owned = false;
			}
			Brush(Brush const& rhs)
				:m_obj(rhs.m_obj)
				,m_owned(false)
			{}
			Brush& operator =(Brush&& rhs)
			{
				if (this != &rhs)
				{
					std::swap(m_obj, rhs.m_obj);
					std::swap(m_owned, rhs.m_owned);
				}
				return *this;
			}
			Brush& operator =(Brush const& rhs)
			{
				if (this != &rhs)
				{
					m_obj = rhs.m_obj;
					m_owned = false;
				}
				return *this;
			}
			operator HBRUSH() const
			{
				return m_obj;
			}
			operator COLORREF() const
			{
				return colour();
			}
			COLORREF colour() const
			{
				LOGBRUSH lb;
				::GetObjectW(m_obj, sizeof(lb), &lb);
				return lb.lbColor;
			}

			static Brush Halftone()
			{
				// Create a 'gray' pattern
				WORD pat[8] = {0x5555, 0xaaaa, 0x5555, 0xaaaa, 0x5555, 0xaaaa, 0x5555, 0xaaaa};
				auto bm_gray = CreateBitmap(8, 8, 1, 1, &pat);
				Throw(bm_gray != nullptr, "Failed to create halftone brush");
				auto bsh = ::CreatePatternBrush(bm_gray);
				::DeleteObject(bm_gray);
				return std::move(Brush(bsh, true));
			}
		};

		// Paint
		struct PaintStruct :PAINTSTRUCT
		{
			HWND m_hwnd;
			PaintStruct(HWND hwnd) :m_hwnd(hwnd) { Throw(::BeginPaint(m_hwnd, this) != nullptr, "BeginPaint failed"); }
			~PaintStruct()                       { Throw(::EndPaint(m_hwnd, this), "EndPaint failed"); }
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

		// A window class is a template from which window instances are created.
		struct WndClassEx :WNDCLASSEXW
		{
			HINSTANCE m_hinst;
			ATOM      m_atom;
			bool      m_unreg; // True to unregister on destruction

			// 'm_unreg' is set to false on copying
			~WndClassEx()
			{
				if (!m_unreg) return;
				::UnregisterClassW(lpszClassName, m_hinst);
			}
			WndClassEx()
				:WNDCLASSEXW()
				,m_hinst()
				,m_atom()
				,m_unreg()
			{
				cbSize = sizeof(WNDCLASSEXW);
			}
			WndClassEx(HINSTANCE hinst) :WndClassEx()
			{
				m_hinst = hinst;
			}
			WndClassEx(wchar_t const* class_name, HINSTANCE hinst = GetModuleHandleW(nullptr)) :WndClassEx(hinst)
			{
				if (class_name == nullptr) return;
				m_atom = ATOM(::GetClassInfoExW(hinst, class_name, this));
			}
			explicit WndClassEx(HWND hwnd) :WndClassEx(WndClassName(hwnd).c_str())
			{}
			WndClassEx(WndClassEx&& rhs)
				:WNDCLASSEXW(rhs)
				,m_hinst(rhs.m_hinst)
				,m_atom(rhs.m_atom)
				,m_unreg(rhs.m_unreg)
			{
				rhs.m_unreg = false;
			}
			WndClassEx(WndClassEx const& rhs)
				:WNDCLASSEXW(rhs)
				,m_hinst(rhs.m_hinst)
				,m_atom(rhs.m_atom)
				,m_unreg(false)
			{}
			WndClassEx& operator = (WndClassEx&& rhs)
			{
				if (this != &rhs)
				{
					static_cast<WNDCLASSEXW&>(*this) = rhs;
					m_hinst = rhs.m_hinst;
					m_atom = rhs.m_atom;
					std::swap(m_unreg, rhs.m_unreg);
				}
				return *this;
			}
			WndClassEx& operator = (WndClassEx const& rhs)
			{
				if (this != &rhs)
				{
					static_cast<WNDCLASSEXW&>(*this) = rhs;
					m_hinst = rhs.m_hinst;
					m_atom = rhs.m_atom;
					m_unreg = false;
				}
				return *this;
			}

			// Register the this window class
			WndClassEx& Register()
			{
				m_atom = ATOM(::RegisterClassExW(this));
				Throw(m_atom != 0, "RegisterClassEx failed");
				m_unreg = true;
				return *this;
			}

			// Return the INTATOM used in CreateWindowEx
			wchar_t const* IntAtom() const
			{
				assert(m_atom != 0);
				return MakeIntAtomW(m_atom);
			}
		};

		#pragma endregion

		#pragma region Menu
		struct MenuItem :MENUITEMINFOW
		{
			#pragma region Enums
			enum ESeparator { Separator };
			enum class EMask :UINT
			{
				None       = 0,
				Bitmap     = MIIM_BITMAP,
				CheckMarks = MIIM_CHECKMARKS,
				Data       = MIIM_DATA,
				FType      = MIIM_FTYPE,
				Id         = MIIM_ID,
				State      = MIIM_STATE,
				String     = MIIM_STRING,
				Submenu    = MIIM_SUBMENU,
				Type       = MIIM_TYPE,
				_bitwise_operators_allowed,
			};
			enum class EType :UINT
			{
				None         = 0,
				Bitmap       = MFT_BITMAP,
				MenuBarBreak = MFT_MENUBARBREAK,
				MenuBreak    = MFT_MENUBREAK,
				OwnerDraw    = MFT_OWNERDRAW,
				RadioCheck   = MFT_RADIOCHECK,
				RightJustify = MFT_RIGHTJUSTIFY,
				RightOrder   = MFT_RIGHTORDER,
				Separator    = MFT_SEPARATOR,
				String       = MFT_STRING,
				_bitwise_operators_allowed,
			};
			enum class EState :UINT
			{
				Default  = MFS_DEFAULT,
				Grayed   = MFS_GRAYED,
				Checked  = MFS_CHECKED,
				Uncheced = MFS_UNCHECKED,
				Enabled  = MFS_ENABLED,
				Disabled = MFS_DISABLED,
				Hilite   = MFS_HILITE,
				Unhilite = MFS_UNHILITE,
				_bitwise_operators_allowed,
			};
			enum class EStockBmp :INT_PTR
			{
				Callback        = (INT_PTR)HBMMENU_CALLBACK       ,
				System          = (INT_PTR)HBMMENU_SYSTEM         ,
				MBar_Restore    = (INT_PTR)HBMMENU_MBAR_RESTORE   ,
				MBar_Minimize   = (INT_PTR)HBMMENU_MBAR_MINIMIZE  ,
				MBar_Close      = (INT_PTR)HBMMENU_MBAR_CLOSE     ,
				MBar_Close_D    = (INT_PTR)HBMMENU_MBAR_CLOSE_D   ,
				MBar_Minimize_D = (INT_PTR)HBMMENU_MBAR_MINIMIZE_D,
				PopUp_Close     = (INT_PTR)HBMMENU_POPUP_CLOSE    ,
				PopUp_Restore   = (INT_PTR)HBMMENU_POPUP_RESTORE  ,
				PopUp_Maximize  = (INT_PTR)HBMMENU_POPUP_MAXIMIZE ,
				PopUp_Minimize  = (INT_PTR)HBMMENU_POPUP_MINIMIZE ,
			};
			#pragma endregion

			MenuItem()
				:MENUITEMINFOW()
			{
				cbSize = sizeof(MENUITEMINFOW);
			}
			MenuItem(ESeparator)
				:MenuItem(EMask::FType, EType::Separator)
			{}
			MenuItem(wchar_t const* text, int id)
				:MenuItem(text, id, MenuItem::EState::Enabled)
			{}
			MenuItem(wchar_t const* text_, int id_, MenuItem::EState state_)
				:MenuItem()
			{
				text(text_).id(id_).state(state_);
			}
			MenuItem(wchar_t const* text_, HMENU submenu_)
				:MenuItem()
			{
				text(text_).id(ID_UNUSED).submenu(submenu_);
			}
			MenuItem(EMask mask, EType type, LPWSTR type_data = nullptr, size_t type_data_size = 0, int id = 0 ,EState state = EState::Default, HMENU submenu = nullptr, HBITMAP bmp = nullptr ,HBITMAP checked = nullptr ,HBITMAP unchecked = nullptr ,void* data = nullptr)
				:MenuItem()
			{
				fMask         = UINT(mask);           // Flags the fields that contain valid data
				fType         = UINT(type);           // used if MIIM_TYPE (4.0) or MIIM_FTYPE (>4.0)
				fState        = UINT(state);          // used if MIIM_STATE
				dwTypeData    = LPWSTR(type_data);    // used if MIIM_TYPE (4.0) or MIIM_STRING (>4.0)
				cch           = UINT(type_data_size); // used if MIIM_TYPE (4.0) or MIIM_STRING (>4.0)
				wID           = id;                   // used if MIIM_ID
				hSubMenu      = submenu;              // used if MIIM_SUBMENU
				hbmpChecked   = checked;              // used if MIIM_CHECKMARKS
				hbmpUnchecked = unchecked;            // used if MIIM_CHECKMARKS
				dwItemData    = ULONG_PTR(data);      // used if MIIM_DATA
				hbmpItem      = bmp;                  // used if MIIM_BITMAP
			}
			MenuItem(MENUITEMINFOW const& mii)
				:MENUITEMINFOW(mii)
			{}

			MenuItem& type(EType ty)                       { fMask |= (UINT)EMask::FType; fType |= (UINT)ty; return *this; }
			MenuItem& text(wchar_t const* t)               { fMask |= (UINT)EMask::String; dwTypeData = (LPWSTR)t; cch = (UINT)wcslen(t); return *this; }
			MenuItem& id(int id_)                          { fMask |= (UINT)EMask::Id; wID = (UINT)id_; return *this; }
			MenuItem& state(EState s)                      { fMask |= (UINT)EMask::State; fState = (UINT)s; return *this; }
			MenuItem& bitmap(HBITMAP bm)                   { fMask |= (UINT)EMask::Bitmap; hbmpItem = bm; return *this; }
			MenuItem& chkmarks(HBITMAP chk, HBITMAP unchk) { fMask |= (UINT)EMask::CheckMarks; hbmpChecked = chk; hbmpUnchecked = unchk; return *this; }
			MenuItem& item_data(void const* data)          { fMask |= (UINT)EMask::Data; dwItemData = (ULONG_PTR)data; return *this; }
			MenuItem& submenu(HMENU m)                     { fMask |= (UINT)EMask::Submenu; hSubMenu = m; return *this; }

			// Out parameters are used by GetMenuItemInfo()
			MenuItem& text_out(wchar_t* buf, size_t sz) { fMask |= (UINT)EMask::Type;  dwTypeData = buf; cch = (UINT)sz; return *this; }
			MenuItem& item_data_out(void* data)         { fMask |= (UINT)EMask::Data;  dwItemData = (ULONG_PTR)data; return *this; }
		};
		struct Menu
		{
			enum class EKind { Strip, Popup };
			using EType = MenuItem::EType;
			using EMask = MenuItem::EMask;
			using EState = MenuItem::EState;
			using ItemList = std::initializer_list<MenuItem>;

			HMENU m_menu;
			bool m_owned;

			// Note: ownership is lost with copying.
			// Controls/Forms don't own menus. Menu ownership is a convenience for callers
			// to automatically destroy menus, almost all other uses should be with non-owned menus.
			// Note: implicit conversion constructors are delibrate
			~Menu()
			{
				if (m_owned)
					DestroyMenu();
			}
			Menu()
				:m_menu(nullptr)
				,m_owned(false)
			{}
			Menu(HMENU menu, bool owned = false)
				:m_menu(menu)
				,m_owned(owned)
			{}
			Menu(int menu_id, HINSTANCE hinst = ::GetModuleHandleW(nullptr))
				:Menu(menu_id != ID_UNUSED ? ::LoadMenuW(hinst, MAKEINTRESOURCEW(menu_id)) : nullptr, false)
			{}
			Menu(EKind type, ItemList const& items = ItemList(), bool owned = false)
				:Menu(type == EKind::Strip ? ::CreateMenu() : type == EKind::Popup ? ::CreatePopupMenu() : nullptr, owned)
			{
				// Construct a menu from a type and a comma separated list of items
				// Allows syntax: Menu(Menu::Strip, {{L"&File",ID_UNUSED}, {L"&Help", ID_HELP}});
				for (auto& item : items)
					Insert(item);
			}

			Menu(Menu&& rhs)
				:Menu()
			{
				std::swap(m_menu, rhs.m_menu);
				std::swap(m_owned, rhs.m_owned);
			}
			Menu(Menu const& rhs)
				:Menu(rhs.m_menu, false)
			{}
			Menu& operator =(Menu&& rhs)
			{
				if (this != &rhs)
				{
					std::swap(m_menu, rhs.m_menu);
					std::swap(m_owned, rhs.m_owned);
				}
				return *this;
			}
			Menu& operator =(Menu const& rhs)
			{
				if (this != &rhs)
				{
					DestroyMenu();
					m_menu = rhs.m_menu;
					m_owned = false;
				}
				return *this;
			}

			// Destroy this menu (if owned)
			void DestroyMenu()
			{
				if (m_owned && m_menu)
					::DestroyMenu(m_menu);

				m_menu = nullptr;
				m_owned = false;
			}

			// Implicit conversion to HMENU
			operator HMENU() const
			{
				return m_menu;
			}

			// Returns the number of menu items in this menu
			size_t Count() const
			{
				assert(m_menu != nullptr);
				return size_t(::GetMenuItemCount(m_menu));
			}

			// Returns the index of a child menu item with the given text
			int IndexByName(wchar_t const* text) const
			{
				// Look for an existing menu called 'text'
				int index = 0;
				wchar_t item[256] = {};
				for (int i = 0, iend = ::GetMenuItemCount(m_menu); i != iend; ++i, ++index)
				{
					// Get the text name of the menu item
					auto len = ::GetMenuStringW(m_menu, UINT(i), item, _countof(item), MF_BYPOSITION);
					if (std::char_traits<wchar_t>::compare(text, item, len) != 0) continue;
					break;
				}
				return index;
			}

			// Insert a menu item at index position 'idx'. Use 'idx == -1' to mean append to the end
			void Insert(MenuItem const& info, int idx = -1)
			{
				assert(m_menu != nullptr);
				auto i = (idx == -1) ? DWORD(Count()) : DWORD(idx);
				//Throw(::InsertMenuW(m_menu, i, MF_BYPOSITION, info.wID, info.dwTypeData), "InsertMenu failed");
				Throw(::InsertMenuItemW(m_menu, i, TRUE, &info), "Insert menu item failed");
			}

			// Set a popup menu by name. If it exists already, then it is replaced, otherwise insert
			void Set(wchar_t const* text, Menu& submenu)
			{
				auto index = IndexByName(text);
				auto info = MenuItem().text(text).submenu(submenu);
				Throw(::SetMenuItemInfoW(m_menu, index, TRUE, &info), "Set menu item failed");
			}
		};
		#pragma endregion

		#pragma region EventHandler

		// Returns an identifier for uniquely id'ing event handlers
		using EventHandlerId = unsigned long long;
		inline EventHandlerId GenerateEventHandlerId()
		{
			static std::atomic_uint s_id = {};
			auto id = s_id.load();
			for (;!s_id.compare_exchange_weak(id, id + 1);) {}
			return id + 1;
		}

		// Place-holder for events that take no arguments. (Makes the templating consistent)
		struct EmptyArgs
		{};

		// Event args used in cancel-able operations
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
			HWND m_hwnd;          // The window being painted
			HDC  m_alternate_hdc; // If non-null, then paint onto this device context
			PaintEventArgs(HWND hwnd, HDC alternate_hdc)
				:m_hwnd(hwnd)
				,m_alternate_hdc(alternate_hdc)
			{}

			// Returns the area the needs painting
			// Using 'erase' == true, causes a WM_ERASEBKGND message to be sent.
			// It's probably better to just fill the area in your paint handler instead
			Rect UpdateRect(BOOL erase = false) const
			{
				Rect rect;
				return ::GetUpdateRect(m_hwnd, &rect, erase) != 0
					? rect : Rect();
			}
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

		// Event args for shown events
		struct ShownEventArgs :EmptyArgs
		{
			bool m_shown;
			int m_reason;
			ShownEventArgs(bool shown, int reason)
				:m_shown(shown)
				,m_reason(reason)
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
			Point     m_point;    // The location of the mouse at the button event (in client space)
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
		// Use:
		//   btn.Click += [&](Button&,EmptyArgs const) {...}
		//   btn.Click += std::bind(&MyDlg::HandleBtn, this, _1, _2);
		template <typename Sender, typename Args> struct EventHandler
		{
			// Note: This isn't thread safe
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
		
		#pragma region MessageLoop

		// An interface for types that need to handle messages from the message
		// loop before TranslateMessage is called. Typically these are dialog windows
		// or windows with keyboard accelerators that need to call 'IsDialogMessage'
		// or 'TranslateAccelerator'
		struct IMessageFilter
		{
			// Implementors should return true to halt processing of the message.
			// Typically, if you're just observing messages as they go past, return false.
			// If you're a dialog return the result of IsDialogMessage()
			// If you're a window with accelerators, return the result of TranslateAccelerator()
			virtual bool TranslateMessage(MSG&)
			{
				return false;
			}
		};

		// Base class and basic implementation of a message loop
		struct MessageLoop :IMessageFilter
		{
			// The collection of message filters filtering msgs in this loop
			std::vector<IMessageFilter*> m_filters;

			virtual ~MessageLoop() {}
			MessageLoop() :m_filters()
			{
				m_filters.push_back(this);
			}

			// Subclasses should replace this method
			virtual int Run()
			{
				MSG msg;
				for (int result; (result = ::GetMessageW(&msg, NULL, 0, 0)) != 0; )
				{
					Throw(result > 0, "GetMessage failed"); // GetMessage returns negative values for errors

					// Pass the message to each filter. The last filter is this message loop which always handles the message.
					for (auto filter : m_filters)
						if (filter->TranslateMessage(msg))
							break;
				}
				return (int)msg.wParam;
			}

			// Add an instance that needs to handle messages before TranslateMessage is called
			virtual void AddMessageFilter(IMessageFilter& filter)
			{
				m_filters.insert(--std::end(m_filters), &filter);
			}

			// Remove a message filter from the chain of filters for this message loop
			virtual void RemoveMessageFilter(IMessageFilter& filter)
			{
				m_filters.erase(std::remove(std::begin(m_filters), std::end(m_filters), &filter), std::end(m_filters));
			}

		private:

			// The message loop is always the last filter in the chain
			bool TranslateMessage(MSG& msg) override
			{
				::TranslateMessage(&msg);
				::DispatchMessageW(&msg);
				return true;
			}
		};

		#pragma endregion

		#pragma region WndRef
		// A helper for referencing a Control* or an HWND.
		// When a Control* is given, the parent is a Control using this framework (preferred).
		// When an HWND is given, this is the case when parenting to a window not using this framework.
		struct WndRef
		{
			Control* m_ctrl;
			HWND     m_hwnd;

			WndRef(nullptr_t)
				:m_ctrl()
				,m_hwnd()
			{}
			WndRef(HWND hwnd)
				:m_ctrl()
				,m_hwnd(hwnd)
			{}
			template <int _ = 0> WndRef(Control* ctrl, int = _) // this is a template so I can keep the definition here
				:m_ctrl(ctrl)
				,m_hwnd(ctrl ? HWND(*ctrl) : HWND(nullptr))
			{}

			operator HWND() const     { return m_hwnd; }
			operator Control*() const { return m_ctrl; }
			operator bool() const = delete; // Don't allow implicit bool because its ambiguous

			bool operator == (nullptr_t) const  { return m_hwnd == nullptr; }
			bool operator != (nullptr_t) const  { return m_hwnd != nullptr; }
			Control const* operator -> () const { return m_ctrl; }
			Control*       operator -> ()       { return m_ctrl; }

			// Returns a window reference for 'hwnd'. Attempts to get the Control* as well.
			static WndRef Lookup(HWND hwnd)
			{
				WndRef ref = hwnd;
				ref.m_ctrl = hwnd != nullptr ? reinterpret_cast<Control*>(::SendMessageW(hwnd, WM_GETCTRLPTR, 0, 0)) : nullptr;
				return ref;
			}
		};
		#pragma endregion

		#pragma region Auto size position
		// Use: e.g. Left|LeftOf|id,
		// Read: left edge of this control, aligned to the left of control with id 'id'
		namespace auto_size_position
		{
			// The mask for auto positioning control bits
			static UINT const AutoPosMask = 0xFF000000;

			// The mask for auto sizing control bits
			static UINT const AutoSizeMask = 0xF0000000;

			// Used as a size value, means expand w,h to match parent
			// Note: CW_USEDEFAULT == 0x80000000
			static UINT const Fill = 0x90000000;
			//static UINT const Auto = 0xA0000000; // too hard...

			// The mask for the control id
			static UINT const IdMask = 0x00FFFFFF;
			static_assert((ID_UNUSED & IdMask) == ID_UNUSED, "");

			// The X,Y coord of the control being positioned
			// Note: CW_USEDEFAULT == 0x80000000
			static UINT const Left = 0x81000000;
			static UINT const Right = 0x82000000;
			static UINT const Centre = 0x83000000;

			// The X coord of the reference control to align to
			static UINT const LeftOf = 0x84000000;
			static UINT const RightOf = 0x88000000;
			static UINT const CentreOf = 0x8C000000;
			static UINT const CentreP = Centre | CentreOf;

			// Handle auto position/size
			// Adjusts x,y,w,h to be positioned and sized related to 'relto'
			// 'relto' is a proxy that provides the dimensions of the parent client area and sibling controls parent space rects.
			// All aligning is done after margins have been added. 'relto' should return bounds that include margins.
			template <typename RelTo> void CalcPosSize(int& x, int& y, int& w, int& h, Rect const& margin, RelTo const& relto)
			{
				// Set the width/height and x/y position
				// 'X' is the x position, 'W' is the width, 'L' is the left margin, 'R' is the right margin
				// 'i' is 0 for the X-axis, 1 for th Y-axis (i.e. Y, height positioning)
				auto auto_size = [=](int& X, int& W, int L, int R, int i)
				{
					auto fill = (W & AutoSizeMask) == Fill;
					if (fill)
					{
						// Get the parent control client area (in parent space, including padding)
						W = relto(0).size(i) - (L+R);
					}
					if (X & AutoPosMask)
					{
						// Get the ref point on the parent. Note, order is important here
						// If the top 4 bits are not '0b1000' then 'X' is just a negative number.
						// Otherwise, the top 8 bits are the auto position bits and the lower 24
						// are the id of the control to position relative to.
						int ref = 0;
						if ((X & 0xF0000000) != 0x80000000)
						{
							// X is a negative number. Position relative to the BR
							auto b = relto(0);
							ref = b.bottomright()[i];
						}
						else if ((X & CentreOf) == CentreOf)
						{
							// Position relative to the centre of 'b' (including margin)
							auto b = relto(X & IdMask);
							ref = b.centre()[i];
						}
						else if ((X & LeftOf) == LeftOf)
						{
							// Position relative to the left edge of 'b' (including margin)
							auto b = relto(X & IdMask);
							ref = b.topleft()[i];
						}
						else if ((X & RightOf) == RightOf)
						{
							// Position relative to the right edge of 'b' (including margin)
							auto b = relto(X & IdMask);
							ref = b.bottomright()[i];
						}

						// Position the control relative to 'ref' including margin
						if ((X & 0xF0000000) != 0x80000000)
						{
							// Position relative to the BR (X is negative)
							X = ref - (W+L+R) + (X+1) + L;
						}
						else if ((X & Left) == Left)
						{
							// If 'fill', fill from X to the right edge
							X = ref + L;
							if (fill) W -= ref;
						}
						else if ((X & Centre) == Centre)
						{
							// If 'fill', fill to left/right edges (ignore X)
							if (fill) X = L;
							else X = ref - (W+L+R)/2 + L;
						}
						else if ((X & Right ) == Right)
						{
							// If 'fill', fill to the left edge
							if (fill) { X = L; W = ref - (L+R); }
							else X = ref - (W+L+R) + L;
						}
					}
					else
					{
						X += L;
					}
				};

				// Auto size in each dimension
				auto_size(x, w, -margin.left, margin.right, 0);
				auto_size(y, h, -margin.top, margin.bottom, 1);
			}
		}
		#pragma endregion

		#pragma region Dialog Template
		// A structure for defining a dialog template
		struct DlgTemplate
		{
			enum { DefW = 640, DefH = 480 };

			#pragma region Auto Size Position
			static UINT const AutoPosMask = auto_size_position::AutoPosMask;
			static UINT const AutoSizeMask = auto_size_position::AutoSizeMask;
			static UINT const Fill = auto_size_position::Fill;

			// The point on this control to position
			static UINT const Left = auto_size_position::Left;
			static UINT const Right = auto_size_position::Right;
			static UINT const Centre = auto_size_position::Centre;
			static UINT const Top = Left;
			static UINT const Bottom = Right;

			// The point of the referenced control to align to
			static UINT const LeftOf = auto_size_position::LeftOf;
			static UINT const RightOf = auto_size_position::RightOf;
			static UINT const CentreOf = auto_size_position::CentreOf;
			static UINT const TopOf = LeftOf;
			static UINT const BottomOf = RightOf;

			static UINT const CentreP = Centre | CentreOf;
			#pragma endregion

			std::vector<byte> m_mem;
			std::vector<size_t> m_item_base;
			bool m_has_menu; // Flag to indicate the dialog will have a menu. Used for auto size/position

			DlgTemplate()
				:m_mem()
				,m_item_base()
			{}

			template <typename TParams = pr::gui::Params>
			DlgTemplate(TParams const& p)
				:m_mem()
				,m_item_base()
				,m_has_menu(p.m_menu != nullptr)  // m_menu just has to be non-null
			{
				// In a standard template for a dialog box, the DLGTEMPLATE structure is always immediately followed
				// by three variable-length arrays that specify the menu, class, and title for the dialog box.
				// When the DS_SETFONT style is specified, these arrays are also followed by a 16-bit value specifying
				// point size and another variable-length array specifying a typeface name. Each array consists of one
				// or more 16-bit elements. The menu, class, title, and font arrays must be aligned on WORD boundaries.

				// Make local copies
				auto x = p.m_x;
				auto y = p.m_y;
				auto w = p.m_w;
				auto h = p.m_h;
				auto style = p.m_style;
				auto style_ex = p.m_style_ex;

				// Auto size to the parent
				auto_size_position::CalcPosSize(x, y, w, h, Rect(), [&](int id) -> Rect
					{
						if (p.m_parent == nullptr) return MinMaxInfo().Bounds();
						if (id != 0) throw std::exception("DlgTemplate can only be positioned related to the screen or owner window");
						return p.m_parent->ClientRect();
					});

				// If 'style' includes DS_SETFONT then windows expects the header to be followed by
				// font data consisting of a 16-bit font size, and unicode font name string
				if (p.m_font_name != nullptr)
					style |= DS_SETFONT;
				else
					style &= ~DS_SETFONT;

				// Add the header
				DLGTEMPLATE hd = {style, style_ex, 0, cast<short>(x), cast<short>(y), cast<short>(w), cast<short>(h)};
				append(m_mem, &hd, sizeof(hd));

				// Immediately following the DLGTEMPLATE structure is a menu array that identifies a menu resource for the dialog box.
				// If the first element of this array is 0x0000, the dialog box has no menu and the array has no other elements.
				// If the first element is 0xFFFF, the array has one additional element that specifies the ordinal value of a menu
				// resource in an executable file. If the first element has any other value, the system treats the array as a
				// null-terminated Unicode string that specifies the name of a menu resource in an executable file.
				AddWord(p.m_menu.m_id != ID_UNUSED ? WORD(MAKEINTRESOURCEW(p.m_menu.m_id)) : 0);

				// Following the menu array is a class array that identifies the window class of the control. If the first element
				// of the array is 0x0000, the system uses the predefined dialog box class for the dialog box and the array has no
				// other elements. If the first element is 0xFFFF, the array has one additional element that specifies the ordinal
				// value of a predefined system window class. If the first element has any other value, the system treats the array
				// as a null-terminated Unicode string that specifies the name of a registered window class.
				AddString(p.wndclassname());

				// Following the class array is a title array that specifies a null-terminated Unicode string that contains the title
				// of the dialog box. If the first element of this array is 0x0000, the dialog box has no title and the array has no other elements.
				AddString(p.m_text);

				// The 16-bit point size value and the typeface array follow the title array, but only if the style member specifies the
				// DS_SETFONT style. The point size value specifies the point size of the font to use for the text in the dialog box and
				// its controls. The typeface array is a null-terminated Unicode string specifying the name of the typeface for the font.
				// When these values are specified, the system creates a font having the specified size and typeface (if possible) and
				// sends a WM_SETFONT message to the dialog box procedure and the control window procedures as it creates the dialog box and controls.
				if (style & DS_SETFONT)
				{
					append(m_mem, &p.m_font_size, sizeof(p.m_font_size));
					AddString(p.m_font_name);
				}

				// Following the DLGTEMPLATE header in a standard dialog box template are one or more DLGITEMTEMPLATE structures that
				// define the dimensions and style of the controls in the dialog box. The cdit member specifies the number of
				// DLGITEMTEMPLATE structures in the template. These DLGITEMTEMPLATE structures must be aligned on *DWORD* boundaries.
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

			// Returns the dialog item by index
			DLGITEMTEMPLATE const& item(size_t idx) const
			{
				if (idx >= m_item_base.size()) throw std::exception("Dialog template item index out of range");
				return *reinterpret_cast<DLGITEMTEMPLATE const*>(&m_mem[m_item_base[idx]]);
			}

			// Add a control to the template
			template <typename TParams = pr::gui::Params>
			DlgTemplate& Add(TParams const& p, WORD creation_data_size_in_bytes = 0, void* creation_data = nullptr)
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

				// Auto size/position
				auto x = p.m_x;
				auto y = p.m_y;
				auto w = p.m_w;
				auto h = p.m_h;
				auto_size_position::CalcPosSize(x, y, w, h, p.m_margin, [this](int id) -> Rect
				{
					if (id == 0)
					{
						auto& h = hdr();
						auto adj = Rect();
						Throw(::AdjustWindowRectEx(&adj, h.style, BOOL(m_has_menu), h.dwExtendedStyle), "AdjustWindowRectEx failed.");
						return Rect(h.x - adj.left, h.y - adj.top, h.x + h.cx - adj.right, h.y + h.cy - adj.bottom);
					}
					for (int i = 0; i != hdr().cdit; ++i)
					{
						auto& itm = this->item(size_t(i));
						if (itm.id != id) continue;

						// this should include the item margin, but it's not available here.
						// This class needs to be changed so that it stores the added items and only
						// generates the memory layout when the template is needed.
						return Rect(itm.x, itm.y, itm.x + itm.cx, itm.y + itm.cy);
					}
					throw std::exception("Sibling control not found");
				});

				// Add a description of the item
				m_item_base.push_back(m_mem.size());
				auto item = DLGITEMTEMPLATE{p.m_style, p.m_style_ex, cast<short>(x), cast<short>(y), cast<short>(w), cast<short>(h), cast<WORD>(p.m_id)};
				append(m_mem, &item, sizeof(item));

				// Immediately following each DLGITEMTEMPLATE structure is a class array that specifies the window class of the control.
				// If the first element of this array is any value other than 0xFFFF, the system treats the array as a null-terminated
				// Unicode string that specifies the name of a registered window class. If the first element is 0xFFFF, the array has
				// one additional element that specifies the ordinal value of a predefined system class. The ordinal can be one of the following atom values.
				auto wcn = p.wndclassname();
				enum class EStdCtrlType :WORD { None = 0, BUTTON = 0x0080, EDIT = 0x0081, STATIC = 0x0082, LISTBOX = 0x0083, SCROLLBAR = 0x0084, COMBOBOX = 0x0085};
				auto ctrl_atom = EStdCtrlType::None;
				if      (wcn == nullptr) {}
				else if (wcscmp(wcn, L"BUTTON"   ) == 0) ctrl_atom = EStdCtrlType::BUTTON;
				else if (wcscmp(wcn, L"EDIT"     ) == 0) ctrl_atom = EStdCtrlType::EDIT;
				else if (wcscmp(wcn, L"STATIC"   ) == 0) ctrl_atom = EStdCtrlType::STATIC;
				else if (wcscmp(wcn, L"LISTBOX"  ) == 0) ctrl_atom = EStdCtrlType::LISTBOX;
				else if (wcscmp(wcn, L"SCROLLBAR") == 0) ctrl_atom = EStdCtrlType::SCROLLBAR;
				else if (wcscmp(wcn, L"COMBOBOX" ) == 0) ctrl_atom = EStdCtrlType::COMBOBOX;
				if (ctrl_atom != EStdCtrlType::None)
					AddWord(WORD(ctrl_atom));
				else
					AddString(wcn);

				// Following the class array is a title array that contains the initial text or resource identifier of the control.
				// If the first element of this array is 0xFFFF, the array has one additional element that specifies an ordinal value
				// of a resource, such as an icon, in an executable file. You can use a resource identifier for controls, such as
				// static icon controls, that load and display an icon or other resource rather than text. If the first element is any
				// value other than 0xFFFF, the system treats the array as a null-terminated Unicode string that specifies the initial text.
				AddString(p.m_text);

				// The creation data array begins at the next WORD boundary after the title array. This creation data can be of any size
				// and format. If the first word of the creation data array is nonzero, it indicates the size, in bytes, of the creation
				// data (including the size word). The control's window procedure must be able to interpret the data. When the system
				// creates the control, it passes a pointer to this data in the lParam parameter of the WM_CREATE message that it sends to the control.
				creation_data_size_in_bytes += (creation_data_size_in_bytes != 0) * sizeof(creation_data_size_in_bytes); // include the size of 'creation_data_size_in_bytes'
				append(m_mem, &creation_data_size_in_bytes, sizeof(creation_data_size_in_bytes));
				if (creation_data_size_in_bytes != 0)
					append(m_mem, creation_data, creation_data_size_in_bytes);

				return *this;
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

		#pragma region CreateParams
		// Data used to create Controls/Forms
		struct Params
		{
			char const*        m_name;
			ECreate            m_create;
			wchar_t const*     m_wcn;
			WndClassEx const*  m_wci;
			wchar_t const*     m_text;
			int                m_x, m_y; // Negative values for 'x,y' mean relative to the right,bottom of the parent. Remember auto position Left|RightOf, etc..
			int                m_w, m_h; // Can use Control::Fill, etc
			int                m_id;
			WndRef             m_parent;
			EAnchor            m_anchor;
			EDock              m_dock;
			DWORD              m_style;
			DWORD              m_style_ex;
			IdOrHandle<HMENU>  m_menu;
			IdOrHandle<HACCEL> m_accel;
			IdOrHandle<HICON>  m_icon_bg;
			IdOrHandle<HICON>  m_icon_sm;
			COLORREF           m_color_fore;
			COLORREF           m_color_back;
			EStartPosition     m_start_pos;
			bool               m_top_level;      // True for non-mdi forms, false for WS_CHILD controls
			bool               m_main_wnd;       // Main application window, closing it exits the application
			bool               m_dlg_behaviour;
			bool               m_hide_on_close;
			bool               m_pin_window;
			HINSTANCE          m_hinst;
			void*              m_init_param;
			MessageLoop*       m_msg_loop;
			DlgTemplate const* m_templ;
			wchar_t const*     m_font_name;
			WORD               m_font_size;
			Rect               m_margin;     // Stored as an addition to the bounding rect (i.e. -ve l,t)
			Rect               m_padding;    // Stored as an addition to the bounding rect (i.e. -ve l,t)

			Params(ECreate create, int w, int h, DWORD style, DWORD style_ex, bool top_level, bool dlg_behaviour)
				:m_name         ()
				,m_create       (create)
				,m_wcn          ()
				,m_wci          ()
				,m_text         ()
				,m_x            (0)
				,m_y            (0)
				,m_w            (w)
				,m_h            (h)
				,m_id           (ID_UNUSED)
				,m_parent       (nullptr)
				,m_anchor       (EAnchor::TopLeft)
				,m_dock         (EDock::None)
				,m_style        (style)
				,m_style_ex     (style_ex)
				,m_menu         ()
				,m_accel        ()
				,m_icon_bg      ()
				,m_icon_sm      ()
				,m_color_fore   (CLR_INVALID)
				,m_color_back   (CLR_INVALID)
				,m_start_pos    (EStartPosition::Default)
				,m_top_level    (top_level)
				,m_main_wnd     (false)
				,m_dlg_behaviour(dlg_behaviour)
				,m_hide_on_close(false)
				,m_pin_window   (false)
				,m_hinst        (::GetModuleHandleW(nullptr))
				,m_init_param   ()
				,m_msg_loop     ()
				,m_templ        ()
				,m_font_name    (L"MS Shell Dlg")
				,m_font_size    (8)
				,m_margin       (-2, -2, 2, 2)
				,m_padding      ()
			{}

			Params& name         (char const* n)               { m_name          = n;                   return *this; }
			Params& create       (ECreate c)                   { m_create        = c;                   return *this; }
			Params& wndclass     (wchar_t const* wcn)          { m_wcn           = wcn;                 return *this; }
			Params& wndclass     (WndClassEx const& wci)       { m_wci           = &wci;                return *this; }
			Params& text         (wchar_t const* t)            { m_text          = t;                   return *this; }
			Params& title        (wchar_t const* t)            { m_text          = t;                   return *this; }
			Params& xy           (int x, int y)                { m_x             = x; m_y = y;          return *this; }
			Params& wh           (int w, int h)                { m_w             = w; m_h = h;          return *this; }
			Params& id           (int id_)                     { m_id            = id_;                 return *this; }
			Params& parent       (WndRef p)                    { m_parent        = p;                   return *this; }
			Params& anchor       (EAnchor a)                   { m_anchor        = a;                   return *this; }
			Params& dock         (EDock d)                     { m_dock          = d;                   return *this; }
			Params& style        (DWORD s)                     { m_style         = s;                   return *this; }
			Params& style_ex     (DWORD s)                     { m_style_ex      = s;                   return *this; }
			Params& menu         (Menu::ItemList const& items) { m_menu.m_handle = Menu(Menu::EKind::Strip, items); return *this; }
			Params& menu         (IdOrHandle<HMENU> m)         { m_menu          = m;                   return *this; }
			Params& accel        (IdOrHandle<HACCEL> a)        { m_accel         = a;                   return *this; }
			Params& icon         (IdOrHandle<HICON> i)         { m_icon_sm = m_icon_bg = i;             return *this; }
			Params& icon_bg      (IdOrHandle<HICON> i)         { m_icon_bg       = i;                   return *this; }
			Params& icon_sm      (IdOrHandle<HICON> i)         { m_icon_sm       = i;                   return *this; }
			Params& fr_col       (COLORREF c)                  { m_color_fore    = c;                   return *this; }
			Params& bk_col       (COLORREF c)                  { m_color_back    = c;                   return *this; }
			Params& start_pos    (EStartPosition pos)          { m_start_pos     = pos;                 return *this; }
			Params& top_level    (bool tl)                     { m_top_level     = tl;                  return *this; }
			Params& mdi_child    (bool mdi)                    { m_top_level     = !mdi;                return *this; }
			Params& main_wnd     (bool mw)                     { m_main_wnd      = mw;                  return *this; }
			Params& dlg          (bool d)                      { m_dlg_behaviour = d;                   return *this; }
			Params& hide_on_close(bool h)                      { m_hide_on_close = h;                   return *this; }
			Params& pin_window   (bool p)                      { m_pin_window    = p;                   return *this; }
			Params& hinst        (HINSTANCE i)                 { m_hinst         = i;                   return *this; }
			Params& init_param   (void* ip)                    { m_init_param    = ip;                  return *this; }
			Params& msg_loop     (MessageLoop* ml)             { m_msg_loop      = ml;                  return *this; }
			Params& templ        (DlgTemplate const& t)        { m_templ = t.valid() ? &t : nullptr;    return *this; }
			Params& font_name    (wchar_t const* fn)           { m_font_name     = fn;                  return *this; }
			Params& font_size    (WORD fs)                     { m_font_size     = fs;                  return *this; }
			Params& margin       (int m)                       { m_margin        = Rect(-m,-m,m,m);     return *this; }
			Params& margin       (int lr, int tb)              { m_margin        = Rect(-lr,-tb,lr,tb); return *this; }
			Params& margin       (int l, int t, int r, int b)  { m_margin        = Rect(-l,-t,r,b);     return *this; }
			Params& padding      (int p)                       { m_padding       = Rect(p,p,-p,-p);     return *this; }
			Params& padding      (int lr, int tb)              { m_padding       = Rect(lr,tb,-lr,-tb); return *this; }
			Params& padding      (int l, int t, int r, int b)  { m_padding       = Rect(l,t,-r,-b);     return *this; }
			Params& border       ()                            { m_style         |= WS_BORDER;          return *this; }

			// Return the debugging name
			char const* name() const
			{
				return m_name ? m_name : "";
			}

			// True if the options say "create"
			bool create() const
			{
				return m_create == ECreate::Create || (m_create == ECreate::Auto && m_parent != nullptr);
			}
				
			// Get the menu handle from 'm_menu' or 'm_menu_id' if the former is null
			HMENU menu() const
			{
				return
					m_menu.m_handle != nullptr ? m_menu.m_handle :
					m_menu.m_id != ID_UNUSED ? ::LoadMenuW(m_hinst, MAKEINTRESOURCEW(m_menu.m_id)) : nullptr;
			}

			// Get the accelerators from 'm_accel' or 'm_accel_id', whichever is valid
			HACCEL accel() const
			{
				return
					m_accel.m_handle != nullptr ? m_accel.m_handle :
					m_accel.m_id != ID_UNUSED ? ::LoadAcceleratorsW(m_hinst, MAKEINTRESOURCEW(m_accel.m_id)) : nullptr;
			}

			// Get the icon
			HICON icon_bg() const
			{
				auto sz = ::GetSystemMetrics(SM_CXICON);
				return
					m_icon_bg.m_handle != nullptr ? m_icon_bg.m_handle :
					m_icon_bg.m_id != ID_UNUSED ? (HICON)::LoadImageW(m_hinst, MAKEINTRESOURCEW(m_icon_bg.m_id), IMAGE_ICON, sz, sz, LR_DEFAULTCOLOR) : nullptr;
			}
			HICON icon_sm() const
			{
				auto sz = ::GetSystemMetrics(SM_CXSMICON);
				return
					m_icon_sm.m_handle != nullptr ? m_icon_sm.m_handle :
					m_icon_sm.m_id != ID_UNUSED ? (HICON)::LoadImageW(m_hinst, MAKEINTRESOURCEW(m_icon_sm.m_id), IMAGE_ICON, sz, sz, LR_DEFAULTCOLOR) : nullptr;
			}

			// Return the fore colour brush
			Brush fore_color() const
			{
				return std::move(m_color_fore != CLR_INVALID ? Brush(m_color_fore) : Brush());
			}

			// Return the back colour brush
			Brush back_color() const
			{
				return std::move(m_color_back != CLR_INVALID ? Brush(m_color_back) : Brush());
			}

			// Get the dialog template, if given or a default instance
			DlgTemplate const& templ() const
			{
				static DlgTemplate null;
				return m_templ != nullptr ? *m_templ : null;
			}

			// Get the window class info, if given or a default instance
			WndClassEx wci() const
			{
				if (m_wci != nullptr) return *m_wci;
				if (m_wcn != nullptr) return WndClassEx(m_wcn, m_hinst);
				return WndClassEx();
			}

			// Return the window class name from 'm_wci' or 'm_wcn', 'm_wci' preferred.
			wchar_t const* wndclassname() const
			{
				return m_wci != nullptr ? m_wci->lpszClassName : m_wcn;
			}

			// Return the window class ATOM
			wchar_t const* atom() const
			{
				return m_wci != nullptr ? m_wci->IntAtom() : m_wcn;
			}
		};

		// Parameters for creating controls or WS_CHILD windows
		struct CtrlParams :Params
		{
			CtrlParams() :Params(ECreate::Auto, 50, 50, DefaultControlStyle, DefaultControlStyleEx, false, false) {}
			CtrlParams(Params const& p) :Params(p) {} 
		};

		// Parameters for creating modal dialogs
		struct DlgParams :Params
		{
			DlgParams() :Params(ECreate::Defer, 640, 480, DefaultDialogStyle, DefaultDialogStyleEx, true, true) {}
			DlgParams(Params const& p) :Params(p) {}
		};

		// Parameters for creating modal dialogs
		struct ModelessParams :Params
		{
			ModelessParams() :Params(ECreate::Auto, 640, 480, DefaultDialogStyle, DefaultDialogStyleEx, true, true) {}
			ModelessParams(Params const& p) :Params(p) {}
		};

		// Parameters for creating forms
		struct FormParams :Params
		{
			FormParams() :Params(ECreate::Create, 800, 600, DefaultFormStyle, DefaultFormStyleEx, true, false) {}
			FormParams(Params const& p) :Params(p) {}
		};

		#pragma endregion

		#pragma region Control
		// Base class for all windows/controls
		struct Control
		{
			// These are here to import the types within Control
			using EAnchor  = pr::gui::EAnchor;
			using EDock    = pr::gui::EDock;
			using Controls = std::vector<Control*>;

			#pragma region Auto Size Position
			static UINT const AutoPosMask  = auto_size_position::AutoPosMask;
			static UINT const AutoSizeMask = auto_size_position::AutoSizeMask;
			static UINT const Fill         = auto_size_position::Fill;

			// The point on this control to position
			static UINT const Left   = auto_size_position::Left;
			static UINT const Right  = auto_size_position::Right;
			static UINT const Centre = auto_size_position::Centre;
			static UINT const Top    = Left;
			static UINT const Bottom = Right;

			// The point of the referenced control to align to
			static UINT const LeftOf   = auto_size_position::LeftOf;
			static UINT const RightOf  = auto_size_position::RightOf;
			static UINT const CentreOf = auto_size_position::CentreOf;
			static UINT const TopOf    = LeftOf;
			static UINT const BottomOf = RightOf;

			static UINT const CentreP = Centre | CentreOf;
			static UINT const IdMask = auto_size_position::IdMask;
			#pragma endregion

		protected:

			HWND                m_hwnd;         // Window handle for the control
			int                 m_id;           // Dialog control id, used to detect windows messages for this control
			std::string         m_name;         // Debugging name for the control
			Control*            m_parent;       // The parent that contains this control
			Controls            m_child;        // The controls nested with this control
			EAnchor             m_anchor;       // How the control resizes with it's parent
			EDock               m_dock;         // Dock style for the control
			Rect                m_margin;       // The control margin
			Rect                m_padding;      // The control padding
			Rect                m_pos_offset;   // Distances from this control to the edges of the parent client area
			bool                m_pos_ofs_save; // Enable/Disables the saving of the position offset when the control is moved
			MinMaxInfo          m_min_max_info; // Minimum/Maximum window size/position
			Brush               m_colour_fore;  // Foreground colour
			Brush               m_colour_back;  // Background colour
			LONG                m_down_at[4];   // Button down timestamp
			bool                m_top_level;    // True if this control is a top level control (typically a form)
			bool                m_handle_only;  // True if this object does not own 'm_hwnd'
			HBITMAP             m_dbl_buffer;   // Non-null if the control is double buffered
			WndClassEx          m_wci;          // The window class info for this control
			ATL::CStdCallThunk  m_thunk;        // WndProc thunk, turns a __stdcall into a __thiscall
			WNDPROC             m_oldproc;      // The window class default wndproc function
			std::thread::id     m_thread_id;    // The thread that this control was created on

			#pragma region Window Class
			// Register the window class for 'WndType'
			// Custom window types need to call this before using Create() or WndClassInfo()
			template <typename WndType> static WndClassEx const& RegisterWndClass(HINSTANCE hinst =  GetModuleHandleW(nullptr))
			{
				// Register on initialisation
				static WndClassEx wc = [=]
					{
						// Get the window class name
						static wchar_t auto_class_name[64];
						auto class_name = WndType::WndClassName() ? WndType::WndClassName()
							: (_swprintf_c(auto_class_name, _countof(auto_class_name), L"wingui::%p", &wc), &auto_class_name[0]);

						// See if the wndclass is already registered
						WndClassEx wc(class_name, hinst);
						if (wc.m_atom != 0)
							return std::move(wc);

						// Register the window class
						wc.cbSize        = sizeof(WNDCLASSEXW);
						wc.style         = WndType::WndClassStyle();
						wc.cbClsExtra    = 0;
						wc.cbWndExtra    = 0;
						wc.hInstance     = hinst;
						wc.hIcon         = WndType::WndIcon(hinst, true);
						wc.hIconSm       = WndType::WndIcon(hinst, false);
						wc.hCursor       = WndType::WndCursor(hinst);
						wc.hbrBackground = WndType::WndBackground();
						wc.lpszMenuName  = WndType::WndMenu();
						wc.lpfnWndProc   = WndType::WndProcPtr();
						wc.lpszClassName = class_name;
						return std::move(wc.Register());
					}();
				return wc;
			}

			// These methods are provided so that windows that inherit from ControlT
			// can use WndClassInfo<MyControl> specialising just parts of the wndclass.
			static wchar_t const* WndClassName()
			{
				// Returning null causes a name to be automatically generated
				return nullptr;
			}
			static int WndClassStyle()
			{
				// Don't include CS_HREDRAW and CS_VREDRAW, these just add unnecessary redraws
				return CS_DBLCLKS;
			}
			static HICON WndIcon(HINSTANCE hinst, bool large)
			{
				(void)hinst,large;
				//::LoadIcon(hinst, MAKEINTRESOURCE(IDI_ICON));
				//::LoadIcon(hinst, MAKEINTRESOURCE(IDI_SMALL));
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
				// Don't return null by default, subclasses can return S_FALSE
				// from WM_ERASEBKGND to not erase the background. Most controls
				// use the background brush.
				return ::GetSysColorBrush(COLOR_3DFACE);
			}
			static wchar_t const* WndMenu()
			{
				// Typically you might return: MAKEINTRESOURCE(IDM_MENU);
				return nullptr; 
			}
			static WNDPROC WndProcPtr()
			{
				return &InitWndProc;
			}
			#pragma endregion

			// Window creation initialisation parameter wrapper
			struct InitParam
			{
				Control* m_this;
				void* m_lparam;
				InitParam(Control* this_, void* lparam) :m_this(this_) ,m_lparam(lparam) {}
			};

			// The initial wndproc function used in forms, dialogs, and custom controls.
			static LRESULT __stdcall InitWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				//WndProcDebug(hwnd, message, wparam, lparam);
				if (message == WM_NCCREATE)
				{
					auto init = reinterpret_cast<InitParam*>(reinterpret_cast<CREATESTRUCT*>(lparam)->lpCreateParams);

					// Set the wndproc to the default before replacing it with the thunk in Attach()
					assert((WNDPROC)::GetWindowLongPtrW(hwnd, GWLP_WNDPROC) == &InitWndProc);
					::SetWindowLongPtrW(hwnd, GWLP_WNDPROC, LONG_PTR(&::DefWindowProcW));
					init->m_this->Attach(hwnd);
					return init->m_this->WndProc(message, wparam, lparam);
				}
				if (message == WM_INITDIALOG)
				{
					auto init = reinterpret_cast<InitParam*>(lparam);

					// The DWLP_DLGPROC is the user wndproc supplied in CreateDialog.
					// GWLP_WNDPROC is an internal dialog wndproc (user32::_DefDlgProc != DefDlgProc)
					// 'user32::_DefDlgProc' calls the user DLGPROC which, on returning FALSE, then
					// handles the message internally (DefWindowProc is never called)
					// Restore 'DWLP_DLGPROC' to the default (nullptr) before replacing it with the thunk
					assert((WNDPROC)::GetWindowLongPtrW(hwnd, DWLP_DLGPROC) == &InitWndProc);
					::SetWindowLongPtrW(hwnd, DWLP_DLGPROC, LONG_PTR(&::DefDlgProcW));
					init->m_this->Attach(hwnd);
					return init->m_this->WndProc(message, wparam, LPARAM(init->m_lparam));
				}
				return ::DefWindowProcW(hwnd, message, wparam, lparam);
			}
			static LRESULT __stdcall StaticWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
			{
				// 'm_thunk' causes 'hwnd' to actually be the pointer to the control rather than the hwnd
				auto& ctrl = *reinterpret_cast<Control*>(hwnd);
				assert(ctrl.m_hwnd != nullptr && "Message received for destructed control");
				return ctrl.WndProc(message, wparam, lparam);
			}
			LRESULT DefWndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				if (m_oldproc == &::DefDlgProcW) return FALSE;
				if (m_oldproc != nullptr) return ::CallWindowProcW(m_oldproc, m_hwnd, message, wparam, lparam);
				return ::DefWindowProcW(m_hwnd, message, wparam, lparam);
			}

			// Handy debugging method for displaying WM_MESSAGES
			// Call with 'hwnd' == 0, message = 0/1 to disable/enable trace
			#define PR_WNDPROCDEBUG 1
			#if PR_WNDPROCDEBUG
			static void WndProcDebug(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, char const* name = nullptr)
			{
				if (true /*&& name != nullptr && strncmp(name,"about",5) == 0 && message == WM_CTLCOLORDLG*/)
				{
					//auto out = [](char const* s) { OutputDebugStringA(s); };
					auto out = [](char const* s) { std::ofstream("P:\\dump\\wingui.log", std::ofstream::app).write(s,strlen(s)); };

					// Tracing, allows messages to be turned on/off for a short period of time
					static bool trace = true;
					if (hwnd == nullptr)
					{
						trace = message != 0;
						if (trace) out("\n\n**********************************************\n");
						return;
					}

					// Display the message
					static int msg_idx = 0; ++msg_idx;
					auto m = pr::gui::DebugMessage(hwnd, message, wparam, lparam);
					if (*m)
					{
						for (int i = 1; i < wnd_proc_nest(); ++i) out("\t");
						out(pr::FmtX<struct X, 256, char>("%5d|%s|%s\n", msg_idx, name, m));
					}
					if (msg_idx == 0) _CrtDbgBreak();
				}
			}
			static int& wnd_proc_nest()
			{
				static int s_wnd_proc_nest = 0;
				return s_wnd_proc_nest;
			}
			#else
			static void WndProcDebug(HWND , UINT , WPARAM , LPARAM , char const* = nullptr) {}
			#endif

			// This method is the window procedure for this control. 'ProcessWindowMessage' is
			// used to process messages sent to the parent window that contains this control.
			// WndProc is called by windows, Forms forward messages to their child controls using 'ProcessWindowMessage'
			virtual LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam)
			{
				//WndProcDebug(m_hwnd, message, wparam, lparam, pr::FmtS("%s WndProc: ",m_name.c_str()));
				switch (message)
				{
				case WM_GETCTRLPTR:
					#pragma region
					{
						return LRESULT(this);
					}
					#pragma endregion
				case WM_CREATE:
					#pragma region
					{
						break;
					}
					#pragma endregion
				case WM_DESTROY:
					#pragma region
					{
						Detach();
						break;
					}
					#pragma endregion
				case WM_ACTIVATE:
					#pragma region
					{
						UpdateWindow(*this);
						break;
					}
					#pragma endregion
				case WM_ERASEBKGND:
					#pragma region
					{
						// Allow subclasses to handle erase background (which might be to do nothing except return true)
						if (OnEraseBkGnd(EmptyArgs()))
							return S_FALSE;

						// If double buffering is enabled, don't do anything in WM_ERASEBKGND
						if (DoubleBuffered())
							return S_FALSE;

						// If the background colour has been set, fill the client area with it
						if (m_colour_back != nullptr)
						{
							auto hdc = (HDC)wparam;
							auto rect = Rect{};
							if (GetUpdateRect(m_hwnd, &rect, FALSE))
								::FillRect(hdc, &rect, m_colour_back);

							//// Use a trick. By using the ETO_OPAQUE flag in the call to ExtTextOut, and supplying a
							//// zero-length string to display, you can quickly and easily fill a rectangular
							//// area using the current background colour.
							//auto rect = ClientRect();
							//::SetBkColor(hdc, m_colour_back);
							//::SetDCBrushColor(hdc, m_colour_back);
							//::ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &rect, L"", 0, 0);
							return S_FALSE;
						}

						break;
					}
					#pragma endregion
				case WM_PAINT:
					#pragma region
					{
						// Notes:
						//  Only create a pr::gui::PaintStruct if you intend to do the painting yourself,
						//  otherwise DefWndProc will do it (i.e. most controls are drawn by DefWndProc).
						//  The update rect in the paint args is the area needing painting.
						//  Typical behaviour is to create a PaintStruct, however you can not do this and
						//  use this sequence instead:
						//    ::GetUpdateRect(*this, &r, FALSE); <- (TRUE sends the WM_ERASEBKGND if needed)
						//    Draw(); <- do your drawing
						//    Validate(&r); <- tell windows the update rect has been updated,
						//  Non-client window parts are drawn in DefWndProc

						// The alternative DC to draw into
						auto alt_dc = HDC(wparam);

						// If double buffering is enabled and we're not drawing to
						// an alternate device (e.g. printer), draw into the double buffer.
						if (DoubleBuffered() && alt_dc == nullptr)
						{
							auto dc = ClientDC(m_hwnd);
							auto client_rect = ClientRect();
							MemDC mem(dc, client_rect, m_dbl_buffer);

							// Fill with the window background colour
							auto bsh =
								m_colour_back ? (HBRUSH)m_colour_back :
								m_wci.hbrBackground ? m_wci.hbrBackground :
								::GetSysColorBrush(DC_BRUSH);
							::FillRect(mem.m_hdc, &mem.m_rect, bsh);

							// Render the window into the memory DC
							if (!OnPaint(PaintEventArgs(m_hwnd, mem.m_hdc)))
								DefWndProc(WM_PRINTCLIENT, (WPARAM)mem.m_hdc, LPARAM(PRF_CHECKVISIBLE|PRF_NONCLIENT|PRF_CLIENT));

							// Bitblt to the screen
							Throw(::BitBlt(dc, 0, 0, client_rect.width(), client_rect.height(), mem.m_hdc, 0, 0, SRCCOPY), "Bitblt failed");

							// Clear the update rect
							Validate();
							return S_OK;
						}
						else
						{
							if (OnPaint(PaintEventArgs(m_hwnd, alt_dc)))
								return S_OK;
						}
						break;
					}
					#pragma endregion
				case WM_WINDOWPOSCHANGING:
				case WM_WINDOWPOSCHANGED:
					#pragma region
					{
						auto& wp = *reinterpret_cast<WindowPos*>(lparam);
						auto before = message == WM_WINDOWPOSCHANGING;
						
						// Recreate the double buffer bitmap at the new size
						if (!before && DoubleBuffered())
							DoubleBuffered(true);

						OnWindowPosChange(SizeEventArgs(wp, before));
						break;
					}
					#pragma endregion
				case WM_SHOWWINDOW:
					#pragma region
					{
						auto shown = wparam != 0;
						auto reason = (int)lparam;
						OnShown(ShownEventArgs(shown, reason));
						break;
					}
					#pragma endregion
				case WM_GETMINMAXINFO:
					#pragma region
					{
						auto& a = *reinterpret_cast<MinMaxInfo*>(lparam);
						auto& b = m_min_max_info;
						if ((b.m_mask & MinMaxInfo::EMask::MaxSize     ) != 0) a.ptMaxSize      = b.ptMaxSize     ; else b.ptMaxSize      = a.ptMaxSize     ;
						if ((b.m_mask & MinMaxInfo::EMask::MaxPosition ) != 0) a.ptMaxPosition  = b.ptMaxPosition ; else b.ptMaxPosition  = a.ptMaxPosition ;
						if ((b.m_mask & MinMaxInfo::EMask::MinTrackSize) != 0) a.ptMinTrackSize = b.ptMinTrackSize; else b.ptMinTrackSize = a.ptMinTrackSize;
						if ((b.m_mask & MinMaxInfo::EMask::MaxTrackSize) != 0) a.ptMaxTrackSize = b.ptMaxTrackSize; else b.ptMaxTrackSize = a.ptMaxTrackSize;
						break;
					}
					#pragma endregion
				case WM_KEYDOWN:
				case WM_KEYUP:
					#pragma region
					{
						auto vk_key = UINT(wparam);
						auto repeats = UINT(lparam & 0xFFFF);
						auto flags = UINT((lparam & 0xFFFF0000) >> 16);
						if (OnKey(KeyEventArgs(vk_key, message == WM_KEYDOWN, repeats, flags)))
							return true;
						break;
					}
					#pragma endregion
				case WM_LBUTTONDOWN:
				case WM_RBUTTONDOWN:
				case WM_MBUTTONDOWN:
				case WM_XBUTTONDOWN:
				case WM_LBUTTONUP:
				case WM_RBUTTONUP:
				case WM_MBUTTONUP:
				case WM_XBUTTONUP:
					#pragma region
					{
						Point pt(lparam);
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
					#pragma endregion
				case WM_MOUSEWHEEL:
					#pragma region
					{
						auto delta = GET_WHEEL_DELTA_WPARAM(wparam);
						auto pt = Point(lparam);
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						if (OnMouseWheel(MouseWheelArgs(delta, pt, keystate)))
							return true;
						break;
					}
					#pragma endregion
				case WM_MOUSEMOVE:
					#pragma region
					{
						auto pt = Point(lparam);
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						OnMouseMove(MouseEventArgs(keystate, false, pt, keystate));
						break;
					}
					#pragma endregion
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
				// Default handling of parent window messages for all controls (including forms)
				// Sub-classed controls should override this method.
				// For Controls:
				//   - This method is called for every message received by the top-level window.
				//   - When you're handling messages, think "is this a message the window receives or the control receives"
				// For Forms:
				//   - WndProc cannot be sub-classed, this method *is* the WndProc for the Form.
				//   - Forms can be parented to other forms, remember to check 'm_hwnd == hwnd'
				//     to detect messages for the form as opposed to messages for parent forms.

				// Remember:
				//   - This control is not necessarily a child control, it can be the Form using
				//     the default implementation, or an owned form.

				//WndProcDebug(hwnd, message, wparam, lparam, pr::FmtS("%s ProcWinMsg: ",m_name.c_str()));
				switch (message)
				{
				case WM_INITDIALOG:
					#pragma region
					{
						// When the parent dialog is initialising, attach this control to the dialog item
						if (m_id != ID_UNUSED)
						{
							Attach(::GetDlgItem(hwnd, m_id));
							RecordPosOffset();
						}
						return ForwardToChildren(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_DESTROY:
					#pragma region
					{
						// Notify children of the WM_DESTROY before destroying this window so that destruction
						// occurs from leaves to root of the control tree
						if (ForwardToChildren(hwnd, message, wparam, lparam, result))
							return true;

						// Parent window is being destroy, destroy this window to
						if (hwnd != m_hwnd)
							::DestroyWindow(m_hwnd);

						// Allow WM_DESTROY to be passed to the parent window's WndProc
						return false;
					}
					#pragma endregion
				case WM_WINDOWPOSCHANGING:
					#pragma region
					{
						// The parent window is about to resize.
						// Resizing this window will cause WM_WINDOWPOSCHANGING/ED to be sent to this
						// window's WndProc which then calls OnWindowPosChange.
						auto& new_size = *(WindowPos*)lparam;

						// If the parent window is actually resizing (don't care about anything else)
						if (m_parent != nullptr && (new_size.flags & SWP_NOSIZE) == 0)
						{
							// Get the new size of the parent's client area.
							Rect rect;
							if (new_size.hwnd == m_parent->m_hwnd)
							{
								// If the window being resized is our immediate parent, then compare
								// the current parent bounds to the new size to figure out how
								// the client area will be changed.
								auto b = m_parent->ParentRect();
								auto c = m_parent->ClientRect();
								rect = Rect(c.left, c.top, c.right + (new_size.cx - b.width()), c.bottom + (new_size.cy - b.height()));
							}
							else
							{
								// If our parent is not the window being resized, we can assume our
								// parent already has the correct new size
								rect = m_parent->ClientRect();
							}
							ResizeToParent(rect);
						}

						return ForwardToChildren(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_TIMER:
					#pragma region
					{
						// Timer event, forwarded to all child controls
						auto event_id = UINT_PTR(wparam);
						OnTimer(TimerEventArgs(event_id));
						return ForwardToChildren(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_CTLCOLORSTATIC:
				case WM_CTLCOLORBTN:
				case WM_CTLCOLOREDIT:
				case WM_CTLCOLORLISTBOX:
				case WM_CTLCOLORSCROLLBAR:
					#pragma region
					{
						// This is a request to set the foreground and background colour in the DC
						// for the specified control. If this is that control then set the fore and
						// back colours in the given DC.
						if (HWND(lparam) == m_hwnd)
						{
							auto hdc = HDC(wparam); // The DC to set colours in

							// If we have a fore colour, set it, otherwise leave it as the default
							if (m_colour_fore != nullptr)
								::SetTextColor(hdc, m_colour_fore);

							// If we have a background colour, set it and return the background brush
							if (m_colour_back != nullptr)
							{
								::SetBkColor(hdc, m_colour_back);
								::SetDCBrushColor(hdc, m_colour_back);
								result =  LRESULT(static_cast<HBRUSH>(m_colour_back));
								return true;
							}

							// If we don't have a background brush, let the WndProc handle it
							return false;
						}

						// Not for this control, forward to children
						return ForwardToChildren(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_DROPFILES:
					#pragma region
					{
						// Files dropped onto this control
						auto drop_info = HDROP(wparam);
						DropFilesEventArgs drop(drop_info);

						// Read the file paths of the dropped files
						int i = 0;
						drop.m_filepaths.resize(::DragQueryFileW(drop_info, 0xFFFFFFFF, nullptr, 0));
						for (auto& path : drop.m_filepaths)
						{
							path.resize(::DragQueryFileW(drop_info, i, 0, 0) + 1, 0);
							Throw(::DragQueryFileW(drop_info, i, &path[0], UINT(path.size())) != 0, "Failed to query file name from dropped files");
							++i;
						}

						// Call the handler
						OnDropFiles(drop);
						return true;
					}
					#pragma endregion
				case WM_MOUSEWHEEL:
					#pragma region
					{
						// WM_MOUSEWHEEL is only sent to the focused window, unlike mouse
						// button/move messages which are sent to the control with focus.

						// Forward to the leaf controls first
						if (ForwardToChildren(hwnd, message, wparam, lparam, result))
							return true;

						auto delta = GET_WHEEL_DELTA_WPARAM(wparam);
						auto pt = Point(lparam);
						auto keystate = EMouseKey(GET_KEYSTATE_WPARAM(wparam)) | (::GetKeyState(VK_MENU) < 0 ? EMouseKey::Alt : EMouseKey()); 
						if (OnMouseWheel(MouseWheelArgs(delta, pt, keystate)))
							return true;

						// Pass to WndProc, I don't think it does anything, but doesn't hurt either
						return false;
					}
					#pragma endregion
				default:
					#pragma region
					{
						// By default, forward the parent window message to the children of this control
						return ForwardToChildren(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				}
			}

			// Forward a window message to child controls
			bool ForwardToChildren(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result, bool exclude_owned_windows = true)
			{
				// Should we be forwarding all messages to all child controls all the time?
				// Answer: yes, because all child controls need to know about the parent window
				// resizing, closing, etc. Also, container controls don't know what the requirements
				// of their contained controls are.
				size_t const LocalBufCount = 256;
				auto child_count = m_child.size();
				if (child_count == 0)
				{}
				else if (child_count < LocalBufCount)
				{
					// Make a copy of the child control pointers since ProcessWindowMessage
					// can cause child controls to be added or removed.
					Control* children[LocalBufCount];
					memcpy(children, &m_child[0], sizeof(Control*) * child_count);

					// Forward the message to each child
					for (size_t i = 0; i != child_count; ++i)
					{
						if (exclude_owned_windows && children[i]->m_top_level) continue;
						if (children[i]->ProcessWindowMessage(hwnd, message, wparam, lparam, result))
							return true;
					}
				}
				else
				{
					// Make a copy
					auto children = m_child;

					// Forward the message to each child
					for (size_t i = 0; i != child_count; ++i)
					{
						if (exclude_owned_windows && children[i]->m_top_level) continue;
						if (children[i]->ProcessWindowMessage(hwnd, message, wparam, lparam, result))
							return true;
					}
				}
				return false;
			}

			// Record the position of the control within the parent
			void RecordPosOffset()
			{
				// Store distances so that this control's position equals
				// parent.left + m_pos_offset.left, parent.right + m_pos_offset.right, etc..
				// (i.e. right and bottom offsets are typically negative)
				if (!m_parent || !m_hwnd || !m_pos_ofs_save) return;
				auto p = m_parent->ClientRect();
				auto c = ParentRect().Adjust(m_margin);
				m_pos_offset = Rect(c.left - p.left, c.top - p.top, c.right - p.right, c.bottom - p.bottom);
			}

			// Adjust the size of this control relative to 'parent_client'
			// 'parent_client' is the available client area on the parent in parent client coordinates
			// (typically 0,0 -> w,h. But not always, e.g. TabControl has the tabs in client space).
			// 'parent_client' may be the client area that the parent *will* have soon.
			virtual void ResizeToParent(Rect const& parent_client, bool repaint = false)
			{
				// Resize even if not visible so that the control has the correct size on becoming visible
				// Top level controls (i.e forms) will only call this if they are pinned.
				if (!m_hwnd || !m_parent || !m_parent->m_hwnd)
					return;

				// Get the available area and this control's area relative to it (including margin).
				auto p = parent_client;
				auto c = ParentRect().Adjust(m_margin);
				auto w = c.width();
				auto h = c.height();
				if (m_dock == EDock::None)
				{
					if (int(m_anchor & EAnchor::Left) != 0)
					{
						c.left = p.left + m_pos_offset.left;
						if (int(m_anchor & EAnchor::Right) == 0)
							c.right = c.left + w;
					}
					if (int(m_anchor & EAnchor::Top) != 0)
					{
						c.top = p.top + m_pos_offset.top;
						if (int(m_anchor & EAnchor::Bottom) == 0)
							c.bottom = c.top + h;
					}
					if (int(m_anchor & EAnchor::Right) != 0)
					{
						c.right = p.right + m_pos_offset.right;
						if (int(m_anchor & EAnchor::Left) == 0)
							c.left = c.right - w;
					}
					if (int(m_anchor & EAnchor::Bottom) != 0)
					{
						c.bottom = p.bottom + m_pos_offset.bottom;
						if (int(m_anchor & EAnchor::Top) == 0)
							c.top = c.bottom - h;
					}
				}
				else
				{
					switch (m_dock)
					{
					default: throw std::exception("Unknown dock style");
					case EDock::Fill:   c = p; break;
					case EDock::Top:    c.left = p.left; c.right  = p.right;  c.top    = 0;        c.bottom = p.top    + h; break;
					case EDock::Bottom: c.left = p.left; c.right  = p.right;  c.bottom = p.bottom; c.top    = c.bottom - h; break;
					case EDock::Left:   c.top  = p.top;  c.bottom = p.bottom; c.left   = 0;        c.right  = c.left   + w; break;
					case EDock::Right:  c.top  = p.top;  c.bottom = p.bottom; c.right  = p.right;  c.left   = c.right  - w; break;
					}
				}
				RAII<bool> no_save_ofs(m_pos_ofs_save, false);
				ParentRect(c.Adjust(-m_margin), repaint);
			}
			void ResizeToParent(bool repaint = false)
			{
				if (!m_parent) return;
				ResizeToParent(m_parent->ClientRect(), repaint);
			}

			// Handle auto position/size
			void AutoSizePosition(int& x, int& y, int& w, int& h, Control* parent)
			{
				using namespace auto_size_position;
				CalcPosSize(x, y, w, h, m_margin, [&](int id) -> Rect
					{
						assert((id == 0 || parent != nullptr) && "Sibling control id given without a parent");
						if (parent == nullptr) return MinMaxInfo().Bounds();
						if (id == 0) return parent->ClientRect(ERectFlags::ExcludeDockedChildren); // Includes padding in the parent

						// Find the child 'id' and return it's parent space rect including margins
						for (auto c : parent->m_child)
						{
							if (c->m_id != id) continue;
							return c->ParentRect().Adjust(c->Margin());
						}
						throw std::exception("Sibling control not found");
					});
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
			enum { DefW = 50, DefH = 50 };

			// Create the control
			Control(pr::gui::Params const& p = CtrlParams())
				:m_hwnd()
				,m_id(p.m_id)
				,m_name(p.name())
				,m_parent()
				,m_child()
				,m_anchor(p.m_anchor)
				,m_dock(p.m_dock)
				,m_margin(p.m_margin)
				,m_padding(p.m_padding)
				,m_pos_offset()
				,m_pos_ofs_save(true)
				,m_min_max_info()
				,m_colour_fore(p.fore_color())
				,m_colour_back(p.back_color())
				,m_down_at()
				,m_top_level(p.m_top_level)
				,m_handle_only(false)
				,m_dbl_buffer(nullptr)
				,m_wci(p.wci())
				,m_thunk()
				,m_oldproc()
				,m_thread_id(std::this_thread::get_id())
			{
				m_thunk.Init(DWORD_PTR(StaticWndProc), this);

				// Create the control if told to, or if the parent exists and not explicitly told not to
				if (p.create())
				{
					// Remember to set 'p.m_create' appropriately
					// If the control has a parent, then the parent should exist before the control is created.
					// This is true for 'owned' forms as well as WS_CHILD controls.
					assert((p.m_top_level || p.m_parent != nullptr) && "Creating a control as a child of a window whose parent is not yet created.");

					// Create the control
					Create(p);
				}
				else
				{
					// You can call 'Create' once the parent has an HWND. Or, if the parent
					// is based on a resource, and 'id' is not ID_UNUSED, then it will automatically
					// be created and attached to the control from the resource due to setting the parent here.
					Parent(p.m_parent);
				}
			}

			// Allow construction from existing handle
			Control(HWND hwnd)
				:Control(CtrlParams().id(::GetDlgCtrlID(hwnd)).anchor(EAnchor::None).top_level(::GetAncestor(hwnd, GA_ROOT) == hwnd))
			{
				m_handle_only = true;
				m_hwnd = hwnd;
			}

			virtual ~Control()
			{
				if (!m_handle_only)
				{
					DoubleBuffered(false);

					// Orphan child controls
					for (; !m_child.empty(); )
						m_child.front()->Parent(nullptr);

					// Detach from our parent
					Parent(nullptr);

					// Destroy the window
					if (::IsWindow(m_hwnd))
						::DestroyWindow(m_hwnd);
				}
				assert((m_hwnd = (HWND)0xdddddddd) != 0);// mark as destructed
			}

			Control(Control&&) = delete;
			Control(Control const&) = delete;

			// Implicit conversion to HWND
			operator HWND() const { return m_hwnd; }

			// Create the hwnd for this control. Derived types should call this.
			// WARNING: The overload of this method will not be called when Create() is called from the Control constructor
			virtual void Create(pr::gui::Params const& p)
			{
				// Check whether p.create() was true in the constructor for this control
				assert(m_hwnd == nullptr && "Window handle already exists");
				assert((p.m_parent == nullptr || ::IsWindow(p.m_parent)) && "Child controls can only be created after the parent has been created");

				// Save creation properties, we need to set these, even tho they're
				// set in the constructor, because 'p' might be newer than what was
				// used when the control was constructed.
				// Don't do p.m_id & IdMask. Controls created by windows use the window
				// handle as the id. IdMask is only needed to AutoPosSize which the caller
				// controls.
				m_id          = p.m_id;
				m_name        = p.name();
				m_anchor      = p.m_anchor;
				m_dock        = p.m_dock;
				m_margin      = p.m_margin;
				m_padding     = p.m_padding;
				m_colour_fore = p.fore_color();
				m_colour_back = p.back_color();
				m_top_level   = p.m_top_level;
				m_wci         = p.wci();

				// Local copies of the window location so we can auto size etc...
				auto x = p.m_x;
				auto y = p.m_y;
				auto w = p.m_w;
				auto h = p.m_h;

				// Handle auto position/size
				AutoSizePosition(x, y, w, h, p.m_parent);

				// If this control is a pop-up or overlapped window, then we need x,y,w,h to be in screen coords
				if ((p.m_style & WS_CHILD) == 0 && p.m_parent.m_ctrl != nullptr)
				{
					auto r = p.m_parent->ScreenRect();
					x += r.left;
					y += r.top;
				}

				// Determine the value to pass at the HMENU parameter in CreateWindowEx
				// For pop-up and overlapped windows this should be a valid menu handle or null
				// Otherwise, it should be the id of the control being created
				auto menu = (p.m_style & WS_CHILD) ? (HMENU)p.m_id : p.menu();

				// CreateWindowEx failure reasons:
				//  invalid menu handle - if the window style is overlapped or pop-up, then 'menu' must be null
				//     or a valid menu handle otherwise it is the id of the control being created.
				InitParam init(this, p.m_init_param);
				auto hwnd = ::CreateWindowExW(p.m_style_ex, p.atom(), p.m_text, p.m_style, x, y, w, h, p.m_parent, menu, p.m_hinst, &init);
				Throw(hwnd != nullptr, "CreateWindowEx failed");

				// If we're creating a control whose window class we don't control (i.e. a third party control),
				// then Attach won't have been called. In this case, we want to subclass the window and install
				// our wndproc.
				if (m_hwnd == nullptr)
					Attach(hwnd);

				Parent(p.m_parent);
				RecordPosOffset();
				Font(HFONT(GetStockObject(DEFAULT_GUI_FONT)));

				// Set the window icon
				HICON icon;
				if ((icon = p.icon_bg()) != nullptr) Icon(icon, true);
				if ((icon = p.icon_sm()) != nullptr) Icon(icon, false);
				
				if (p.m_style & WS_VISIBLE)
				{
					ShowWindow(m_hwnd, SW_SHOW);
					UpdateWindow(m_hwnd);
					DrawMenuBar(m_hwnd);
				}
			}

			// Attach/Detach this control wrapper to/form the associated window handle
			// WARNING: The overload of this method will not be called when Create() is called from the Control constructor
			virtual void Attach(HWND hwnd)
			{
				assert(m_hwnd == nullptr && hwnd != nullptr);
				m_hwnd = hwnd;

				// If the wndproc for this control is not our thunk, hook it
				auto wndproc = (WNDPROC)::GetWindowLongPtrW(m_hwnd, GWLP_WNDPROC);
				auto dlgproc = (DLGPROC)::GetWindowLongPtrW(m_hwnd, DWLP_DLGPROC);
				if (wndproc != m_thunk.GetCodeAddress() && dlgproc != m_thunk.GetCodeAddress())
				{
					if (dlgproc == nullptr)
						m_oldproc = (WNDPROC)::SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_thunk.GetCodeAddress()));
					else
						m_oldproc = (WNDPROC)::SetWindowLongPtrW(m_hwnd, DWLP_DLGPROC, LONG_PTR(m_thunk.GetCodeAddress()));
				}
			}
			virtual void Detach()
			{
				if (m_hwnd == nullptr)
					return;

				// Restore the original wndproc
				auto wndproc = (WNDPROC)::GetWindowLongPtrW(m_hwnd, GWLP_WNDPROC);
				auto dlgproc = (DLGPROC)::GetWindowLongPtrW(m_hwnd, DWLP_DLGPROC);
				if (wndproc == m_thunk.GetCodeAddress())
					::SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, LONG_PTR(m_oldproc));
				else if (dlgproc == m_thunk.GetCodeAddress())
					::SetWindowLongPtrW(m_hwnd, DWLP_DLGPROC, LONG_PTR(0));

				m_oldproc = nullptr;
				m_hwnd = nullptr;
			}

			#pragma region Accessors

			// Get/Set the parent of this control.
			WndRef Parent() const
			{
				return m_parent;
			}
			virtual void Parent(WndRef parent)
			{
				// Check we're not parenting to ourself or a child
				#ifndef NDEBUG
				if (parent.m_ctrl != nullptr)
				{
					std::vector<Control*> stack;
					for (stack.push_back(this); !stack.empty();)
					{
						auto x = stack.back(); stack.pop_back();
						assert(parent.m_ctrl != x && "Cannot parent to a child");
						for (auto c : x->m_child)
							stack.push_back(c);
					}
				}
				#endif

				// Change the ancestor window (only if this is a child control)
				if (::IsWindow(m_hwnd) && !m_top_level)
				{
					// Set the owner or ancestor. (Owner if this is a top level window, otherwise ancestor)
					Throw(::SetParent(m_hwnd, parent.m_hwnd) != nullptr, "SetParent failed");

					// Supposed to send WM_CHANGEDUISTATE after changing the parent of a window
					auto hwnd = parent.m_hwnd ? parent.m_hwnd : m_hwnd;
					auto uis = SendMessageW(hwnd, WM_QUERYUISTATE, 0, 0);
					SendMessageW(hwnd, WM_CHANGEUISTATE, WPARAM(MakeWord(uis, UIS_INITIALIZE)), 0);
				}

				// Change the window that this control is dependent on
				if (m_parent != parent.m_ctrl)
				{
					if (m_parent != nullptr) // Remove from existing parent
					{
						auto& c = m_parent->m_child;
						c.erase(std::remove(c.begin(), c.end(), this), c.end());
					}

					m_parent = parent.m_ctrl;

					if (m_parent != nullptr) // Add to new parent
					{
						m_parent->m_child.push_back(this);
					}
				}
			}

			// Get the number of child controls
			size_t ChildCount() const
			{
				return m_child.size();
			}

			// Get a child control
			WndRef Child(int i) const
			{
				return m_child[i];
			}

			// Get the collection of child controls
			Controls const& Children() const
			{
				return m_child;
			}

			// Get the top level control. This is typically the window containing this control
			Control const* TopLevelControl() const
			{
				auto p = this;
				for (;!p->m_top_level && p->m_parent != nullptr; p = p->m_parent) {}
				return p;
			}

			// Get/Set the window style
			LONG_PTR Style() const
			{
				assert(::IsWindow(m_hwnd));
				return ::GetWindowLongPtrW(m_hwnd, GWL_STYLE);
			}
			void Style(LONG_PTR style)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowLongPtrW(m_hwnd, GWL_STYLE, style);
			}

			// Get/Set the extended window style
			LONG_PTR StyleEx() const
			{
				assert(::IsWindow(m_hwnd));
				return ::GetWindowLongPtrW(m_hwnd, GWL_EXSTYLE);
			}
			void StyleEx(LONG_PTR style)
			{
				assert(::IsWindow(m_hwnd));
				::SetWindowLongPtrW(m_hwnd, GWL_EXSTYLE, style);
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
				::ShowWindow(m_hwnd, vis ? SW_SHOW : SW_HIDE);
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
				ResizeToParent(false);
				Invalidate();
			}

			// Get/set the padding for the control
			Rect Padding() const
			{
				return m_padding;
			}
			void Padding(Rect padding)
			{
				m_padding = padding;
				ResizeToParent(false);
				Invalidate();
			}

			// Get/set the margin for the control
			Rect Margin() const
			{
				return m_margin;
			}
			void Margin(Rect margin)
			{
				m_margin = margin;
				ResizeToParent(false);
				Invalidate();
			}

			// Get/Set Drag/Drop allowed
			bool AllowDrop() const
			{
				return (StyleEx() & WS_EX_ACCEPTFILES) != 0;
			}
			void AllowDrop(bool allow)
			{
				::DragAcceptFiles(*this, allow);
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
			bool DoubleBuffered() const { return m_dbl_buffer != nullptr; }
			void DoubleBuffered(bool dbl_buffer)
			{
				assert(!m_handle_only && "Cannot double buffer handle-only instances");
				if (m_dbl_buffer != nullptr)
				{
					::DeleteObject(m_dbl_buffer);
					m_dbl_buffer = nullptr;
				}
				if (dbl_buffer)
				{
					ClientDC dc(m_hwnd);
					auto r = ClientRect();
					m_dbl_buffer = ::CreateCompatibleBitmap(dc, r.width(), r.height());
				}
			}

			// Get/Set the control's background colour
			COLORREF BackColor() const
			{
				if (m_colour_back != nullptr)
					return m_colour_back;
				else
					return CLR_INVALID;

				//assert(::IsWindow(m_hwnd));
				//DC dc(::GetDC(m_hwnd));
				//return ::GetBkColor(dc);
			}
			COLORREF BackColor(COLORREF col)
			{
				return m_colour_back = col != CLR_INVALID
					? std::move(Brush(col))
					: std::move(Brush());

				//assert(::IsWindow(m_hwnd));
				//DC dc(::GetDC(m_hwnd));
				//return ::SetBkColor(dc, m_colour_back);
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

			// Return the position of this control in parent client space
			Point loc() const
			{
				return ParentRect().topleft();
			}
			Size size() const
			{
				auto r = ParentRect();
				return Size(r.width(), r.height());
			}
			int width() const
			{
				return ParentRect().width();
			}
			int height() const
			{
				return ParentRect().height();
			}

			// Returns a copy of 'rect' increased by the non-client areas of the window
			// Remember, ClientRect is [inclusive,inclusive] (if 'rect' is the client rect)
			Rect AdjRect(Rect const& rect = Rect()) const
			{
				auto r = rect;
				Throw(::AdjustWindowRectEx(&r, DWORD(Style()), BOOL(::GetMenu(m_hwnd) != nullptr), DWORD(StyleEx())), "AdjustWindowRectEx failed.");
				return r;
			}

			// Get the client rect [TL,BR) for the window in this controls client space.
			// Note: Menus are part of the non-client area, you don't need to offset
			// the client rect for the menu.
			virtual Rect ClientRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect rect;
				Throw(::GetClientRect(m_hwnd, &rect), "GetClientRect failed.");
				rect = rect.Adjust(m_padding);
				return rect;
			}
			Rect ClientRect(ERectFlags flags) const
			{
				auto r = ClientRect();
				if ((flags & ERectFlags::ExcludeDockedChildren) != 0)
				{
					for (auto& child : m_child)
					{
						if (child->m_dock == EDock::None) continue;
						if (!child->Visible()) continue;
						r = r.Subtract(child->ParentRect());
					}
				}
				return r;
			}

			// Get/Set the control bounds [TL,BR) in screen space
			Rect ScreenRect() const
			{
				assert(::IsWindow(m_hwnd));
				Rect r;
				Throw(::GetWindowRect(m_hwnd, &r), "GetWindowRect failed.");
				return r;
			}
			void ScreenRect(Rect r, bool repaint = true, HWND prev = nullptr, EWindowPos flags = EWindowPos::NoZorder)
			{
				assert(::IsWindow(m_hwnd));
				if (!repaint) flags = flags | EWindowPos::NoRedraw;

				// SetWindowPos takes client space coordinates
				if (Style() & WS_CHILD)
				{
					auto hwndparent = ::GetParent(m_hwnd);
					::MapWindowPoints(nullptr, hwndparent, r.points(), 2);
				}

				// Use prev = ::GetWindow(m_hwnd, GW_HWNDPREV) for the current z-order
				Throw(::SetWindowPos(m_hwnd, prev, r.left, r.top, r.width(), r.height(), (UINT)flags), "SetWindowPos failed");
				RecordPosOffset();
			}

			// Get/Set the bounds [TL,BR) of this control within it's parent client space.
			// Only applies to WS_CHILD windows, owned windows are still positioned relative to the screen
			Rect ParentRect() const
			{
				assert(::IsWindow(m_hwnd));

				// If the control has no parent, then the screen is the parent
				auto hwndparent = ::GetParent(m_hwnd);
				if (hwndparent == nullptr)
					return ScreenRect();

				// Return the bounds of this control relative to 'parent'
				auto rect = ScreenRect(); // Not using ClientRect() because we don't want this control's padding included
				::MapWindowPoints(nullptr, hwndparent, rect.points(), 2);
				return rect;
			}
			void ParentRect(Rect r, bool repaint = true, HWND prev = nullptr, EWindowPos flags = EWindowPos::NoZorder)
			{
				assert(::IsWindow(m_hwnd));
				if (!repaint) flags = flags | EWindowPos::NoRedraw;

				// Invalidate the previous and new rect on the parent
				auto hwndparent = ::GetParent(m_hwnd);
				if (hwndparent != nullptr)
				{
					auto pr = ParentRect();
					auto inv = r.Union(pr);
					::InvalidateRect(hwndparent, &inv, FALSE);
				}

				// SetWindowPos takes client space coordinates
				// Use prev = ::GetWindow(m_hwnd, GW_HWNDPREV) for the current z-order
				Throw(::SetWindowPos(m_hwnd, prev, r.left, r.top, r.width(), r.height(), (UINT)flags), "SetWindowPos failed");
				RecordPosOffset();
			}

			// Get/Set the position of this control within the parent's client space
			Point ParentPos() const
			{
				return ParentRect().topleft();
			}
			void ParentPos(int x, int y, bool repaint = false)
			{
				auto r = ParentRect();
				ParentRect(r.Shifted(x - r.left, y - r.top), repaint);
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
			Menu MenuStrip() const
			{
				assert(::IsWindow(m_hwnd));
				return Menu(::GetMenu(m_hwnd), false);
			}
			Menu MenuStrip(Menu menu)
			{
				assert(::IsWindow(m_hwnd));
				auto prev = Menu();
				Throw(::SetMenu(m_hwnd, menu) != 0, "Failed to set menu");
				return prev;
			}
			Menu MenuStrip(Menu::EKind kind, Menu::ItemList const& items)
			{
				return MenuStrip(Menu(kind, items));
			}

			// Get/Set the control's icon
			HICON Icon(bool big_icon) const
			{
				assert(::IsWindow(m_hwnd));
				return HICON(::SendMessageW(m_hwnd, WM_GETICON, big_icon ? ICON_BIG : ICON_SMALL, 0));
			}
			HICON Icon(HICON icon, bool big_icon)
			{
				assert(::IsWindow(m_hwnd));
				return HICON(::SendMessageW(m_hwnd, WM_SETICON, big_icon ? ICON_BIG : ICON_SMALL, LPARAM(icon)));
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
				assert(m_hwnd != centre_hwnd && "'centre_hwnd' is the window to centre relative to. It shouldn't be this window");

				// Determine the owning window to centre against
				auto style = Style();
				if (centre_hwnd == nullptr)
					centre_hwnd = (style & WS_CHILD) ? ::GetParent(m_hwnd) : ::GetWindow(m_hwnd, GW_OWNER);

				Rect area, centre;

				// Get the coordinates of the window relative to 'centre_hwnd'
				if (!(style & WS_CHILD))
				{
					// Don't centre against invisible or minimized windows
					if (centre_hwnd != nullptr)
					{
						auto parent_state = ::GetWindowLong(centre_hwnd, GWL_STYLE);
						if (!(parent_state & WS_VISIBLE) || (parent_state & WS_MINIMIZE))
							centre_hwnd = nullptr;
					}

					// Centre within screen coordinates
					HMONITOR monitor = ::MonitorFromWindow(centre_hwnd ? centre_hwnd : m_hwnd, MONITOR_DEFAULTTONEAREST);
					Throw(monitor != nullptr, "Failed to determine the monitor containing the centre on window");

					MonitorInfo minfo;
					Throw(::GetMonitorInfoW(monitor, &minfo), "Failed to get info on monitor containing centre on window");

					area = minfo.rcWork;
					centre = centre_hwnd ? ::GetWindowRect(centre_hwnd, &centre),centre : area;
				}
				else
				{
					// centre within parent client coordinates
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

			// Position this window relative to it's parent
			// AutoSizePosition values can be used, e.g. Left|LeftOf|<sibbling_ctrl_id>
			// Use SWP_ flags to ignore position or size changes
			void PositionWindow(int x, int y, int w, int h, EWindowPos flags = EWindowPos::NoZorder | EWindowPos::NoActivate)
			{
				if ((flags & EWindowPos::NoMove) != 0)
				{
					auto r = ParentRect();
					x = r.left;
					y = r.top;
				}
				if ((flags & EWindowPos::NoSize) != 0)
				{
					auto r = ParentRect();
					w = r.width();
					h = r.height();
				}
				AutoSizePosition( x, y, w, h, m_parent);
				ParentRect(Rect(x, y, x + w, y + h), false, nullptr, flags);
			}
			void PositionWindow(int x, int y)
			{
				PositionWindow(x, y, 0, 0, EWindowPos::NoSize | EWindowPos::NoZorder | EWindowPos::NoActivate);
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
				if (::GetKeyState(VK_LSHIFT  ) & 0x8000) state = state | EControlKey::LShift;
				if (::GetKeyState(VK_RSHIFT  ) & 0x8000) state = state | EControlKey::RShift;
				if (::GetKeyState(VK_LCONTROL) & 0x8000) state = state | EControlKey::LCtrl;
				if (::GetKeyState(VK_RCONTROL) & 0x8000) state = state | EControlKey::RCtrl;
				if (::GetKeyState(VK_LMENU   ) & 0x8000) state = state | EControlKey::LAlt;
				if (::GetKeyState(VK_RMENU   ) & 0x8000) state = state | EControlKey::RAlt;
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

			// Window shown or hidden
			EventHandler<Control&, ShownEventArgs const&> Shown;

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

			// Handle window shown or hidden
			virtual void OnShown(ShownEventArgs const& args)
			{
				Shown(*this, args);
			}

			// Handle the Paint event. Return true, to prevent anything else handling the event
			virtual bool OnPaint(PaintEventArgs const& args)
			{
				Paint(*this, args);
				return false;
			}

			// Handle the Erase background event. Return true, to prevent anything else handling the event
			virtual bool OnEraseBkGnd(EmptyArgs const& args)
			{
				EraseBkGnd(*this, args);
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
		// A common, non-template, base class for all forms
		struct Form :Control ,IMessageFilter
		{
		protected:
			// Notes:
			// Neither Form or Control define a load of OnXYZ handlers. This is because it adds a
			// load of potentially unneeded entries to the 'vtbl'. The expected way to use this class
			// is to override ProcessWindowMessage and decode/handle the window messages you actually
			// need. Notice, WM_CREATE is typically not needed, the constructor of your derived type
			// is where OnCreate() code should go.
			// Re WndProc from MSDN:
			//  "You should use the dialog box procedure only if you use the dialog box wndclass for the dialog box.
			//   This is the default class and is used when no explicit wndclass is specified in the dialog box template"

			enum { IDC_PINWINDOW_OPT = 0x4e50, IDC_PINWINDOW_SEP };

			HINSTANCE      m_hinst;            // Module instance
			bool           m_app_main_window;  // True if this is the main application window
			Menu           m_menu;             // The main menu
			HACCEL         m_accel;            // Keyboard accelerators for this window
			DlgTemplate    m_templ;            // A dialog template for this form (if given)
			EStartPosition m_start_pos;        // How to position the form when first shown
			int            m_exit_code;        // The code to return when the form closes
			bool           m_dialog_behaviour; // True if IsDialogMessage() is called in the message loop
			bool           m_hide_on_close;    // True if the window should just hide when closed
			bool           m_pin_window;       // True if this window is pinned to its parent
			bool           m_modal;            // True if this is a dialog being displayed modally

		public:

			enum { DefW = 800, DefH = 600 };

			// Form Constructor
			// The creation of the window is kept for the Form class to do, rather than the control,
			// so that a window or dialog gets created as appropriate. Also, passing parent == nullptr
			// to 'Control' because the virtual Parent() function will not call our overload until
			// construction is complete.
			// Callers should use m_create == ECreate::Create for modeless frame windows, ECreate::Defer
			// for windows that will be created later (either with Create() or ShowDialog()). ECreate::Auto
			// should not typically be used for forms.
			Form(pr::gui::Params const& p = FormParams())
				:Control(CtrlParams(p).create(ECreate::Defer).parent(nullptr))
				,m_hinst(p.m_hinst)
				,m_app_main_window(p.m_main_wnd)
				,m_menu(p.menu())
				,m_accel(p.accel())
				,m_templ(p.templ())
				,m_start_pos(p.m_start_pos)
				,m_exit_code()
				,m_dialog_behaviour(p.m_dlg_behaviour)
				,m_hide_on_close(p.m_hide_on_close)
				,m_pin_window(p.m_pin_window)
				,m_modal(false)
			{
				if (p.create())
					Create(p);
				else
					Parent(p.m_parent);
			}

			// Close on destruction
			virtual ~Form()
			{
				HideOnClose(false);
				Close();
			}

			// Create the HWND for this window
			// After calling this, only 'Show()' can be used.
			// This function should be called after construction with m_create == ECreate::Defer
			void Create(pr::gui::Params const& p) override
			{
				assert(m_hwnd == nullptr && "window already created");

				// Save the creation data
				m_hinst             = p.m_hinst;
				m_app_main_window  |= p.m_main_wnd; // 'm_app_main_window' can only be set to true. A main window can't become not the main window
				m_menu              = p.menu();
				m_accel             = p.accel();
				m_templ             = p.templ();
				m_start_pos         = p.m_start_pos;
				m_dialog_behaviour  = p.m_dlg_behaviour;
				m_hide_on_close     = p.m_hide_on_close;
				m_pin_window        = p.m_pin_window;
				
				InitParam lparam(this, p.m_init_param);

				// If this form has a dialog template, then create the window as a modeless dialog
				if (p.m_templ != nullptr)
				{
					assert(p.m_templ->valid());
					m_hwnd = ::CreateDialogIndirectParamW(p.m_hinst, p.templ(), p.m_parent, (DLGPROC)&InitWndProc, LPARAM(&lparam));
					Throw(m_hwnd != nullptr, "CreateDialogIndirectParam failed");
					Parent(p.m_parent);
				}
				else if (p.m_id != ID_UNUSED) // Create from a dialog resource id
				{
					m_hwnd = ::CreateDialogParamW(p.m_hinst, MAKEINTRESOURCEW(p.m_id), p.m_parent, (DLGPROC)&InitWndProc, LPARAM(&lparam));
					Throw(m_hwnd != nullptr, "CreateDialogParam failed");
					Parent(p.m_parent);
				}
				else // Otherwise create as a normal window
				{
					Control::Create(p);
				}
			}

			// Display as a modeless form.
			virtual void Show(int show = SW_SHOW)
			{
				assert(m_hwnd != nullptr && "Window does not exist yet, call Create() first");

				// Not showing the window modally
				m_modal = false;

				// Show the window, non-modally
				ShowWindow(m_hwnd, show);
				UpdateWindow(m_hwnd);
			}

			// Display as a modeless form, creating the window first if necessary
			virtual void Show(pr::gui::Params const& p)
			{
				if (m_hwnd == nullptr)
					Create(p);

				Show(SW_SHOW);
			}

			// Display the form modally
			virtual EDialogResult ShowDialog(WndRef parent = nullptr, void* init_param = nullptr)
			{
				// Modal dialogs should not have their window handle created yet, the
				// DialogBox() functions create the window and the message loop
				assert(m_hwnd == nullptr && "Window already created, cannot be displayed modally");

				// Showing the window modally
				m_modal = true;

				// If a template is given, create the window from the template
				InitParam lparam(this, init_param);
				if (m_templ.valid())
				{
					return EDialogResult(::DialogBoxIndirectParamW(m_hinst, m_templ, parent, (DLGPROC)&InitWndProc, LPARAM(&lparam)));
				}
				else
				{
					assert(m_id != ID_UNUSED && "Modal dialog without a resource id or template");
					return EDialogResult(::DialogBoxParamW(m_hinst, MAKEINTRESOURCEW(m_id), parent, (DLGPROC)&InitWndProc, LPARAM(&lparam)));
				}
			}

			// Close this form
			virtual bool Close(int exit_code = 0)
			{
				if (m_hwnd == nullptr) return false;

				// If we're only hiding, just go invisible
				if (m_hide_on_close)
				{
					Visible(false);
					return true;
				}

				// Remove this window from its parent.
				// Don't detach children, that happens when the Form/Control is destructed
				Parent(nullptr);

				m_exit_code = exit_code;
				auto r = m_modal
					? ::EndDialog(m_hwnd, INT_PTR(m_exit_code))
					: ::DestroyWindow(m_hwnd);

				// Don't null m_hwnd here, that happens in WM_DESTROY
				return r != 0;
			}
			virtual bool Close(EDialogResult dialog_result)
			{
				return Close(int(dialog_result));
			}

			// Get/Set whether the form uses dialog-like message handling
			bool DialogBehaviour() const { return m_dialog_behaviour; }
			void DialogBehaviour(bool enabled) { m_dialog_behaviour = enabled; }

			// Get/Set whether the window closes or just hides when closed
			bool HideOnClose() const { return m_hide_on_close; }
			void HideOnClose(bool enable) { m_hide_on_close = enable; }

			// Get/Set whether the window is pinned to it's parent
			bool PinWindow() const { return m_pin_window; }
			void PinWindow(bool pin)
			{
				m_pin_window = pin;
				if (pin) RecordPosOffset();
			}

			// Set the parent of this form
			void Parent(WndRef parent) override
			{
				if (m_parent != nullptr)
				{
					auto sysmenu = ::GetSystemMenu(m_hwnd, FALSE);
					if (sysmenu)
					{
						::RemoveMenu(sysmenu, IDC_PINWINDOW_SEP, MF_BYCOMMAND|MF_SEPARATOR);
						::RemoveMenu(sysmenu, IDC_PINWINDOW_OPT, MF_BYCOMMAND|MF_STRING);
					}
				}

				Control::Parent(parent);

				if (m_parent != nullptr)
				{
					auto sysmenu = ::GetSystemMenu(m_hwnd, FALSE);
					if (sysmenu)
					{
						auto idx = ::GetMenuItemCount(sysmenu) - 2;
						Throw(::InsertMenuW(sysmenu, idx++, MF_BYPOSITION|MF_SEPARATOR, IDC_PINWINDOW_SEP, 0), "InsertMenu failed");
						Throw(::InsertMenuW(sysmenu, idx++, MF_BYPOSITION|MF_STRING, IDC_PINWINDOW_OPT, L"Pin Window"), "InsertMenu failed");
					}
				}
			}

		private:

			// Support dialog behaviour and keyboard accelerators
			bool TranslateMessage(MSG& msg) override
			{
				return
					(m_accel && TranslateAcceleratorW(m_hwnd, m_accel, &msg)) ||
					(m_dialog_behaviour && IsDialogMessageW(m_hwnd, &msg));
			}

			// Window proc. Derived forms should not override WndProc.
			// All messages are passed to 'ProcessWindowMessage' so use that.
			LRESULT WndProc(UINT message, WPARAM wparam, LPARAM lparam) override final
			{
				#if PR_WNDPROCDEBUG
				RAII<int> nest(wnd_proc_nest(), wnd_proc_nest()+1);
				//WndProcDebug(m_hwnd, message, wparam, lparam, pr::FmtS("%s FormWndProc: ", m_name.c_str()));
				#endif

				LRESULT result = S_OK;

				// Forward the message to the message map function
				// which will forward the message to nested controls
				// If the message map doesn't handle the message,
				// pass it to the WndProc for this form.
				if (!ProcessWindowMessage(m_hwnd, message, wparam, lparam, result))
					result = Control::WndProc(message, wparam, lparam);
				
				// This is used for DialogProc somehow
				::SetWindowLongPtrW(m_hwnd, DWLP_MSGRESULT, result);
				return result;
			}

		protected:

			// Message map function
			// 'hwnd' is the handle of the parent window that contains this control.
			// Messages processed here are the messages sent to the parent window.
			// Only change 'result' when specifically returning a result (it defaults to S_OK)
			// Return true to halt message processing, false to allow other controls to process the message
			bool ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				// For Forms, this method is effectively the WndProc for the form.
				// By default we don't forward messages to child controls. Use Control::ProcessWindowMessage()
				// to explicitly forward some message types, call ForwardToChildren only when you want different
				// behaviour to the Control default implementation but still want child controls to process the message.
				// Forms can be parented to other forms
				// Any message not handled here or in Control::ProcessWindowMessage() is passed to Control::WndProc
				// I.e the logic goes:
				//  <new message> arrives
				//    Form::ProcessWindowMessage() - form processes it which may mean passing it to 
				//    Control::ProcessWindowMessage() - the base class (Control)
				//      - each message may be passed to child controls using ForwardToChildren(), this is
				//      what Control::ProcessWindowMessage() does by default.
				//  otherwise:
				//     Control::WndProc() - otherwise the WndProc for the window handles it (in the base class)
				//  Some events in Control::WndProc() have handler virtual functions

				//WndProcDebug(hwnd, message, wparam, lparam, pr::FmtS("%s FormProcWinMsg: ",m_name.c_str()));
				switch (message)
				{
				case WM_INITDIALOG:
					#pragma region
					{
						// The default Control::ProcessWindowMessage() handler for WM_INITDIALOG calls
						// 'Attach' which Forms don't need because we call Attach in InitWndProc.
						// Note: typically sub-classed forms will call base::ProcessWindowMessage()
						// before doing whatever they need to do in WM_INITDIALOG. This is so that
						// child controls get attached.
						if (ForwardToChildren(hwnd, message, wparam, lparam, result))
							return true;

						// Return false so that WM_INITDIALOG is passed to the WndProc
						return false;
					}
					#pragma endregion
				case WM_CLOSE:
					#pragma region
					{
						// Close the form in response to the WM_CLOSE request
						Close();

						// If this is a hide-on-close form, return true so that the WM_CLOSE message isn't
						// passed to the WndProc which would then send WM_DESTROY.
						return m_hide_on_close;
					}
					#pragma endregion
				case WM_DESTROY:
					#pragma region
					{
						// Let children know the parent is destroying
						if (ForwardToChildren(hwnd, message, wparam, lparam, result))
							return true;

						// If we're the main app, post WM_QUIT
						if (m_app_main_window)
							::PostQuitMessage(m_exit_code);

						// Return false so that WM_DESTROY is passed to the WndProc which
						// while unhook the thunk and null the 'm_hwnd'
						return false;
					}
					#pragma endregion
				case WM_SHOWWINDOW:
					#pragma region
					{
						// WM_SHOWWINDOW is sent to notify that the window was shown or hidden.
						auto shown = wparam != 0;
						auto reason = lparam;
						if (shown && reason == 0) // Due to ShowWindow
						{
							switch (m_start_pos)
							{
							case EStartPosition::Default:
								{
									auto pt = m_parent != nullptr ? m_parent->loc() : Point();
									PositionWindow(pt.x + 50, pt.y + 50);
									break;
								}
							case EStartPosition::CentreParent:
								{
									auto parent = m_parent != nullptr ? (HWND)*m_parent : (HWND)nullptr;
									CenterWindow(parent);
									break;
								}
							}
							m_start_pos = EStartPosition::Manual;
						}

						// Pass WM_SHOWWINDOW to the WndProc
						return false;
					}
					#pragma endregion
				case WM_CTLCOLORDLG:
					#pragma region
					{
						// Our background brush is only valid if we have valid wndclass info
						// otherwise, let the default handle it.
						result = LRESULT(m_wci.hbrBackground);
						return m_wci.m_atom != 0;
					}
					#pragma endregion
				case WM_COMMAND:
					#pragma region
					{
						auto id        = UINT(LoWord(wparam)); // The menu_item id or accelerator id
						auto src       = UINT(HiWord(wparam)); // 0 = menu, 1 = accelerator, 2 = control-defined notification code
						auto ctrl_hwnd = HWND(lparam);         // Only valid if sent from a control (i.e. not sent from a menu or accelerator)

						// This is a menu or accelerator command if the control hwnd is 0
						if (ctrl_hwnd == nullptr)
							return HandleMenu(id, src, ctrl_hwnd);

						// Otherwise forward to child controls
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_SYSCOMMAND:
					#pragma region
					{
						auto id = UINT(LoWord(wparam)); // The menu_item id or accelerator id
						if (id == IDC_PINWINDOW_OPT)
						{
							PinWindow(!PinWindow());
							::CheckMenuItem(::GetSystemMenu(m_hwnd, FALSE), IDC_PINWINDOW_OPT, MF_BYCOMMAND|(PinWindow()? MF_CHECKED : MF_UNCHECKED));
							return true;
						}

						// Pass the WM_SYSCOMMAND to the WndProc
						return false;
					}
					#pragma endregion
				case WM_WINDOWPOSCHANGED:
					#pragma region
					{
						// If we're a pinned window, record our offset from our target
						if (PinWindow())
							RecordPosOffset();

						// Resize child controls and child windows (if pinned)
						RAII<bool> unpin(m_pin_window, false);
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				case WM_DROPFILES:
				case WM_NOTIFY:
				case WM_MOUSEWHEEL:
				case WM_SETFOCUS:
				case WM_KILLFOCUS:
				case WM_TIMER:
				case WM_ENTERSIZEMOVE:
				case WM_EXITSIZEMOVE:
				case WM_WINDOWPOSCHANGING:
				case WM_CTLCOLORSTATIC:
				case WM_CTLCOLORBTN:
				case WM_CTLCOLOREDIT:
				case WM_CTLCOLORLISTBOX:
				case WM_CTLCOLORSCROLLBAR:
					#pragma region
					{
						// Messages that get here will be forwarded to child controls as well
						return Control::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
					}
					#pragma endregion
				default:
					#pragma region
					{
						// By default, messages aren't forwarded to child controls
						return false;
					}
					#pragma endregion
				}
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
				switch (item_id)
				{
				case IDCLOSE:
					Close(EDialogResult::Close);
					return true;
				case IDCANCEL:
					Close(EDialogResult::Cancel);
					return true;
				case IDOK:
					Close(EDialogResult::Ok);
					return true;
				}
				return false;
			}

			// Adjust the size of this control relative to it's parent
			void ResizeToParent(Rect const& parent_client, bool repaint = false) override
			{
				if (!PinWindow()) return;
				Control::ResizeToParent(parent_client, repaint);
			}

			#pragma region Handlers

			// Handle the Paint event. Return true, to prevent anything else handling the event
			bool OnPaint(PaintEventArgs const& args) override
			{
				// Do the painting for the form (which is just
				// fill the update region with the background colour)
				if (m_colour_back != nullptr || m_wci.hbrBackground != nullptr)
				{
					PaintStruct ps(args.m_hwnd);
					auto dc = args.m_alternate_hdc ? args.m_alternate_hdc : ps.hdc;
					if (m_colour_back != nullptr)
						::FillRect(dc, &ps.rcPaint, m_colour_back);
					else
						::FillRect(dc, &ps.rcPaint, m_wci.hbrBackground);
				}

				// Let the base class raise the event
				Control::OnPaint(args);

				// We've done the painting so we're done
				return true;
			}

			// Handle the Erase background event. Return true, to prevent anything else handling the event
			bool OnEraseBkGnd(EmptyArgs const& args) override
			{
				EraseBkGnd(*this, args);
				return true;
			}

			#pragma endregion
		};
		#pragma endregion

		#pragma region Controls
		struct Label :Control
		{
			enum { DefW = 80, DefH = 23 };
			enum :DWORD { DefaultStyle   = (DefaultControlStyle | WS_GROUP | SS_LEFT) & ~WS_TABSTOP };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"STATIC"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("lbl").wh(DefW,DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			Label(pr::gui::Params const& p = Params())
				:Control(p)
			{}
		};
		struct Button :Control
		{
			enum { DefW = 75, DefH = 23 };
			enum :DWORD { DefaultStyle       = DefaultControlStyle | WS_TABSTOP | BS_PUSHBUTTON | BS_CENTER | BS_TEXT};
			enum :DWORD { DefaultStyleDefBtn = (DefaultStyle | BS_DEFPUSHBUTTON) & ~BS_PUSHBUTTON};
			enum :DWORD { DefaultStyleEx     = DefaultControlStyleEx};
			static wchar_t const* WndClassName() { return L"BUTTON"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("btn").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			Button(pr::gui::Params const& p = Params()) :Control(p) {}

			// Events
			// Click += [&](Button&,EmptyArgs const&){}
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
					{
						// If the command came from this button
						auto ctrl_hwnd = HWND(lparam);
						if (ctrl_hwnd == m_hwnd)
						{
							switch (HiWord(wparam)) {
							case BN_CLICKED: OnClick(); return true;
							}
						}
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct CheckBox :Control
		{
			enum { DefW = 75, DefH = 23 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle | WS_TABSTOP | BS_AUTOCHECKBOX | BS_LEFT | BS_TEXT};
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx};
			static wchar_t const* WndClassName() { return L"BUTTON"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("chk").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			CheckBox(pr::gui::Params const& p = Params())
				:Control(p)
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
					{
						// If the command came from this button
						auto ctrl_hwnd = HWND(lparam);
						if (ctrl_hwnd == m_hwnd)
						{
							switch (HiWord(wparam))
							{
							case BN_CLICKED:
								OnClick();
								if (Style() & BS_AUTOCHECKBOX) OnCheckedChanged();
								return true;
							}
						}
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct TextBox :Control
		{
			enum { DefW = 80, DefH = 23 };
			enum :DWORD { DefaultStyle = DefaultControlStyle | WS_TABSTOP | WS_BORDER | ES_AUTOHSCROLL | ES_AUTOVSCROLL | ES_LEFT };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"EDIT"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("edit").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			TextBox(pr::gui::Params const& p = Params())
				:Control(p)
			{}

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

			// Get/Set the range of selected text
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

			// Select all text in the control
			void SelectAll(bool scroll_to_caret = true)
			{
				Selection(RangeI(0, -1), scroll_to_caret);
			}

			// Scroll to the caret position
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
						// If the command came from this button
						auto ctrl_hwnd = HWND(lparam);
						if (ctrl_hwnd == m_hwnd)
						{
							switch (HiWord(wparam)) {
							case EN_CHANGE: OnTextChanged(); return true;
							}
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
			enum :DWORD { DefaultStyle   = DefaultControlStyle | WS_TABSTOP | CBS_DROPDOWN | CBS_AUTOHSCROLL };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"COMBOBOX"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("combo").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			ComboBox(pr::gui::Params const& p = Params())
				:Control(p)
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

			// Remove all items from the combo drop down list
			void ResetContent()
			{
				assert(::IsWindow(m_hwnd));
				::SendMessageW(m_hwnd, CB_RESETCONTENT, 0, 0L);
			}

			// Add a string to the combo box drop down list
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
					{
						// If the command came from this button
						auto ctrl_hwnd = HWND(lparam);
						if (ctrl_hwnd == m_hwnd)
						{
							switch (HiWord(wparam)) {
							case CBN_DROPDOWN: result = OnDropDown(); return true;
							case CBN_SELCHANGE: result = OnSelectedIndexChanged(); return true;
							}
						}
						break;
					}
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct ListView :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle | LVS_ALIGNLEFT | LVS_SHOWSELALWAYS | LVS_EDITLABELS | LVS_NOLABELWRAP | LVS_REPORT };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx | LVS_EX_HEADERDRAGDROP | LVS_EX_FULLROWSELECT };//| LVS_EX_TWOCLICKACTIVATE | LVS_EX_FLATSB | LVS_EX_AUTOSIZECOLUMNS };
			static wchar_t const* WndClassName() { return L"SysListView32"; }

			// Modes for the list view
			enum class EViewType
			{
				Icon   = LVS_ICON,
				SmIcon = LVS_SMALLICON,
				List   = LVS_LIST,
				Report = LVS_REPORT,
			};

			// List Item
			struct ItemInfo :LVITEMW
			{
				ItemInfo(wchar_t const* text_ = L"") :LVITEMW() { text(text_); }
				ItemInfo(HLISTITEM item, int mask_ = 0) :LVITEMW() { iItem = item; mask = mask_; }
				ItemInfo& index(int i)                 { iItem = i; return *this; }
				ItemInfo& subitem(int i)               { iSubItem = i; return *this; }
				ItemInfo& text(wchar_t const* text_)   { mask |= LVIF_TEXT; pszText = const_cast<wchar_t*>(text_); return *this; }
				ItemInfo& image(int img_idx)           { mask |= LVIF_IMAGE; iImage = img_idx; return *this; }
				ItemInfo& state(int state_, int mask_) { mask |= LVIF_STATE; LVITEMW::state = state_; stateMask = mask_; return *this; }
				ItemInfo& user(void* ctx)              { mask |= LVIF_PARAM; lParam = LPARAM(ctx); return *this; }
			};

			// Details view column
			struct ColumnInfo :LVCOLUMNW
			{
				ColumnInfo(wchar_t const* text_ = L"", int fmt = LVCFMT_LEFT) :LVCOLUMNW() { text(text_).format(fmt); }
				ColumnInfo& text(wchar_t const* text)  { mask |= LVCF_TEXT; pszText = const_cast<wchar_t *>(text); return *this; }
				ColumnInfo& width(int w)               { mask |= LVCF_WIDTH; cx = w; return *this; }
				ColumnInfo& format(int lvcfmt)         { mask |= LVCF_FMT; fmt = lvcfmt; return *this; }
				ColumnInfo& subitem(int i)             { mask |= LVCF_SUBITEM; iSubItem = i; return *this; }
				ColumnInfo& image(int img_idx)         { mask |= LVCF_IMAGE; iImage = img_idx; return *this; }
				ColumnInfo& min_width(int w)           { mask |= LVCF_MINWIDTH; cxMin = w; return *this; }
				ColumnInfo& def_width(int w)           { mask |= LVCF_DEFAULTWIDTH; cxDefault = w; return *this; }
				ColumnInfo& ideal_width(int w)         { mask |= LVCF_IDEALWIDTH; cxIdeal = w; return *this; }
			};

			struct Params :CtrlParams
			{
				Params() { mode(EViewType::Report).wndclass(WndClassName()).name("listview").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
				Params& mode(EViewType mode) { m_style = (m_style & ~LVS_TYPEMASK) | (DWORD(mode) & LVS_TYPEMASK); return *this; }
			};

			ListView(pr::gui::Params const& p = Params())
				:Control(p)
			{
				DoubleBuffered(true);
			}

			// Get/Set view type.
			EViewType ViewType() const
			{
				assert(::IsWindow(m_hwnd));
				return EViewType(Style() & LVS_TYPEMASK);
			}
			void ViewType(EViewType vt)
			{
				assert(::IsWindow(m_hwnd));
				Style((Style() & ~LVS_TYPEMASK) | (DWORD(vt) & LVS_TYPEMASK));
			}

			// Remove all items
			void Clear()
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_DELETEALLITEMS, 0, 0L), "Delete all list items failed");
			}

			// Get the number of elements in the list
			size_t ItemCount() const
			{
				assert(::IsWindow(m_hwnd));
				return (size_t)::SendMessageW(m_hwnd, LVM_GETITEMCOUNT, 0, 0L);
			}

			// Get the number of selected list items
			size_t SelectedCount() const
			{
				assert(::IsWindow(m_hwnd));
				return (size_t)::SendMessageW(m_hwnd, LVM_GETSELECTEDCOUNT, 0, 0L);
			}

			// Returns the next item with state matching 'flags' (e.g. LVNI_SELECTED) or -1
			HLISTITEM NextItem(int flags, HLISTITEM item = -1) const
			{
				assert(::IsWindow(m_hwnd));
				return (int)::SendMessageW(m_hwnd, LVM_GETNEXTITEM, item, MakeLParam(flags, 0));
			}

			// Add a row to the list
			HLISTITEM InsertItem(ItemInfo const& info)
			{
				assert(::IsWindow(m_hwnd));
				return (HLISTITEM)::SendMessageW(m_hwnd, LVM_INSERTITEMW, 0, (LPARAM)&info);
			}

			// Remove an item from the list
			bool DeleteItem(HLISTITEM item)
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessageW(m_hwnd, LVM_DELETEITEM, item, 0L) != 0;
			}

			// Get/Set an item. Construct 'info' with the item handle and mask for the data you want to get/set
			ItemInfo Item(ItemInfo info) const
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_GETITEMW, 0, (LPARAM)&info), "Get list item failed");
				return info;
			}
			void Item(ItemInfo info)
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_SETITEMW, 0, (LPARAM)&info), "Set list item failed");
			}

			// Get/Set the state of an item
			UINT ItemState(HLISTITEM item, UINT state_mask) const
			{
				assert(::IsWindow(m_hwnd));
				return (UINT)::SendMessageW(m_hwnd, LVM_GETITEMSTATE, (WPARAM)item, (LPARAM)state_mask) & state_mask;
			}
			void ItemState(HLISTITEM item, int state, int state_mask)
			{
				assert(::IsWindow(m_hwnd));
				auto info = ItemInfo(item).state(state, state_mask);
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_SETITEMSTATE, item, (LPARAM)&info), "Set list item state failed");
			}

			// Scroll an item into view
			void EnsureVisible(HLISTITEM item, bool partial_ok)
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_ENSUREVISIBLE, (WPARAM)item, MakeLParam(partial_ok, 0)), "Ensure list item is visible failed");
			}

			// Get/Set user data on the item
			template <typename T> T* UserData(HLISTITEM item) const
			{
				assert(item != INVALID_LIST_ITEM);
				return reinterpret_cast<T*>(Item(ItemInfo(item, LVIF_PARAM)).lParam);
			}
			void UserData(HLISTITEM item, void* ctx)
			{
				Item(ItemInfo(item).user(ctx));
			}

			#pragma region Columns
			// Get the number of columns in the list
			size_t ColumnCount() const
			{
				assert(::IsWindow(m_hwnd));
				auto hdr = (HWND)::SendMessageW(m_hwnd, LVM_GETHEADER, 0, 0L);
				return (size_t)::SendMessageW(hdr, HDM_GETITEMCOUNT, 0, 0L);
			}

			// Insert a column into the list
			void InsertColumn(int idx, ColumnInfo const& column)
			{
				assert(::IsWindow(m_hwnd));
				Throw(::SendMessageW(m_hwnd, LVM_INSERTCOLUMNW, idx, (LPARAM)&column) != -1, "Insert column failed.");
			}

			// Get/Set the width of a column
			int ColumnWidth(int col) const
			{
				assert(::IsWindow(m_hwnd));
				return (int)::SendMessageW(m_hwnd, LVM_GETCOLUMNWIDTH, col, 0L);
			}
			void ColumnWidth(int col, int width) // use 'LVSCW_AUTOSIZE'
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, LVM_SETCOLUMNWIDTH, col, MakeLParam(width, 0)), "Set list column width failed");
			}
			#pragma endregion
		};
		struct TreeView :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle | TVS_EDITLABELS | TVS_HASBUTTONS | TVS_HASLINES | TVS_LINESATROOT | TVS_DISABLEDRAGDROP | TVS_SHOWSELALWAYS | TVS_FULLROWSELECT | TVS_NOSCROLL };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"SysTreeView32"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("treeview").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			// Next item codes
			enum EItem
			{
				Root         = TVGN_ROOT,
				Next         = TVGN_NEXT,
				Prev         = TVGN_PREVIOUS,
				Parent       = TVGN_PARENT,
				Child        = TVGN_CHILD,
				FirstVisible = TVGN_FIRSTVISIBLE,
				NextVisible  = TVGN_NEXTVISIBLE,
				PrevVisible  = TVGN_PREVIOUSVISIBLE,
				LastVisible  = TVGN_LASTVISIBLE,
				NextSelected = 0x000B, //TVGN_NEXTSELECTED,
				DropHilite   = TVGN_DROPHILITE,
				Caret        = TVGN_CARET,
			};

			// Expand or collapse codes
			enum EExpand
			{
				Collapse = TVE_COLLAPSE,
				Expand = TVE_EXPAND,
				Toggle = TVE_TOGGLE,

				// Flags that can be combined with above
				ExpandPartial = TVE_EXPANDPARTIAL,
				CollapseReset = TVE_COLLAPSERESET,
			};

			// Tree item
			struct ItemInfo :TVITEMEXW
			{
				ItemInfo(wchar_t const* text_ = L"") :TVITEMEXW() { text(text_); }
				ItemInfo(HTREEITEM item, int mask_ = 0) :TVITEMEXW() { hItem = item; mask = mask_; }
				ItemInfo& text(wchar_t const* text_)   { mask |= TVIF_TEXT; pszText = const_cast<wchar_t*>(text_); return *this; }
				ItemInfo& image(int img_idx)           { mask |= TVIF_IMAGE; iImage = img_idx; return *this; }
				ItemInfo& image_sel(int img_idx)       { mask |= TVIF_SELECTEDIMAGE; iSelectedImage = img_idx; return *this; }
				ItemInfo& state(int state_, int mask_) { mask |= TVIF_STATE; TVITEMEXW::state = state_; stateMask = mask_; return *this; }
				ItemInfo& user(void* ctx)              { mask |= TVIF_PARAM; lParam = LPARAM(ctx); return *this; }
			};

			TreeView(pr::gui::Params const& p = Params())
				:Control(p)
			{}

			// Remove all items
			void Clear()
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, TVM_DELETEITEM, 0, (LPARAM)TVI_ROOT), "Delete all tree items failed");
			}

			// Return the root, next sibling, previous sibling, child, parent, etc item relative to 'item'
			// 'item' can be null for Root, and FirstXYZ codes.
			HTREEITEM NextItem(EItem code, HTREEITEM item = nullptr) const
			{
				assert(::IsWindow(m_hwnd));
				return (HTREEITEM)::SendMessageW(m_hwnd, TVM_GETNEXTITEM, (WPARAM)code, (LPARAM)item);
			}

			// Insert an item into the tree
			HTREEITEM InsertItem(ItemInfo const& info, HTREEITEM parent, HTREEITEM insert_after)
			{
				assert(::IsWindow(m_hwnd));
				auto ins = TVINSERTSTRUCTW{};
				ins.hParent = parent;
				ins.hInsertAfter = insert_after;
				ins.itemex = info;
				return (HTREEITEM)::SendMessageW(m_hwnd, TVM_INSERTITEMW, 0, (LPARAM)&ins);
			}

			// Delete an item and it's children from the tree
			bool DeleteItem(HTREEITEM item)
			{
				assert(::IsWindow(m_hwnd));
				return ::SendMessageW(m_hwnd, TVM_DELETEITEM, 0, (LPARAM)item) != 0;
			}

			// Get/Set an item. Construct 'info' with the item handle and mask for the data you want to get/set
			ItemInfo Item(ItemInfo info) const
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, TVM_GETITEMW, 0, (LPARAM)&info), "Get tree item failed");
				return info;
			}
			void Item(ItemInfo info)
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, TVM_SETITEMW, 0, (LPARAM)&info), "Set tree item failed");
			}

			// Get/Set the state of an item
			UINT ItemState(HTREEITEM item, UINT state_mask) const
			{
				assert(::IsWindow(m_hwnd));
				return (UINT)::SendMessageW(m_hwnd, TVM_GETITEMSTATE, (WPARAM)item, (LPARAM)state_mask) & state_mask;
			}
			void ItemState(HTREEITEM item, int state, int state_mask)
			{
				Item(ItemInfo(item).state(state, state_mask));
			}

			// Scroll an item into view
			void EnsureVisible(HTREEITEM item)
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, TVM_ENSUREVISIBLE, 0, (LPARAM)item), "Ensure tree item is visible failed");
			}

			// Get/Set user data on the item
			template <typename T> T* UserData(HTREEITEM item) const
			{
				assert(item != INVALID_TREE_ITEM);
				return reinterpret_cast<T*>(Item(ItemInfo(item, TVIF_PARAM)).lParam);
			}
			void UserData(HTREEITEM item, void* ctx)
			{
				Item(ItemInfo(item).user(ctx));
			}

			// Expand or collapse a node in the tree
			void ExpandItem(HTREEITEM item, EExpand code)
			{
				assert(::IsWindow(m_hwnd));
				Throw((BOOL)::SendMessageW(m_hwnd, TVM_EXPAND, code, (LPARAM)item), "Expand tree node failed");
			}

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				//switch (message)
				//{
				//case WM_WINDOWPOSCHANGED:
				//	{
				//		OutputDebugStringA(FmtS("Tree %s resize\n", m_name.c_str()));
				//		break;
				//	}
				//}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}
		};
		struct ProgressBar :Control
		{
			enum { DefW = 100, DefH = 23 };
			enum :DWORD { DefaultStyle   = (DefaultControlStyle | PBS_SMOOTH) & ~WS_TABSTOP };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"msctls_progress32"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("progress").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			ProgressBar(pr::gui::Params const& p = Params())
				:Control(p)
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
				return int(short(LoWord(::SendMessageW(m_hwnd, PBM_SETPOS, pos, 0L))));
			}

			// Move the bar position by a delta
			int OffsetPos(int delta)
			{
				assert(::IsWindow(m_hwnd));
				return int(short(LoWord(::SendMessageW(m_hwnd, PBM_DELTAPOS, delta, 0L))));
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
				return int(short(LoWord(::SendMessageW(m_hwnd, PBM_SETSTEP, step_size, 0L))));
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
				return int(short(LoWord(::SendMessageW(m_hwnd, PBM_STEPIT, 0, 0L))));
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
					if (LoWord(wparam) != m_id) break;
					switch (HiWord(wparam))
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
			enum :DWORD { DefaultStyle   = DefaultControlStyle };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"pr::gui::Panel"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(RegisterWndClass<Panel>()).name("panel").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			Panel(pr::gui::Params const& p = Params())
				:Control(p)
			{}

			// Handle the Paint event. Return true, to prevent anything else handling the event
			bool OnPaint(PaintEventArgs const& args) override
			{
				//auto r = Rect{};
				//::GetUpdateRect(m_hwnd, &r, FALSE);

				HRGN rgn = ::CreateRectRgn(0,0,0,0);
				::GetUpdateRgn(m_hwnd, rgn, FALSE);

				auto res = Control::OnPaint(args);

				{
					ClientDC dc(m_hwnd);
					Brush b(0x00FFFF);
					auto cr = ClientRect();
					::FrameRgn(dc, rgn, b, cr.width(), cr.height());
				}

				return res;
			}
		};
		struct GroupBox :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle | BS_GROUPBOX };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"BUTTON"; } // yes, groupbox's use the button window class
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("grp").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			GroupBox(pr::gui::Params const& p = Params())
				:Control(p)
			{}
		};
		struct RichTextBox :TextBox
		{
			static wchar_t const* WndClassName() { return ::LoadLibraryW(L"msftedit.dll") ? L"RICHEDIT50W" : L"RICHEDIT20W"; }
			struct Params :TextBox::Params
			{
				Params() { wndclass(WndClassName()).name("richedit"); }
			};

			RichTextBox(pr::gui::Params const& p = Params())
				:TextBox(p)
			{}
		};
		struct StatusBar :Control
		{
			enum :DWORD { DefaultStyle   = DefaultControlStyle | WS_CLIPCHILDREN | WS_CLIPSIBLINGS | SBARS_SIZEGRIP };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return STATUSCLASSNAMEW; }
			struct Params :CtrlParams
			{
				Params() { create(ECreate::Defer).wndclass(WndClassName()).name("status").style(DefaultStyle).style_ex(DefaultStyleEx).anchor(EAnchor::LeftBottomRight).dock(EDock::Bottom); }
			};

			StatusBar(pr::gui::Params const& p = Params())
				:Control(p)
			{
				Attach(::CreateStatusWindowW(p.m_style, p.m_text, p.m_parent, p.m_id));
				Throw(IsWindow(m_hwnd), "Failed to create the status bar");

				// Don't set the parent until we have an hwnd
				Parent(p.m_parent);
				Dock(p.m_dock);
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
				s.resize(LoWord(::SendMessageW(m_hwnd, SB_GETTEXTLENGTH, WPARAM(pane), LPARAM(0))) + 1, 0);
				if (!s.empty())
				{
					auto ret = DWORD(::SendMessageW(m_hwnd, SB_GETTEXT, WPARAM(pane), LPARAM(&s[0])));
					if (type) *type = (int)(short)HiWord(ret);
					s.resize(LoWord(ret));
				}
				return s;
			}
			void Text(int pane, string text, int type = 0)
			{
				assert(::IsWindow(m_hwnd) && pane >= 0 && pane < 256);
				Throw(::SendMessageW(m_hwnd, SB_SETTEXTW, WPARAM(MakeLong(MakeWord(pane, type), 0)), LPARAM(text.c_str())) != 0, "Failed to set status bar pane text");
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

			enum { DefW = 80, DefH = 80 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle }; // TCS_VERTICAL | TCS_RIGHT | TCS_BOTTOM
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"SysTabControl32"; }
			struct Params :CtrlParams
			{
				Params() { wndclass(WndClassName()).name("tabctrl").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx); }
			};

			std::vector<Control*> m_tabs; // The tab pages. Owned externally

			TabControl(pr::gui::Params const& p = Params())
				:Control(p)
				,m_tabs()
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

				// Save the index of the currently selected
				auto sel = SelectedIndex();

				// Insert the item at the end of the tab control
				index = index != -1 ? index : TabCount();
				index = int(::SendMessageW(m_hwnd, TCM_INSERTITEMW, WPARAM(index), LPARAM(&item)));
				Throw(index != -1, pr::FmtS("Failed to add tab %s", Narrow(label).c_str()));

				// Add the tab
				m_tabs.push_back(&tab);
				tab.Parent(this);

				// Resize it appropriately
				LayoutTab(tab, ClientRect());

				// Select the tab that is being added, if desired
				if (active)
					SwitchTab(sel, index, true);

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

			// Update the position of all the contained windows.
			// 'client_rect' is the client area of this tab control (or what it will be soon)
			void UpdateLayout(Rect const& client_rect, bool repaint = false)
			{
				for (auto tab : m_tabs)
					LayoutTab(*tab, client_rect, repaint);
			}
			void UpdateLayout(bool repaint = false)
			{
				UpdateLayout(ClientRect(), repaint);
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

			// The client rect for a tab control, excludes the tabs
			Rect ClientRect() const override
			{
				auto cr = Control::ClientRect();
				::SendMessageW(m_hwnd, TCM_ADJUSTRECT, FALSE, LPARAM(&cr));

				// TC has built in padding, we want to remove that and use our own padding
				// There is also a 2px 3D border
				auto style = Style();
				if      ((style & TCS_BOTTOM) && !(style & TCS_VERTICAL)) cr = cr.Adjust(-4, -4, +2, +1); // Bottom
				else if ((style & TCS_RIGHT ) &&  (style & TCS_VERTICAL)) cr = cr.Adjust(-4, -4, +2, +4); // Right
				else if (                         (style & TCS_VERTICAL)) cr = cr.Adjust(-2, -4, +4, +4); // Left
				else                                                      cr = cr.Adjust(-3, -1, +1, +2); // Top
				return cr;
			}

		protected:

			// Message map function
			bool ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result) override
			{
				switch (message)
				{
				case WM_NOTIFY:
					#pragma region
					{
						auto hdr = reinterpret_cast<NMHDR*>(lparam);
						if (hdr->hwndFrom == m_hwnd)
						{
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
							return true;
						}
						break;
					}
					#pragma endregion
				}
				return Control::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
			}

			// Resize a tab to fit this control
			void LayoutTab(Control& tab, Rect const& client_rect, bool repaint = false)
			{
				tab.ParentRect(client_rect.Adjust(-tab.Margin()), repaint, nullptr, EWindowPos::NoZorder);
			}

			// Throw if 'tab_index' is invalid
			void ValidateTabIndex(int tab_index) const
			{
				Throw(tab_index >= 0 && tab_index < TabCount(), pr::FmtS("Tab index (%d) out of range", tab_index));
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

			// Handle window size changing starting or stopping
			void OnWindowPosChange(SizeEventArgs const& args) override
			{
				if (args.m_before)
				{
					// This control is about to resize, resize the child windows to the new size
					auto& new_size = args.m_pos;
					auto b = ParentRect();
					auto c = ClientRect();
					auto rect = Rect(c.left, c.top, c.right + (new_size.cx - b.width()), c.bottom + (new_size.cy - b.height()));
					UpdateLayout(rect);
				}
				Control::OnWindowPosChange(args);
			}
		};
		struct Splitter :Control
		{
			enum { DefW = 80, DefH = 80 };
			enum :DWORD { DefaultStyle   = DefaultControlStyle };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"pr::gui::Splitter"; }
			struct Params :CtrlParams
			{
				int m_bar_width;
				float m_bar_pos;
				int m_min_pane_size;
				bool m_vertical;
				bool m_full_drag;
				
				Params() :CtrlParams() ,m_bar_width(4) ,m_bar_pos(0.5f) ,m_min_pane_size(20) ,m_vertical(false) ,m_full_drag()
				{
					::SystemParametersInfoW(SPI_GETDRAGFULLWINDOWS, 0, &m_full_drag, 0);
					wndclass(RegisterWndClass<Splitter>()).name("split").wh(DefW, DefH).style(DefaultStyle).style_ex(DefaultStyleEx);
				}
				Params& width(int w)          { m_bar_width = w; return *this; }
				Params& pos(float p)          { m_bar_pos = std::min(1.0f, std::max(0.0f, p)); return *this; }
				Params& min_pane_width(int w) { m_min_pane_size = w; return *this; }
				Params& vertical()            { m_vertical = true; return *this; }
				Params& horizontal()          { m_vertical = false; return *this; }
				Params& full_drag(bool fd)    { m_full_drag = fd; return *this; }
			};

			Panel Pane0;
			Panel Pane1;
			bool m_vertical;
			bool m_full_drag;
			int m_bar_width;
			float m_bar_pos;
			int m_min_pane_size;
			HCURSOR m_cursor;

			Splitter(pr::gui::Params const& p = Params())
				:Control(p)
				,Pane0(Panel::Params().parent(this).name(FmtS("%s-L",p.name())).anchor(EAnchor::None).bk_col(::GetSysColor(COLOR_APPWORKSPACE)))
				,Pane1(Panel::Params().parent(this).name(FmtS("%s-R",p.name())).anchor(EAnchor::None).bk_col(::GetSysColor(COLOR_APPWORKSPACE)))
				,m_vertical(static_cast<Params const&>(p).m_vertical)
				,m_full_drag(static_cast<Params const&>(p).m_full_drag)
				,m_bar_width(static_cast<Params const&>(p).m_bar_width)
				,m_bar_pos(static_cast<Params const&>(p).m_bar_pos)
				,m_min_pane_size(static_cast<Params const&>(p).m_min_pane_size)
				,m_cursor(::LoadCursor(nullptr, m_vertical ? IDC_SIZEWE : IDC_SIZENS))
			{
				if (IsWindow(*this))
					UpdateLayout();
			}

			// Get/Set the bar position as a fraction
			float BarPos() const
			{
				return m_bar_pos;
			}
			void BarPos(float pos, bool repaint = false)
			{
				// Get the available client size based on orientation
				auto w = m_vertical ? ClientRect().width() : ClientRect().height();
				if (w > 0)
				{
					auto f = 2*w > m_min_pane_size ? m_min_pane_size / (float)w : 0.5f;
					m_bar_pos = std::min(1.0f - f, std::max(0.0f + f, pos));
					UpdateLayout(repaint);
				}
			}

			// Update the layout of child windows
			// 'client_rect' is the client area of this control (or what it will be soon)
			void UpdateLayout(Rect const& client_rect, bool repaint = false)
			{
				auto bar_pos = BarPos();
				Pane0.Visible(bar_pos != 0.0f);
				Pane1.Visible(bar_pos != 1.0f);

				// Invalidate the current area of the splitter bar
				auto bar_rect = BarRect();
				Invalidate(false, &bar_rect);

				// Update the size of the child panes
				if (Pane0.Visible())
					Pane0.ParentRect(PaneRect(0, client_rect), repaint);
				if (Pane1.Visible())
					Pane1.ParentRect(PaneRect(1, client_rect), repaint);
			}
			void UpdateLayout(bool repaint = false)
			{
				UpdateLayout(ClientRect(), repaint);
			}

		protected:

			// Return the rect for the bar in client space
			Rect BarRect(Rect client) const
			{
				if (BarPos() == 0.0f)
					return m_vertical
						? Rect(client.left, client.top, client.left, client.bottom)
						: Rect(client.left, client.top, client.right, client.top);

				if (BarPos() == 1.0f)
					return m_vertical
						? Rect(client.right, client.top, client.right, client.bottom)
						: Rect(client.left, client.bottom, client.right, client.bottom);

				auto hw = m_bar_width * 0.5f;
				return m_vertical
					? Rect(client.left + int(client.width() * BarPos() - hw), client.top, client.left +  int(client.width() * BarPos() + hw), client.bottom)
					: Rect(client.left, client.top + int(client.height() * BarPos() - hw), client.right, client.top + int(client.height() * BarPos() + hw));
			}
			Rect BarRect() const
			{
				return BarRect(ClientRect());
			}

			// Return the rect for a pane in client space
			Rect PaneRect(int idx, Rect client) const
			{
				auto bar = BarRect(client);
				switch (idx)
				{
				default:
					assert(false);
					return Rect();
				case 0:
					return m_vertical
						? Rect(client.left, client.top, bar.left, client.bottom)
						: Rect(client.left, client.top, client.right, bar.top);
				case 1:
					return m_vertical
						? Rect(bar.right, client.top, client.right, client.bottom)
						: Rect(client.left, bar.bottom, client.right, client.bottom);
				}
			}
			Rect PaneRect(int idx) const
			{
				return PaneRect(idx, ClientRect());
			}

			// Draw the ghost bar. Note, drawing twice 'undraws' the ghost bar
			void DrawGhostBar()
			{
				auto rect = BarRect();
				if (!rect.empty())
				{
					// convert client to window coordinates
					auto wndrect = ScreenRect();
					::MapWindowPoints(nullptr, m_hwnd, (LPPOINT)&wndrect, 2);
					::OffsetRect(&rect, -wndrect.left, -wndrect.top);

					// invert the brush pattern (looks just like frame window sizing)
					WindowDC dc(m_hwnd);
					auto brush = Brush::Halftone();

					// Paint the ghost bar
					auto old = ::SelectObject(dc, brush);
					::PatBlt(dc, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, PATINVERT);
					::SelectObject(dc, old);
				}
			}

			// Handle window size changing starting or stopping
			void OnWindowPosChange(SizeEventArgs const& args) override
			{
				if (args.m_before)
				{
					// This control is about to resize, resize the child windows to the new size
					auto& new_size = args.m_pos;
					auto b = ParentRect();
					auto c = ClientRect();
					auto rect = Rect(c.left, c.top, c.right + (new_size.cx - b.width()), c.bottom + (new_size.cy - b.height()));
					UpdateLayout(rect);
				}
				Control::OnWindowPosChange(args);
			}

			// Handle the Paint event. Return true, to prevent anything else handling the event
			bool OnPaint(PaintEventArgs const& args) override
			{
				Control::OnPaint(args);

				PaintStruct p(m_hwnd);
				auto hdc = args.m_alternate_hdc != nullptr ? args.m_alternate_hdc : p.hdc;

				// Draw the splitter bar
				if (BarPos() != 0.0f && BarPos() != 1.0f)
				{
					auto rect = BarRect();
					::FillRect(hdc, &rect, ::GetSysColorBrush(COLOR_3DFACE));
				}
				return true;
			}

			// Handle mouse button down/up events. Return true, to prevent anything else handling the event
			bool OnMouseButton(MouseEventArgs const& args) override
			{
				Control::OnMouseButton(args);
				if (args.m_down)
				{
					auto pt = args.m_point;
					auto bar_rect = BarRect();
					if (::GetCapture() != m_hwnd && bar_rect.Contains(pt))
					{
						::SetCapture(m_hwnd);
						::SetCursor(m_cursor);

						if (m_full_drag)
						{}
						else
							DrawGhostBar();
					}
					else if (::GetCapture() == m_hwnd && !bar_rect.Contains(pt))
					{
						// If we have capture but are not over the splitter, this is
						// the case where we alt-tab during a splitter bar drag.
						::ReleaseCapture();
					}
				}
				else
				{
					if (::GetCapture() == m_hwnd)
						::ReleaseCapture();
				}
				return false;
			}

			// Handle mouse move events
			void OnMouseMove(MouseEventArgs const& args) override
			{
				Control::OnMouseMove(args);

				auto pt = args.m_point;
				auto bar_rect = BarRect();
				if (::GetCapture() == m_hwnd)
				{
					auto client = ClientRect();
					auto pos = m_vertical
						? float(pt.x - client.left) / client.width()
						: float(pt.y - client.top ) / client.height();

					if (pos != BarPos())
					{
						if (m_full_drag)
						{
							BarPos(pos, true);
						}
						else
						{
							DrawGhostBar();
							BarPos(pos);
							DrawGhostBar();
						}
					}
				}
				else // Not dragging, just hovering
				{
					if (bar_rect.Contains(pt))
						::SetCursor(m_cursor);
				}
			}
		};
		#pragma endregion

		#pragma region Dialogs
		// Options for the Open/Save file ui functions
		struct FileUIOptions
		{
			wchar_t const*           m_def_extn;       // The default extension, e.g. L"txt". It seems a list is supported, e.g. L"doc;docx"
			size_t                   m_filter_count;   // The length of the 'm_filters' array
			COMDLG_FILTERSPEC const* m_filters;        // File type filters, e.g. COMDLG_FILTERSPEC spec[] = {{L"JayPegs",L"*.jpg;*.jpeg"}, {L"Bitmaps", L"*.bmp"}, {L"Whatever", L"*.*"}};
			size_t                   m_filter_index;   // The index to select from the filters
			DWORD                    m_flags;          // Additional options
			IFileDialogEvents*       m_handler;        // A handler for events generated using the use of the file dialog
			mutable DWORD            m_handler_cookie; // Used to identify the handler when registered. Leave this as 0

			FileUIOptions(wchar_t const* def_extn = nullptr, size_t filter_count = 0, COMDLG_FILTERSPEC const* filters = nullptr, size_t filter_index = 0, DWORD flags = 0, IFileDialogEvents* handler = nullptr)
				:m_def_extn(def_extn)
				,m_filter_count(filter_count)
				,m_filters(filters)
				,m_filter_index(filter_index)
				,m_flags(flags)
				,m_handler(handler)
				,m_handler_cookie()
			{}
		};

		// Open or SaveAs file dialog. Returns true if the user did not cancel
		template <typename ResultPred> bool FileUI(CLSID type, HWND parent, FileUIOptions const& opts, ResultPred results)
		{
			// see: https://msdn.microsoft.com/en-us/library/windows/desktop/bb776913(v=vs.85).aspx

			// CoCreate the File Open Dialog object.
			CComPtr<IFileDialog> fd;
			Throw(fd.CoCreateInstance(type), "CoCreateInstance failed. Ensure CoInitialize has been called");

			// Hook up the event handler.
			struct EvtHook
			{
				IFileDialog* m_fd;
				FileUIOptions const* m_opts;
				EvtHook(IFileDialog* fd, FileUIOptions const& opts) :m_fd(fd) ,m_opts()
				{
					if (opts.m_handler == nullptr) return;
					Throw(m_fd->Advise(opts.m_handler, &opts.m_handler_cookie), "Failed to assign file open/save event handler");
					m_opts = &opts; // use the saved pointer to indicate unadvise is needed
				}
				~EvtHook()
				{
					if (m_opts == nullptr) return;
					Throw(m_fd->Unadvise(m_opts->m_handler_cookie), "Failed to unregister file open/save dialog event handler");
				}
			} evt_hook(fd.p, opts);

			// Set the options on the dialog.
			if (opts.m_flags != 0)
			{
				// Before setting, always get the options first in order not to override existing options.
				DWORD flags;
				Throw(fd->GetOptions(&flags), "Failed to set file open/save dialog options");
				Throw(fd->SetOptions(flags | opts.m_flags), "");
			}

			// Set the file types to display only.
			if (opts.m_filter_count != 0)
			{
				Throw(fd->SetFileTypes(UINT(opts.m_filter_count), opts.m_filters), "Failed to set file type filters");
				Throw(fd->SetFileTypeIndex(UINT(opts.m_filter_index)), "Failed to set the file type filter index");
			}

			// Set the default extension to be ".doc" file.
			if (opts.m_def_extn != nullptr)
				Throw(fd->SetDefaultExtension(opts.m_def_extn), "Failed to set the default file extension");

			// Show the dialog
			auto r = fd->Show(parent);
			if (r == HRESULT_FROM_WIN32(ERROR_CANCELLED)) return false;
			if (r != S_OK) Throw(r, "Failed to show the file open/save dialog");

			// Pass the dialog to 'results' to allow the caller to get what they want
			return results(fd.p);
		}

		// Present the Open file dialog and return the selected filepath
		inline std::vector<std::wstring> OpenFileUI(HWND parent = nullptr, FileUIOptions const& opts = FileUIOptions())
		{
			std::vector<std::wstring> results;
			FileUI(CLSID_FileOpenDialog, parent, opts, [&](IFileDialog* fd)
				{
					auto fod = static_cast<IFileOpenDialog*>(fd);

					// Obtain the results once the user clicks the 'Open' button. The result is an IShellItem object.
					CComPtr<IShellItemArray> items;
					Throw(fod->GetResults(&items), "Failed to retrieve the array of results from the file open dialog result");
					if (items != nullptr)
					{
						DWORD count;
						Throw(items->GetCount(&count), "Failed to read the number of results from the file open dialog result");
						for (DWORD i = 0; i != count; ++i)
						{
							CComPtr<IShellItem> item;
							Throw(items->GetItemAt(i, &item), pr::FmtS("Failed to read result %d from the file open dialog results", i));
						
							wchar_t* fpath;
							Throw(item->GetDisplayName(SIGDN_FILESYSPATH, &fpath), "Failed to read the filepath from an open file dialog result");
							results.emplace_back(fpath);
							CoTaskMemFree(fpath);
						}
					}
					else
					{
						CComPtr<IShellItem> item;
						Throw(fod->GetResult(&item), "Failed to read result from the file open dialog results");

						wchar_t* fpath;
						Throw(item->GetDisplayName(SIGDN_FILESYSPATH, &fpath), "Failed to read the filepath from an open file dialog result");
						results.emplace_back(fpath);
						CoTaskMemFree(fpath);
					}
					return true;
				});
			return results;
		}

		// Present the SaveAs file dialog and return the selected filepath
		inline std::wstring SaveFileUI(HWND parent = nullptr, FileUIOptions const& opts = FileUIOptions())
		{
			std::wstring filepath;
			FileUI(CLSID_FileSaveDialog, parent, opts, [&](IFileDialog* fd)
				{
					auto fsd = static_cast<IFileSaveDialog*>(fd);
					
					CComPtr<IShellItem> res;
					Throw(fsd->GetResult(&res.p), "Failed to read result %d from the file save dialog result");

					wchar_t* fpath;
					Throw(res->GetDisplayName(SIGDN_FILESYSPATH, &fpath), "Failed to read the filepath from a the save file dialog result");
					filepath = std::wstring(fpath);
					CoTaskMemFree(fpath);
					return true;
				});
			return filepath;
		}
		#pragma endregion

		#pragma region Misc
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
