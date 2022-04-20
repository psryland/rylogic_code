//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/lighting/light.h"
#include "pr/view3d-12/shaders/shader.h"

namespace pr::rdr12
{
	// Per-Scene data for diagnostics
	struct DiagState
	{
		float     m_normal_lengths;     // The length of vertex normals
		Colour32  m_normal_colour;      // The colour of vertex normals
		bool      m_bboxes_visible;     // True if we should draw object bounding boxes
		ShaderPtr m_gs_fillmode_points; // The GS for point fill mode

		DiagState(Scene& scn);
	};

	// Enable/Disable normals on 'model'. Set length to 0 to disable
	void ShowNormals(Model* model, bool show);

	// Create a scale transform that positions a unit box at 'bbox'
	m4x4 BBoxTransform(BBox const& bbox);

	#if 0 //todo
	// Ldr Helper light graphics
	struct LdrLight :ldr::fluent::LdrBase<LdrLight>
	{
		LdrLight()
			:m_light()
			,m_scale(1.0f)
		{}

		// Set the light to represent
		LdrLight& light(rdr::Light const& light)
		{
			m_light = light;
			return *this;
		}
		rdr::Light m_light;

		// Scale the light graphics
		LdrLight& scale(float s)
		{
			m_scale = s;
			return *this;
		}
		float m_scale;

		/// <inheritdoc/>
		void ToString(std::string& str) const override
		{
			using namespace ldr::fluent;
			switch (m_light.m_type)
			{
				case rdr::ELight::Directional:
					LdrCylinder().modifiers(*this).hr(m_scale * 1.6f, m_scale * 0.4f, m_scale * 0.1f).ToString(str);
					break;
				case rdr::ELight::Spot:
					LdrCylinder().modifiers(*this).hr(m_scale * 1.6f, m_scale * 0.4f, m_scale * 0.1f).ToString(str);
					break;
				case rdr::ELight::Point:
					LdrSphere().modifiers(*this).r(m_scale * 0.3f).ToString(str);
					break;
				default:
					throw std::runtime_error("Unsupported light type");
			}
		}
	};
	#endif
}
namespace pr::ldr::fluent
{
	#if 0 //todo
	using LdrLight = rdr::LdrLight;
	#endif
}
