#include "src/forward.h"
#include "src/body.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::physics;

#define TEST_PAIR 1
std::default_random_engine rng;

struct MainUI :Form
{
	using Physics = Engine<broadphase::Brute<Body>, MaterialMap>;

	StatusBar   m_status;
	View3DPanel m_view3d;
	double m_clock;
	int m_steps;

	#if TEST_PAIR
	Body m_body[2];
	#else
	Body m_body[2];
	#endif
	Physics m_physics;
	ShapeSphere m_sph;
	ShapeBox m_box;

	MainUI()
		: Form(Params<>().name("main-ui").title(L"Rylogic Physics").start_pos(EStartPosition::Manual).xy(1000, 50).padding(0).wndclass(RegisterWndClass<MainUI>()))
		, m_status(StatusBar::Params<>().parent(this_).dock(EDock::Bottom))
		, m_view3d(View3DPanel::Params().parent(this_).error_cb(ReportErrorCB, this_).dock(EDock::Fill).border().show_focus_point())
		, m_clock()
		, m_steps()
		, m_body()
		, m_physics()
		, m_sph(0.5f)
		#if TEST_PAIR
		, m_box(v4{maths::inv_root2f, maths::inv_root2f, maths::inv_root2f, 0}, m4x4::Transform(0, 0, maths::tau_by_8f, v4Origin))
		#else
		, m_box(Abs(Random3(rng, v4{0.8f}, v4{1.4f}, 0)))
		#endif
	{
		Reset();
		m_view3d.Key += [&](Control&, KeyEventArgs const& args)
		{
			if (args.m_down && args.m_vk_key == 'R')
				Reset();
			if (args.m_down && args.m_vk_key == 'S')
				m_steps = 1;
			if (args.m_down && args.m_vk_key == 'G')
				m_steps = 0xFFFFFFFF;
		};
	}

	void Reset()
	{
		m_steps = 0;
		m_clock = 0;
		
		// Reset the bodies
		for (auto& body : m_body)
		{
			body.Shape(m_box, 10.0f);
			body.ZeroForces();
			body.ZeroMomentum();
			
			auto o2w = m4x4::Random(rng, v4::Origin(), 5.0f);
			//o2w.pos.xyz += o2w.pos.xyz;
			body.O2W(o2w);
		}

		#if !TEST_PAIR
		m_physics.m_materials(0).m_elasticity_norm = 0;
		#else
		{// Test case setup
			auto& objA = m_body[0];
			auto& objB = m_body[1];
			objA.Shape(m_box, physics::Inertia::Box(v4{0.5f,0.5f,0.5f,0}, 10.0f));
			objB.Shape(m_box, physics::Inertia::Box(v4{0.5f,0.5f,0.5f,0}, 10.0f));
			objA.O2W(m4x4::Transform(0, 0, 0, v4{-0.5f, -0.0f, 1, 1}));
			objB.O2W(m4x4::Transform(0, 0, 0, v4{+0.5f, +0.1f, 1, 1}));

			objA.Mass(10);
			objB.Mass(5);
			objA.VelocityWS(v4{0,0,0,0}, v4{+0, 0, 0, 0});
			objB.VelocityWS(v4{0,0,0,0}, v4{-10,-10, 0, 0});

			// Self detaching handler
			static Sub sub;
			sub = m_physics.PostCollisionDetection += [&](Physics const&, std::vector<physics::Contact>& contacts)
			{
				(void)contacts;
				m_physics.PostCollisionDetection -= sub;
				return;

			//	contacts.resize(0);
			//
			//	physics::Contact c(objA, objB);
			//	c.m_axis = Normalise(v4{Cos(maths::tauf/16), Sin(maths::tauf/16),0,0});
			//	c.m_point = v4{0.5f, 0, 0, 0};
			//	c.m_mat.m_friction_static = 1.0f;
			//	c.m_mat.m_elasticity_norm = 0.0f;
			//	c.m_mat.m_elasticity_tors = -1.0f;
			//	c.update(0);
			//	contacts.push_back(c);

			};
		}
		#endif

		// Reset the broad phase
		m_physics.m_broadphase.Clear();
		for (auto& body : m_body)
			m_physics.m_broadphase.Add(body);

		for (auto& body : m_body)
			View3D_WindowAddObject(m_view3d.m_win, body.m_gfx);

		Render();

		View3D_ResetView(m_view3d.m_win, view3d::Vec4{+0.0f,0,-1,0}, view3d::Vec4{0,1,0,0}, 0, TRUE, TRUE);
	}

