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

namespace pr::geometry
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

		_bitwise_operators_allowed = 0x7FFFFFFF,
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
		using iterator_category = std::forward_iterator_tag;
		using value_type = typename std::iterator_traits<TVertCIter>::value_type;
		using difference_type = typename std::iterator_traits<TVertCIter>::difference_type;
		using reference = typename std::iterator_traits<TVertCIter>::reference;
		using pointer = typename std::iterator_traits<TVertCIter>::pointer;

		TVertCIter m_pt;
		m4x4 const* m_o2w;

		Transformer(TVertCIter points, m4x4 const& o2w)
			:m_pt(points)
			,m_o2w(&o2w)
		{}
		v4 operator*() const
		{
			return *m_o2w * *m_pt;
		}
		Transformer& operator ++()
		{
			++m_pt;
			return *this;
		}
		Transformer operator ++(int)
		{
			auto x = *this;
			++(*this);
			return x;
		}
	};

	// Output iterator adapter for flipping faces
	template <typename TIdxIter> struct FaceFlipper
	{
		// Notes:
		//  - Don't implement post-increment. Code that uses post increment won't work
		//    with this iterator because the assignment occurs after the increment. This
		//    iterator wrapper outputs to the underlying iterator on increment.

		using iterator_category = std::output_iterator_tag;
		using value_type = typename std::iterator_traits<TIdxIter>::value_type;
		using difference_type = void;
		using reference = void;
		using pointer = void;

		TIdxIter m_out;
		value_type m_idx[3];
		int m_count;

		FaceFlipper(TIdxIter out)
			:m_out(out)
			,m_idx()
			,m_count()
		{}
		FaceFlipper& operator*()
		{
			return *this;
		}
		FaceFlipper& operator =(value_type idx)
		{
			m_idx[m_count] = idx;
			return *this;
		}
		FaceFlipper& operator ++()
		{
			if (++m_count == 3)
			{
				*m_out++ = m_idx[0];
				*m_out++ = m_idx[2];
				*m_out++ = m_idx[1];
				m_count = 0;
			}
			return *this;
		}
		FaceFlipper operator ++(int) = delete;
	};
}
