//********************************
// Geometry properties
//  Copyright (c) Rylogic Ltd 2006
//********************************
#pragma once

#include <type_traits>
#include "pr/common/cast.h"
#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/common/fmt.h"
#include "pr/common/repeater.h"
#include "pr/common/algorithm.h"
#include "pr/common/interpolate.h"
#include "pr/common/flags_enum.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/container/ring.h"
#include "pr/maths/maths.h"
#include "pr/maths/interpolate.h"

namespace pr
{
	namespace geometry
	{
		// EGeom
		enum class EGeom
		{
			Invalid = 0,
			None    = 0,
			Vert    = 1 << 0, // Object space 3D position
			Colr    = 1 << 1, // Diffuse base colour
			Norm    = 1 << 2, // Object space 3D normal
			Tex0    = 1 << 3, // Diffuse texture
			_bitwise_operators_allowed
		};

		// EPrim
		enum class EPrim
		{
			Invalid   = 0,
			None      = 0,
			PointList = 1,
			LineList  = 2,
			LineStrip = 3,
			TriList   = 4,
			TriStrip  = 5,
		};

		struct Props
		{
			BBox m_bbox;  // Bounding box in model space of the generated model
			EGeom m_geom;     // The components of the generated geometry
			bool m_has_alpha; // True if the model contains any alpha

			Props()
				:m_bbox(BBoxReset)
				,m_geom(EGeom::Vert)
				,m_has_alpha(false)
			{}
		};

		// An iterator wrapper for applying a transform to 'points'
		template <typename TVertCIter> struct Transformer
		{
			TVertCIter m_pt;
			m4x4 const* m_o2w;

			Transformer(TVertCIter points, m4x4 const& o2w) :m_pt(points) ,m_o2w(&o2w) {}
			v4 operator*() const         { return *m_o2w * *m_pt; }
			Transformer& operator ++()   { ++m_pt; return *this; }
			Transformer operator ++(int) { auto x = *this; ++(*this); return x; }
		};
	}
}
