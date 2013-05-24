#pragma once

#include "elements/forward.h"
#include "elements/iview.h"

namespace ele
{
	struct ConsoleUI
	{
		GameInstance& m_inst;
		pr::Console m_cons;
		pr::SimMsgLoop m_loop;
		std::shared_ptr<IView> m_view;

		ConsoleUI(GameInstance& inst);
		void Run(double elapsed);

	private:
		PR_NO_COPY(ConsoleUI);
	};
}