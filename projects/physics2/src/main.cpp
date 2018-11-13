#include "physics2/src/forward.h"
#include "physics2/src/body.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::physics;

struct MainUI :Form
{
	StatusBar   m_status;
	View3DPanel m_view3d;
	int m_steps;

	Body m_body0;
	Body m_body1;

	MainUI()
		:Form(MakeFormParams<>().name("main-ui").title(L"Rylogic Physics").padding(0).wndclass(RegisterWndClass<MainUI>()))
		,m_status(StatusBar::Params<>().parent(this_).dock(EDock::Bottom))
		,m_view3d(View3DPanel::Params<>().parent(this_).error_cb(ReportErrorCB, this_).dock(EDock::Fill).border().show_focus_point())
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
	}

	// Step the main loop
	void Step(double elapsed_seconds)
	{
		(void)elapsed_seconds;

		// Apply gravity: GMm/r^2
		float const G = 1.0f;
		auto sep = m_body0.O2W().pos - m_body1.O2W().pos;
		auto r_sq = Length3Sq(sep);
		if (r_sq > 0.1_m)
		{
			auto force_mag = G * m_body0.Mass() * m_body1.Mass() / r_sq;
			auto force = force_mag * sep / Sqrt(r_sq);
			m_body0.ApplyForceWS(+force, v4Zero, m_body0.O2W().pos);
			m_body1.ApplyForceWS(-force, v4ZAxis, m_body1.O2W().pos);
		}

		// Evolve the bodies
		for (auto body : {&m_body0, &m_body1})
			Evolve(*body, elapsed_seconds);
	}

	// Render a frame
	void Render(double)
	{
		m_body0.UpdateGfx();
		m_body1.UpdateGfx();
		Invalidate(false, nullptr, true);
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
