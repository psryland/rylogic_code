//**********************************************
// Triangulate a polygon
//  Copyright (c) Rylogic Ltd 2007
//**********************************************
// Triangulates a polygon that may have holes and be in separate pieces
// Usage:
//	VertContainer verts;	// v4 verts[] = {...} should work
//	EdgeContainer edges;	// struct Edge { int m_i0, m_i1; }; Edge edges[] = {...} should work
//	struct FaceOut { void TriangulationFace(VIndex i0, VIndex i1, VIndex i2, bool last_one) { triangle = i0,i1,i2; } };
//	namespace pr { namespace triangulate {	// Overload these if necessary
//	template <> inline v4 const&   Vertex    (VertContainer const& verts, std::size_t idx)	{ return verts[idx];      }
//	template <> inline std::size_t EdgeIndex0(EdgeContainer const& edges, std::size_t idx)	{ return edges[idx].m_i0; }
//	template <> inline std::size_t EdgeIndex1(EdgeContainer const& edges, std::size_t idx)	{ return edges[idx].m_i1; }
//	}}
//	Triangulate<Axis0, Axis1>(verts, verts.size(), edges, edges.size(), FaceOut());
//	0 = X, 1 = Y, 2 = Z
#ifndef PR_MATHS_TRIANGULATE_H
#define PR_MATHS_TRIANGULATE_H

#include <malloc.h>	// for _malloca()
#include "pr/maths/maths.h"

//#define PR_GEOMETRY_TRIANGULATE_PR_EXPAND
#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
	#include "pr/macros/link.h"
	#include "pr/filesys/AutoFile.h"
	#include "pr/linedrawer/ldr_helper.h"
#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND

namespace pr
{
	namespace triangulate
	{
		typedef std::size_t VIndex;

		enum EChain
		{
			EUnused		= 0,
			EFree		= 1 << 0,
			EConcave	= 1 << 1,
			EConvex		= 1 << 2,
			EEar		= (1 << 3) | EConvex,
		};

		struct Vert
		{
			EChain	m_type;
			Vert*	m_edge_in;
			VIndex	m_idx;
			Vert*	m_edge_out;
			//float	m_nearest;
			Vert*	m_next;
			Vert*	m_prev;
			Vert*	m_ear_next;
		};

		// Accessor functions. If unsuitable, supply template specialisations
		// Functions for accessing a vertex or the start and end vertex index of an edge
		template <typename VertCntr> inline v4 const&   Vertex    (VertCntr const& verts, std::size_t idx)	{ return verts[idx];      }
		template <typename EdgeCntr> inline std::size_t EdgeIndex0(EdgeCntr const& edges, std::size_t idx)	{ return edges[idx].m_i0; }
		template <typename EdgeCntr> inline std::size_t EdgeIndex1(EdgeCntr const& edges, std::size_t idx)	{ return edges[idx].m_i1; }

		// Chain helper functions
		// CHANGE THIS TO USE THE TEMPLATE VERSION IN PODCHAIN.H
		inline void  ChainInit(Vert& elem, EChain type)	{ elem.m_next = &elem; elem.m_prev = &elem; elem.m_type = type; }
		inline bool  ChainEmpty(Vert const& elem)		{ return elem.m_next == &elem; }
		inline Vert const* ChainBegin(Vert const& end)	{ return end.m_next; }
		inline Vert const* ChainEnd(Vert const& end)	{ return &end; }
		inline Vert* ChainBegin(Vert& end)				{ return end.m_next; }
		inline Vert* ChainEnd(Vert& end)				{ return &end; }
		inline bool  ChainSizeAtLeast(Vert const& end, std::size_t count)
		{
			for( Vert const* v = ChainBegin(end); v != ChainEnd(end) && count; v = v->m_next ) --count;
			return count == 0;
		}
		inline void ChainRemove(Vert& elem)
		{
			elem.m_prev->m_next = elem.m_next;
			elem.m_next->m_prev = elem.m_prev;
		}
		inline void ChainInsert(Vert& elem, Vert* before_me)
		{
			ChainRemove(elem);
			elem.m_type			= before_me->m_type;
			elem.m_next			= before_me;
			elem.m_prev			= before_me->m_prev;
			elem.m_next->m_prev = &elem;
			elem.m_prev->m_next = &elem;
		}

