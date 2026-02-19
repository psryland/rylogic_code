//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Diagnostic UI system.
// Toggle visibility with F3. Register panels from anywhere in the app.
// Each panel gets its own collapsible section and visibility checkbox.
// In the future, wrap registration in #if LAS_DIAG to strip from release builds.
#pragma once
#include <string>
#include <vector>
#include <functional>
#include "pr/view3d-12/imgui/imgui.h"

namespace las
{
	using ImGuiUI = pr::rdr12::imgui::ImGuiUI;

	struct DiagUI
	{
		struct Panel
		{
			std::string m_name;
			bool m_visible;
			std::function<void(ImGuiUI&)> m_draw;
		};

		bool m_visible;
		std::vector<Panel> m_panels;

		DiagUI()
			: m_visible(false)
			, m_panels()
		{}

		// Toggle the diagnostic overlay on/off
		void Toggle() { m_visible = !m_visible; }

		// Register a diagnostic panel with a draw callback.
		// The callback receives the ImGuiUI and should draw widgets directly.
		void AddPanel(std::string name, std::function<void(ImGuiUI&)> draw)
		{
			m_panels.push_back({ std::move(name), true, std::move(draw) });
		}

		// Draw all visible panels. Call between NewFrame and Render.
		void Draw(ImGuiUI& imgui)
		{
			if (!m_visible)
				return;

			// Panel selector window
			imgui.SetNextWindowPos(10, 300, 1 /*ImGuiCond_Once*/);
			imgui.SetNextWindowSize(320, 0, 1 /*ImGuiCond_Once*/);
			imgui.SetNextWindowBgAlpha(0.85f);
			if (imgui.BeginWindow("Diagnostics [F3]", &m_visible, 0))
			{
				for (auto& panel : m_panels)
				{
					imgui.Checkbox(panel.m_name.c_str(), &panel.m_visible);
				}
			}
			imgui.EndWindow();

			// Draw each visible panel in its own window
			for (auto& panel : m_panels)
			{
				if (!panel.m_visible)
					continue;

				imgui.SetNextWindowSize(350, 0, 1 /*ImGuiCond_Once*/);
				imgui.SetNextWindowBgAlpha(0.85f);
				if (imgui.BeginWindow(panel.m_name.c_str(), &panel.m_visible, 0))
				{
					panel.m_draw(imgui);
				}
				imgui.EndWindow();
			}
		}
	};
}
