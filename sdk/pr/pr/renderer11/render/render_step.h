//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RENDERSTEP_H
#define PR_RDR_RENDER_RENDERSTEP_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// Base class for render steps
		struct RenderStep
		{
			typedef pr::Array<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement> > TDrawList;

			Scene*    m_scene;       // The scene this render step is owned by
			TDrawList m_drawlist;    // The drawlist for this render step
			bool      m_sort_needed; // True when the list needs sorting
			BSBlock   m_bsb;         // Blend states
			RSBlock   m_rsb;         // Raster states
			DSBlock   m_dsb;         // Depth buffer states

			RenderStep(Scene& scene);
			virtual ~RenderStep() {}

			// The type of render step this is
			virtual ERenderStep Id() const = 0;

			// Reset/Populate the drawlist
			void ClearDrawlist();
			void UpdateDrawlist();

			// Sort the drawlist based on sortkey
			void Sort();
			void SortIfNeeded();

			// Add an instance. The instance must be resident for the entire time that it is
			// in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(BaseInstance const& inst);
			template <typename Inst> void AddInstance(Inst const& inst) { AddInstance(inst.m_base); }

			// Remove an instance from the scene
			void RemoveInstance(BaseInstance const& inst);
			template <typename Inst> void RemoveInstance(Inst const& inst) { RemoveInstance(inst.m_base); }

			// Remove a batch of instances. Optimised by a single past through the drawlist
			void RemoveInstances(BaseInstance const** inst, std::size_t count);

			// Perform the render step
			virtual void Execute() = 0;

		private:
			RenderStep(RenderStep const&);
			RenderStep& operator = (RenderStep const&);
		};

		// Forward rendering
		struct ForwardRender
			:RenderStep
		{
			pr::Colour           m_background_colour; // The colour to clear the background to
			Light                m_global_light;      // The global light to use
			D3DPtr<ID3D11Buffer> m_cbuf_frame;        // A constant buffer for the frame constant shader variables
			bool                 m_clear_bb;          // True if this render step clears the backbuffer before rendering

			ForwardRender(Scene& scene, bool clear_bb = true, pr::Colour const& bkgd_colour = pr::ColourBlack);

		private:

			// The type of render step this is
			ERenderStep Id() const { return ERenderStep::ForwardRender; }

			// Perform the render step
			void Execute();
		};
	}
}

#endif
