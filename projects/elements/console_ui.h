#pragma once

#include "elements/forward.h"
#include "elements/view_base.h"

namespace ele
{
	struct ConsoleUI
	{
		GameInstance& m_inst;
		pr::Console m_cons;
		pr::SimMsgLoop m_loop;
		std::shared_ptr<ViewBase> m_view;

		ConsoleUI(GameInstance& inst);
		void Run(double elapsed);

		PR_NO_COPY(ConsoleUI);
	};
}