		// Evaluate a line equation for 'vert' compared to an infinite line passing through 'edgeS' and 'edgeE'
		template <int Axis0, int Axis1>
		inline float EvalLineEqn(v4 const& vert, v4 const& edgeS, v4 const& edgeE)
		{
			return	(vert[Axis0] - edgeS[Axis0])*(edgeE[Axis1] - edgeS[Axis1]) -
					(vert[Axis1] - edgeS[Axis1])*(edgeE[Axis0] - edgeS[Axis0]);
		}

		// Returns true if 'vert' is "less than" (meaning to the left of) 'rhs'
		// if 'vert' is on the line it is considered on the right
		template <int Axis0, int Axis1>
		inline bool LessThan(v4 const& vert, v4 const& edgeS, v4 const& edgeE)
		{
			return EvalLineEqn<Axis0, Axis1>(vert, edgeS, edgeE) < -maths::tiny;
		}

		// Triangulation implementation
		template <int Axis0, int Axis1, typename VertCntr, typename FaceOut>
		struct Triangulator
		{
			VertCntr const&	m_verts;
			FaceOut*		m_face_out;
			Vert*			m_adj;
			Vert			m_concave_chain;
			Vert			m_convex_chain;
			Vert			m_ear_chain;
			Vert			m_free_chain;

			// Returns true if 'vert' is convex
			inline bool IsConvex(Vert& vert) const
			{
				return LessThan<Axis0, Axis1>(
					Vertex(m_verts, vert.m_edge_out->m_idx),
					Vertex(m_verts, vert.m_edge_in ->m_idx),
					Vertex(m_verts, vert.m_idx) );
			}

			// Return true if 'vert' is an ear of the polygon
			inline bool IsEar(Vert& vert) const
			{
				assert(vert.m_type & EConvex && "Should only be testing convex verts");
				if( vert.m_edge_in == vert.m_edge_out ) return false;

				v4 const& a = Vertex(m_verts, vert.m_edge_in->m_idx);
				v4 const& b = Vertex(m_verts, vert.m_idx);
				v4 const& c = Vertex(m_verts, vert.m_edge_out->m_idx);
				//vert.m_nearest = maths::float_max;
				for( Vert const* v = ChainBegin(m_concave_chain); v != ChainEnd(m_concave_chain); v = v->m_next )
				{
					if( v->m_idx != vert.m_edge_in->m_idx && v->m_idx != vert.m_edge_out->m_idx )
					{
						v4 const& pt = Vertex(m_verts, v->m_idx);
						//float d0 = EvalLineEqn<Axis0, Axis1>(pt, b, a);
						//float d1 = EvalLineEqn<Axis0, Axis1>(pt, c, b);
						//float d2 = EvalLineEqn<Axis0, Axis1>(pt, a, c);
						//if( !(d0 < -maths::tiny || d1 < -maths::tiny || d2 < -maths::tiny) ) return false;
						//if( d0 < vert.m_nearest ) vert.m_nearest = d0;
						//if( d1 < vert.m_nearest ) vert.m_nearest = d1;
						//if( d2 < vert.m_nearest ) vert.m_nearest = d2;
						if( !(LessThan<Axis0, Axis1>(pt, b, a) ||
							  LessThan<Axis0, Axis1>(pt, c, b) ||
							  LessThan<Axis0, Axis1>(pt, a, c)) )
							return false;
					}
				}
				return true;
			}

			// If a vert was previously convex it will stay convex. If not,
			// check whether it has become convex. If the vert is convex,
			// check whether it was not an ear and now is or visa versa
			inline void ExamineVert(Vert& vert)
			{
				if( vert.m_type == EConcave && IsConvex(vert) )
				{
					ChainInsert(vert, &m_convex_chain);
				}
				if( vert.m_type & EConvex )
				{
					bool is_ear = IsEar(vert);
					if     (  is_ear && vert.m_type == EConvex )	{ ChainInsert(vert, &m_ear_chain); }
					else if( !is_ear && vert.m_type == EEar    )	{ ChainInsert(vert, &m_convex_chain); }
				}
			}

			// Constructor
			Triangulator(Triangulator const&);
			Triangulator& operator = (Triangulator const&);
			Triangulator(VertCntr const& verts, std::size_t num_verts, Vert* adj, FaceOut& face_out)
			:m_verts(verts)
			,m_face_out(&face_out)
			,m_adj(adj)
			{
				ChainInit(m_concave_chain, EConcave);
				ChainInit(m_convex_chain , EConvex);
				ChainInit(m_ear_chain    , EEar);
				ChainInit(m_free_chain   , EFree);

				// Categorise the verts into concave and convex lists
				for( Vert* v = m_adj, *v_end = m_adj + num_verts; v != v_end; ++v )
				{
					if( v->m_type != EFree ) continue; // Only categorise verts that are part of the polygon
					assert(v->m_edge_in != v->m_edge_out);
					ChainInit(*v, EFree);
					ChainInsert(*v, IsConvex(*v) ? &m_convex_chain : &m_concave_chain);
				}

				// Build initial list of ears
				for( Vert* v = ChainBegin(m_convex_chain); v != ChainEnd(m_convex_chain); )
				{
					Vert& vert = *v; v = v->m_next;
					if( !IsEar(vert) ) continue;
					ChainInsert(vert, &m_ear_chain);
				}
				#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
				DumpVerts();
				#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND

				// Trim ears from the polygon
				ClipEars();
			}

