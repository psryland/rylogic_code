//************************************
// Camera Dialog
//  Copyright (c) Rylogic Ltd 2014
//************************************

#pragma once

#include <string>
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"

namespace pr
{
	namespace camera
	{
		class PositionDlg :public CIndirectDialogImpl<PositionDlg>
		{
			CEdit m_edit_position;
			CEdit m_edit_lookat;
			CEdit m_edit_up;
			CEdit m_edit_horz_fov;
			CButton m_btn_preview;

			// Return the string in an edit control as a std::string
			template <typename Ctrl> std::string GetCtrlText(Ctrl const& ctrl)
			{
				std::string str;
				str.resize(::GetWindowTextLengthA(ctrl) + 1);
				if (!str.empty()) ::GetWindowTextA(ctrl, &str[0], (int)str.size());
				while (!str.empty() && *(--str.end()) == 0) str.resize(str.size() - 1);
				return str;
			}

			// Return a vector as a string
			char const* Str(pr::v4 const& v)
			{
				return pr::FmtS("%3.3f %3.3f %3.3f",v.x,v.y,v.z);
			}
			char const* Str(float f)
			{
				return pr::FmtS("%3.3f",f);
			}

		public:
			pr::Camera m_cam;
			bool m_allow_preview;

			PositionDlg(bool allow_preview = false)
				:m_edit_position()
				,m_edit_lookat()
				,m_edit_up()
				,m_edit_horz_fov()
				,m_btn_preview()
				,m_cam()
				,m_allow_preview(allow_preview)
			{}
			~PositionDlg()
			{
				if (IsWindow())
					EndDialog(0);
			}

			// Handler methods
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				CenterWindow(GetParent());

				// Attach and initialise controls
				m_edit_position.Attach(GetDlgItem(IDC_EDIT_POSITION));
				m_edit_lookat  .Attach(GetDlgItem(IDC_EDIT_LOOKAT  ));
				m_edit_up      .Attach(GetDlgItem(IDC_EDIT_UP      ));
				m_edit_horz_fov.Attach(GetDlgItem(IDC_EDIT_HORZ_FOV));
				m_btn_preview  .Attach(GetDlgItem(IDC_BTN_PREVIEW  ));
				m_btn_preview.ShowWindow(m_allow_preview ? SW_SHOW : SW_HIDE);

				PopulateControls();
				return TRUE;
			}
			void OnCommand(UINT, int nID, CWindow)
			{
				ReadValues();
				PopulateControls();
				switch (nID)
				{
				default:
					SetMsgHandled(FALSE);
					break;
				case IDOK:
				case IDCANCEL:
					EndDialog(nID);
					return;
				case IDC_BTN_PREVIEW:
					Preview();
					break;
				}
			}
			void ReadValues()
			{
				// Update the values
				auto position = pr::To<pr::v3>(GetCtrlText(m_edit_position)).w1();
				auto lookat   = pr::To<pr::v3>(GetCtrlText(m_edit_lookat)).w1();
				auto up       = pr::To<pr::v3>(GetCtrlText(m_edit_up)).w0();
				auto hfov     = pr::DegreesToRadians(pr::To<float>(GetCtrlText(m_edit_horz_fov)));

				if (pr::FEql3(lookat - position, pr::v4Zero))
					lookat = position + pr::v4ZAxis;
				if (pr::Parallel(lookat - position, up))
					up = pr::Perpendicular(up);
		
				m_cam.LookAt(position, lookat, up);
				m_cam.FovX(hfov);
			}
			void PopulateControls()
			{
				m_edit_position.SetWindowTextA(Str(m_cam.CameraToWorld().pos));
				m_edit_lookat  .SetWindowTextA(Str(m_cam.FocusPoint()));
				m_edit_up      .SetWindowTextA(Str(m_cam.CameraToWorld().y));
				m_edit_horz_fov.SetWindowTextA(Str(pr::RadiansToDegrees(m_cam.FovX())));
			}
			virtual void Preview()
			{
				// Subclass for preview ability
			}

			enum
			{
				IDC_EDIT_POSITION = 0x1000,
				IDC_EDIT_LOOKAT,
				IDC_EDIT_UP,
				IDC_EDIT_HORZ_FOV,
				IDC_BTN_PREVIEW,
			};

			BEGIN_DIALOG_EX(0, 0, 169, 93, 0)
				DIALOG_STYLE(DS_3DLOOK | DS_CENTER | DS_MODALFRAME | DS_SHELLFONT | WS_CAPTION | WS_POPUP | WS_VISIBLE)
				DIALOG_EXSTYLE(WS_EX_TOOLWINDOW)
				DIALOG_CAPTION(TEXT("Position Camera"))
				DIALOG_FONT(8, TEXT("MS Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_RTEXT(TEXT("Position:"), IDC_STATIC, 12, 11, 28, 8, SS_RIGHT, WS_EX_LEFT)
				CONTROL_RTEXT(TEXT("Look At:"), IDC_STATIC, 12, 27, 28, 8, SS_RIGHT, WS_EX_LEFT)
				CONTROL_RTEXT(TEXT("Up:"), IDC_STATIC, 28, 43, 12, 8, SS_RIGHT, WS_EX_LEFT)
				CONTROL_RTEXT(TEXT("Horizonal FOV (deg):"), IDC_STATIC, 38, 59, 71, 8, SS_RIGHT, WS_EX_LEFT)
				CONTROL_EDITTEXT(IDC_EDIT_POSITION, 48, 8, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT(IDC_EDIT_LOOKAT, 48, 24, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT(IDC_EDIT_UP, 48, 40, 119, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_EDITTEXT(IDC_EDIT_HORZ_FOV, 114, 56, 53, 14, ES_AUTOHSCROLL, WS_EX_LEFT)
				CONTROL_PUSHBUTTON(TEXT("Preview"), IDC_BTN_PREVIEW, 5, 75, 50, 14, 0, WS_EX_LEFT)
				CONTROL_PUSHBUTTON(TEXT("Cancel"), IDCANCEL, 117, 75, 50, 14, 0, WS_EX_LEFT)
				CONTROL_DEFPUSHBUTTON(TEXT("OK"), IDOK, 62, 75, 50, 14, 0, WS_EX_LEFT)
			END_CONTROLS_MAP()
			BEGIN_MSG_MAP(PositionDlg)
				MSG_WM_INITDIALOG(OnInitDialog)
				COMMAND_ID_HANDLER_EX(IDOK    ,OnCommand)
				COMMAND_ID_HANDLER_EX(IDCANCEL,OnCommand)
			END_MSG_MAP()
		};
	}
}