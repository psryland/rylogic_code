// Fluid
#include "src/forward.h"
#include "src/fluid_simulation.h"
#include "src/fluid_visualisation.h"
#include "src/kdtree_partition.h"
#include "src/bucket_collision.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::rdr12;
using namespace pr::fluid;

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };
	enum class ERunMode { Paused, SingleStep, FreeRun };

	Renderer m_rdr;
	Window m_wnd;
	Scene m_scn;

	SimMessageLoop m_loop;
	BucketCollision m_bucket_collision;
	KDTreePartition m_kdtree_partition;
	FluidSimulation m_fluid_sim;
	FluidVisualisation m_fluid_vis;

	ERunMode m_run_mode;
	float m_time;

	Main(HINSTANCE hinst)
		: Form(Params<>()
			.name("main")
			.title(L"Fluid")
			.xy(1200,100)
			.wh(1024, 768, true)
			.main_wnd()
			.dbl_buffer()
			.wndclass(RegisterWndClass<Main>()))
		, m_rdr(RdrSettings(hinst).DebugLayer())
		, m_wnd(m_rdr, WndSettings(CreateHandle(), true, m_rdr.Settings()).BackgroundColour(0xFFA0A080))
		, m_scn(m_wnd)
		, m_loop()
		, m_bucket_collision()
		, m_kdtree_partition()
		, m_fluid_sim(m_bucket_collision, m_kdtree_partition)
		, m_fluid_vis(m_fluid_sim, m_rdr, m_scn)
		, m_time()
		, m_run_mode(ERunMode::Paused)
	{
		m_scn.m_cam.Aspect(m_scn.m_viewport.Aspect());
		if constexpr (Dimensions == 2)
			m_scn.m_cam.LookAt(v4(0.0f, 0.5f, 2.8f, 1), v4(0, 0.7f, 0, 1), v4(0, 1, 0, 0));
		if constexpr (Dimensions == 3)
			m_scn.m_cam.LookAt(v4(0.2f, 0.2f, 0.2f, 1), v4(0, 0.5f, 0, 1), v4(0, 1, 0, 0));
		m_scn.m_cam.Align(v4::YAxis());

		m_loop.AddMessageFilter(*this);
		m_loop.AddLoop(10, false, [this](auto dt) // Sim loop
		{
			switch (m_run_mode)
			{
				case ERunMode::Paused:
				{
					break;
				}
				case ERunMode::SingleStep:
				{
					m_run_mode = ERunMode::Paused;
					m_fluid_sim.Step(dt * 0.001f);
					break;
				}
				case ERunMode::FreeRun:
				{
					m_fluid_sim.Step(dt * 0.001f);
					break;
				}
			}
		});
		m_loop.AddLoop(30, false, [this](auto dt) // Render Loop
		{
			m_time += dt * 0.001f;

			// Update the window title
			auto c2w = m_scn.m_cam.CameraToWorld();
			SetWindowTextA(*this, pr::FmtS("Fluid - Time: %3.3fs - Cam: %3.3f %3.3f %3.3f  Dir: %3.3f %3.3f %3.3f", m_time, c2w.w.x, c2w.w.y, c2w.w.z, -c2w.z.x, -c2w.z.y, -c2w.z.z));

			//auto density = m_fluid_sim.DensityAt(m_fluid_vis.m_probe.m_position);
			//SetWindowTextA(*this, pr::FmtS("Fluid - Density: %3.3f", density));

			// Render the particles
			m_scn.ClearDrawlists();
			m_fluid_vis.AddToScene(m_scn);

			// Render the frame
			auto frame = m_wnd.NewFrame();
			m_scn.Render(frame);
			m_wnd.Present(frame);
		});
	}
	int Run()
	{
		return m_loop.Run();
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
			m_scn.m_cam.Aspect(1.0*w/h);
		}
	}
	void OnMouseButton(MouseEventArgs& args) override
	{
		Form::OnMouseButton(args);
		m_fluid_vis.OnMouseButton(args);
		if (args.m_handled)
			return;
		
		auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
		auto nav_op =
			pr::AllSet(args.m_button, EMouseKey::Left) ? Camera::ENavOp::Rotate :
			pr::AllSet(args.m_button, EMouseKey::Right) ? Camera::ENavOp::Translate :
			Camera::ENavOp::None;

		m_scn.m_cam.MouseControl(nss_point, nav_op, true);
	}
	void OnMouseMove(MouseEventArgs& args) override
	{
		Form::OnMouseMove(args);
		m_fluid_vis.OnMouseMove(args);
		if (args.m_handled)
			return;

		auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
		auto nav_op =
			pr::AllSet(args.m_button, EMouseKey::Left) ? Camera::ENavOp::Rotate :
			pr::AllSet(args.m_button, EMouseKey::Right) ? Camera::ENavOp::Translate :
			Camera::ENavOp::None;

		m_scn.m_cam.MouseControl(nss_point, nav_op, false);
	}
	void OnMouseWheel(MouseWheelArgs& args) override
	{
		Form::OnMouseWheel(args);
		m_fluid_vis.OnMouseWheel(args);
		if (args.m_handled)
			return;

		auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
		m_scn.m_cam.MouseControlZ(nss_point, args.m_delta, true);
	}
	void OnKey(KeyEventArgs& args) override
	{
		Form::OnKey(args);
		m_fluid_vis.OnKey(args);
		if (args.m_handled)
			return;

		if (args.m_down) return;
		switch (args.m_vk_key)
		{
			case VK_ESCAPE:
			{
				Close();
				break;
			}
			case VK_SPACE:
			{
				m_run_mode = KeyDown(VK_CONTROL) ? ERunMode::FreeRun : ERunMode::SingleStep;
				break;
			}
		}
	}

	// Error handler
	static void __stdcall ReportError(void*, char const* msg, char const* filepath, int line, int64_t)
	{
		std::cout << filepath << "(" << line << "): " << msg << std::endl;
	}
};

// Entry point
int __stdcall wWinMain(HINSTANCE hinstance, HINSTANCE, LPWSTR, int)
{
	try
	{
		InitCom com;

		Main main(hinstance);
		main.Show();
		return main.Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA("Died: ");
		OutputDebugStringA(ex.what());
		OutputDebugStringA("\n");
		return -1;
	}
}
