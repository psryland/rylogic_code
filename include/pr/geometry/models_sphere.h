//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_SPHERE_H
#define PR_GEOMETRY_SPHERE_H

#include "pr/geometry/common.h"
#include "pr/linedrawer/ldr_helper.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for a geosphere
		template <typename Tvr, typename Tir>
		void GeosphereSize(std::size_t divisions, Tvr& vcount, Tir& icount)
		{
			int div = checked_cast<int>(divisions);
			vcount = checked_cast<Tvr>(3 + 10 * Pow2(2 * div) + 11 * Pow2(div));
			icount = checked_cast<Tir>(3 * 10 * Pow2(2 * div + 1));
		}

		namespace impl
		{
			namespace geosphere
			{
				typedef std::size_t VIndex;
				struct GeosphereVert
				{
					pr::v4 m_vert;
					pr::v4 m_norm;
					float m_ang;
					bool m_pole;
				};
				struct GeosphereFace
				{
					VIndex m_vidx[3];
				};
				struct Adjacent
				{
					VIndex m_adjacent; // The adjacent vertex (not necessarily at the same recursion level)
					VIndex m_child;    // The vertex between the associated vertex and 'm_adjacent' in the recursion level below 'm_adjacent'
					Adjacent() {}
					Adjacent(VIndex adj, VIndex child) :m_adjacent(adj) ,m_child(child) {}
				};
				typedef pr::vector<GeosphereVert, 1024> TVertCont;
				typedef pr::vector<GeosphereFace, 1024> TFaceCont;
				typedef pr::vector<Adjacent> TAdjacent;          // A collection of adjacent vertices
				typedef pr::vector<TAdjacent> TVertexLookupCont; // A map from vertex -> adjacent vertices

				// A struct to hold all of the generation data
				struct CreateGeosphereData
				{
					TVertexLookupCont m_adjacent;
					TVertCont*        m_vcont;
					TFaceCont*        m_fcont;
					v4                m_radius;
					std::size_t       m_divisions;
				};

				// Create a vertex and add it to the vertex container
				// Returns the index position of the vertex
				inline std::size_t AddVertex(v4 const& norm, float ang, bool pole, CreateGeosphereData& data)
				{
					PR_ASSERT(PR_DBG, IsNormal3(norm), "");

					// Add the vertex
					GeosphereVert vertex;
					vertex.m_vert = (data.m_radius * norm).w1();
					vertex.m_norm = norm;
					vertex.m_ang  = ang;
					vertex.m_pole = pole;
					data.m_vcont->push_back(vertex);

					// Add an entry in the adjacency map
					data.m_adjacent.push_back(TAdjacent());
					return data.m_vcont->size() - 1;
				}

				// Get the vertex that has these two vertices as parents
				inline VIndex GetVertex(VIndex parent0, VIndex parent1, CreateGeosphereData& data)
				{
					// Get the containers of adjacent vertex for each of parent0, and parent1
					// Note: Not using the lowest index value here because we want to minimise
					// the lengths of the adjacency containers by adding any new adjacency info
					// to the shortest container
					auto& adj0 = data.m_adjacent[parent0];
					auto& adj1 = data.m_adjacent[parent1];

					// Try and find 'parent1' in 'adj0' or 'parent0' in 'adj1'
					for (auto i = std::begin(adj0), iend = std::end(adj0); i != iend; ++i)
						if (i->m_adjacent == parent1) return i->m_child;
					for (auto i = std::begin(adj1), iend = std::end(adj1); i != iend; ++i)
						if (i->m_adjacent == parent0) return i->m_child;

					// If no child is found, add one
					auto& v0 = (*data.m_vcont)[parent0];
					auto& v1 = (*data.m_vcont)[parent1];
					v4 norm = Normalise3(v0.m_norm + v1.m_norm);
					auto ang = v0.m_pole ? v1.m_ang : v1.m_pole ? v0.m_ang : (v0.m_ang + v1.m_ang) * 0.5f; // Use the average angle unless one of the verts is a pole
					auto new_vidx = AddVertex(norm, ang, false, data);

					// Add an entry to the shortest adjacency map for this new child
					if (adj0.size() < adj1.size())
						adj0.push_back(Adjacent(parent1, new_vidx));
					else
						adj1.push_back(Adjacent(parent0, new_vidx));

					return new_vidx;
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
					float const A    = 2.0f / (1.0f + float(Sqr(maths::phi)));
					float const H1   =  1.0f - A;
					float const H2   = -1.0f + A;
					float const R    = Sqrt(1.0f - H1 * H1);
					float const dAng = float(maths::tau / 5.0);
					float const ua[] = {0.0f,0.2f,0.4f,0.6f,0.8f,1.0f,1.2f};
					float const ub[] = {0.1f,0.3f,0.5f,0.7f,0.9f,1.1f,1.3f};

					// Add the vertices
					float ang1 = 0.0f, ang2 = dAng * 0.5f;
					for (uint w = 0; w != 6; ++w, ang1 += dAng, ang2 += dAng)
					{
						v4 norm_a = v4(R * Cos(ang1), R * Sin(ang1), H1, 0.0f);
						v4 norm_b = v4(R * Cos(ang2), R * Sin(ang2), H2, 0.0f);
						AddVertex( v4ZAxis ,ub[w] ,true  ,data);
						AddVertex( norm_a  ,ua[w] ,false ,data);
						AddVertex( norm_b  ,ub[w] ,false ,data);
						AddVertex(-v4ZAxis ,ua[w+1] ,true  ,data);
					}

					// Add the faces
					for (std::size_t i = 0, ibase = 0; i != 5; ++i, ibase += 4)
					{
						AddFace(ibase + 0, ibase + 1, ibase + 5, 0, data);
						AddFace(ibase + 1, ibase + 2, ibase + 5, 0, data);
						AddFace(ibase + 5, ibase + 2, ibase + 6, 0, data);
						AddFace(ibase + 6, ibase + 2, ibase + 3, 0, data);
					}
				}
			}
		}

		// Generate an ellipsoid geosphere
		template <typename TVertIter, typename TIdxIter>
		Props Geosphere(v4 const& radius, std::size_t divisions, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
		{
			Props props;
			props.m_bbox = BBox(pr::v4Origin, radius);
			props.m_geom = EGeom::Vert | EGeom::Colr | EGeom::Norm | EGeom::Tex0;
			props.m_has_alpha = HasAlpha(colour);

			// Preallocate buffers to compile the geosphere into
			std::size_t vcount, icount;
			GeosphereSize(divisions, vcount, icount);
			impl::geosphere::TVertCont verts; verts.reserve(vcount);
			impl::geosphere::TFaceCont faces; faces.reserve(icount / 3);

			impl::geosphere::CreateGeosphereData data;
			data.m_vcont     = &verts;
			data.m_fcont     = &faces;
			data.m_radius    = radius;
			data.m_divisions = divisions;
			data.m_adjacent.reserve(vcount);
			impl::geosphere::CreateIcosahedron(data);

			PR_ASSERT(PR_DBG, verts.size() == vcount, "Number of verts in geosphere calculated incorrectly");
			PR_ASSERT(PR_DBG, faces.size() == icount/3, "Number of faces in geosphere calculated incorrectly");

			// Output the verts and indices
			for (auto i = std::begin(verts), iend = std::end(verts); i != iend; ++i)
			{
				SetPCNT(*out_verts++, i->m_vert, colour, i->m_norm, v2(i->m_ang, (1.0f - i->m_norm.z) * 0.5f));
			}
			for (auto i = std::begin(faces), iend = std::end(faces); i != iend; ++i)
			{
				using VIdx = typename std::remove_reference<decltype(*out_indices)>::type;
				*out_indices++ = checked_cast<VIdx>(i->m_vidx[0]);
				*out_indices++ = checked_cast<VIdx>(i->m_vidx[1]);
				*out_indices++ = checked_cast<VIdx>(i->m_vidx[2]);
			}

			return props;
		}

		// Generate a spherical geosphere
		template <typename TVertIter, typename TIdxIter>
		Props Geosphere(float radius, std::size_t divisions, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
		{
			return Geosphere(v4(radius, radius, radius, 0.0f), divisions, colour, out_verts, out_indices);
		}

		// Returns the number of verts and number of indices needed to hold geometry for a sphere
		template <typename Tvr, typename Tir>
		void SphereSize(std::size_t wedges, std::size_t layers, Tvr& vcount, Tir& icount)
		{
			if (wedges < 3) wedges = 3;
			if (layers < 2) layers = 2;
			vcount = value_cast<Tvr>((wedges + 1) * (layers + 1));
			icount = value_cast<Tir>(3 * wedges * (2 * layers - 2));
		}

		// Generate a standard sphere
		template <typename TVertIter, typename TIdxIter>
		Props Sphere(v4 const& radius, std::size_t wedges, std::size_t layers, Colour32 colour, TVertIter out_verts, TIdxIter out_indices)
		{
			Props props;
			props.m_bbox = BBox(pr::v4Origin, radius);
			props.m_has_alpha = colour.a() != 0xFF;

			if (wedges < 3) wedges = 3;
			if (layers < 2) layers = 2;

			// Verts
			for (std::size_t w = 0; w <= wedges; ++w)
			{
				v4 norm = v4ZAxis;
				v2 uv   = v2(float(w + 0.5f) / wedges, 0.0f);
				SetPCNT(*out_verts++, (radius * norm).w1(), colour, norm, uv);

				for (std::size_t l = 1; l < layers; ++l)
				{
					float a = maths::tau * w / wedges;
					float b = maths::tau_by_2 * l / layers;
					norm = v4(Cos(a) * Sin(b), Sin(a) * Sin(b), Cos(b), 0.0f);
					uv   = v2(float(w) / wedges, (1.0f - norm.z) * 0.5f);
					SetPCNT(*out_verts++, (radius * norm).w1(), colour, norm, uv);
				}

				norm = -v4ZAxis;
				uv   = v2(float(w + 0.5f) / wedges, 1.0f);
				SetPCNT(*out_verts++, (radius * norm).w1(), colour, norm, uv);
			}

			// Faces
			typedef decltype(impl::remove_ref(*out_indices)) VIdx;
			std::size_t ibase = 0, ilayer = 0, verts_per_wedge = 1 + layers;
			for (std::size_t w = 0; w != wedges; ++w, ibase += verts_per_wedge, ilayer = ibase)
			{
				*out_indices++ = value_cast<VIdx>(ilayer + 0);
				*out_indices++ = value_cast<VIdx>(ilayer + 1);
				*out_indices++ = value_cast<VIdx>(ilayer + 1 + verts_per_wedge);
				++ilayer;
				for (std::size_t l = 1; l != layers - 1; ++l)
				{
					*out_indices++ = value_cast<VIdx>(ilayer + 0);
					*out_indices++ = value_cast<VIdx>(ilayer + 1);
					*out_indices++ = value_cast<VIdx>(ilayer + 0 + verts_per_wedge);
					*out_indices++ = value_cast<VIdx>(ilayer + 0 + verts_per_wedge);
					*out_indices++ = value_cast<VIdx>(ilayer + 1);
					*out_indices++ = value_cast<VIdx>(ilayer + 1 + verts_per_wedge);
					++ilayer;
				}
				*out_indices++ = value_cast<VIdx>(ilayer + 0 + verts_per_wedge);
				*out_indices++ = value_cast<VIdx>(ilayer + 0 );
				*out_indices++ = value_cast<VIdx>(ilayer + 1 );
			}

			return props;
		}
	}
}

#endif
