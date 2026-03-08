#pragma once
#include "src/forward.h"
#include "src/scene/scene.h"

namespace physics_sandbox
{
	// A right-side panel that displays the properties of each rigid body in the scene.
	// Currently a stub that shows name, position, velocity, mass, and KE in a static text control.
	// Future: tree view, editable properties, selection sync with 3D viewport.
	struct DetailsPanel : Panel
	{
		TextBox m_text;
		std::wstring m_last_text;

		DetailsPanel(Panel::Params<> p = Panel::Params<>());

		// Update the displayed properties from the current scene state.
		void Update(Scene const& scene);
	};
}
