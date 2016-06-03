//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "pr/gui/wingui.h"
#include "pr/view3d/view3d.h"

namespace pr
{
	namespace gui
	{
		struct View3DPanel :Panel
		{
			struct View3DPanelParams :PanelParams
			{
				View3D_ReportErrorCB m_error_cb;         // View3D error reporting callback function
				void*                m_error_ctx;        // View3D error reporting callback context
				bool                 m_gdi_compat;       // GDI compatibility
				bool                 m_show_focus_point; // True if the focus cross hair should be rendered

				View3DPanelParams()
					:m_error_cb([](void*, char const* msg){ throw std::exception(msg); })
					,m_error_ctx()
					,m_gdi_compat(false)
					,m_show_focus_point(false)
				{}
				View3DPanelParams* clone() const override
				{
					return new View3DPanelParams(*this);
				}
			};
			template <typename TParams = View3DPanelParams, typename Derived = void> struct Params :Panel::Params<TParams, choose_non_void<Derived, Params<>>>
			{
				using base = Panel::Params<TParams, choose_non_void<Derived, Params<>>>;
				Params()
				{
					name("view3d").margin(0).selectable();
				}
				operator View3DPanelParams const&() const
				{
					return params;
				}
				This& error_cb(View3D_ReportErrorCB error_cb, void* error_ctx)
				{
					params.m_error_cb = error_cb;
					params.m_error_ctx = error_ctx;
					return me();
				}
				This& gdi_compat(bool on = true)
				{
					params.m_gdi_compat = on;
					return me();
				}
				This& show_focus_point(bool on = true)
				{
					params.m_show_focus_point = on;
					return me();
				}
			};

			View3DContext m_ctx;
			View3DWindow m_win;

			View3DPanel() :View3DPanel(View3DPanelParams()) {}
			View3DPanel(View3DPanelParams const& p)
				:Panel(p)
				,m_ctx(View3D_Initialise(p.m_error_cb, p.m_error_ctx))
				,m_win(View3D_CreateWindow(CreateHandle(), View3DWindowOptions{p.m_error_cb, p.m_error_ctx, p.m_gdi_compat, "vrex_gui"}))
			{
				//View3D_CreateDemoScene(m_win);
				View3D_ShowFocusPoint(m_win, cp().m_show_focus_point);
			}
			~View3DPanel()
			{
				//View3D_DeleteDemoScene();
				if (m_win) View3D_DestroyWindow(m_win);
				if (m_ctx) View3D_Shutdown(m_ctx);
			}

			// The parameters used to create this control (but updated to the current state)
			View3DPanelParams& cp() const
			{
				return static_cast<View3DPanelParams&>(*m_cp);
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
				View3D_MouseNavigate(m_win, view3d::To<View3DV2>(args.m_point), args.m_down ? int(args.m_button) : 0, TRUE);
			}
			void OnMouseMove(MouseEventArgs& args) override
			{
				View3D_MouseNavigate(m_win, view3d::To<View3DV2>(args.m_point), int(args.m_button), FALSE);
			}
			void OnMouseWheel(MouseWheelArgs& args) override
			{
				View3D_Navigate(m_win, 0.0f, 0.0f, args.m_delta / 120.0f);
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
					View3D_SetRenderTargetSize(m_win, args.m_wp->cx, args.m_wp->cy);
			}
		};
	}
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
