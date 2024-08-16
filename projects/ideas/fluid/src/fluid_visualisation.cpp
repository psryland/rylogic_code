// Fluid
#include "src/fluid_visualisation.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	FluidVisualisation::FluidVisualisation(FluidSimulation& sim, Renderer& rdr, Scene& scn, std::string_view ldr)
		: m_sim(&sim)
		, m_rdr(&rdr)
		, m_scn(&scn)
		, m_gfx_container(rdr12::CreateLdr(*m_rdr, ldr))
		, m_gs_points(Shader::Create<shaders::PointSpriteGS>(v2(2*sim.Params.ParticleRadius), true))
		, m_gfx_fluid()
		, m_gfx_vector_field()
		, m_read_back()
		, m_last_read_back(-1)
	{
		// Create a dynamic model for the fluid particles (using the particle buffer)
		{
			auto vb = ResDesc::VBuf<Vert>(m_sim->m_r_particles.get()).usage(EUsage::UnorderedAccess);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto mdesc = ModelDesc(vb, ib).name("Fluid:Particles");
			m_gfx_fluid.m_model = rdr.res().CreateModel(mdesc, m_sim->m_r_particles, nullptr);
			m_gfx_fluid.m_model->CreateNugget(NuggetDesc(ETopo::PointList, EGeom::Vert | EGeom::Colr | EGeom::Tex0)
				.use_shader(ERenderStep::RenderForward, m_gs_points)
				.tex_diffuse(rdr.res().StockTexture(EStockTexture::WhiteSpike))
				.irange(0, 0));
		}

		// Create a dynamic model for the pressure gradient lines
		{
			auto vb = ResDesc::VBuf<Vert>(2LL * m_sim->Params.NumParticles, nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto mdesc = ModelDesc(vb, ib).name("Fluid:VectorField");
			m_gfx_vector_field.m_model = rdr.res().CreateModel(mdesc);
			m_gfx_vector_field.m_model->CreateNugget(NuggetDesc(ETopo::LineList, EGeom::Vert | EGeom::Colr).irange(0, 0));
		}

		// Create a texture for displaying a fluid property
		{
			auto src = Image(4096, 4096, nullptr, DXGI_FORMAT_B8G8R8A8_UNORM);
			auto rdesc = ResDesc::Tex2D(src, 1).usage(EUsage::UnorderedAccess);
			auto tdesc = TextureDesc(AutoId, rdesc).name("Fluid:Map");
			m_tex_map = rdr.res().CreateTexture2D(tdesc);

			auto opts = ModelGenerator::CreateOptions().bake(m4x4::Translation(0,0,-0.01f));
			m_gfx_map.m_model = ModelGenerator::Quad(rdr, AxisId::PosZ, { 0, 0 }, 2, 2, iv2::Zero(), &opts);
			m_gfx_map.m_model->m_name = "Fluid:MapQuad";
			
			auto& nug = m_gfx_map.m_model->m_nuggets.front();
			nug.m_tex_diffuse = m_tex_map;
			nug.m_sam_diffuse = rdr.res().StockSampler(EStockSampler::PointClamp);
		}

		// Make sure everything ready to go
		rdr.res().FlushToGpu(EGpuFlush::Block);
	}
	FluidVisualisation::~FluidVisualisation()
	{
		// Remove instances from the scene before deleting them
		m_scn->ClearDrawlists();
	}

	// Add the particles to the scene that renders them
	void FluidVisualisation::AddToScene(GpuJob& job, Scene& scene)
	{
		// The container
		scene.AddInstance(m_gfx_container);

		// The particles
		Tweakable<bool, "ShowParticles"> ShowParticles = true;
		if (ShowParticles)
		{
			Tweakable<float, "DropletSize"> DropletSize = 0.4f;
			m_gs_points->m_size = v2(DropletSize * 2 * m_sim->Params.ParticleRadius);
			scene.AddInstance(m_gfx_fluid);
		}

		// Update the vector field
		Tweakable<int, "VectorFieldMode"> VectorFieldMode = 0;
		if (VectorFieldMode != 0)
		{
			Tweakable<float, "VectorFieldScale"> VectorFieldScale = 0.01f;
			Colour32 const col = 0xFF800000;
				
			UpdateSubresourceScope update = m_gfx_vector_field.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto const& particle : ReadParticles(job))
			{
				ptr->m_vert = particle.pos;
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;

				ptr->m_vert = particle.pos + VectorFieldScale * (
					VectorFieldMode == 1 ? particle.vel :
					VectorFieldMode == 2 ? particle.acc.w0() :
					VectorFieldMode == 3 ? particle.mass * v4::YAxis() :
					v4::Zero());
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			scene.AddInstance(m_gfx_vector_field);
		}

		// Show the density map
		Tweakable<int, "MapType"> MapType = 0;
		if (MapType != 0)
		{
			FluidSimulation::MapData map_data = {
				.MapToWorld = m4x4::Scale(2.0f/m_tex_map->m_dim.xy.x, 2.0f/m_tex_map->m_dim.xy.y, 1.0f, v4(-1, -1, 0, 1)),
				.MapTexDim = m_tex_map->m_dim.xy,
				.MapType = MapType,
			};
			m_sim->GenerateMap(job, m_tex_map, map_data);
			scene.AddInstance(m_gfx_map);
		}

		// Update the gradient
		#if 0
		if (Params.ShowGradients)
		{
			Tweakable<float, "GradientScale"> Scale = 0.0001f;
			Colour32 const col = Colour32Green;

			UpdateSubresourceScope update = m_gfx_gradient.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto& particle : ReadParticles())
			{
				ptr->m_vert = particle.pos;
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;

				ptr->m_vert = particle.pos + Scale * m_sim->PressureAt(particle.m_pos, m_sim->m_particles.index(particle));
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			scene.AddInstance(m_gfx_gradient);
		}
		#endif

		#if 0 // Read back particles
		auto const& p = particles[0];
			
		if (m_time != m_last_frame_rendered)
			OutputDebugStringA(pr::FmtS("Pos: %3.3f %3.3f %3.3f - |Vel|: %3.3f\n"
				,p.pos.x, p.pos.y, p.pos.z
				,Length(p.vel)
		));
		#endif
	}

	// Read the particles from the GPU
	std::span<Particle const> FluidVisualisation::ReadParticles(GpuJob& job)
	{
		if (m_last_read_back != m_sim->m_frame)
		{
			m_read_back.resize(m_sim->Params.NumParticles);
			m_sim->ReadParticles(job, m_read_back);
		}
		return m_read_back;
	}

	// Handle input
	void FluidVisualisation::OnMouseButton(gui::MouseEventArgs&)
	{
	}
	void FluidVisualisation::OnMouseMove(gui::MouseEventArgs&)
	{
	}
	void FluidVisualisation::OnMouseWheel(gui::MouseWheelArgs&)
	{
	}
	void FluidVisualisation::OnKey(gui::KeyEventArgs&)
	{
	}
}
