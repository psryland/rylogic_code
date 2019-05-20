//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#pragma once
#include "p3d/src/forward.h"

// Generate normals for the p3d file
void GenerateVertNormals(pr::geometry::p3d::Mesh& mesh, float smoothing_angle, int verbosity);

// Generate normals for the p3d file
void GenerateVertNormals(pr::geometry::p3d::File& p3d, float smoothing_angle, int verbosity);