			// Add a face to the triangulation
			inline void AddFace(Vert& vert, bool last_one)
			{
				#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
				AppendFile("C:/Deleteme/triangulate_result.pr_script");
				ldr::Triangle("face", "8000FF00",
					Vertex(m_verts, vert.m_edge_in->m_idx),
					Vertex(m_verts, vert.m_idx),
					Vertex(m_verts, vert.m_edge_out->m_idx) );
				EndFile();
				#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND

				m_face_out->TriangulationFace(vert.m_edge_in->m_idx, vert.m_idx, vert.m_edge_out->m_idx, last_one);
			}

			// Insert a diagonal between a convex vert and the nearest concave vert
			// 'vert2' and 'diag2' are verts to be used for the degenerates
			inline void AddDiagonal(Vert& vert2, Vert& diag2)
			{
				Vert& vert1 = *ChainBegin(m_convex_chain);

				// Find the concave vertex that maximises the area of the triangle a,d,c
				// where a = edge_in, (b = vert), c = edge_out, d = concave_vert
				v4 const& a = Vertex(m_verts, vert1.m_edge_in ->m_idx);
				v4 const& c = Vertex(m_verts, vert1.m_edge_out->m_idx);
				Vert* diag = 0;
				float max_dist = 0.0f;
				for( Vert* d = ChainBegin(m_concave_chain); d != ChainEnd(m_concave_chain); d = d->m_next )
				{
					float dist = EvalLineEqn<Axis0, Axis1>(Vertex(m_verts, d->m_idx), a, c);
					if( dist <= max_dist ) continue;
					max_dist = dist;
					diag = d;
				}
				assert(diag && diag->m_type == EConcave);
				Vert& diag1 = *diag;

				#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
				AppendFile("C:/Deleteme/triangulate_polygon.pr_script");
				ldr::Line("diagonal", "FFA00000", Vertex(m_verts, vert1.m_idx), Vertex(m_verts, diag1.m_idx));
				EndFile();
				#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND

				vert2 = vert1;
				diag2 = diag1;
				vert1.m_edge_out	= &diag1;
				diag1.m_edge_in		= &vert1;
				diag2.m_edge_out	= &vert2;
				vert2.m_edge_in		= &diag2;
				ExamineVert(vert1);
				ExamineVert(diag1);
				ExamineVert(diag2);
				ExamineVert(vert2);
			}

			// Clip the ears of naughty polygons
			void ClipEars()
			{
				do
				{
					while( !ChainEmpty(m_ear_chain) )
					{
						// Find the ear with the greatest 'nearest' distance
						Vert* best_ear = ChainBegin(m_ear_chain);
						//for( Vert* v = best_ear->m_next, *v_end = ChainEnd(m_ear_chain); v != v_end; v = v->m_next )
						//	if( v->m_nearest > best_ear->m_nearest ) best_ear = v;

						Vert& ear = *best_ear;//*ChainBegin(m_ear_chain);
						assert(ear.m_type == EEar);
						ChainInsert(ear, &m_free_chain);

						// Examine the adjacent verts
						Vert& vertL		 = *ear.m_edge_in;
						Vert& vertR		 = *ear.m_edge_out;
						vertL.m_edge_out = ear.m_edge_out;
						vertR.m_edge_in  = ear.m_edge_in;
						ExamineVert(vertL);
						ExamineVert(vertR);

						// Add a face to the triangulation
						AddFace(ear, ChainEmpty(m_ear_chain) && ChainEmpty(m_concave_chain));
						#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
						DumpVerts();
						#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND
					}

					// If there are still concave verts then the polygon
					// must have contained holes. Add diagonals and new ears
					if( !ChainEmpty(m_concave_chain) )
					{
						assert(!ChainEmpty(m_convex_chain));

						// Add diagonals. If there are verts free in the free chain,
						// use them, otherwise allocate them on the stack
						if( ChainSizeAtLeast(m_free_chain, 2) )
						{
							Vert& vert2 = *ChainBegin(m_free_chain); ChainRemove(vert2);
							Vert& diag2 = *ChainBegin(m_free_chain); ChainRemove(diag2);
							AddDiagonal(vert2, diag2);
						}
						else
						{
							// "Allocate" on the stack by calling this function recursively
							Vert vert2, diag2;
							AddDiagonal(vert2, diag2);
							ClipEars();
							return;
						}
					}
				}
				while( !ChainEmpty(m_ear_chain) );
			}

