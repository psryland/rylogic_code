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
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/instance.h"
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
			void Execute(StateStack& ss);

		protected:

			// Add model nuggets to the draw list for this render step
			virtual void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) = 0;

			// Dervied render steps perform their action
			virtual void ExecuteInternal(StateStack& ss) = 0;

		private:
			RenderStep(RenderStep const&);
			RenderStep& operator = (RenderStep const&);
		};

		// Fixed container of render steps. Doesn't really need to be fixed,
		// but non-fixed means we need the pr::rdr::Allocator to construct it.
		typedef pr::Array<RenderStepPtr, 16, true> RenderStepCont;

		// Gbuffer base
		struct GBuffer
		{
			#include "renderer11/shaders/common/gbuffer_cbuf.hlsli"
		};

		// GBufferCreate ******************************************************

		struct GBufferCreate
			:RenderStep
			,GBuffer
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
			D3DPtr<ID3D11Buffer>             m_cbuf_camera;  // A constant buffer for the frame constant shader variables
			pr::Colour const&                m_bkgd_colour; // The colour to clear the background to (ref of value in scene)
			ShaderPtr                        m_shader;      // The shader used to generate the g-buffer

			GBufferCreate(Scene& scene, pr::Colour const& bkgd_colour = pr::ColourBlack);

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
			void ExecuteInternal(StateStack& ss) override;

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

		// DSLightingPass *****************************************************

		struct DSLightingPass
			:RenderStep
			,GBuffer
		{
			// An instance type for the full screen quad
			#define PR_RDR_INST(x)\
				x(ModelPtr ,m_model  ,EInstComp::ModelPtr)
			PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
			#undef PR_RDR_INST

			// Uses g-buffer data to perform post process lighting
			static const ERenderStep::Enum_ Id = ERenderStep::DSLighting;

			GBufferCreate&       m_gbuffer;       // The gbuffer render step for access to the gbuffer textures
			D3DPtr<ID3D11Buffer> m_cbuf_camera;   // A constant buffer for the frame constant shader variables
			D3DPtr<ID3D11Buffer> m_cbuf_lighting; // A constant buffer for the frame constant shader variables
			Instance             m_unit_quad;     // The quad drawn to the screen for post processing
			bool                 m_clear_bb;      // True if this render step clears the backbuffer before rendering
			ShaderPtr            m_shader;        // The shader used to generate the g-buffer

			DSLightingPass(Scene& scene);

		private:

			DSLightingPass(DSLightingPass const&);
			DSLightingPass& operator = (DSLightingPass const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const&, TNuggetChain const&) override {}

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};

		// Forward Render *****************************************************

		struct ForwardRender
			:RenderStep
		{
			static const ERenderStep::Enum_ Id = ERenderStep::ForwardRender;
			#include "renderer11/shaders/common/forward_cbuf.hlsli"

			D3DPtr<ID3D11Buffer> m_cbuf_frame;   // A constant buffer for the frame constant shader variables
			bool                 m_clear_bb;     // True if this render step clears the backbuffer before rendering

			//typedef std::vector<ProjectedTexture> ProjTextCont;
			//ProjTextCont m_proj_tex;

			ForwardRender(Scene& scene, bool clear_bb = true);

		private:

			ForwardRender(ForwardRender const&);
			ForwardRender& operator = (ForwardRender const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};
	}
}
