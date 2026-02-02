//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2006
//********************************
#pragma once
#include <cassert>
#include <concepts>
#include <type_traits>
#include "pr/common/cast.h"
#include "pr/common/range.h"
#include "pr/common/fmt.h"
#include "pr/common/repeater.h"
#include "pr/common/interpolate.h"
#include "pr/common/flags_enum.h"
#include "pr/algorithm/algorithm.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/container/ring.h"
#include "pr/gfx/colour.h"
#include "pr/maths/maths.h"
#include "pr/maths/line3.h"
#include "pr/maths/interpolate.h"

namespace pr::geometry
{
	// Notes:
	//  - Ray is an infinite line defined as 'start' and 'direction': Ray = start + t*direction
	//  - Line is a line segment defined as 'start' and 'end' points: Line = (1-t)*start + t*end for t in [0,1]
	//  - All points are in homogeneous coordinates (x,y,z,w) where w==1 for positions and w==0 for directions

	// EGeom
	enum class EGeom
	{
		Invalid = 0,
		None    = 0,
		Vert    = 1 << 0, // Object space 3D position
		Colr    = 1 << 1, // Diffuse base colour
		Norm    = 1 << 2, // Object space 3D normal
		Tex0    = 1 << 3, // Diffuse texture
		All     = Vert | Colr | Norm | Tex0,

		_flags_enum = 0,
	};
	static_assert(is_flags_enum_v<EGeom>);

	// ETopo
	enum class ETopo
	{
		// Note: don't assume these are the same as directX. Dx11/Dx12 have different values
		Undefined = 0,
		PointList,
		LineList,
		LineStrip,
		TriList,
		TriStrip,
		LineListAdj,
		LineStripAdj,
		TriListAdj,
		TriStripAdj,
	};

	// EPrimGroup
	enum class ETopoGroup
	{
		None,
		Points,
		Lines,
		Triangles,
	};

	// Parts of an model file scene
	enum class ESceneParts
	{
		None           = 0,
		GlobalSettings = 1 << 0,
		NodeHierarchy  = 1 << 1,
		Materials      = 1 << 2,
		Meshes         = 1 << 3,
		Skeletons      = 1 << 4,
		Skins          = 1 << 5 | Meshes | Skeletons,
		Animation      = 1 << 6,
		MainObjects    = 1 << 7,

		All           = Meshes | Materials | Skeletons | Skins | Animation,
		ModelOnly     = Meshes | Materials,
		SkinnedModels = ModelOnly | Skins,
		AnimationOnly = Skeletons | Animation,

		_flags_enum = 0,
	};

	// Generator function signatures
	template <typename T> concept VertCIter = requires (T t) { { *t++ } -> std::convertible_to<v4>; };
	template <typename T> concept VertOutputFn = std::invocable<T, v4 const&, Colour32, v4 const&, v2 const&>;
	template <typename T> concept IndexOutputFn = std::is_invocable_v<T, int>;
	template <typename T> concept GetVertFn = std::is_invocable_r_v<v4 const&, T, int>;
	template <typename T> concept GetNormFn = std::is_invocable_r_v<v4 const&, T, int>;
	template <typename T> concept SetNormFn = std::is_invocable_v<T, int, v4 const&>;

	// Geometry properties
	struct Props
	{
		BBox  m_bbox;      // Bounding box in model space of the generated model
		EGeom m_geom;      // The components of the generated geometry
		bool  m_has_alpha; // True if the model contains any alpha

		Props()
			:m_bbox(BBox::Reset())
			,m_geom(EGeom::Vert)
			,m_has_alpha(false)
		{}
	};

	// Vertex and Index buffer sizes
	struct BufSizes
	{
		int vcount;
		int icount;

		constexpr BufSizes(int nv, int ni)
			:vcount(nv)
			,icount(ni)
		{}
	};

	// Classify topology types
	constexpr ETopoGroup TopoGroup(ETopo topo)
	{
		return
			topo == ETopo::TriList || topo == ETopo::TriListAdj ? ETopoGroup::Triangles :
			topo == ETopo::TriStrip || topo == ETopo::TriStripAdj ? ETopoGroup::Triangles :
			topo == ETopo::LineList || topo == ETopo::LineListAdj ? ETopoGroup::Lines :
			topo == ETopo::LineStrip || topo == ETopo::LineStripAdj ? ETopoGroup::Lines :
			topo == ETopo::PointList ? ETopoGroup::Points :
			ETopoGroup::None;
	}

