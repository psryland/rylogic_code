//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
// Forward declaration for GpuCollisionDetector (pimpl pattern).
// The full definition lives in gpu_collision_detector.cpp and includes view3d-12 headers.
// This header is safe to include from any physics-2 public header.
#pragma once
#include <memory>
#include <vector>
#include "pr/physics-2/collision/gpu_collision_types.h"

struct ID3D12Device4; // Forward declare D3D12 device

namespace pr::physics
{
	struct GpuIntegrator; // Forward declare (pimpl)

	// Opaque GPU collision detector — performs GJK+EPA collision detection on the GPU.
	// Created by the application and owned by the Engine via unique_ptr.
	struct GpuCollisionDetector;

	// Custom deleter for GpuCollisionDetector pimpl (calls through to .cpp
	// where the type is complete).
	struct GpuCollisionDetectorDeleter
	{
		void operator()(GpuCollisionDetector* p) const;
	};
	using GpuCollisionDetectorPtr = std::unique_ptr<GpuCollisionDetector, GpuCollisionDetectorDeleter>;

	// Create a GpuCollisionDetector instance that shares the D3D12 device and command queue
	// from an existing GpuIntegrator. This avoids creating a second Gpu instance which
	// causes D3D12 debug layer warnings (break-on-warning triggers SEH exceptions).
	GpuCollisionDetectorPtr CreateGpuCollisionDetector(GpuIntegrator& integrator, int max_pairs, int max_shapes, int max_verts);

	// Run GPU collision detection. Returns the contacts found.
	int GpuDetectCollisions(
		GpuCollisionDetector& detector,
		std::span<GpuShape const> shapes,
		std::span<GpuCollisionPair const> pairs,
		std::span<v4 const> verts,
		std::vector<GpuContact>& out_contacts);
}
