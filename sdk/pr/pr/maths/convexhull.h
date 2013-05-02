//**********************************************
// ConvexHull
//  Copyright ©  Paul Ryland 2008
//**********************************************

#pragma once
#ifndef PR_MATHS_CONVEX_HULL_H
#define PR_MATHS_CONVEX_HULL_H

#include <iterator>
#include <algorithm>
#include "pr/common/assert.h"
#include "pr/common/alloca.h"
#include "pr/maths/maths.h"

#ifndef PR_DBG_CONVEX_HULL
#	define PR_DBG_CONVEX_HULL 0
#endif//PR_DBG_CONVEX_HULL
#if PR_DBG_CONVEX_HULL
#	include "pr/linedrawer/ldr_helper.h"
#endif//PR_DBG_CONVEX_HULL

namespace pr
{
	namespace impl
	{
		namespace hull
		{
			// A struct containing the parameters provided to ConvexHull()
			// Prevents passing lots of variables around between functions
			template <typename VertCont, typename VIter, typename FIter>
			struct HullData
			{
				enum { MaxVisFaceCount = 64 };
				typedef VIter VertIter;
				typedef FIter FaceIter;
				typedef typename std::iterator_traits<VIter>::value_type VIndex;

				FIter           m_fbegin;                            // Start of the face container
				FIter           m_flast;                             // One past the last face added to the face container
				FIter           m_fend;                              // End of the face container
				VIter           m_vbegin;                            // Start of the vert index container
				VIter           m_vhull_last;                        // One past the last vert index that is on the convex hull
				VIter           m_vnon_hull;                         // First vert index that is definitely not on the convex hull
				VIter           m_vend;                              // End of the vert index container
				VertCont const& m_vcont;                             // Vertex container
				v4*             m_hs_begin;                          // Start of a buffer of face normals with the same length as fend - fbegin
				v4*             m_hs_last;                           // Pointer into the half space buffer, equilavent position to flast
				v4*             m_hs_end;                            // End of the buffer of face normals
				int*            m_visible_face;                      // Pointer to one of the m_vis_face_buf's
				int             m_vis_face_buf1[MaxVisFaceCount];    // Double buffer for the
				int             m_vis_face_buf2[MaxVisFaceCount];    //  indices of visible faces
				int             m_vis_face_count;                    // The number of visible faces

				HullData(HullData const&);             // No copying
				HullData& operator=(HullData const&);  // No copying
				HullData(VertCont const& vcont, VIter vbegin, VIter vend, FIter fbegin, FIter fend, v4* half_space)
				:m_vcont(vcont)
				,m_vbegin(vbegin)
				,m_vhull_last(vbegin)
				,m_vnon_hull(vend)
				,m_vend(vend)
				,m_fbegin(fbegin)
				,m_flast(fbegin)
				,m_fend(fend)
				,m_hs_begin(half_space)
				,m_hs_last(half_space)
				,m_hs_end(half_space + (fend - fbegin))
				,m_visible_face(m_vis_face_buf1)
				,m_vis_face_count(0)
				{}
				void SwapVisFaceDblBuffer()
				{
					m_visible_face = m_visible_face == m_vis_face_buf1 ? m_vis_face_buf2 : m_vis_face_buf1;
				}
			};

			#if PR_DBG_CONVEX_HULL
			void const* g_data;
			template <typename HData> void DumpFaces(HData const& data);
			template <typename HData> void DumpVerts(HData const& data);			
			#endif//PR_DBG_CONVEX_HULL

			// Add a face to the face container
			template <typename HData>
			inline void AddFace(HData& data, typename HData::VIndex a, typename HData::VIndex b, typename HData::VIndex c)
			{
				PR_ASSERT(PR_DBG_CONVEX_HULL, a != b && b != c && c != a, "Should not be adding degenerate faces");
				PR_ASSERT(PR_DBG_CONVEX_HULL, !(data.m_flast == data.m_fend), "Shouldn't be trying to add faces if there isn't room");

				// Set the face in the face container
				pr::hull::SetFace(*data.m_flast, a, b, c);
				++data.m_flast;
					
				// Record the half space that this face represents
				v4 const& A = data.m_vcont[*(data.m_vbegin + a)];
				v4 e0 = data.m_vcont[*(data.m_vbegin + b)] - A;
				v4 e1 = data.m_vcont[*(data.m_vbegin + c)] - A;
				v4& plane = *data.m_hs_last;
				plane   = GetNormal3(Cross3(e0, e1));
				plane.w = -Dot3(plane, A);
				++data.m_hs_last;
	
				PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
			}

