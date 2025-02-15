//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Flags controlling the behaviour of hit testing
	enum class EHitTestFlags :int
	{
		Faces = 1 << 0,
		Edges = 1 << 1,
		Verts = 1 << 2,
		_flags_enum = 0,
	};

	// Snap types (in priority order) (Keep in sync with SNAP_TYPE_ in 'ray_cast_cbuf.hlsli')
	enum class ESnapType :int
	{
		NoSnap = 0,
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
		v4 m_ws_origin;
		v4 m_ws_direction;
	};

	// The output of a ray cast into the scene
	struct HitTestResult
	{
		v4                  m_ws_origin;     // The origin of the ray that hit something
		v4                  m_ws_direction;  // The direction of the ray that hit something
		v4                  m_ws_intercept;  // Where the intercept is in world space
		BaseInstance const* m_instance;      // The instance that was hit. (const because it's a pointer from the drawlist. Callers should use this pointer to find in ObjectSets)
		float               m_distance;      // The distance from the ray origin to the intercept
		int                 m_ray_index;     // The index of the input ray
		ESnapType           m_snap_type;     // How the point was snapped (if at all)
	};

	// Coroutine callback function that supplies instances to hit test against
	using RayCastInstancesCB = std::function<BaseInstance const*()>;

	// Callback function that returns the hit test results
	using RayCastResultsOut = std::function<bool(HitTestResult const&)>;

	// Callback function to filter instances for hit testing
	using RayCastFilter = std::function<bool(BaseInstance const*)>;

}
