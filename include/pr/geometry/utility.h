//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <type_traits>
#include <cassert>
#include "pr/geometry/common.h"
#include "pr/geometry/triangle.h"

namespace pr
{
	namespace geometry
	{
		// Calculate the model space axis aligned bounding box
		template <typename TVertCIter>
		pr::BBox CalculateBBox(std::size_t num_verts, TVertCIter verts)
		{
			auto bbox = pr::BBoxReset;
			for (; num_verts-- != 0; ++verts)
				pr::Encompass(bbox, GetP(*vert));
			return bbox;
		}

		// Generate normals for a collection of faces.
		// 'num_indices' is the number of indices available through 'indices'. Multiple of 3 expected.
		// 'indices' is an iterator to model face data. Expects 3 indices per face.
		// 'smoothing_angle' is the threshold above which normals are not merged and a new vertex is created (in radians)
		// 'getv' is an accessor to the vertex for a given face index: v4 const& getv(VIdx idx)
		// 'new_vidx' is the start index to assign to new vertices. Effectively, it's the size of the container 'getv' is
		//   pulling from. You can set this to zero in which case one passed the largest vertex index encountered will be used.
		// 'vout' outputs the new vertex normals: vout(VIdx new_idx, VIdx orig_idx, v4 normal)
		// 'iout' outputs the new face indices: iout(VIdx i0, VIdx i1, VIdx i2)
		// This function will only add verts, not remove any, so 'vout' can overwrite and add to the existing container.
		// It also outputs the verts in order.
		// e.g.
		//   vout = [&](VIdx new_idx, VIdx orig_idx, pr::v4 const& normal)
		//     {
		//        if (new_idx >= verts.size()) verts.resize(new_idx + 1, verts[orig_idx]);
		//        verts[new_idx].norm = normal;
		//      }
		// The number of indices returned will equal 'num_indices' so it's also fine to overwrite the index container
		// e.g.
		//    iout = [&](VIdx i0, VIdx i1, VIdx i2)
		//      {
		//         *iptr++ = i0;
		//         *iptr++ = i1;
		//         *iptr++ = i2;
		//      }
		template <typename TIdxCIter, typename TGetV, typename TVertOut, typename TIdxOut>
		void GenerateNormals(std::size_t num_indices, TIdxCIter indices, float smoothing_angle, TGetV getv, std::size_t new_vidx, TVertOut vout, TIdxOut iout)
		{
			// Notes:
			// - Can't weld verts because that would destroy distinct texture
			//  verts or colours. If verts are distinct it's likely they represent
			//  a discontinuous edge in the model and are therefore not edges that
			//  should be smoothed anyway.
			typedef std::remove_reference<decltype(*indices)>::type VIdx;
			if ((num_indices % 3) != 0)
				throw std::exception("GenerateNormals expects triangle list data");

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
						// 'idx' is the vertex index in m_verts
						if (idx == m_idx[0]) return m_norm * m_angles.x;
						if (idx == m_idx[1]) return m_norm * m_angles.y;
						if (idx == m_idx[2]) return m_norm * m_angles.z;
						assert(false && "index is not part of this face");
						return v4Zero;
					}
				};
				struct Edge
				{
					size_t m_sidx;      // Index of the start of the edge
					size_t m_eidx;      // Index of the other end of the edge
					Face*  m_lface;     // The face to the left of the edge
					Face*  m_rface;     // The face to the right of the edge
					Edge*  m_next;      // Forms a linked list of edges
					bool   m_nonplanar; // True if this edge has more than two left or right faces
					bool   smooth(float cos_angle_threshold) const
					{
						return
							m_lface && m_rface && // two faces needed to be smooth
							!m_nonplanar &&       // no more than two faces per side
							(
								(m_lface->m_grp == m_rface->m_grp) ||                           // Already in the same group
								(Dot3(m_lface->m_norm, m_rface->m_norm) > cos_angle_threshold)  // Normals within the tolerance
							);
					}
					//pr::v4 norm() const
					//{
					//	// Return the edge normal assuming this is a smooth edge
					//	assert(m_lface && m_rface && !m_nonplanar && "Edge normal is only defined for smooth edges");
					//	auto n0 = m_lface->normal(m_sidx);
					//	auto n1 = m_rface->normal(m_eidx);
					//}
				};
				struct Vert
				{
					pr::v4 m_norm;       // Smoothed vertex normal
					Edge*  m_edges;      // The edges that connect to this vertex
					Vert*  m_next;       // Other vert the same as this, but in a different smoothing group
					size_t m_orig_idx;   // Index of the original vertex
					size_t m_new_idx;    // Index of the vertex in 'm_verts'
					int    m_edge_count; // The number of edges connecting to this vertex
					int    m_grp;        // The smoothing group number that all contributing faces have
				};
				static_assert(std::alignment_of<Face>::value == 16, "Face not aligned correctly");
				static_assert(std::alignment_of<Vert>::value == 16, "Vert not aligned correctly");
				static_assert((sizeof(Face) % sizeof(pr::v4)) == 0, "Face size not a multiple of pr::v4 alignment");
				static_assert((sizeof(Vert) % sizeof(pr::v4)) == 0, "Vert size not a multiple of pr::v4 alignment");

