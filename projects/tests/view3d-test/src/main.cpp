#include <stdexcept>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"

#include "pr/view3d/dll/view3d.h"
#include "pr/view3d/dll/conversion.h"

using namespace pr;
using namespace pr::gui;
using namespace view3d;

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	//// Declare an instance type
	//#define PR_RDR_INST(x)\
	//	x(m4x4, m_i2w, EInstComp::I2WTransform)\
	//	x(ModelPtr, m_model, EInstComp::ModelPtr)\
	//	x(Colour32, m_tint, EInstComp::TintColour32)
	//PR_RDR12_DEFINE_INSTANCE(Instance, PR_RDR_INST)
	//#undef PR_RDR_INST

	View3DContext m_view3d;
	View3DWindow m_win3d;
	View3DObject m_obj0;
	View3DObject m_obj1;
	//Renderer m_rdr;
	//Window m_wnd;
	//Scene m_scn;
	//Instance m_inst0;
	//Instance m_inst1;

	// Error handler
	static void __stdcall ReportError(void*, wchar_t const* msg, wchar_t const* filepath, int line, int64_t)
	{
		std::wcout << filepath << "(" << line << "): " << msg << std::endl;
	}

	Main(HINSTANCE hinstance)
		: Form(Params<>()
			.name("main")
			.title(L"View3d 11 Test")
			.xy(1500,100).wh(800,600)
			.main_wnd(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<Main>()))
		, m_view3d(View3D_Initialise(ReportError, this, s_cast<D3D11_CREATE_DEVICE_FLAG>(D3D11_CREATE_DEVICE_DEBUG|D3D11_CREATE_DEVICE_BGRA_SUPPORT)))
		, m_win3d(View3D_WindowCreate(CreateHandle(), {.m_error_cb = ReportError, .m_error_cb_ctx = this, .m_dbg_name = "TestWnd"}))
		, m_obj0(View3D_ObjectCreateLdr(L"*Box first_box_eva 8000FF00 { 1 2 3 }", false, nullptr, nullptr))
		, m_obj1(View3D_ObjectCreateLdr(L"*Sphere sever FF0080FF { 0.4 }", FALSE, nullptr, nullptr))
		//,m_rdr(RSettings(hinstance))
		//,m_wnd(m_rdr, WSettings(CreateHandle(), m_rdr.Settings()))
		//,m_scn(m_wnd)
		//,m_inst0()
		//,m_inst1()
	{
		// Set up the scene
		//m_scn.m_bkgd_colour = Colour32(0xFF908080);
		View3D_BackgroundColourSet(m_win3d, 0xFF908080);
		//m_scn.m_cam.LookAt(v4{0, 0, +3, 1}, v4::Origin(), v4::YAxis());
		View3D_CameraPositionSet(m_win3d, {0, 0, +7, 1}, {0, 0, 0, 1}, {0, 1, 0, 0});

		View3DLight light;
		View3D_LightPropertiesGet(m_win3d, light);
		light.m_type = EView3DLight::Directional;
		light.m_direction = To<View3DV4>(v4::Normal(-1, -1, -1, 0));
		light.m_cast_shadow = 10;
		View3D_LightPropertiesSet(m_win3d, light);

		//m_inst0.m_model = m_rdr.res_mgr().FindModel(EStockModel::UnitQuad);
		//
		//auto tex = m_rdr.res_mgr().FindTexture(EStockTexture::Checker);
		//for (Nugget& nug : m_inst0.m_model->m_nuggets)
		//	nug.m_tex_diffuse = tex;

		//m_inst1.m_model = m_rdr.res_mgr().FindModel(EStockModel::BBoxModel);
		//m_inst1.m_i2w = m4x4::Identity();
		//m_inst1.m_tint = Colour32White;
		//m_scn.AddInstance(m_inst1);
		View3D_WindowAddObject(m_win3d, m_obj0);
		View3D_WindowAddObject(m_win3d, m_obj1);

		//View3D_DemoSceneCreate(m_win3d);

		//m_inst0.m_i2w = m4x4::Identity();
		//m_inst0.m_tint = Colour32Green;
		//m_scn.AddInstance(m_inst0);
		(void)hinstance;
	}
	~Main()
	{
		View3D_WindowDestroy(m_win3d);
		View3D_ObjectDelete(m_obj0);
		View3D_ObjectDelete(m_obj1);
		View3D_Shutdown(m_view3d);
	}
	void OnWindowPosChange(WindowPosEventArgs const& args) override
	{
		Form::OnWindowPosChange(args);
		if (!args.m_before && args.IsResize())
		{
			auto dpi = GetDpiForWindow(*this);
			auto w = s_cast<int>(args.m_wp->cx * dpi / 96.0);
			auto h = s_cast<int>(args.m_wp->cy * dpi / 96.0);
			View3D_BackBufferSizeSet(m_win3d, w, h);
			View3D_SetViewport(m_win3d, View3DViewport{
				.m_x = 0,
				.m_y = 0,
				.m_width = 1.f * w,
				.m_height = 1.f * h,
				.m_min_depth = 0,
				.m_max_depth = 1,
				.m_screen_w = args.m_wp->cx,
				.m_screen_h = args.m_wp->cy,
				});
			//m_wnd.BackBufferSize(sz, false);
			//m_scn.m_viewport.Set(sz);
		}
	}
	void OnMouseButton(MouseEventArgs& args) override
	{
		Form::OnMouseButton(args);
		if (!args.m_handled)
		{
			View3DV2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			auto nav_op =
				args.m_button == EMouseKey::Left ? EView3DNavOp::Rotate :
				args.m_button == EMouseKey::Right ? EView3DNavOp::Translate :
				EView3DNavOp::None;

			View3D_MouseNavigate(m_win3d, pt, nav_op, TRUE);
		}
	}
	void OnMouseMove(MouseEventArgs& args) override
	{
		Form::OnMouseMove(args);
		if (!args.m_handled)
		{
			View3DV2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			auto nav_op =
				args.m_button == EMouseKey::Left ? EView3DNavOp::Rotate :
				args.m_button == EMouseKey::Right ? EView3DNavOp::Translate :
				EView3DNavOp::None;

			View3D_MouseNavigate(m_win3d, pt, nav_op, FALSE);
		}
	}
	void OnMouseWheel(MouseWheelArgs& args) override
	{
		Form::OnMouseWheel(args);
		if (!args.m_handled)
		{
			View3DV2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			View3D_MouseNavigateZ(m_win3d, pt, args.m_delta, TRUE);
		}
	}
};

