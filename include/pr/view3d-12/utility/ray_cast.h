//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Notes:
	//  - The geometry shader used for hit testing (face, edge, or vert) depends on the model topology.
	//  - ESnapMode controls what sort of snapping is allowed.
	//  - ESnapType is an output value that indicates how a hit result was snapped.
	static constexpr int MaxRays = 16;
	static constexpr int MaxIntercepts = 256;


	// Point snapping mode. How rays should snap to nearby features (Keep in sync with 'SnapMode_' in 'ray_cast_cbuf.hlsli')
	enum class ESnapMode :int
	{
		NoSnap = 0,
		Verts = 1 << 0,
		Edges = 1 << 1,
		Faces = 1 << 2,
		Perspective = 1 << 8, // If set, then snap distance scales with distance from the origin
		All  = Faces | Edges | Verts,
		AllPerspective = All | Perspective,
		_flags_enum = 0,
	};

	// Snap types (in priority order) (Keep in sync with 'SnapType_' in 'ray_cast_cbuf.hlsli')
	enum class ESnapType :int
	{
		None = 0,
		Vert = 1,
		EdgeMiddle = 2,
		FaceCentre = 3,
		Edge = 4,
		Face = 5,
	};

	// A single hit test ray into the scene
	struct HitTestRay
	{
		// The world space origin and direction of the ray (normalisation not required)
		v4 m_ws_origin = v4::Origin();
		v4 m_ws_direction = v4::Zero();

		// Snap mode and distance. If snap_mode includes ESnapMode::Perspective, then the snap distance scales with distance from the origin
		ESnapMode m_snap_mode = ESnapMode::Verts | ESnapMode::Edges | ESnapMode::Faces | ESnapMode::Perspective; // Snap behaviour
		float m_snap_distance = 0; // Snap distance: 'snap_dist = Perspectvie ? snap_distance * depth : snap_distance'

		// User provided id for the ray
		int m_id = 0;
		int pad = 0;
	};

	// The output of a ray cast into the scene
	struct HitTestResult
	{
		// The origin and direction of the cast ray (in world space)
		v4 m_ws_ray_origin;
		v4 m_ws_ray_direction;

		// The intercept point and surface normal (in world space)
		v4 m_ws_intercept;
		v4 m_ws_normal;

		// The instance that was hit. (const because it's a pointer from the drawlist. Callers should use this pointer to find in ObjectSets)
		BaseInstance const* m_instance;

		// The distance from the ray origin to the intercept
		float m_distance;

		// The index/id of the input ray
		int m_ray_index;
		int m_ray_id;

		// How the point was snapped (if at all)
		ESnapType m_snap_type;

		int pad0;
		int pad1;

		// True if this was a hit
		bool IsHit() const
		{
			return m_instance != nullptr;
		}
	};

	// Callback function that return a hit test result
	using RayCastResultsOut = std::function<void(std::span<HitTestResult const>)>;

	// Coroutine callback function that supplies instances to hit test against
	using RayCastInstancesCB = std::function<BaseInstance const*()>;

	// Callback function to filter instances for hit testing
	using RayCastFilter = std::function<bool(BaseInstance const*)>;

}