	// An iterator wrapper for applying a transform to 'points'
	template <typename TVertCIter> struct Transformer
	{
		using iterator_category = std::forward_iterator_tag;
		using value_type = typename std::iterator_traits<TVertCIter>::value_type;
		using difference_type = typename std::iterator_traits<TVertCIter>::difference_type;
		using reference = typename std::iterator_traits<TVertCIter>::reference;
		using pointer = typename std::iterator_traits<TVertCIter>::pointer;

		TVertCIter m_pt;
		m4x4 const* m_o2w;

		Transformer(TVertCIter points, m4x4 const& o2w)
			:m_pt(points)
			,m_o2w(&o2w)
		{}
		v4 operator*() const
		{
			return *m_o2w * *m_pt;
		}
		Transformer& operator ++()
		{
			++m_pt;
			return *this;
		}
		Transformer operator ++(int)
		{
			auto x = *this;
			++(*this);
			return x;
		}
	};

	// Output iterator adapter for flipping faces
	template <typename TIdxIter> struct FaceFlipper
	{
		// Notes:
		//  - Don't implement post-increment. Code that uses post increment won't work
		//    with this iterator because the assignment occurs after the increment. This
		//    iterator wrapper outputs to the underlying iterator on increment.

		using iterator_category = std::output_iterator_tag;
		using value_type = typename std::iterator_traits<TIdxIter>::value_type;
		using difference_type = void;
		using reference = void;
		using pointer = void;

		TIdxIter m_out;
		value_type m_idx[3];
		int m_count;

		FaceFlipper(TIdxIter out)
			:m_out(out)
			,m_idx()
			,m_count()
		{}
		FaceFlipper& operator*()
		{
			return *this;
		}
		FaceFlipper& operator =(value_type idx)
		{
			m_idx[m_count] = idx;
			return *this;
		}
		FaceFlipper& operator ++()
		{
			if (++m_count == 3)
			{
				*m_out++ = m_idx[0];
				*m_out++ = m_idx[2];
				*m_out++ = m_idx[1];
				m_count = 0;
			}
			return *this;
		}
		FaceFlipper operator ++(int) = delete;
	};

	// Closest point result object
	struct MinSeparation
	{
		v4 m_axis;
		float m_axis_len_sq;
		float m_depth_sq;

		MinSeparation()
			:m_axis()
			,m_axis_len_sq()
			,m_depth_sq(maths::float_inf)
		{}

		// Boolean test of penetration
		bool Contact() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_depth_sq > 0;
		}

