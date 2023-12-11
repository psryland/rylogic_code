#include <stdexcept>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"
#include "pr/view3d-12/view3d.h"

#include "src/fluid_simulation.h"
#include "src/fluid_visualisation.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::rdr12;

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	Renderer m_rdr;
	Window m_wnd;
	Scene m_scn;

	Main(HINSTANCE hinst)
		: Form(Params<>()
			.name("main")
			.title(L"Fluid")
			.xy(1400,100)
			.wh(1024, 768, true)
			.main_wnd()
			.dbl_buffer()
			.wndclass(RegisterWndClass<Main>()))
		, m_rdr(RdrSettings(hinst).DebugLayer())
		, m_wnd(m_rdr, WndSettings(CreateHandle(), true, m_rdr.Settings()).BackgroundColour(0xA0A080))
		, m_scn(m_wnd)
	{
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
			m_wnd.BackBufferSize({ w, h }, false);
			m_scn.m_viewport.Set({ w, h });
		}
	}
	void OnMouseButton(MouseEventArgs& args) override
	{
		Form::OnMouseButton(args);
		if (!args.m_handled)
		{
			auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
			auto nav_op =
				args.m_button == EMouseKey::Left ? Camera::ENavOp::Rotate :
				args.m_button == EMouseKey::Right ? Camera::ENavOp::Translate :
				Camera::ENavOp::None;

			m_scn.m_cam.MouseControl(nss_point, nav_op, true);
		}
	}
	void OnMouseMove(MouseEventArgs& args) override
	{
		Form::OnMouseMove(args);
		if (!args.m_handled)
		{
			auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
			auto nav_op =
				args.m_button == EMouseKey::Left ? Camera::ENavOp::Rotate :
				args.m_button == EMouseKey::Right ? Camera::ENavOp::Translate :
				Camera::ENavOp::None;

			m_scn.m_cam.MouseControl(nss_point, nav_op, false);
		}
	}
	void OnMouseWheel(MouseWheelArgs& args) override
	{
		Form::OnMouseWheel(args);
		if (!args.m_handled)
		{
			auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
			m_scn.m_cam.MouseControlZ(nss_point, args.m_delta, true);
		}
	}
	void OnKey(KeyEventArgs& args) override
	{
		Form::OnKey(args);
		if (!args.m_down && args.m_vk_key == VK_F7)
		{
		}
	}

	// Error handler
	static void __stdcall ReportError(void*, char const* msg, char const* filepath, int line, int64_t)
	{
		std::cout << filepath << "(" << line << "): " << msg << std::endl;
	}
};

// Entry point
int __stdcall WinMain(HINSTANCE hinstance, HINSTANCE, LPTSTR, int)
{

	try
	{
		InitCom com;
		Main main(hinstance);
		main.Show();

		main.m_scn.m_cam.LookAt(v4(2, 2, -5, 1), v4(0, 1, 0, 1), v4(0, 1, 0, 0));
		main.m_scn.m_cam.Align(v4::YAxis());

		fluid::FluidSimulation fluid_sim;
		fluid::FluidVisualisation fluid_vis(fluid_sim, main.m_rdr);

		float time = 0.0f;
		SimMessageLoop loop;
		loop.AddMessageFilter(main);
		loop.AddLoop(30, false, [&main, &time, &fluid_sim](auto dt)
		{
			fluid_sim.Step(dt * 0.001f);
		});
		loop.AddLoop(16, true, [&main, &time, &fluid_vis](auto dt)
		{
			time += dt * 0.001f;

			// Update the window title
			auto c2w = main.m_scn.m_cam.CameraToWorld();
			SetWindowTextA(main, pr::FmtS("Fluid - Time: %3.3fs - Cam: %3.3f %3.3f %3.3f  Dir: %3.3f %3.3f %3.3f", time, c2w.w.x, c2w.w.y, c2w.w.z, -c2w.z.x, -c2w.z.y, -c2w.z.z));

			// Render the particles
			main.m_scn.ClearDrawlists();
			fluid_vis.AddToScene(main.m_scn);

			// Render the frame
			auto frame = main.m_wnd.NewFrame();
			main.m_scn.Render(frame);
			main.m_wnd.Present(frame);
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