			// Initialise the convex hull by finding a shape with volume from the bounding verts
			// This function is set up to return false if any degenerate cases are detected.
			template <typename HData>
			bool InitHull(HData& data)
			{
				PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::FileOutput ldr_extm("C:/DeleteMe/hull_extremeverts.pr_script", true); ldr_extm.clear());
				
				// A minimum of 4 verts are needed for a hull with volume
				if( data.m_vend - data.m_vbegin < 4 )
					return false;

				// Check that there is room for the initial 4 faces
				if( data.m_fend - data.m_flast < 4 )
					return false;

				// Scan through and find the min/max verts on the Z axis
				typename HData::VertIter vmin = data.m_vbegin, vmax = data.m_vbegin;
				{
					float dmin =  maths::float_max;
					float dmax = -maths::float_max;
					for( typename HData::VertIter i = data.m_vbegin; i != data.m_vend; ++i )
					{
						float d = Dot3(v4ZAxis, data.m_vcont[*i]);
						if( d < dmin ) {dmin = d; vmin = i;}
						if( d > dmax ) {dmax = d; vmax = i;}
					}

					// If the span is zero then all verts must lie
					// in a plane parallel to the XY plane.
					if( dmax - dmin < maths::tiny )
						return false;
				}
				PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::Line("zaxis", "FF0000FF", data.m_vcont[*vmin], data.m_vcont[*vmax], ldr_extm));
				
				// Use these extreme verts as the Z axis
				v4 zaxis = data.m_vcont[*vmax] - data.m_vcont[*vmin];

				// Move these vert indices to the convex hull end of the range.
				// Ensure vmin is less than vmax, this prevents problems with swapping.
				if( vmax < vmin ) { Swap(*vmin,*vmax); Swap(vmin,vmax); }
				Swap(*vmin, *data.m_vhull_last++);
				Swap(*vmax, *data.m_vhull_last++);

				v4 const& zmin = data.m_vcont[*data.m_vbegin];
				float zaxis_lensq = Length3Sq(zaxis);
				
				{// Find the most radially distant vertex from the zaxis
					float dmax = 0.0f;
					for( typename HData::VertIter i = data.m_vhull_last; i != data.m_vend; ++i )
					{
						v4 vert = data.m_vcont[*i] - zmin;
						float d = Length3Sq(vert) - Sqr(Dot3(vert, zaxis)) / zaxis_lensq;
						if( d > dmax ) {dmax = d; vmax = i;}
					}
					
					// If all verts lie on the zaxis...
					if( dmax < maths::tiny )
						return false;
				}
				PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::Line("yaxis", "FF00FF00", zmin, data.m_vcont[*vmax], ldr_extm));

				// Choose a perpendicular axis
				v4 axis = Cross3(zaxis, data.m_vcont[*vmax] - zmin);

				// Move 'vmax' to the convex hull end of the range.
				Swap(*vmax, *data.m_vhull_last++);

				bool flip = false;

