//*******************************************************************************************
// Colour32
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
#pragma once
#include <stdexcept>
#include <stdint.h>
#include <windows.h>
#include <d2d1.h>

namespace pr
{
	// Notes:
	//  - Use 'DpiForWindow' if possible. It handles graceful fall-back.
	//  - Assumes DPIX == DPIY

	#if 0 // This causes deprecation errors nowadays, but it might be useful for supporting old windows one day
	using HMONITOR = void*;

	// Flags for the MonitorFromPoint function
	enum class EMonitorFromFlags :DWORD
	{
		DEFAULT_TO_NULL    = 0x00000000,
		DEFAULT_TO_PRIMARY = 0x00000001,
		DEFAULT_TO_NEAREST = 0x00000002,
	};

	enum class EMonitorDpiType
	{
		Effective = 0,
		Angular = 1,
		Raw = 2,
	};

	// Return the monitor that contains the given window handle
	inline HMONITOR MonitorFromWindow(HWND hwnd, EMonitorFromFlags flags = EMonitorFromFlags::DEFAULT_TO_NEAREST)
	{
		auto h = LoadLibraryA("user32.dll");
		using MonitorFromWindowFn = HMONITOR(*)(HWND, DWORD);
		auto ptr = reinterpret_cast<MonitorFromWindowFn>(GetProcAddress(h, "MonitorFromWindow")); // Windows 10, version 10.0.14393
		return ptr != nullptr
			? ptr(hwnd, static_cast<DWORD>(flags))
			: throw std::runtime_error("This version of windows doesn't support 'MonitorFromWindow'. Windows 10, version 10.0.14393 required");
	}

	// Return the monitor that contains the given point
	inline HMONITOR MonitorFromPoint(POINT pt, EMonitorFromFlags flags = EMonitorFromFlags::DEFAULT_TO_NEAREST)
	{
		auto h = LoadLibraryA("user32.dll");
		using MonitorFromPointFn = HMONITOR(*)(POINT, DWORD);
		auto ptr = reinterpret_cast<MonitorFromPointFn>(GetProcAddress(h, "MonitorFromPoint")); // Windows 10 1607
		return ptr != nullptr
			? ptr(pt, static_cast<DWORD>(flags))
			: throw std::runtime_error("This version of windows doesn't support 'MonitorFromPoint'. Windows 10, version 10.0.14393 required");
	}
	inline HMONITOR MonitorFromPoint(int x, int y, EMonitorFromFlags flags = EMonitorFromFlags::DEFAULT_TO_NEAREST)
	{
		return MonitorFromPoint(POINT{x, y}, flags);
	}
	
	// Return the monitor that contains the given rectangle
	inline HMONITOR MonitorFromPoint(RECT rc, EMonitorFromFlags flags = EMonitorFromFlags::DEFAULT_TO_NEAREST)
	{
		auto h = LoadLibraryA("user32.dll");
		using MonitorFromRectFn = HMONITOR(*)(RECT, DWORD);
		auto ptr = reinterpret_cast<MonitorFromRectFn>(GetProcAddress(h, "MonitorFromRect")); // Windows 10 1607
		return ptr != nullptr
			? ptr(rc, static_cast<DWORD>(flags))
			: throw std::runtime_error("This version of windows doesn't support 'MonitorFromRect'. Windows 10, version 10.0.14393 required");
	}

	// Return the monitor associated with the desktop window
	inline HMONITOR DesktopMonitor()
	{
		return MonitorFromWindow(GetDesktopWindow());
	}

	// Return the monitor associated with the shell window
	inline HMONITOR ShellMonitor()
	{
		return MonitorFromWindow(GetShellWindow());
	}

	// Get the DPI setting for the whole desktop
	inline int DpiForDesktop()
	{
		ID2D1Factory* factory;
		auto hr = D2D1CreateFactory(D2D1_FACTORY_TYPE::D2D1_FACTORY_TYPE_SINGLE_THREADED, __uuidof(ID2D1Factory), nullptr, (void**)&factory);
		if (hr < 0)
			return 96; // we really hit the ground, don't know what to do next!

		float x,y;
		factory->GetDesktopDpi(&x, &y); // Windows 7
		factory->Release();
		return (int)x;
	}

	// Get the DPI for a monitor
	inline int DpiForMonitor(HMONITOR monitor, EMonitorDpiType type = EMonitorDpiType::Effective)
	{
		auto h = LoadLibraryA("shcore.dll");
		using GetDpiForMonitorFn = int(*)(HMONITOR hmonitor, EMonitorDpiType dpiType, uint32_t* dpiX, uint32_t* dpiY);
		auto ptr = reinterpret_cast<GetDpiForMonitorFn>(GetProcAddress(h, "GetDpiForMonitor")); // Windows 8.1
		if (ptr == nullptr)
			return DpiForDesktop();

		uint32_t x, y;
		auto hr = ptr(monitor, type, &x, &y);
		if (hr < 0)
			return DpiForDesktop();

		return static_cast<int>(x);
	}
	inline int DpiForNearestMonitor(HWND hwnd)
	{
		DpiForMonitor(MonitorFromWindow(hwnd));
	}
	inline int DpiForNearestMonitor(int x, int y)
	{
		DpiForMonitor(MonitorFromPoint(x, y));
	}

	// Return the DPI for the given window
	inline int DpiForWindow(HWND hwnd)
	{
		// Notes:
		//  - This should be the first method to use with fall-back to nearest monitor
		// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow
		auto h = LoadLibraryA("user32.dll");
		using GetDpiForWindowFn = uint32_t(*)(HWND);
		auto ptr = reinterpret_cast<GetDpiForWindowFn>(GetProcAddress(h, "GetDpiForWindow")); // Windows 10 1607
		return ptr != nullptr ? ptr(hwnd) : DpiForNearestMonitor(hwnd);
	}
	#endif
}
