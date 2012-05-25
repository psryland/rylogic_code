//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PHYSICS_SHAPE_CONVEX_DECOMPOSE_H
#define PR_PHYSICS_SHAPE_CONVEX_DECOMPOSE_H

#include "PR/Common/StdVector.h"
#include "PR/Common/PRArray.h"
#include "PR/Physics/Types/Types.h"
#include "PR/Physics/Types/Forward.h"

namespace pr
{
	namespace ph
	{
		namespace convex_decompose
		{
			typedef std::size_t				TIndex;
			typedef std::size_t				TSetId;
			//typedef pr::Array<TIndex, 20>	TNbrs;
			typedef std::vector<TIndex>		TNbrs;
			TIndex const InvalidVertIndex	= 0xFFFFFFFF;

			// The basic vertex type used for convex decomposition
			struct Vert
			{
				v4			m_pos;			// Position of the vertex
				TNbrs		m_nbrs;			// The neighbours of this vertex
				v4			m_delta;		// The vector from a split plane to this vert (only valid for zdv verts for now)
				bool		m_zdv;			// True if this is a zero distance vert (see cpp)
				TSetId		m_set_id;		// Used to group the vertex into sub meshes
				TIndex		m_link_index;	// Link used for list of duplicates and list of zero distance verts
				TIndex		m_next;			// Link to the next vertex in the mesh, used by Mesh only
			};
			
			// A container for the vertices
			struct VertContainer
			{
				Vert const& operator [](TIndex idx) const	{ return m_buffer[idx]; }
				Vert&       operator [](TIndex idx)			{ return m_buffer[idx]; }
				TIndex		add(Vert const& v)				{ m_buffer.push_back(v); return m_buffer.size() - 1; }
				void		clear()							{ m_buffer.clear(); }
				void		reserve(std::size_t num)		{ m_buffer.reserve(num); }
				std::size_t size() const					{ return m_buffer.size(); }

			private:
				typedef pr::Array<Vert, 50> TVerts;
				TVerts m_buffer; // This could be replaced with a block of new'd memory
			};

			// A structure representing an edge in the mesh
			struct Edge
			{
				TIndex	m_i0;
				TIndex	m_i1;
				TIndex	m_iter;			// Used when iterating over edges, index of the next nbr after m_i1
				TSetId	m_set_id;		// The set id of the verts that this edge lies between
				float	m_concavity;	// A measure of how concave an edge is. Approximately the maximum distance to the convex hull
				v4		m_bisect_dir;	// A favourable direction for bisecting a concave edge
			};

			// A structure representing a face of the mesh
			struct Face
			{
				TIndex	m_i0;		// Vertex indices for a face of the mesh
				TIndex	m_i1;
				TIndex	m_i2;
				TIndex	m_iter;		// Used during iteration over faces
			};

			// A mesh is a linked list of verts within a contiguous array
			struct Mesh
			{
				Mesh() : m_vert(0), m_first(InvalidVertIndex), m_count(0) {}
				explicit Mesh(VertContainer* vert, TIndex first = InvalidVertIndex, std::size_t count = 0) : m_vert(vert), m_first(first), m_count(count) {}
				Mesh(Mesh const& mesh, TIndex first, std::size_t count) : m_vert(mesh.m_vert), m_first(first), m_count(count) {}
				
				std::size_t	Add(Vert const& v)								{ std::size_t idx = m_vert->add(v); (*m_vert)[idx].m_next = m_first; m_first = idx; ++m_count; return idx; }
				void		Clear()											{ m_vert->clear(); m_first = InvalidVertIndex; m_count = 0; }
				void		Reserve(std::size_t num)						{ m_vert->reserve(num); }
				void		Copy(Mesh const& m)								{ *m_vert = *m.m_vert; m_first = m.m_first; m_count = m.m_count; }
				TIndex		MaxIndex() const								{ return static_cast<TIndex>(m_vert->size()); }
				
				// Iterate over the verts of this mesh
				Vert const&	vert(TIndex idx) const							{ return (*m_vert)[idx]; }
				Vert&		vert(TIndex idx)								{ return (*m_vert)[idx]; }
				Vert const* vert_first() const								{ return (m_first   == InvalidVertIndex) ? 0 : &(*m_vert)[m_first]; } // These 'if's should be able to be removed
				Vert*		vert_first()									{ return (m_first   == InvalidVertIndex) ? 0 : &(*m_vert)[m_first]; } // Something like: (m_first != InvalidVertIndex)*(m_vert + m_first)
				Vert const* vert_next(Vert const* v) const					{ return (v->m_next == InvalidVertIndex) ? 0 : &(*m_vert)[v->m_next]; }
				Vert*		vert_next(Vert const* v)						{ return (v->m_next == InvalidVertIndex) ? 0 : &(*m_vert)[v->m_next]; }

				// Iterate over the indices of the verts of this mesh
				TIndex		idx(Vert const* v) const						{ return v - &(*m_vert)[0]; }
				TIndex		idx_first() const								{ return m_first; }
				TIndex		idx_next(TIndex idx) const						{ return (*m_vert)[idx].m_next; }

			private:
				// Store the verts in a contiguous array so that
				// neighbour indices can be absolute indices into the buffer
				VertContainer*	m_vert;		// Pointer to the buffer containing the verts
				TIndex			m_first;	// Index of the first vertex for the mesh we represent
				std::size_t		m_count;	// The number of verts in this mesh
			};
			typedef pr::Array<Mesh, 10> TMesh;

			// Helper functions for iterating over faces/edges of the mesh
			convex_decompose::Edge* EdgeFirst(convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge);
			convex_decompose::Edge* EdgeNext (convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge);
			convex_decompose::Edge*	EdgeIter (convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge, TIndex i0, TIndex i1);
			convex_decompose::Face* FaceFirst(convex_decompose::Mesh const& mesh, convex_decompose::Face& face);
			convex_decompose::Face* FaceNext (convex_decompose::Mesh const& mesh, convex_decompose::Face& face);

			// Generate a mesh that can be decomposed into convex pieces
			void CreateMesh(v4 const* verts, std::size_t num_verts, TIndex const* indices, std::size_t num_faces, convex_decompose::Mesh& mesh);

		}//namespace convex_decompose

		// Decompose a mesh into convex pieces
		void ConvexDecompose(convex_decompose::Mesh const& mesh, convex_decompose::VertContainer& vert_container, convex_decompose::TMesh& polytopes);

	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_SHAPE_CONVEX_DECOMPOSE_H
