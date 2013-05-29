#include "elements/stdafx.h"
#include "elements/view_lab.h"
#include "elements/view_base.h"
#include "elements/game_instance.h"
#include "elements/material.h"

using namespace pr::console;

namespace ele
{
	ViewLab::ViewLab(pr::Console& cons, GameInstance& inst)
		:ViewBase(cons, inst)
		,m_pad_elem()
		,m_pad_mats()
		,m_pad_popup()
		,m_elements_selected(true)
		,m_show_popup(false)
	{
		PopulateElementsList();
		PopulateMaterialList();
		PopulatePeriodicTable();
		Render();
	}

	// Update the view
	void ViewLab::Render() const
	{
		Scope s(m_cons);

		m_cons.Write(EAnchor::TopLeft, "Material Lab");
		m_pad_mats.Draw(m_cons, EAnchor::TopLeft, 0, TitleHeight);
		m_pad_elem.Draw(m_cons, EAnchor::TopRight, 0, TitleHeight);

		std::vector<std::string> options;
		options.push_back("P - periodic table");
		options.push_back("E# - display element info");
		options.push_back("M# - display material info");
		RenderMenu(EView::MaterialLab, options);

		if (m_show_popup)
			m_pad_popup.Draw(m_cons, EAnchor::Centre);
	}

	// Render the panel with the known elements
	void ViewLab::PopulateElementsList()
	{
		m_pad_elem.Clear();
		m_pad_elem.Title(" Known Elements ");
		m_pad_elem.Border(m_elements_selected ? EColour::Green : EColour::Black);
		for (auto& elem : m_inst.m_lab.m_elements)
		{
			//if (!e.m_discovered) return;
			m_pad_elem << elem.m_atomic_number << ". " << elem.m_name->m_fullname << "\n";
		}
		m_pad_elem.Width(m_panel_width);
		m_pad_elem.Height(m_panel_height);
	}

	// Render the panel with the known materials
	void ViewLab::PopulateMaterialList()
	{
		m_pad_mats.Clear();
		m_pad_mats.Title(" Known Materials ");
		m_pad_mats.Border(m_elements_selected ? EColour::Black : EColour::Green);
		for (auto& mat : m_inst.m_lab.m_mats)
		{
			//if (!e.m_discovered) return;
			m_pad_mats << mat.m_index << ". " << mat.m_name << "\n";
		}
		m_pad_mats.Width(m_panel_width);
		m_pad_mats.Height(m_panel_height);
	}

	// Render a popup of the known periodic table
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
	}

	// Populate the popup with the details of an element
	void ViewLab::PopulateElementDetail(Element const& elem)
	{
		m_pad_popup.Clear();
		m_pad_popup.Title(elem.m_name->m_fullname);
		m_pad_popup.Border(EColour::White);
		m_pad_popup << "Symbollic Name: " << elem.m_name->m_symbol << "\n";
		m_pad_popup << "Enthapy: " << 0 << "\n";
		m_pad_popup.AutoSize();
	}

	// Populate the popup with the details of an material
	void ViewLab::PopulateMaterialDetail(Material const& mat)
	{
		m_pad_popup.Clear();
		m_pad_popup.Title(mat.m_name);
		m_pad_popup.Border(EColour::White);
		m_pad_popup << "Symbollic Name: " << mat.m_name_symbolic << "\n";
		m_pad_popup << "Enthapy: " << 0 << "\n";
		m_pad_popup << "Ionicity: " << mat.m_ionicity << "\n";
		m_pad_popup.AutoSize();
	}

	void ViewLab::OnEvent(pr::console::Evt_KeyDown const& e)
	{
		if (!e.m_key.bKeyDown) return;

		// Escape clears the popup view if visible
		if (e.m_key.wVirtualKeyCode == VK_ESCAPE && m_show_popup)
		{
			m_show_popup = false;
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_TAB)
		{
			m_elements_selected = !m_elements_selected;
			m_pad_elem.Border(m_elements_selected ? EColour::Green : EColour::Black);
			m_pad_mats.Border(m_elements_selected ? EColour::Black : EColour::Green);
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_PRIOR) // ie. page up
		{
			auto& pad = m_elements_selected ? m_pad_elem : m_pad_mats;
			int shift = pad.Height() / 2;
			pad.DisplayOffset(0, pad.DisplayOffset().Y - shift);
			pad.Selected(std::max(pad.Selected() - shift, 0));
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_NEXT) // ie. page down
		{
			auto& pad = m_elements_selected ? m_pad_elem : m_pad_mats;
			int shift = pad.Height() / 2;
			pad.DisplayOffset(0, pad.DisplayOffset().Y + shift);
			pad.Selected(pad.Selected() + shift);
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_UP)
		{
			auto& pad = m_elements_selected ? m_pad_elem : m_pad_mats;
			pad.Selected((pad.Selected() + pad.LineCount() - 1) % pad.LineCount());
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_DOWN)
		{
			auto& pad = m_elements_selected ? m_pad_elem : m_pad_mats;
			pad.Selected((pad.Selected() + 1) % pad.LineCount());
			Render();
			return;
		}
		if (e.m_key.wVirtualKeyCode == VK_RETURN)
		{
			if (m_show_popup)
			{
				m_show_popup = false;
				Render();
				return;
			}
			auto& pad = m_elements_selected ? m_pad_elem : m_pad_mats;
			auto selected = pad.Selected();
			if (m_elements_selected && selected >= 0 && selected < int(m_inst.m_lab.m_elements.size()))
			{
				PopulateElementDetail(m_inst.m_lab.m_elements[selected]);
				m_show_popup = true;
				Render();
				return;
			}
			if (!m_elements_selected && selected >= 0 && selected < int(m_inst.m_lab.m_mats.size()))
			{
				PopulateMaterialDetail(m_inst.m_lab.m_mats[selected]);
				m_show_popup = true;
				Render();
				return;
			}
		}
		ViewBase::HandleKeyEvent(EView::MaterialLab, e);
	}

	void ViewLab::OnEvent(pr::console::Evt_Line<char> const& e)
	{
		std::string option = e.m_input;
		std::transform(begin(option), end(option), begin(option), ::tolower);

		size_t num;
		if (option[0] == 'p')
		{
			PopulatePeriodicTable();
			m_show_popup = true;
			Render();
			return;
		}
		if (option[0] == 'e' && (num = pr::To<size_t>(option.c_str()+1, 10)) < m_inst.m_lab.m_elements.size())
		{
			PopulateElementDetail(m_inst.m_lab.m_elements[num]);
			m_show_popup = true;
			Render();
			return;
		}
		if (option[0] == 'm' && (num = pr::To<size_t>(option.c_str()+1, 10)) < m_inst.m_lab.m_mats.size())
		{
			PopulateMaterialDetail(m_inst.m_lab.m_mats[num]);
			m_show_popup = true;
			Render();
			return;
		}
		ViewBase::HandleOption(EView::MaterialLab, option);
	}
}