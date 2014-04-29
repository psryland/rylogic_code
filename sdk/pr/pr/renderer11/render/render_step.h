//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/util/event_types.h"

namespace pr
{
	namespace rdr
	{
		// Base class for render steps
		struct RenderStep
		{
			typedef pr::Array<DrawListElement, 1024, false, pr::rdr::Allocator<DrawListElement>> TDrawList;

			Scene*    m_scene;       // The scene this render step is owned by
			TDrawList m_drawlist;    // The drawlist for this render step
			bool      m_sort_needed; // True when the list needs sorting
			BSBlock   m_bsb;         // Blend states
			RSBlock   m_rsb;         // Raster states
			DSBlock   m_dsb;         // Depth buffer states

			RenderStep(Scene& scene);
			virtual ~RenderStep() {}

			// The type of render step this is
			virtual ERenderStep::Enum_ GetId() const = 0;
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type const& as() const { return *static_cast<RStep const*>(this); }
			template <typename RStep> typename std::enable_if<std::is_base_of<RenderStep,RStep>::value, RStep>::type&       as()       { return *static_cast<RStep*>(this); }

			// Reset the drawlist
			void ClearDrawlist();

			// Sort the drawlist based on sortkey
			void Sort();
			void SortIfNeeded();

			// Add an instance. The instance,model,and nuggets must be resident for the entire time
			// that it is in the drawlist, i.e. until 'RemoveInstance' or 'ClearDrawlist' is called.
			void AddInstance(BaseInstance const& inst);
			template <typename Inst> void AddInstance(Inst const& inst) { AddInstance(inst.m_base); }

			// Remove an instance from the scene
			void RemoveInstance(BaseInstance const& inst);
			template <typename Inst> void RemoveInstance(Inst const& inst) { RemoveInstance(inst.m_base); }

			// Remove a batch of instances. Optimised by a single past through the drawlist
			void RemoveInstances(BaseInstance const** inst, std::size_t count);

			// Perform the render step
			void Execute();

		protected:

			// Add model nuggets to the draw list for this render step
			virtual void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) = 0;

			// Dervied render steps perform their action
			virtual void ExecuteInternal() = 0;

		private:
			RenderStep(RenderStep const&);
			RenderStep& operator = (RenderStep const&);
		};

		// Fixed container of render steps. Doesn't really need to be fixed,
		// but non-fixed means we need the pr::rdr::Allocator to construct it.
		typedef pr::Array<RenderStepPtr, 16, true> RenderStepCont;

		// GBufferCreate ******************************************************

		struct GBufferCreate
			:RenderStep
			,pr::events::IRecv<pr::rdr::Evt_Resize>
		{
			// Constructs the g-buffer for a scene
			static const ERenderStep::Enum_ Id = ERenderStep::GBufferCreate;
			enum RTEnum_ { RTDiffuse = 0, RTNormal = 1, RTDepth = 2, RTCount = 3 };
			static_assert(RTCount <= D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, "Too many sumultaneous render targets");

			D3DPtr<ID3D11Texture2D>          m_tex[RTCount];
			D3DPtr<ID3D11RenderTargetView>   m_rtv[RTCount];
			D3DPtr<ID3D11ShaderResourceView> m_srv[RTCount];
			D3DPtr<ID3D11DepthStencilView>   m_dsv;
			D3DPtr<ID3D11RenderTargetView>   m_main_rtv;
			D3DPtr<ID3D11DepthStencilView>   m_main_dsv;
			D3DPtr<ID3D11Buffer>             m_cbuf_frame; // A constant buffer for the frame constant shader variables
			ShaderPtr                        m_shader;     // The shader used to generate the g-buffer

			GBufferCreate(Scene& scene);

		private:

			GBufferCreate(GBufferCreate const&);
			GBufferCreate& operator = (GBufferCreate const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Create render targets for the gbuffer based on the current render target size
			void InitGBuffer(bool create_buffers);

			// Set the g-buffer as the render target
			void BindGBuffer(bool bind);

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Perform the render step
			void ExecuteInternal() override;

			// Handle main window resize events
			void OnEvent(Evt_Resize const& evt) override;

			// Debugging helper for printing the render target resource name
			char const* ToString(RTEnum_ rt) const
			{
				switch (rt){
				default: return "unknown";
				case RTDiffuse: return "diffuse";
				case RTNormal:  return "normal";
				case RTDepth:   return "depth";
				}
			}
		};

		// DeferredShading ****************************************************

		struct DeferredShading
			:RenderStep
		{
			// Uses g-buffer data to perform post process lighting
			static const ERenderStep::Enum_ Id = ERenderStep::DeferredShading;

			ModelPtr   m_unit_quad;         // The quad drawn to the screen for post processing
			SceneView  m_unit_view;         // A scene view set up for post processing
			pr::Colour m_background_colour; // The colour to clear the background to
			bool       m_clear_bb;          // True if this render step clears the backbuffer before rendering

			DeferredShading(Scene& scene, bool clear_bb = true, pr::Colour const& bkgd_colour = pr::ColourBlack);

		private:

			DeferredShading(DeferredShading const&);
			DeferredShading& operator = (DeferredShading const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const&, TNuggetChain const&) override {}

			// Perform the render step
			void ExecuteInternal() override;
		};

		// Forward Render *****************************************************

		struct ForwardRender
			:RenderStep
		{
			static const ERenderStep::Enum_ Id = ERenderStep::ForwardRender;

			pr::Colour           m_background_colour; // The colour to clear the background to
			Light                m_global_light;      // The global light to use
			D3DPtr<ID3D11Buffer> m_cbuf_frame;        // A constant buffer for the frame constant shader variables
			bool                 m_clear_bb;          // True if this render step clears the backbuffer before rendering

			//typedef std::vector<ProjectedTexture> ProjTextCont;
			//ProjTextCont m_proj_tex;

			ForwardRender(Scene& scene, bool clear_bb = true, pr::Colour const& bkgd_colour = pr::ColourBlack);

		private:

			ForwardRender(ForwardRender const&);
			ForwardRender& operator = (ForwardRender const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Perform the render step
			void ExecuteInternal() override;
		};
	}
}
