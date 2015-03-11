//***************************************************************************************************
// Ldr Gizmo
//  Copyright (c) Rylogic Ltd 2015
//***************************************************************************************************

#include <exception>
#include "pr/maths/geometryfunctions.h"
#include "pr/linedrawer/ldr_gizmo.h"

namespace pr
{
	namespace ldr
	{
		#pragma region Ldr Strings
		char const ldrstr_translate[] = R"(
#define Width 0.06
#define Length 1.0
#define TipRadius 0.15
#define TipLength 0.25

*Group TranslateGizmo
{
	*Sphere O FFFFFFFF { Width }
	*CylinderHR X FFFF0000
	{
		-1 #eval{Length - Width} Width
		*CylinderHR X FFFF0000 { -1 TipLength 0 TipRadius *o2w{*pos{#eval{Length/2} 0 0}} }
		*o2w{*pos{#eval{Length/2 - Width/2} 0 0}}
	}
	*CylinderHR Y FF00FF00
	{
		-2 #eval{Length - Width} Width
		*CylinderHR Y FF00FF00 { -2 TipLength 0 TipRadius *o2w{*pos{0 #eval{Length/2} 0}} }
		*o2w{*pos{0 #eval{Length/2 - Width/2} 0}}
	}
	*CylinderHR Z FF0000FF
	{
		-3 #eval{Length - Width} Width
		*CylinderHR Z FF0000FF { -3 TipLength 0 TipRadius *o2w{*pos{0 0 #eval{Length/2}}} }
		*o2w{*pos{0 0 #eval{Length/2 - Width/2}}}
	}
})";
		#pragma endregion

		Gizmo::Gizmo(pr::Camera& camera, pr::Renderer& rdr, ContextId ctx_id, EMode mode)
			:m_cam(&camera)
			,m_rdr(&rdr)
			,m_ctx_id(ctx_id)
			,m_mode(EMode::Disabled)
			,m_gfx()
			,m_ref_o2w()
			,m_ref_pt()
			,m_last_hit(EComponent::None)
			,m_component(EComponent::None)
			,m_manipulating(false)
			,m_moved(false)
		{
			Mode(mode);
		}

		// Get/Set the mode the gizmo is in
		Gizmo::EMode Gizmo::Mode() const
		{
			return m_mode;
		}
		void Gizmo::Mode(EMode mode)
		{
			if (m_mode == mode) return;
			m_mode = mode;

			// Create the gizmo graphics
			switch (m_mode)
			{
			default:
				{
					throw std::exception("Unknown gizmo mode");
				}
			case EMode::Disabled:
				{
					m_gfx = nullptr;
					break;
				}
			case EMode::Translate:
				{
					pr::ldr::ParseResult res;
					pr::ldr::ParseString(*m_rdr, ldrstr_translate, res, false, m_ctx_id);
					m_gfx = res.m_objects[0];
					break;
				}
			}

			//// Turn off the z buffer for the gizmo
			//m_gfx->Apply([](pr::ldr::LdrObject* obj)
			//{
			//	if (!obj->m_model) return true;
			//	obj->m_dsb.Set(pr::rdr::EDS::DepthEnable, FALSE);
			//	obj->m_sko.Alpha(true);
			//	return true;
			//});
		}

		// Get/Set the gizmo object to world transform
		pr::m4x4 const& Gizmo::O2W() const
		{
			if (!m_gfx) return pr::m4x4Identity;
			return m_gfx->m_o2p;
		}
		void Gizmo::O2W(pr::m4x4 const& o2w)
		{
			if (!m_gfx) return;
			m_gfx->m_o2p = o2w;
			m_moved = true;
		}

		// Returns the transform offset between the position when
		// manipulating started and the current gizmo position
		pr::m4x4 Gizmo::Offset() const
		{
			return pr::Invert(m_ref_o2w) * O2W();
		}

		// Interact with the gizmo based on mouse movement.
		// 'pos_ns' should be normalised. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
		// The start of a mouse movement is indicated by 'btn_state' being non-zero
		// The end of the mouse movement is indicated by 'btn_state' being zero
		// 'btn_state' is one of the MK_LBUTTON, MK_RBUTTON, values
		// 'ref_point' should be true on the mouse down/up event, false while dragging
		// Returns true if the input was used by the gizmo
		void Gizmo::MouseControl(pr::v2 const& pos_ns, int btn_state, bool ref_point)
		{
			m_moved = false;

			// Not visible, nothing to do
			if (m_gfx == nullptr)
				return;

			// On Lmouse down or up, start or stop manipulating
			if (ref_point)
			{
				// If left mouse down on an axis component, start manipulating
				if (m_component == EComponent::None && pr::AllSet(btn_state, MK_LBUTTON))
				{
					auto hit = HitTest(pos_ns);
					if (hit != EComponent::None)
					{
						m_ref_o2w = O2W();
						m_ref_pt = pos_ns;
						m_component = hit;
						m_manipulating = true;
					}
					return;
				}

				// If mouse up, end manipulating
				if (m_component != EComponent::None)
				{
					m_component = EComponent::None;
					m_manipulating = false;
					return;
				}
			}

			// If a manipulation is in progress, continue it
			else if (m_manipulating)
			{
				switch (m_mode)
				{
				case EMode::Disabled:  m_manipulating = false; break;
				case EMode::Translate: DoTranslation(pos_ns); break;
				case EMode::Rotate:    DoRotation(pos_ns); break;
				case EMode::Scale:     DoScale(pos_ns); break;
				}
				return;
			}

			// If we're not currently manipulating, check for mouse over the gizmo
			else if (btn_state == 0)
			{
				auto hit = HitTest(pos_ns);
				if (hit != m_last_hit)
				{
					m_last_hit = hit;

					// Reset all to origin colours
					m_gfx->ResetColour("");

					// Highlight the axis the mouse is over
					if (hit != EComponent::None)
						m_gfx->SetColour(pr::Colour32Yellow, 0xFFFFFFFFU, &"\0\0X\0Y\0Z\0"[int(hit)*2]);

					m_moved = true;
					return;
				}
			}

			return;
		}

		// Perform a hit test given a normalised screen-space point
		Gizmo::EComponent Gizmo::HitTest(pr::v2 const& pos_ns)
		{
			auto hit = EComponent::None;
			
			// Gizmo not visible? no hit
			if (m_gfx == nullptr)
				return hit;

			// Get the transform from world space to gizmo space (note, it might be scaled)
			auto w2o = pr::Invert(O2W());

			// Cast a ray into the view to get a line in world space
			// then transform the ray into gizmo space
			pr::v4 p, d;
			m_cam->WSRayFromNormSSPoint(pr::v4::make(pos_ns, 1.0f, 0.0f), p, d);
			p = w2o * p;
			d = w2o * d;

			// Test for intersection of the ray with the gizmo
			// Since the ray is in gizmo space, we're testing agains the X,Y,Z unit basis axes
			switch (m_mode)
			{
			default: throw std::exception("Unknown gizmo mode");
			case EMode::Disabled:
				{
					break;
				}
			case EMode::Translate:
				{
					float const threshold_sq = pr::Sqr(0.25f);
					float const t_min = 0.15f, t_max = 0.85f;
					float t0,t1,dist_sq;
					
					// Close to the X axis? Closest pt in the range [0.25,1] on the xaxis
					// and within the threshold distance. Also, on the positive side of the ray
					pr::ClosestPoint_LineSegmentToInfiniteLine(pr::v4Origin, pr::v4XAxis.w1(), p, d, t0, t1, dist_sq);
					if (t0 > t_min && t0 <= t_max && dist_sq < threshold_sq && t1 > 0.0f)
					{
						hit = EComponent::X;
						break;
					}
					pr::ClosestPoint_LineSegmentToInfiniteLine(pr::v4Origin, pr::v4YAxis.w1(), p, d, t0, t1, dist_sq);
					if (t0 > t_min && t0 <= t_max && dist_sq < threshold_sq && t1 > 0.0f)
					{
						hit = EComponent::Y;
						break;
					}
					pr::ClosestPoint_LineSegmentToInfiniteLine(pr::v4Origin, pr::v4ZAxis.w1(), p, d, t0, t1, dist_sq);
					if (t0 > t_min && t0 <= t_max && dist_sq < threshold_sq && t1 > 0.0f)
					{
						hit = EComponent::Z;
						break;
					}
					break;
				}
			}

			return hit;
		}

		// Perform translation
		void Gizmo::DoTranslation(pr::v2 const& pos_ns)
		{
			auto i = (int)m_component - 1;
			auto p = m_ref_o2w.pos;
			auto d = m_ref_o2w[i];

			// Project the component axis back into normalised screen space
			auto p0 = m_cam->NormSSPointFromWSPoint(p).xy;
			auto p1 = m_cam->NormSSPointFromWSPoint(p + d).xy;
			auto axis = p1 - p0;
			auto axis_lensq = pr::Length3Sq(axis);
			if (axis_lensq < pr::maths::tiny)
				return;

			// Compare the mouse movement to the on-screen component axis
			auto t = pr::Dot2(pos_ns - m_ref_pt, axis) / axis_lensq;

			// Translate the gizmo by t * the component axis
			auto o2w = O2W();
			o2w.pos = m_ref_o2w.pos + t * m_ref_o2w[i];
			O2W(o2w);
		}

		// Perform rotation
		void Gizmo::DoRotation(pr::v2 const& pos_ns)
		{
			(void)pos_ns;
		}

		// Perform scale
		void Gizmo::DoScale(pr::v2 const& pos_ns)
		{
			(void)pos_ns;
		}

	}
}
