//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Physics/Shape/ConvexDecompose/ShapeConvexDecompose.h"
#include "PR/Maths/Triangulate.h"

// TODO:
//	Try improving FindOppositeVerts, should be able to search for a pair that is common to both verts
//	Get rid of std::vectors

using namespace pr;
using namespace pr::ph;
using namespace pr::ph::convex_decompose;

#define PR_DBG_CVX_DECOM 0

namespace pr { namespace ph { namespace convex_decompose
{
	typedef std::vector<Edge> TEdges;
	struct BySetId   { bool operator()	(convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs) const { return lhs.m_set_id < rhs.m_set_id; } };
	struct Concavity { bool operator()	(convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs) const { return lhs.m_concavity < rhs.m_concavity; } };
	bool		operator ==				(convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs);
	bool		operator <				(convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs);
	bool		PointOnPlane			(Plane const& split_plane, v4 const& point, float& dist);
	void		Replace					(TNbrs& nbrs, TIndex old_idx, TIndex new_idx);
	void		Insert					(TNbrs& nbrs, TIndex where_idx, TIndex new_idx, bool after);
	void		Remove					(TNbrs& nbrs, TIndex idx);
	void		RemoveNonSetElements	(convex_decompose::Mesh const& mesh, TNbrs& nbrs, TSetId set_id);

	void		ConvexDecompose			(convex_decompose::Mesh& mesh, convex_decompose::TMesh& polytopes, TEdges& concave_edges, convex_decompose::Edge const& most_concave);
	void		TriangulateHoles		(convex_decompose::Mesh& mesh, Plane split_plane);
	void		LinkSubMeshes			(convex_decompose::Mesh& mesh, TIndex* sub_meshes, std::size_t* vert_counts);
	void		SeparateSubMeshes		(convex_decompose::Mesh& mesh, TIndex first_zdv_idx, TEdges& concave_edges);
	void		RemoveZDVNeighbours		(convex_decompose::Mesh& mesh, TIndex first_zdv_idx);
	std::size_t	GroupVerts				(convex_decompose::Mesh& mesh, TSetId& max_set_id);
	void		DivideMesh				(convex_decompose::Mesh& mesh, Plane const& split_plane, TEdges& concave_edges);
	TIndex		SplitEdge				(convex_decompose::Mesh& mesh, convex_decompose::Edge const& edge, float t);
	void		FindOppositeVerts		(convex_decompose::Mesh const& mesh, Edge const& edge, TIndex& lhs, TIndex& rhs, std::size_t& lhs_i, std::size_t& rhs_i);
	void		FindSplitPlane			(convex_decompose::Mesh const& mesh, Plane& split_plane, TEdges const& concave_edges, convex_decompose::Edge const& most_concave);
	bool		FindConcaveEdges		(convex_decompose::Mesh const& mesh, TEdges& concave_edges, Edge& most_concave);
	void		MeasureConcavity		(convex_decompose::Mesh const& mesh, Edge& edge);
	void		AddPolytope				(convex_decompose::Mesh const& mesh, convex_decompose::TMesh& polytopes);
	
	#if PR_DBG_CVX_DECOM == 1
	void	DumpMesh (convex_decompose::Mesh const&	mesh, char const* colour);
	void	DumpEdges(convex_decompose::Mesh const&	mesh);
	void	DumpEdges(convex_decompose::Mesh const& mesh, TEdges const& edges, char const* colour, bool show_bisect);
	#endif//PR_DBG_CVX_DECOM == 1

	float const PointOnPlaneTolerance	= 0.1f;//maths::tiny;//
	float const	ConcaveTolerance		= 0.01f;
}}}

// Iterate over the edges in a mesh visiting each edge only once
Edge* pr::ph::convex_decompose::EdgeFirst(convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge)
{
	edge.m_i0 = mesh.idx_first();
	edge.m_iter = 0;
	return EdgeNext(mesh, edge);
}
Edge* pr::ph::convex_decompose::EdgeNext(convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge)
{
	PR_ASSERT(PR_DBG_PHYSICS, edge.m_iter <= mesh.vert(edge.m_i0).m_nbrs.size());

	TNbrs const* nbrs = &mesh.vert(edge.m_i0).m_nbrs;
	std::size_t nbrs_size = nbrs->size();
	for(;;)
	{
		for( ; edge.m_iter != nbrs_size && (*nbrs)[edge.m_iter] < edge.m_i0; ++edge.m_iter ) {}
		if( edge.m_iter != nbrs_size ) break;
		
		edge.m_i0 = mesh.idx_next(edge.m_i0);
		if( edge.m_i0 == InvalidVertIndex ) return 0;

		nbrs = &mesh.vert(edge.m_i0).m_nbrs;
		nbrs_size = nbrs->size();
		edge.m_iter = 0;
	}
	edge.m_i1 = (*nbrs)[edge.m_iter++];
	return &edge;
}
Edge* pr::ph::convex_decompose::EdgeIter(convex_decompose::Mesh const& mesh, convex_decompose::Edge& edge, TIndex i0, TIndex i1)
{
	if( i0 < i1 )	{ edge.m_i0 = i0; edge.m_i1 = i1; }
	else			{ edge.m_i0 = i1; edge.m_i1 = i0; }
	TNbrs const& nbrs = mesh.vert(edge.m_i0).m_nbrs;
	edge.m_iter = std::find(nbrs.begin(), nbrs.end(), edge.m_i1) - nbrs.begin() + 1;
	PR_ASSERT(PR_DBG_PHYSICS, edge.m_iter <= nbrs.size());
	return &edge;
}

// Iterate over the faces in a mesh visiting each face only once
convex_decompose::Face* pr::ph::convex_decompose::FaceFirst(convex_decompose::Mesh const& mesh, convex_decompose::Face& face)
{
	face.m_i0 = mesh.idx_first();
	face.m_iter = 0;
	return FaceNext(mesh, face);
}
convex_decompose::Face* pr::ph::convex_decompose::FaceNext(convex_decompose::Mesh const& mesh, convex_decompose::Face& face)
{
	PR_ASSERT(PR_DBG_PHYSICS, face.m_iter <= mesh.vert(face.m_i0).m_nbrs.size());

	TNbrs const* nbrs = &mesh.vert(face.m_i0).m_nbrs;
	std::size_t  nbrs_size = nbrs->size();
	for(;;)
	{
		for( ; face.m_iter != nbrs_size && ((*nbrs)[face.m_iter] < face.m_i0 || (*nbrs)[(face.m_iter + 1) % nbrs_size] < face.m_i0); ++face.m_iter ) {}
		if( face.m_iter != nbrs_size ) break;

		face.m_i0 = mesh.idx_next(face.m_i0);
		if( face.m_i0 == InvalidVertIndex ) return 0;

		nbrs = &mesh.vert(face.m_i0).m_nbrs;
		nbrs_size = nbrs->size();
		face.m_iter = 0;
	}
	face.m_i1 = (*nbrs)[ face.m_iter                 ];
	face.m_i2 = (*nbrs)[(face.m_iter + 1) % nbrs_size];
	face.m_iter++;
	return &face;
}

