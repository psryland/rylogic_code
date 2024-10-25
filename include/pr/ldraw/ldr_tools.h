//***************************************************************************************************
// Ldr Measure
//  Copyright (c) Rylogic Ltd 2010
//***************************************************************************************************
#pragma once
#include <memory>
#include "pr/common/guid.h"
#include "pr/maths/maths.h"
#include "pr/gui/wingui.h"
#include "pr/ldraw/ldr_object.h"

namespace pr::ldr
{
	// Callback function for reading a world space point
	using ReadPointCB = v4(__stdcall*)(void* ctx);

	// A UI for measuring distances within a 3D environment
	struct alignas(16) LdrMeasureUI :gui::Form
	{
	private:
		enum { ID_BTN_SET0 = 100, ID_BTN_SET1, ID_TB_VALUES };

		// Members
		Guid         m_context_id;     // A graphics context Id
		ReadPointCB  m_read_point_cb;  // The callback for reading a world space point
		void* m_read_point_ctx; // Context for the callback function
		Renderer& m_rdr;            // Reference to the renderer
		LdrObjectPtr m_gfx;            // Graphics created by this tool
		gui::Button  m_btn_set0;       // Set the start point for measuring
		gui::Button  m_btn_set1;       // Set the end point for measuring
		gui::TextBox m_tb_values;      // The measured values
		v4           m_point0;         // The start of the measurement
		v4           m_point1;         // The end of the measurement

	public:

		LdrMeasureUI(HWND parent, ReadPointCB read_point_cb, void* ctx, Renderer& rdr)
			:Form(Params<>()
				.parent(parent).name("ldr-measure-ui").title(L"Measure Distances")
				.wh(300, 150).style_ex('+', WS_EX_TOOLWINDOW)
				.hide_on_close(true).pin_window(true)
				.wndclass(RegisterWndClass<LdrMeasureUI>()))
			, m_context_id(GenerateGUID())
			, m_read_point_cb(read_point_cb)
			, m_read_point_ctx(ctx)
			, m_rdr(rdr)
			, m_gfx()
			, m_btn_set0(gui::Button::Params<>().parent(this_).name("btn-set0").id(ID_BTN_SET0).xy(0, 0).anchor(EAnchor::TopLeft).text(L"Set Point 0"))
			, m_btn_set1(gui::Button::Params<>().parent(this_).name("btn-set1").id(ID_BTN_SET1).xy(Left | RightOf | ID_BTN_SET0, 0).anchor(EAnchor::TopLeft).text(L"Set Point 1"))
			, m_tb_values(gui::TextBox::Params<>().parent(this_).name("tb-values").id(ID_TB_VALUES).wh(Fill, Fill).xy(0, Top | BottomOf | ID_BTN_SET0).anchor(EAnchor::All).multiline(true))
			, m_point0(v4Origin)
			, m_point1(v4Origin)
		{
			CreateHandle();
			m_btn_set0.Click += std::bind(&LdrMeasureUI::HandleSetPoint, this, _1, _2);
			m_btn_set1.Click += std::bind(&LdrMeasureUI::HandleSetPoint, this, _1, _2);
			UpdateMeasurementInfo();
		}

		// Set the callback function used to read points in the 3d environment
		void SetReadPoint(ReadPointCB cb, void* ctx)
		{
			m_read_point_cb = cb;
			m_read_point_ctx = ctx;
		}

		// Graphics associated with the this measure tool
		LdrObjectPtr Gfx() const
		{
			return m_gfx;
		}

		// The context id for graphics objects belonging to this measurement UI
		Guid GfxContextId() const
		{
			return m_context_id;
		}

		// Handle a 'Set Point' button being clicked
		void HandleSetPoint(gui::Button& btn, gui::EmptyArgs const&)
		{
			auto dummy = v4{};
			auto& point =
				&btn == &m_btn_set0 ? m_point0 :
				&btn == &m_btn_set1 ? m_point1 :
				dummy;

			// Read the 3D point from the scene
			point = m_read_point_cb(m_read_point_ctx);

			// Update the measurement data
			UpdateMeasurementInfo();
		}

