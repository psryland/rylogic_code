#pragma once

struct Prim
{
	// Note: Alignment matters. These are an array in 'm_collision' so need to be a multiple of 16 bytes.
	// Primitive data:
	//    Plane: data[0].xyz = normal, data[0].w = distance (positive if the normal faces the origin, expects normalised xyz)
	//   Sphere: data[0].xyz = center, data[0].w = radius
	// Triangle: data[0..2] = corners
	float4 data[3];

	// flag.x = primitive type
	uint4 flags;
};

static const uint Prim_Plane = 0;
static const uint Prim_Sphere = 1;
static const uint Prim_Triangle = 2;
