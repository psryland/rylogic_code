//************************************
// Camera Dialog
//  Copyright (c) Rylogic Ltd 2014
//************************************

#pragma once

#include <functional>
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"
#include "pr/gui/wingui.h"

namespace pr
{
	namespace camera
	{
		struct PositionUI :pr::gui::Form
		{
		private:

			enum { ID_TB_POSITION = 100, ID_TB_LOOKAT, ID_TB_UP, ID_TB_HORZ_FOV, ID_BTN_PREVIEW, };
			pr::gui::Label   m_lbl_position;
			pr::gui::Label   m_lbl_lookat;
			pr::gui::Label   m_lbl_up;
			pr::gui::Label   m_lbl_horz_fov;
			pr::gui::TextBox m_tb_position;
			pr::gui::TextBox m_tb_lookat;
			pr::gui::TextBox m_tb_up;
			pr::gui::TextBox m_tb_horz_fov;
			pr::gui::Button  m_btn_preview;
			pr::gui::Button  m_btn_cancel;
			pr::gui::Button  m_btn_ok;
			bool m_allow_preview;

		public:

			// The camera transform set in the dialog
			pr::Camera m_cam;

			PositionUI(HWND parent, pr::Camera& cam, bool allow_preview = false)
				:Form(MakeDlgParams<>().wndclass(RegisterWndClass<PositionUI>()).name("cam-position-ui").title(L"Position Camera").wh(169, 93).style_ex('+',WS_EX_TOOLWINDOW).parent(parent))

				,m_lbl_position(pr::gui::Label  ::Params<>().parent(this_).name("lbl-position"     ).text(L"Position:"       ).xy(12, 11).wh(28, 8).style('+',SS_RIGHT))
				,m_lbl_lookat  (pr::gui::Label  ::Params<>().parent(this_).name("lbl-lookat"       ).text(L"Look At:"        ).xy(12, 27).wh(28, 8).style('+',SS_RIGHT))
				,m_lbl_up      (pr::gui::Label  ::Params<>().parent(this_).name("lbl-up"           ).text(L"Up:"             ).xy(28, 43).wh(12, 8).style('+',SS_RIGHT))
				,m_lbl_horz_fov(pr::gui::Label  ::Params<>().parent(this_).name("lbl-horz_fov"     ).text(L"Horz. FOV (deg):").xy(12, 11).wh(28, 8).style('+',SS_RIGHT))

				,m_tb_position (pr::gui::TextBox::Params<>().parent(this_).name("tb-position"      ).id(ID_TB_POSITION).xy(48,8  ).wh(119,14))
				,m_tb_lookat   (pr::gui::TextBox::Params<>().parent(this_).name("tb-lookat"        ).id(ID_TB_LOOKAT  ).xy(48,24 ).wh(119,14))
				,m_tb_up       (pr::gui::TextBox::Params<>().parent(this_).name("tb-up"            ).id(ID_TB_UP      ).xy(48,40 ).wh(119,14))
				,m_tb_horz_fov (pr::gui::TextBox::Params<>().parent(this_).name("tb-horz-fov"      ).id(ID_TB_HORZ_FOV).xy(114,56).wh(53,14))
				,m_btn_preview (pr::gui::Button ::Params<>().parent(this_).name("btn-preview"      ).id(ID_BTN_PREVIEW).xy(5,75  ).wh(50,14))
				,m_btn_cancel  (pr::gui::Button ::Params<>().parent(this_).name("btn-cancel"       ).id(IDCANCEL      ).xy(117,75).wh(50,14))
				,m_btn_ok      (pr::gui::Button ::Params<>().parent(this_).name("btn-ok"           ).id(IDOK          ).xy(62,75 ).wh(50,14).style('+',BS_DEFPUSHBUTTON))

				,m_allow_preview(allow_preview)
				,m_cam(cam)
			{
				CreateHandle();

				m_btn_cancel.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					ReadValues();
					Close(EDialogResult::Cancel);
				};
				m_btn_ok.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					ReadValues();
					Close(EDialogResult::Ok);
				};
				m_btn_preview.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					ReadValues();
					Preview();
				};

				Populate();
			}

			// Apply the values to the camera instance
			void ReadValues()
			{
				// Update the values
				auto position = pr::To<pr::v3>(m_tb_position.Text()).w1();
				auto lookat   = pr::To<pr::v3>(m_tb_lookat  .Text()).w1();
				auto up       = pr::To<pr::v3>(m_tb_up      .Text()).w0();
				auto hfov     = pr::DegreesToRadians(pr::To<float>(m_tb_horz_fov.Text()));

				if (pr::FEql3(lookat - position, pr::v4Zero))
					lookat = position + pr::v4ZAxis;
				if (pr::Parallel(lookat - position, up))
					up = pr::Perpendicular(up);
		
				m_cam.LookAt(position, lookat, up);
				m_cam.FovX(hfov);
			}

			// Populate the text boxes from the current camera values
			void Populate()
			{
				auto StrV = [](pr::v4 const& v) { return pr::FmtS(L"%3.3f %3.3f %3.3f",v.x,v.y,v.z); };
				auto StrF = [](float f)         { return pr::FmtS(L"%3.3f", f); };

				m_tb_position.Text(StrV(m_cam.CameraToWorld().pos));
				m_tb_lookat  .Text(StrV(m_cam.FocusPoint()));
				m_tb_up      .Text(StrV(m_cam.CameraToWorld().y));
				m_tb_horz_fov.Text(StrF(pr::RadiansToDegrees(m_cam.FovX())));
			}

			// Preview the new camera position
			virtual void Preview()
			{
				// Subclass for preview ability
			}
		};
	}
}