				{// Find the vert with the greatest distance along 'axis'
					float dmax = 0.0f;
					for( typename HData::VertIter i = data.m_vhull_last; i != data.m_vend; ++i )
					{
						float d = Dot3(axis, data.m_vcont[*i] - zmin);
						if( Abs(d) > dmax ) {dmax = Abs(d); vmax = i; flip = d < 0.0f;}
					}
					
					// If all verts lie on in the plane...
					if( dmax < maths::tiny )
						return false;
				}
				PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::Line("xaxis", "FFFF0000", zmin, data.m_vcont[*vmax], ldr_extm));

				// Move 'vmax' to the convex hull end of the range.
				Swap(*vmax, *data.m_vhull_last++);

				// Generate the starting shape from the four hull verts we've found.
				if( flip )
				{
					AddFace(data, 0, 1, 2);
					AddFace(data, 0, 2, 3);
					AddFace(data, 0, 3, 1);
					AddFace(data, 3, 2, 1);
				}
				else
				{
					AddFace(data, 0, 2, 1);
					AddFace(data, 0, 3, 2);
					AddFace(data, 0, 1, 3);
					AddFace(data, 1, 2, 3);
				}
				return true;
			}

			// Move vert indices that are inside the current hull to the 'non-hull' end of the range.
			// Records the vertex with the greatest distance from any face in the hull, this must be
			// a vertex on the convex hull because it is the extreme vertex in the direction of the
			// face normal. Returns this extreme vert, also caches the indices of the faces that can
			// see this max vert.
			template <typename HData>
			typename HData::VertIter PartitionVerts(HData& data)
			{
				float max_dist = 0.0f;
				data.m_vis_face_count = 0;
				typename HData::VertIter max_vert = data.m_vnon_hull;
				for( typename HData::VertIter v = data.m_vhull_last; v != data.m_vnon_hull; )
				{
					// Find the maximum distance of 'v' from the faces of the hull
					float dist = 0.0f;
					int vis_count = 0, face_index = 0;
					for( v4 const* plane = data.m_hs_begin; plane != data.m_hs_last; ++plane, ++face_index )
					{
						PR_ASSERT(PR_DBG_CONVEX_HULL, data.m_vcont[*v].w == 1.0f, "Should be finding the convex hull of positions, not directions");
						float d = Dot4(*plane, data.m_vcont[*v]);
						if( d <= 0.0f ) continue; // behind 'plane'
						dist = Max(dist, d);
						if (vis_count < HData::MaxVisFaceCount)
							data.m_visible_face[vis_count] = face_index;
						++vis_count;
					}

					// If this vert is within all half spaces then it's not a hull vert
					if( dist == 0.0f )
					{
						Swap(*v, *(--data.m_vnon_hull));
						continue;
					}

					// If this vert has the greatest distance from a face, record it.
					if( dist > max_dist )
					{
						max_dist = dist;
						max_vert = v;
						data.m_vis_face_count = vis_count;
						data.SwapVisFaceDblBuffer();
					}
					++v;
				}
				data.SwapVisFaceDblBuffer();
				return max_vert;
			}

			// A structure used to keep track of the perimeter edges
			// as faces are removed from the hull.
			template <typename HData>
			struct Perimeter
			{
				struct Edge	 { typename HData::VIndex m_i0, m_i1; };
				Edge *m_begin, *m_last, *m_end;
				
				Perimeter(Edge* buf, int count)
				:m_begin(buf)
				,m_last(m_begin)
				,m_end(m_begin + count)
				{}
				void AddEdge(typename HData::VIndex i0, typename HData::VIndex i1)
				{
					for( Edge* e = m_begin; e != m_last; ++e )
					{
						if( !(e->m_i0 == i1 && e->m_i1 == i0) ) continue;
						*e = *(--m_last); // erase 'e'
						PR_EXPAND(PR_DBG_CONVEX_HULL, DumpEdges());
						return;
					}
					PR_ASSERT(PR_DBG_CONVEX_HULL, m_last != m_end, "");
					m_last->m_i0 = i0;
					m_last->m_i1 = i1;
					++m_last;
					PR_EXPAND(PR_DBG_CONVEX_HULL, DumpEdges());
				}
				
				#if PR_DBG_CONVEX_HULL
				void DumpEdges()
				{
					HData const& data = *(HData const*)g_data;
					ldr::FileOutput file("C:/DeleteMe/hull_edges.pr_script"); file.Open();
					ldr::GroupStart("Edges", file);
					for( Edge* i = m_begin; i != m_last; ++i )
					{
						ldr::Line("edge", "FFFFFFFF",
							data.m_vcont[data.m_vbegin[i->m_i0]],
							data.m_vcont[data.m_vbegin[i->m_i1]], file);
					}
					ldr::GroupEnd(file);
				}
				#endif//PR_DBG_CONVEX_HULL
			};

			// Expand the convex hull to include 'v'.
			template <typename HData>
			void GrowHull(HData& data, typename HData::VertIter v)
			{
				typedef typename std::iterator_traits<typename HData::VertIter>::value_type VIndex;

				// Move 'v' into the convex hull
				Swap(*v, *data.m_vhull_last);
				typename HData::VIndex v_idx = static_cast<typename HData::VIndex>(data.m_vhull_last - data.m_vbegin);
				v4 const& vert = data.m_vcont[*data.m_vhull_last];
				++data.m_vhull_last;
			
				// Allocate stack memory for the edges remaining when the faces that can
				// see 'v' are removed. The number of perimeter edges will ultimately be
				// 2 * vis_face_count, however in the worst case we can receive the faces
				// in an order that doesn't sharing any edges so allow for 3 edges for each face.
				int max_edge_count = 3 * data.m_vis_face_count;
				Perimeter<HData> perimeter(PR_ALLOCA_POD(Perimeter<HData>::Edge, max_edge_count), max_edge_count);

				// If the number of visible faces is less than the size of 
				// the visible face cache then we don't need to retest the faces 
				if( data.m_vis_face_count <= HData::MaxVisFaceCount )
				{
					for( int* i = data.m_visible_face + data.m_vis_face_count; i-- != data.m_visible_face; )
					{
						typename HData::FaceIter	face  = data.m_fbegin   + *i;
						v4*							plane = data.m_hs_begin + *i;
				
						typename HData::VIndex a, b, c;
						pr::hull::GetFace(*face, a, b, c);

						// Add the edges of 'face' to the edge stack
						perimeter.AddEdge(a, b);
						perimeter.AddEdge(b, c);
						perimeter.AddEdge(c, a);

						// Erase 'face'
						Swap(*face , *(--data.m_flast));
						Swap(*plane, *(--data.m_hs_last));
						PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
					}
				}
				// Otherwise we need to test all faces again for being able to see 'v'
				else
				{
					v4* plane = data.m_hs_begin;
					for( typename HData::FaceIter face = data.m_fbegin; !(face == data.m_flast); )
					{
						// Ignore faces that face away or are edge on to 'v'
						if( Dot4(*plane, vert) <= 0.0f )
						{
							++face;
							++plane;
							continue;
						}
					
						typename HData::VIndex a, b, c;
						pr::hull::GetFace(*face, a, b, c);

						// Add the edges of 'face' to the edge stack
						perimeter.AddEdge(a, b);
						perimeter.AddEdge(b, c);
						perimeter.AddEdge(c, a);

						// Erase 'face'
						Swap(*face , *(--data.m_flast));
						Swap(*plane, *(--data.m_hs_last));
						PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
					}
				}
	
				// Add faces for each remaining edge
				for( Perimeter<HData>::Edge const* edge = perimeter.m_begin; edge != perimeter.m_last; ++edge )
				{
					AddFace(data, v_idx, edge->m_i0, edge->m_i1);
				}
			}

			// Debugging functions *******************************************************************
			#if PR_DBG_CONVEX_HULL
			template <typename HData>
			void DumpFaces(HData const& data)
			{
				ldr::FileOutput file("C:/DeleteMe/hull_faces.pr_script"); file.Open();
				ldr::GroupStart("Faces", file);
				for( typename HData::FaceIter f = data.m_fbegin; !(f == data.m_flast); ++f )
				{
					typename HData::VIndex i0,i1,i2; pr::hull::GetFace(*f, i0, i1, i2);
					ldr::Triangle("face", "FF00FF00"
						,data.m_vcont[*(data.m_vbegin + i0)]
						,data.m_vcont[*(data.m_vbegin + i1)]
						,data.m_vcont[*(data.m_vbegin + i2)]
						,file);
				}
				ldr::GroupEnd(file);
			}
			template <typename HData>
			void DumpVerts(HData const& data)
			{
				ldr::FileOutput file("C:/DeleteMe/hull_verts.pr_script"); file.Open();
				ldr::GroupStart("Verts", file);
				for( typename HData::VertIter v = data.m_vbegin; v != data.m_vend; ++v )
				{
					if     ( v < data.m_vhull_last )	ldr::Box("v", "FFA00000", data.m_vcont[*v], 0.05f, file);
					else if( v < data.m_vnon_hull )		ldr::Box("v", "FF00A000", data.m_vcont[*v], 0.05f, file);
					else								ldr::Box("v", "FF0000A0", data.m_vcont[*v], 0.05f, file);
				}
				ldr::GroupEnd(file);
			}
			#endif//PR_DBG_CONVEX_HULL
		}//namespace hull
	}//namespace impl

	// Generate the convex hull of a point cloud.
	// 'vcont' is some container of verts that supports operator [] (e.g. v4 const*)
	// 'vfirst','vlast' are a range of vertex indices into 'vcont' (i.e. the point cloud)
	// 'ffirst','flast' are an output iterator range to faces
	// This function partitions the range of vert indices with those on the convex hull at
	// the start of the range. Returns true if the convex hull was generated. If false is
	// returned and vert_count and face_count are not zero, then a convex closed polytope was
	// generated.
	// Notes:
	// -A point cloud of N verts will have a convex hull with at most 2*(N-2) faces.
	// -The faces output by this function are not necessarily the hull faces until the algorithm
	//   has completed. 'ffirst' and 'flast' should point to a container of faces. Faces in this
	//   container may be written to and read from several times.
	// -The indices in the faces refer to the positions in the vert index container. This allows
	//   new or old vert indices to be used. For new indices, map the verts using the vert index
	//   buffer. For old indices use the vert index bufer as a look up map.
    // -You may need to provide the "hull::SetFace" and "hull::GetFace" functions for your face 
    //   and vert index types
	template <typename VertCont, typename VIter, typename FIter>
	bool ConvexHull(VertCont const& vcont, VIter vbegin, VIter vend, FIter fbegin, FIter fend, std::size_t& vert_count, std::size_t& face_count)
	{
		using namespace impl::hull;
		
		vert_count = 0;
		face_count = 0;

		// Allocate stack or heap memory for the normal directions of the faces.
		v4* half_space = PR_MALLOCA(half_space, v4, fend - fbegin);

		// Add all of the parameters to a struct to save on passing them around between function calls
		HullData<VertCont, VIter, FIter> data(vcont, vbegin, vend, fbegin, fend, half_space);
		PR_EXPAND(PR_DBG_CONVEX_HULL, g_data = &data);
		PR_EXPAND(PR_DBG_CONVEX_HULL, DumpVerts(data));
		PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
		
		// Find an initial volume
		if( !InitHull(data) )
			return false;

		// Test the unclassified verts and move any that are within the convex hull to
		// the 'non-hull' end of the range. 'v' is the vert most not in the convex hull.
		// 'vis_count' is the number of faces of the current hull that can "see" 'v'
		VIter v = PartitionVerts(data);		

		// While there are unclassified verts remaining
		PR_EXPAND(PR_DBG_CONVEX_HULL, int iteration = 0);
		while( data.m_vhull_last != data.m_vnon_hull )
		{
			// Check there is enough space in the face range to expand the convex hull.
			// Since the hull is convex, the faces that can see 'v' will form one connected
			// polygon. This means the number of perimeter edges will be 2 + vis_face_count.
			// Since we're also removing vis_face_count faces, the number of new faces added
			// will be 2.
			if( data.m_fend - data.m_flast < 2 )
				break;

			PR_EXPAND(PR_DBG_CONVEX_HULL, DumpVerts(data));
			PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
			PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::FileOutput extm("C:/DeleteMe/hull_extremevert.pr_script"));
			PR_EXPAND(PR_DBG_CONVEX_HULL, ldr::Box("extreme", "FFFFFFFF", data.m_vcont[*v], 0.07f, extm));

			// Expand the convex hull to include 'v'
			GrowHull(data, v);

			PR_EXPAND(PR_DBG_CONVEX_HULL, DumpVerts(data));
			PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));

			// Test the unclassified verts and move any that are within the convex hull to
			// the 'non-hull' end of the range. Find a new most not in the convex hull vertex.
			v = PartitionVerts(data);
			PR_EXPAND(PR_DBG_CONVEX_HULL, ++iteration);
		}

		vert_count = static_cast<int>(data.m_vhull_last - data.m_vbegin);
		face_count = static_cast<int>(data.m_flast      - data.m_fbegin);
		PR_EXPAND(PR_DBG_CONVEX_HULL, DumpVerts(data));
		PR_EXPAND(PR_DBG_CONVEX_HULL, DumpFaces(data));
		return data.m_vhull_last == data.m_vnon_hull;
	}

	// Helper overloads *********************************************************************************
	namespace hull
	{
		// An object for iterating over faces
		template <int StrideInBytes, typename VIndex> union Face
		{
			typedef VIndex VIndex;
			VIndex        vindex(int i) const  { return m_vindex[i]; }
			VIndex&       vindex(int i)        { return m_vindex[i]; }
			VIndex        m_vindex[3];
			unsigned char m_stride[StrideInBytes];
		};
		template <int StrideInBytes, typename VIndex> inline Face<StrideInBytes, VIndex>* FaceIter(VIndex* index)
		{
			return reinterpret_cast<Face<StrideInBytes, VIndex>*>(index);
		}
	
		// Get/Set the indices of a face
		template <typename Face, typename VIndex> inline void SetFace(Face& face, VIndex a, VIndex b, VIndex c)
		{
			face.vindex(0) = static_cast<Face::VIndex>(a);
			face.vindex(1) = static_cast<Face::VIndex>(b);
			face.vindex(2) = static_cast<Face::VIndex>(c);
		}
		template <typename Face, typename VIndex> inline void GetFace(Face const& face, VIndex& a, VIndex& b, VIndex& c)
		{
			a = static_cast<VIndex>(face.vindex(0));
			b = static_cast<VIndex>(face.vindex(1));
			c = static_cast<VIndex>(face.vindex(2));
		}
	}

	// Overload for faces given as an array of index triples.
	template <typename VertCont, typename VIndex>
	inline bool ConvexHull(VertCont const& vcont, VIndex* vfirst, VIndex* vlast, VIndex* ffirst, VIndex* flast, std::size_t& vert_count, std::size_t& face_count)
	{
		typedef typename hull::Face<3*sizeof(VIndex), typename VIndex> Face;
		Face* face_first = hull::FaceIter<3*sizeof(VIndex)>(ffirst);
		Face* face_last  = hull::FaceIter<3*sizeof(VIndex)>(flast);
		return ConvexHull<VertCont, VIndex*, Face*>(vcont, vfirst, vlast, face_first, face_last, vert_count, face_count);
	}

	// Predicate for sorting PHv4's based on their 'w' component
	struct Pred_VertWSort
	{
		bool operator () (v4 const& lhs, v4 const& rhs) const {return lhs.w < rhs.w;}
	};

	// Overload that reorders the verts in the vertex container
	// 'vcont' is the container of verts supporting operator [] (e.g v4*)
	// 'num_verts' is the number of verts in 'vcont'
	// Other parameters as for the main version of this function.
	template <typename VertCont, typename FIter>
	inline bool ConvexHull(VertCont& vcont, std::size_t num_verts, FIter fbegin, FIter fend, std::size_t& vert_count, std::size_t& face_count)
	{
		// Create an index buffer for the verts
		int* index = PR_MALLOCA(index, int, num_verts);
		for( int i = 0; i != int(num_verts); ++i ) index[i] = i;
		bool result = ConvexHull<VertCont, int*, FIter>(vcont, index, index+num_verts, fbegin, fend, vert_count, face_count);

		// Write the indices into the 'w' component of the verts
		// Then sort on 'w', then restore the 'w' component to '1.0f'
		for( int* i = index, *i_end = i+ num_verts; i != i_end; ++i )
			vcont[*i].w = static_cast<float>(i - index);
		std::sort(&vcont[0], &vcont[0] + num_verts, Pred_VertWSort());
		for( v4* i = &vcont[0], *i_end = i + num_verts; i != i_end; ++i )
			i->w = 1.0f;

		return result;
	}

	// Overload that reorders the verts in the vertex container
	// and for faces given as an array of index triples.
	template <typename VertCont, typename VIndex>
	inline bool ConvexHull(VertCont& vcont, std::size_t num_verts, VIndex* ffirst, VIndex* flast, std::size_t& vert_count, std::size_t& face_count)
	{
		typedef typename hull::Face<3*sizeof(VIndex), typename VIndex> Face;
		Face* face_first = hull::FaceIter<3*sizeof(VIndex)>(ffirst);
		Face* face_last  = hull::FaceIter<3*sizeof(VIndex)>(flast);
		return ConvexHull<VertCont, Face*>(vcont, num_verts, face_first, face_last, vert_count, face_count);
	}
}//namespace pr

#endif//PR_MATHS_CONVEX_HULL_H
