#pragma once

#include "elements/forward.h"

namespace ele
{
	struct ConsoleUI
	{
		GameInstance* m_inst;
		pr::Console m_cons;
		pr::SimMsgLoop m_loop;

		ConsoleUI(GameInstance& inst);

		void Run(double elapsed);

		void Render();
		void RenderWorldState();
		void RenderShipSpec();
		void RenderMenu();
	};
}