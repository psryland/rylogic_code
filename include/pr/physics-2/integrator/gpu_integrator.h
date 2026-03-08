//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
// Forward declaration for GpuIntegrator (pimpl pattern).
// The full definition lives in gpu_integrator.cpp and includes view3d-12 headers.
// This header is safe to include from any physics-2 public header.
#pragma once
#include <memory>
#include <span>
#include "pr/physics-2/integrator/rigid_body_dynamics.h"

struct ID3D12Device4; // Forward declare D3D12 device (avoids including d3d12.h)

namespace pr::physics
{
	// Opaque GPU integrator — performs Störmer-Verlet integration on the GPU via compute shader.
	// Created by the application and owned by the Engine via unique_ptr.
	struct GpuIntegrator;

	// Custom deleter for GpuIntegrator pimpl (calls through to gpu_integrator.cpp
	// where the type is complete).
	struct GpuIntegratorDeleter
	{
		void operator()(GpuIntegrator* p) const;
	};
	using GpuIntegratorPtr = std::unique_ptr<GpuIntegrator, GpuIntegratorDeleter>;

	// Create a GpuIntegrator instance. The device pointer can come from a Renderer or standalone Gpu.
	// 'max_bodies' determines the GPU buffer capacity.
	GpuIntegratorPtr CreateGpuIntegrator(ID3D12Device4* device, int max_bodies);

	// Get the D3D12 device owned by a GpuIntegrator. Used to share the device with
	// other GPU subsystems (e.g. GpuCollisionDetector) to avoid creating multiple devices.
	ID3D12Device4* GpuIntegratorDevice(GpuIntegrator& gpu);

	// Get the Gpu instance owned by a GpuIntegrator, for sharing the command queue.
	// Returns an opaque void* that can be cast to rdr12::Gpu* by code that includes view3d-12.
	void* GpuIntegratorGpuPtr(GpuIntegrator& gpu);

	// Run GPU integration on the dynamics buffer. This is the public API that engine.cpp calls.
	// Defined in gpu_integrator.cpp where GpuIntegrator is a complete type.
	void GpuIntegrate(GpuIntegrator& gpu, std::span<RigidBodyDynamics> dynamics, float dt, std::span<IntegrateOutput> output);
}
