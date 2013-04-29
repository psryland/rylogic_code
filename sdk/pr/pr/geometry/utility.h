//********************************
// Geometry
//  Copyright © Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_UTILITY_H
#define PR_GEOMETRY_UTILITY_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Generate normals for a model
		// 'num_indices' is the number of indices available through the 'indices' iterator (should be a multiple of 3)
		// 'indices' is an iterator to the model face data (sets of 3 indices per face)
		// 'verts' should be a container of verts accessible via the [] operator with the indices returned from 'indices'
		// Only overwrites the normals for vertices adjoining the provided faces
		template <typename TIdxCIter, typename TVertCont>
		void GenerateNormals(std::size_t num_indices, TIdxCIter indices, TVertCont& verts)
		{
			// Initialise all of the vertex normals to zero
			auto ib = indices;
			for (std::size_t i = 0; i != num_indices; ++i, ++ib)
				SetN(verts[*ib], v4Zero);

			// For each face, calculate the face normal and add it to the normals of each adjoining vertex
			ib = indices;
			for (std::size_t i = 0, iend = num_indices/3; i != iend; ++i)
			{
				auto& v0 = GetP(verts[*ib++]);
				auto& v1 = GetP(verts[*ib++]);
				auto& v2 = GetP(verts[*ib++]);

				// Calculate the face normal
				v4 norm = Normalise3IfNonZero(Cross3(v1 - v0, v2 - v0));

				// Add the normal to each vertex that references the face
				SetN(v0, GetN(v0) + norm);
				SetN(v1, GetN(v1) + norm);
				SetN(v2, GetN(v2) + norm);
			}

			// Normalise all of the normals
			ib = indices;
			for (std::size_t i = 0; i != num_indices; ++i, ++ib)
			{
				auto& v = verts[*ib++];
				SetN(v, GetNormal3IfNonZero(GetN(v)));
			}
		}
	}
}

#endif
