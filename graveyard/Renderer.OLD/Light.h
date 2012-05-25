//*******************************************************************************************
//
//	A struct that encapsulates a single light
//
//*******************************************************************************************
//
#ifndef LIGHT_H
#define LIGHT_H

#include "PR/Geometry/PRColour.h"

namespace pr
{
	namespace rdr
	{
		struct Light
		{
			enum State { Off = 0, On = 1 };
			enum Type  { Ambient = 0, Point = D3DLIGHT_POINT, Spot = D3DLIGHT_SPOT, Directional = D3DLIGHT_DIRECTIONAL };
			Light()
			{
				m_type				= Ambient;
				m_state				= Off;
				m_position			= v4Origin;
				m_direction			= v4ZAxis;
				m_ambient			= ColourWhite;
				m_diffuse			= ColourWhite;
				m_specular			= ColourWhite;
				m_specular_power	= 1.0f;
				m_inner_angle		= 0.0f;
				m_outer_angle		= 0.0f;
				m_range				= 1000.0f;
				m_falloff			= 0.0f;
				m_attenuation0		= 1.0f;
				m_attenuation1		= 0.0f;
				m_attenuation2		= 0.0f;
			};
			State	GetState() const		{ return m_state; }
			void	SetState(State state)	{ m_state = state; }
			Type	GetType() const			{ return (m_state == On) ? (m_type) : (Ambient); }
			void	SetType(Type type)		{ (type == Ambient) ? (m_state = Off) : (m_state = On, m_type = type); }
			bool	IsValid() const;
			
			v4		m_position;
			v4		m_direction;
			Colour	m_ambient;
			Colour	m_diffuse;
			Colour	m_specular;
			float	m_specular_power;
			float	m_inner_angle;		// Theta
			float	m_outer_angle;		// Phi
			float	m_range;
			float	m_falloff;
			float	m_attenuation0;
			float	m_attenuation1;
			float	m_attenuation2;

		private:
			Type	m_type;
			State	m_state;
		};

		//*****
		// Return true if this light is valid
		inline bool Light::IsValid() const
		{
			if( m_state != Off && m_state != On ) return false;
			switch( m_type )
			{
			case Ambient:		return true;
			case Point:			return true;
			case Spot:			return true;
			case Directional:	return !m_direction.IsZero3();
			default:			return false;
			}
		}
	}//namespace rdr
}//namespace pr

#endif//LIGHT_H
