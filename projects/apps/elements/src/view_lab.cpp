#include "elements/stdafx.h"
#include "elements/view_lab.h"
#include "elements/view_base.h"
#include "elements/game_instance.h"
#include "elements/material.h"

using namespace pr::console;
using namespace std::placeholders;

namespace ele
{
	enum
	{
		ElementsListHeight = 16,
		MaterialsListHeight = 16,
		DetailsPanelHeight = 14,
		ExperimentHeight = 6,
		RelatedMaterialsHeight = 10,
	};

	ViewLab::ViewLab(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
		,m_pad_elements()
		,m_pad_materials()
		,m_pad_detail()
		,m_pad_mats()
		,m_pad_experiment()
		,m_pad_popup()
		,m_show_elements(true)
		,m_show_popup(false)
		,m_reaction()
	{
		m_pad_elements .OnFocusChanged = [this](Pad&){ UpdateUI(); };
		m_pad_materials.OnFocusChanged = [this](Pad&){ UpdateUI(); };
		m_pad_mats     .OnFocusChanged = [this](Pad&){ UpdateUI(); };

		m_pad_elements.OnTab += [this](Pad&, Evt_Tab const&){ m_pad_mats    .Focus(true); };
		m_pad_mats    .OnTab += [this](Pad&, Evt_Tab const&){ m_pad_elements.Focus(true); };

		m_pad_elements .OnKeyDown += std::bind(&ViewLab::KeyHandler_KeyDown, this, _1, _2);
		m_pad_materials.OnKeyDown += std::bind(&ViewLab::KeyHandler_KeyDown, this, _1, _2);
		m_pad_mats     .OnKeyDown += std::bind(&ViewLab::KeyHandler_KeyDown, this, _1, _2);

		m_pad_elements.Focus(true);
		UpdateUI();
	}

	void ViewLab::UpdateUI()
	{
		PopulateElementsList();
		PopulateMaterialList();
		PopulateExperiment();
		UpdateDetailPads();
		Render();
	}

	// Update the view
	void ViewLab::Render() const
	{
		Scope s(m_cons);

		// Write the title
		m_cons.Write(EAnchor::TopLeft, "Material Lab");

		// Display the elements or materials
		auto& list = (m_show_elements ? m_pad_elements : m_pad_materials);
		list.Draw(m_cons, EAnchor::TopLeft, 0, TitleHeight);

		// Display the details view
		m_pad_detail.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight);

		// Draw the experiment view
		m_pad_experiment.Draw(m_cons, EAnchor::TopLeft, 0, TitleHeight + (int)list.WindowHeight());

		// For the elements view, draw the materials that are known to include that element
		if (m_show_elements)
			m_pad_mats.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight + (int)m_pad_detail.WindowHeight());

		// Display the popup if visible
		if (m_show_popup)
			m_pad_popup.Draw(m_cons, EAnchor::Centre);

		// Determine if an experiment can be run
		// For this we need two materials, and a value for the input energy
		// The user then has the option to run the experiment to see the results
		bool reaction_possible = m_reaction.m_mat1 != nullptr && m_reaction.m_mat2 != nullptr;

