//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/lights/light.h"

namespace pr::rdr
{
	Light::Light()
		:m_position(v4Origin)
		,m_direction(-0.577350f, -0.577350f, -0.577350f, 0.0f)
		,m_type(ELight::Directional)
		,m_ambient(0.25f, 0.25f, 0.25f, 0.0f)
		,m_diffuse(0.25f, 0.25f, 0.25f, 1.0f)
		,m_specular(0.1f, 0.1f, 0.1f, 0.0f)
		,m_specular_power(1000.0f)
		,m_range(100.0f)
		,m_falloff(0.0f)
		,m_inner_angle(maths::tau_by_4f)
		,m_outer_angle(maths::tau_by_4f)
		,m_cast_shadow(0.0f)
		,m_cam_relative(false)
		,m_on(true)
	{}

	// Return true if this light is in a valid state
	bool Light::IsValid() const
	{
		switch (m_type)
		{
		default: return false;
		case ELight::Ambient:     return true;
		case ELight::Point:       return m_position.w == 1.0f;
		case ELight::Spot:        return m_direction != v4Zero;
		case ELight::Directional: return m_direction != v4Zero;
		}
	}

	// Returns a light to world transform appropriate for this light type and facing 'centre'
	m4x4 Light::LightToWorld(v4 const& centre, float centre_dist, m4x4 const& c2w) const
	{
		auto pos = m_cam_relative ? c2w * m_position : m_position;
		auto dir = m_cam_relative ? c2w * m_direction : m_direction;
		auto preferred_up = m_cam_relative ? c2w.y : v4YAxis;
		centre_dist = centre_dist != 0 ? centre_dist : 1.0f;
		switch (m_type)
		{
			case ELight::Directional: return m4x4::LookAt(centre - centre_dist * dir, centre, Perpendicular(dir, preferred_up));
			case ELight::Point:       return m4x4::LookAt(pos, centre, Perpendicular(centre - pos, preferred_up));
			case ELight::Spot:        return m4x4::LookAt(pos, centre, Perpendicular(centre - pos, preferred_up));
			default:                  return m4x4Identity;
		}
	}

	// Returns a projection transform appropriate for this light type
	m4x4 Light::Projection(float zn, float zf, float w, float h, float focus_dist) const
	{
		auto s = zn / focus_dist;
		switch (m_type)
		{
			case ELight::Directional: return m4x4::ProjectionOrthographic(w, h, zn, zf, true);
			case ELight::Point:       return m4x4::ProjectionPerspective(w * s, h * s, zn, zf, true);
			case ELight::Spot:        return m4x4::ProjectionPerspective(w * s, h * s, zn, zf, true);
			default:                  return m4x4Identity;
		}
	}
	m4x4 Light::ProjectionFOV(float zn, float zf, float aspect, float fovY, float focus_dist) const
	{
		auto height = 2.0f * focus_dist * tan(fovY * 0.5f);
		switch (m_type)
		{
			case ELight::Directional: return m4x4::ProjectionOrthographic(height * aspect, height, zn, zf, true);
			case ELight::Point:       return m4x4::ProjectionPerspectiveFOV(fovY, aspect, zn, zf, true);
			case ELight::Spot:        return m4x4::ProjectionPerspectiveFOV(fovY, aspect, zn, zf, true);
			default:                  return m4x4Identity;
		}
	}

	enum class ELightKW
	{
		#define PR_ENUM(x)\
		x(Pos  ,= pr::hash::HashICT(L"Pos" ))\
		x(Dir  ,= pr::hash::HashICT(L"Dir" ))\
		x(Type ,= pr::hash::HashICT(L"Type"))\
		x(Amb  ,= pr::hash::HashICT(L"Amb" ))\
		x(Diff ,= pr::hash::HashICT(L"Diff"))\
		x(Spec ,= pr::hash::HashICT(L"Spec"))\
		x(SPwr ,= pr::hash::HashICT(L"SPwr"))\
		x(Ang0 ,= pr::hash::HashICT(L"Ang0"))\
		x(Ang1 ,= pr::hash::HashICT(L"Ang1"))\
		x(Rng  ,= pr::hash::HashICT(L"Rng" ))\
		x(FOff ,= pr::hash::HashICT(L"FOff"))\
		x(Shdw ,= pr::hash::HashICT(L"Shdw"))\
		x(On   ,= pr::hash::HashICT(L"On"  ))\
		x(CRel ,= pr::hash::HashICT(L"CRel"))
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(ELightKW, PR_ENUM);
	#undef PR_ENUM

	// Get/Set light settings
	std::wstring Light::Settings() const
	{
		std::wstringstream out;
		out << "  *" << ELightKW::Pos  << "{" << m_position.xyz << "}\n"
			<< "  *" << ELightKW::Dir  << "{" << m_direction.xyz << "}\n"
			<< "  *" << ELightKW::Type << "{" << m_type << "}\n"
			<< std::hex
			<< "  *" << ELightKW::Amb  << "{" << m_ambient.argb << "}\n"
			<< "  *" << ELightKW::Diff << "{" << m_diffuse.argb << "}\n"
			<< "  *" << ELightKW::Spec << "{" << m_specular.argb << "}\n"
			<< std::dec
			<< "  *" << ELightKW::SPwr << "{" << m_specular_power << "}\n"
			<< "  *" << ELightKW::Ang0 << "{" << m_inner_angle << "}\n"
			<< "  *" << ELightKW::Ang1 << "{" << m_outer_angle << "}\n"
			<< "  *" << ELightKW::Rng  << "{" << m_range << "}\n"
			<< "  *" << ELightKW::FOff << "{" << m_falloff << "}\n"
			<< "  *" << ELightKW::Shdw << "{" << m_cast_shadow << "}\n"
			<< "  *" << ELightKW::On   << "{" << m_on << "}\n"
			<< "  *" << ELightKW::CRel << "{" << m_cam_relative << "}\n"
			;
		return out.str();
	}
	void Light::Settings(std::wstring_view settings)
	{
		using namespace pr::script;
		try
		{
			// Parse the settings for light, if no errors are found update *this
			Light light;

			// Parse the settings
			StringSrc src(settings);
			Reader reader(src, false);

			ELightKW kw;
			while (reader.NextKeywordH(kw))
			{
				switch (kw)
				{
				case ELightKW::Pos:  reader.Vector3S(light.m_position, 1.0f); break;
				case ELightKW::Dir:  reader.Vector3S(light.m_direction, 0.0f); break;
				case ELightKW::Type: reader.EnumS(light.m_type); break;
				case ELightKW::Amb:  reader.IntS(light.m_ambient.argb, 16); break;
				case ELightKW::Diff: reader.IntS(light.m_diffuse.argb, 16); break;
				case ELightKW::Spec: reader.IntS(light.m_specular.argb, 16); break;
				case ELightKW::SPwr: reader.RealS(light.m_specular_power); break;
				case ELightKW::Ang0: reader.RealS(light.m_inner_angle); break;
				case ELightKW::Ang1: reader.RealS(light.m_outer_angle); break;
				case ELightKW::Rng:  reader.RealS(light.m_range); break;
				case ELightKW::FOff: reader.RealS(light.m_falloff); break;
				case ELightKW::Shdw: reader.RealS(light.m_cast_shadow); break;
				case ELightKW::On:   reader.BoolS(light.m_on); break;
				case ELightKW::CRel: reader.BoolS(light.m_cam_relative); break;
				}
			}
			*this = light;
		}
		catch (ScriptException const& e)
		{
			throw std::invalid_argument(e.what());
		}
	}
}