		// Update the text in the measurement details text box
		void UpdateMeasurementInfo()
		{
			using namespace maths;

			// Remove any existing graphics
			m_gfx = nullptr;

			// Create graphics for the two measurement points
			if (m_point0 != m_point1)
			{
				auto p0 = v4(m_point1.x, m_point0.y, m_point0.z, 1.0f);
				auto p1 = v4(m_point1.x, m_point1.y, m_point0.z, 1.0f);

				auto str = std::string{};
				GroupStart(str, "Measurement");
				Line(str, "dist", 0xFFFFFFFF, m_point0, m_point1);
				Line(str, "distX", 0xFFFF0000, m_point0, p0);
				Line(str, "distY", 0xFF00FF00, p0, p1);
				Line(str, "distZ", 0xFF0000FF, p1, m_point1);
				GroupEnd(str);

				ParseResult out;
				ParseString(m_rdr, str.c_str(), out, GfxContextId());
				if (!out.m_objects.empty())
					m_gfx = out.m_objects.back();
			}

			auto dx = m_point1.x - m_point0.x;
			auto dy = m_point1.y - m_point0.y;
			auto dz = m_point1.z - m_point0.z;
			auto len = Len(dx, dy, dz);
			auto dxy = Len(dx, dy);
			auto dyz = Len(dy, dz);
			auto dzx = Len(dz, dx);
			auto angx = dyz > tinyf && fabs(dy) > tinyf ? RadiansToDegrees(Angle(dyz, fabs(dy), fabs(dz))) : 0.0f;
			auto angy = dzx > tinyf && fabs(dx) > tinyf ? RadiansToDegrees(Angle(dzx, fabs(dx), fabs(dz))) : 0.0f;
			auto angz = dxy > tinyf && fabs(dx) > tinyf ? RadiansToDegrees(Angle(dxy, fabs(dx), fabs(dy))) : 0.0f;

			// Update the text description
			m_tb_values.Text(FmtS(
				L"     sep: %f %f %f  (%f) \r\n"
				L"xy,yz,zx: %f %f %f \r\n"
				L" ang (\uC2B0): %f %f %f \r\n"
				, dx, dy, dz, len
				, dxy, dyz, dzx
				, angx, angy, angz));

			// Notify the measurement data changed
			MeasurementChanged(*this, gui::EmptyArgs());
		}

		// Raised when the measurement data changes
		// MeasurementChanged += [&](LdrMeasureUI&,EmptyArgs const&){}
		gui::EventHandler<LdrMeasureUI&, gui::EmptyArgs const&> MeasurementChanged;
	};

	// A UI for measuring angles within a 3D environment
	struct alignas(16) LdrAngleUI :gui::Form
	{
	private:
		enum { ID_BTN_ORIG = 100, ID_BTN_SET0, ID_BTN_SET1, ID_TB_VALUES };

		// Members
		Guid         m_context_id;     // A graphics context Id
		ReadPointCB      m_read_point_cb;  // The callback for reading a world space point
		void* m_read_point_ctx; // Context for the callback function
		Renderer& m_rdr;            // Reference to the renderer
		LdrObjectPtr     m_gfx;            // Graphics created by this tool
		gui::Button  m_btn_orig;       // Set the origin for angle measurement
		gui::Button  m_btn_set0;       // Set the point 0 for angle measurement
		gui::Button  m_btn_set1;       // Set the point 1 for angle measurement
		gui::TextBox m_tb_values;      // The measured values
		v4           m_origin;         // The angle apex
		v4           m_point0;         // The point0
		v4           m_point1;         // The end of the measurement

	public:

