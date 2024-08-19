// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct Probe
	{
		v4 m_position;
		float m_radius;
		float m_sign;
		std::function<v4(gui::Point)> m_update;
		rdr12::LdrObjectPtr m_gfx;
		bool m_active;

		explicit Probe(rdr12::Renderer& rdr, std::function<v4(gui::Point)> update_cb)
			: m_position(0,0,0,1)
			, m_radius(0.1f)
			, m_sign()
			, m_update(update_cb)
			, m_gfx(rdr12::CreateLdr(rdr, "*Sphere probe 40FF0000 { 1 }"))
			, m_active(false)
		{
			UpdateGfx();
		}

		// Add the probe to the scene
		void AddToScene(rdr12::Scene& scene) const
		{
			if (!m_active)
				return;

			// Add the probe graphics
			scene.AddInstance(m_gfx);
		}

		// Update the graphics position
		void UpdateGfx()
		{
			m_gfx->m_o2p = m4x4::Scale(m_radius, m_position);
		}

		// Handle input
		void OnMouseButton(gui::MouseEventArgs& args)
		{
			if (!m_active || args.m_handled)
				return;

			m_sign = 
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Left) ? +1.0f :
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Right) ? -1.0f :
				0.0f;

			m_position = m_update(args.point_px());
			args.m_handled = true;
			UpdateGfx();
		}
		void OnMouseMove(gui::MouseEventArgs& args)
		{
			if (!m_active || args.m_handled)
				return;

			m_position = m_update(args.point_px());
			args.m_handled = true;
			UpdateGfx();
		}
		void OnMouseWheel(gui::MouseWheelArgs& args)
		{
			if (!m_active || args.m_handled)
				return;

			m_radius = Clamp(m_radius + args.m_delta * 0.0001f, 0.001f, 0.500f);
			UpdateGfx();
			args.m_handled = true;
		}
		void OnKey(gui::KeyEventArgs& args)
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
					m_position = m_update(args.point_px());
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
			UpdateGfx();
		}
	};
}