// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	enum class EProbeMode
	{
		None,
		PushPull,
		SourceSink,
	};
	struct IProbeActions
	{
		virtual ~IProbeActions() = default;
		
		// Calculate the position for the probe based on the screen space point 'ss_pt'
		virtual v4 PositionProbe(gui::Point ss_pt) const = 0;

		// Push or pull the fluid
		virtual void PushPull(GpuJob& job, FluidSimulation::ProbeData const& data) = 0;

		// Add or remove count particles. Negative count removes particles.
		virtual void SourceSink(GpuJob& job, int count) = 0;
	};

	struct Probe
	{
		inline static constexpr float MaxRadius = 2.0f;
		inline static constexpr float MinRadius = 0.001f;

		v4 m_position;
		float m_radius;
		float m_sign;
		IProbeActions* m_actions;
		rdr12::LdrObjectPtr m_gfx;
		EProbeMode m_mode;
		float m_last_action_time;
		float m_time;

		Probe(rdr12::Renderer& rdr, float initial_radius, IProbeActions* actions = nullptr)
			: m_position(0, 0, 0, 1)
			, m_radius(initial_radius)
			, m_sign()
			, m_actions(actions)
			, m_gfx(rdr12::ldraw::Parse(rdr, "*Sphere probe { 1 }")[0])
			, m_mode(EProbeMode::None)
			, m_last_action_time()
			, m_time()
		{
			UpdateGfx();
		}

		// Return the probe data
		FluidSimulation::ProbeData Data() const
		{
			Tweakable<float, "ProbeForce"> ProbeForce = 1.0f;
			Tweakable<bool, "ShowWithinProbe"> ShowWithinProbe = true;
			return {
				.Position = m_position,
				.Radius = m_radius,
				.Force = m_sign * ProbeForce,
				.Highlight = ShowWithinProbe,
			};
		}

		// Reset the probe
		void Reset()
		{
			m_last_action_time = 0;
			m_time = 0;
		}

		// Perform probe actions
		void Step(GpuJob& job, float elapsed_s)
		{
			m_time += elapsed_s;
			switch (m_mode)
			{
				case EProbeMode::None:
				{
					break;
				}
				case EProbeMode::PushPull:
				{
					//  Push or pull the fluid
					if (m_sign != 0)
					{
						m_actions->PushPull(job, Data());
					}
					break;
				}
				case EProbeMode::SourceSink:
				{
					// Emit or absorb particles
					if (m_sign != 0)
					{
						// Determine the number of particles to be added or removed
						Tweakable<float, "ProbeFlowRate"> ProbeFlowRate = 1.0f; // per second
						auto count = static_cast<int>(ProbeFlowRate * (m_time - m_last_action_time));
						if (count > 0)
						{
							m_last_action_time += count / ProbeFlowRate;
							m_actions->SourceSink(job, static_cast<int>(m_sign) * count);
						}
					}
					break;
				}
			}
		}

		// Add the probe to the scene
		void AddToScene(rdr12::Scene& scene) const
		{
			if (m_mode == EProbeMode::None)
				return;

			m_gfx->Colour(
				m_mode == EProbeMode::PushPull ? 0x4000FF00 :
				m_mode == EProbeMode::SourceSink ? 0x40FF0000 :
				0x40FFFFFF, 0xFFFFFFFF);

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
			if (m_mode == EProbeMode::None || args.m_handled || m_actions == nullptr)
				return;

			args.m_handled = true;

			m_position = m_actions->PositionProbe(args.point_px());
			UpdateGfx();

			m_sign =
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Left) ? +1.0f :
				args.m_down && gui::AllSet(args.m_button, gui::EMouseKey::Right) ? -1.0f :
				0.0f;
			m_last_action_time = m_time;

		}
		void OnMouseMove(gui::MouseEventArgs& args)
		{
			if (m_mode == EProbeMode::None || args.m_handled || m_actions == nullptr)
				return;

			args.m_handled = true;

			m_position = m_actions->PositionProbe(args.point_px());
			UpdateGfx();
		}
		void OnMouseWheel(gui::MouseWheelArgs& args)
		{
			if (m_mode == EProbeMode::None || args.m_handled)
				return;

			args.m_handled = true;

			int delta = Clamp<short>(args.m_delta, -999, 999);
			m_radius *= (1.0f - delta * 0.001f);
			m_radius = Clamp(m_radius, MinRadius, MaxRadius);
			UpdateGfx();
		}
		void OnKey(gui::KeyEventArgs& args)
		{
			if (args.m_handled || !args.m_down || m_actions == nullptr)
				return;

			args.m_handled = true;

			switch (args.m_vk_key)
			{
				case '1':
				{
					m_mode = EProbeMode::None;
					m_position = m_actions->PositionProbe(args.point_px());
					m_sign = 0.0f;
					break;
				}
				case '2':
				{
					m_mode = m_mode != EProbeMode::PushPull ? EProbeMode::PushPull : EProbeMode::None;
					m_position = m_actions->PositionProbe(args.point_px());
					m_sign = 0.0f;
					break;
				}
				case '3':
				{
					m_mode = m_mode != EProbeMode::SourceSink ? EProbeMode::SourceSink : EProbeMode::None;
					m_position = m_actions->PositionProbe(args.point_px());
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