// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct Probe
	{
		v4 m_position;
		rdr12::LdrObjectPtr m_gfx;
		float m_radius;
		bool m_active;

		explicit Probe(rdr12::Renderer& rdr)
			:m_position(0,0,0,1)
			,m_gfx(rdr12::CreateLdr(rdr, "*Sphere probe 40FF0000 { 1 }"))
			,m_radius(0.05f)
			,m_active(false)
		{
			UpdateI2W();
		}

		// Handle input
		void OnMouseMove(gui::MouseEventArgs& args, rdr12::Scene& scn)
		{
			if (!gui::AllSet(args.m_keystate, gui::EMouseKey::Ctrl))
				return;

			// Shoot a ray through the mouse pointer
			auto nss_point = scn.m_viewport.SSPointToNSSPoint(To<v2>(args.m_point));
			auto [pt, dir] = scn.m_cam.NSSPointToWSRay(v4(nss_point, 1, 0));

			// Find where it intersect the plane at y = m_position.y
			auto t = (m_position.y - pt.y) / dir.y;
			m_position = pt + t * dir;
			UpdateI2W();
			args.m_handled = true;
		}
		void OnMouseWheel(gui::MouseWheelArgs& args)
		{
			if (!gui::AllSet(args.m_keystate, gui::EMouseKey::Ctrl))
				return;

			m_radius = Clamp(m_radius + args.m_delta * 0.0001f, 0.001f, 0.100f);
			UpdateI2W();
			args.m_handled = true;
		}
		void OnKey(gui::KeyEventArgs& args)
		{
			const float step = 0.05f;
			if (args.m_down) return;
			if (args.m_vk_key == 'P')
			{
				m_active = !m_active;
				return;
			}
			if (!m_active)
			{
				return;
			}
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
			}
			UpdateI2W();
		}
		void UpdateI2W()
		{
			m_gfx->m_i2w = m4x4::Scale(m_radius, m_position);
		}
	};
}