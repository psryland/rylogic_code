#include "physics2/src/forward.h"
#include "physics2/src/body.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::physics;

struct MainUI :Form
{
	StatusBar   m_status;
	View3DPanel m_view3d;
	double m_clock;
	int m_steps;

	Body m_body0;
	Body m_body1;

	MainUI()
		:Form(MakeFormParams<>().name("main-ui").title(L"Rylogic Physics").start_pos(EStartPosition::Manual).xy(1000, 50).padding(0).wndclass(RegisterWndClass<MainUI>()))
		,m_status(StatusBar::Params<>().parent(this_).dock(EDock::Bottom))
		,m_view3d(View3DPanel::Params<>().parent(this_).error_cb(ReportErrorCB, this_).dock(EDock::Fill).border().show_focus_point())
		,m_clock()
		,m_steps()
		,m_body0()
		,m_body1()
	{
		View3D_WindowAddObject(m_view3d.m_win, m_body0.m_gfx);
		View3D_WindowAddObject(m_view3d.m_win, m_body1.m_gfx);

		Reset();

		m_view3d.Key += [&](Control&, KeyEventArgs const& args)
		{
			if (args.m_down && args.m_vk_key == 'R')
				Reset();
			if (args.m_down && args.m_vk_key == 'S')
				m_steps = 1;
			if (args.m_down && args.m_vk_key == 'F')
				m_steps = 0xFFFFFFFF;
		};
	}

	void Reset()
	{
		m_steps = 0;
		m_clock = 0;
		
		// Reset the bodies
		m_body0.ZeroForces();
		m_body1.ZeroForces();
		m_body0.O2W(m4x4::Transform(0,0,float(maths::tau_by_8), v4{-2,0,0,1}));
		m_body1.O2W(m4x4::Transform(0,0,float(maths::tau_by_8), v4{+2,0,0,1}));

		//m_body0.VelocityWS(v4{float(maths::tau_by_4),float(maths::tau_by_8),float(maths::tau),0}, v4{0,0,0,0});
		m_body0.VelocityWS(v4{0,0,0.1f*float(maths::tau),0}, v4{0,0,0,0});

		Render(0);
		View3D_ResetView(m_view3d.m_win, View3DV4{0,0,-1,0}, View3DV4{0,1,0,0}, 0, TRUE, TRUE);
	}

	// Step the main loop
	void Step(double elapsed_seconds)
	{
		m_clock += elapsed_seconds;
		SetWindowTextA(*this, pr::FmtS("Rylogic Physics - %3.3lf", m_clock));
		if (m_steps == 0 || m_steps-- == 0)
			return;

		auto accel = v4{};// v4Origin - m_body0.O2W().pos; accel.y = accel.z = 0;
		auto w_dot = v4{};//Cross(v4YAxis, accel);
		m_body0.ApplyForceWS(m_body0.Mass() * accel, m_body0.InertiaWS() * w_dot);

		//elapsed_seconds = 0.5f;
		Dump();
		Evolve(m_body0, float(elapsed_seconds));
	}
	//{
	//	// Apply gravity: GMm/r^2
	//	float const G = 1.0f;
	//	auto sep = m_body0.O2W().pos - m_body1.O2W().pos;
	//	auto r_sq = Length3Sq(sep);
	//	if (r_sq > 0.1_m)
	//	{
	//		auto force_mag = G * m_body0.Mass() * m_body1.Mass() / r_sq;
	//		auto force = force_mag * sep / Sqrt(r_sq);
	//		m_body0.ApplyForceWS(-force, v4Zero);
	//		m_body1.ApplyForceWS(+force, v4ZAxis);
	//	}

	//	// Evolve the bodies
	//	for (auto body : {&m_body0, &m_body1})
	//		Evolve(*body, float(elapsed_seconds));
	//}

	// Render a frame
	void Render(double)
	{
		m_body0.UpdateGfx();
		m_body1.UpdateGfx();
		Invalidate(false, nullptr, true);
	}

	// Export the scene as LDraw script
	void Dump()
	{
		using namespace ldr;

		//auto flags = ERigidBodyFlags::LVel|ERigidBodyFlags::AVel|ERigidBodyFlags::Force|ERigidBodyFlags::Torque;
		auto flags = ERigidBodyFlags::All;

		std::string str;
		ldr::RigidBody(str, "body0", 0x8000FF00, m_body0, 0.1f, flags);
		ldr::RigidBody(str, "body1", 0x10FF0000, m_body1, 0.1f, flags);
		ldr::Write(str, L"P:\\dump\\physics_dump.ldr");
	}

	// Handle errors reported within view3d
	static void __stdcall ReportErrorCB(void* ctx, wchar_t const* msg)
	{
		auto this_ = static_cast<MainUI*>(ctx);
		::MessageBoxW(this_->m_hwnd, msg, L"Error", MB_OK);
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

		SimMsgLoop loop;
		loop.AddStepContext("step", std::bind(&MainUI::Step, &main, std::placeholders::_1), 100.0f, true);
		loop.AddStepContext("rdr" , std::bind(&MainUI::Render, &main, std::placeholders::_1), 60.0f, true);
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
