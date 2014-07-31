//***************************************************************************************************
// Ldr Measure
//  Copyright (c) Rylogic Ltd 2010
//***************************************************************************************************

#pragma once

#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atldlgs.h>
#include "pr/common/min_max_fix.h"
#include "pr/common/events.h"
#include "pr/renderer11/forward.h"
#include "pr/linedrawer/ldr_object.h"

namespace pr
{
	namespace ldr
	{
		// Callback function for reading a world space point
		typedef pr::v4 (__stdcall *ReadPointCB)(void* ctx);

		// Measure *****************************************************************

		// Special context id for private measure objects
		extern ContextId LdrMeasurePrivateContextId;

		class MeasureDlg
			:public CIndirectDialogImpl<MeasureDlg>
			,public CDialogResize<MeasureDlg>
		{
			ReadPointCB           m_read_point_cb;        // The callback for reading a world space point
			void*                 m_read_point_ctx;       // Context for the callback function
			pr::Renderer&         m_rdr;                  // Reference to the renderer
			HWND                  m_parent;               // The parent window containing the 3d view
			WTL::CButton          m_btn_point0;           // Set Point 0 button
			WTL::CButton          m_btn_point1;           // Set Point 1 button
			WTL::CEdit            m_edit_details;         // Measurement details
			HFONT                 m_edit_details_font;    // The font to print the measurement details in
			pr::v4                m_point0;               // The start of the measurement
			pr::v4                m_point1;               // The end of the measurement
			pr::ldr::LdrObjectPtr m_measurement_gfx;      // Graphics created by this Measure tool

			MeasureDlg(MeasureDlg const&);
			MeasureDlg& operator=(MeasureDlg const&);

		public:
			enum {IDC_POINT0=1000, IDC_POINT1, IDC_DETAILS};
			BEGIN_DIALOG_EX(0, 0, 83, 134, 0)
				DIALOG_STYLE(DS_SHELLFONT | WS_CAPTION | WS_GROUP | WS_MAXIMIZEBOX | WS_POPUP | WS_THICKFRAME | WS_SYSMENU)
				DIALOG_EXSTYLE(WS_EX_TOOLWINDOW | WS_EX_APPWINDOW)
				DIALOG_CAPTION(TEXT("Measure"))
				DIALOG_FONT(8, "MS Shell Dlg")
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_PUSHBUTTON(TEXT("Set Point 0"), IDC_POINT0, 0, 0, 42, 14, 0, 0)
				CONTROL_PUSHBUTTON(TEXT("Set Point 1"), IDC_POINT1, 41, 0, 42, 14, 0, 0)
				CONTROL_EDITTEXT(IDC_DETAILS, 1, 15, 80, 118, ES_AUTOHSCROLL | ES_MULTILINE, 0)
			END_CONTROLS_MAP()
			BEGIN_DLGRESIZE_MAP(MeasureDlg)
				DLGRESIZE_CONTROL(IDC_POINT0  ,0)
				DLGRESIZE_CONTROL(IDC_POINT1  ,0)
				DLGRESIZE_CONTROL(IDC_DETAILS ,DLSZ_SIZE_X|DLSZ_SIZE_Y)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(MeasureDlg)
				MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
				MESSAGE_HANDLER(WM_DESTROY    ,OnDestDialog)
				COMMAND_ID_HANDLER(IDOK       ,OnClose)
				COMMAND_ID_HANDLER(IDCLOSE    ,OnClose)
				COMMAND_ID_HANDLER(IDCANCEL   ,OnClose)
				COMMAND_HANDLER(IDC_POINT0 ,BN_CLICKED  ,OnSetPoint)
				COMMAND_HANDLER(IDC_POINT1 ,BN_CLICKED  ,OnSetPoint)
				CHAIN_MSG_MAP(CDialogResize<MeasureDlg>)
			END_MSG_MAP()

			MeasureDlg(ReadPointCB read_point_cb ,void* ctx ,pr::Renderer& rdr ,HWND parent = 0);
			~MeasureDlg();

			// Handler methods
			LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnDestDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnClose(WORD, WORD, HWND, BOOL&);
			LRESULT OnSetPoint(WORD, WORD, HWND, BOOL&);

			pr::ldr::LdrObjectPtr Gfx() const { return m_measurement_gfx; }

			void SetReadPointCB(ReadPointCB read_point_cb, void* ctx);
			void SetReadPointCtx(void* ctx);
			void Show(bool show);
			void UpdateMeasurementInfo(bool raise_event = true);
			void Close();
		};

		// Events
		struct Evt_LdrMeasureCloseWindow {}; // The measurement window has closed
		struct Evt_LdrMeasureUpdate {};      // The measurement info has been updated

