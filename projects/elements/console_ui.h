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
		void RenderHomeView();
		void RenderShipView();
		void RenderLabView();
		void RenderLaunchView();
		void RenderWorldState();
		void RenderMaterialInventory();
		void RenderShipSpec();
		void RenderMenu();
	};
}