		LdrAngleUI(HWND parent, ReadPointCB read_point_cb, void* ctx, Renderer& rdr)
			:Form(Params<>()
				.parent(parent).name("ldr-angle-ui").title(L"Measure Angles")
				.wh(220, 186).style_ex('+', WS_EX_TOOLWINDOW)
				.hide_on_close(true).pin_window(true)
				.wndclass(RegisterWndClass<LdrMeasureUI>()))
			, m_context_id(GenerateGUID())
			, m_read_point_cb(read_point_cb)
			, m_read_point_ctx(ctx)
			, m_rdr(rdr)
			, m_gfx()
			, m_btn_orig(gui::Button::Params<>().parent(this_).name("btn-orig").id(ID_BTN_ORIG).wh(50, 20).xy(0, 0).anchor(EAnchor::TopLeft).text(L"Origin"))
			, m_btn_set0(gui::Button::Params<>().parent(this_).name("btn-set0").id(ID_BTN_SET0).wh(50, 20).xy(Left | RightOf | ID_BTN_ORIG, 0).anchor(EAnchor::TopLeft).text(L"Point 0"))
			, m_btn_set1(gui::Button::Params<>().parent(this_).name("btn-set1").id(ID_BTN_SET1).wh(50, 20).xy(Left | RightOf | ID_BTN_SET0, 0).anchor(EAnchor::TopLeft).text(L"Point 1"))
			, m_tb_values(gui::TextBox::Params<>().parent(this_).name("tb-values").id(ID_TB_VALUES).wh(Fill, Fill).xy(0, Top | BottomOf | ID_BTN_ORIG).anchor(EAnchor::All).multiline(true))
			, m_origin(v4Origin)
			, m_point0(v4Origin)
			, m_point1(v4Origin)
		{
			CreateHandle();
			m_btn_orig.Click += std::bind(&LdrAngleUI::HandleSetPoint, this, _1, _2);
			m_btn_set0.Click += std::bind(&LdrAngleUI::HandleSetPoint, this, _1, _2);
			m_btn_set1.Click += std::bind(&LdrAngleUI::HandleSetPoint, this, _1, _2);
			UpdateMeasurementInfo();
		}

		// Set the callback function used to read points in the 3d environment
		void SetReadPoint(ReadPointCB cb, void* ctx)
		{
			m_read_point_cb = cb;
			m_read_point_ctx = ctx;
		}

		// Graphics associated with the this measure tool
		LdrObjectPtr Gfx() const
		{
			return m_gfx;
		}

		// The context id for graphics objects belonging to this measurement UI
		Guid GfxContextId() const
		{
			return m_context_id;
		}

		// Handle a 'Set Point' button being clicked
		void HandleSetPoint(gui::Button& btn, gui::EmptyArgs const&)
		{
			auto dummy = v4{};
			auto& point =
				&btn == &m_btn_orig ? m_origin :
				&btn == &m_btn_set0 ? m_point0 :
				&btn == &m_btn_set1 ? m_point1 :
				dummy;

			// Read the 3D point from the scene
			point = m_read_point_cb(m_read_point_ctx);

			// Update the measurement data
			UpdateMeasurementInfo();
		}

		// Update the text in the measurement details text box
		void UpdateMeasurementInfo()
		{
			using namespace maths;

			// Remove any existing graphics
			m_gfx = nullptr;

			// Create graphics
			if (m_origin != m_point0 || m_origin != m_point1)
			{
				std::string str;
				GroupStart(str, "Angle");
				Line(str, "edge0", 0xFFFFFFFF, m_origin, m_point0);
				Line(str, "edge1", 0xFFFFFF00, m_origin, m_point1);
				Line(str, "edge2", 0xFF00FF00, m_point0, m_point1);
				GroupEnd(str);

				ParseResult out;
				ParseString(m_rdr, str.c_str(), out, GfxContextId());
				if (!out.m_objects.empty())
					m_gfx = out.m_objects.back();
			}

			auto  e0 = m_point0 - m_origin;
			auto  e1 = m_point1 - m_origin;
			auto  e2 = m_point1 - m_point0;
			float edge0 = Length(e0);
			float edge1 = Length(e1);
			float edge2 = Length(e2);
			float ang = (edge0 < tinyf || edge1 < tinyf) ? 0.0f : RadiansToDegrees(ACos(Clamp(Dot3(e0, e1) / (edge0 * edge1), -1.0f, 1.0f)));

			// Update the text description
			m_tb_values.Text(FmtS(
				L"edge0: %f\r\n"
				L"edge1: %f\r\n"
				L"edge2: %f\r\n"
				L"angle: %f\uC2B0\r\n"
				, edge0, edge1, edge2
				, ang
			));

			// Notify the measurement data changed
			MeasurementChanged(*this, gui::EmptyArgs());
		}

		// Raised when the measurement data changes
		// MeasurementChanged += [&](LdrAngleUI&,EmptyArgs const&){}
		gui::EventHandler<LdrAngleUI&, gui::EmptyArgs const&> MeasurementChanged;
	};
}

//// Add a manifest dependency on common controls version 6
//#if defined _M_IX86
//#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
//#elif defined _M_IA64
//#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
//#elif defined _M_X64
//#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
//#else
//#	pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
//#endif