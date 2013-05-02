//************************************
// Lighting Dialog
//  Copyright © Rylogic Ltd 2009
//************************************

#pragma once
#ifndef PR_RDR_LIGHTS_LIGHT_DLG_H
#define PR_RDR_LIGHTS_LIGHT_DLG_H

#include "pr/common/min_max_fix.h"
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>
#include "pr/macros/count_of.h"
#include "pr/common/fmt.h"
#include "pr/str/prstring.h"
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
			CButton m_check_cast_shadows;
			CEdit   m_edit_ambient;
			CEdit   m_edit_diffuse;
			CEdit   m_edit_specular;
			CEdit   m_edit_spec_power;
			CEdit   m_edit_spot_range;
			CEdit   m_edit_spot_inner;
			CEdit   m_edit_spot_outer;

		public:
			pr::rdr::Light m_light;
			bool m_camera_relative;

			enum
			{
				IDC_RADIO_AMBIENT=1000, IDC_RADIO_DIRECTIONAL, IDC_RADIO_POINT, IDC_RADIO_SPOT,
				IDC_EDIT_POSITION, IDC_EDIT_DIRECTION, IDC_CHECK_CAM_RELATIVE, IDC_CHECK_CAST_SHADOWS,
				IDC_EDIT_AMBIENT, IDC_EDIT_DIFFUSE, IDC_EDIT_SPECULAR, IDC_EDIT_SPECULAR_POWER,
				IDC_EDIT_SPOT_INNER_ANGLE, IDC_EDIT_SPOT_OUTER_ANGLE, IDC_EDIT_SPOT_RANGE
			};
			BEGIN_DIALOG_EX(0, 0, 191, 166, 0)
				DIALOG_STYLE(DS_3DLOOK | DS_CENTER | DS_MODALFRAME | DS_SHELLFONT | WS_CAPTION | WS_POPUP | WS_VISIBLE)
				DIALOG_EXSTYLE(WS_EX_TOOLWINDOW)
				DIALOG_CAPTION(TEXT("Lighting Options"))
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
				END_DIALOG()
				BEGIN_CONTROLS_MAP()
					CONTROL_AUTORADIOBUTTON(TEXT("Ambient"), IDC_RADIO_AMBIENT, 9, 9, 42, 10, WS_TABSTOP|WS_GROUP, 0)
					CONTROL_AUTORADIOBUTTON(TEXT("Point"), IDC_RADIO_POINT, 9, 19, 32, 10, WS_TABSTOP, 0)
					CONTROL_AUTORADIOBUTTON(TEXT("Spot"), IDC_RADIO_SPOT, 9, 29, 31, 10, WS_TABSTOP, 0)
					CONTROL_AUTORADIOBUTTON(TEXT("Directional"), IDC_RADIO_DIRECTIONAL, 9, 39, 49, 10, WS_TABSTOP, 0)
					CONTROL_EDITTEXT(IDC_EDIT_POSITION, 87, 7, 94, 14, WS_GROUP|ES_AUTOHSCROLL, 0)
					CONTROL_EDITTEXT(IDC_EDIT_DIRECTION, 87, 23, 94, 14, ES_AUTOHSCROLL, 0)
					CONTROL_AUTOCHECKBOX(TEXT("Camera Relative:"), IDC_CHECK_CAM_RELATIVE, 87, 40, 92, 8, BS_LEFT, WS_EX_RIGHT)
					CONTROL_AUTOCHECKBOX(TEXT("Cast Shadows:"), IDC_CHECK_CAST_SHADOWS, 95, 52, 84, 8, BS_LEFT, WS_EX_RIGHT)
					CONTROL_EDITTEXT(IDC_EDIT_AMBIENT, 87, 64, 94, 14, ES_AUTOHSCROLL, 0)
					CONTROL_EDITTEXT(IDC_EDIT_DIFFUSE, 87, 80, 94, 14, ES_AUTOHSCROLL, 0)
					CONTROL_EDITTEXT(IDC_EDIT_SPECULAR, 87, 96, 94, 14, ES_AUTOHSCROLL, 0)
					CONTROL_EDITTEXT(IDC_EDIT_SPECULAR_POWER, 87, 112, 94, 14, ES_AUTOHSCROLL, 0)
					CONTROL_PUSHBUTTON(TEXT("Cancel"), IDCANCEL, 7, 146, 50, 14, 0, 0)
					CONTROL_PUSHBUTTON(TEXT("Preview"), IDRETRY, 70, 146, 50, 14, 0, 0)
					CONTROL_DEFPUSHBUTTON(TEXT("OK"), IDOK, 133, 146, 50, 14, 0, 0)
					CONTROL_LTEXT(TEXT("Position:"), IDC_STATIC, 57, 10, 28, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Direction:"), IDC_STATIC, 53, 26, 32, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Ambient (AARRGGBB):"), IDC_STATIC, 12, 67, 73, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Diffuse (AARRGGBB):"), IDC_STATIC, 15, 83, 70, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Specular (AARRGGBB):"), IDC_STATIC, 11, 99, 74, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Specular Power:"), IDC_STATIC, 32, 115, 53, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Inner:"), IDC_STATIC, 84, 130, 21, 8, SS_LEFT, 0)
					CONTROL_LTEXT(TEXT("Outer:"), IDC_STATIC, 133, 130, 22, 8, SS_LEFT, 0)
					CONTROL_EDITTEXT(IDC_EDIT_SPOT_INNER_ANGLE, 106, 128, 25, 14, ES_AUTOHSCROLL, 0)
					CONTROL_EDITTEXT(IDC_EDIT_SPOT_OUTER_ANGLE, 156, 128, 25, 14, ES_AUTOHSCROLL, 0)
					CONTROL_LTEXT(TEXT("Spot - Range:"), IDC_STATIC, 9, 130, 46, 8, SS_LEFT, 0)
					CONTROL_EDITTEXT(IDC_EDIT_SPOT_RANGE, 56, 128, 25, 14, ES_AUTOHSCROLL, 0)
				END_CONTROLS_MAP()
				BEGIN_MSG_MAP(LightingDlg)
					MSG_WM_INITDIALOG(OnInitDialog)
					COMMAND_ID_HANDLER_EX(IDOK                  ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDRETRY               ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDCANCEL              ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDC_RADIO_AMBIENT     ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDC_RADIO_DIRECTIONAL ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDC_RADIO_POINT       ,OnCommand)
					COMMAND_ID_HANDLER_EX(IDC_RADIO_SPOT        ,OnCommand)
				END_MSG_MAP()

				explicit LightingDlg(Preview const& preview)
					:m_preview(preview)
					,m_light()
					,m_camera_relative(true)
				{}
				~LightingDlg()
				{
					if (IsWindow()) EndDialog(0);
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
					m_check_cam_rel      .Attach(GetDlgItem(IDC_CHECK_CAM_RELATIVE));
					m_check_cast_shadows .Attach(GetDlgItem(IDC_CHECK_CAST_SHADOWS));
					m_edit_ambient       .Attach(GetDlgItem(IDC_EDIT_AMBIENT));
					m_edit_diffuse       .Attach(GetDlgItem(IDC_EDIT_DIFFUSE));
					m_edit_specular      .Attach(GetDlgItem(IDC_EDIT_SPECULAR));
					m_edit_spec_power    .Attach(GetDlgItem(IDC_EDIT_SPECULAR_POWER));
					m_edit_spot_range    .Attach(GetDlgItem(IDC_EDIT_SPOT_RANGE));
					m_edit_spot_inner    .Attach(GetDlgItem(IDC_EDIT_SPOT_INNER_ANGLE));
					m_edit_spot_outer    .Attach(GetDlgItem(IDC_EDIT_SPOT_OUTER_ANGLE));

					int ids[ELight::NumberOf];
					ids[ELight::Ambient]     = IDC_RADIO_AMBIENT;
					ids[ELight::Directional] = IDC_RADIO_DIRECTIONAL;
					ids[ELight::Point]       = IDC_RADIO_POINT;
					ids[ELight::Spot]        = IDC_RADIO_SPOT;
					CheckRadioButton(IDC_RADIO_AMBIENT, IDC_RADIO_SPOT, ids[m_light.m_type]);
					m_edit_position      .SetWindowText(pr::FmtS("%3.3f %3.3f %3.3f" ,m_light.m_position.x ,m_light.m_position.y ,m_light.m_position.z));
					m_edit_direction     .SetWindowText(pr::FmtS("%3.3f %3.3f %3.3f" ,m_light.m_direction.x ,m_light.m_direction.y ,m_light.m_direction.z));
					m_check_cam_rel      .SetCheck(m_camera_relative);
					m_check_cast_shadows .SetCheck(m_light.m_cast_shadows);
					m_edit_ambient       .SetWindowTextA(pr::FmtS("%8.8X" ,m_light.m_ambient.m_aarrggbb));
					m_edit_diffuse       .SetWindowTextA(pr::FmtS("%8.8X" ,m_light.m_diffuse.m_aarrggbb));
					m_edit_specular      .SetWindowTextA(pr::FmtS("%8.8X" ,m_light.m_specular.m_aarrggbb));
					m_edit_spec_power    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + m_light.m_specular_power)));
					m_edit_spot_range    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + m_light.m_range)));
					m_edit_spot_inner    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_inner_cos_angle)))));
					m_edit_spot_outer    .SetWindowTextA(pr::FmtS("%d" ,(int)(0.5f + pr::RadiansToDegrees(pr::ACos(m_light.m_outer_cos_angle)))));

					UpdateUI();
					return TRUE;
				}
				void OnCommand(UINT, int nID, CWindow)
				{
					if (nID == IDOK || nID == IDCANCEL) { UpdateUI(); EndDialog(nID); return; }
					if (nID == IDRETRY)                 { UpdateUI(); m_preview(m_light, m_camera_relative); return; }
					if (nID == IDC_RADIO_AMBIENT || nID == IDC_RADIO_DIRECTIONAL || nID == IDC_RADIO_POINT || nID == IDC_RADIO_SPOT) { UpdateUI(); return; }
					SetMsgHandled(FALSE);
				}

				void UpdateUI()
				{
					// Update the values
					char str[256];
					if (m_btn_ambient      .GetCheck()) m_light.m_type = pr::rdr::ELight::Ambient;
					if (m_btn_directional  .GetCheck()) m_light.m_type = pr::rdr::ELight::Directional;
					if (m_btn_point        .GetCheck()) m_light.m_type = pr::rdr::ELight::Point;
					if (m_btn_spot         .GetCheck()) m_light.m_type = pr::rdr::ELight::Spot;
					m_edit_position        .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealArrayC(m_light.m_position .ToArray(), 3, str);
					m_edit_direction       .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealArrayC(m_light.m_direction.ToArray(), 3, str); pr::Normalise3(m_light.m_direction);
					m_camera_relative      = m_check_cam_rel.GetCheck() != 0;
					m_light.m_cast_shadows = m_check_cast_shadows.GetCheck() != 0;
					m_edit_ambient         .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_ambient.m_aarrggbb, 16, str);  m_light.m_ambient.a() = 0;
					m_edit_diffuse         .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_diffuse.m_aarrggbb, 16, str);  m_light.m_diffuse.a() = 0xFF;
					m_edit_specular        .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_specular.m_aarrggbb, 16, str); m_light.m_specular.a() = 0;
					m_edit_spec_power      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_specular_power, str);
					m_edit_spot_range      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_range, str);
					m_edit_spot_inner      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_inner_cos_angle, str); m_light.m_inner_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_inner_cos_angle));
					m_edit_spot_outer      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_outer_cos_angle, str); m_light.m_outer_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_outer_cos_angle));

					// Enable/Disable controls
					m_edit_position      .EnableWindow(m_light.m_type == ELight::Point       || m_light.m_type == ELight::Spot);
					m_edit_direction     .EnableWindow(m_light.m_type == ELight::Directional || m_light.m_type == ELight::Spot);
					m_check_cam_rel      .EnableWindow(m_light.m_type != ELight::Ambient);
					m_check_cast_shadows .EnableWindow(m_light.m_type != ELight::Ambient);
					m_edit_ambient       .EnableWindow(true);
					m_edit_diffuse       .EnableWindow(m_light.m_type != ELight::Ambient);
					m_edit_specular      .EnableWindow(m_light.m_type != ELight::Ambient);
					m_edit_spec_power    .EnableWindow(m_light.m_type != ELight::Ambient);
					m_edit_spot_range    .EnableWindow(m_light.m_type == ELight::Spot);
					m_edit_spot_inner    .EnableWindow(m_light.m_type == ELight::Spot);
					m_edit_spot_outer    .EnableWindow(m_light.m_type == ELight::Spot);
				}
		};
	}
}

#endif
