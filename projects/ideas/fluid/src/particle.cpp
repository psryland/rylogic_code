// Fluid
#include "src/particle.h"

namespace pr::fluid
{
	// The influence at 'distance' from a particle
	float Particle::InfluenceAt(float distance, float radius)
	{
		// Influence is the contribution to a property that a particle has at a given distance. The range of this contribution is controlled
		// by 'radius', which is the smoothing kernel radius. A property at a given point is calculated by taking the sum of that property for
		// all particles, weighted by their distance from the given point. If we limit the influence to a given radius, then we don't need to
		// consider all particles when measuring a property.
		// 
		// As 'radius' increases, more particles contribute to the measurement of the property. This means the weights need to reduce.
		// Consider a uniform grid of particles. A measured property (e.g. density) should be constant regardless of the value of 'radius'.
		// To make the weights independent of radius, we need to normalise them, i.e. divide by the total weight, which is the volume (in 2D)
		// under the influence curve (in 3D, it's a hyper volume).
		// 
		// If the smoothing curve is: P(r) = (R - |r|)^2
		// Then the volume under the curve is found from the double integral (in polar coordinates)
		// 2D:
		//   The volume under the curve is found from the double integral (in polar coordinates)
		//   (To understand where the extra 'r' comes from: https://youtu.be/PeeC_rWbios. Basically the delta area is r * dr * dtheta)
		//   $$\int_{0}_{tau} \int_{0}_{R} P(r) r dr dtheta$$
		//   auto volume = (1.0f / 12.0f) * maths::tauf * Pow(radius, 4.0f);
		// 3D:
		//   The volume under the curve is found from the triple integral (in spherical coordinates)
		//   $$\int_{0}_{tau} \int_{0}_{pi} \int_{0}_{R} P(r) r^2 sin(theta) dr dtheta dphi$$
		//   auto volume = (1.0f / 15.0f) * maths::tauf * Pow(radius, 5.0f);
		//
		// In reality, it doesn't matter what the volume is, as long as it scales correctly with 'radius' (i.e. R^4 for 2D, R^5 for 3D).
		// So, start with a uniform grid and a known property (e.g. density @ 1g/cm^3) and a radius that ensures a typical number of particles
		// influence each point. Then measure the combined influence, and use that value to rescale.

		if (distance >= radius)
			return 0.0f;

		if constexpr (Dimensions == 2)
		{
			float const C = 0.95f * (1.0f / 4.0f);
			return C * Sqr(radius - distance) / Sqr(Sqr(radius)); //Pow(radius, 4.0f);
		}
		if constexpr (Dimensions == 3)
		{
			float const C = 0.00242f;
			return C * Sqr(radius - distance) / (Sqr(Sqr(radius))*radius); //Pow(radius, 5.0f);
		}
	}
	
	// The gradient of the influence at 'distance' from a particle
	float Particle::dInfluenceAt(float distance, float radius)
	{
		if (distance >= radius)
			return 0.0f;

		if constexpr (Dimensions == 2)
		{
			float const C = 0.00242f;
			return 2 * C * (radius - distance) / Sqr(Sqr(radius)); //Pow(radius, 4.0f);
		}
		if constexpr (Dimensions == 3)
		{
			float const C = 0.00242f;
			return 2 * C * (radius - distance) / (Sqr(Sqr(radius))*radius); //Pow(radius, 5.0f);
		}
	}

}
