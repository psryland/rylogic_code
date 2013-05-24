#pragma once

#include "elements/forward.h"

namespace ele
{
	struct IView
	{
		pr::Console& m_cons;
		GameInstance& m_inst;
		std::string m_input;

		IView(pr::Console& cons, GameInstance& inst)
			:m_cons(cons)
			,m_inst(inst)
			,m_input()
		{}

		virtual ~IView() {}

		// Step the view, returns the next view to display
		virtual EView Step(double elapsed) = 0;
	
	private:
		PR_NO_COPY(IView);
	};
}