// Edge binary operators
inline bool pr::ph::convex_decompose::operator == (convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs)
{
	PR_ASSERT(PR_DBG_PHYSICS, lhs.m_i0 < lhs.m_i1 && rhs.m_i0 < rhs.m_i1);
	return lhs.m_i0 == rhs.m_i0 && lhs.m_i1 == rhs.m_i1;
}
inline bool pr::ph::convex_decompose::operator <  (convex_decompose::Edge const& lhs, convex_decompose::Edge const& rhs)
{
	PR_ASSERT(PR_DBG_PHYSICS, lhs.m_i0 < lhs.m_i1 && rhs.m_i0 < rhs.m_i1);
	if( lhs.m_i0 == rhs.m_i0 ) return lhs.m_i1 < rhs.m_i1;
	return lhs.m_i0 < rhs.m_i0;
}

// Returns true if 'point' is considered to lie on 'split_plane'
// Also returns the actual distance to the plane in 'dist'
inline bool pr::ph::convex_decompose::PointOnPlane(Plane const& split_plane, v4 const& point, float& dist)
{
	dist = Distance_PointToPlane(point, split_plane);
	return Abs(dist) <= PointOnPlaneTolerance;
}

// Replace 'old_idx' with 'new_idx' in a list of neighbours
// Each index in a list of neighbours should be unique so we only have to replace one index
inline void pr::ph::convex_decompose::Replace(TNbrs& nbrs, TIndex old_idx, TIndex new_idx)
{
	if( old_idx == new_idx ) return;
	PR_ASSERT(PR_DBG_PHYSICS, std::find(nbrs.begin(), nbrs.end(), old_idx) != nbrs.end());	// 'old_idx' should be a neighbour index
	PR_ASSERT(PR_DBG_PHYSICS, std::find(nbrs.begin(), nbrs.end(), new_idx) == nbrs.end());	// Shouldn't be adding an index that is already there
	*std::find(nbrs.begin(), nbrs.end(), old_idx) = new_idx;
}

// Insert 'new_idx' into the list of neighbours at the
// position of 'where_idx' either before or after (depending on 'after')
inline void pr::ph::convex_decompose::Insert(TNbrs& nbrs, TIndex where_idx, TIndex new_idx, bool after)
{
	PR_ASSERT(PR_DBG_PHYSICS, std::find(nbrs.begin(), nbrs.end(), where_idx) != nbrs.end());	// 'where' should be a neighbour index
	PR_ASSERT(PR_DBG_PHYSICS, std::find(nbrs.begin(), nbrs.end(), new_idx  ) == nbrs.end());	// Shouldn't be adding an index that is already there
	if( std::find(nbrs.begin(), nbrs.end(), new_idx) == nbrs.end() )
	{
		nbrs.insert(std::find(nbrs.begin(), nbrs.end(), where_idx) + 1*(after), new_idx);
	}
}

// Remove 'idx' from a list of nbrs
inline void pr::ph::convex_decompose::Remove(TNbrs& nbrs, TIndex idx)
{
	PR_ASSERT(PR_DBG_PHYSICS, std::find(nbrs.begin(), nbrs.end(), idx) != nbrs.end());
	nbrs.erase(std::find(nbrs.begin(), nbrs.end(), idx));
}

// Remove neighbours from 'nbrs' that are not members of the set 'set_id'
inline void pr::ph::convex_decompose::RemoveNonSetElements(convex_decompose::Mesh const& mesh, TNbrs& nbrs, TSetId set_id)
{
	TNbrs::iterator i = nbrs.begin();
	for( TNbrs::const_iterator j = nbrs.begin(), j_end = nbrs.end(); j != j_end; ++j )
	{
		if( mesh.vert(*j).m_set_id != set_id ) continue;
		*i++ = *j;
	}
	nbrs.resize(i - nbrs.begin());
}

