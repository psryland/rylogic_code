#pragma once

#include "pr/app/forward.h"
#include "pr/app/main.h"
#include "pr/app/main_ui.h"
#include "pr/app/default_setup.h"
#include "pr/gui/sim_message_loop.h"
#include "AceInspaders/src/space_invaders.h"

namespace ace
{
	struct Main;
	struct MainUI;

	// Create a UserSettings object for loading/saving app settings
	struct UserSettings
	{
		explicit UserSettings(int) {}
	};

	// Derive a application logic type from pr::app::Main
	struct Main
		:pr::app::Main<Main, MainUI, UserSettings>
		,pr::SpaceInvaders::ISystem
	{
		using base = pr::app::Main<Main, MainUI, UserSettings>;
		using Texture2DPtr = pr::rdr::Texture2DPtr;
		using SpaceInvaders = pr::SpaceInvaders;
		static wchar_t const* AppName() { return L"AceInspaders"; };

		#define PR_FIELDS(x)\
		x(pr::m4x4, m_i2w, pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr, m_model, pr::rdr::EInstComp::ModelPtr)
		PR_RDR_DEFINE_INSTANCE(ScreenQuad, PR_FIELDS);
		#undef PR_FIELDS

		SpaceInvaders m_space_invaders;
		Texture2DPtr m_screen_tex;
		ScreenQuad m_screen_quad;

		Main(MainUI& ui)
			:base(pr::app::DefaultSetup(), ui)
			,m_space_invaders(this)
			,m_screen_tex()
			,m_screen_quad()
		{
			using namespace pr;
			using namespace pr::rdr;
		
			// Display aspect ratio
			auto aspect = (float)SpaceInvaders::ScreenDimX / SpaceInvaders::ScreenDimY;

			// Create a texture to use as the 2D render target
			auto sdesc = SamplerDesc::PointClamp();
			auto tdesc = Texture2DDesc { SpaceInvaders::ScreenDimX, SpaceInvaders::ScreenDimY, 1, DXGI_FORMAT_R8G8B8A8_UNORM, EUsage::Dynamic };
			tdesc.CPUAccessFlags = static_cast<UINT>(ECPUAccess::Write);
			m_screen_tex = m_rdr.m_tex_mgr.CreateTexture2D(AutoId, Image(), tdesc, sdesc, false, "ScreenBuf");
			m_screen_tex->m_t2s.y = -m_screen_tex->m_t2s.y;
			m_screen_tex->m_t2s.pos.y = 1.0f;

			// Setup a flat light
			m_scene.m_global_light.m_type = ELight::Ambient;
			m_scene.m_global_light.m_ambient = 0xFF808080;

			// Set up the renderer to render a quad containing a texture
			auto mat = NuggetProps{};
			mat.m_tint = Colour32White;
			mat.m_tex_diffuse = m_screen_tex;
			
			m_screen_quad.m_model = ModelGenerator<>::Quad(m_rdr, &mat);
			m_screen_quad.m_i2w = pr::m4x4::Scale(aspect, 1.0f, 1.0f, pr::v4Origin);

			// Add the quad to the scene
			m_scene.OnUpdateScene += std::bind(&Main::UpdateScene, this, _1);
			
			// Initialise the display
			DoRender();
		}
		Main::~Main()
		{
			// Clear the drawlists so that destructing models
			// don't assert because they're still in a drawlist.
			m_scene.ClearDrawlists();
		}

		// Step the game
		void Step(double elapsed_s)
		{
			m_space_invaders.Step(static_cast<int>(elapsed_s * 1000));
		}

		// Prepare the scene for render
		void UpdateScene(pr::rdr::Scene& scene)
		{
			using namespace pr;
			using namespace pr::rdr;

			// Render the display
			auto const& display = m_space_invaders.Display();
			{
				Lock lock;
				auto img = m_screen_tex->GetPixels(lock);
				for (int y = 0; y != display.m_dimy; ++y)
				{
					auto px = img.Pixels<uint32_t>(y);
					for (int x = 0; x != display.m_dimx; ++x)
					{
						if (display(x, y))
							*px++ = 0xFF000000;
						else
							*px++ = 0xFFA0A0A0;
					}
				}
			}

			scene.AddInstance(m_screen_quad);
		}

		#pragma region SpaceInvaders::ISystem

		// Play the indicated sound
		void SpaceInvaders::ISystem::PlaySound(SpaceInvaders::ESound)
		{
		}

		// Return user input
		SpaceInvaders::UserInputData SpaceInvaders::ISystem::UserInput()
		{
			SpaceInvaders::UserInputData data = {};
			data.JoystickX = static_cast<int>(1000 * sin(GetTickCount64() * pr::maths::tauf / 4000.0));
			data.FireButton = (GetTickCount64() % 10) == 0;
			return data;
		}

		#pragma endregion
	};

	// Derive a GUI class from pr::app::MainGUI
	struct MainUI :pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>
	{
		using base = pr::app::MainUI<MainUI, Main, pr::gui::SimMsgLoop>;
		static wchar_t const* AppTitle() { return L"Ace Inspaders"; };
		MainUI(wchar_t const*, int)
			:base(Params().title(AppTitle()))
		{
			m_msg_loop.AddStepContext("render", [this](double) { m_main->DoRender(true); }, 60.0f, false);
			m_msg_loop.AddStepContext("step", [this](double s) { m_main->Step(s); }, 60.0f, true);
		}
	};
}

namespace pr::app
{
	// Create the GUI window
	inline std::unique_ptr<IAppMainUI> CreateUI(wchar_t const* lpstrCmdLine, int nCmdShow)
	{
		return std::unique_ptr<IAppMainUI>(new ace::MainUI(lpstrCmdLine, nCmdShow));
	}
}
