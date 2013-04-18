//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
	
#include "renderer/utility/stdafx.h"
#include "pr/renderer/lighting/light.h"
	
pr::rdr::Light::Light()
{
	m_type            = ELight::Directional;
	m_on              = true;
	m_position        = v4Origin;
	m_direction       .set(-0.577350f, -0.577350f, -0.577350f, 0.0f);
	m_ambient         .set(0.0f, 0.0f, 0.0f, 0.0f);
	m_diffuse         .set(0.5f, 0.5f, 0.5f, 1.0f);
	m_specular        .set(0.1f, 0.1f, 0.1f, 0.0f);
	m_specular_power  = 1000.0f;
	m_inner_cos_angle = 0.97f;
	m_outer_cos_angle = 0.92f;
	m_range           = 1000.0f;
	m_falloff         = 0.0f;
	m_cast_shadows    = false;
}
	
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
pr::m4x4 pr::rdr::Light::LightToWorld(pr::v4 const& centre, float centre_dist) const
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
	
// Get/Set light settings
std::string pr::rdr::Light::Settings() const
{
	std::stringstream out;
	out << "  *On   {" << m_on << "}\n"
		<< "  *Pos  {" << m_position << "}\n"
		<< "  *Dir  {" << m_direction << "}\n"
		<< std::hex
		<< "  *Amb  {" << m_ambient.m_aarrggbb << "}\n"
		<< "  *Diff {" << m_diffuse.m_aarrggbb << "}\n"
		<< "  *Spec {" << m_specular.m_aarrggbb << "}\n"
		<< std::dec
		<< "  *SPwr {" << m_specular_power << "}\n"
		<< "  *InCA {" << m_inner_cos_angle << "}\n"
		<< "  *OtCA {" << m_outer_cos_angle << "}\n"
		<< "  *Rng  {" << m_range << "}\n"
		<< "  *FOff {" << m_falloff << "}\n"
		<< "  *Shdw {" << m_cast_shadows << "}\n";
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
		
		std::string kw;
		while (reader.NextKeywordS(kw))
		{
			if      (pr::str::EqualI(kw, "On"  )) { reader.ExtractBoolS(light.m_on); }
			else if (pr::str::EqualI(kw, "Pos" )) { reader.ExtractVector3S(light.m_position, 1.0f); }
			else if (pr::str::EqualI(kw, "Dir" )) { reader.ExtractVector3S(light.m_direction, 0.0f); }
			else if (pr::str::EqualI(kw, "Amb" )) { reader.ExtractIntS(light.m_ambient.m_aarrggbb, 16); }
			else if (pr::str::EqualI(kw, "Diff")) { reader.ExtractIntS(light.m_diffuse.m_aarrggbb, 16); }
			else if (pr::str::EqualI(kw, "Spec")) { reader.ExtractIntS(light.m_specular.m_aarrggbb, 16); }
			else if (pr::str::EqualI(kw, "SPwr")) { reader.ExtractRealS(light.m_specular_power); }
			else if (pr::str::EqualI(kw, "InCA")) { reader.ExtractRealS(light.m_inner_cos_angle); }
			else if (pr::str::EqualI(kw, "OtCA")) { reader.ExtractRealS(light.m_outer_cos_angle); }
			else if (pr::str::EqualI(kw, "Rng" )) { reader.ExtractRealS(light.m_range); }
			else if (pr::str::EqualI(kw, "FOff")) { reader.ExtractRealS(light.m_falloff); }
			else if (pr::str::EqualI(kw, "Shdw")) { reader.ExtractBoolS(light.m_cast_shadows); }
		}
		*this = light;
		return true;
	}
	catch (pr::script::EResult) {}
	return false;
}
	
