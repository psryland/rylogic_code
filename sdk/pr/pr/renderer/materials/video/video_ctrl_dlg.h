//************************************
// Video Control Dialog
//  Copyright © Rylogic Ltd 2009
//************************************

#pragma once
#ifndef PR_RDR_VIDEO_CTRL_DLG_H
#define PR_RDR_VIDEO_CTRL_DLG_H

#include "pr/common/min_max_fix.h"
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
//#include <atlcom.h>
//#include <atlmisc.h>
//#include <atlddx.h>
//#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include <atlcrack.h>
//#include <strsafe.h>
//#include "pr/common/fmt.h"
//#include "pr/macros/count_of.h"
//#include "pr/str/prstring.h"
#include "pr/renderer/materials/video/video.h"
#include "pr/gui/round_button_ctrl.h"
	
namespace pr
{
	namespace rdr
	{
		class VideoCtrlDlg
			:public CIndirectDialogImpl<VideoCtrlDlg>
			,public CDialogResize<VideoCtrlDlg>
			,public CMessageFilter
		{
			pr::rdr::Video*       m_video;
			pr::gui::CRndButton   m_play;
			WTL::CButton          m_vol;
			WTL::CTrackBarCtrl    m_position;
			WTL::CStatic          m_clock;
			WTL::CIcon            m_icon_play;
			WTL::CIcon            m_icon_stop;
			WTL::CIcon            m_icon_vol;
			
			VideoCtrlDlg(VideoCtrlDlg const&);
			VideoCtrlDlg& operator =(VideoCtrlDlg const&);
			
		public:
			enum { IDC_SLIDER_VIDEO_POSITION=1000, IDC_BUTTON_VIDEO_PLAY, IDC_BUTTON_VIDEO_VOLUME, IDC_STATIC_VIDEO_CLOCK };
			BEGIN_DIALOG_EX(0, 0, 340, 28, 0)
				DIALOG_STYLE(DS_SHELLFONT | WS_POPUP)
				DIALOG_FONT(8, TEXT("Ms Shell Dlg"))
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_CONTROL(TEXT(""), IDC_SLIDER_VIDEO_POSITION, TRACKBAR_CLASS, WS_TABSTOP | TBS_BOTH | TBS_NOTICKS, 55, 6, 205, 14, 0)
				CONTROL_PUSHBUTTON(TEXT("Play"), IDC_BUTTON_VIDEO_PLAY, 5, 5, 22, 18, BS_ICON | BS_FLAT, 0)
				CONTROL_PUSHBUTTON(TEXT("Volume"), IDC_BUTTON_VIDEO_VOLUME, 29, 5, 21, 18, BS_ICON | BS_FLAT, 0)
				CONTROL_LTEXT(TEXT("00:00:00 / 00:00:00"), IDC_STATIC_VIDEO_CLOCK, 265, 10, 70, 8, SS_LEFT, 0)
			END_CONTROLS_MAP()
			BEGIN_DLGRESIZE_MAP(VideoCtrlDlg)
				DLGRESIZE_CONTROL(IDC_BUTTON_VIDEO_PLAY     ,DLSZ_REPAINT)
				DLGRESIZE_CONTROL(IDC_BUTTON_VIDEO_VOLUME   ,DLSZ_REPAINT)
				DLGRESIZE_CONTROL(IDC_SLIDER_VIDEO_POSITION ,DLSZ_SIZE_X|DLSZ_REPAINT)
				DLGRESIZE_CONTROL(IDC_STATIC_VIDEO_CLOCK    ,DLSZ_MOVE_X|DLSZ_REPAINT)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(VideoCtrlDlg)
				MSG_WM_INITDIALOG(OnInitDialog)
				MSG_WM_CTLCOLORDLG(OnCtlColorDlg)
				MSG_WM_SIZE(OnSize)
				CHAIN_MSG_MAP(CDialogResize<VideoCtrlDlg>) // note this sets bHandled to true... might be better to use OnSize and DlgResize_UpdateLayout
			END_MSG_MAP()
			
			VideoCtrlDlg()
			:m_video(0)
			,m_play()
			,m_vol()
			,m_position()
			,m_clock()
			,m_icon_play()
			,m_icon_stop()
			,m_icon_vol()
			{
			}
			~VideoCtrlDlg()
			{
				if (IsWindow())
					EndDialog(0);
			}
			
			// Set the video to be controlled by this dlg
			void AttachVideo(pr::rdr::Video& video)
			{
				m_video = &video;
			}
			
			// Fit this dialog to the client area of the parent window
			enum DockType { Dock_Bottom, Dock_Top };
			void ResizeToParent(DockType dock = Dock_Bottom)
			{
				PR_ASSERT(PR_DBG, IsWindow() && GetParent(), "");
				CWindow parent = GetParent();
				CRect rectp; parent.GetClientRect(&rectp); parent.ClientToScreen(&rectp);
				CRect rectc; GetWindowRect(&rectc);
				switch (dock)
				{
				default: PR_ASSERT(PR_DBG, false, "not implemented"); break;
				case Dock_Bottom: MoveWindow(rectp.left, rectp.bottom - rectc.Height(), rectp.Width(), rectc.Height()); break;
				case Dock_Top:    MoveWindow(rectp.left, rectp.top                    , rectp.Width(), rectc.Height()); break;
				}
			}
			
			BOOL PreTranslateMessage(MSG* pMsg)
			{
				return IsDialogMessage(pMsg);
			}
			
			// Handler methods
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				CenterWindow(GetParent());
				
				// Attach and initialise controls
				m_play     .Attach(GetDlgItem(IDC_BUTTON_VIDEO_PLAY));
				m_vol      .Attach(GetDlgItem(IDC_BUTTON_VIDEO_VOLUME));
				m_position .Attach(GetDlgItem(IDC_SLIDER_VIDEO_POSITION));
				m_clock    .Attach(GetDlgItem(IDC_STATIC_VIDEO_CLOCK));
				
				// Set icons
				HINSTANCE inst = GetModuleHandle(0);
				m_icon_play = (HICON)::LoadImage(inst, "VideoPlay"/*MAKEINTRESOURCE(IDI_ICON_VIDEO_PLAY)*/ ,IMAGE_ICON, 32, 32, LR_DEFAULTCOLOR);
				m_icon_stop = (HICON)::LoadImage(inst, "VideoStop"/*MAKEINTRESOURCE(IDI_ICON_VIDEO_STOP)*/ ,IMAGE_ICON, 32, 32, LR_DEFAULTCOLOR);
				m_icon_vol  = (HICON)::LoadImage(inst, "VideoVol" /*MAKEINTRESOURCE(IDI_ICON_VIDEO_VOL )*/ ,IMAGE_ICON, 32, 32, LR_DEFAULTCOLOR);
				
				m_play.SetIcon(m_icon_play);
				m_vol .SetIcon(m_icon_vol);
				
				//SetClassLongPtr(m_hWnd, GCL_HBRBACKGROUND, (LONG_PTR)::GetStockObject(BLACK_BRUSH));
				// Remove WS_EX_LAYERED from the window styles
				//SetWindowLong(GWL_EXSTYLE, GetWindowLong(GWL_EXSTYLE) & ~WS_EX_LAYERED);
				//RedrawWindow(hChildWnd, 
				//             NULL, 
				//             NULL, 
				//             RDW_ERASE | RDW_INVALIDATE | RDW_FRAME | RDW_ALLCHILDREN);
				//SetWindowLong(GWL_EXSTYLE, GetWindowLong(GWL_EXSTYLE) | WS_EX_LAYERED);
				//SetLayeredWindowAttributes(m_hWnd, GetBkColor(CDC()), 0, LWA_COLORKEY);
				
				DlgResize_Init(false, false);
				UpdateUI();
				return S_OK;
			}
			LRESULT OnCloseDialog(WORD, WORD, HWND, BOOL&)
			{
				UpdateUI();
				EndDialog(0);
				return S_OK;
			}
			
