// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	enum class EScene
	{
		None        = 0,
		StaticScene = 1 << 0,
		Particles   = 1 << 1,
		VectorField = 1 << 2,
		Map         = 1 << 3,
		_flags_enum = 0,
	};

	struct FluidVisualisation
	{
		#define PR_INST(x)\
			x(m4x4, m_i2w, rdr12::EInstComp::I2WTransform)\
			x(rdr12::ModelPtr, m_model, rdr12::EInstComp::ModelPtr)
		PR_RDR12_DEFINE_INSTANCE(Instance, PR_INST)
		#undef PR_INST
		using PointShaderPtr = rdr12::RefPtr<rdr12::shaders::PointSpriteGS>;

		rdr12::Renderer* m_rdr;
		rdr12::Scene* m_scn;
		rdr12::LdrObjectPtr m_gfx_scene;
		rdr12::Texture2DPtr m_tex_map;
		PointShaderPtr m_gs_points;
		Instance m_gfx_fluid;
		Instance m_gfx_vector_field;
		Instance m_gfx_map;

		FluidVisualisation(rdr12::Renderer& rdr, rdr12::Scene& scn);
		~FluidVisualisation();

		void Init(int particle_count, std::string_view ldr, D3DPtr<ID3D12Resource> particle_buffer);
		void UpdateVectorField(std::span<Particle const> particles, float scale, int mode);
		void AddToScene(rdr12::Scene& scene, EScene flags);

		// Handle input
		void OnMouseButton(gui::MouseEventArgs& args);
		void OnMouseMove(gui::MouseEventArgs& args);
		void OnMouseWheel(gui::MouseWheelArgs& args);
		void OnKey(gui::KeyEventArgs& args);
	};
}
