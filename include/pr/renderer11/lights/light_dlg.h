//************************************
// Lighting Dialog
//  Copyright (c) Rylogic Ltd 2009
//************************************

#pragma once
#ifndef PR_RDR_LIGHTS_LIGHT_DLG_H
#define PR_RDR_LIGHTS_LIGHT_DLG_H

#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>
#include "pr/common/min_max_fix.h"
#include "pr/macros/count_of.h"
#include "pr/common/to.h"
#include "pr/common/fmt.h"
//#include "pr/str/prstring.h"
#include "pr/renderer11/lights/light.h"

namespace pr
{
	namespace rdr
	{
		template <typename Preview>
		class LightingDlg :public CIndirectDialogImpl< LightingDlg<Preview> >
		{
			Preview m_preview;
			CButton m_btn_ambient;
			CButton m_btn_directional;
			CButton m_btn_point;
			CButton m_btn_spot;
			CEdit   m_edit_position;
			CEdit   m_edit_direction;
			CButton m_check_cam_rel;
			CEdit   m_edit_range;
			CEdit   m_edit_falloff;
			CEdit   m_edit_shadow_range;
			CEdit   m_edit_ambient;
			CEdit   m_edit_diffuse;
			CEdit   m_edit_specular;
			CEdit   m_edit_spec_power;
			CEdit   m_edit_spot_inner;
			CEdit   m_edit_spot_outer;
			CToolTipCtrl m_tt;

		public:
			pr::rdr::Light m_light;
			bool m_camera_relative;

			explicit LightingDlg(Preview const& preview)
				:m_preview(preview)
				,m_light()
				,m_camera_relative(true)
			{}
			~LightingDlg()
			{
				if (IsWindow())
					EndDialog(0);
			}

			// Handler methods
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				CenterWindow(GetParent());

				// Attach and initialise controls
				m_btn_ambient        .Attach(GetDlgItem(IDC_RADIO_AMBIENT));
				m_btn_directional    .Attach(GetDlgItem(IDC_RADIO_DIRECTIONAL));
				m_btn_point          .Attach(GetDlgItem(IDC_RADIO_POINT));
				m_btn_spot           .Attach(GetDlgItem(IDC_RADIO_SPOT));
				m_edit_position      .Attach(GetDlgItem(IDC_EDIT_POSITION));
				m_edit_direction     .Attach(GetDlgItem(IDC_EDIT_DIRECTION));
				m_check_cam_rel      .Attach(GetDlgItem(IDC_CHECK_CAMERA_RELATIVE));
				m_edit_range         .Attach(GetDlgItem(IDC_EDIT_RANGE));
				m_edit_falloff       .Attach(GetDlgItem(IDC_EDIT_FALLOFF));
				m_edit_shadow_range  .Attach(GetDlgItem(IDC_EDIT_SHADOW_RANGE));
				m_edit_ambient       .Attach(GetDlgItem(IDC_EDIT_AMBIENT));
				m_edit_diffuse       .Attach(GetDlgItem(IDC_EDIT_DIFFUSE));
				m_edit_specular      .Attach(GetDlgItem(IDC_EDIT_SPECULAR));
				m_edit_spec_power    .Attach(GetDlgItem(IDC_EDIT_SPECULAR_POWER));
				m_edit_spot_inner    .Attach(GetDlgItem(IDC_EDIT_INNER_ANGLE));
				m_edit_spot_outer    .Attach(GetDlgItem(IDC_EDIT_OUTER_ANGLE));

				m_tt.Create(m_hWnd, 0, 0, TTS_NOPREFIX);//|TTS_BALLOON
				
				CToolInfo ti(TTF_IDISHWND | TTF_SUBCLASS, m_hWnd, 0, 0, MAKEINTRESOURCE(IDD));
				m_tt.AddTool(&ti);

				m_tt.SetDelayTime(TTDT_INITIAL, 1000);
				m_tt.SetDelayTime(TTDT_AUTOPOP, 10000);
				m_tt.SetDelayTime(TTDT_RESHOW, 1000);
				
				m_tt.SetMaxTipWidth(300);
				m_tt.SetTipBkColor(0xe0e0e0);
				m_tt.SetTipTextColor(0x000000);
				
				m_tt.AddTool(m_edit_position    , "The position of the light in world space or camera space if 'Camera Relative' is checked");
				m_tt.AddTool(m_edit_direction   , "The light direction in world space or camera space if 'Camera Relative' is checked");
				m_tt.AddTool(m_check_cam_rel    , "Check to have the light move with the camera");
				m_tt.AddTool(m_edit_range       , "The maximum range of the light.");
				m_tt.AddTool(m_edit_falloff     , "Controls the light attenuation with distance. 0 means no attenuation");
				m_tt.AddTool(m_edit_shadow_range, "");
				m_tt.AddTool(m_edit_ambient     , "The ambient light colour");
				m_tt.AddTool(m_edit_diffuse     , "The colour of the light emitted from this light");
				m_tt.AddTool(m_edit_specular    , "The colour of specular reflected light");
				m_tt.AddTool(m_edit_spec_power  , "Controls the scattering of the specular reflection");
				m_tt.AddTool(m_edit_spot_inner  , "The solid angle (deg) of maximum intensity for a spot light");
				m_tt.AddTool(m_edit_spot_outer  , "The solid angle (deg) of zero intensity for a spot light");
				m_tt.Activate(TRUE);
				
				PopulateControls();
				UpdateUI();
				return TRUE;
			}
			void OnCommand(UINT, int nID, CWindow)
			{
				ReadValues();
				PopulateControls();
				UpdateUI();

				switch (nID)
				{
				default:
					SetMsgHandled(FALSE);
					break;
				case IDOK:
				case IDCANCEL:
					EndDialog(nID);
					return;
				case IDRETRY:
					m_preview(m_light, m_camera_relative);
					break;
				case IDC_RADIO_AMBIENT:
				case IDC_RADIO_DIRECTIONAL:
				case IDC_RADIO_POINT:
				case IDC_RADIO_SPOT:
					break;
				}
			}
			LRESULT OnMouse(UINT, WPARAM, LPARAM, BOOL& bHandled)
			{
				if (m_tt.IsWindow())
					m_tt.RelayEvent((LPMSG)GetCurrentMessage());
				
				bHandled = FALSE;
				return 0;
			}