// Entry point
int __stdcall WinMain(HINSTANCE hinstance, HINSTANCE, LPTSTR, int)
{
	pr::InitCom com;

	try
	{
		Main main(hinstance);
		main.Show();

		float time = 0.0f;
		SimMessageLoop loop;
		loop.AddMessageFilter(main);
		loop.AddLoop(10, true, [&main, &time](auto dt)
		{
			time += dt * 0.001f;
			auto i2w0 = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			View3D_ObjectO2WSet(main.m_obj0, To<View3DM4x4>(i2w0), nullptr);

			auto i2w1 = m4x4::Translation(1.0f, 1.0f, 1.0f);
			View3D_ObjectO2WSet(main.m_obj1, To<View3DM4x4>(i2w1), nullptr);
			//main.m_inst0.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			//main.m_inst1.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
		
			View3DM4x4 c2w;
			View3D_CameraToWorldGet(main.m_win3d, c2w);
			SetWindowTextA(main, pr::FmtS("View3d 11 Test - Cam: %3.3f %3.3f %3.3f", c2w.w.x, c2w.w.y, c2w.w.z));

			//auto frame = main.m_wnd.RenderFrame();
			//frame.Render(main.m_scn);
			//frame.Present();
			View3D_Render(main.m_win3d);
			View3D_Present(main.m_win3d);
		});
		return loop.Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA("Died: ");
		OutputDebugStringA(ex.what());
		OutputDebugStringA("\n");
		return -1;
	}
}
