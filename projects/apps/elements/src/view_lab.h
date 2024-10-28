#pragma once

#include "elements/forward.h"
#include "elements/view_base.h"
#include "elements/reaction.h"
#include "elements/game_events.h"

namespace ele
{
	struct ViewLab
		:ViewBase
		,pr::events::IRecv<pr::console::Evt_Line<char>>
		,pr::events::IRecv<pr::console::Evt_KeyDown>
		,pr::events::IRecv<Evt_Discovery>
	{
		pr::console::Pad m_pad_elements;    // A list of the known elements
		pr::console::Pad m_pad_materials;   // A list of the known materials
		pr::console::Pad m_pad_detail;      // A panel containing detail about the selected element/material
		pr::console::Pad m_pad_mats;        // Materials based on the selected element (hidden for material list)
		pr::console::Pad m_pad_experiment;  // The panel that describes the experiment to carry out
		pr::console::Pad m_pad_popup;       // Popup window
		bool m_show_elements;               // True while the elements list is shown, false when the materials list is shown
		bool m_show_popup;                  // True while the popup is visible
		Reaction m_reaction;                
		
		ViewLab(pr::Console& cons, GameInstance& inst);

		void UpdateUI();
		void Render() const;

		void PopulateElementsList();
		void PopulateMaterialList();
		void PopulateDetail(Element const* element);
		void PopulateDetail(Material const* material);
		void PopulateRelatedMaterials(Element const* element);
		void PopulatePeriodicTable();
		void PopulateExperiment();

		void UpdateDetailPads();

		void KeyHandler_KeyDown(pr::console::Pad& p, pr::console::Evt_KeyDown const& e);
		void KeyHandler_ClosePopup(pr::console::Pad&, pr::console::Evt_KeyDown const& e);

		void OnEvent(pr::console::Evt_Line<char> const& e);
		void OnEvent(pr::console::Evt_KeyDown const&);
		void OnEvent(Evt_Discovery const&);
	};
}