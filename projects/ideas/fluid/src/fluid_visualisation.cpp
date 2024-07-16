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
		, m_gs_points(Shader::Create<shaders::PointSpriteGS>(v2(2*sim.m_radius), true))
		, m_gfx_fluid()
		, m_gfx_container()
		, m_probe(rdr)
	{
		// Create the model for the container
		m_gfx_container.m_model = rdr12::ModelGenerator::Quad(*m_rdr, AxisId::PosY, v2::Zero(), 2, 2);

		{// Create a dynamic model for the fluid particles
			auto vb = ResDesc::VBuf<Vert>(m_sim->ParticleCount(), nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto mdesc = ModelDesc(vb, ib).name("particles");
			m_gfx_fluid.m_model = rdr.res().CreateModel(mdesc);

			// Use the point sprite shader
			m_gfx_fluid.m_model->CreateNugget(NuggetDesc(ETopo::PointList, EGeom::Vert | EGeom::Colr | EGeom::Tex0)
				.use_shader(ERenderStep::RenderForward, m_gs_points)
				.tex_diffuse(rdr.res().StockTexture(EStockTexture::WhiteSpike))
				.irange(0, 0));
		}
		
		{// Create a dynamic model for the pressure gradient lines
			auto vb = ResDesc::VBuf<Vert>(2 * m_sim->ParticleCount(), nullptr);
			auto ib = ResDesc::IBuf<uint16_t>(0, nullptr);
			auto mdesc = ModelDesc(vb, ib).name("pressure gradient");
			m_gfx_gradient.m_model = rdr.res().CreateModel(mdesc);

			m_gfx_gradient.m_model->CreateNugget(NuggetDesc(ETopo::LineList, EGeom::Vert | EGeom::Colr)
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
		auto within = std::set<int>{};
		auto Idx = [&](auto& particle) { return s_cast<int>(m_sim->m_particles.index(particle)); };
		auto IsWithin = [&](auto& particle) { return within.find(Idx(particle)) != std::end(within); };

		// If the probe is active, find all the particles within the probe
		if (m_probe.m_active)
		{
			m_sim->m_spatial->Find(m_probe.m_position, m_probe.m_radius, m_sim->m_particles, [&](auto& particle, float)
			{
				within.insert(Idx(particle));
			});
		}

		// Update the colour from the spatial partitioning so we can see when it's wrong
		auto GetColour = [&](auto& particle)
		{
			if (m_probe.m_active && IsWithin(particle))
				return Colour32(0xFFFFFF00);

			auto relative_density = m_sim->m_densities[Idx(particle)] / m_sim->m_density0;
			Colour32 const colours[] = {Colour32(0xFFEE0000), Colour32(0xFF0055EE), Colour32(0xFF0000AA), Colour32(0xFFFFFFFF)};
		
			return Lerp(colours, Clamp(relative_density / _countof(colours), 0.0f, 1.0f));
		};

		{// Update the positions of the particles in the vertex buffer
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
		}

		{// Update the gradient
			UpdateSubresourceScope update = m_gfx_gradient.m_model->UpdateVertices();
			auto* ptr = update.ptr<Vert>();
			for (auto& particle : m_sim->m_particles)
			{
				ptr->m_vert = particle.m_pos;
				ptr->m_diff = Colour32Green;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;

				static float Scale = 1.1f;
				ptr->m_vert = particle.m_pos + Scale * m_sim->PressureAt(particle.m_pos, m_sim->m_particles.index(particle));
				ptr->m_diff = Colour32Green;
				ptr->m_norm = v4::Zero();
				ptr->m_tex0 = v2::Zero();
				ptr->pad = v2::Zero();
				++ptr;
			}
			update.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		}

		// Add the instance to the scene to be rendered
		scene.AddInstance(m_gfx_fluid);
		scene.AddInstance(m_gfx_container);
		scene.AddInstance(m_gfx_gradient);
		if (m_probe.m_active)
			scene.AddInstance(m_probe.m_gfx);
	}

	// Handle input
	void FluidVisualisation::OnMouseButton(gui::MouseEventArgs& args)
	{
		(void)args;
	}
	void FluidVisualisation::OnMouseMove(gui::MouseEventArgs& args)
	{
		m_probe.OnMouseMove(args, *m_scn);
		if (args.m_handled)
			return;

		
		if (args.ModifierKey(VK_CONTROL))
		{
			// Shoot a ray through the mouse pointer
			auto nss_point = m_scn->m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
			auto [pt, dir] = m_scn->m_cam.NSSPointToWSRay(v4(nss_point, 1, 0));

			if constexpr (Dimensions == 2)
			{
				// Find the intercept with the z = 0 plane
				auto t = -pt.z / dir.z;
				auto epicentre = pt + t * dir;

				static float radius = 0.4f;
				m_sim->m_spatial->Find(epicentre, radius, m_sim->m_particles, [&](auto& particle, float dist_sq)
				{
					auto dist = Sqrt(dist_sq);
					if (dist == 0.0f) return;

					auto dir = (particle.m_pos - epicentre) / dist;
					auto& part = m_sim->m_particles[m_sim->m_particles.index(particle)];
					part.m_vel += SmoothStep(10.0f, 0.0f, dist / radius) * dir;
				});
			}
			args.m_handled = true;
		}
	}
	void FluidVisualisation::OnMouseWheel(gui::MouseWheelArgs& args)
	{
		m_probe.OnMouseWheel(args);
		if (args.m_handled)
			return;
	}
	void FluidVisualisation::OnKey(gui::KeyEventArgs& args)
	{
		m_probe.OnKey(args);
		if (args.m_handled)
			return;

		if (args.m_down) return;
		switch (args.m_vk_key)
		{
			case VK_OEM_PLUS:
			{
				//m_sim.m_radius = Clamp(m_sim.m_radius * 1.1f, 0.01f, 1.0f);
				break;
			}
		}
	}
}