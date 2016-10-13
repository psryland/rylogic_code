//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <random>
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/bbox.h"
#include "pr/maths/rand_vector.h"

namespace pr
{
	// x = start position
	// y = start control point. tangent = y - x
	// z = end control point.   tangent = w - z
	// w = end position
	struct Spline :m4x4
	{
		enum { Start, SCtrl, ECtrl, End };

		// Construct a spline from 4 control points
		Spline() = default;
		Spline(v4 const& start, v4 const& start_ctrl, v4 const& end_ctrl, v4 const& end)
			:m4x4(start, start_ctrl, end_ctrl, end)
		{
			assert(start.w == 1.0f && start_ctrl.w == 1.0f && end_ctrl.w == 1.0f && end.w == 1.0f && "Splines are constructed from 4 positions");
		}
		Spline(v4 const* spline)
			:Spline(spline[Spline::Start], spline[Spline::SCtrl], spline[Spline::ECtrl], spline[Spline::End])
		{}

		// Interpretation of the control points
		v4 Point0() const   { return x; }
		v4 Forward0() const { return y - x; }
		v4 Forward1() const { return w - z; }
		v4 Point1() const   { return w; }

		// Return the position along the spline at 'time'
		v4 Position(float time) const
		{
			v4 blend;
			blend.x = (1.0f - time) * (1.0f - time) * (1.0f - time);
			blend.y = 3.0f * time * (1.0f - time) * (1.0f - time);
			blend.z = 3.0f * time * time * (1.0f - time);
			blend.w = time * time * time;
			return (static_cast<m4x4 const&>(*this) * blend).w1();
		}

		// Return the tangent along the spline at 'time'
		// Notes about velocity:
		// A spline from (0,0,0) to (1,0,0) with control points at (1/3,0,0) and (2/3,0,0) will
		// have a constant velocity of (1,0,0) over the full length of the spline.
		v4 Velocity(float time) const
		{
			v4 dblend; // the derivative of blend
			dblend.x = 3.0f * (time - 1.0f) * (1.0f - time);
			dblend.y = 3.0f * (1.0f - time) * (1.0f - 3.0f * time);
			dblend.z = 3.0f * time * (2.0f - 3.0f * time);
			dblend.w = 3.0f * time * time;
			return (static_cast<m4x4 const&>(*this) * dblend).w0();
		}

		// Return the acceleration along the spline at 'time'
		v4 Acceleration(float time) const
		{
			v4 ddblend; // the 2nd derivative of blend
			ddblend.x = 6.0f * (1.0f - time);
			ddblend.y = 6.0f * (3.0f * time - 2.0f);
			ddblend.z = 6.0f * (1.0f - 3.0f * time);
			ddblend.w = 6.0f * time;
			return (static_cast<m4x4 const&>(*this) * ddblend).w0();
		}

		// Return an object to world transform for a position along the spline
		// 'axis' is the axis id that will lie along the tangent of the spline
		// By default, the z axis is aligned to the spline with Y as up
		m4x4 O2W(float time) const
		{
			return O2W(time, 2, v4YAxis);
		}
		m4x4 O2W(float time, int axis, v4 const& up) const
		{
			return OriFromDir(Velocity(time), axis, up, Position(time));
		}
	};

