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
	struct UserSettings { explicit UserSettings(int) {} };

	// Derive a application logic type from pr::app::Main
	struct Main
		:pr::app::Main<Main, MainUI, UserSettings>
		,pr::SpaceInvaders::ISystem
	{
		using base = pr::app::Main<Main, MainUI, UserSettings>;
		using SpaceInvaders = pr::SpaceInvaders;
		static wchar_t const* AppName() { return L"AceInspaders"; };

		#define PR_FIELDS(x)\
		x(pr::rdr::ModelPtr, m_model, pr::rdr::EInstComp::ModelPtr)
		PR_RDR_DEFINE_INSTANCE(ScreenQuad, PR_FIELDS);
		#undef PR_FIELDS

		SpaceInvaders m_space_invaders;
		pr::rdr::Texture2DPtr m_screen_tex;
		ScreenQuad m_screen_quad;

		Main(MainUI& ui)
			:base(pr::app::DefaultSetup(), ui)
			,m_space_invaders(this)
			,m_screen_tex()
			,m_screen_quad()
		{
			using namespace pr;

			// Create a texture to use as the 2D render target
			auto tdesc = rdr::Texture2DDesc{ SpaceInvaders::ScreenDimX, SpaceInvaders::ScreenDimY, 1 };
			auto sdesc = rdr::SamplerDesc::LinearClamp();
			m_screen_tex = m_rdr.m_tex_mgr.CreateTexture2D(rdr::AutoId, rdr::Image(), tdesc, sdesc, false, "ScreenBuf");

			// Set up the renderer to render a quad containing a texture
			auto mat = rdr::NuggetProps{};
			mat.m_tint = Colour32Blue;
			mat.m_tex_diffuse = m_screen_tex;
			m_screen_quad.m_model = rdr::ModelGenerator<>::Quad(m_rdr, &mat);

			// Add the quad to the scene
			m_scene.OnUpdateScene += [&](auto& scn, auto)
			{
				scn.AddInstance(m_screen_quad);
			};
		}


		#pragma region SpaceInvaders::ISystem

		// Reads the system clock
		int SpaceInvaders::ISystem::ClockMS()
		{
			return static_cast<int>(GetTickCount64());
		}

		// Play the indicated sound
		void SpaceInvaders::ISystem::PlaySound(SpaceInvaders::ESound)
		{
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
		{}
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
