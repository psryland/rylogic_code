#pragma once
#include "src/forward.h"
#include "src/view_base.h"

namespace ele
{
	struct ConsoleUI
	{
		GameInstance& m_inst;
		Console m_cons;
		SimMsgLoop m_loop;
		std::shared_ptr<ViewBase> m_view;

		ConsoleUI(GameInstance& inst);
		void Run(double elapsed);

		PR_NO_COPY(ConsoleUI);
	};
}