		std::vector<std::string> options;
		options.push_back(m_show_elements ? "M - show materials" : "E - show elements");
		if (reaction_possible) options.push_back("R - react materials");
		options.push_back("P - periodic table");
		RenderMenu(EView::MaterialLab, options);
	}

	// Populate the panel with all of the known elements
	void ViewLab::PopulateElementsList()
	{
		auto selected = m_pad_elements.Selected();
		m_pad_elements.Clear();
		m_pad_elements.Title(" A# | Elements ====== | % ", EColour::Black, EAnchor::Left);
		m_pad_elements.Border(m_pad_elements.Focus() ? EColour::BrightGreen : EColour::Black);
		m_pad_elements.Size(m_panel_width, ElementsListHeight);

		// Add the known elements
		for (auto& elem : m_inst.m_lab.m_element_order)
		{
			PR_ASSERT(PR_DBG, pr::AllSet(elem->m_known_properties, EElemProp::Existence), "Only known elements should be in this list");

			// If the element atomic number is known, show it
			if (pr::AllSet(elem->m_known_properties, EElemProp::AtomicNumber))
				m_pad_elements << pr::FmtS("%3.0d | ", elem->m_atomic_number);
			else
				m_pad_elements << " ?? | ";

			// If the element name is known, show it
			if (pr::AllSet(elem->m_known_properties, EElemProp::Name))
				m_pad_elements << pr::FmtS("%-16s|", elem->m_name->m_fullname);
			else
				m_pad_elements << pr::FmtS("%-16s|", " ");

			// Display the percentage of the known properties
			pr::uint prop_count  = pr::CountBits<pr::uint>(m_inst.m_lab.m_known_properties);
			pr::uint known_count = pr::CountBits<pr::uint>(elem->m_known_properties);
			m_pad_elements << pr::FmtS("%3.0d%%", known_count * 100 / prop_count);

			m_pad_elements << "\n";
		}
		m_pad_elements.Selected(selected); // Restore the selection
	}

	// Populate the panel with all of the known materials
	void ViewLab::PopulateMaterialList()
	{
		auto selected = m_pad_materials.Selected();
		m_pad_materials.Clear();
		m_pad_materials.Title(" Materials ==== | Sym | % ", EColour::Black, EAnchor::Left);
		m_pad_materials.Border(m_pad_materials.Focus() ? EColour::Green : EColour::Black);
		m_pad_materials.Size(m_panel_width, MaterialsListHeight);

		// Add the known materials
		for (auto& mat : m_inst.m_lab.m_materials_order)
		{
			PR_ASSERT(PR_DBG, mat->m_discovered, "Only known materials should be in this list");
			m_pad_materials << EColour::Blue;

			// Display the name for the material, use the common name if the chemical name isn't known
			m_pad_materials << pr::FmtS("%20s | %7s | ", mat->m_name.c_str(), mat->m_name_symbolic.c_str());

			// Display a percentage of the known properties
			m_pad_materials << 0; //todo

			m_pad_materials << "\n";
		}
		m_pad_materials.Selected(selected); // Restore the selection
	}

	// Populate the panel with details of the given element
	void ViewLab::PopulateDetail(Element const* elem)
	{
		m_pad_detail.Clear();
		m_pad_detail.Title(" Element Info ");
		m_pad_detail.Border(EColour::Black);
		m_pad_detail.Size(m_panel_width, DetailsPanelHeight);

		// Display info about each known element property
		if (elem != nullptr)
		{
			for (auto prop : EElemProp::Members())
			{
				// If the player doesn't know about this property yet, skip it
				if (!pr::AllSet(m_inst.m_lab.m_known_properties, prop))
					continue;

				switch (prop)
				{
				default: throw std::exception("Unknown element property");
				case EElemProp::Existence: break;
				case EElemProp::Name:
					m_pad_detail << pr::FmtS("%-16s: %s\n", "Name", elem->m_name->m_fullname);
					break;
				case EElemProp::AtomicNumber:
					m_pad_detail << pr::FmtS("%-16s: %d\n", "Atomic Number", elem->m_atomic_number);
					break;
				case EElemProp::MeltingPoint:
					m_pad_detail << pr::FmtS("%-16s: %1.0f°C\n", "Melting Point", elem->m_melting_point);
					break;
				case EElemProp::BoilingPoint:
					m_pad_detail << pr::FmtS("%-16s: %1.0f°C\n", "Boiling Point", elem->m_boiling_point);
					break;
				case EElemProp::ValenceElectrons:
					m_pad_detail << pr::FmtS("%-16s: %d\n", "Valence Electrons", elem->m_valence_electrons);
					break;
				case EElemProp::ElectroNegativity:
					m_pad_detail << pr::FmtS("%-16s: %1.2f\n", "Electronegativity", elem->m_electro_negativity);
					break;
				case EElemProp::AtomicRadius:
					m_pad_detail << pr::FmtS("%-16s: %1.2fm\n", "Atomic Radius", elem->m_atomic_radius);
					break;
				}
			}
		}
	}

	// Populate the panel with details of the given material
	void ViewLab::PopulateDetail(Material const* mat)
	{
		m_pad_detail.Clear();
		m_pad_detail.Title(" Material Info ");
		m_pad_detail.Border(EColour::Black);
		m_pad_detail.Size(m_panel_width, DetailsPanelHeight);

		if (mat != nullptr)
		{
			// Display material names
			if (pr::AllSet(m_inst.m_lab.m_known_properties, EElemProp::Name))
			{
				m_pad_detail << pr::FmtS("%-16s: %s\n", "Common Name", mat->m_name_common.c_str());
				m_pad_detail << pr::FmtS("%-16s: %s\n", "Chemical Name", mat->m_name.c_str());
				m_pad_detail << pr::FmtS("%-16s: %s\n", "Formula", mat->m_name_symbolic.c_str());
			}

			// Display the strength of the materials stuck together ness
			m_pad_detail << pr::FmtS("%-16s: %1.0f°C\n", "Chemical Stability", mat->m_enthalpy);

			// Display measured properties
			if (pr::AllSet(m_inst.m_lab.m_known_properties, EElemProp::MeltingPoint))
			{
				m_pad_detail << pr::FmtS("%-16s: %1.0f°C\n", "Melting Point", mat->m_melting_point);
			}
			if (pr::AllSet(m_inst.m_lab.m_known_properties, EElemProp::BoilingPoint))
			{
				m_pad_detail << pr::FmtS("%-16s: %1.0f°C\n", "Boiling Point", mat->m_boiling_point);
			}

			// Display properties derived from electronegativity
			if (pr::AllSet(m_inst.m_lab.m_known_properties, EElemProp::AtomicNumber))
			{
				// leads to molar mass, density, etc
			}

			// Display properties derived from electronegativity
			if (pr::AllSet(m_inst.m_lab.m_known_properties, EElemProp::ElectroNegativity))
			{
				m_pad_detail << pr::FmtS("%-16s: %1.2f\n", "Ionic Bond Strength", mat->m_ionicity);
			}
		}
	}

	// Populate the list of materials known to contain 'element'
	void ViewLab::PopulateRelatedMaterials(Element const* element)
	{
		auto selected = m_pad_mats.Selected();
		m_pad_mats.Clear();
		m_pad_mats.Title(" Related Materials ", EColour::Black, EAnchor::Centre);
		m_pad_mats.Border(m_pad_mats.Focus() ? EColour::Green : EColour::Black);
		m_pad_mats.Size(m_panel_width, RelatedMaterialsHeight);
		m_pad_mats << EColour::Blue;

		if (element != nullptr)
		{
			PR_ASSERT(PR_DBG, pr::AllSet(element->m_known_properties, EElemProp::Existence), "Only known elements should be used to populate the related materials list");

			// Search the list of known materials for those known to be based on 'element'
			for (auto& mat : m_inst.m_lab.RelatedMaterials(*element))
			{
				PR_ASSERT(PR_DBG, mat->m_discovered, "Only known materials should be in this list");
				m_pad_mats << pr::FmtS("%20s | %7s \n", mat->m_name.c_str(), mat->m_name_symbolic.c_str());
			}
		}
		m_pad_mats.Selected(selected); // Restore the selection
	}

	// Populate the popup with the known periodic table
	void ViewLab::PopulatePeriodicTable()
	{
		m_pad_popup.Clear();
		m_pad_popup.Title(" Periodic Table ");
		m_pad_popup.Border(EColour::Black);

		std::string s;
		for (auto& e : m_inst.m_lab.m_elements)
			s.append("| ").append(pr::FmtS("%-2s", e.m_name->m_symbol)).append(" ").append(e.IsNobal() ? "|\n" : "");

		m_pad_popup << s;
		m_pad_popup.AutoSize();
		m_pad_popup.OnKeyDown = std::bind(&ViewLab::KeyHandler_ClosePopup, this, _1, _2);
	}

	// Populate the fields of the experiment pad
	void ViewLab::PopulateExperiment()
	{
		m_pad_experiment.Clear();
		m_pad_experiment.Title(" Experiment ", EColour::Black, EAnchor::Left);
		m_pad_experiment.Border(EColour::Purple);
		m_pad_experiment.Size(m_panel_width, ExperimentHeight);

		m_pad_experiment << pr::FmtS("%-16s: %s\n", "Reactant 1", m_reaction.m_mat1 ? m_reaction.m_mat1->m_name.c_str() : "");
		m_pad_experiment << pr::FmtS("%-16s: %s\n", "Reactant 2", m_reaction.m_mat2 ? m_reaction.m_mat2->m_name.c_str() : "");
		m_pad_experiment << pr::FmtS("%-16s: %.3f\n", "Energy Added", m_reaction.m_input_energy);
	}

	// Update the details in the detail pad
	void ViewLab::UpdateDetailPads()
	{
		auto& pad = (m_show_elements ? m_pad_elements : m_pad_materials);
		auto selected = pad.Selected();

		if (m_show_elements)
		{
			Element const* elem = nullptr;
			if (selected >= 0 && selected < int(m_inst.m_lab.m_element_order.size()))
				elem = m_inst.m_lab.m_element_order[selected];
			
			PopulateDetail(elem);
			PopulateRelatedMaterials(elem);
		}
		else
		{
			Material const* mat = nullptr;
			if (selected >= 0 && selected < int(m_inst.m_lab.m_materials_order.size()))
				mat = m_inst.m_lab.m_materials_order[selected];
			PopulateDetail(mat);
		}
	}

	// Keydown handler for pads
	void ViewLab::KeyHandler_KeyDown(Pad& pad, Evt_KeyDown const& e)
	{
		auto selected = pad.Selected();
		int shift = (int)pad.Height() / 2;

		// Page up/down, scrolls up/down on the focused pad
		if (e.m_key.wVirtualKeyCode == VK_PRIOR) // ie. page up
		{
			pad.DisplayOffset(0, pad.DisplayOffset().Y - shift);
			pad.Selected(std::max(selected - shift, 0));
		}
		if (e.m_key.wVirtualKeyCode == VK_NEXT) // ie. page down
		{
			pad.DisplayOffset(0, pad.DisplayOffset().Y + shift);
			pad.Selected(selected + shift);
		}

		// Up/Down arrows scroll the selection by 1 line
		if (e.m_key.wVirtualKeyCode == VK_UP)
		{
			pad.Selected((selected + pad.LineCount() - 1) % pad.LineCount());
		}
		if (e.m_key.wVirtualKeyCode == VK_DOWN)
		{
			pad.Selected((selected + 1) % pad.LineCount());
		}

		// Enter is context sensitive
		if (e.m_key.wVirtualKeyCode == VK_RETURN)
		{
			// Enter on the material list adds the material to the reaction
			if (&pad == &m_pad_materials)
			{
				if (auto mat = Find(m_inst.m_lab.m_materials_order, selected))
				{
					std::swap(m_reaction.m_mat1, m_reaction.m_mat2);
					m_reaction.m_mat1 = *mat;
				}
			}
			else if (&pad == &m_pad_mats)
			{
				if (auto element = Find(m_inst.m_lab.m_element_order, m_pad_elements.Selected()))
				{
					auto mats = m_inst.m_lab.RelatedMaterials(**element);
					if (auto mat = Find(mats, selected))
					{
						std::swap(m_reaction.m_mat1, m_reaction.m_mat2);
						m_reaction.m_mat1 = *mat;
					}
				}
			}
		}
		UpdateUI();
	}

	// Keydown handler for popups
	void ViewLab::KeyHandler_ClosePopup(Pad&, Evt_KeyDown const& e)
	{
		// Escape clears the popup view if visible
		if (e.m_key.wVirtualKeyCode == VK_ESCAPE)
		{
			m_show_popup = false;
			(m_show_elements ? m_pad_elements : m_pad_materials).Focus(true);
			Render();
			return;
		}
	}

	// Handle key down events
	void ViewLab::OnEvent(pr::console::Evt_KeyDown const& e)
	{
		ViewBase::HandleKeyEvent(EView::MaterialLab, e);
	}

	void ViewLab::OnEvent(pr::console::Evt_Line<char> const& e)
	{
		std::string option = e.m_input;
		std::transform(begin(option), end(option), begin(option), ::tolower);

		if (option[0] == 'p')
		{
			PopulatePeriodicTable();
			m_pad_popup.Focus(true);
			m_show_popup = true;
			Render();
			return;
		}
		if (option[0] == 'e')
		{
			m_show_elements = true;
			UpdateUI();
			return;
		}
		if (option[0] == 'm')
		{
			m_show_elements = false;
			UpdateUI();
			return;
		}
		ViewBase::HandleOption(EView::MaterialLab, option);
	}

	void ViewLab::OnEvent(Evt_Discovery const& e)
	{
		m_pad_popup.Clear();
		m_pad_popup.Colour(EColour::White, EColour::Blue);
		m_pad_popup.Title(" Discovery! ", EColour::BrightGreen, EAnchor::HCentre);
		m_pad_popup.Border(EColour::White);
		m_pad_popup << "\n" << e.m_blurb << "\n";
		m_pad_popup.AutoSize();
		m_pad_popup.OnKeyDown = std::bind(&ViewLab::KeyHandler_ClosePopup, this, _1, _2);
		m_show_popup = true;
		Render();
	}
}
