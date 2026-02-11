//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Terrain mesh rendering for visible land (height > 0).
#pragma once
#include "src/forward.h"
#include "src/world/height_field.h"

namespace las
{
	struct Terrain
	{
		static constexpr int GridDim = 128;
		static constexpr float GridExtent = 500.0f;

		struct Instance
		{
			#define PR_RDR_INST(x)\
			x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
			x(ModelPtr , m_model, EInstComp::ModelPtr)
			PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
			#undef PR_RDR_INST
		};

		HeightField const* m_height_field;
		Instance m_inst;
		ResourceFactory m_factory;

		pr::rdr12::ModelGenerator::Buffers<Vert> m_cpu_data;
		bool m_dirty;

		explicit Terrain(Renderer& rdr, HeightField const& hf);

		// Simulation: recompute terrain vertices around the camera
		void Update(v4 camera_world_pos);

		// Rendering: upload dirty verts to GPU and add to scene
		void AddToScene(Scene& scene);

	private:

		static Colour TerrainColour(float height, float flatness);
		void BuildMesh();
	};
}