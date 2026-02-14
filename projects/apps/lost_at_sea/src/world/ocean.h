//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Gerstner wave ocean simulation.
// GPU vertex shader handles wave displacement. CPU-side queries for physics.
#pragma once
#include "src/forward.h"

namespace las
{
	struct OceanShader;

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
		OceanShader* m_shader; // Owned by the nugget's RefPtr; raw pointer for convenient access

		explicit Ocean(Renderer& rdr);

		// Physics queries (read-only, no rendering side effects)
		float HeightAt(float world_x, float world_y, float time) const;
		v4 DisplacedPosition(float world_x, float world_y, float time) const;
		v4 NormalAt(float world_x, float world_y, float time) const;

		// Rendering: update shader constants and add to the scene.
		void AddToScene(Scene& scene, v4 camera_world_pos, float time);

	private:

		void InitDefaultWaves();
		void BuildMesh(Renderer& rdr);
	};
}