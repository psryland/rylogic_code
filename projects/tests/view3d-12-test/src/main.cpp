#include <stdexcept>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"

//#include "pr/view3d-12/view3d.h"
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/utility/conversion.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::rdr12;

// TODO:
//  Finish the View3d API
//  Ray cast/ Hit test support

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	view3d::DllHandle m_view3d;
	view3d::Window m_win3d;
	view3d::Object m_obj0;
	view3d::Object m_obj1;
	view3d::CubeMap m_envmap;
	
	// Error handler
	static void __stdcall ReportError(void*, char const* msg, char const* filepath, int line, int64_t)
	{
		std::cout << filepath << "(" << line << "): " << msg << std::endl;
	}

	Main(HINSTANCE)
		: Form(Params<>()
			.name("main")
			.title(L"View3d 12 Test")
			.xy(1400,100).wh(1024, 768, true)
			.main_wnd(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<Main>()))
		, m_view3d(View3D_Initialise(ReportError, this))
		, m_win3d(View3D_WindowCreate(CreateHandle(), {.m_error_cb = ReportError, .m_error_cb_ctx = this, .m_multisampling = 8, .m_dbg_name = "TestWnd"}))
		, m_obj0(View3D_ObjectCreateLdrA(
			"*Box first_box_eva FF00FF00 { 1 2 3 }"
			, false, nullptr, nullptr))
		, m_obj1(View3D_ObjectCreateLdrA(
			"*Sphere sever FF0080FF { 0.4 }"
			, false, nullptr, nullptr))
		, m_envmap(View3D_CubeMapCreateFromUri("E:/Rylogic/art/textures/cubemaps/hanger/hanger-??.jpg", {}))
	{
		// Load script
		auto ctx0 = View3D_LoadScriptFromString(
			"*Spline spline\n"
			"{\n"
			"	0 0 0  -1 1 0  -1 2 0  0 1.5 0 FF00FF00\n"
			"	0 0 0  +1 1 0  +1 2 0  0 1.5 0 FFFF0000\n"
			"	*Width { 4 }\n"
			"	*o2w {*pos{0 10 0}}\n"
			"}\n"
			, nullptr, nullptr, nullptr, nullptr);

		// Set up the scene
		View3D_WindowBackgroundColourSet(m_win3d, 0xFF908080);
		View3D_CameraPositionSet(m_win3d, {0, +35, +40, 1}, {0, 0, 0, 1}, {0, 1, 0, 0});
	
		// Cast shadows
		auto light = View3D_LightPropertiesGet(m_win3d);
		light.m_type = view3d::ELight::Directional;
		light.m_direction = To<view3d::Vec4>(v4::Normal(-1, -1, -1, 0));
		light.m_cast_shadow = 10.0f;
		light.m_cam_relative = false;
		View3D_LightPropertiesSet(m_win3d, light);

		// Add objects to the scene
		//View3D_WindowAddObject(m_win3d, m_obj0);
		//View3D_WindowAddObject(m_win3d, m_obj1);
		View3D_WindowAddObjectsById(m_win3d, &ctx0, 1, 0);
		View3D_DemoSceneCreate(m_win3d);

		// EnvMap
		View3D_WindowEnvMapSet(m_win3d, m_envmap);
		View3D_WindowEnumObjects(m_win3d, [](void*, view3d::Object obj)
			{
				View3D_ObjectReflectivitySet(obj, 0.2f, "");
				return true;
			}, nullptr);
	}
	~Main()
	{
		View3D_CubeMapRelease(m_envmap);
		View3D_WindowDestroy(m_win3d);
		View3D_ObjectDelete(m_obj0);
		View3D_ObjectDelete(m_obj1);
		View3D_Shutdown(m_view3d);
	}
	void OnWindowPosChange(WindowPosEventArgs const& args) override
	{
		Form::OnWindowPosChange(args);
		if (!args.m_before && args.IsResize() && !IsIconic(*this))
		{
			auto dpi = GetDpiForWindow(*this);
			auto rect = ClientRect();
			auto w = s_cast<int>(rect.width() * dpi / 96.0);
			auto h = s_cast<int>(rect.height() * dpi / 96.0);
			View3D_WindowBackBufferSizeSet(m_win3d, w, h);
			View3D_WindowViewportSet(m_win3d, view3d::Viewport{
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
			view3d::Vec2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			auto nav_op =
				args.m_button == EMouseKey::Left ? view3d::ENavOp::Rotate :
				args.m_button == EMouseKey::Right ? view3d::ENavOp::Translate :
				view3d::ENavOp::None;

			View3D_MouseNavigate(m_win3d, pt, nav_op, TRUE);
		}
	}
	void OnMouseMove(MouseEventArgs& args) override
	{
		Form::OnMouseMove(args);
		if (!args.m_handled)
		{
			view3d::Vec2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			auto nav_op =
				args.m_button == EMouseKey::Left ? view3d::ENavOp::Rotate :
				args.m_button == EMouseKey::Right ? view3d::ENavOp::Translate :
				view3d::ENavOp::None;

			View3D_MouseNavigate(m_win3d, pt, nav_op, FALSE);
		}
	}
	void OnMouseWheel(MouseWheelArgs& args) override
	{
		Form::OnMouseWheel(args);
		if (!args.m_handled)
		{
			view3d::Vec2 pt = {s_cast<float>(args.m_point.x), s_cast<float>(args.m_point.y)};
			View3D_MouseNavigateZ(m_win3d, pt, args.m_delta, TRUE);
		}
	}
	void OnKey(KeyEventArgs& args) override
	{
		Form::OnKey(args);
		if (!args.m_down && args.m_vk_key == VK_F7)
		{
			View3D_ReloadScriptSources();
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
			auto i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			//View3D_ObjectO2WSet(main.m_obj0, To<view3d::Mat4x4>(i2w), nullptr);
			//main.m_inst0.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			//main.m_inst1.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
		
			auto c2w = View3D_CameraToWorldGet(main.m_win3d);
			SetWindowTextA(main, pr::FmtS("View3d 12 Test - Cam: %3.3f %3.3f %3.3f  Dir: %3.3f %3.3f %3.3f", c2w.w.x, c2w.w.y, c2w.w.z, -c2w.z.x, -c2w.z.y, -c2w.z.z));

			//auto frame = main.m_wnd.RenderFrame();
			//frame.Render(main.m_scn);
			//frame.Present();
			View3D_WindowRender(main.m_win3d);
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
