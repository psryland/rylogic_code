// Fluid
#include "src/fluid_simulation.h"
#include "src/fluid_visualisation.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	FluidVisualisation::FluidVisualisation(FluidSimulation const& sim, Renderer& rdr)
		: m_sim(&sim)
		, m_rdr(&rdr)
		, m_gs_points(Shader::Create<shaders::PointSpriteGS>(v2(2*sim.m_particles.m_radius), true))
		, m_fluid()
		, m_container()
	{
		// Create the model for the container
		m_container.m_model = rdr12::ModelGenerator::Quad(*m_rdr, AxisId::PosY, v2::Zero(), 2, 2);

		// Create a dynamic model
		auto vb = ResDesc::VBuf<Vert>(m_sim->ParticleCount(), nullptr);
		auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
		auto mdesc = ModelDesc(vb, ib).name("particles");
		m_fluid.m_model = rdr.res().CreateModel(mdesc);
	
		// Use the point sprite shader
		m_fluid.m_model->CreateNugget(NuggetDesc(ETopo::PointList, EGeom::Vert|EGeom::Colr|EGeom::Tex0)
			.use_shader(ERenderStep::RenderForward, m_gs_points)
			.tex_diffuse(rdr.res().StockTexture(EStockTexture::WhiteSpot))
			.irange(0,0)
			);
	}
	FluidVisualisation::~FluidVisualisation()
	{
		// Todo, wait for rendering to finish and remove instance from scene
	}

	// Add the particles to the scene that renders them
	void FluidVisualisation::AddToScene(Scene& scene)
	{
		// Update the positions of the particles in the vertex buffer
		UpdateSubresourceScope update = m_fluid.m_model->UpdateVertices();
		auto* ptr = update.ptr<Vert>();
		for (auto& particles : m_sim->m_particles)
		{
			ptr->m_vert = particles.m_pos;
			ptr->m_diff = Colour32(0xFF0055EE);
			ptr->m_norm = v4::Zero();
			ptr->m_tex0 = v2::Zero();
			ptr->pad = v2::Zero();
			++ptr;
		}
		update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);

		// Add the instance to the scene to be rendered
		scene.AddInstance(m_fluid);
		scene.AddInstance(m_container);
	}
}