#if PR_DBG_CVX_DECOM == 1
#include <list>
#include "PR/Common/PRScript.h"
// Export a mesh to file so we can run it again if there is a problem
convex_decompose::Mesh const& LoadSaveState(convex_decompose::Mesh const& mesh)
{
	static bool use_prev_state = false;
	if( !use_prev_state )
	{
		// This returns the verts in reverse order
		typedef std::list<convex_decompose::Vert const*> TVertList; TVertList vert_list;
		for( convex_decompose::Vert const* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
			vert_list.push_front(v);

		// Save the mesh to file
		pr::ScriptSaver saver;
		for( TVertList::const_iterator v = vert_list.begin(); v != vert_list.end(); ++v )
		{
			convex_decompose::Vert const& vert = **v;
			saver.WriteKeyword("Vert");
			saver.WriteSectionStart();
				saver.WriteVector3(vert.m_pos);
				saver.WriteSectionStart();
				for( TNbrs::const_iterator n = vert.m_nbrs.begin(); n != vert.m_nbrs.end(); ++n )
					saver.WriteUInt((unsigned int)*n, 10);
				saver.WriteSectionEnd();
			saver.WriteSectionEnd();
		}
		saver.Save("C:/DeleteMe/deformable_SavedMesh.txt");
		return mesh;
	}
	else
	{
		static VertContainer			saved_vert_container;
		static convex_decompose::Mesh	saved_mesh(&saved_vert_container);
		saved_mesh.Clear();

		// Read the mesh from file
		pr::ScriptLoader loader("C:/DeleteMe/deformable_SavedMesh.txt");
		std::string keyword;
		while( loader.GetKeyword(keyword) && str::EqualsNoCase(keyword, "Vert") )
		{
			convex_decompose::Vert vert;
			loader.FindSectionStart();
				loader.ExtractVector3(vert.m_pos, 1.0f);
				loader.FindSectionStart();
					while( !loader.IsSectionEnd() )
					{
						unsigned int n; loader.ExtractUInt(n, 10);
						vert.m_nbrs.push_back(n);
					}
				loader.FindSectionEnd();
			loader.FindSectionEnd();
			saved_mesh.Add(vert);
		}
		return saved_mesh;
	}
}
#else//PR_DBG_CVX_DECOM == 1
	#define LoadSaveState(exp) exp
#endif//PR_DBG_CVX_DECOM == 1

// Make a copy of the mesh into 'vert_container'
void pr::ph::ConvexDecompose(convex_decompose::Mesh const& mesh_, convex_decompose::VertContainer& vert_container, convex_decompose::TMesh& polytopes)
{
	convex_decompose::Mesh const& mesh = LoadSaveState(mesh_);
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_polytopes.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "80A00000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	// Find the concave edges of the initial model. In theory these should be
	// the only edges we need to consider when partitioning the mesh. 
	TEdges concave_edges;
	convex_decompose::Edge most_concave;
	if( !FindConcaveEdges(mesh, concave_edges, most_concave) )
	{
		// If no concave edges are found then the mesh must be convex
		AddPolytope(mesh, polytopes);
		return;
	}
	std::sort(concave_edges.begin(), concave_edges.end());

	// Take a copy of the mesh because the decomposition is destructive
	convex_decompose::Mesh start_mesh(&vert_container);
	start_mesh.Copy(mesh);

	// Decompose the mesh into pieces that don't contain concave edges
	ConvexDecompose(start_mesh, polytopes, concave_edges, most_concave);
}

// Recursively decompose a mesh into convex pieces
PR_EXPAND(PR_DBG_PHYSICS, static int call_number = 0;)
void pr::ph::convex_decompose::ConvexDecompose(convex_decompose::Mesh& mesh, convex_decompose::TMesh& polytopes, TEdges& concave_edges, convex_decompose::Edge const& most_concave)
{
	PR_EXPAND(PR_DBG_PHYSICS, ++call_number;)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edges.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_splitplane.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edge.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_vert.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edgeset.pr_script");EndFile();)
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_perimedges.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_OppVerts.pr_script");EndFile();)
	//PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_polytopes.pr_script");EndFile();)

	// Display what we're trying to decompose
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_concaveedges.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpEdges(mesh, concave_edges, "80A00000", true);)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("MostConcave", "FFFF0000", mesh.vert(most_concave.m_i0).m_pos, mesh.vert(most_concave.m_i1).m_pos);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	// Find a plane to split the mesh
	Plane split_plane;
	FindSplitPlane(mesh, split_plane, concave_edges, most_concave);
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_splitplane.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Plane("SplitPlane", "800080FF", split_plane, 5.0f);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	// Assign a distance for each vertex to the split plane
	// and split any edges that cross the split plane
	// Also resets the 'm_set_id' for each vert to 0
	DivideMesh(mesh, split_plane, concave_edges);

	// Group the verts into sets
	// 'first_zdv_idx' is the start of a linked list of verts
	// that lie on the split plane (zdv = zero distance verts)
	TSetId max_set_id;
	TIndex first_zdv_idx = GroupVerts(mesh, max_set_id);
	
	// Duplicate the zero distance verts such that the mesh can be seperated
	// into distinct sub meshes that don't shared verts (or neighbours)
	SeparateSubMeshes(mesh, first_zdv_idx, concave_edges);

	// Link sub meshes together
	TNbrs sub_meshes;  sub_meshes .resize(max_set_id, InvalidVertIndex);
	TNbrs vert_counts; vert_counts.resize(max_set_id, 0);
	LinkSubMeshes(mesh, &sub_meshes[0], &vert_counts[0]);

	// Sort the concave edges by set id.
	std::sort(concave_edges.begin(), concave_edges.end(), BySetId());
	convex_decompose::Edge cc_edge_search;
	TEdges::iterator cc_edge_begin, cc_edge_end = concave_edges.begin();

	// Decompose each sub mesh.
	// The only verts in set_id 0 should be the ones that have all
	// of their neighbours lying in the plane
	for( TSetId s = 1; s != max_set_id; ++s )
	{
		if( vert_counts[s] == 0 ) continue;

		convex_decompose::Mesh sub_mesh(mesh, sub_meshes[s], vert_counts[s]);
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(sub_mesh, "8000A000");)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_splitplane.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Plane("SplitPlane", "800080FF", split_plane, 5.0f);)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_concaveedges.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, DumpEdges(mesh, concave_edges, "80A00000", true);)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

		// Triangulate any holes in the sub mesh caused by chopping the mesh
		TriangulateHoles(sub_mesh, split_plane);
	
		// Find the range of concave edges that belong to this set
		cc_edge_search.m_set_id = s;	 cc_edge_begin = std::lower_bound(cc_edge_end  , concave_edges.end(), cc_edge_search, BySetId());
		cc_edge_search.m_set_id = s + 1; cc_edge_end   = std::lower_bound(cc_edge_begin, concave_edges.end(), cc_edge_search, BySetId());

		// If there are no concave edges for this set then it must be convex
		if( cc_edge_begin == cc_edge_end )
		{
			AddPolytope(sub_mesh, polytopes);
			continue;
		}

		// Build a list of concave edges that are part of this sub mesh
		TEdges sub_concave_edges(cc_edge_begin, cc_edge_end);
		std::sort(sub_concave_edges.begin(), sub_concave_edges.end());
		convex_decompose::Edge sub_most_concave = *std::max_element(sub_concave_edges.begin(), sub_concave_edges.end(), Concavity());
		
		// Do it all again for this sub mesh	
		ConvexDecompose(sub_mesh, polytopes, sub_concave_edges, sub_most_concave);
	}
}

// Support functions for the triangulate code
namespace pr { namespace triangulate {
m3x3 g_rotate_to_xy;
inline v4 Vertex (convex_decompose::Mesh const& mesh, TIndex idx)
{
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(mesh.vert(idx).m_pos - mesh.vert(idx).m_delta));
	return g_rotate_to_xy * (mesh.vert(idx).m_pos - mesh.vert(idx).m_delta);
}
inline TIndex EdgeIndex0(Edge const* edges, int idx)				{ return edges[idx].m_i0; }
inline TIndex EdgeIndex1(Edge const* edges, int idx)				{ return edges[idx].m_i1; }
struct TriangulateMesh
{
	convex_decompose::Mesh& m_mesh;
	TriangulateMesh(convex_decompose::Mesh& mesh) : m_mesh(mesh) {}
	TriangulateMesh(TriangulateMesh const&);
	TriangulateMesh& operator = (TriangulateMesh const&);
	void TriangulationFace(TIndex i0, TIndex i1, TIndex i2, bool last_one)
	{
		// Add the neighbour links
		if( last_one ) return;
		Insert(m_mesh.vert(i0).m_nbrs, i1, i2, false);
		Insert(m_mesh.vert(i2).m_nbrs, i1, i0, true);
	}
};
}}