			// Update the values in the controls
			void PopulateControls()
			{
				int ids[ELight::NumberOf];
				ids[ELight::Ambient]     = IDC_RADIO_AMBIENT;
				ids[ELight::Directional] = IDC_RADIO_DIRECTIONAL;
				ids[ELight::Point]       = IDC_RADIO_POINT;
				ids[ELight::Spot]        = IDC_RADIO_SPOT;

				CheckRadioButton(IDC_RADIO_AMBIENT, IDC_RADIO_SPOT, ids[m_light.m_type]);
				m_check_cam_rel      .SetCheck(m_camera_relative);
				m_edit_position      .SetWindowTextA(pr::FmtS("%3.3f %3.3f %3.3f" ,m_light.m_position.x ,m_light.m_position.y ,m_light.m_position.z));
				m_edit_direction     .SetWindowTextA(pr::FmtS("%3.3f %3.3f %3.3f" ,m_light.m_direction.x ,m_light.m_direction.y ,m_light.m_direction.z));
				m_edit_range         .SetWindowTextA(pr::FmtS("%3.3f" ,m_light.m_range));
				m_edit_falloff       .SetWindowTextA(pr::FmtS("%3.3f" ,m_light.m_falloff));
				m_edit_shadow_range  .SetWindowTextA(pr::FmtS("%3.3f" ,m_light.m_cast_shadow));
				m_edit_ambient       .SetWindowTextA(pr::FmtS("%6.6X" ,0xFFFFFF & m_light.m_ambient.m_aarrggbb ));
				m_edit_diffuse       .SetWindowTextA(pr::FmtS("%6.6X" ,0xFFFFFF & m_light.m_diffuse.m_aarrggbb ));
				m_edit_specular      .SetWindowTextA(pr::FmtS("%6.6X" ,0xFFFFFF & m_light.m_specular.m_aarrggbb));
				m_edit_spec_power    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + m_light.m_specular_power)));
				m_edit_spot_inner    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_inner_cos_angle)))));
				m_edit_spot_outer    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_outer_cos_angle)))));
			}

			// Read an validate values from the controls
			void ReadValues()
			{
				char str[256];

				// Light type
				if (m_btn_ambient    .GetCheck()) m_light.m_type = pr::rdr::ELight::Ambient;
				if (m_btn_directional.GetCheck()) m_light.m_type = pr::rdr::ELight::Directional;
				if (m_btn_point      .GetCheck()) m_light.m_type = pr::rdr::ELight::Point;
				if (m_btn_spot       .GetCheck()) m_light.m_type = pr::rdr::ELight::Spot;

				// Position
				m_edit_position.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_position = pr::To<pr::v4>(str, 1.0f);

				// Direction
				m_edit_direction.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_direction = pr::Normalise3(pr::To<pr::v4>(str, 0.0f));

				// Camera relative
				m_camera_relative = m_check_cam_rel.GetCheck() != 0;
				
				// Range
				m_edit_range.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_range = pr::To<float>(str);

				// Falloff
				m_edit_falloff.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_falloff = pr::To<float>(str);

				// Cast shadows
				m_edit_shadow_range.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_cast_shadow = pr::To<float>(str);

				// Ambient
				m_edit_ambient.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_ambient = pr::To<Colour32>(str);
				m_light.m_ambient.a() = 0;

				// Diffuse
				m_edit_diffuse.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_diffuse = pr::To<Colour32>(str);
				m_light.m_diffuse.a()  = 0xFF;

				// Specular
				m_edit_specular.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_specular = pr::To<Colour32>(str);
				m_light.m_specular.a() = 0;

				// Specular power
				m_edit_spec_power.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_specular_power = pr::To<float>(str);

				// Spot inner
				m_edit_spot_inner.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_inner_cos_angle = pr::To<float>(str);
				m_light.m_inner_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_inner_cos_angle));
				
				// Spot outer
				m_edit_spot_outer.GetWindowTextA(str, PR_COUNTOF(str));
				m_light.m_outer_cos_angle = pr::To<float>(str);
				m_light.m_outer_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_outer_cos_angle));
			}

			// Enable/Disable controls
			void UpdateUI()
			{
				m_edit_position      .EnableWindow(m_light.m_type == ELight::Point       || m_light.m_type == ELight::Spot);
				m_edit_direction     .EnableWindow(m_light.m_type == ELight::Directional || m_light.m_type == ELight::Spot);
				m_check_cam_rel      .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_range         .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_falloff       .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_shadow_range  .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_ambient       .EnableWindow(true);
				m_edit_diffuse       .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_specular      .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_spec_power    .EnableWindow(m_light.m_type != ELight::Ambient);
				m_edit_spot_inner    .EnableWindow(m_light.m_type == ELight::Spot);
				m_edit_spot_outer    .EnableWindow(m_light.m_type == ELight::Spot);
			}

			enum
			{
				IDC_RADIO_AMBIENT=1000, IDC_RADIO_DIRECTIONAL, IDC_RADIO_POINT, IDC_RADIO_SPOT,
				IDC_EDIT_POSITION, IDC_EDIT_DIRECTION, IDC_CHECK_CAMERA_RELATIVE,
				IDC_EDIT_RANGE, IDC_EDIT_FALLOFF, IDC_EDIT_SHADOW_RANGE,
				IDC_EDIT_AMBIENT, IDC_EDIT_DIFFUSE, IDC_EDIT_SPECULAR, IDC_EDIT_SPECULAR_POWER,
				IDC_EDIT_INNER_ANGLE, IDC_EDIT_OUTER_ANGLE,
			};
			BEGIN_DIALOG_EX(0, 0, 218, 190, 0)
				DIALOG_STYLE(DS_3DLOOK | DS_CENTER | DS_MODALFRAME | DS_SHELLFONT | WS_CAPTION | WS_POPUP | WS_VISIBLE)
				DIALOG_EXSTYLE(WS_EX_TOOLWINDOW)
				DIALOG_CAPTION(TEXT("Lighting Options"))
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_AUTORADIOBUTTON (TEXT("Ambient"              ), IDC_RADIO_AMBIENT, 9, 17, 41, 8, 0, WS_EX_LEFT)
				CONTROL_AUTORADIOBUTTON (TEXT("Directional"          ), IDC_RADIO_DIRECTIONAL, 9, 30, 49, 8, 0, WS_EX_LEFT)
				CONTROL_AUTORADIOBUTTON (TEXT("Point"                ), IDC_RADIO_POINT, 9, 43, 32, 8, 0, WS_EX_LEFT)
				CONTROL_AUTORADIOBUTTON (TEXT("Spot"                 ), IDC_RADIO_SPOT, 9, 56, 31, 8, 0, WS_EX_LEFT)
				CONTROL_GROUPBOX        (TEXT("Light Type"           ), IDC_STATIC, 3, 4, 56, 67, 0, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_POSITION      ), 94, 4, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_DIRECTION     ), 94, 20, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_RANGE         ), 94, 50, 39, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_FALLOFF       ), 174, 50, 39, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_SHADOW_RANGE  ), 138, 66, 75, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_AMBIENT       ), 94, 85, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_DIFFUSE       ), 94, 101, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_SPECULAR      ), 94, 117, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_SPECULAR_POWER), 138, 133, 75, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_INNER_ANGLE   ), 94, 149, 39, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT        (    (IDC_EDIT_OUTER_ANGLE   ), 174, 149, 39, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_AUTOCHECKBOX    (TEXT("Camera Relative:"     ), IDC_CHECK_CAMERA_RELATIVE, 138, 38, 70, 8, BS_LEFTTEXT, WS_EX_LEFT)
				CONTROL_LTEXT           (TEXT("Position:"            ), IDC_STATIC, 64, 7, 28, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Ambient (RRGGBB):"    ), IDC_STATIC, 27, 88, 65, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Diffuse (RRGGBB):"    ), IDC_STATIC, 30, 104, 62, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Specular (RRGGBB):"   ), IDC_STATIC, 24, 120, 68, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Specular Power:"      ), IDC_STATIC, 80, 136, 53, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Range:"               ), IDC_STATIC, 68, 53, 24, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Shadow Range:"        ), IDC_STATIC, 84, 69, 52, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Falloff:"             ), IDC_STATIC, 138, 53, 31, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Direction:"           ), IDC_STATIC, 61, 23, 31, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Spot Angles:"         ), IDC_STATIC, 13, 152, 41, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Inner:"               ), IDC_STATIC, 68, 152, 24, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_LTEXT           (TEXT("Outer:"               ), IDC_STATIC, 138, 152, 34, 8, SS_LEFT, WS_EX_RIGHT)
				CONTROL_PUSHBUTTON      (TEXT("Preview"              ), IDRETRY, 5, 170, 50, 14, 0, WS_EX_LEFT)
				CONTROL_PUSHBUTTON      (TEXT("Cancel"               ), IDCANCEL, 163, 170, 50, 14, 0, WS_EX_LEFT)
				CONTROL_DEFPUSHBUTTON   (TEXT("OK"                   ), IDOK, 111, 170, 50, 14, 0, WS_EX_LEFT)
			END_CONTROLS_MAP()
			BEGIN_MSG_MAP(LightingDlg)
				MSG_WM_INITDIALOG(OnInitDialog)
				COMMAND_ID_HANDLER_EX(IDOK                   ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDRETRY                ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDCANCEL               ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDC_RADIO_AMBIENT      ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDC_RADIO_DIRECTIONAL  ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDC_RADIO_POINT        ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDC_RADIO_SPOT         ,OnCommand)
				MESSAGE_RANGE_HANDLER(WM_MOUSEFIRST, WM_MOUSELAST, OnMouse);
			END_MSG_MAP()
		};
	}
}

#endif
