//***************************************************************************************************
// Ldr Measure
//  Copyright (c) Rylogic Ltd 2010
//***************************************************************************************************
#include <string>
#include "pr/common/hash.h"
#include "pr/common/assert.h"
#include "pr/gui/misc.h"
#include "pr/gui/font_helper.h"
#include "pr/renderer11/renderer.h"
#include "pr/linedrawer/ldr_tools.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace ldr
	{
		// Measure *****************************************************************

		// Special context id for private measure objects
		ContextId LdrMeasurePrivateContextId = pr::hash::HashC("Ldr Measure private context id");

		MeasureDlg::MeasureDlg(ReadPointCB read_point_cb ,void* ctx ,pr::Renderer& rdr ,HWND parent)
			:m_read_point_cb(read_point_cb)
			,m_read_point_ctx(ctx)
			,m_rdr(rdr)
			,m_parent(parent)
			,m_btn_point0()
			,m_btn_point1()
			,m_edit_details()
			,m_edit_details_font(pr::gui::CreateFontSimple(pr::gui::font::CourierNew, 16, 6))
			,m_point0(pr::v4Origin)
			,m_point1(pr::v4Origin)
			,m_measurement_gfx(0)
		{
			Create(parent);
		}
		MeasureDlg::~MeasureDlg()
		{
			::DeleteObject(m_edit_details_font);
			PR_ASSERT(PR_DBG, !IsWindow(), "DestroyWindow() must be called before destruction");
		}

		// First display of the ldr measure window
		LRESULT MeasureDlg::OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			InitCommonControls(); // remember to link to comctl32.lib

			// Initalise controls.
			m_btn_point0  .Attach(GetDlgItem(IDC_POINT0));
			m_btn_point1  .Attach(GetDlgItem(IDC_POINT1));
			m_edit_details.Attach(GetDlgItem(IDC_DETAILS));
			//m_edit_details.SetFont(m_edit_details_font);

			DlgResize_Init();
			UpdateMeasurementInfo(false);
			return S_OK;
		}

		// Clean up the tool window
		LRESULT MeasureDlg::OnDestDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			m_edit_details.Detach();
			m_btn_point1.Detach();
			m_btn_point0.Detach();
			return S_OK;
		}

		// Hide the measure window
		LRESULT MeasureDlg::OnClose(WORD, WORD, HWND, BOOL&)
		{
			Show(false);
			return S_OK;
		}

		// Called when a measurement point is set
		LRESULT MeasureDlg::OnSetPoint(WORD, WORD wID, HWND, BOOL&)
		{
			pr::v4& point = wID == IDC_POINT0 ? m_point0 : m_point1;
			point = m_read_point_cb(m_read_point_ctx);
			UpdateMeasurementInfo();
			return S_OK;
		}

		// Set the callback function for reading the world space point
		void MeasureDlg::SetReadPointCB(ReadPointCB read_point_cb, void* ctx)
		{
			m_read_point_cb = read_point_cb;
			m_read_point_ctx = ctx;
		}

		// Set the context for the Read Point callback
		void MeasureDlg::SetReadPointCtx(void* ctx)
		{
			m_read_point_ctx = ctx;
		}

		// Display the window
		void MeasureDlg::Show(bool show)
		{
			bool visible = IsWindowVisible() != 0;
			if (show != visible)
				ShowWindow(show ? SW_SHOW : SW_HIDE);

			if (show)
			{
				if (m_parent == 0 || visible)
				{
					SetWindowPos(HWND_TOPMOST,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE); // bring to front
				}
				else
				{
					RECT p;
					::GetClientRect(m_parent, &p);
					::ClientToScreen(m_parent, (POINT*)&p.left);
					::ClientToScreen(m_parent, (POINT*)&p.right);
					pr::IRect r = pr::WindowBounds(m_hWnd);
					SetWindowPos(HWND_TOPMOST,p.right - r.SizeX(),p.top,0,0,SWP_NOSIZE); // bring to front
				}
			}
			else
			{
				m_measurement_gfx = 0;
				pr::events::Send(Evt_LdrMeasureCloseWindow());
			}
		}

		// Update the text in the measurement details edit control
		void MeasureDlg::UpdateMeasurementInfo(bool raise_event)
		{
			// Remove any existing graphics
			m_measurement_gfx = 0;

			// Create graphics for the two measurement points
			if (m_point0 != m_point1)
			{
				pr::v4 p0 = m_point0;  p0.x = m_point1.x;
				pr::v4 p1 = p0;        p1.y = m_point1.y;

				std::string str;
				GroupStart("Measurement", str);
				Line("dist" , 0xFFFFFFFF, m_point0, m_point1, str);
				Line("distX", 0xFFFF0000, m_point0, p0, str);
				Line("distY", 0xFF00FF00, p0, p1, str);
				Line("distZ", 0xFF0000FF, p1, m_point1, str);
				GroupEnd(str);
				ObjectCont cont;
				AddString(m_rdr, str.c_str(), nullptr, cont, LdrMeasurePrivateContextId);
				if (!cont.empty()) m_measurement_gfx = cont.back();
			}

			float len = pr::Length3(m_point1 - m_point0);
			float dx  = m_point1.x - m_point0.x;
			float dy  = m_point1.y - m_point0.y;
			float dz  = m_point1.z - m_point0.z;
			float dxy = Len2(dx, dy);
			float dyz = Len2(dy, dz);
			float dzx = Len2(dz, dx);

			// Update the text description
			m_edit_details.SetWindowTextA(pr::FmtS(
				"sep: %f\r\n"
				"  x: %f\r\n"
				"  y: %f\r\n"
				"  z: %f\r\n"
				"\r\n"
				" xy: %f\r\n"
				" yz: %f\r\n"
				" zx: %f\r\n"
				"\r\n"
				" ax: %f°\r\n"
				" ay: %f°\r\n"
				" az: %f°\r\n"
				,len
				,dx ,dy ,dz
				,dxy ,dyz ,dzx
				,dyz > pr::maths::tiny && fabs(dy) > pr::maths::tiny ? pr::RadiansToDegrees(pr::Angle(dyz, fabs(dy), fabs(dz))) : 0.0f
				,dzx > pr::maths::tiny && fabs(dx) > pr::maths::tiny ? pr::RadiansToDegrees(pr::Angle(dzx, fabs(dx), fabs(dz))) : 0.0f
				,dxy > pr::maths::tiny && fabs(dx) > pr::maths::tiny ? pr::RadiansToDegrees(pr::Angle(dxy, fabs(dx), fabs(dy))) : 0.0f
				));

			if (raise_event)
				pr::events::Send(Evt_LdrMeasureUpdate());
		}

		// Shutdown the dialog
		void MeasureDlg::Close()
		{
			if (!IsWindow()) return;
			DestroyWindow();
		}

		// Angle *****************************************************************

		// Special context id for private measure objects
		ContextId LdrAngleDlgPrivateContextId = pr::hash::HashC("Ldr Angle Dlg private context id");

		AngleDlg::AngleDlg(ReadPointCB read_point_cb ,void* ctx ,pr::Renderer& rdr ,HWND parent)
			:m_read_point_cb(read_point_cb)
			,m_read_point_ctx(ctx)
			,m_rdr(rdr)
			,m_parent(parent)
			,m_btn_origin()
			,m_btn_point0()
			,m_btn_point1()
			,m_edit_details()
			,m_edit_details_font(pr::gui::CreateFontSimple(pr::gui::font::CourierNew, 16, 6))
			,m_origin(pr::v4Origin)
			,m_point0(pr::v4Origin)
			,m_point1(pr::v4Origin)
			,m_angle_gfx(0)
		{
			Create(parent);
		}
		AngleDlg::~AngleDlg()
		{
			::DeleteObject(m_edit_details_font);
			PR_ASSERT(PR_DBG, !IsWindow(), "DestroyWindow() must be called before destruction");
		}

		// First display of the tool window
		LRESULT AngleDlg::OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			InitCommonControls(); // remember to link to comctl32.lib

			// Initalise controls.
			m_btn_origin  .Attach(GetDlgItem(IDC_ORIGIN));
			m_btn_point0  .Attach(GetDlgItem(IDC_POINT0));
			m_btn_point1  .Attach(GetDlgItem(IDC_POINT1));
			m_edit_details.Attach(GetDlgItem(IDC_DETAILS));
			//m_edit_details.SetFont(m_edit_details_font);

			DlgResize_Init();
			UpdateAngleInfo(false);
			return S_OK;
		}

		// Clean up the tool window
		LRESULT AngleDlg::OnDestDialog(UINT, WPARAM, LPARAM, BOOL&)
		{
			m_edit_details.Detach();
			m_btn_origin.Detach();
			m_btn_point1.Detach();
			m_btn_point0.Detach();
			return S_OK;
		}

		// Hide the measure window
		LRESULT AngleDlg::OnClose(WORD, WORD, HWND, BOOL&)
		{
			Show(false);
			return S_OK;
		}

		// Called when a point is set
		LRESULT AngleDlg::OnSetPoint(WORD, WORD wID, HWND, BOOL&)
		{
			switch (wID)
			{
			case IDC_ORIGIN: m_origin = m_read_point_cb(m_read_point_ctx); break;
			case IDC_POINT0: m_point0 = m_read_point_cb(m_read_point_ctx); break;
			case IDC_POINT1: m_point1 = m_read_point_cb(m_read_point_ctx); break;
			}
			UpdateAngleInfo();
			return S_OK;
		}

		// Set the callback function for reading the world space point
		void AngleDlg::SetReadPointCB(ReadPointCB read_point_cb, void* ctx)
		{
			m_read_point_cb = read_point_cb;
			m_read_point_ctx = ctx;
		}

		// Set the context for the Read Point callback
		void AngleDlg::SetReadPointCtx(void* ctx)
		{
			m_read_point_ctx = ctx;
		}

		// Display the window
		void AngleDlg::Show(bool show)
		{
			bool visible = IsWindowVisible() != 0;
			if (show != visible)
				ShowWindow(show ? SW_SHOW : SW_HIDE);

			if (show)
			{
				if (m_parent == 0 || visible)
				{
					SetWindowPos(HWND_TOPMOST,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE); // bring to front
				}
				else
				{
					RECT p;
					::GetClientRect(m_parent, &p);
					::ClientToScreen(m_parent, (POINT*)&p.left);
					::ClientToScreen(m_parent, (POINT*)&p.right);
					pr::IRect r = pr::WindowBounds(m_hWnd);
					SetWindowPos(HWND_TOPMOST,p.right - r.SizeX(),p.top,0,0,SWP_NOSIZE); // bring to front
				}
			}
			else
			{
				m_angle_gfx = 0;
				pr::events::Send(Evt_LdrAngleDlgCloseWindow());
			}
		}

		// Update the text in the measurement details edit control
		void AngleDlg::UpdateAngleInfo(bool raise_event)
		{
			// Remove any existing graphics
			m_angle_gfx = 0;

			// Create graphics
			if (m_origin != m_point0 || m_origin != m_point1)
			{
				std::string str;
				GroupStart("AngleDlg", str);
				Line("edge0", 0xFFFFFFFF, m_origin, m_point0, str);
				Line("edge1", 0xFFFFFF00, m_origin, m_point1, str);
				Line("edge2", 0xFF00FF00, m_point0, m_point1, str);
				GroupEnd(str);
				ObjectCont cont;
				AddString(m_rdr, str.c_str(), nullptr, cont, LdrAngleDlgPrivateContextId);
				if (!cont.empty()) m_angle_gfx = cont.back();
			}

			pr::v4 e0   = m_point0 - m_origin;
			pr::v4 e1   = m_point1 - m_origin;
			pr::v4 e2   = m_point1 - m_point0;
			float edge0 = pr::Length3(e0);
			float edge1 = pr::Length3(e1);
			float edge2 = pr::Length3(e2);
			float ang   = (edge0 < pr::maths::tiny || edge1 < pr::maths::tiny) ? 0.0f :
				pr::RadiansToDegrees(pr::ACos(pr::Clamp(pr::Dot3(e0,e1) / (edge0 * edge1), -1.0f, 1.0f)));

			// Update the text description
			m_edit_details.SetWindowTextA(pr::FmtS(
				"edge0: %f\r\n"
				"edge1: %f\r\n"
				"edge2: %f\r\n"
				"angle: %f°\r\n"
				,edge0
				,edge1
				,edge2
				,ang
				));

			if (raise_event)
				pr::events::Send(Evt_LdrAngleDlgUpdate());
		}

		// Shutdown the dialog
		void AngleDlg::Close()
		{
			if (!IsWindow()) return;
			DestroyWindow();
		}
	}
}

// Add a manifest dependency on common controls version 6
#if defined _M_IX86
#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif