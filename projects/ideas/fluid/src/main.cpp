// Fluid
#include "src/forward.h"
#include "src/fluid_simulation.h"
#include "src/fluid_visualisation.h"
#include "src/particle.h"
#include "src/probe.h"

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

	enum class ERunMode
	{
		Paused,
		SingleStep,
		FreeRun,
	};
	enum class EFillStyle
	{
		Point,
		Random,
		Lattice,
		Grid,
	};

	inline static constexpr iv2 WinSize = { 2048, 1600 };
	inline static constexpr int ParticleCount = 946;//100;//30 * 30;
	inline static constexpr float ParticleRadius = 0.1f;
	inline static constexpr int GridCellCount = 1021;//65521;//1021;//1048573;//16777213;
	inline static constexpr wchar_t const* PositionLayout = L"struct PosType { float4 pos; float4 col; float4 vel; float4 pad; }";

	Renderer m_rdr;
	Window m_wnd;
	Scene m_scn;

	Probe m_probe;
	SimMessageLoop m_loop;
	CollisionBuilder m_col_builder;
	FluidSimulation m_fluid_sim;
	FluidVisualisation m_fluid_vis;

	std::string m_title;
	ERunMode m_run_mode;
	bool m_frame_lock;
	int m_last_frame;
	float m_time;

	Main(HINSTANCE hinst)
		: Form(Params<>()
			.name("main")
			.title(L"Fluid")
			.xy(1200, 100)
			.wh(WinSize.x, WinSize.y, true)
			.main_wnd()
			.dbl_buffer()
			.wndclass(RegisterWndClass<Main>()))
		, m_rdr(RdrSettings(hinst).DebugLayer())
		, m_wnd(m_rdr, WndSettings(CreateHandle(), true, m_rdr.Settings()).BackgroundColour(0xFFA0A080))
		, m_scn(m_wnd)
		, m_probe(m_rdr)
		, m_loop()
		, m_col_builder(CollisionInitData())
		, m_fluid_sim(m_rdr, FluidConstants(), ParticleInitData(EFillStyle::Lattice), m_col_builder.Build())
		, m_fluid_vis(m_fluid_sim, m_rdr, m_scn, m_col_builder.Ldr().WrapAsGroup().ToString())
		, m_title()
		, m_run_mode(ERunMode::Paused)
		, m_frame_lock(false)
		, m_last_frame(-1)
		, m_time()
	{
		Tweakables::filepath = "E:/Rylogic/projects/ideas/fluid/tweakables.ini";

		m_scn.m_cam.Aspect(m_scn.m_viewport.Aspect());
		if constexpr (Dimensions == 2)
			m_scn.m_cam.LookAt(v4(0.0f, 0.0f, 2.8f, 1), v4(0, 0.0f, 0, 1), v4(0, 1, 0, 0));
		if constexpr (Dimensions == 3)
			m_scn.m_cam.LookAt(v4(0.2f, 0.5f, 0.2f, 1), v4(0, 0.0f, 0, 1), v4(0, 1, 0, 0));
		m_scn.m_cam.Align(v4::YAxis());

		m_loop.AddMessageFilter(*this);
		m_loop.AddLoop(10, false, [this](auto dt) // Sim loop
		{
			Tweakable<float, "ProbeForce"> ProbeForce = 1.0f;

			m_fluid_sim.Colours.VelocityBased = true;
			m_fluid_sim.Colours.WithinProbe = m_probe.m_active;
			m_fluid_sim.Probe.Position = m_probe.m_position;
			m_fluid_sim.Probe.Radius = m_probe.m_radius;
			m_fluid_sim.Probe.Force = m_probe.m_active ? m_probe.m_sign * ProbeForce : 0;

			switch (m_run_mode)
			{
				case ERunMode::Paused:
				{
					break;
				}
				case ERunMode::SingleStep:
				{
					m_time += dt * 0.001f;
					m_fluid_sim.Step(dt * 0.001f);
					m_run_mode = ERunMode::Paused;
					break;
				}
				case ERunMode::FreeRun:
				{
					m_time += dt * 0.001f;
					m_fluid_sim.Step(dt * 0.001f);
					break;
				}
			}
		});
		m_loop.AddLoop(50, false, [this](auto) // Render Loop
		{
			// Update the window title
			UpdateWindowTitle();

			// Use this only render per main loop step
			if (m_frame_lock && m_last_frame == m_fluid_sim.m_frame)
				return;

			// Render the particles
			m_scn.ClearDrawlists();
			m_probe.AddToScene(m_scn);
			m_fluid_vis.AddToScene(m_scn);

			// Render the frame
			auto frame = m_wnd.NewFrame();
			m_scn.Render(frame);
			m_wnd.Present(frame);

			m_last_frame = m_fluid_sim.m_frame;
		});
	}
	int Run()
	{
		return m_loop.Run();
	}
	void UpdateWindowTitle()
	{
		m_title = "Fluid";

		if (m_frame_lock)
			m_title.append(std::format("[FL={}]", m_last_frame));

		if (m_probe.m_active)
		{
			auto pos = m_probe.m_position;
			auto rad_sq = Sqr(m_probe.m_radius);
			//auto density = 0;
			//auto press = v3{ 0,0,0 };

			auto count = 0;
			auto nearest = 0;
			auto nearest_dist_sq = std::numeric_limits<float>::max();
			auto particles = m_fluid_vis.ReadParticles();
			for (auto& particle : particles)
			{
				auto dist_sq = LengthSq(particle.pos - pos);
				if (dist_sq > rad_sq)
					continue;

				++count;
				if (dist_sq < nearest_dist_sq)
				{
					nearest = s_cast<int>(&particle - particles.data());
					nearest_dist_sq = dist_sq;
				}
			}

			m_title.append(std::format(" - Pos: {:.3f} {:.3f} {:.3f}", pos.x, pos.y, pos.z));
			m_title.append(std::format(" - Nearest: {}", nearest));
			m_title.append(std::format(" - Count: {}", count));
			//m_title.append(std::format(" - Density: {:.3f}", density));
			//m_title.append(std::format(" - Press: {:.3f} {:.3f} {:.3f}", press.x, press.y, press.z));
			m_title.append(std::format(" - Probe Radius: {:.3f}", m_probe.m_radius));
		}
		else
		{
			auto c2w = m_scn.m_cam.CameraToWorld();
			m_title.append(std::format(" - Time: {:.3f}s", m_time));
			m_title.append(std::format(" - Frame: {}", m_fluid_sim.m_frame));
			m_title.append(std::format(" - Cam: {:.3f} {:.3f} {:.3f}  Dir: {:.3f} {:.3f} {:.3f}", c2w.w.x, c2w.w.y, c2w.w.z, -c2w.z.x, -c2w.z.y, -c2w.z.z));
		}

		SetWindowTextA(*this, m_title.c_str());
	}
	void OnWindowPosChange(WindowPosEventArgs const& args) override
	{
		Form::OnWindowPosChange(args);
		if (!args.m_before && args.IsResize() && !IsIconic(*this))
		{
			auto rect = ClientRect(false);
			auto dpi = GetDpiForWindow(*this);
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
		m_probe.OnMouseButton(args, m_scn);
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
		m_probe.OnMouseMove(args, m_scn);
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
		m_probe.OnMouseWheel(args, m_scn);
		if (args.m_handled)
			return;

		auto nss_point = m_scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
		m_scn.m_cam.MouseControlZ(nss_point, args.m_delta, true);
	}
	void OnKey(KeyEventArgs& args) override
	{
		Form::OnKey(args);
		m_fluid_vis.OnKey(args);
		m_probe.OnKey(args, m_scn);
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
			case 'F':
			{
				m_frame_lock = !m_frame_lock;
				break;
			}
			case VK_F5:
			{
				m_run_mode = m_run_mode != ERunMode::FreeRun ? ERunMode::FreeRun : ERunMode::Paused;
				break;
			}
			case VK_F6:
			{
				m_run_mode = ERunMode::SingleStep;
				break;
			}
			case VK_SPACE:
			{
				m_run_mode = KeyDown(VK_CONTROL) ? ERunMode::FreeRun : ERunMode::SingleStep;
				break;
			}
		}
	}

	static FluidSimulation::ParamsData FluidConstants()
	{
		return FluidSimulation::ParamsData
		{
			.NumParticles = ParticleCount,
			.ParticleRadius = ParticleRadius,
			.CellCount = GridCellCount,
			.GridScale = 1.0f / ParticleRadius,
			.Gravity = v4(0, -9.8f, 0, 0),
			.Mass = 1.0f,
			.DensityToPressure = 100.0f,
			.Density0 = 1.0f,
			.Viscosity = 10.0f,
			.ThermalDiffusion = 0.01f,
			.TimeStep = 0,
			.RandomSeed = 0,
		};
	}
	static std::vector<Particle> ParticleInitData(EFillStyle style)
	{
		std::vector<Particle> particles;
		particles.reserve(ParticleCount);
		auto points = [&](v4 p, v4 v)
		{
			assert(p.w == 1 && v.w == 0);
			particles.push_back(Particle{ .pos = p, .col = v4::One(), .vel = v, .acc = {}, .density = {} });
		};

		const float hwidth = 1.0f;
		const float hheight = 0.5f;

		switch (style)
		{
			case EFillStyle::Point:
			{
				for (int i = 0; i != ParticleCount; ++i)
					points(v4(0,-1,0,1), v4(1,-1,0,0));

				break;
			}
			case EFillStyle::Random:
			{
				auto const margin = 0.95f;
				auto hw = hwidth * margin;
				auto hh = hheight * margin;
				auto vx = 0.2f;

				// Uniform distribution over the volume
				std::default_random_engine rng;
				for (int i = 0; i != ParticleCount; ++i)
				{
					auto pos = v3::Random(rng, v3(-hw, -hh, -hw), v3(+hw, +hh, +hw)).w1();
					auto vel = v3::Random(rng, v3(-vx, -vx, -vx), v3(+vx, +vx, +vx)).w0();
					if constexpr (Dimensions == 2) { pos.z = 0; vel.z = 0; }
					points(pos, vel);
				}
				break;
			}
			case EFillStyle::Lattice:
			{
				auto const margin = 0.95f;
				auto hw = hwidth * margin;
				auto hh = hheight * margin;

				if constexpr (Dimensions == 2)
				{
					// Want to spread N particles evenly over the volume.
					// Area is 2*hwidth * 2*hheight
					// Want to find 'step' such that:
					//   (2*hwidth / step) * (2*hheight / step) = N
					// => step = sqrt((2*hwidth * 2*hheight) / N)
					auto step = Sqrt((2 * hw * 2 * hh) / ParticleCount);

					auto x = -hw + step/2;
					auto y = -hh + step/2;
					for (int i = 0; i != ParticleCount; ++i)
					{
						points(v4(x, y, 0, 1), v4::Zero());

						x += step;
						if (x > hw) { x = -hw + step/2; y += step; }
					}
				}
				if constexpr (Dimensions == 3)
				{
					// Want to spread N particles evenly over the volume.
					// Volume is 2*hwidth * 2*hwidth * 2*hheight
					// Want to find 'step' such that:
					//  (2*hwidth/step) * (2*hwidth/step) * (2*hheight/step) = N
					// => step = cubert((2*hwidth * 2*hwidth * 2*hheight) / N)
					auto step = Cubert((2 * hw * 2 * hh * 2 * hw) / ParticleCount);

					auto x = -hw + step/2;
					auto y = -hh + step/2;
					auto z = -hw + step/2;
					for (int i = 0; i != ParticleCount; ++i)
					{
						points(v4(x, y, z, 1), v4::Zero());

						x += step;
						if (x > hw) { x = -hw + step/2; z += step; }
						if (z > hw) { z = -hw + step/2; y += step; }
					}
				}
				break;
			}
			case EFillStyle::Grid:
			{
				auto const margin = 1.0f;//0.95f;
				auto hw = hwidth * margin;
				auto hh = hheight * margin;
				auto step = 0.1f;

				if constexpr (Dimensions == 2)
				{
					auto x = -hw + step / 2.0f;
					auto y = -hh + step / 2.0f;
					for (int i = 0; i != ParticleCount; ++i)
					{
						points(v4(x, y, 0, 1), v4::Zero());

						x += step;
						if (x > hw) { x = -hw + step/2; y += step; }
					}
				}
				break;
			}
		}

		return particles;
	}
	static CollisionBuilder CollisionInitData()
	{
		using namespace ldr;
		return std::move(CollisionBuilder(true)
			.Plane(v4(0, +1, 0, 0.5f), ldr::Name("floor"), 0xFFade3ff, {2, 0.5f})
			.Plane(v4(0, -1, 0, 1.5f), ldr::Name("ceiling"), 0xFFade3ff, {2, 0.5f})
			.Plane(v4(+1, 0, 0, 1), ldr::Name("wall"), 0xFFade3ff, {0.5f, 1})
			.Plane(v4(-1, 0, 0, 1), ldr::Name("wall"), 0xFFade3ff, {0.5f, 1})
		);
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
