#pragma once
#include "src/forward.h"
#include "src/idemo_scene.h"

namespace pr::fluid
{
	struct Scene2d : IDemoScene
	{
		enum class EFillStyle
		{
			Point,
			Random,
			Lattice,
			Grid,
		};

		std::vector<Particle> m_particles;
		CollisionBuilder m_col;
		ldr::Builder m_ldr;

		explicit Scene2d(int particle_count)
			: m_col()
			, m_ldr()
			, m_particles(ParticleInitData(EFillStyle::Random, particle_count))
		{
			// Floor
			m_ldr.Plane("floor", 0xFFade3ff).wh({ 2, 0.5f }).o2w(m3x4::Rotation(AxisId::PosZ, AxisId::PosY), v4{ 0, -1, 0, 1 });
			m_col.Plane().o2w(m3x4::Rotation(AxisId::PosZ, AxisId::PosY), v4{ 0, -1, 0, 1 });

			// Ceiling
			m_ldr.Plane("ceiling", 0xFFade3ff).wh({ 2, 0.5f }).o2w(m3x4::Rotation(AxisId::PosZ, AxisId::NegY), v4{ 0, +1, 0, 1 });
			m_col.Plane().o2w(m3x4::Rotation(AxisId::PosZ, AxisId::NegY), v4{ 0, +1, 0, 1 });

			// Left Wall
			m_ldr.Plane("left_wall", 0xFFade3ff).wh({ 0.5f, 2 }).o2w(m3x4::Rotation(AxisId::PosZ, AxisId::PosX), v4{ -1, 0, 0, 1 });
			m_col.Plane().o2w(m3x4::Rotation(AxisId::PosZ, AxisId::PosX), v4{ -1, 0, 0, 1 });

			// Right Wall
			m_ldr.Plane("right_wall", 0xFFade3ff).wh({ 0.5f, 2 }).o2w(m3x4::Rotation(AxisId::PosZ, AxisId::NegX), v4{ +1, 0, 0, 1 });
			m_col.Plane().o2w(m3x4::Rotation(AxisId::PosZ, AxisId::NegX), v4{ +1, 0, 0, 1 });

			m_ldr.WrapAsGroup();
		}

		// Initial camera position
		std::optional<pr::Camera> Camera() const override
		{
			pr::Camera cam;
			if constexpr (Dimensions == 2)
				cam.LookAt(v4(0.0f, 0.0f, 2.8f, 1), v4(0, 0.0f, 0, 1), v4(0, 1, 0, 0));
			if constexpr (Dimensions == 3)
				cam.LookAt(v4(0.2f, 0.5f, 0.2f, 1), v4(0, 0.0f, 0, 1), v4(0, 1, 0, 0));
			cam.Align(v4::YAxis());
			return cam;
		}

		// Return the visualisation scene
		std::string LdrScene() const override
		{
			return m_ldr.ToString();
		}

		// Returns initialisation data for the particles.
		std::span<Particle const> Particles() const override
		{
			return m_particles;
		}

		// Return the collision
		std::span<CollisionPrim const> Collision() const override
		{
			return m_col.Primitives();
		}
		
		// Create particles
		static std::vector<Particle> ParticleInitData(EFillStyle style, int count)
		{
			std::vector<Particle> particles;
			particles.reserve(count);
			auto points = [&](v4 p, v4 v)
			{
				assert(p.w == 1 && v.w == 0);
				particles.push_back(Particle{ .pos = p, .col = v4::One(), .vel = v, .acc = {}, .mass = 1.0f });
			};

			const float hwidth = 1.0f;
			const float hheight = 0.5f;

			switch (style)
			{
				case EFillStyle::Point:
				{
					for (int i = 0; i != count; ++i)
						points(v4(-0.9f, 0, 0, 1), v4(-0.1f, -0.1f, 0, 0));

					break;
				}
				case EFillStyle::Random:
				{
					auto const margin = 0.95f;
					auto hw = hwidth * margin;
					auto hh = hheight * margin;
					auto vx = 0.2f;

					// Uniform distribution over the volume
					std::default_random_engine rng;
					for (int i = 0; i != count; ++i)
					{
						auto pos = v3::Random(rng, v3(-hw, -hh, -hw), v3(+hw, +hh, +hw)).w1();
						auto vel = v3::Random(rng, v3(-vx, -vx, -vx), v3(+vx, +vx, +vx)).w0();
						if constexpr (Dimensions == 2) { pos.z = 0; vel.z = 0; }
						points(pos, vel);
					}
					break;
				}
				case EFillStyle::Lattice:
				{
					auto const margin = 0.95f;
					auto hw = hwidth * margin;
					auto hh = hheight * margin;

					if constexpr (Dimensions == 2)
					{
						// Want to spread N particles evenly over the volume.
						// Area is 2*hwidth * 2*hheight
						// Want to find 'step' such that:
						//   (2*hwidth / step) * (2*hheight / step) = N
						// => step = sqrt((2*hwidth * 2*hheight) / N)
						auto step = Sqrt((2 * hw * 2 * hh) / count);

						auto x = -hw + step / 2;
						auto y = -hh + step / 2;
						for (int i = 0; i != count; ++i)
						{
							points(v4(x, y, 0, 1), v4::Zero());

							x += step;
							if (x > hw) { x = -hw + step / 2; y += step; }
						}
					}
					if constexpr (Dimensions == 3)
					{
						// Want to spread N particles evenly over the volume.
						// Volume is 2*hwidth * 2*hwidth * 2*hheight
						// Want to find 'step' such that:
						//  (2*hwidth/step) * (2*hwidth/step) * (2*hheight/step) = N
						// => step = cubert((2*hwidth * 2*hwidth * 2*hheight) / N)
						auto step = Cubert((2 * hw * 2 * hh * 2 * hw) / count);

						auto x = -hw + step / 2;
						auto y = -hh + step / 2;
						auto z = -hw + step / 2;
						for (int i = 0; i != count; ++i)
						{
							points(v4(x, y, z, 1), v4::Zero());

							x += step;
							if (x > hw) { x = -hw + step / 2; z += step; }
							if (z > hw) { z = -hw + step / 2; y += step; }
						}
					}
					break;
				}
				case EFillStyle::Grid:
				{
					auto const margin = 1.0f;//0.95f;
					auto hw = hwidth * margin;
					auto hh = hheight * margin;
					auto step = 0.1f;

					if constexpr (Dimensions == 2)
					{
						auto x = -hw + step / 2.0f;
						auto y = -hh + step / 2.0f;
						for (int i = 0; i != count; ++i)
						{
							points(v4(x, y, 0, 1), v4::Zero());

							x += step;
							if (x > hw) { x = -hw + step / 2; y += step; }
						}
					}
					break;
				}
			}

			return particles;
		}
	};
}
