//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/lights/light.h"

namespace pr
{
	namespace rdr
	{
		Light::Light()
			:m_position        (v4Origin)
			,m_direction       (-0.577350f, -0.577350f, -0.577350f, 0.0f)
			,m_type            (ELight::Directional)
			,m_ambient         (0.0f, 0.0f, 0.0f, 0.0f)
			,m_diffuse         (0.5f, 0.5f, 0.5f, 1.0f)
			,m_specular        (0.1f, 0.1f, 0.1f, 0.0f)
			,m_specular_power  (1000.0f)
			,m_range           (100.0f)
			,m_falloff         (0.0f)
			,m_inner_cos_angle (0.97f)
			,m_outer_cos_angle (0.92f)
			,m_cast_shadow     (0.0f)
			,m_on              (true)
			,m_cam_relative    (false)
		{}

		// Return true if this light is in a valid state
		bool Light::IsValid() const
		{
			switch (m_type)
			{
			default: return false;
			case ELight::Ambient:     return true;
			case ELight::Point:       return m_position.w == 1.0f;
			case ELight::Spot:        return !IsZero3(m_direction);
			case ELight::Directional: return !IsZero3(m_direction);
			}
		}

		// Returns a light to world transform appropriate for this light type and facing 'centre'
		m4x4 Light::LightToWorld(v4 const& centre, float centre_dist) const
		{
			switch (m_type)
			{
			default:                  return m4x4Identity;
			case ELight::Directional: return m4x4::LookAt(centre - centre_dist*m_direction, centre, Perpendicular(m_direction));
			case ELight::Point:       return m4x4::LookAt(m_position, centre, Perpendicular(centre - m_position));
			case ELight::Spot:        return m4x4::LookAt(m_position, centre, Perpendicular(centre - m_position));
			}
		}

		// Returns a projection transform appropriate for this light type
		m4x4 Light::Projection(float centre_dist) const
		{
			switch (m_type)
			{
			default:                  return m4x4Identity;
			case ELight::Directional: return m4x4::ProjectionOrthographic(10.0f, 10.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
			case ELight::Point:       return m4x4::ProjectionPerspectiveFOV(maths::tau_by_8, 1.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
			case ELight::Spot:        return m4x4::ProjectionPerspectiveFOV(maths::tau_by_8, 1.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
			}
		}

		#define PR_ENUM(x)\
			x(Pos  ,= pr::hash::HashICT(L"Pos" ))\
			x(Dir  ,= pr::hash::HashICT(L"Dir" ))\
			x(Type ,= pr::hash::HashICT(L"Type"))\
			x(Amb  ,= pr::hash::HashICT(L"Amb" ))\
			x(Diff ,= pr::hash::HashICT(L"Diff"))\
			x(Spec ,= pr::hash::HashICT(L"Spec"))\
			x(SPwr ,= pr::hash::HashICT(L"SPwr"))\
			x(InCA ,= pr::hash::HashICT(L"InCA"))\
			x(OtCA ,= pr::hash::HashICT(L"OtCA"))\
			x(Rng  ,= pr::hash::HashICT(L"Rng" ))\
			x(FOff ,= pr::hash::HashICT(L"FOff"))\
			x(Shdw ,= pr::hash::HashICT(L"Shdw"))\
			x(On   ,= pr::hash::HashICT(L"On"  ))\
			x(CRel ,= pr::hash::HashICT(L"CRel"))
		PR_DEFINE_ENUM2(ELightKW, PR_ENUM);
		#undef PR_ENUM

		// Check the hash values are correct and match the hash function that the script reader will use
		#if PR_DBG_RDR
		auto hash = [](wchar_t const* s) { return script::Reader::StaticHashKeyword(s, false); };
		static bool s_light_kws_checked = CheckHashEnum<ELightKW,wchar_t>(hash);
		#endif

		// Get/Set light settings
		std::string Light::Settings() const
		{
			std::stringstream out;
			out << "  *" << ELightKW::Pos  << "{" << m_position.xyz << "}\n"
				<< "  *" << ELightKW::Dir  << "{" << m_direction.xyz << "}\n"
				<< "  *" << ELightKW::Type << "{" << m_type << "}\n"
				<< std::hex
				<< "  *" << ELightKW::Amb  << "{" << m_ambient.argb << "}\n"
				<< "  *" << ELightKW::Diff << "{" << m_diffuse.argb << "}\n"
				<< "  *" << ELightKW::Spec << "{" << m_specular.argb << "}\n"
				<< std::dec
				<< "  *" << ELightKW::SPwr << "{" << m_specular_power << "}\n"
				<< "  *" << ELightKW::InCA << "{" << m_inner_cos_angle << "}\n"
				<< "  *" << ELightKW::OtCA << "{" << m_outer_cos_angle << "}\n"
				<< "  *" << ELightKW::Rng  << "{" << m_range << "}\n"
				<< "  *" << ELightKW::FOff << "{" << m_falloff << "}\n"
				<< "  *" << ELightKW::Shdw << "{" << m_cast_shadow << "}\n"
				<< "  *" << ELightKW::On   << "{" << m_on << "}\n"
				<< "  *" << ELightKW::CRel << "{" << m_cam_relative << "}\n"
				;
			return out.str();
		}
		void Light::Settings(char const* settings)
		{
			try
			{
				// Parse the settings for light, if no errors are found update *this
				Light light;

				// Parse the settings
				script::PtrA src(settings);
				script::Reader reader(src, false);

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
					case ELightKW::InCA: reader.RealS(light.m_inner_cos_angle); break;
					case ELightKW::OtCA: reader.RealS(light.m_outer_cos_angle); break;
					case ELightKW::Rng:  reader.RealS(light.m_range); break;
					case ELightKW::FOff: reader.RealS(light.m_falloff); break;
					case ELightKW::Shdw: reader.RealS(light.m_cast_shadow); break;
					case ELightKW::On:   reader.BoolS(light.m_on); break;
					case ELightKW::CRel: reader.BoolS(light.m_cam_relative); break;
					}
				}
				*this = light;
			}
			catch (script::Exception const& e)
			{
				throw Exception<HRESULT>(E_INVALIDARG, e.what());
			}
		}
	}
}