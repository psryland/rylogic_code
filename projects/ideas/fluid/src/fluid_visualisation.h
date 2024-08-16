// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct FluidVisualisation
	{
		#define PR_INST(x)\
			x(rdr12::ModelPtr, m_model, rdr12::EInstComp::ModelPtr)
		PR_RDR12_DEFINE_INSTANCE(Instance, PR_INST)
		#undef PR_INST
		using PointShaderPtr = rdr12::RefPtr<rdr12::shaders::PointSpriteGS>;

		FluidSimulation* m_sim;
		rdr12::Renderer* m_rdr;
		rdr12::Scene* m_scn;
		rdr12::LdrObjectPtr m_gfx_container;
		rdr12::Texture2DPtr m_tex_map;
		PointShaderPtr m_gs_points;
		Instance m_gfx_fluid;
		Instance m_gfx_vector_field;
		Instance m_gfx_map;
		std::vector<Particle> m_read_back;
		int m_last_read_back = 0;

		FluidVisualisation(FluidSimulation& sim, rdr12::Renderer& rdr, rdr12::Scene& scn, std::string_view ldr);
		~FluidVisualisation();

		void AddToScene(GpuJob& job, rdr12::Scene& scene);
		std::span<Particle const> ReadParticles(GpuJob& job);

		// Handle input
		void OnMouseButton(gui::MouseEventArgs& args);
		void OnMouseMove(gui::MouseEventArgs& args);
		void OnMouseWheel(gui::MouseWheelArgs& args);
		void OnKey(gui::KeyEventArgs& args);
	};
}
