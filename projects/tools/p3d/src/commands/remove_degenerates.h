//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#pragma once
#include "src/forward.h"

// Remove degenerate verts
void RemoveDegenerateVerts(pr::geometry::p3d::Mesh& mesh, int quantisation = 10, float smoothing_angle = -1.0f, float colour_distance = -1.0f, float uv_distance = 1.0f, int verbosity = 2);

// Remove degenerate verts
void RemoveDegenerateVerts(pr::geometry::p3d::File& p3d, int quantisation = 10, float smoothing_angle = -1.0f, float colour_distance = -1.0f, float uv_distance = 1.0f, int verbosity = 2);