			// The size of this dialog has changed
			void OnSize(UINT nType, CSize size)
			{
				if (nType == SIZE_MINIMIZED) return;
				DlgResize_UpdateLayout(size.cx, size.cy);
			}
			
			// Return the brush to paint the background with
			HBRUSH OnCtlColorDlg(CDCHandle, CWindow)
			{
				return (HBRUSH)::GetStockObject(BLACK_BRUSH);
			}
			
			// Set the opacity of the window
			void Opacity(BYTE alpha)
			{
				SetWindowLongPtr(GWL_EXSTYLE, pr::SetBits(GetWindowLongPtr(GWL_EXSTYLE), WS_EX_LAYERED, alpha != 1.0f));
				::SetLayeredWindowAttributes(m_hWnd, 0, alpha, LWA_ALPHA);
			}
			
			void UpdateUI()
			{}
			//{
			//	using namespace pr::rdr;

			//	// Update the values
			//	char str[256];
			//	if      (static_cast<CButton const&>(GetDlgItem(IDC_RADIO_AMBIENT    )).GetCheck()) m_light.m_type = pr::rdr::ELight::Ambient;
			//	else if (static_cast<CButton const&>(GetDlgItem(IDC_RADIO_DIRECTIONAL)).GetCheck()) m_light.m_type = pr::rdr::ELight::Directional;
			//	else if (static_cast<CButton const&>(GetDlgItem(IDC_RADIO_POINT      )).GetCheck()) m_light.m_type = pr::rdr::ELight::Point;
			//	else if (static_cast<CButton const&>(GetDlgItem(IDC_RADIO_SPOT       )).GetCheck()) m_light.m_type = pr::rdr::ELight::Spot;
			//	m_edit_position        .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealArrayC(m_light.m_position .ToArray(), 3, str);
			//	m_edit_direction       .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealArrayC(m_light.m_direction.ToArray(), 3, str); pr::Normalise3(m_light.m_direction);
			//	m_camera_relative      = m_check_cam_rel.GetCheck() != 0;
			//	m_light.m_cast_shadows = m_check_cast_shadows.GetCheck() != 0;
			//	m_edit_ambient         .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_ambient.m_aarrggbb, 16, str);  m_light.m_ambient.a() = 0;
			//	m_edit_diffuse         .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_diffuse.m_aarrggbb, 16, str);  m_light.m_diffuse.a() = 0xFF;
			//	m_edit_specular        .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractIntC(m_light.m_specular.m_aarrggbb, 16, str); m_light.m_specular.a() = 0;
			//	m_edit_spec_power      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_specular_power, str);
			//	m_edit_spot_range      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_range, str);
			//	m_edit_spot_inner      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_inner_cos_angle, str); m_light.m_inner_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_inner_cos_angle));
			//	m_edit_spot_outer      .GetWindowText(str, PR_COUNTOF(str)); pr::str::ExtractRealC(m_light.m_outer_cos_angle, str); m_light.m_outer_cos_angle = pr::Cos(pr::DegreesToRadians(m_light.m_outer_cos_angle));

			//	// Enable/Disable controls
			//	m_edit_position      .EnableWindow(m_light.m_type == ELight::Point       || m_light.m_type == ELight::Spot);
			//	m_edit_direction     .EnableWindow(m_light.m_type == ELight::Directional || m_light.m_type == ELight::Spot);
			//	m_check_cam_rel      .EnableWindow(m_light.m_type != ELight::Ambient);
			//	m_check_cast_shadows .EnableWindow(m_light.m_type != ELight::Ambient);
			//	m_edit_ambient       .EnableWindow(true);
			//	m_edit_diffuse       .EnableWindow(m_light.m_type != ELight::Ambient);
			//	m_edit_specular      .EnableWindow(m_light.m_type != ELight::Ambient);
			//	m_edit_spec_power    .EnableWindow(m_light.m_type != ELight::Ambient);
			//	m_edit_spot_range    .EnableWindow(m_light.m_type == ELight::Spot);
			//	m_edit_spot_inner    .EnableWindow(m_light.m_type == ELight::Spot);
			//	m_edit_spot_outer    .EnableWindow(m_light.m_type == ELight::Spot);
			//}

		};
	}
}

#endif