	// Step the main loop
	void Step(double elapsed_seconds)
	{
		m_clock += elapsed_seconds;
		SetWindowTextA(*this, pr::FmtS("Rylogic Physics - %3.3lf", m_clock));
		if (m_steps == 0 || m_steps-- == 0)
			return;

		auto dt = float(elapsed_seconds);
		//auto dt = 1.0f;

		#if TEST_PAIR

		m_physics.Step(dt, m_body);

		#else

		for (int i = 0, iend = _countof(m_body); i != iend; ++i)
		{
			for (int j = i + 1; j != iend; ++j)
			{
				auto& body0 = m_body[i];
				auto& body1 = m_body[j];

				// Apply gravity: GMm/r^2
				float const G = 1.0f;
				auto sep = body0.O2W(dt/2).pos - body1.O2W(dt/2).pos;
				auto r_sq = Length3Sq(sep);
				if (r_sq > Sqr(0.1_m))
				{
					auto force_mag = G * body0.Mass() * body1.Mass() / r_sq;
					auto force = force_mag * sep / Sqrt(r_sq);
					body0.ApplyForceWS(-force, v4Zero);
					body1.ApplyForceWS(+force, v4Zero);
				}
			}
		}

		// Pull things back to the origin
		for (int i = 0, iend = _countof(m_body); i != iend; ++i)
		{
			auto& body = m_body[i];

			auto r = body.O2W().pos.w0();
			auto rlen = Length(r);
			if (rlen > 10.0f)
			{
				body.ApplyForceWS(-r * Sqrt(rlen - 10.0f) / rlen, v4{});
			}
		}

		//Dump();

		m_physics.Step(dt, m_body);

		#endif
	}

	// Render a frame
	void Render()
	{
		for (auto& body : m_body)
			body.UpdateGfx();

		Invalidate(false, nullptr, true);
	}

	// Export the scene as LDraw script
	void Dump()
	{
		using namespace pr::rdr12::ldraw;

		//auto flags = ERigidBodyFlags::LVel|ERigidBodyFlags::AVel|ERigidBodyFlags::Force|ERigidBodyFlags::Torque;
		auto flags = ERigidBodyFlags::All;

		Builder builder;
		builder._<LdrRigidBody>("body0", 0x8000FF00).rigid_body(m_body[0]).flags(flags);
		builder._<LdrRigidBody>("body1", 0x10FF0000).rigid_body(m_body[1]).flags(flags);
		builder.Save(L"\\dump\\physics_dump.ldr");
	}

	// Handle errors reported within view3d
	static void __stdcall ReportErrorCB(void* ctx, char const* msg, char const* filepath, int line, int64_t)
	{
		auto this_ = static_cast<MainUI*>(ctx);
		auto message = pr::FmtS(L"%s(%d): %s", filepath, line, msg);
		::MessageBoxW(this_->m_hwnd, message, L"Error", MB_OK);
	}
};

// Entry point
int __stdcall WinMain(HINSTANCE, HINSTANCE, LPTSTR, int)
{
	pr::InitCom com;
	pr::GdiPlus gdi;

	try
	{
		pr::win32::LoadDll<struct Scintilla>(L"scintilla.dll");
		pr::win32::LoadDll<struct View3d>(L"view3d.dll");
		InitCtrls();

		MainUI main;
		main.Show();

		MessageLoop loop;
		loop.AddLoop(100.0, false, [&](int64_t ms) { main.Step(ms * 0.001); });
		loop.AddLoop(60.0, true, [&](int64_t) { main.Render(); });
		loop.AddMessageFilter(main);
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