// Create a closed polygon for the cut made through the mesh by 'split_plane'
// Note, we cannot use the edges that lie in the plane (and are dropped when partitioning
// the mesh) because in general we cannot tell which set these edges are connected to. 
void pr::ph::convex_decompose::TriangulateHoles(convex_decompose::Mesh& mesh, Plane split_plane)
{
	// Set the correct sign for 'split_plane' by finding a vert that 
	// is not a zero distance vert and ensure that the dot product with
	// the plane normal is positive
	for( Vert* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
	{
		if( v->m_zdv ) continue;
		split_plane *= (Dot4(split_plane, v->m_pos) > 0.0f) * 2.0f - 1.0f;
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_splitplane.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Plane("SplitPlane", "800080FF", split_plane, 5.0f);)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
		break;
	}

	TEdges polygon;
	for( Vert* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
	{
		if( !v->m_zdv ) continue;

		// If this vert is a zero distance vert it lies on the split plane and
		// therefore on the perimeter of a hole in the mesh. Search around the
		// perimeter making edges and marking the verts as not zdv
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_vert.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("Vertex", "FF0000FF", v->m_pos, 0.07f);)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_perimedges.pr_script");EndFile();)

		// No two zero distance verts should be neighbours of each other, so we
		// can use the first neighbour as a starting point
		TIndex other = v->m_nbrs.front();
		TIndex perim = mesh.idx(v);
		TIndex first = perim;
		TNbrs* nbrs = &mesh.vert(other).m_nbrs;
		TNbrs::const_iterator n = std::find(nbrs->begin(), nbrs->end(), perim);		PR_ASSERT(PR_DBG_PHYSICS, n != nbrs->end());
		bool closed = false;
		do
		{
			PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edge.pr_script");)
			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("Edge", "FFFFFF00", mesh.vert(other).m_pos, mesh.vert(perim).m_pos);)
			PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	
			// Look at the next nbr, if this vertex is a zero distance vert
			// insert a nbr link between 'perim' and 'n' and add a polygon edge
			if( ++n == nbrs->end() ) n = nbrs->begin();
			if( *n == first ) closed = true;
			if( mesh.vert(*n).m_zdv )
			{
				// Add the neighbour links
				Insert(mesh.vert(perim).m_nbrs, other, *n,   false);
				Insert(mesh.vert(*n   ).m_nbrs, other, perim, true);
				PR_EXPAND(PR_DBG_CVX_DECOM, AppendFile("C:/Deleteme/deformable_perimedges.pr_script");)
				PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("PerimeterEdge", "FFFFFFFF", mesh.vert(perim).m_pos, mesh.vert(*n).m_pos);)
				PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

				// Add a perimeter edge
				Edge perim_edge;
				perim_edge.m_i0	= perim;
				perim_edge.m_i1	= *n;
				polygon.push_back(perim_edge);
			
				mesh.vert(*n).m_zdv = false;
				perim = *n;
			}

			// If not, make this neighbour the 'other' vert and find 'perim'
			else
			{
				other	= *n;
				nbrs	= &mesh.vert(other).m_nbrs;
				n		= std::find(nbrs->begin(), nbrs->end(), perim);
				PR_ASSERT(PR_DBG_PHYSICS, n != nbrs->end());
				// If this fires it probably means the triangulation has gone
				// wrong in the previous slice of the mesh...
			}
		}
		while( !closed );			
	}

	// All zdv verts should have now been visited and all perimeter edges created
	// This will result in a closed polygon, potentially with holes. Triangulate
	// this polygon to fill in the holes
	if( polygon.size() > 3 )
	{
		RotationToZAxis(triangulate::g_rotate_to_xy, split_plane);
		triangulate::TriangulateMesh tri_mesh(mesh);
		Triangulate<0,1>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh);

		//switch( Abs(split_plane).LargestElement3() )
		//{
		//case 0:	if( split_plane[0] > 0.0f )	Triangulate<1,2>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh);
		//		else						Triangulate<2,1>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh); break;
		//case 1: if( split_plane[1] > 0.0f )	Triangulate<2,0>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh);
		//		else						Triangulate<0,2>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh); break;
		//case 2: if( split_plane[2] > 0.0f )	Triangulate<0,1>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh);
		//		else						Triangulate<1,0>(mesh, mesh.MaxIndex(), &polygon[0], polygon.size(), tri_mesh); break;
		//}
	}
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Link all verts from the same set id together into new sub meshes.
// 'sub_meshes' and 'vert_counts' should be pointers to arrays of
// 'TIndex's and 'std::size_t's respectively at least as big as the maximum set id value.
void pr::ph::convex_decompose::LinkSubMeshes(convex_decompose::Mesh& mesh, TIndex* sub_meshes, std::size_t* vert_counts)
{
	for( Vert* v = mesh.vert_first(); v; )
	{
		Vert& vert = *v; v = mesh.vert_next(v); // Advance the iterater
		vert.m_next = sub_meshes[vert.m_set_id];
		sub_meshes[vert.m_set_id] = mesh.idx(&vert);
		++vert_counts[vert.m_set_id];
	}
}

