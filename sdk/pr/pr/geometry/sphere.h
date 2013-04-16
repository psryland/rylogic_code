//********************************
// Geometry
//  Copyright © Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_SPHERE_H
#define PR_GEOMETRY_SPHERE_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for a geosphere
		template <typename Tvr, typename Tir>
		void SphereSize(std::size_t divisions, pr::Range<Tvr>& vrange, pr::Range<Tir>& irange)
		{
			vrange.set(0, 3 + 10 * Pow2(2 * divisions) + 11 * Pow2(divisions));
			irange.set(0, 3 * 10 * Pow2(2 * divisions + 1));
		}

		namespace impl { namespace geosphere
		{
			typedef std::size_t VIndex;
			struct GeosphereVert
			{
				pr::v4 m_vert;
				pr::v4 m_norm;
				pr::v2 m_uv;
			};
			struct GeosphereFace
			{
				VIndex m_vidx[3];
			};
			struct Child
			{
				VIndex m_other_parent;
				VIndex m_child;
			};
			typedef pr::Array<GeosphereVert, 1024> TVertCont;
			typedef pr::Array<GeosphereFace, 1024> TFaceCont;
			typedef pr::Array<Child>  TChild;
			typedef pr::Array<TChild> TVertexLookupCont;

			// A struct to hold all of the generation data
			struct CreateGeosphereData
			{
				TVertexLookupCont m_vlookup;
				TVertCont*        m_vcont;
				TFaceCont*        m_fcont;
				v4                m_radius;
				uint              m_divisions;
			};

			// Create a vertex and add it to the vertex container
			inline pr::uint AddVertex(v4 const& norm, v2 const& uv, CreateGeosphereData& data)
			{
				PR_ASSERT(PR_DBG, IsNormal3(norm), "");

				// Add the vertex
				GeosphereVert vertex;
				vertex.m_vert = (data.m_radius * norm).w1();
				vertex.m_norm = norm;
				vertex.m_uv   = uv;
				data.m_vcont->push_back(vertex);

				// Add an entry in the lookup table
				data.m_vlookup.push_back(TChild());
				data.m_vlookup.back().reserve(Max(3 << (data.m_divisions - 2), 3));
				return data.m_vcont->size() - 1;
			}

			// Get the vertex that has these two vertices as parents
			VIndex GetVertex(VIndex parent1, VIndex parent2, CreateGeosphereData& data)
			{
				auto& vlookup_p1 = data.m_vlookup[parent1];
				auto& vlookup_p2 = data.m_vlookup[parent2];

				// Try and find 'parent2' in 'vlookup_p1' or 'parent1' in 'vlookup_p2'
				for (auto i = begin(vlookup_p1), i_end = end(vlookup_p1); i != i_end; ++i)
					if (i->m_other_parent == parent2) return i->m_child;
				for (auto i = begin(vlookup_p2), i_end = end(vlookup_p2); i != i_end; ++i)
					if (i->m_other_parent == parent1) return i->m_child;

				// If it wasn't found add a vertex
				auto& vert_a = (*data.m_vcont)[parent1].m_uv.x < (*data.m_vcont)[parent2].m_uv.x ? (*data.m_vcont)[parent1] : (*data.m_vcont)[parent2];
				auto& vert_b = (*data.m_vcont)[parent1].m_uv.x < (*data.m_vcont)[parent2].m_uv.x ? (*data.m_vcont)[parent2] : (*data.m_vcont)[parent1];
				v4 norm = GetNormal3(vert_a.m_norm + vert_b.m_norm);
				v2 uv = v2::make(ATan2Positive(norm.y, norm.x) / maths::tau, (1.0f - norm.z) * 0.5f);
				if (!(FGtrEql(uv.x, vert_a.m_uv.x) && FLessEql(uv.x, vert_b.m_uv.x))) { uv.x += 1.0f; }
				PR_ASSERT(PR_DBG, FGtrEql(uv.x, vert_a.m_uv.x) && FLessEql(uv.x, vert_b.m_uv.x), "");
				auto new_vert_index = AddVertex(norm, uv, data); // Remember this modifies data.m_vlookup

				// Add the entry to the parent with the least number of children
				if (vlookup_p1.size() < vlookup_p2.size())
				{
					Child child = {parent2, new_vert_index};
					vlookup_p1.push_back(child);
				}
				else
				{
					Child child = {parent1, new_vert_index};
					vlookup_p2.push_back(child);
				}
				return new_vert_index;
			}

				// Recursively add a face
			inline void AddFace(VIndex V00, VIndex V11, VIndex V22, VIndex level, CreateGeosphereData& data)
			{
				PR_ASSERT(PR_DBG, V00 < data.m_vcont->size(), "");
				PR_ASSERT(PR_DBG, V11 < data.m_vcont->size(), "");
				PR_ASSERT(PR_DBG, V22 < data.m_vcont->size(), "");

				if (level == data.m_divisions)
				{
					GeosphereFace face;
					face.m_vidx[0] = V00;
					face.m_vidx[1] = V11;
					face.m_vidx[2] = V22;
					data.m_fcont->push_back(face);
				}
				else
				{
					auto V01 = GetVertex(V00, V11, data);
					auto V12 = GetVertex(V11, V22, data);
					auto V20 = GetVertex(V22, V00, data);
					AddFace(V00, V01, V20, level + 1, data);
					AddFace(V01, V11, V12, level + 1, data);
					AddFace(V20, V12, V22, level + 1, data);
					AddFace(V01, V12, V20, level + 1, data);
				}
			}

			// Create an Icosahedron and recursively subdivide the triangles
			inline void CreateIcosahedron(CreateGeosphereData& data)
			{
				float const A    = 2.0f / (1.0f + maths::phi * maths::phi);
				float const H1   =  1.0f - A;
				float const H2   = -1.0f + A;
				float const R    = Sqrt(1.0f - H1 * H1);
				float const dAng = maths::tau / 5.0f;

				// Add the vertices
				float ang1 = 0.0f, ang2 = maths::tau / 10.0f;
				float ua = 0.0f, ub = 0.0f;
				for (uint w = 0; w != 6; ++w, ang1 += dAng, ang2 += dAng)
				{
					v4 norm_a = v4::make(R * Cos(ang1), R * Sin(ang1), H1, 0.0f);
					v4 norm_b = v4::make(R * Cos(ang2), R * Sin(ang2), H2, 0.0f);
					float u_a = ATan2Positive(norm_a.y, norm_a.x) / maths::tau; ua = u_a + (u_a < ua);
					float u_b = ATan2Positive(norm_b.y, norm_b.x) / maths::tau; ub = u_b + (u_b < ub);
					AddVertex( v4ZAxis ,v2::make(ua, 0.0f), data);
					AddVertex( norm_a  ,v2::make(ua, (1.0f - norm_a.z) * 0.5f), data);
					AddVertex( norm_b  ,v2::make(ub, (1.0f - norm_b.z) * 0.5f), data);
					AddVertex(-v4ZAxis ,v2::make(ub, 1.0f), data);
				}

				// Add the faces
				for (pr::uint16 i = 0; i != 5; ++i)
				{
					AddFace(i *4 + 0, i*4 + 1, (1+i)*4 + 1, 0, data);
					AddFace(i *4 + 1, i*4 + 2, (1+i)*4 + 1, 0, data);
					AddFace((1+i)*4 + 1, i*4 + 2, (1+i)*4 + 2, 0, data);
					AddFace(i *4 + 2, i*4 + 3, (1+i)*4 + 2, 0, data);
				}
			}
		}}

		// Generate an ellipsoid geosphere
		template <typename TVertIter, typename TIdxIter>
		Props Geosphere(v4 const& radius, std::size_t divisions, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
		{
			// Preallocate buffers to compile the geosphere into
			pr::Range<> vrange, irange;
			SphereSize(divisions, vrange, irange);
			std::size_t num_verts = vrange.size();
			std::size_t num_faces = irange.size() / 3;
			impl::geosphere::TVertCont verts; verts.reserve(num_verts);
			impl::geosphere::TFaceCont faces; faces.reserve(num_faces);

			impl::geosphere::CreateGeosphereData data;
			data.m_vcont     = &verts;
			data.m_fcont     = &faces;
			data.m_radius    = radius;
			data.m_divisions = divisions;
			data.m_vlookup.reserve(num_verts);
			impl::geosphere::CreateIcosahedron(data);

			PR_ASSERT(PR_DBG, verts.size() == num_verts, "Number of verts in geosphere calculated incorrectly");
			PR_ASSERT(PR_DBG, faces.size() == num_faces, "Number of faces in geosphere calculated incorrectly");

			// Output the verts and indices
			for (auto i = begin(verts), iend = end(verts); i != iend; ++i)
			{
				SetPCNT(*out_verts++, i->m_vert, colour, i->m_norm, i->m_uv);
			}
			for (auto i = begin(faces), iend = end(faces); i != iend; ++i)
			{
				typedef decltype(impl::remove_ref(*out_indices)) VIdx;
				*out_indices++ = value_cast<VIdx>(i->m_vidx[0]);
				*out_indices++ = value_cast<VIdx>(i->m_vidx[1]);
				*out_indices++ = value_cast<VIdx>(i->m_vidx[2]);
			}

			Props props;
			props.m_bbox = BoundingBox::make(pr::v4Origin, radius);
			props.m_has_alpha = colour.a() != 0xFF;
			return props;
		}

		// Generate a spherically geosphere
		template <typename TVertIter, typename TIdxIter>
		Props Geosphere(float radius, std::size_t divisions, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
		{
			return Geosphere(v4::make(radius, radius, radius, 0.0f), divisions, colour, out_verts, out_indices);
		}
	}
}

#endif
