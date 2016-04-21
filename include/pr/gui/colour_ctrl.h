//***********************************************
// Colour Control
//  Copyright (c) Rylogic Ltd 2016
//***********************************************
#pragma once

//#include <vector>
//#include <algorithm>
//#include <mutex>
//#include <thread>
//#include <cassert>

#include "pr/common/colour.h"
#include "pr/gui/wingui.h"
#include "pr/gui/gdiplus.h"

namespace pr
{
	namespace gui
	{
		// A control for picking a colour
		struct ColourCtrl :Control
		{
			enum { DefW = 80, DefH = 23 };
			enum :DWORD { DefaultStyle   = (DefaultControlStyle | WS_GROUP | SS_LEFT) & ~WS_TABSTOP };
			enum :DWORD { DefaultStyleEx = DefaultControlStyleEx };
			static wchar_t const* WndClassName() { return L"pr::gui::ColourCtrl"; }

			template <typename TParams = CtrlParams, typename Derived = void> struct Params :MakeCtrlParams<TParams, choose_non_void<Derived, Params<>>>
			{
				using base = MakeCtrlParams<TParams, choose_non_void<Derived, Params<>>>;
				Params() { wndclass(RegisterWndClass<ColourCtrl>()); }
			};

		protected:

			pr::GdiPlus      m_gdiplus;     // Initialises the GdiPlus library
			
		public:

			ColourCtrl() :ColourCtrl(Params<>()){}
			ColourCtrl(Params<> const& p)
				:Control(p)
				,m_gdiplus()
			{}

		private:

			// Render the control in to 'dc'
			void DoPaint(HDC dc, Rect const& area)
			{
				MemDC memdc(dc, area, nullptr);
				gdi::Graphics gfx(memdc);
				assert(gfx.GetLastStatus() == gdi::Ok && "GDI+ not initialised");
			}

			// Handlers
			bool OnPaint(PaintEventArgs const& args) override
			{
				if (args.m_alternate_hdc) { DoPaint(args.m_alternate_hdc, ClientRect()); }
				else                      { PaintStruct ps(m_hwnd); DoPaint(ps.hdc, ClientRect()); }
				return Control::OnPaint(args);
			}
		};

		// A dialog for picking a colour
		struct ColourUI :pr::gui::Form
		{
			pr::Colour32 m_colour;

			ColourUI(HWND parent, pr::Colour32 colour)
				:Form(MakeDlgParams<>().parent(parent).wndclass(RegisterWndClass<ColourUI>()))
				,m_colour(colour)
			{}
		};
	}
}