			#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
			void DumpVerts()
			{
				StartFile("C:/Deleteme/triangulate_verts.pr_script");
				ldr::GroupStart("Concave");
				for( Vert* v = ChainBegin(m_concave_chain); v != ChainEnd(m_concave_chain); v = v->m_next )
					ldr::Box("vert", "FF00FF00", Vertex(m_verts, v->m_idx), 0.08f);
				ldr::GroupEnd();

				ldr::GroupStart("Convex");
				for( Vert* v = ChainBegin(m_convex_chain); v != ChainEnd(m_convex_chain); v = v->m_next )
					ldr::Box("vert", "FF0000FF", Vertex(m_verts, v->m_idx), 0.08f);
				ldr::GroupEnd();

				ldr::GroupStart("Ears");
				for( Vert* v = ChainBegin(m_ear_chain); v != ChainEnd(m_ear_chain); v = v->m_next )
					ldr::Box("vert", "FFFFFF00", Vertex(m_verts, v->m_idx), 0.08f);
				ldr::GroupEnd();
				EndFile();
			}
			#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND
		};// Triangulator
	}//namespace triangulate

	// Triangulate a polygon.
	// 'verts' is a pointer to an array of vertices in the XY plane
	// 'num_verts' is the length of the 'verts' array
	// 'edges' is a pointer to an array of index pairs describing the edges of the polygon
	// 'num_edges' is the length of the 'edges' array
	// 'face_out' is an object that receives the faces of the triangulation
	// Notes:
	//	The polygon must not be self intersecting. If triangulation of a self intersecting polygon
	//	 is needed you'll first have to split any intersecting edges and add new vertices. This will
	//	 turn a self intersecting polygon into a collection of non-intersecting polygons which the
	//	 triangulate function can handle
	// Use: Triangulate<0, 1>(verts, num_verts, edges, num_edges, FaceOut()); // For XY
	template <int Axis0, int Axis1, typename VertCntr, typename EdgeCntr, typename FaceOut>
	void Triangulate(VertCntr const& verts, std::size_t num_verts, EdgeCntr const& edges, std::size_t num_edges, FaceOut& face_out)
	{
		assert(num_edges >= 2);
		#ifdef PR_GEOMETRY_TRIANGULATE_PR_EXPAND
		StartFile("C:/Deleteme/triangulate_result.pr_script"); EndFile();
		StartFile("C:/Deleteme/triangulate_polygon.pr_script");
		ldr::GroupStart("Polygon");
		for( std::size_t e = 0; e != num_edges; ++e )
			ldr::Line("edge", "FFA00000",
				triangulate::Vertex(verts, triangulate::EdgeIndex0(edges, e)),
				triangulate::Vertex(verts, triangulate::EdgeIndex1(edges, e)));
		ldr::GroupEnd();
		EndFile();
		#endif//PR_GEOMETRY_TRIANGULATE_PR_EXPAND

		// Build vert to edge adjacency data
		triangulate::Vert* adj = static_cast<triangulate::Vert*>(_malloca(num_verts * sizeof(triangulate::Vert)));
		memset(adj, 0, num_verts * sizeof(triangulate::Vert));
		for( std::size_t e = 0; e != num_edges; ++e )
		{
			using namespace triangulate;
			adj[EdgeIndex0(edges, e)].m_type		= EFree;
			adj[EdgeIndex0(edges, e)].m_idx			= EdgeIndex0(edges, e);
			adj[EdgeIndex0(edges, e)].m_edge_out	= &adj[EdgeIndex1(edges, e)];
			adj[EdgeIndex1(edges, e)].m_edge_in		= &adj[EdgeIndex0(edges, e)];
		}

		// Do the triangulation
		triangulate::Triangulator<Axis0, Axis1, VertCntr, FaceOut> triangulator(verts, num_verts, adj, face_out);

		// Free the adjacency data
		_freea(adj);
	}
}//namespace pr

#endif//PR_MATHS_TRIANGULATE_H
