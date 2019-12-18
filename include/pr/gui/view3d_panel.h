//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "pr/gui/wingui.h"
#include "pr/view3d/view3d.h"

namespace pr::gui
{
	struct View3DPanel :Panel
	{
		struct Params :Panel::Params<Params>
		{
			using this_type = typename Params::this_type;

			View3D_ReportErrorCB     m_error_cb;         // View3D error reporting callback function
			void*                    m_error_ctx;        // View3D error reporting callback context
			D3D11_CREATE_DEVICE_FLAG m_device_flags;     // Device creation flags
			bool                     m_show_focus_point; // True if the focus cross hair should be rendered
			bool                     m_gdi_compat;       // True if 'GDI' compatibility is enabled

			Params()
				:m_error_cb([](void*, wchar_t const* msg){ throw std::exception(pr::gui::Narrow(msg).c_str()); })
				,m_error_ctx()
				,m_device_flags()
				,m_show_focus_point(false)
				,m_gdi_compat(false)
			{
				name("view3d")
					.margin(0)
					.selectable();
			}
			View3DWindowOptions wnd_opts() const
			{
				auto opts = View3DWindowOptions{};
				opts.m_error_cb = m_error_cb;
				opts.m_error_cb_ctx = m_error_ctx;
				opts.m_gdi_compatible_backbuffer = m_gdi_compat ? AllSet(m_device_flags, D3D11_CREATE_DEVICE_BGRA_SUPPORT) : 0;
				opts.m_multisampling = 4;
				opts.m_dbg_name = "vrex_gui";
				return opts;
			}
			this_type& error_cb(View3D_ReportErrorCB error_cb, void* error_ctx)
			{
				m_error_cb = error_cb;
				m_error_ctx = error_ctx;
				return me();
			}
			this_type& gdi_compat(bool on = true)
			{
				m_gdi_compat = on;
				return me();
			}
			this_type& show_focus_point(bool on = true)
			{
				m_show_focus_point = on;
				return me();
			}
		};

		View3DContext m_ctx;
		View3DWindow m_win;

		View3DPanel()
			:View3DPanel(Params())
		{}
		explicit View3DPanel(Params const& p)
			:Panel(p)
			,m_ctx(View3D_Initialise(p.m_error_cb, p.m_error_ctx, p.m_device_flags))
			,m_win(View3D_WindowCreate(CreateHandle(), p.wnd_opts()))
		{
			//View3D_CreateDemoScene(m_win);
			View3D_FocusPointVisibleSet(m_win, cp().m_show_focus_point);
		}
		~View3DPanel()
		{
			//View3D_DeleteDemoScene();
			if (m_win) View3D_WindowDestroy(m_win);
			if (m_ctx) View3D_Shutdown(m_ctx);
		}

		// The parameters used to create this control (but updated to the current state)
		Params const& cp() const
		{
			return m_cp->as<Params>();
		}
		Params& cp()
		{
			return m_cp->as<Params>();
		}

		// Key shortcuts
		void OnKey(KeyEventArgs& args) override
		{
			Panel::OnKey(args);
			if (args.m_handled) return;

			if (View3D_TranslateKey(m_win, args.m_vk_key))
				args.m_handled = true;
		}

		// Mouse navigation
		void OnMouseButton(MouseEventArgs& args) override
		{
			auto op = View3D_MouseBtnToNavOp(int(args.m_button));
			if (View3D_MouseNavigate(m_win, view3d::To<View3DV2>(args.m_point), args.m_down ? op : EView3DNavOp::None, TRUE))
			{
				Invalidate();
				UpdateWindow(m_hwnd);
			}
		}
		void OnMouseMove(MouseEventArgs& args) override
		{
			auto op = View3D_MouseBtnToNavOp(int(args.m_button));
			if (View3D_MouseNavigate(m_win, view3d::To<View3DV2>(args.m_point), op, FALSE))
			{
				Invalidate();
				UpdateWindow(m_hwnd);
			}
		}
		void OnMouseWheel(MouseWheelArgs& args) override
		{
			if (View3D_MouseNavigateZ(m_win, view3d::To<View3DV2>(args.m_point), args.m_delta, TRUE))
			{
				Invalidate();
				UpdateWindow(m_hwnd);
			}
		}

		// Render the panel
		void OnPaint(PaintEventArgs& args) override
		{
			if (m_win)
			{
				View3D_Render(m_win);
				View3D_Present(m_win);
				args.m_handled = true;
				return;
			}
			Panel::OnPaint(args);
		}

		// Handle window size changing starting or stopping
		void OnWindowPosChange(WindowPosEventArgs const& args) override
		{
			Control::OnWindowPosChange(args);
			if (!args.m_before && args.IsResize() && !args.Iconic())
				View3D_BackBufferSizeSet(m_win, args.m_wp->cx, args.m_wp->cy);
		}
	};
}
namespace view3d
{
	template <> struct Convert<View3DV2, pr::gui::Point>
	{
		static View3DV2 To(pr::gui::Point const& v)
		{
			return View3DV2{float(v.x), float(v.y)};
		}
	};
	template <> struct Convert<pr::gui::Point, View3DV2>
	{
		static pr::gui::Point To(View3DV2 const& v)
		{
			return pr::gui::Point(long(v.x), long(v.y));
		}
	};
}
