//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <deque>
#include <vector>
#include <type_traits>
#include <cassert>
#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Generate normals for a collection of faces.
		// 'num_indices' is the number of indices available through 'indices'. Multiple of 3 expected.
		// 'indices' is an iterator to model face data. Expects 3 indices per face.
		// 'smoothing_angle' is the threshold above which normals are not merged and a new vertex is created (in radians)
		// 'getv' is an accessor to the vertex for a given face index.
		// 'vout' outputs the new vertex normals: vout(VIdx new_idx, VIdx orig_idx, v4 normal)
		// 'iout' outputs the new face indices: iout(VIdx i0, VIdx i1, VIdx i2)
		// This function will only adds verts, not remove any, so 'vout' can overwrite and add to the existing container
		// It also outputs the verts in order, so if new_idx >= vert_cont.size() => push_back is fine.
		// The number of indices returned will equal 'num_indices' so it's also fine to overwrite the index container
		template <typename TIdxCIter, typename TGetV, typename TVertOut, typename TIdxOut>
		void GenerateNormals(std::size_t num_indices, TIdxCIter indices, float smoothing_angle, TGetV getv, TVertOut vout, TIdxOut iout)
		{
			// Notes:
			// - Can't weld verts because that would destroy distinct texture
			//  verts or colours. If verts are distinct it's likely they represent
			//  a discontinuous edge in the model and are therefore not edges that
			//  should be smoothed anyway.
			typedef std::remove_reference<decltype(*indices)>::type VIdx;
			if ((num_indices % 3) != 0) throw std::exception("GenerateNormals expects triangle list data");

			struct L
			{
				struct Face
				{
					pr::v4 m_norm;
					pr::v4 m_angles;
					size_t m_idx[3];
					int    m_grp;
					pr::v4 normal(size_t idx) const
					{
						if (idx == m_idx[0]) return m_norm * m_angles.x;
						if (idx == m_idx[1]) return m_norm * m_angles.y;
						if (idx == m_idx[2]) return m_norm * m_angles.z;
						assert(false && "index is not part of this face");
						return v4Zero;
					}
				};
				struct Edge
				{
					size_t m_eidx;      // Index of the other end of the edge
					Face* m_lface;
					Face* m_rface;     
					Edge* m_next;      // Forms a linked list of edges
					bool m_nonplanar; // True if this edge has more than two left or right faces
					bool smooth(float cos_angle_threshold) const
					{
						return
							m_lface && m_rface && // two faces needed to be smooth
							!m_nonplanar &&       // no more than two faces per side
							(
								(m_lface->m_grp == m_rface->m_grp) ||                           // Already in the same group
								(Dot3(m_lface->m_norm, m_rface->m_norm) > cos_angle_threshold)  // Normals within the tolerance
							);
					}
				};
				struct Vert
				{
					pr::v4 m_norm;     // Smoothed vertex normal
					Edge*  m_edges;    // The edges that connect to this vertex
					Vert*  m_next;     // Other vert the same as this, but in a different smoothing group
					size_t m_orig_idx; // Index of the original vertex
					size_t m_new_idx;  // Index of the vertex in 'm_verts'
					int    m_grp;      // The smoothing group number that all contributing faces have
				};
				static_assert(std::alignment_of<Face>::value == 16, "Face not aligned correctly");
				static_assert(std::alignment_of<Vert>::value == 16, "Vert not aligned correctly");
				static_assert((sizeof(Face) % sizeof(pr::v4)) == 0, "Face size not a multiple of pr::v4 alignment");
				static_assert((sizeof(Vert) % sizeof(pr::v4)) == 0, "Vert size not a multiple of pr::v4 alignment");

				std::vector<Face> m_faces;
				std::deque<Vert> m_verts;
				std::deque<Edge> m_edge_alloc;

				L(std::size_t num_indices, TIdxCIter indices, float smoothing_angle, TGetV getv)
					:m_faces()
					,m_verts()
					,m_edge_alloc()
				{
					BuildAdjacencyData(num_indices, indices, getv);
					AssignSmoothingGroups(smoothing_angle);
					CreateNormals();
				}

				// Creates the adjacency data
				void BuildAdjacencyData(std::size_t num_indices, TIdxCIter indices, TGetV getv)
				{
					size_t max_index = 0;
					int sg = 0;

					// Generate a collection of faces including their normals and vertex angles
					m_faces.reserve(num_indices / 3);
					for (std::size_t i = 0; i != num_indices; i += 3)
					{
						Face face = {};
						face.m_idx[0] = *indices++;
						face.m_idx[1] = *indices++;
						face.m_idx[2] = *indices++;

						v4 const& v0 = getv(checked_cast<VIdx>(face.m_idx[0]));
						v4 const& v1 = getv(checked_cast<VIdx>(face.m_idx[1]));
						v4 const& v2 = getv(checked_cast<VIdx>(face.m_idx[2]));

						max_index = std::max(*std::max_element(std::begin(face.m_idx), std::end(face.m_idx)), max_index);
						face.m_norm = Normalise3(Cross3(v1 - v0, v2 - v1), pr::v4Zero);
						face.m_angles = TriangleAngles(v0, v1, v2);
						face.m_grp = ++sg; // each face is in a unique smoothing group to start with

						m_faces.emplace_back(face);
					}

					// Generate a collection of verts
					for (size_t i = 0; i != max_index+1; ++i)
					{
						Vert vert = {};
						vert.m_orig_idx = i;
						vert.m_new_idx = i;
						m_verts.emplace_back(vert);
					}

					// Add edges to the corresponding vertices
					auto add_edge = [&](size_t i0, size_t i1, Face* face)
					{
						Edge* eptr;

						// Left side
						eptr = m_verts[i0].m_edges;
						for (; eptr != nullptr && eptr->m_eidx != i1; eptr = eptr->m_next) {}
						if (eptr)
						{
							eptr->m_nonplanar = eptr->m_lface != nullptr;
							eptr->m_lface = face;
						}
						else
						{
							Edge& edge = (m_edge_alloc.emplace_back(), m_edge_alloc.back());
							edge.m_eidx = i1;
							edge.m_lface = face;
							edge.m_next = m_verts[i0].m_edges;
							m_verts[i0].m_edges = &edge;
						}

						// Right side
						eptr = m_verts[i1].m_edges;
						for (; eptr != nullptr && eptr->m_eidx != i0; eptr = eptr->m_next) {}
						if (eptr)
						{
							eptr->m_nonplanar = eptr->m_rface != nullptr;
							eptr->m_rface = face;
						}
						else
						{
							Edge& edge = (m_edge_alloc.emplace_back(), m_edge_alloc.back());
							edge.m_eidx = i0;
							edge.m_rface = face;
							edge.m_next = m_verts[i1].m_edges;
							m_verts[i1].m_edges = &edge;
						}
					};

					// Add the edges to each vertex
					for (auto& face : m_faces)
					{
						add_edge(face.m_idx[0], face.m_idx[1], &face);
						add_edge(face.m_idx[1], face.m_idx[2], &face);
						add_edge(face.m_idx[2], face.m_idx[0], &face);
					}
				}

				// Partition the edges for each vert into seperate groups
				void AssignSmoothingGroups(float smoothing_angle)
				{
					auto cos_angle_threshold = pr::Cos(smoothing_angle);
					for (auto& vert : m_verts)
					{
						bool no_changes;
						do
						{
							no_changes = true;
							for (auto eptr = vert.m_edges; eptr != nullptr; eptr = eptr->m_next)
							{
								bool smooth_edge = eptr->smooth(cos_angle_threshold);
								if (smooth_edge && eptr->m_lface->m_grp != eptr->m_rface->m_grp)
								{
									// Assign the left and right faces to the same smoothing group (the lowest)
									eptr->m_lface->m_grp = eptr->m_rface->m_grp = std::min(eptr->m_lface->m_grp, eptr->m_rface->m_grp);
									no_changes = false;
								}
							}
						}
						while (!no_changes);
					}
				}

				// Generate the normal for each vertex, adding new vertices for seperate smoothing groups
				void CreateNormals()
				{
					// Return the new index for vertex 'idx' in smoothing group 'face.m_grp'
					auto vert_index = [&](Face& face, size_t idx)
					{
						auto* vptr = &m_verts[idx];

						// Vertex has not yet been assigned a smoothing group
						if (vptr->m_grp == 0)
						{
							vptr->m_norm = face.normal(idx);
							vptr->m_grp = face.m_grp;
							return vptr->m_new_idx;
						}

						// Find a vertex with a matching smoothing group
						for (; vptr != nullptr && vptr->m_grp != face.m_grp; vptr = vptr->m_next) {}
						if (vptr)
						{
							vptr->m_norm += face.normal(idx);
							return vptr->m_new_idx;
						}

						// Add a new copy of this vertex
						vptr = (m_verts.emplace_back(), &m_verts.back());
						vptr->m_next = m_verts[idx].m_next;
						m_verts[idx].m_next = vptr;

						// Initialise the new vertex
						vptr->m_orig_idx = idx;
						vptr->m_new_idx = m_verts.size() - 1;
						vptr->m_norm = face.normal(idx);
						vptr->m_grp = face.m_grp;
						return vptr->m_new_idx;
					};

					// Loop over the faces updating the vertex indices if necessary
					for (auto& face : m_faces)
					{
						face.m_idx[0] = vert_index(face, face.m_idx[0]);
						face.m_idx[1] = vert_index(face, face.m_idx[1]);
						face.m_idx[2] = vert_index(face, face.m_idx[2]);
					}
				}
			};
			
			// Generate the normals
			L gen(num_indices, indices, smoothing_angle, getv);

			// Output the new verts
			for (auto& vert : gen.m_verts)
			{
				// On x86 release, vert.m_norm isn't aligned, don't know why.
				// I think it's something to do with std::deque.
				// Copying to stack makes it aligned
				v4 norm = vert.m_norm;

				// Output the original vertex index, and the normal.
				// Callback function should duplicate the original vertex and set the normal to that provided.
				vout(
					checked_cast<VIdx>(vert.m_new_idx),
					checked_cast<VIdx>(vert.m_orig_idx),
					Normalise3(norm, v4Zero));
			}

			// Output the new faces
			for (auto& face : gen.m_faces)
			{
				// Output faces, should be the same number as provided via 'indices'
				iout(
					checked_cast<VIdx>(face.m_idx[0]),
					checked_cast<VIdx>(face.m_idx[1]),
					checked_cast<VIdx>(face.m_idx[2]));
			}
		}

		// Generate normals for a model. Assumes the model data is a TriList
		// 'num_indices' is the number of indices available through the 'indices' iterator (should be a multiple of 3)
		// 'indices' is an iterator to the model face data (sets of 3 indices per face)
		// 'GetV' is a function object with sig pr::v4 (*GetV)(size_t i) returning the vertex position at index position 'i'
		// 'GetN' is a function object with sig pr::v4 (*GetN)(size_t i) returning the vertex normal at index position 'i'
		// 'SetN' is a function object with sig void (*SetN)(size_t i, pr::v4 const& n) used to set the value of the normal at index position 'i'
		// Only reads/writes to the normals for vertices adjoining the provided faces
		// Note: This is the simple version without vertex weights or edge detection
		template <typename TIdxCIter, typename TGetV, typename TGetN, typename TSetN>
		void GenerateNormalsSpherical(std::size_t num_indices, TIdxCIter indices, TGetV GetV, TGetN GetN, TSetN SetN)
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

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/new.h"
#include "pr/macros/count_of.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_utility)
		{
			using namespace pr::geometry;
			struct Vert :pr::AlignTo<16>
			{
				pr::v4 m_pos;
				pr::v4 m_norm;

				Vert(){}
				Vert(pr::v4 const& pos, pr::v4 const& norm) :m_pos(pos) ,m_norm(norm) {}
			};
			Vert verts[] =
			{
				Vert(pr::v4::make(0.0f, 0.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(pr::v4::make(1.0f, 0.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(pr::v4::make(1.0f, 1.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(pr::v4::make(0.0f, 1.0f, 0.0f, 1.0f), pr::v4Zero),
			};
			int faces[] =
			{
				0, 1, 2,
				0, 2, 3,
			};

			for (int i = 0; i != 2; ++i)
			{
				switch (i)
				{
				case 1:
					// try again with v[2] moved out of the plane
					verts[2].m_pos.z = 1.0f;
					break;
				}

				std::vector<Vert> vout;
				std::vector<int> iout;
				GenerateNormals(PR_COUNTOF(faces), &faces[0], pr::DegreesToRadians(10.0f)
					,[&](size_t i)
					{
						assert(i < PR_COUNTOF(verts));
						return verts[i].m_pos;
					}
					,[&](int, int orig_idx, pr::v4 const& norm)
					{
						assert(orig_idx < PR_COUNTOF(verts));
						vout.emplace_back(verts[orig_idx].m_pos, norm);
					}
					,[&](int i0, int i1, int i2)
					{
						iout.emplace_back(i0);
						iout.emplace_back(i1);
						iout.emplace_back(i2);
					});

				switch (i)
				{
				case 0:
					PR_CHECK(vout.size(), 4U);
					PR_CHECK(iout.size(), 6U);
					break;
				case 1:
					PR_CHECK(vout.size(), 6U);
					PR_CHECK(iout.size(), 6U);
					break;
				}
			}

		}
	}
}
#endif