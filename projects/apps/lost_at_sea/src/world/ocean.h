//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Gerstner wave ocean simulation.
// CPU-side wave displacement for physics queries + mesh generation for rendering.
#pragma once
#include "src/forward.h"

namespace las
{
	// Parameters for a single Gerstner wave component
	struct GerstnerWave
	{
		v4 m_direction;     // Normalised wave travel direction (XY plane, Z=0, w=0)
		float m_amplitude;  // Wave height (peak to mean), in metres
		float m_wavelength; // Distance between crests, in metres
		float m_speed;      // Phase speed, in m/s
		float m_steepness;  // Gerstner steepness Q [0..1], controls sharpness of peaks

		float Frequency() const;
		float WaveNumber() const;
	};

	// Ocean simulation and rendering
	struct Ocean
	{
		// Radial mesh parameters. Rings are spaced logarithmically so that triangles
		// appear roughly the same size on screen regardless of distance from camera.
		static constexpr int NumRings = 80;       // Number of concentric rings
		static constexpr int NumSegments = 128;    // Vertices per ring (around 360°)
		static constexpr float InnerRadius = 2.0f; // Radius of the innermost ring (metres)
		static constexpr float OuterRadius = 1000.0f; // Radius of the outermost ring (metres)
		static constexpr float WaterDensity = 1025.0f; // kg/m^3 (seawater)

		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		std::vector<GerstnerWave> m_waves;
		Instance m_inst;

		// CPU-side vertex data (simulation writes here, render reads from here)
		pr::rdr12::ModelGenerator::Buffers<Vert> m_cpu_data;
		v4 m_grid_origin;  // World-space position of the grid centre
		bool m_dirty; // True when CPU verts have been updated but not yet uploaded to GPU

		explicit Ocean(Renderer& rdr);

		// Physics queries (read-only, no rendering side effects)
		float HeightAt(float world_x, float world_y, float time) const;

		// Query the displaced position of the ocean surface at a world position and time, including horizontal displacement from the Gerstner wave formula.
		v4 DisplacedPosition(float world_x, float world_y, float time) const;

		// Query the normal of the ocean surface at a world position and time, including contributions from all wave components.
		v4 NormalAt(float world_x, float world_y, float time) const;

		// Simulation step: recompute CPU vertex positions for the current time and camera position.
		// This only modifies simulation state (m_cpu_verts), not GPU resources.
		void Update(float time, v4 camera_world_pos);

		// Rendering: upload dirty verts to GPU and add to the scene.
		void AddToScene(Scene& scene, v4 camera_world_pos, GfxCmdList& cmd_list, GpuUploadBuffer& upload);

	private:

		// Initialise the ocean with a set of default wave components. These are arbitrary values that look good, but could be tweaked or made user-configurable.
		void InitDefaultWaves();

		// Create the ocean mesh as a flat grid, with vertex positions and normals to be displaced by the Gerstner wave formula in the shader.
		void BuildMesh(Renderer& rdr);
	};
}