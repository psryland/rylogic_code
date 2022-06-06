//********************************
// Geometry properties
//  Copyright (c) Rylogic Ltd 2006
//********************************
#pragma once
#include <cassert>
#include <type_traits>
#include "pr/common/cast.h"
#include "pr/common/range.h"
#include "pr/common/fmt.h"
#include "pr/common/repeater.h"
#include "pr/common/algorithm.h"
#include "pr/common/interpolate.h"
#include "pr/common/flags_enum.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/container/ring.h"
#include "pr/gfx/colour.h"
#include "pr/maths/maths.h"
#include "pr/maths/line3.h"
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
		All     = Vert | Colr | Norm | Tex0,

		_flags_enum = 0x7FFFFFFF,
	};
	static_assert(is_flags_enum_v<EGeom>);

	// ETopo
	enum class ETopo
	{
		// Note: don't assume these are the same as directX. Dx11/Dx12 have different values
		Undefined = 0,
		PointList,
		LineList,
		LineStrip,
		TriList,
		TriStrip,
		LineListAdj,
		LineStripAdj,
		TriListAdj,
		TriStripAdj,
	};

	// EPrimGroup
	enum class ETopoGroup
	{
		None,
		Points,
		Lines,
		Triangles,
	};

	// Geometry properties
	struct Props
	{
		BBox  m_bbox;      // Bounding box in model space of the generated model
		EGeom m_geom;      // The components of the generated geometry
		bool  m_has_alpha; // True if the model contains any alpha

		Props()
			:m_bbox(BBox::Reset())
			,m_geom(EGeom::Vert)
			,m_has_alpha(false)
		{}
	};

	// Vertex and Index buffer sizes
	struct BufSizes
	{
		int vcount;
		int icount;

		constexpr BufSizes(int nv, int ni)
			:vcount(nv)
			,icount(ni)
		{}
	};

	// Classify topology types
	constexpr ETopoGroup TopoGroup(ETopo topo)
	{
		return
			topo == ETopo::TriList || topo == ETopo::TriListAdj ? ETopoGroup::Triangles :
			topo == ETopo::TriStrip || topo == ETopo::TriStripAdj ? ETopoGroup::Triangles :
			topo == ETopo::LineList || topo == ETopo::LineListAdj ? ETopoGroup::Lines :
			topo == ETopo::LineStrip || topo == ETopo::LineStripAdj ? ETopoGroup::Lines :
			topo == ETopo::PointList ? ETopoGroup::Points :
			ETopoGroup::None;
	}

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

	// Closest point result object
	struct MinSeparation
	{
		v4 m_axis;
		float m_axis_len_sq;
		float m_depth_sq;

		MinSeparation()
			:m_axis()
			,m_axis_len_sq()
			,m_depth_sq(maths::float_inf)
		{}

		// Boolean test of penetration
		bool Contact() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_depth_sq > 0;
		}

		// Return the depth of penetration
		float Depth() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return SignedSqrt(m_depth_sq);
		}

		// The direction of minimum penetration (normalised)
		v4 SeparatingAxis() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_axis / Sqrt(m_axis_len_sq);
		}

		// Record the minimum depth separation
		void operator()(float depth, v4_cref<> axis)
		{
			// Defer the sqrt by comparing squared depths.
			// Need to preserve the sign however.
			auto len_sq = LengthSq(axis);
			auto d_sq = SignedSqr(depth) / len_sq;
			if (d_sq < m_depth_sq)
			{
				m_axis = axis;
				m_axis_len_sq = len_sq;
				m_depth_sq = d_sq;
			}
		};
	};
}
