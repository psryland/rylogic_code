#include <stdexcept>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"
#include "pr/view3d-12/view3d.h"

using namespace pr;
using namespace pr::gui;
using namespace pr::rdr12;

// Application window
struct Main :Form
{
	enum { IDR_MAINFRAME = 100 };
	enum { ID_FILE, ID_FILE_EXIT };
	enum { IDC_PROGRESS = 100, IDC_NM_PROGRESS, IDC_MODELESS, IDC_CONTEXTMENU, IDC_POSTEST, IDC_ABOUT, IDC_MSGBOX, IDC_SCINT, IDC_TAB, IDC_TAB1, IDC_TAB2, IDC_SPLITL, IDC_SPLITR };

	// Declare an instance type
	#define PR_RDR_INST(x)\
		x(m4x4, m_i2w, EInstComp::I2WTransform)\
		x(ModelPtr, m_model, EInstComp::ModelPtr)\
		x(Colour32, m_tint, EInstComp::TintColour32)
	PR_RDR12_DEFINE_INSTANCE(Instance, PR_RDR_INST)
	#undef PR_RDR_INST

	Renderer m_rdr;
	Window m_wnd;
	Scene m_scn;
	Instance m_inst0;
	Instance m_inst1;

	Main(HINSTANCE hinstance)
		:Form(Params<>()
			.name("main")
			.title(L"View3d 12 Test")
			.xy(1500,100).wh(800,600)
			.main_wnd(true)
			.dbl_buffer(true)
			.wndclass(RegisterWndClass<Main>()))
		,m_rdr(RSettings(hinstance))
		,m_wnd(m_rdr, WSettings(CreateHandle(), m_rdr.Settings()))
		,m_scn(m_wnd)
		,m_inst0()
		,m_inst1()
	{
		m_scn.m_bkgd_colour = Colour32(0xFF908080);
		m_scn.m_cam.LookAt(v4{0, 0, +3, 1}, v4::Origin(), v4::YAxis());

		m_inst0.m_model = m_rdr.res_mgr().FindModel(EStockModel::UnitQuad);
		
		auto tex = m_rdr.res_mgr().FindTexture(EStockTexture::Checker);
		for (Nugget& nug : m_inst0.m_model->m_nuggets)
			nug.m_tex_diffuse = tex;

		m_inst1.m_model = m_rdr.res_mgr().FindModel(EStockModel::BBoxModel);
		m_inst1.m_i2w = m4x4::Identity();
		m_inst1.m_tint = Colour32White;
		m_scn.AddInstance(m_inst1);

		m_inst0.m_i2w = m4x4::Identity();
		m_inst0.m_tint = Colour32Green;
		m_scn.AddInstance(m_inst0);
	}
	void OnWindowPosChange(WindowPosEventArgs const& args) override
	{
		Form::OnWindowPosChange(args);
		if (!args.m_before)
		{
			iv2 sz(args.m_wp->cx, args.m_wp->cy);
			m_wnd.BackBufferSize(sz, false);
			m_scn.m_viewport.Set(sz);
		}
	}
	static RdrSettings RSettings(HINSTANCE hinstance)
	{
		return RdrSettings(hinstance)
			.DebugLayer()
			.DefaultAdapter();
	}
	static WndSettings WSettings(HWND hwnd, RdrSettings const& rdr_settings)
	{
		return WndSettings(hwnd, true, rdr_settings)
			.DefaultOutput()
			.Size(800,600);
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
			main.m_inst0.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			main.m_inst1.m_i2w = m4x4::Transform(time*0.5f, time*0.3f, time*0.1f, v4::Origin());
			auto frame = main.m_wnd.RenderFrame();
			frame.Render(main.m_scn);
			frame.Present();
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
