//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_UTILITY_H
#define PR_GEOMETRY_UTILITY_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Generate normals for a model. Assumes the model data is a TriList
		// 'num_indices' is the number of indices available through the 'indices' iterator (should be a multiple of 3)
		// 'indices' is an iterator to the model face data (sets of 3 indices per face)
		// 'GetV' is a function object with sig pr::v4 (*GetV)(size_t i) returning the vertex position at index position 'i'
		// 'GetN' is a function object with sig pr::v4 (*GetN)(size_t i) returning the vertex normal at index position 'i'
		// 'SetN' is a function object with sig void (*SetN)(size_t i, pr::v4 const& n) used to set the value of the normal at index position 'i'
		// Only reads/writes to the normals for vertices adjoining the provided faces
		template <typename TIdxCIter, typename TGetV, typename TGetN, typename TSetN>
		void GenerateNormals(std::size_t num_indices, TIdxCIter indices, TGetV GetV, TGetN GetN, TSetN SetN)
		{
			// Initialise all of the vertex normals to zero
			auto ib = indices;
			for (std::size_t i = 0; i != num_indices; ++i, ++ib)
				SetN(*ib, v4Zero);

			// For each face, calculate the face normal and add it to the normals of each adjoining vertex
			ib = indices;
			for (std::size_t i = 0, iend = num_indices/3; i != iend; ++i)
			{
				std::size_t i0 = *ib++;
				std::size_t i1 = *ib++;
				std::size_t i2 = *ib++;

				v4 const& v0 = GetV(i0);
				v4 const& v1 = GetV(i1);
				v4 const& v2 = GetV(i2);

				// Calculate the face normal
				v4 norm = Normalise3IfNonZero(Cross3(v1 - v0, v2 - v0));

				// Add the normal to each vertex that references the face
				SetN(i0, GetN(i0) + norm);
				SetN(i1, GetN(i1) + norm);
				SetN(i2, GetN(i2) + norm);
			}

			// Normalise all of the normals
			ib = indices;
			for (std::size_t i = 0; i != num_indices; ++i, ++ib)
				SetN(*ib, Normalise3IfNonZero(GetN(*ib)));
		}
	}
}

#endif
