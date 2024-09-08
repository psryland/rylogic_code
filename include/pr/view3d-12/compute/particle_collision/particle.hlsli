//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// Defines:
//   POSITION_TYPE - Define the element structure of the 'positions' buffer. The field 'pos' is
//      expected to be the position information. It can be float3 or float4.
//   DYNAMICS_TYPE - Define the element structure of the 'dynamics' buffer.

#ifndef PARTICLE_HLSLI
#define PARTICLE_HLSLI

// Custom type for position data
#ifndef POSITION_TYPE
#define POSITION_TYPE \
struct PositionType \
{ \
	float4 pos; \
	float4 col; \
}
#endif
POSITION_TYPE;

// Custom type for dynamics data
#ifndef DYNAMICS_TYPE
#define DYNAMICS_TYPE \
struct DynamicsType \
{ \
	float3 accel; \
	float density; \
	float3 vel; \
	int flags; \
	float4 surface; \
}
#endif
DYNAMICS_TYPE;

// A struct that collects all the information about a particle
struct Particle
{
	float4 pos;
	float4 vel;
	float4 acc;
	float4 colour;
	float4 surface; // This is a plane equation of a nearby surface
	float density;
	int flags;
	int2 pad;
};

// The particle is in resting contact with a boundary
static const int ParticleFlag_Boundary = 1 << 0;

#endif
