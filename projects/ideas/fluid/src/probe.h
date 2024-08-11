// Fluid
#pragma once
#include "src/forward.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	struct Probe
	{
		v4 m_position;
		rdr12::LdrObjectPtr m_gfx;
		IndexSet m_found;
		float m_radius;
		float m_sign;
		bool m_active;

		explicit Probe(rdr12::Renderer& rdr)
			: m_position(0,0,0,1)
			, m_gfx(rdr12::CreateLdr(rdr, "*Sphere probe 40FF0000 { 1 }"))
			, m_found()
			, m_radius(0.1f)
			, m_sign()
			, m_active(false)
		{
			UpdateI2W();
		}

		// Add the probe to the scene
		void AddToScene(Scene& scene)
		{
			if (!m_active)
				return;

			// Add the probe graphics
			scene.AddInstance(m_gfx);
		}

		// Update the graphics position
		void UpdateI2W()
		{
			m_gfx->m_o2p = m4x4::Scale(m_radius, m_position);
		}

		//// Returns the acceleration Apply external forces to the particles
		//v4 ForceAt(FluidSimulation&, v4_cref position, std::optional<size_t>) const
		//{
		//	if (!m_active)
		//		return v4::Zero();

		//	auto dir = position - m_position;
		//	auto dist_sq = LengthSq(dir);
		//	if (dist_sq < maths::tinyf || dist_sq > m_radius * m_radius)
		//		return v4::Zero();

		//	Tweakable<float, "PushForce"> PushForce = 100.0f;
		//	auto dist = Sqrt(dist_sq);
		//	auto frac = SmoothStep<float>(1.0f, 0.0f, dist / m_radius);
		//	return (m_sign * frac * PushForce / dist) * dir;
		//}

		// Set the probe position from a SS point
		void SetPosition(gui::Point ss_pt, rdr12::Scene& scn)
		{
			// Shoot a ray through the mouse pointer
			auto nss_point = scn.m_viewport.SSPointToNSSPoint(To<v2>(ss_pt));
			auto [pt, dir] = scn.m_cam.NSSPointToWSRay(v4(nss_point, 1, 0));

			// Find where it intersects the XY plane at z = m_position.z
			auto t = (m_position.z - pt.z) / dir.z;
			m_position = pt + t * dir;
			UpdateI2W();
		}

		// Handle input
		void OnMouseButton(gui::MouseEventArgs& args, rdr12::Scene& scn)
		{
			if (!m_active || args.m_handled)
				return;

			m_sign = 
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Left) ? +1.0f :
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Right) ? -1.0f :
				0.0f;

			SetPosition(args.point_px(), scn);
			args.m_handled = true;
		}
		void OnMouseMove(gui::MouseEventArgs& args, rdr12::Scene& scn)
		{
			if (!m_active || args.m_handled)
				return;

			SetPosition(args.point_px(), scn);
			args.m_handled = true;
		}
		void OnMouseWheel(gui::MouseWheelArgs& args, rdr12::Scene&)
		{
			if (!m_active || args.m_handled)
				return;

			m_radius = Clamp(m_radius + args.m_delta * 0.0001f, 0.001f, 0.500f);
			UpdateI2W();
			args.m_handled = true;
		}
		void OnKey(gui::KeyEventArgs& args, rdr12::Scene& scn)
		{
			if (args.m_handled)
				return;

			if (!args.m_down)
				return;

			args.m_handled = true;
			const float step = 0.05f;
			switch (args.m_vk_key)
			{
				case 'W': { m_position.z += step; break; }
				case 'A': { m_position.x -= step; break; }
				case 'S': { m_position.z -= step; break; }
				case 'D': { m_position.x += step; break; }
				case 'Q': { m_position.y -= step; break; }
				case 'E': { m_position.y += step; break; }
				case 'R': { m_radius = std::min(m_radius * 1.1f, 0.100f); break; }
				case 'F': { m_radius = std::max(m_radius * 0.9f, 0.001f); break; }
				case VK_SHIFT:
				{
					SetPosition(args.point_px(), scn);
					m_active = !m_active;
					m_sign = 0.0f;
					break;
				}
				default:
				{
					args.m_handled = false;
					break;
				}
			}
			UpdateI2W();
		}
	};
}