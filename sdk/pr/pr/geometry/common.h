//********************************
// Geometry properties
//  Copyright © Rylogic Ltd 2006
//********************************
#pragma once
#ifndef PR_GEOMETRY_COMMON_H
#define PR_GEOMETRY_COMMON_H

#include "pr/common/valuecast.h"
#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/common/array.h"
#include "pr/common/repeater.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace geometry
	{
		struct Props
		{
			pr::BoundingBox m_bbox; // Bounding box in model space of the generated model
			bool m_has_alpha;       // True if the model contains any alpha
			
			Props()
			:m_bbox(pr::BBoxReset)
			,m_has_alpha(false)
			{}
		};

		// A colour iterator wrapper
		struct ColourRepeater
		{
			pr::Repeater<Colour32 const*,Colour32> m_rep;
			bool m_alpha;   // True if any of the returned colours contain a non-0xFF alpha value

			Colour32 operator *()
			{
				Colour32 col = *m_rep;
				m_alpha |= col.a() != 0xff;
				return col;
			}
			ColourRepeater& operator ++()
			{
				m_rep++;
				return *this;
			}
			ColourRepeater operator ++(int)
			{
				ColourRepeater r = *this;
				++*this;
				return r;
			}

			// 'colours' is the src iterator
			// 'num_colours' is the number of available colours pointed to by 'colours'
			// 'output_count' is the number of times the iterator will be incremented
			ColourRepeater(Colour32 const* colours, std::size_t num_colours, std::size_t output_count, Colour32 def)
			:m_rep(colours, num_colours, output_count, def)
			,m_alpha(false)
			{}
		};

		// An iterator wrapper for applying a transform to 'points'
		template <typename TVertCIter> struct Transformer
		{
			TVertCIter m_pt;
			m4x4 const* m_o2w;
			v4 operator*() const       { return *m_o2w * *m_pt; }
			Transformer& operator ++() { ++m_pt; return *this; }
			Transformer(TVertCIter points, m4x4 const& o2w) :m_pt(points) ,m_o2w(&o2w) {}
		};

		namespace impl
		{
			// meta code helper
			template <typename T> T remove_ref(T&);
		}

	}
}

#endif
