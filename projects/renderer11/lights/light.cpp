//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/lights/light.h"

using namespace pr;
using namespace pr::rdr;

pr::rdr::Light::Light()
:m_position        (v4Origin)
,m_direction       (v4::make(-0.577350f, -0.577350f, -0.577350f, 0.0f))
,m_type            (ELight::Directional)
,m_ambient         (Colour32::make(0.0f, 0.0f, 0.0f, 0.0f))
,m_diffuse         (Colour32::make(0.5f, 0.5f, 0.5f, 1.0f))
,m_specular        (Colour32::make(0.1f, 0.1f, 0.1f, 0.0f))
,m_specular_power  (1000.0f)
,m_inner_cos_angle (0.97f)
,m_outer_cos_angle (0.92f)
,m_range           (1000.0f)
,m_falloff         (0.0f)
,m_cast_shadows    (false)
,m_on              (true)
{}

// Return true if this light is in a valid state
bool pr::rdr::Light::IsValid() const
{
	switch (m_type)
	{
	default: return false;
	case ELight::Ambient:     return true;
	case ELight::Point:       return true;
	case ELight::Spot:        return !pr::IsZero3(m_direction);
	case ELight::Directional: return !pr::IsZero3(m_direction);
	}
}

// Returns a light to world transform appropriate for this light type and facing 'centre'
pr::m4x4 pr::rdr::Light::LightToWorld(v4 const& centre, float centre_dist) const
{
	switch (m_type)
	{
	default:                  return pr::m4x4Identity;
	case ELight::Directional: return pr::LookAt(centre - centre_dist*m_direction, centre, pr::Perpendicular(m_direction));
	case ELight::Point:       return pr::LookAt(m_position, centre, pr::Perpendicular(centre - m_position));
	case ELight::Spot:        return pr::LookAt(m_position, centre, pr::Perpendicular(centre - m_position));
	}
}

// Returns a projection transform appropriate for this light type
pr::m4x4 pr::rdr::Light::Projection(float centre_dist) const
{
	switch (m_type)
	{
	default:                  return pr::m4x4Identity;
	case ELight::Directional: return pr::ProjectionOrthographic(10.0f, 10.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
	case ELight::Point:       return pr::ProjectionPerspectiveFOV(pr::maths::tau_by_8, 1.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
	case ELight::Spot:        return pr::ProjectionPerspectiveFOV(pr::maths::tau_by_8, 1.0f, centre_dist * 0.01f, centre_dist * 100.0f, true);
	}
}

#define PR_ENUM(x)\
	x(Pos  ,= 0x0B5FC56C)\
	x(Dir  ,= 0x0A792773)\
	x(Type ,= 0x119E006A)\
	x(Amb  ,= 0x1CB1C5AE)\
	x(Diff ,= 0x13B95B1A)\
	x(Spec ,= 0x0184DD00)\
	x(SPwr ,= 0x104C3C82)\
	x(InCA ,= 0x04017F1B)\
	x(OtCA ,= 0x1398A506)\
	x(Rng  ,= 0x164CFC58)\
	x(FOff ,= 0x14C9C72F)\
	x(Shdw ,= 0x0FF86A40)\
	x(On   ,= 0x0E59DCC9)
PR_DEFINE_ENUM2(ELightKW, PR_ENUM);
#undef PR_ENUM

// Get/Set light settings
std::string pr::rdr::Light::Settings() const
{
	std::stringstream out;
	out << "  *" << ELightKW::Pos  << "{" << m_position << "}\n"
		<< "  *" << ELightKW::Dir  << "{" << m_direction << "}\n"
		<< "  *" << ELightKW::Type << "{" << m_type << "}\n"
		<< std::hex
		<< "  *" << ELightKW::Amb  << "{" << m_ambient.m_aarrggbb << "}\n"
		<< "  *" << ELightKW::Diff << "{" << m_diffuse.m_aarrggbb << "}\n"
		<< "  *" << ELightKW::Spec << "{" << m_specular.m_aarrggbb << "}\n"
		<< std::dec
		<< "  *" << ELightKW::SPwr << "{" << m_specular_power << "}\n"
		<< "  *" << ELightKW::InCA << "{" << m_inner_cos_angle << "}\n"
		<< "  *" << ELightKW::OtCA << "{" << m_outer_cos_angle << "}\n"
		<< "  *" << ELightKW::Rng  << "{" << m_range << "}\n"
		<< "  *" << ELightKW::FOff << "{" << m_falloff << "}\n"
		<< "  *" << ELightKW::Shdw << "{" << m_cast_shadows << "}\n"
		<< "  *" << ELightKW::On   << "{" << m_on << "}\n"
		;
	return out.str();
}
bool pr::rdr::Light::Settings(char const* settings)
{
	try
	{
		struct ErrorHandler :pr::script::IErrorHandler
		{
			void IErrorHandler_ImplementationVersion0() {}
			void IErrorHandler_ShowMessage(char const* str) {::MessageBoxA(::GetFocus(), str, "Light Settings Invalid", MB_OK);}
		} error_handler;

		// Parse the settings for light, if no errors are found update *this
		Light light;

		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings);
		reader.ErrorHandler() = &error_handler;
		reader.AddSource(src);

		// Check the hash values are correct
		PR_EXPAND(PR_DBG_RDR, static bool s_light_kws_checked = pr::CheckHashEnum<ELightKW>([&](char const* s) { return reader.HashKeyword(s); }));

		ELightKW kw;
		while (reader.NextKeywordH(kw))
		{
			switch (kw)
			{
			case ELightKW::Pos:  reader.ExtractVector3S(light.m_position, 1.0f); break;
			case ELightKW::Dir:  reader.ExtractVector3S(light.m_direction, 0.0f); break;
			case ELightKW::Type: reader.ExtractEnumS(light.m_type); break;
			case ELightKW::Amb:  reader.ExtractIntS(light.m_ambient.m_aarrggbb, 16); break;
			case ELightKW::Diff: reader.ExtractIntS(light.m_diffuse.m_aarrggbb, 16); break;
			case ELightKW::Spec: reader.ExtractIntS(light.m_specular.m_aarrggbb, 16); break;
			case ELightKW::SPwr: reader.ExtractRealS(light.m_specular_power); break;
			case ELightKW::InCA: reader.ExtractRealS(light.m_inner_cos_angle); break;
			case ELightKW::OtCA: reader.ExtractRealS(light.m_outer_cos_angle); break;
			case ELightKW::Rng:  reader.ExtractRealS(light.m_range); break;
			case ELightKW::FOff: reader.ExtractRealS(light.m_falloff); break;
			case ELightKW::Shdw: reader.ExtractBoolS(light.m_cast_shadows); break;
			case ELightKW::On:   reader.ExtractBoolS(light.m_on); break;
			}
		}
		*this = light;
		return true;
	}
	catch (pr::script::EResult) {}
	return false;
}
