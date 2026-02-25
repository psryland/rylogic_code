//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// CDLOD (Continuous Distance-Dependent LOD) terrain system.
// World-axis-aligned grid patches at multiple LOD levels eliminate
// vertex swimming. Geomorphing in the VS provides seamless transitions.
#pragma once
#include "src/forward.h"

namespace las
{
	namespace cdlod
	{
		static constexpr int GridN = 64;                              // Subdivisions per patch edge
		static constexpr int GridVerts = GridN + 1;                   // Vertices per edge = 65
		static constexpr int GridVertCount = GridVerts * GridVerts;   // 4225
		static constexpr int GridIdxCount = GridN * GridN * 6;        // 24576
		static constexpr int SkirtVertCount = 4 * GridVerts;          // 260 (one strip per edge)
		static constexpr int SkirtIdxCount = 4 * GridN * 6;           // 1536 (quads = 2 tris each)
		static constexpr int TotalVertCount = GridVertCount + SkirtVertCount; // 4485
		static constexpr int TotalIdxCount = GridIdxCount + SkirtIdxCount;    // 26112
		static constexpr float MinPatchSize = 16.0f;                  // Finest patch size (0.25m cells)
		static constexpr float MaxDrawDist = 5000.0f;                 // Maximum draw distance
		static constexpr int MaxPatches = 512;                        // Max visible patches per frame
		static constexpr float SubdivFactor = 2.0f;                    // Subdivide when cam dist < size * factor
	}

	// A visible terrain patch selected by the CDLOD quadtree
	struct PatchInfo
	{
		float origin_x; // World-space origin X
		float origin_y; // World-space origin Y
		float size;      // Patch size in metres (= MinPatchSize * 2^lod_level)
	};

	// Quadtree LOD selection for CDLOD terrain
	struct CDLODSelection
	{
		std::vector<PatchInfo> m_patches;

		// Select visible patches centred on camera position.
		// min_distance: skip patches entirely within this radius (0 = no inner cutout).
		void Select(v4 camera_pos, float draw_distance, float min_distance = 0.0f)
		{
			using namespace cdlod;
			m_patches.clear();

			// Root node: smallest power-of-2 >= draw_distance
			auto root_size = MinPatchSize;
			while (root_size < draw_distance)
				root_size *= 2.0f;

			// Snap root to grid-aligned position containing the camera
			auto snap_x = std::floor(camera_pos.x / root_size) * root_size;
			auto snap_y = std::floor(camera_pos.y / root_size) * root_size;

			// 3×3 roots guarantee full draw_distance coverage regardless of
			// camera position within the snap grid cell. The distance culling
			// in SelectNode quickly eliminates the far roots.
			for (int dy = -1; dy <= 1; ++dy)
				for (int dx = -1; dx <= 1; ++dx)
					SelectNode(camera_pos, snap_x + dx * root_size, snap_y + dy * root_size, root_size, draw_distance, min_distance);
		}

	private:

		void SelectNode(v4 camera_pos, float nx, float ny, float size, float draw_distance, float min_distance)
		{
			using namespace cdlod;

			// Distance from camera to nearest point of this node
			auto nearest_x = std::clamp(camera_pos.x, nx, nx + size);
			auto nearest_y = std::clamp(camera_pos.y, ny, ny + size);
			auto dx = camera_pos.x - nearest_x;
			auto dy = camera_pos.y - nearest_y;
			auto dist_sq = dx * dx + dy * dy;

			// Cull nodes entirely outside draw distance
			if (dist_sq > draw_distance * draw_distance)
				return;

			// Cull nodes entirely within min_distance (covered by a near system)
			if (min_distance > 0.0f)
			{
				auto far_x = (std::abs(camera_pos.x - nx) > std::abs(camera_pos.x - (nx + size))) ? nx : nx + size;
				auto far_y = (std::abs(camera_pos.y - ny) > std::abs(camera_pos.y - (ny + size))) ? ny : ny + size;
				auto fdx = camera_pos.x - far_x;
				auto fdy = camera_pos.y - far_y;
				if (fdx * fdx + fdy * fdy < min_distance * min_distance)
					return;
			}

			// Subdivide if the node is large enough and the camera is close enough
			auto can_subdivide = size > MinPatchSize;
			auto threshold = size * SubdivFactor;
			auto should_subdivide = dist_sq < threshold * threshold;

			if (can_subdivide && should_subdivide)
			{
				auto half = size * 0.5f;
				SelectNode(camera_pos, nx,        ny,        half, draw_distance, min_distance);
				SelectNode(camera_pos, nx + half, ny,        half, draw_distance, min_distance);
				SelectNode(camera_pos, nx,        ny + half, half, draw_distance, min_distance);
				SelectNode(camera_pos, nx + half, ny + half, half, draw_distance, min_distance);
			}
			else if (static_cast<int>(m_patches.size()) < MaxPatches)
			{
				m_patches.push_back({ nx, ny, size });
			}
		}
	};
}