// Duplicate the zero distance verts for each sub mesh they are connected to.
// A zero distance vert is one that has been classified as lying on the split plane
void pr::ph::convex_decompose::SeparateSubMeshes(convex_decompose::Mesh& mesh, TIndex first_zdv_idx, TEdges& concave_edges)
{
	// Remove neighbour links between zero distance verts
	// We need to do this because zero distance verts can link
	// sub meshes together through the split plane (which we don't want)
	RemoveZDVNeighbours(mesh, first_zdv_idx);

	// Add duplicates of the zero distance verts for each set and fix up
	// the neighbour indices so that they point to the correct vertex for the set
	for( TIndex zdv_iter = first_zdv_idx; zdv_iter != InvalidVertIndex; )
	{
		// Advance the iterator
		TIndex zdv_idx = zdv_iter;
		zdv_iter = mesh.vert(zdv_idx).m_link_index;

		// Get the zero distance vert
		// Note, adding duplicates modifies the vertex container which may cause it
		// to move in memory. We cannot use a reference to 'mesh.vert(zdv_idx)'
		convex_decompose::Vert* zero_dist_vert = &mesh.vert(zdv_idx);
		PR_ASSERT(PR_DBG_PHYSICS, zero_dist_vert->m_zdv);
		PR_ASSERT(PR_DBG_PHYSICS, zero_dist_vert->m_set_id == 0);
		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_vert.pr_script");)
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("Vertex", "FFFF0000", zero_dist_vert->m_pos, 0.05f);)
		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

		// Remove the zero dist vert from the chain of zero dist verts
		// and add it as the first item in the chain of duplicates
		// The chain is singularly linked from the original vert to 'dup_end'
		zero_dist_vert->m_link_index	= InvalidVertIndex;
		TIndex dup_end					= zdv_idx;

		// Create a new duplicate for each set we find when looking at the neighbours of the zero distance vert
		// Also, update the neighbour so that it refers to the correct duplicate
		for( TIndex n = 0, n_end = zero_dist_vert->m_nbrs.size(); n != n_end; ++n )
		{
			convex_decompose::Vert* nbr = &mesh.vert(zero_dist_vert->m_nbrs[n]);
			if( nbr->m_set_id )
			{
				// Get or create a duplicate for each set encountered, link each new duplicate together
				TIndex dup_idx = InvalidVertIndex;
				for( TIndex d = zdv_idx; d != InvalidVertIndex; d = mesh.vert(d).m_link_index )
				{
					convex_decompose::Vert& vert = mesh.vert(d);
					if( vert.m_set_id == nbr->m_set_id )	{ dup_idx = d; break; }
					if( vert.m_set_id == 0)					{ vert.m_set_id = nbr->m_set_id; dup_idx = d; break; }
				}
				if( dup_idx == InvalidVertIndex )
				{
					convex_decompose::Vert dup		= mesh.vert(zdv_idx); // Make a copy
					dup.m_set_id					= nbr->m_set_id;
					dup.m_link_index				= InvalidVertIndex;
					dup_idx							= mesh.Add(dup);
					mesh.vert(dup_end).m_link_index	= dup_idx;
					dup_end							= dup_idx;
					zero_dist_vert					= &mesh.vert(zdv_idx);
					nbr								= &mesh.vert(zero_dist_vert->m_nbrs[n]);
				}
				
				// Update the neighbour index to refer to the correct duplicate
				Replace(nbr->m_nbrs, zdv_idx, dup_idx);
			}
		}

		// We should have created enough duplicates for each set now, and modified the neighbour
		// indices so that each neighbour refers to the correct duplicate for its set.

		// Go through the duplicates deleting the nbrs that don't belong to the same set
		for( TIndex d = zdv_idx; d != InvalidVertIndex; d = mesh.vert(d).m_link_index )
		{
			convex_decompose::Vert& vert = mesh.vert(d);		
			RemoveNonSetElements(mesh, vert.m_nbrs, vert.m_set_id);
		}
	}

	// Go through the concave edges updating any that have verts in different sets
	for( TEdges::iterator e = concave_edges.begin(), e_end = concave_edges.end(); e != e_end; ++e )
	{
		// If one of the verts is a zdv then we want to find the index of the
		// duplicate that belongs to the same set as the vert that isn't a zdv
		if( mesh.vert(e->m_i0).m_zdv != mesh.vert(e->m_i1).m_zdv )
		{
			TIndex* i;
			TSetId set_id;
			if( mesh.vert(e->m_i0).m_zdv )	{ i = &e->m_i0; set_id = mesh.vert(e->m_i1).m_set_id; }
			else							{ i = &e->m_i1; set_id = mesh.vert(e->m_i0).m_set_id; }
			while( mesh.vert(*i).m_set_id != set_id )
			{
				*i = mesh.vert(*i).m_link_index;
				PR_ASSERT(PR_DBG_PHYSICS, *i != InvalidVertIndex);	// We should find it
			}
			if( e->m_i0 > e->m_i1 ) Swap(e->m_i0, e->m_i1);
			e->m_set_id = set_id;
		}
		// Otherwise, both verts should be in the same set unless they're both zdv's
		else
		{
			PR_ASSERT(PR_DBG_PHYSICS,	mesh.vert(e->m_i0).m_zdv ||
										mesh.vert(e->m_i0).m_set_id == mesh.vert(e->m_i1).m_set_id);
			e->m_set_id = !mesh.vert(e->m_i0).m_zdv * mesh.vert(e->m_i0).m_set_id;
		}
	}
	std::sort(concave_edges.begin(), concave_edges.end());

	// Display the separated meshes
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edgeset.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpEdges(mesh);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Remove all neighbour links begin zero distance verts. We need to do this
// because zero distance verts can link sub meshes together through the split plane (which we don't want)
void pr::ph::convex_decompose::RemoveZDVNeighbours(convex_decompose::Mesh& mesh, TIndex first_zdv_idx)
{
	for( TIndex zdv_idx = first_zdv_idx; zdv_idx != InvalidVertIndex; zdv_idx = mesh.vert(zdv_idx).m_link_index )
	{
		convex_decompose::Vert& zero_dist_vert = mesh.vert(zdv_idx);
		int i = 0;
		for( std::size_t j = 0, j_end = zero_dist_vert.m_nbrs.size(); j != j_end; ++j )
		{
			convex_decompose::Vert& nbr = mesh.vert(zero_dist_vert.m_nbrs[j]);
			if( nbr.m_set_id == 0 )		{ Remove(nbr.m_nbrs, zdv_idx); }
			else						{ zero_dist_vert.m_nbrs[i++] = zero_dist_vert.m_nbrs[j]; }
		}
		zero_dist_vert.m_nbrs.resize(i);
		// If a vert ends up with no neighbours this is ok. It just means all faces
		// connecting to this vert lie in the plane. Some of these faces should be
		// part of the sub mesh and some shouldn't, this will be dealt with in SeparateSubMeshes()
	}
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edgeset.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpEdges(mesh);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Group the verts of 'mesh' into sets.
// A vert is a member of a set if it is connected to another
// vert in the set that doesn't lie on the split plane (recursive definition)
// This process identifies the N sub meshes formed after we
// chop the mesh through the split plane
// Returns the start of a linked list of verts that lie on the split plane
TIndex pr::ph::convex_decompose::GroupVerts(convex_decompose::Mesh& mesh, TSetId& max_set_id)
{
	max_set_id = 1;
	TIndex first_zdv_idx = InvalidVertIndex;
	for( Vert* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
	{
		// If the vert is not on the plane, look for a neighbour that
		// is already part of a set and add it to that set
		if( !v->m_zdv )
		{
			TSetId set_id = max_set_id;
			TNbrs::const_iterator n = v->m_nbrs.begin(), n_end = v->m_nbrs.end();
			for( ; n != n_end; ++n )
			{
				TSetId id = mesh.vert(*n).m_set_id;
				if( id ) { set_id = id; break; }
			}

			v->m_set_id = set_id;
			if( n == n_end ) { ++max_set_id; continue; }

			// Check the remaining neighbours to see if this vert connects two sets
			// If so, change the set ids in the previous verts to make them all belong to the same set
			for( ; n != n_end; ++n )
			{
				TSetId id = mesh.vert(*n).m_set_id;
				if( id && id != set_id )
				{
					v->m_set_id = id;
					for( Vert* u = mesh.vert_first(); u != v; u = mesh.vert_next(u) )
						if( u->m_set_id == set_id ) u->m_set_id = id;
				}
			}
		}
		else
		{
			v->m_link_index = first_zdv_idx;
			first_zdv_idx = mesh.idx(v);
		}
	}

	// Display the sets
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edgeset.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpEdges(mesh);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	return first_zdv_idx;
}

// Consider all edges in 'mesh', split those that cross the split plane
void pr::ph::convex_decompose::DivideMesh(convex_decompose::Mesh& mesh, Plane const& split_plane, TEdges& concave_edges)
{
	Edge iter;
	for( Edge* e = EdgeFirst(mesh, iter); e; e = EdgeNext(mesh, iter) )
	{
		convex_decompose::Vert& v0 = mesh.vert(e->m_i0);
		convex_decompose::Vert& v1 = mesh.vert(e->m_i1);
		v0.m_set_id = 0;
		v1.m_set_id = 0;

		// Find the distances to the plane for the start and end of the edge
		float d0, d1;
		v0.m_zdv = PointOnPlane(split_plane, v0.m_pos, d0); // Find 'zero distance vert's
		v1.m_zdv = PointOnPlane(split_plane, v1.m_pos, d1);

		// Move verts so that the lie on the split plane.
		// Pros: Faces that form along split plane can't be concave if we do this
		//       Triangulation is most robust because the polygon is actually planar
		// Cons: This creates more concave edges - but, we're not recalculating edges
		//		 now so these new ones should be ignored
		if( v0.m_zdv ) v0.m_delta = d0 * plane::GetDirection(split_plane);
		if( v1.m_zdv ) v1.m_delta = d1 * plane::GetDirection(split_plane);

		// If the edge crosses the plane, split the edge
		if( !v0.m_zdv && !v1.m_zdv && d0*d1 < 0.0f )
		{
			float d = d1 - d0;
			float t = -d0 / d; // Use similar triangles to find 't'
			TIndex idx = SplitEdge(mesh, *e, t);
			// Note: the SplitEdge function adds a new vertex to the mesh
			// and therefore new edges. These edges may be missed by this
			// loop over edges, however, the edges that are added cannot
			// cross the split plane so they do not need to be considered.

			// If the edge we've split is one of the concave edges,
			// we need to split that too.
			PR_ASSERT(PR_DBG_PHYSICS, e->m_i0 < e->m_i1);
			TEdges::iterator iter = std::lower_bound(concave_edges.begin(), concave_edges.end(), *e);
			if( iter != concave_edges.end() && *iter == *e )
			{
				// Split the concave edge
				Edge half = *iter;
				EdgeIter(mesh,  half, iter->m_i1, idx);
				EdgeIter(mesh, *iter, iter->m_i0, idx);
				MeasureConcavity(mesh, half);
				MeasureConcavity(mesh, *iter);
				if( iter->m_concavity < ConcaveTolerance ) concave_edges.erase(iter);
				if( half .m_concavity > ConcaveTolerance ) concave_edges.push_back(half);
				std::sort(concave_edges.begin(), concave_edges.end());
			}
		}
		// Don't use 'v0' and 'v1' here. SplitEdge() may have invalidated them
	}

	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Split an edge in the mesh, returns the index of the new vertex
TIndex pr::ph::convex_decompose::SplitEdge(convex_decompose::Mesh& mesh, convex_decompose::Edge const& edge, float t)
{
	// Look for the verts that form the triangles on either side of 'edge'
	TIndex lhs, rhs;
	std::size_t lhs_i, rhs_i;
	FindOppositeVerts(mesh, edge, lhs, rhs, lhs_i, rhs_i);

	// Insert a vertex at the point 't' along 'edge'
	convex_decompose::Vert vert;
	vert.m_pos		= (1.0f - t)*mesh.vert(edge.m_i0).m_pos + t*mesh.vert(edge.m_i1).m_pos;
	vert.m_delta	= v4Zero;
	vert.m_nbrs		.push_back(edge.m_i0);	
	vert.m_nbrs		.push_back(rhs);	
	vert.m_nbrs		.push_back(edge.m_i1);	
	vert.m_nbrs		.push_back(lhs);	
	vert.m_zdv		= true;
	vert.m_set_id	= 0;
	TIndex vert_idx = mesh.Add(vert);

	// Adjust the neighbours of i0, i1, lhs, and rhs
	Replace(mesh.vert(edge.m_i0).m_nbrs, edge.m_i1, vert_idx);
	Replace(mesh.vert(edge.m_i1).m_nbrs, edge.m_i0, vert_idx);
	TNbrs& nbr_lhs = mesh.vert(lhs).m_nbrs;	nbr_lhs.insert(nbr_lhs.begin() + lhs_i + 1, vert_idx);
	TNbrs& nbr_rhs = mesh.vert(rhs).m_nbrs;	nbr_rhs.insert(nbr_rhs.begin() + rhs_i + 1, vert_idx);

	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	return vert_idx;
}

// Look for the sequence 'i0,i1' or 'i1,i0' in the nbrs of the neighbours of i0 and i1
// 'lhs' is the vertex that completes the triangle <i0, i1, lhs>
// 'rhs' is the vertex that completes the triangle <i1, i0, rhs>
// 'lhs_i' is the neighbour index of i0, i.e. vert[lhs].m_nbr[lhs_i] == i0
// 'rhs_i' is the neighbour index of i1, i.e. vert[rhs].m_nbr[rhs_i] == i1
void pr::ph::convex_decompose::FindOppositeVerts(convex_decompose::Mesh const& mesh, Edge const& edge, TIndex& lhs, TIndex& rhs, std::size_t& lhs_i, std::size_t& rhs_i)
{
	PR_EXPAND(PR_DBG_PHYSICS, lhs = rhs = InvalidVertIndex;)
	for( int k = 0; k != 2; ++k ) // for i0 then i1
	{
		TIndex							i0	  = (k == 0) ? edge.m_i0 : edge.m_i1;
		TIndex							i1	  = (k == 0) ? edge.m_i1 : edge.m_i0;
		TIndex&							idx   = (k == 0) ? lhs : rhs;
		std::size_t&					idx_i = (k == 0) ? lhs_i : rhs_i;
		convex_decompose::Vert const&	vert  = mesh.vert(i0);

		// Check the neighbours of vert
		for( std::size_t j = 0, j_end = vert.m_nbrs.size(); j != j_end; ++j )
		{
			TIndex nbr_idx = vert.m_nbrs[j];
			if( nbr_idx == i1 ) continue;

			convex_decompose::Vert const& v = mesh.vert(nbr_idx);
			for( std::size_t i = 0, i_end = v.m_nbrs.size(); i != i_end; ++i )
			{
				if( i0 == v.m_nbrs[i] && i1 == v.m_nbrs[(i+1)%i_end] )
				{
					idx = nbr_idx;
					idx_i = i;
					j = j_end - 1; // break to the k for loop
					break;
				}
			}
		}
	}
	PR_ASSERT(PR_DBG_PHYSICS, lhs != InvalidVertIndex && rhs != InvalidVertIndex && lhs != rhs);
	PR_ASSERT(PR_DBG_PHYSICS, mesh.vert(lhs).m_nbrs[lhs_i] == edge.m_i0);
	PR_ASSERT(PR_DBG_PHYSICS, mesh.vert(rhs).m_nbrs[rhs_i] == edge.m_i1);
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_oppverts.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("OppVert", "FFFFFF00", mesh.vert(lhs).m_pos, 0.05f);)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("OppVert", "FFFFFF00", mesh.vert(rhs).m_pos, 0.05f);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Return a plane with which to split the mesh
// Returns true if a split plane was found, false if the mesh is convex
// Choosing a split plane is done by finding the most concave edge, then choosing
//	a plane that passes through that edge and as many other concave edges as possible
void pr::ph::convex_decompose::FindSplitPlane(convex_decompose::Mesh const& mesh, Plane& split_plane, TEdges const& concave_edges, convex_decompose::Edge const& most_concave)
{
	split_plane = v4Zero;

	// Choose another edge with which to make a split plane
	std::size_t num_coplanar		= 0;
	v4			most_concave_v0		= mesh.vert(most_concave.m_i0).m_pos;
	v4			most_concave_ed		= mesh.vert(most_concave.m_i1).m_pos - most_concave_v0;
	float		best_dot_bisect_dir = 0.0f;

	// Search the neighbours of the most concave edge
	TIndex i0, i1;
	for( int k = 0; k != 2; ++k ) // For each end of the concave edge
	{
		if( k == 0 )	{ i0 = most_concave.m_i0; i1 = most_concave.m_i1; }
		else			{ i0 = most_concave.m_i1; i1 = most_concave.m_i0; }
		TNbrs const& nbrs = mesh.vert(i0).m_nbrs;
		v4 const&	 v0	  = mesh.vert(i0).m_pos;
		for( std::size_t n = 0, n_end = nbrs.size(); n != n_end; ++n )
		{
			if( nbrs[n] == i1 ) continue;
			PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edges.pr_script");)
			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::LineD("most_concave", "FF0000FF", most_concave_v0, most_concave_ed);)
			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("adjoining", "FFFF0000", v0, mesh.vert(nbrs[n]).m_pos);)
			PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

			// See how close to the ideal bisect direction this edge is
			v4		edge				= (mesh.vert(nbrs[n]).m_pos - v0).GetNormal3();
			float	edge_dot_bisect_dir = Abs(Dot3(edge, most_concave.m_bisect_dir));

			// Form a plane using this edge
			v4 norm = Cross3(most_concave_ed, edge);
			if( FEqlZero3(norm) ) continue; // don't use edges that are colinear with 'most_concave'
			Plane plane = plane::Make(most_concave_v0, norm.GetNormal3());
			PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_splitplane.pr_script");)
			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Plane("SplitPlane", "800080FF", plane, 5.0f);)
			PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

			// See how many concave edges lie in this plane. If the number is
			// the highest then this is the split plane we want to use as it
			// will remove the most concave edges from the mesh
			std::size_t coplanar_count	= 0;
			for( TEdges::const_iterator ee = concave_edges.begin(), ee_end = concave_edges.end(); ee != ee_end; ++ee )
			{
				PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edge.pr_script");)
				PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("on_plane_maybe", "FF00FF00", mesh.vert(ee->m_i0).m_pos, mesh.vert(ee->m_i1).m_pos);)
				PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

				float d0, d1;
				if( PointOnPlane(plane, mesh.vert(ee->m_i0).m_pos, d0) && 
					PointOnPlane(plane, mesh.vert(ee->m_i1).m_pos, d1) )
				{
					++coplanar_count;
				}
			}

			// Record the best edge
			if( (coplanar_count > num_coplanar) ||
				(coplanar_count == num_coplanar && edge_dot_bisect_dir > best_dot_bisect_dir) )
			{
				num_coplanar		= coplanar_count;
				best_dot_bisect_dir	= edge_dot_bisect_dir;
				split_plane			= plane;
			}
		}
	}
	PR_ASSERT_STR(PR_DBG_PHYSICS, !FEqlZero4(split_plane), "No suitable split plane was found");
}

