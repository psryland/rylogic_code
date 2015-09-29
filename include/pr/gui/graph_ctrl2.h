//*****************************************************************************************
// Graph Control
//  Copyright (c) Rylogic Ltd 2015
//*****************************************************************************************
// DirectX based graph control
#pragma once

#include "pr/gui/wingui.h"
#include "pr/gui/gdiplus.h"
#include "pr/view3d/view3d.h"

namespace pr
{
	namespace gui
	{
		struct GraphCtrl :Control
		{
		private:
			View3DContext m_view3d;
			View3DWindow m_wnd;
			View3DObject m_obj;

			friend struct Control;
			static TCHAR const* WndClassName() { return _T("PRGRAPHCTRL2"); }
			static WNDCLASSEX WndClassInfo(HINSTANCE hinst) { return Control::WndClassInfo<GraphCtrl>(hinst); }
			static HBRUSH WndBackground() { return reinterpret_cast<HBRUSH>(COLOR_BACKGROUND+1); }

			// Handle the Paint event
			// Return true, to prevent anything else handling the event
			bool OnPaint(PaintEventArgs const& args) override
			{
				Control::OnPaint(args);
				View3D_Render(m_wnd);
				View3D_Present(m_wnd);
				return false;
			}
			
			// Handle window size changing starting or stopping
			void OnWindowPosChange(SizeEventArgs const& args) override
			{
				Control::OnWindowPosChange(args);
				if (!args.m_before)
					View3D_SetRenderTargetSize(m_wnd, args.m_size.cx, args.m_size.cy);
			}

		public:
			GraphCtrl(int x = CW_USEDEFAULT, int y = CW_USEDEFAULT, int w = CW_USEDEFAULT, int h = CW_USEDEFAULT
				,int id = IDC_UNUSED
				,HWND hwndparent = 0
				,Control* parent = nullptr
				,EAnchor anchor = EAnchor::Left|EAnchor::Top
				,DWORD style = WS_CHILD | WS_VISIBLE | WS_TABSTOP
				,DWORD ex_style = 0
				,char const* name = nullptr)
				:Control(MAKEINTATOM(RegisterWndClass<GraphCtrl>()), nullptr, x, y, w, h, id, hwndparent, parent, anchor, style, ex_style, name)
			{
				auto error_cb = [](void*, char const* msg){ OutputDebugStringA(msg); };
				m_view3d = View3D_Initialise(error_cb, this);
				m_wnd = View3D_CreateWindow(m_hwnd, FALSE, error_cb, this);
				m_obj = View3D_ObjectCreateLdr("*Sphere bob FF00FF00 { 1 }", FALSE, 0, FALSE, nullptr, nullptr);
				View3D_ObjectSetO2P(m_obj, View3DM4x4{{1.0f,0,0,0},{0,1.0f,0,0},{0,0,1.0f,0},{0,0,-5.0f,1.0f}}, nullptr);
				View3D_AddObject(m_wnd, m_obj);
			}
			~GraphCtrl()
			{
				View3D_ObjectDelete(m_obj);
				View3D_DestroyWindow(m_wnd);
				View3D_Shutdown(m_view3d);
			}

		private:

			//// Returns an area for the plot part of the graph given a bitmap
			//// with size 'size'. (i.e. excl titles, axis labels, etc)
			//Gdiplus::Rect PlotArea(Gdiplus::Graphics const& gfx, Gdiplus::Rect const& area) const
			//{
			//	using namespace Gdiplus;
			//	
			//	RectF rect(0.0f, 0.0f, float(area.Width), float(area.Height));
			//	
			//	// Add margins
			//	rect.X      += m_opts.m_margin_left;
			//	rect.Y      += m_opts.m_margin_top;
			//	rect.Width  -= m_opts.m_margin_left + m_opts.m_margin_right;
			//	rect.Height -= m_opts.m_margin_top  + m_opts.m_margin_bottom;
			//	
			//	// Add space for tick marks
			//	rect.X      += m_yaxis.m_opts.m_tick_length;
			//	rect.Width  -= m_yaxis.m_opts.m_tick_length;
			//	rect.Height -= m_xaxis.m_opts.m_tick_length;
			//	
			//	// Add space for the title and axis labels
			//	RectF r;
			//	if (!m_title.empty())         { gfx.MeasureString(m_title.c_str()         ,int(m_title.size())         ,&m_opts.m_font_title         ,PointF(), &r); rect.Y      += r.Height; rect.Height -= r.Height; }
			//	if (!m_xaxis.m_label.empty()) { gfx.MeasureString(m_xaxis.m_label.c_str() ,int(m_xaxis.m_label.size()) ,&m_xaxis.m_opts.m_font_label ,PointF(), &r); rect.Height -= r.Height; }
			//	if (!m_yaxis.m_label.empty()) { gfx.MeasureString(m_yaxis.m_label.c_str() ,int(m_yaxis.m_label.size()) ,&m_yaxis.m_opts.m_font_label ,PointF(), &r); rect.X      += r.Height; rect.Width -= r.Height; } // will be rotated by 90°
			//	
			//	// Add space for tick labels
			//	wchar_t const lbl[] = L"99999.999";
			//	int const lbl_len = sizeof(lbl)/sizeof(lbl[0]);
			//	gfx.MeasureString(lbl, lbl_len, &m_xaxis.m_opts.m_font_tick ,PointF(), &r); rect.Height -= r.Height;
			//	gfx.MeasureString(lbl, lbl_len, &m_yaxis.m_opts.m_font_tick ,PointF(), &r); rect.X += r.Width; rect.Width -= r.Width;
			//	
			//	return Rect(int(rect.X), int(rect.Y), int(rect.Width), int(rect.Height));
			//}
		};
	}
}