	// Equality operators
	inline bool operator == (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Split 'spline' at 't' to produce two new splines
	// Given the 4 control points P0,P1,P2,P3 of 'spline'
	// the position is given by:
	//  P4 = lerp(P0,P1,t); P5 = lerp(P1,P2,t); P6 = lerp(P2,P3,t);
	//  P7 = lerp(P4,P5,t); P8 = lerp(P5,P6,t);
	//  P9 = lerp(P7,P8,t);
	// The two resulting splines 'lhs' and 'rhs' are:
	//  lhs = P0,P4,P7,P9;  rhs = P9,P8,P6,P3
	// Note: 'spline' passed by value to prevent aliasing problems with 'lhs' and 'rhs'
	inline void Split(Spline const& spline, float t, Spline& lhs, Spline& rhs)
	{
		assert(&lhs != &rhs && "lhs and rhs must not be the same spline");
		v4 P5;
		lhs.x = spline.x;                      // P0
		lhs.y = Lerp(spline.x, spline.y, t);   // P4
		P5    = Lerp(spline.y, spline.z, t);   // P5
		rhs.z = Lerp(spline.z, spline.w, t);   // P6
		rhs.w = spline.w;                      // P3
		lhs.z = Lerp(lhs.y, P5, t);            // P7
		rhs.y = Lerp(P5, rhs.z, t);            // P8
		lhs.w = rhs.x = Lerp(lhs.z, rhs.y, t); // P9
	}

	// Return the length of a spline from t0 to t1
	inline float Length(Spline const& spline, float t0, float t1, float tol = maths::tiny)
	{
		struct L { static float Len(Spline const& s, float tol)
		{
			float poly_length  = Length3(s.y - s.x) + Length3(s.z - s.y) + Length3(s.w - s.z);
			float chord_length = Length3(s.w - s.x);
			if ((poly_length - chord_length) < tol) return (poly_length + chord_length) * 0.5f;

			Spline lhs, rhs;
			Split(s, 0.5f, lhs, rhs);
			return Len(lhs, tol) + Len(rhs, tol);
		}};

		// Trim 'spline' to the region of interest
		Spline clipped = spline, dummy;
		if (t0 != 0.0f) Split(clipped, t0, dummy, clipped);
		if (t1 != 1.0f) Split(clipped, t1, clipped, dummy);
		return L::Len(clipped, tol);
	}

	// Find the closest point on 'spline' to 'pt'
	// Note: the analytic solution to this problem involves solving a 5th order polynomial
	// This method uses Newton's method and relies on a "good" initial estimate of the nearest point
	// Should have quadratic convergence
	inline float ClosestPoint_PointToSpline(Spline const& spline, v4 const& pt, float initial_estimate, bool bound01 = true, int iterations = 5)
	{
		// The distance (squared) from 'pt' to the spline is: Dist(t) = |pt - S(t)|^2.    (S(t) = spline at t)
		// At the closest point, Dist'(t) = 0.
		// Dist'(t) = -2(pt - S(t)).S'(t)
		// So we want to find 't' such that Dist'(t) = 0
		// Newton's method of iteration = t_next = t_current - f(x)/f'(x)
		//	f(x) = Dist'(t)
		//	f'(x) = Dist''(t) = 2S'(t).S'(t) - 2(pt - S(t)).S''(t)
		float time = initial_estimate;
		for (int iter = 0; iter != iterations; ++iter)
		{
			v4 S   = spline.Position(time);
			v4 dS  = spline.Velocity(time);
			v4 ddS = spline.Acceleration(time);
			v4 R   = pt - S;
			time += Dot3(R, dS) / (Dot3(dS,dS) - Dot3(R,ddS));
			if (bound01 && (time <= 0.0f || time >= 1.0f))
				return Clamp(time, 0.0f, 1.0f);
		}
		return time;
	}

	// This overload attempts to find the nearest point robustly
	// by testing 3 starting points and returning minimum.
	inline float ClosestPoint_PointToSpline(Spline const& spline, v4 const& pt, bool bound01 = true)
	{
		float t0 = ClosestPoint_PointToSpline(spline, pt, -0.5f, bound01, 5);
		float t1 = ClosestPoint_PointToSpline(spline, pt,  0.5f, bound01, 5);
		float t2 = ClosestPoint_PointToSpline(spline, pt,  1.5f, bound01, 5);
		float d0 = Length3Sq(pt - spline.Position(t0));
		float d1 = Length3Sq(pt - spline.Position(t1));
		float d2 = Length3Sq(pt - spline.Position(t2));
		if (d0 < d1 && d0 < d2) return t0;
		if (d1 < d0 && d1 < d2) return t1;
		return t2;
	}

	// Fill a container of points with a rastered version of 'spline'
	// 'points' is the vert along the spline
	// 'times' is the times along 'spline' at the point locations
	template <typename PCont, typename TCont>
	void Raster(Spline const& spline, PCont& points, TCont& times, int max_points, float tol = maths::tiny)
	{
		struct L
		{
			struct Elem // Linked list element type used to form a priority queue
			{
				Elem*         m_next;      // The next subsection of spline with less error
				Spline const* m_spline;    // Pointer to the subsection of the spline
				float         m_t0, m_t1;  // Times along the original spline
				int           m_ins;       // The position to insert a vert in the out container
				float         m_err;       // How much a straight line diverges from 'm_spline'
				
				Elem(Spline const& s, float t0, float t1, int ins)
					:m_next(0)
					,m_spline(&s)
					,m_t0(t0)
					,m_t1(t1)
					,m_ins(ins)
					,m_err(Length3(s.y - s.x) + Length3(s.z - s.y) + Length3(s.w - s.z) - Length3(s.w - s.x))
				{}
			};

			// Insert 'elem' into the priority queue of which 'queue' is the head
			static Elem* QInsert(Elem* queue, Elem& elem)
			{
				if (queue == 0 || queue->m_err < elem.m_err) { elem.m_next = queue; return &elem; }
				Elem* i; for (i = queue; i->m_next && i->m_next->m_err > elem.m_err; i = i->m_next) {}
				elem.m_next = i->m_next; // 'i->m_next' has an error less than 'elem' (i.m_next might be 0)
				i->m_next = &elem;
				return queue;
			}

			// Breadth-first recursive raster of this spline
			static void Raster(PCont& points, TCont& times, Elem* queue, int& pts_remaining, float tol)
			{
				// Pop the top spline segment from the queue or we've run out of points
				Elem const& elem = *queue;
				queue = queue->m_next;

				// Stop if the error is less than 'tol'
				if (elem.m_err < tol || pts_remaining <= 0)
					return;

				// Subdivide the spline segment and insert the mid-point into 'points'
				Spline Lhalf, Rhalf;                       // Spline segments for each half
				Split(*elem.m_spline, 0.5f, Lhalf, Rhalf); // splits the spline at t=0.5
				float t = (elem.m_t0 + elem.m_t1) * 0.5f;  // Find the time on the original spline
				points.insert(points.begin() + elem.m_ins, Lhalf.Point1());
				times .insert(times .begin() + elem.m_ins, t);

				// Increment the insert position for all elements after 'elem.m_ins'
				for (Elem* i = queue; i != 0; i = i->m_next)
					i->m_ins += (i->m_ins > elem.m_ins);

				// Insert both halves into the queue
				Elem lhs(Lhalf, elem.m_t0, t, elem.m_ins  );
				Elem rhs(Rhalf, t, elem.m_t1, elem.m_ins+1);
				queue = QInsert(queue, lhs);
				queue = QInsert(queue, rhs);
				Raster(points, times, queue, --pts_remaining, tol); // continue recursively
			}
		};

		points.insert(std::end(points), spline.Point0());
		points.insert(std::end(points), spline.Point1());
		times.insert(std::end(times), 0.0f);
		times.insert(std::end(times), 1.0f);

		int pts_remaining = max_points - 2;
		L::Elem elem(spline, 0.0f, 1.0f, 1);
		L::Raster(points, times, &elem, pts_remaining, tol);
	}
	template <typename PCont>
	void Raster(Spline const& spline, PCont& points, int max_points, float tol = maths::tiny)
	{
		// Dummy container for the time values
		struct TCont
		{
			int begin() { return 0; }
			int end()   { return 0; }
			void insert(int, float) {}
		} times;
		Raster(spline, points, times, max_points, tol);
	}

	// Convert a container of points into a list of splines
	// Generates a spline from each set of three points in 'points'
	// VOut(Spline const& spline, bool last);
	template <typename Cont, typename VOut>
	void CreateSplines(Cont const& points, VOut out)
	{
		if (points.size() < 2)
		{
			return;
		}
		if (points.size() < 3)
		{
			out(Spline(points[0], points[1], points[0], points[1]), true);
			return;
		}

		// Generate a spline from each set of three points in 'points'
		for (size_t i = 1, iend = points.size() - 1; i != iend; ++i)
		{
			// Generate points for the spline
			auto sp = i == 1 ? points[i-1] : (points[i-1] + points[i]) * 0.5f;
			auto sc = points[i];
			auto ec = points[i];
			auto ep = i == iend-1 ? points[i+1] : (points[i+1] + points[i]) * 0.5f;
			auto spline = Spline(sp, sc, ec, ep);
			out(spline, i == iend-1);
		}
	}

	// Fill a container of points with a smoothed spline based on 'points
	template <typename Cont, typename VOut, int MaxPointsPerSpline = 30>
	void Smooth(Cont const& points, VOut out, float tol = maths::tiny)
	{
		if (points.size() < 3)
		{
			float times[] = {0.0f, 1.0f};
			out(points.data(), times, points.size());
			return;
		}

		vector<v4, MaxPointsPerSpline, true> raster;
		vector<float , MaxPointsPerSpline, true> times;
		CreateSplines(points, [&](Spline const& spline, bool last)
		{
			raster.resize(0);
			times.resize(0);

			// Raster the spline into a temp buffer
			Raster(spline, raster, times, MaxPointsPerSpline, tol);

			// Stream out the verts
			out(raster.data(), times.data(), raster.size() - !last);
		});
	}
	template <typename Cont, int MaxPointsPerSpline = 30>
	void Smooth(Cont const& points, Cont& out, float tol = maths::tiny)
	{
		Smooth(points, [&](v4 const* vert, float const*, size_t count)
		{
			out.insert(std::end(out), vert, vert + count);
		}, tol);
	}
	
	// Random infinite spline within a bounding box
	template <typename Rng = std::default_random_engine> 
	struct RandSpline :Spline
	{
		Rng*   m_rng;
		Spline m_next;
		BBox   m_bbox;
		float  m_clock;  // The current 'time' along the spline [0,1)

		RandSpline(Rng& rng, BBox const& bbox = BBoxUnit)
			:m_rng(&rng)
			,m_next()
			,m_bbox(bbox)
			,m_clock(0.0f)
		{
			m_next = Spline(GenPoint(), GenPoint(), GenPoint(), GenPoint());
			Roll();
			Roll();
		}
		void Reset(Rng& rng)
		{
			m_rng = &rng;
			m_clock = 0.0f;
		}
		v4 GenPoint()
		{
			return Random3(*m_rng, m_bbox.Lower(), m_bbox.Upper(), 1.0f);
		}
		void Roll()
		{
			static_cast<Spline&>(*this) = m_next;
			m_next.x = m_next.Point1();
			m_next.y = m_next.Point1() + m_next.Forward1();
			m_next.z = GenPoint();
			m_next.w = GenPoint();
		}
		void Adv(float dt)
		{
			m_clock += dt;
			for (int i = 0; i != 2 && m_clock >= 1.0f; ++i, m_clock -= 1.0f) Roll();
		}

		// Return an object to world transform for the current position on the spline
		m4x4 O2W() const
		{
			return Spline::O2W(m_clock);
		}
		m4x4 O2W(int axis, v4 const& up) const
		{
			return Spline::O2W(m_clock, axis, up);
		}

		// Return the current position along the spline
		v4 Position() const
		{
			return Spline::Position(m_clock);
		}

		// Return the current velocity along the spline
		v4 Velocity() const
		{
			return Spline::Velocity(m_clock);
		}

		// Return the acceleration along the spline at 'time'
		v4 Acceleration() const
		{
			return Spline::Acceleration(m_clock);
		}
	};
}