// Search 'mesh' for concave edges and save them in 'concave_edges'
// 'most_concave' is returned as the concave edge with the greatest concavity
bool pr::ph::convex_decompose::FindConcaveEdges(convex_decompose::Mesh const& mesh, TEdges& concave_edges, Edge& most_concave)
{
	most_concave.m_concavity = 0.0f;

	Edge iter;
	for( Edge* e = EdgeFirst(mesh, iter); e; e = EdgeNext(mesh, iter) )
	{
		MeasureConcavity(mesh, *e);
		if( e->m_concavity < ConcaveTolerance ) continue; // Not concave...
		
		PR_ASSERT(PR_DBG_PHYSICS, e->m_i0 < e->m_i1); // This is needed for sorting to work
		concave_edges.push_back(*e);

		if( e->m_concavity > most_concave.m_concavity + maths::tiny )
		{	most_concave = *e; }
	}
	
	// If no concave edges are found, then the mesh must be convex
	return most_concave.m_concavity != 0.0f;
}

// Measure the depth of a concave edge.
// Updates the value of 'm_concavity' and 'm_bisect_dir' in 'edge' 
void pr::ph::convex_decompose::MeasureConcavity(convex_decompose::Mesh const& mesh, Edge& edge)
{
	// Verify that 'edge.m_iter' is correct for 'edge'. It should be if 'edge'
	// is the result of iterating over edges using EdgeFirst, EdgeNext
	PR_ASSERT(PR_DBG_PHYSICS, edge.m_iter <= mesh.vert(edge.m_i0).m_nbrs.size());
	PR_ASSERT(PR_DBG_PHYSICS, mesh.vert(edge.m_i0).m_nbrs[edge.m_iter-1] == edge.m_i1);

	edge.m_concavity = 0.0f;
		
	TNbrs const& nbrs = mesh.vert(edge.m_i0).m_nbrs;
	std::size_t num_nbrs = nbrs.size();
	TIndex n0 = edge.m_i1;
	TIndex n1 = nbrs[(edge.m_iter - 2 + num_nbrs) % num_nbrs]; // Previous neighbour
	TIndex n2 = nbrs[(edge.m_iter               ) % num_nbrs]; // Next neighbour

	v4 const&	pos   = mesh.vert(edge.m_i0).m_pos;
	v4			edge0 = mesh.vert(n0).m_pos - pos;
	v4			edge1 = mesh.vert(n1).m_pos - pos;
	v4			edge2 = mesh.vert(n2).m_pos - pos;
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_edge.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("edge", "FFFFFF00", pos, pos+edge0);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	float convexity = Triple3(edge0, edge1, edge2);
	if( convexity >= 0.0f ) return; // Edge is not concave...

	v4 bridge = edge2 - edge1;
	edge.m_bisect_dir = Cross3(edge0, bridge).GetNormal3();
	edge.m_concavity  = Dot3(edge.m_bisect_dir, edge1);
	PR_EXPAND(PR_DBG_CVX_DECOM, float t0; float t1; ClosestPoint_InfiniteLineToInfiniteLine(pos, edge0, pos + edge1, edge2 - edge1, t0, t1);)
	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_bridge.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("bridge", "FFFFFFFF", pos + edge1, pos + edge2);)
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::LineD("bisect", "FFFFFF00", pos + t0*edge0, edge.m_bisect_dir * edge.m_concavity);)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	//
	//// If this is a concave edge, find the "depth" of the concavity
	//// using a hill climbing search of the neighbours toward the convex
	//// hull. Note: this isn't really correct because it is only a local
	//// search and is therefore not guaranteed to find the convex hull
	//// We're only doing this to find the "best choice" for chopping the
	//// mesh however so it doesn't matter too much if it's not quite right
	//bool higher_nbr_found;
	//do
	//{
	//	higher_nbr_found = false;

	//	v4 bridge = edge2 - edge1;
	//	edge.m_bisect_dir = Cross3(edge0, bridge).GetNormal3();
	//	edge.m_concavity  = Dot3(edge.m_bisect_dir, edge1);
	//	PR_EXPAND(PR_DBG_CVX_DECOM, float t0; float t1; ClosestPoint_InfiniteLineToInfiniteLine(pos, edge0, pos + edge1, edge2 - edge1, t0, t1);)
	//	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_bridge.pr_script");)
	//	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("bridge", "FFFFFFFF", pos + edge1, pos + edge2);)
	//	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::LineD("bisect", "FFFFFF00", pos + t0*edge0, edge.m_bisect_dir * edge.m_concavity);)
	//	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	//
	//	// Look at the neighbours of n1 and n2
	//	for( int k = 0; k != 2; ++k )
	//	{
	//		TIndex&	n		= (k == 0 ? n1 : n2);
	//		v4&		edge_n	= (k == 0 ? edge1 : edge2);
	//		PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_vert.pr_script");)
	//		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("nbr", "FF0000FF", pos + edge_n, 0.05f);)
	//		PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)

	//		TNbrs const& nbrs_n = mesh.vert(n).m_nbrs;
	//		for( TNbrs::const_iterator i = nbrs_n.begin(), i_end = nbrs_n.end(); i != i_end; ++i )
	//		{
	//			v4 nbr = mesh.vert(*i).m_pos - pos;
	//			PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_vert.pr_script");)
	//			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("nbr", "FF0000FF", pos + nbr, 0.05f);)
	//			PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	//			float depth = Dot3(nbr, edge.m_bisect_dir);
	//			if( depth > edge.m_concavity + maths::tiny )
	//			{
	//				higher_nbr_found = true;
	//				n		= *i;
	//				edge_n	= nbr;
	//				
	//				// Leave the for loops
	//				i = i_end - 1;
	//				k = 1;
	//			}
	//		}
	//	}
	//}
	//while( higher_nbr_found );
}

