#pragma once

#include "elements/forward.h"

namespace ele
{
	struct PromptBox :pr::console::Pad
	{
		std::string m_message;
		strvec_t m_options;

		pr::Event<void(int)> OnOptionSelected;//(int option_index);

		PromptBox(pr::console::EColour fore = pr::console::EColour::Black, pr::console::EColour back = pr::console::EColour::White)
			:pr::console::Pad(fore, back)
		{
			Border(pr::console::EColour::Black);
			SelectionColour(pr::console::EColour::White, pr::console::EColour::Blue);
			OnKeyDown += [this](Pad&, pr::console::Evt_KeyDown const& e)
				{
					switch (e.m_key.wVirtualKeyCode)
					{
					default: break;
					case VK_UP:     Selected(pr::Clamp<int>(Selected() - 1, LineCount() - int(m_options.size()), LineCount() - 1));
					case VK_DOWN:   Selected(pr::Clamp<int>(Selected() + 1, LineCount() - int(m_options.size()), LineCount() - 1));
					case VK_ESCAPE: Close();
					case VK_RETURN:
					}
				};
		}

		void Show(pr::Console& cons)
		{
			Clear(true, true, false, false, false, false);
			*this << m_message;

			Draw(cons,pr::console::EAnchor::Centre);
			Focus(true);
		}
		void Close()
		{
		}

	};
}