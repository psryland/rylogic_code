//************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//************************************
#pragma once

#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/gui/wingui.h"
#include "pr/renderer11/lights/light.h"

namespace pr
{
	namespace rdr
	{
		template <typename Preview>
		class LightingUI :public pr::gui::Form
		{
			enum
			{
				ID_RADIO_AMBIENT = 100, ID_RADIO_DIRECTIONAL, ID_RADIO_POINT, ID_RADIO_SPOT,
				ID_EDIT_POSITION, ID_EDIT_DIRECTION, ID_CHECK_CAMERA_RELATIVE,
				ID_EDIT_RANGE, ID_EDIT_FALLOFF, ID_EDIT_SHADOW_RANGE,
				ID_EDIT_AMBIENT, ID_EDIT_DIFFUSE, ID_EDIT_SPECULAR, ID_EDIT_SPECULAR_POWER,
				ID_EDIT_INNER_ANGLE, ID_EDIT_OUTER_ANGLE,
			};

			pr::gui::GroupBox    m_grp_light_type;
			pr::gui::Button      m_rdo_ambient;
			pr::gui::Button      m_rdo_directional;
			pr::gui::Button      m_rdo_point;
			pr::gui::Button      m_rdo_spot;
			pr::gui::Button      m_chk_cam_rel;
			pr::gui::TextBox     m_tb_position;
			pr::gui::TextBox     m_tb_direction;
			pr::gui::TextBox     m_tb_range;
			pr::gui::TextBox     m_tb_falloff;
			pr::gui::TextBox     m_tb_shadow_range;
			pr::gui::TextBox     m_tb_ambient;
			pr::gui::TextBox     m_tb_diffuse;
			pr::gui::TextBox     m_tb_specular;
			pr::gui::TextBox     m_tb_spec_power;
			pr::gui::TextBox     m_tb_spot_inner;
			pr::gui::TextBox     m_tb_spot_outer;
			pr::gui::Label       m_lbl_position;
			pr::gui::Label       m_lbl_ambient;
			pr::gui::Label       m_lbl_diffuse;
			pr::gui::Label       m_lbl_specular;
			pr::gui::Label       m_lbl_spec_power;
			pr::gui::Label       m_lbl_range;
			pr::gui::Label       m_lbl_shadow_range;
			pr::gui::Label       m_lbl_falloff;
			pr::gui::Label       m_lbl_direction;
			pr::gui::Label       m_lbl_spot_angles;
			pr::gui::Label       m_lbl_inner;
			pr::gui::Label       m_lbl_outer;
			pr::gui::Button      m_btn_preview;
			pr::gui::Button      m_btn_cancel;
			pr::gui::Button      m_btn_ok;
			pr::gui::ToolTip     m_tt;
			Preview              m_preview;

		public:

			// The light we're displaying properties for
			pr::rdr::Light m_light;
			bool m_camera_relative;

			LightingUI(HWND parent, Preview const& preview)
				:Form(MakeDlgParams<>().name("rdr-lighting-ui").title(L"Lighting Options").wh(218,190).style_ex('+',WS_EX_TOOLWINDOW).parent(parent).wndclass(RegisterWndClass<LightingUI>()))
				,m_grp_light_type    (pr::gui::GroupBox::Params<>().parent(this_).text(L"Light Type").xy(3, 4).wh(56, 67))

				,m_rdo_ambient       (pr::gui::Button::Params<>().parent(this_).text(L"Ambient"    ).id(ID_RADIO_AMBIENT    ).xy(9, 17).wh(41, 8).radio())
				,m_rdo_directional   (pr::gui::Button::Params<>().parent(this_).text(L"Directional").id(ID_RADIO_DIRECTIONAL).xy(9, 30).wh(49, 8).radio())
				,m_rdo_point         (pr::gui::Button::Params<>().parent(this_).text(L"Point"      ).id(ID_RADIO_POINT      ).xy(9, 43).wh(32, 8).radio())
				,m_rdo_spot          (pr::gui::Button::Params<>().parent(this_).text(L"Spot"       ).id(ID_RADIO_SPOT       ).xy(9, 56).wh(31, 8).radio())