// Add a polytope from a set of edges that are known to be convex
void pr::ph::convex_decompose::AddPolytope(convex_decompose::Mesh const& mesh, convex_decompose::TMesh& polytopes)
{
	PR_EXPAND(PR_DBG_CVX_DECOM, AppendFile("C:/Deleteme/deformable_polytopes.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");) mesh;
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
	polytopes.push_back(mesh);
}

// Generate a mesh that can be decomposed into convex pieces
void pr::ph::convex_decompose::CreateMesh(v4 const* verts, std::size_t num_verts, TIndex const* indices, std::size_t num_faces, convex_decompose::Mesh& mesh)
{
	mesh.Clear();
	mesh.Reserve(num_verts);

	// Copy the verts
	for( v4 const *v = verts, *v_end = verts + num_verts; v != v_end; ++v )
	{
		convex_decompose::Vert vert;
		vert.m_pos		= *v;
		mesh.Add(vert);
	}

	struct Face { TIndex i[3]; };

	// Generate neighbour data, adjacent neighbours form faces
	for( Face const* f = reinterpret_cast<Face const*>(indices), *f_end = f + num_faces; f != f_end; ++f )
	{
		Face const& face = *f;
		for( std::size_t i = 0; i != 3; ++i )
		{
			TIndex i0 = face.i[ i     ];
			TIndex i1 = face.i[(i+1)%3];
			TIndex i2 = face.i[(i+2)%3];
			TNbrs& nbr = mesh.vert(i0).m_nbrs;
			
			TNbrs::iterator n_begin = nbr.begin(), n_end = nbr.end(), n;
			for( n = n_begin; n != n_end; ++n )
			{
				if( *n == i1 || *n == i2 ) break;
			}
			if     ( n == n_end  )	{ nbr.push_back(i1); nbr.push_back(i2); }
			else if( *n == i1 )		{ if( n+1 == n_end || *(n+1) != i2 ) nbr.insert(n+1, i2); }
			else if( *n == i2 )		{ if( n == n_begin || *(n-1) != i1 ) nbr.insert(n  , i1); }
		}
	}

	// Remove the repeated index at the end of the array
	for( Vert* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
	{
		PR_ASSERT(PR_DBG_PHYSICS, std::find(v->m_nbrs.begin(), v->m_nbrs.end(), mesh.idx(v)) == v->m_nbrs.end());
		v->m_nbrs.pop_back();
	}

	PR_EXPAND(PR_DBG_CVX_DECOM, StartFile("C:/Deleteme/deformable_shape.pr_script");)
	PR_EXPAND(PR_DBG_CVX_DECOM, DumpMesh(mesh, "8000A000");)
	PR_EXPAND(PR_DBG_CVX_DECOM, EndFile();)
}

// Dump the mesh to ldr
#if PR_DBG_CVX_DECOM == 1
void pr::ph::convex_decompose::DumpMesh(convex_decompose::Mesh const& mesh, char const* colour)
{
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupStart("Deformable");)
	convex_decompose::Face face;
	for( convex_decompose::Face const* f = FaceFirst(mesh, face); f; f = FaceNext(mesh, face) )
	{
		PR_EXPAND(PR_DBG_CVX_DECOM, v4 const& v0 = mesh.vert(f->m_i0).m_pos;)
		PR_EXPAND(PR_DBG_CVX_DECOM, v4 const& v1 = mesh.vert(f->m_i1).m_pos;)
		PR_EXPAND(PR_DBG_CVX_DECOM, v4 const& v2 = mesh.vert(f->m_i2).m_pos;)
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Triangle("Face", colour, v0, v1, v2);)
	}
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupEnd();)

	// This version shows the triangles around each vertex
	//PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupStart("Deformable");)
	//for( Vert const* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
	//{
	//	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupStart("Vert");)
	//	//PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box(FmtS("Vert_%d", mesh.idx(v)), colour, v->m_pos, 0.02f);)
	//	std::size_t num_nbrs = v->m_nbrs.size();
	//	for( std::size_t n = 0; n != num_nbrs; ++n )
	//	{
	//		PR_EXPAND(PR_DBG_CVX_DECOM, v4 const& v0 = mesh.vert(v->m_nbrs[ n            ]).m_pos;)
	//		PR_EXPAND(PR_DBG_CVX_DECOM, v4 const& v1 = mesh.vert(v->m_nbrs[(n+1)%num_nbrs]).m_pos;)
	//		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Triangle("Face", colour, v->m_pos, v0, v1);)
	//	}
	//	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupEnd();)
	//}
	//PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupEnd();)
	////ldr::Polytope("Hull", "80FF0000", verts, verts + num_verts);
}
void pr::ph::convex_decompose::DumpEdges(convex_decompose::Mesh const& mesh)
{
	uint col = 0xFFFFFFFF;
	for( TSetId set = 0; set != InvalidVertIndex; )
	{
		TSetId	next_set = InvalidVertIndex;
		uint	next_col = 0xFFFFFFFF;
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupStart(FmtS("Set_%d", set));)
		for( Vert const* v = mesh.vert_first(); v; v = mesh.vert_next(v) )
		{
			if( v->m_set_id != set )
			{
				if( v->m_set_id > set && v->m_set_id < next_set )
				{
					next_set = v->m_set_id;
					uint8 bb = static_cast<uint8>(RandU32());
					next_col = 0xFF000000 | (bb << 16) | (~bb << 8);
				}
				continue;
			}
			if( v->m_zdv )	{ PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("pt", "FFFFFFFF", v->m_pos, 0.03f);) }
			else			{ PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Box("pt", FmtS("%X", col), v->m_pos, 0.03f);) }
			std::size_t num_nbrs = v->m_nbrs.size();
			for( std::size_t n = 0; n != num_nbrs; ++n )
			{
				convex_decompose::Vert const& v_nbr = mesh.vert(v->m_nbrs[n]);
				if( set == 0 && v_nbr.m_set_id != 0 ) continue;
				PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("edge", FmtS("%X", col&0x80FFFFFF), v->m_pos, v_nbr.m_pos);)
				//v4 const& v0 = mesh.m_vert[v->m_nbrs[ n            ]].m_pos;
				//v4 const& v1 = mesh.m_vert[v->m_nbrs[(n+1)%num_nbrs]].m_pos;
				//PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Triangle("Face", "2000A000", v->m_pos + move_dir, v0 + move_dir, v1 + move_dir);)
			}
		}
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupEnd();)
		set = next_set;
		col = next_col;
	}
}
void pr::ph::convex_decompose::DumpEdges(convex_decompose::Mesh const& mesh, TEdges const& edges, char const* colour, bool show_bisect)
{
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupStart("Edges");)
	for( TEdges::const_iterator e = edges.begin(), e_end = edges.end(); e != e_end; ++e )
	{
		PR_EXPAND(PR_DBG_CVX_DECOM, ldr::Line("edge", colour, mesh.vert(e->m_i0).m_pos, mesh.vert(e->m_i1).m_pos);)
		if( show_bisect )
		{
			PR_EXPAND(PR_DBG_CVX_DECOM, ldr::LineD("bisect", colour, (mesh.vert(e->m_i0).m_pos + mesh.vert(e->m_i1).m_pos) * 0.5f, e->m_bisect_dir * e->m_concavity);)
		}
	}
	PR_EXPAND(PR_DBG_CVX_DECOM, ldr::GroupEnd();)
}
#endif//PR_DBG_CVX_DECOM == 1