		// Return the depth of penetration
		float Depth() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return SignedSqrt(m_depth_sq);
		}

		// The direction of minimum penetration (normalised)
		v4 SeparatingAxis() const
		{
			assert("No separating axes have been tested yet" && m_depth_sq != maths::float_inf);
			return m_axis / Sqrt(m_axis_len_sq);
		}

		// Record the minimum depth separation
		void operator()(float depth, v4_cref axis)
		{
			// Defer the sqrt by comparing squared depths.
			// Need to preserve the sign however.
			auto len_sq = LengthSq(axis);
			auto d_sq = SignedSqr(depth) / len_sq;
			if (d_sq < m_depth_sq)
			{
				m_axis = axis;
				m_axis_len_sq = len_sq;
				m_depth_sq = d_sq;
			}
		};
	};

	// Forward declare functions
	v4 pr_vectorcall BaryPoint(v4_cref a, v4_cref b, v4_cref c, v4_cref bary);
	v4 pr_vectorcall BaryPoint(v4_cref a, v4_cref b, v4_cref c, v3 bary);
	v4 pr_vectorcall Barycentric(v4_cref point, v4_cref a, v4_cref b, v4_cref c);
	bool pr_vectorcall PointWithinTriangle(v4_cref point, v4_cref a, v4_cref b, v4_cref c, float tol);
	bool pr_vectorcall PointWithinTriangle2(v4_cref point, v4_cref a, v4_cref b, v4_cref c, float tol);
	bool pr_vectorcall PointWithinTriangle(v4_cref point, v4_cref a, v4_cref b, v4_cref c, v4& pt);
	bool pr_vectorcall PointWithinTetrahedron(v4_cref point, v4_cref a, v4_cref b, v4_cref c, v4_cref d);
	bool pr_vectorcall PointWithinConvexPolygon(v4_cref point, v4 const* poly, int count, v4_cref norm);
	bool pr_vectorcall PointWithinConvexPolygon(v4_cref point, v4 const* poly, int count);
	bool pr_vectorcall PointWithinHalfSpaces(v4_cref point, Plane const* planes, int count, float tol);
	bool pr_vectorcall PointInFrontOfPlane(v4_cref point, v4_cref a, v4_cref b, v4_cref c);

	namespace distance
	{
		float pr_vectorcall PointToPlane(v4_cref point, v4_cref a, v4_cref b, v4_cref c);
		float pr_vectorcall PointToPlane(v4_cref point, Plane const& plane);
		float pr_vectorcall PointToRay(v4_cref point, v4_cref start, v4_cref end);
		float pr_vectorcall RayToRay(v4_cref s0, v4_cref line0, v4_cref s1, v4_cref line1);
		float pr_vectorcall PointToRaySq(v4_cref point, v4_cref s, v4_cref d);
		float pr_vectorcall PointToLineSq(v4_cref point, v4_cref s, v4_cref e);
		float pr_vectorcall PointToBoundingBoxSq(v4_cref point, BBox const& bbox);
		float pr_vectorcall PointToTriangleSq(v4_cref point, v4_cref a, v4_cref b, v4_cref c);
		float pr_vectorcall LineToBBox(v4_cref s, v4_cref e, BBox_cref bbox);
	}
	namespace closest_point
	{
		v4 pr_vectorcall PointToPlane(v4_cref point, Plane const& plane);
		v4 pr_vectorcall PointToPlane(v4_cref point, v4_cref a, v4_cref b, v4_cref c);
		v4 pr_vectorcall PointToRay(v4_cref point, v4_cref start, v4_cref end, float& t);
		v4 pr_vectorcall PointToRay(v4_cref point, v4_cref start, v4_cref end);
		v4 pr_vectorcall PointToRay(v4_cref point, const Line3& line, float& t);
		v4 pr_vectorcall PointToRay(v4_cref point, const Line3& line);
		v4 pr_vectorcall PointToLine(v4_cref point, v4_cref s, v4_cref e, float& t);
		v4 pr_vectorcall PointToLine(v4_cref point, v4_cref start, v4_cref end);
		v4 pr_vectorcall PointToLine(v4_cref point, Line3 const& line, float& t);
		v4 pr_vectorcall PointToLine(v4_cref point, Line3 const& line);
		v4 pr_vectorcall PointToBoundingBox(v4_cref point, BBox_cref bbox, bool surface_only = false);
		v2 pr_vectorcall PointToEllipse(float x, float y, float major, float minor);
		v4 pr_vectorcall PointToTriangle(v4_cref p, v4_cref a, v4_cref b, v4_cref c, v4& barycentric);
		v4 pr_vectorcall PointToTriangle(v4_cref point, v4_cref a, v4_cref b, v4_cref c);
		v4 pr_vectorcall PointToTriangle(v4_cref point, const v4* tri, v4& barycentric);
		v4 pr_vectorcall PointToTriangle(v4_cref point, const v4* tri);
		v4 pr_vectorcall PointToTetrahedron(v4_cref p, v4_cref a, v4_cref b, v4_cref c, v4_cref d, v4& barycentric);
		v4 pr_vectorcall PointToTetrahedron(v4_cref point, v4_cref a, v4_cref b, v4_cref c, v4_cref d);
		v4 pr_vectorcall PointToTetrahedron(v4_cref point, const v4* tetra, v4& barycentric);
		v4 pr_vectorcall PointToTetrahedron(v4_cref point, const v4* tetra);
		v2 pr_vectorcall RayToRay(v4_cref s0, v4_cref d0, v4_cref s1, v4_cref d1);
		void pr_vectorcall LineToLine(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref e1, float& t0, float& t1);
		void pr_vectorcall LineToLine(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref e1, v4& pt0, v4& pt1);
		void pr_vectorcall LineToLine(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref e1, v4& pt0, v4& pt1, float& t0, float& t1);
		void pr_vectorcall LineToLine(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref e1, float& dist_sq);
		void pr_vectorcall LineToRay(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref line1, float& t0, float& t1);
		void pr_vectorcall LineToRay(v4_cref s0, v4_cref e0, v4_cref s1, v4_cref line1, float& t0, float& t1, float& dist_sq);
		MinSeparation pr_vectorcall LineToBBox(v4_cref s, v4_cref e, BBox_cref bbox);
		MinSeparation pr_vectorcall LineToBBox(v4_cref s, v4_cref e, BBox_cref bbox, float& t);
		MinSeparation pr_vectorcall LineToBBox(v4_cref s, v4_cref e, BBox_cref bbox, v4& pt0, v4& pt1);
		v4 pr_vectorcall RayToTriangle(v4_cref s, v4_cref d, v4_cref a, v4_cref b, v4_cref c);
	}
	namespace intersect
	{
		bool RayVsRay(v2_cref p0, v2_cref d0, v2_cref p1, v2_cref d1);
		bool RayVsRay(v2_cref p0, v2_cref d0, v2_cref p1, v2_cref d1, v2& intersect);
		bool LineVsLine(v2_cref a0, v2_cref a1, v2_cref b0, v2_cref b1, float& ta, float& tb);
		bool LineVsBBox(v2_cref a, v2_cref b, v2_cref bbox_min, v2_cref bbox_max, v2& A, v2& B);
		bool pr_vectorcall RayVsTriangle(v4_cref s, v4_cref d, int dummy, v4_cref a, v4_cref b, v4_cref c);
		bool pr_vectorcall RayVsTriangle(v4_cref s, v4_cref d, int dummy, v4_cref a, v4_cref b, v4_cref c, v4& bary);
		bool pr_vectorcall RayVsTriangle(v4_cref s, v4_cref d, int dummy, v4_cref a, v4_cref b, v4_cref c, float& front_to_back, v4& bary);
		bool pr_vectorcall RayVsTriangle(v4_cref s, v4_cref d, int dummy, v4_cref a, v4_cref b, v4_cref c, float* t, v4* bary, float* f2b, float tmin, float tmax);
		bool pr_vectorcall RayVsSphere(v4_cref s, v4_cref d, float radius, float& tmin, float& tmax);
		bool pr_vectorcall RayVsBBox(v4_cref s, v4_cref d, BBox_cref box, float& tmin, float& tmax);
		bool pr_vectorcall RayVsFrustum(v4_cref s, v4_cref d, Frustum const& frustum, bool accumulative, float& t0, float& t1, bool include_zfar);
		bool pr_vectorcall LineVsPlane(Plane const& plane, v4_cref s, v4_cref e, float& t0, float& t1);
		bool pr_vectorcall LineVsPlane(Plane const& plane, v4_cref s, v4_cref e, v4& s_out, v4& e_out);
		bool pr_vectorcall LineVsBoundingBox(v4_cref s, v4_cref e, BBox_cref bbox);
		bool pr_vectorcall RayVsPlane(Plane const& plane, v4_cref s, v4_cref e, float* t, float tmin, float tmax);
		bool pr_vectorcall LineVsSlab(v4_cref norm, float dist1, float dist2, v4_cref s, v4_cref e, v4& s_out, v4& e_out);
		bool pr_vectorcall BBoxVsPlane(BBox_cref bbox, Plane const& plane);
		bool pr_vectorcall BBoxVsBBox(BBox_cref lhs, BBox_cref rhs);
		bool pr_vectorcall OBoxVsOBox(OBox const& lhs, OBox const& rhs);
		bool pr_vectorcall ConvexPolygonVsConvexPolygon(v4 const* poly0, int count0, v4 const* poly1, int count1, v4_cref norm, std::invocable<v4> auto& out);
}
}
