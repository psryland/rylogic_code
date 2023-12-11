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

		FluidSimulation const* m_sim;
		rdr12::Renderer* m_rdr;
		rdr12::ShaderPtr m_gs_points;
		Instance m_container;
		Instance m_fluid;

		FluidVisualisation(FluidSimulation const& sim, rdr12::Renderer& rdr);
		~FluidVisualisation();

		void AddToScene(rdr12::Scene& scene);
	};
}