		// Angle *****************************************************************

		// Special context id for private angle dlg objects
		extern ContextId LdrAngleDlgPrivateContextId;

		class AngleDlg
			:public CIndirectDialogImpl<AngleDlg>
			,public CDialogResize<AngleDlg>
		{
			ReadPointCB              m_read_point_cb;        // The callback for reading a world space point
			void*                    m_read_point_ctx;       // Context for the callback function
			pr::Renderer&            m_rdr;                  // Reference to the renderer
			HWND                     m_parent;               // The parent window containing the 3d view
			WTL::CButton             m_btn_origin;           // Set Origin button
			WTL::CButton             m_btn_point0;           // Set Point 0 button
			WTL::CButton             m_btn_point1;           // Set Point 1 button
			WTL::CEdit               m_edit_details;         // Angle details
			HFONT                    m_edit_details_font;    // The font to print the measurement details in
			pr::v4                   m_origin;               // The origin of the angle measurement
			pr::v4                   m_point0;               // Point0 of the angle measurement
			pr::v4                   m_point1;               // Point1 of the angle measurement
			pr::ldr::LdrObjectPtr    m_angle_gfx;            // Graphics created by this tool

			AngleDlg(AngleDlg const&);
			AngleDlg& operator=(AngleDlg const&);

		public:
			enum {IDC_ORIGIN=1000, IDC_POINT0, IDC_POINT1, IDC_DETAILS};
			BEGIN_DIALOG_EX(0, 0, 83, 134, 0)
				DIALOG_STYLE(DS_SHELLFONT | WS_CAPTION | WS_GROUP | WS_MAXIMIZEBOX | WS_POPUP | WS_THICKFRAME | WS_SYSMENU)
				DIALOG_EXSTYLE(WS_EX_TOOLWINDOW | WS_EX_APPWINDOW)
				DIALOG_CAPTION("Angle")
				DIALOG_FONT(8, "MS Shell Dlg")
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_PUSHBUTTON(TEXT("Origin"), IDC_ORIGIN, 0, 0, 28, 14, 0, 0)
				CONTROL_PUSHBUTTON(TEXT("Point0"), IDC_POINT0, 28, 0, 28, 14, 0, 0)
				CONTROL_PUSHBUTTON(TEXT("Point1"), IDC_POINT1, 55, 0, 28, 14, 0, 0)
				CONTROL_EDITTEXT(IDC_DETAILS, 1, 15, 80, 118, ES_AUTOHSCROLL | ES_MULTILINE, 0)
			END_CONTROLS_MAP()
			BEGIN_DLGRESIZE_MAP(AngleDlg)
				DLGRESIZE_CONTROL(IDC_ORIGIN  ,0)
				DLGRESIZE_CONTROL(IDC_POINT0  ,0)
				DLGRESIZE_CONTROL(IDC_POINT1  ,0)
				DLGRESIZE_CONTROL(IDC_DETAILS ,DLSZ_SIZE_X|DLSZ_SIZE_Y)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(AngleDlg)
				MESSAGE_HANDLER(WM_INITDIALOG ,OnInitDialog)
				MESSAGE_HANDLER(WM_DESTROY    ,OnDestDialog)
				COMMAND_ID_HANDLER(IDOK       ,OnClose)
				COMMAND_ID_HANDLER(IDCLOSE    ,OnClose)
				COMMAND_ID_HANDLER(IDCANCEL   ,OnClose)
				COMMAND_HANDLER(IDC_ORIGIN ,BN_CLICKED  ,OnSetPoint)
				COMMAND_HANDLER(IDC_POINT0 ,BN_CLICKED  ,OnSetPoint)
				COMMAND_HANDLER(IDC_POINT1 ,BN_CLICKED  ,OnSetPoint)
				CHAIN_MSG_MAP(CDialogResize<AngleDlg>)
			END_MSG_MAP()

			AngleDlg(ReadPointCB read_point_cb ,void* ctx ,pr::Renderer& rdr ,HWND parent = 0);
			~AngleDlg();

			// Handler methods
			LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnDestDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnClose(WORD, WORD, HWND, BOOL&);
			LRESULT OnSetPoint(WORD, WORD, HWND, BOOL&);

			pr::ldr::LdrObjectPtr Gfx() const { return m_angle_gfx; }

			void SetReadPointCB(ReadPointCB read_point_cb, void* ctx);
			void SetReadPointCtx(void* ctx);
			void Show(bool show);
			void UpdateAngleInfo(bool raise_event = true);
			void Close();
		};

		// Events
		struct Evt_LdrAngleDlgCloseWindow {}; // The angle dlg window has closed
		struct Evt_LdrAngleDlgUpdate {};      // The angle info has been updated
	}
}
