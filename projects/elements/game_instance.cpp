#include "elements/stdafx.h"
#include "elements/game_instance.h"

namespace ele
{


	GameInstance::GameInstance(int seed)
		:m_constants(seed)
		,m_world_state(m_constants)
		,m_stockpile()
		,m_ship()
		,m_view(EView::Home)
	{
	}

	void GameInstance::Step(pr::seconds_t elapsed)
	{
		m_world_state.Step(elapsed);
		m_stockpile.Step(elapsed);
	}

	// Called at the end of the game when the star goes nova
	void GameInstance::Supernova()
	{
	}


	//	int Run()
	//	{
	//		m_cons.Write(EAnchor::TopCentre, "Elements");
	//		for (;;)
	//		{
	//			Render();
	//			if (!HandleInput()) break;
	//		}
	//		return 0;
	//	}

	//	void Render()
	//	{
	//		m_cons.Clear();
	//		RenderInventry();
	//		ShowMenu();
	//	}

	//	void RenderInventry()
	//	{
	//	}

	//	void ShowMenu()
	//	{
	//		std::string str =
	//			"Menu:\n"
	//			"m - Mine for new elements\n"
	//			"q - quit\n";
	//		m_cons.Write(EAnchor::BottomLeft, str);
	//	}

	//	bool HandleInput()
	//	{
	//		std::string input = m_cons.GetLine();
	//		if (input == "q") return false;
	//		if (input == "m") return true;
	//		return true;
	//	}
	//};
}