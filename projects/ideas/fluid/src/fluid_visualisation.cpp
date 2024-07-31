// Fluid
#include "src/fluid_simulation.h"
#include "src/fluid_visualisation.h"
#include "src/ispatial_partition.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	FluidVisualisation::FluidVisualisation(FluidSimulation& sim, Renderer& rdr, Scene& scn)
		: m_sim(&sim)
		, m_rdr(&rdr)
		, m_scn(&scn)
		, m_gfx_container()
		, m_gs_points(Shader::Create<shaders::PointSpriteGS>(v2(2*sim.m_radius), true))
		, m_gfx_fluid()
		, m_gfx_gradient()
		, m_gfx_velocities()
	{
		// Create the model for the container
		ldr::Builder L;
		auto& g = L.Group();
		auto r = sim.m_radius;
		g.Plane("floor", 0x80008000).wh(2.0f + 2*r, 0.1f).pos(v4(0, -0.5f-r, 0, 1)).dir(v4::YAxis());
		g.Plane("wall-L", 0x80008000).wh(0.1f, 1.0f + 2*r).pos(v4(-1-r, 0, 0, 1)).dir(+v4::XAxis());
		g.Plane("wall-R", 0x80008000).wh(0.1f, 1.0f + 2*r).pos(v4(+1+r, 0, 0, 1)).dir(-v4::XAxis());
		g.Plane("ceiling", 0x80008000).wh(2.0f + 2*r, 0.1f).pos(v4(0, +0.5f+r, 0, 1)).dir(v4::YAxis());
		m_gfx_container = rdr12::CreateLdr(*m_rdr, L.ToString().c_str());
		
		{// Create a dynamic model for the fluid particles
			auto vb = ResDesc::VBuf<Vert>(m_sim->ParticleCount(), nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto& mdesc = ModelDesc(vb, ib).name("particles");
			m_gfx_fluid.m_model = rdr.res().CreateModel(mdesc);

			// Use the point sprite shader
			m_gfx_fluid.m_model->CreateNugget(NuggetDesc(ETopo::PointList, EGeom::Vert | EGeom::Colr | EGeom::Tex0)
				.use_shader(ERenderStep::RenderForward, m_gs_points)
				.tex_diffuse(rdr.res().StockTexture(EStockTexture::WhiteSpike))
				.irange(0, 0));
		}
		{// Create a dynamic model for the pressure gradient lines
			auto vb = ResDesc::VBuf<Vert>(2LL * m_sim->ParticleCount(), nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto& mdesc = ModelDesc(vb, ib).name("pressure gradient");
			m_gfx_gradient.m_model = rdr.res().CreateModel(mdesc);

			m_gfx_gradient.m_model->CreateNugget(NuggetDesc(ETopo::LineList, EGeom::Vert | EGeom::Colr)
				.irange(0, 0));
		}
		{// Create a dynamic model for particle velocities
			auto vb = ResDesc::VBuf<Vert>(2LL * m_sim->ParticleCount(), nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto& mdesc = ModelDesc(vb, ib).name("particle velocities");
			m_gfx_velocities.m_model = rdr.res().CreateModel(mdesc);

			m_gfx_velocities.m_model->CreateNugget(NuggetDesc(ETopo::LineList, EGeom::Vert | EGeom::Colr)
				.irange(0, 0));
		}
	}
	FluidVisualisation::~FluidVisualisation()
	{
		// Remove instances from the scene before deleting them
		m_scn->ClearDrawlists();
	}

	// Add the particles to the scene that renders them
	void FluidVisualisation::AddToScene(Scene& scene)
	{
		// Update the positions of the particles in the vertex buffer
		if (true)
		{
			// Update the colour from the spatial partitioning so we can see when it's wrong
			auto GetColour = [&](auto& particle)
			{
				// Set colour based on velocity
				Colour32 const colours[] = {
					0xFF0000A0,
					0xFFFF0000,
					0xFFFFFF00,
					0xFFFFFFFF,
				};

				auto vel = Length(particle.m_vel);
				static Tweakable<float, "VisMaxSpeed"> VisMaxSpeed = 10.f;
				return Lerp(colours, Clamp(vel / VisMaxSpeed, 0.f, 1.f));
			};

			UpdateSubresourceScope update = m_gfx_fluid.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto& particle : m_sim->m_particles)
			{
				ptr->m_vert = particle.m_pos;
				ptr->m_diff = GetColour(particle);
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);

			static Tweakable<float, "DropletSize"> DropletSize = 1.0f;
			m_gs_points->m_size = v2(DropletSize * 2 * m_sim->m_radius);

			scene.AddInstance(m_gfx_fluid);
		}

		// Update the gradient
		if (false)
		{
			Colour32 const col = Colour32Green;
			static float Scale = 0.0001f;

			UpdateSubresourceScope update = m_gfx_gradient.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto& particle : m_sim->m_particles)
			{
				ptr->m_vert = particle.m_pos;
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;

				ptr->m_vert = particle.m_pos + Scale * m_sim->PressureAt(particle.m_pos, m_sim->m_particles.index(particle));
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			scene.AddInstance(m_gfx_gradient);
		}

		// Update the velocities
		if (false)
		{
			Colour32 const col = 0xFF800000;
			static float Scale = 0.01f;

			UpdateSubresourceScope update = m_gfx_velocities.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto& particle : m_sim->m_particles)
			{
				ptr->m_vert = particle.m_pos;
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;

				ptr->m_vert = particle.m_pos + Scale * particle.m_vel;
				ptr->m_diff = col;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			scene.AddInstance(m_gfx_velocities);
		}

		// The container
		scene.AddInstance(m_gfx_container);
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