				pr::vector<Face> m_faces;
				pr::deque<Vert> m_verts;
				pr::deque<Edge> m_edge_alloc;

				L(std::size_t num_indices, TIdxCIter indices, float smoothing_angle, TGetV getv, std::size_t new_vidx)
					:m_faces()
					,m_verts()
					,m_edge_alloc()
				{
					BuildAdjacencyData(num_indices, indices, getv);
					//todo GenerateDegenerateVerts(smoothing_angle);
					AssignSmoothingGroups(smoothing_angle);
					CreateNormals(new_vidx);
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

						max_index = pr::max(max_index, face.m_idx[0], face.m_idx[1], face.m_idx[2]);
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
							edge.m_sidx = i0;
							edge.m_eidx = i1;
							edge.m_lface = face;
							edge.m_next = m_verts[i0].m_edges;
							m_verts[i0].m_edges = &edge;
							++m_verts[i0].m_edge_count;
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
							edge.m_sidx = i1;
							edge.m_eidx = i0;
							edge.m_rface = face;
							edge.m_next = m_verts[i1].m_edges;
							m_verts[i1].m_edges = &edge;
							++m_verts[i1].m_edge_count;
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

				//// Create vertices with distinct normals
				//void GenerateDegenerateVerts(float smoothing_angle)
				//{
				//	// For each original model vertex...
				//	for (size_t i = 0, iend = m_verts.size(); i != iend; ++i)
				//	{
				//		auto& vert = m_verts[i];

				//		//TODO
				//		// Build a set of all the norms this vertex could have
				//		// i.e. one for each face, one for each smooth edge
				//		// Partition the group of normals into sets where all
				//		// normals in each set are within a solid angle of 'smoothing_angle'
				//		// Create a degenerate vert for each normal set
				//		// Later, for each face, find the vertex with the normal
				//		// closest to the face normal
				//	}
				//}

				// Partition the edges for each vert into seperate groups
				void AssignSmoothingGroups(float smoothing_angle)
				{
					auto cos_angle_threshold = pr::Cos(smoothing_angle);
					bool no_changes;
					do
					{
						no_changes = true;
						for (auto& vert : m_verts)
						{
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
					}
					while (!no_changes);
				}

				// Generate the normal for each vertex, adding new vertices for seperate smoothing groups
				void CreateNormals(std::size_t new_vidx)
				{
					// Set the starting index for any new verts created
					new_vidx = std::max(new_vidx, m_verts.size());

					// Return the new index for vertex 'idx' in smoothing group 'face.m_grp'
					auto vert_index = [&](Face& face, size_t idx)
					{
						// m_verts[idx] is the head of a linked list of verts added for
						// each vertex with a unique normal.
						auto* vptr = &m_verts[idx];

						// Vertex has not yet been assigned a smoothing group
						if (vptr->m_grp == 0)
						{
							vptr->m_norm = face.normal(idx);
							vptr->m_grp = face.m_grp;
							return vptr->m_new_idx;// == vptr->m_orig_idx
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
						vptr->m_new_idx = new_vidx++;
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
			L gen(num_indices, indices, smoothing_angle, getv, new_vidx);

			// Output the new verts
			for (auto& vert : gen.m_verts)
			{
				// Skip verts that weren't used
				if (vert.m_grp == 0)
					continue;

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

		// Generate C++ code declaration from vertex, index, and nugget buffers
		template <typename TName, typename TVertCIter, typename TIdxCIter, typename TNugCIter, typename TOut>
		void GenerateModelCode(TName const& name, std::size_t num_verts, std::size_t num_indices, std::size_t num_nuggets, TVertCIter verts, TIdxCIter indices, TNugCIter nuggets, TOut& out, char const* indent = "")
		{
			out << "// " << name << "\n";

			// Write the model vertices
			if (num_verts != 0)
			{
				out << indent << "#pragma region Verts\n";
				out << indent << "static pr::rdr::Vert const verts[] =\n";
				out << indent << "{\n";
				for (std::size_t i = 0; i != num_verts; ++i)
				{
					auto& vert = *verts++;
					auto p = GetP(vert);
					auto c = GetC(vert);
					auto n = GetN(vert);
					auto t = GetT(vert);
					out << indent << "\t{"
						<< pr::Fmt("{%+ff, %+ff, %+ff, 1.0f}, ", p.x, p.y, p.z)
						<< pr::Fmt("{%+ff, %+ff, %+ff, %+ff}, ", c.r, c.g, c.b, c.a)
						<< pr::Fmt("{%+ff, %+ff, %+ff, 0.0f}, ", n.x, n.y, n.z)
						<< pr::Fmt("{%+ff, %+ff}", t.x, t.y)
						<< "},\n";
				}
				out << indent << "};\n";
				out << indent << "#pragma endregion\n";
			}

			// Write the model indices
			if (num_indices != 0)
			{
				out << indent << "#pragma region Indices\n";
				if      (sizeof(*indices) <= 2) out << indent << "static pr::uint16 const idxs[] =\n";
				else if (sizeof(*indices) <= 4) out << indent << "static pr::uint32 const idxs[] =\n";
				else if (sizeof(*indices) <= 8) out << indent << "static pr::uint64 const idxs[] =\n";
				else throw std::exception("Index type > 8 bytes is unsupported");
				
				auto wrap = 32U;
				out << indent << "{\n";
				for (std::size_t i = 0; i != num_indices; ++i)
				{
					auto& idx = *indices++;
					if ((i%wrap) == 0) out << indent << '\t'; else out << ' ';
					out << idx << ",";
					if ((i%wrap) == wrap-1) out << '\n'; else out << ' ';
				}
				if (num_indices != wrap-1) out << '\n';
				out << indent << "};\n";
				out << indent << "#pragma endregion\n";
			}

			// Write out model nuggets
			if (num_nuggets != 0)
			{
				out << indent << "#pragma region Nuggets\n";
				out << indent << "static pr::rdr::NuggetProps const nuggets[] = \n{\n";
				for (std::size_t i = 0; i != num_nuggets; ++i)
				{
					auto& nug = *nuggets++;
					out << indent << "pr::rdr::NuggetProps("
						<< "pr::rdr::EPrim(" << nug.m_topo << "), "
						<< "pr::rdr::EGeom(" << nug.m_geom << "), "
						<< "nullptr, "
						<< "pr::rdr::Range::make(" << std::begin(nug.m_vrange) << "," << std::end(nug.m_vrange) << "), "
						<< "pr::rdr::Range::make(" << std::begin(nug.m_irange) << "," << std::end(nug.m_irange) << ")"
						<< "),\n";
				}
				out << indent << "};\n";
				out << indent << "#pragma endregion\n";
			}
		}

		// Generate C++ code declaration from vertex and index buffers
		template <typename TName, typename TVertCIter, typename TIdxCIter, typename TOut>
		void GenerateModelCode(TName const& name, std::size_t num_verts, std::size_t num_indices, TVertCIter verts, TIdxCIter indices, TOut& out, char const* indent = "")
		{
			// Dummy nugget type
			struct Nug { int m_topo, m_geom; pr::Range<int> m_vrange, m_irange; };
			GenerateModelCode(name, num_verts, num_indices, 0, verts, indices, static_cast<Nug*>(nullptr), out, indent);
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
		PRUnitTest(pr_geometry_utility_generatenormals)
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
				Vert(v4(0.0f, 0.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(v4(1.0f, 0.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(v4(1.0f, 1.0f, 0.0f, 1.0f), pr::v4Zero),
				Vert(v4(0.0f, 1.0f, 0.0f, 1.0f), pr::v4Zero),
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

				pr::vector<Vert> vout;
				pr::vector<int> iout;
				GenerateNormals(PR_COUNTOF(faces), &faces[0], pr::DegreesToRadians(10.0f)
					,[&](size_t i)
					{
						assert(i < PR_COUNTOF(verts));
						return verts[i].m_pos;
					}
					,0
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