				,m_chk_cam_rel       (pr::gui::Button::Params<>().parent(this_).text(L"Camera Relative:").id(ID_CHECK_CAMERA_RELATIVE).xy(138, 38).wh(70, 8).chk_box().style('+',BS_LEFTTEXT))

				,m_tb_position       (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_POSITION      ).xy(94, 4)   .wh(119, 14))
				,m_tb_direction      (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_DIRECTION     ).xy(94, 20)  .wh(119, 14))
				,m_tb_range          (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_RANGE         ).xy(94, 50)  .wh(39, 14 ))
				,m_tb_falloff        (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_FALLOFF       ).xy(174, 50) .wh(39, 14 ))
				,m_tb_shadow_range   (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_SHADOW_RANGE  ).xy(138, 66) .wh(75, 14 ))
				,m_tb_ambient        (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_AMBIENT       ).xy(94, 85)  .wh(119, 14))
				,m_tb_diffuse        (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_DIFFUSE       ).xy(94, 101) .wh(119, 14))
				,m_tb_specular       (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_SPECULAR      ).xy(94, 117) .wh(119, 14))
				,m_tb_spec_power     (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_SPECULAR_POWER).xy(138, 133).wh(75, 14 ))
				,m_tb_spot_inner     (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_INNER_ANGLE   ).xy(94, 149) .wh(39, 14 ))
				,m_tb_spot_outer     (pr::gui::TextBox::Params<>().parent(this_).id(ID_EDIT_OUTER_ANGLE   ).xy(174, 149).wh(39, 14 ))

				,m_lbl_position      (pr::gui::Label::Params<>().parent(this_).text(L"Position:"         ).xy(64 , 7  ).wh(28, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_ambient       (pr::gui::Label::Params<>().parent(this_).text(L"Ambient (RRGGBB):" ).xy(27 , 88 ).wh(65, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_diffuse       (pr::gui::Label::Params<>().parent(this_).text(L"Diffuse (RRGGBB):" ).xy(30 , 104).wh(62, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_specular      (pr::gui::Label::Params<>().parent(this_).text(L"Specular (RRGGBB):").xy(24 , 120).wh(68, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_spec_power    (pr::gui::Label::Params<>().parent(this_).text(L"Specular Power:"   ).xy(80 , 136).wh(53, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_range         (pr::gui::Label::Params<>().parent(this_).text(L"Range:"            ).xy(68 , 53 ).wh(24, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_shadow_range  (pr::gui::Label::Params<>().parent(this_).text(L"Shadow Range:"     ).xy(84 , 69 ).wh(52, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_falloff       (pr::gui::Label::Params<>().parent(this_).text(L"Falloff:"          ).xy(138, 53 ).wh(31, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_direction     (pr::gui::Label::Params<>().parent(this_).text(L"Direction:"        ).xy(61 , 23 ).wh(31, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_spot_angles   (pr::gui::Label::Params<>().parent(this_).text(L"Spot Angles:"      ).xy(13 , 152).wh(41, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_inner         (pr::gui::Label::Params<>().parent(this_).text(L"Inner:"            ).xy(68 , 152).wh(24, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))
				,m_lbl_outer         (pr::gui::Label::Params<>().parent(this_).text(L"Outer:"            ).xy(138, 152).wh(34, 8).style('+', SS_LEFT).style_ex('+',WS_EX_RIGHT))

				,m_btn_preview       (pr::gui::Button::Params<>().parent(this_).text(L"Preview").id(IDRETRY ).xy(  5, 170).wh(50, 14))
				,m_btn_cancel        (pr::gui::Button::Params<>().parent(this_).text(L"Cancel" ).id(IDCANCEL).xy(163, 170).wh(50, 14))
				,m_btn_ok            (pr::gui::Button::Params<>().parent(this_).text(L"OK"     ).id(IDOK    ).xy(111, 170).wh(50, 14))

				,m_tt                (pr::gui::ToolTip::Params<>().parent(this_))

				,m_preview(preview)
				,m_light()
				,m_camera_relative(true)
			{
				CenterWindow(Parent());

				auto update_ui = [&](pr::gui::Button&, pr::gui::EmptyArgs const&) { UpdateUI(); };
				m_rdo_ambient.Click += update_ui;
				m_rdo_directional.Click += update_ui;
				m_rdo_point.Click += update_ui;
				m_rdo_spot.Click += update_ui;

				m_btn_preview.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					ReadValues();
					m_preview(m_light, m_camera_relative);
				};
				m_btn_cancel.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					Close(EDialogResult::Cancel);
				};
				m_btn_ok.Click += [&](pr::gui::Button&, pr::gui::EmptyArgs const&)
				{
					Close(EDialogResult::Ok);
				};


				//m_tt.Create(m_hWnd, 0, 0, TTS_NOPREFIX);//|TTS_BALLOON
				//
				//CToolInfo ti(TTF_IDISHWND | TTF_SUBCLASS, m_hWnd, 0, 0, MAKEINTRESOURCE(IDD));
				//m_tt.AddTool(&ti);

				//m_tt.SetDelayTime(TTDT_INITIAL, 1000);
				//m_tt.SetDelayTime(TTDT_AUTOPOP, 10000);
				//m_tt.SetDelayTime(TTDT_RESHOW, 1000);
				//
				//m_tt.SetMaxTipWidth(300);
				//m_tt.SetTipBkColor(0xe0e0e0);
				//m_tt.SetTipTextColor(0x000000);
				//
				//m_tt.AddTool(m_edit_position    , "The position of the light in world space or camera space if 'Camera Relative' is checked");
				//m_tt.AddTool(m_edit_direction   , "The light direction in world space or camera space if 'Camera Relative' is checked");
				//m_tt.AddTool(m_check_cam_rel    , "Check to have the light move with the camera");
				//m_tt.AddTool(m_edit_range       , "The maximum range of the light.");
				//m_tt.AddTool(m_edit_falloff     , "Controls the light attenuation with distance. 0 means no attenuation");
				//m_tt.AddTool(m_edit_shadow_range, "");
				//m_tt.AddTool(m_edit_ambient     , "The ambient light colour");
				//m_tt.AddTool(m_edit_diffuse     , "The colour of the light emitted from this light");
				//m_tt.AddTool(m_edit_specular    , "The colour of specular reflected light");
				//m_tt.AddTool(m_edit_spec_power  , "Controls the scattering of the specular reflection");
				//m_tt.AddTool(m_edit_spot_inner  , "The solid angle (deg) of maximum intensity for a spot light");
				//m_tt.AddTool(m_edit_spot_outer  , "The solid angle (deg) of zero intensity for a spot light");
				//m_tt.Activate(TRUE);


			//BEGIN_MSG_MAP          (LightingDlg)
			//	MSG_WM_INITDIALOG    (OnInitDialog)
			//	COMMAND_ID_HANDLER_EX(IDOK                   ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDRETRY                ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDCANCEL               ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDC_RADIO_AMBIENT      ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDC_RADIO_DIRECTIONAL  ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDC_RADIO_POINT        ,OnCommand)
			//	COMMAND_ID_HANDLER_EX(IDC_RADIO_SPOT         ,OnCommand)
			//	MESSAGE_RANGE_HANDLER(WM_MOUSEFIRST, WM_MOUSELAST, OnMouse);
			//END_MSG_MAP            ()

				PopulateControls();
				UpdateUI();
			}


			//LRESULT OnMouse(UINT, WPARAM, LPARAM, BOOL& bHandled)
			//{
			//	if (m_tt.IsWindow())
			//		m_tt.RelayEvent((LPMSG)GetCurrentMessage());
			//	
			//	bHandled = FALSE;
			//	return 0;
			//}

			// Update the values in the controls
			void PopulateControls()
			{
				int ids[ELight::NumberOf];
				ids[ELight::Ambient]     = ID_RADIO_AMBIENT;
				ids[ELight::Directional] = ID_RADIO_DIRECTIONAL;
				ids[ELight::Point]       = ID_RADIO_POINT;
				ids[ELight::Spot]        = ID_RADIO_SPOT;

				::CheckRadioButton(m_hwnd, ID_RADIO_AMBIENT, ID_RADIO_SPOT, ids[m_light.m_type]);
				m_chk_cam_rel      .Checked(m_camera_relative);
				m_tb_position      .Text(pr::FmtS(L"%3.3f %3.3f %3.3f" ,m_light.m_position.x ,m_light.m_position.y ,m_light.m_position.z));
				m_tb_direction     .Text(pr::FmtS(L"%3.3f %3.3f %3.3f" ,m_light.m_direction.x ,m_light.m_direction.y ,m_light.m_direction.z));
				m_tb_range         .Text(pr::FmtS(L"%3.3f" ,m_light.m_range));
				m_tb_falloff       .Text(pr::FmtS(L"%3.3f" ,m_light.m_falloff));
				m_tb_shadow_range  .Text(pr::FmtS(L"%3.3f" ,m_light.m_cast_shadow));
				m_tb_ambient       .Text(pr::FmtS(L"%6.6X" ,0xFFFFFF & m_light.m_ambient.m_aarrggbb ));
				m_tb_diffuse       .Text(pr::FmtS(L"%6.6X" ,0xFFFFFF & m_light.m_diffuse.m_aarrggbb ));
				m_tb_specular      .Text(pr::FmtS(L"%6.6X" ,0xFFFFFF & m_light.m_specular.m_aarrggbb));
				m_tb_spec_power    .Text(pr::FmtS(L"%d" ,(int)(0.5f + m_light.m_specular_power)));
				m_tb_spot_inner    .Text(pr::FmtS(L"%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_inner_cos_angle)))));
				m_tb_spot_outer    .Text(pr::FmtS(L"%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_outer_cos_angle)))));
			}

			// Read an validate values from the controls
			void ReadValues()
			{
				// Light type
				if (m_rdo_ambient    .Checked()) m_light.m_type = pr::rdr::ELight::Ambient;
				if (m_rdo_directional.Checked()) m_light.m_type = pr::rdr::ELight::Directional;
				if (m_rdo_point      .Checked()) m_light.m_type = pr::rdr::ELight::Point;
				if (m_rdo_spot       .Checked()) m_light.m_type = pr::rdr::ELight::Spot;

				// Transform
				m_light.m_position        = pr::To<v4>(m_tb_position.Text(), 1.0f);
				m_light.m_direction       = pr::Normalise3(pr::To<v4>(m_tb_direction.Text(), 0.0f));
				m_camera_relative         = m_chk_cam_rel.Checked();
				m_light.m_range           = pr::To<float>(m_tb_range.Text());
				m_light.m_falloff         = pr::To<float>(m_tb_falloff.Text());
				m_light.m_cast_shadow     = pr::To<float>(m_tb_shadow_range.Text());
				m_light.m_ambient         = pr::To<Colour32>(m_tb_ambient.Text()).a0();
				m_light.m_diffuse         = pr::To<Colour32>(m_tb_diffuse.Text()).a1();
				m_light.m_specular        = pr::To<Colour32>(m_tb_specular.Text()).a0();
				m_light.m_specular_power  = pr::To<float>(m_tb_spec_power.Text());
				m_light.m_inner_cos_angle = pr::Cos(pr::DegreesToRadians(pr::To<float>(m_tb_spot_inner.Text())));
				m_light.m_outer_cos_angle = pr::Cos(pr::DegreesToRadians(pr::To<float>(m_tb_spot_outer.Text())));
			}

			// Enable/Disable controls
			void UpdateUI()
			{
				m_tb_position    .Enabled(m_light.m_type == ELight::Point       || m_light.m_type == ELight::Spot);
				m_tb_direction   .Enabled(m_light.m_type == ELight::Directional || m_light.m_type == ELight::Spot);
				m_chk_cam_rel    .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_range       .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_falloff     .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_shadow_range.Enabled(m_light.m_type != ELight::Ambient);
				m_tb_ambient     .Enabled(true);
				m_tb_diffuse     .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_specular    .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_spec_power  .Enabled(m_light.m_type != ELight::Ambient);
				m_tb_spot_inner  .Enabled(m_light.m_type == ELight::Spot);
				m_tb_spot_outer  .Enabled(m_light.m_type == ELight::Spot);
			}
		};
	}
}
