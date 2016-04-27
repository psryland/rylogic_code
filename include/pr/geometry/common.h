//********************************
// Geometry properties
//  Copyright (c) Rylogic Ltd 2006
//********************************
#pragma once

#include "pr/macros/enum.h"
#include "pr/common/cast.h"
#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/common/fmt.h"
#include "pr/common/repeater.h"
#include "pr/common/interpolate.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/maths/maths.h"
#include "pr/maths/interpolate.h"

namespace pr
{
	namespace geometry
	{
		// EGeom
		#define PR_ENUM(x) /*
			*/x(Invalid ,= 0     ) /*
			*/x(Vert    ,= 1 << 0) /* Object space 3D position
			*/x(Colr    ,= 1 << 1) /* Diffuse base colour
			*/x(Norm    ,= 1 << 2) /* Object space 3D normal
			*/x(Tex0    ,= 1 << 3) // Diffuse texture
		PR_DEFINE_ENUM2_FLAGS(EGeom, PR_ENUM);
		#undef PR_ENUM

		// EPrim
		#define PR_ENUM(x)\
			x(Invalid   ,= 0)\
			x(PointList ,= 1)\
			x(LineList  ,= 2)\
			x(LineStrip ,= 3)\
			x(TriList   ,= 4)\
			x(TriStrip  ,= 5)
		PR_DEFINE_ENUM2(EPrim, PR_ENUM);
		#undef PR_ENUM

		struct Props
		{
			pr::BBox m_bbox;  // Bounding box in model space of the generated model
			EGeom m_geom;     // The components of the generated geometry
			bool m_has_alpha; // True if the model contains any alpha

			Props()
				:m_bbox(pr::BBoxReset)
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

		namespace impl
		{
			// meta code helper
			template <typename T> T remove_ref(T&);
		}
	}
}
