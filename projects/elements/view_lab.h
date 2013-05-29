#pragma once

#include "elements/forward.h"
#include "elements/view_base.h"

namespace ele
{
	struct ViewLab
		:ViewBase
		,pr::events::IRecv<pr::console::Evt_Line<char>>
		,pr::events::IRecv<pr::console::Evt_KeyDown>
	{
		pr::console::Pad m_pad_elem;
		pr::console::Pad m_pad_mats;
		pr::console::Pad m_pad_popup;
		bool m_elements_selected; // True if the elements pad is highlighted, false for the mats pad
		bool m_show_popup;        

		ViewLab(pr::Console& cons, GameInstance& inst);

		void Render() const;

		void PopulateElementsList();
		void PopulateMaterialList();
		void PopulatePeriodicTable();
		void PopulateElementDetail(Element const& elem);
		void PopulateMaterialDetail(Material const& mat);

		void OnEvent(pr::console::Evt_Line<char> const& e);
		void OnEvent(pr::console::Evt_KeyDown const&);
	};
}