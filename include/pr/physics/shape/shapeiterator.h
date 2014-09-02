//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PHYSICS_SHAPE_ITERATOR_H
#define PR_PHYSICS_SHAPE_ITERATOR_H

#include "PR/Physics/Types/Types.h"
#include "PR/Physics/Utility/Assert.h"
#include "PR/Physics/Shape/Shape.h"

namespace pr
{
	namespace ph
	{
		struct ShapeCIter
		{
			union {
			Shape const* m_shape;
			uint8 const* m_byte;
			void  const* m_ptr;
			};
			ShapeCIter&		operator ++()		{ m_byte += m_shape->m_size; return *this; }
			ShapeCIter&		operator ++(int)	{ ShapeCIter iter = *this; ++(*this); return iter; }
			Shape const&	operator *() const	{ return *m_shape; }
			Shape const*	operator ->() const	{ return m_shape; }
		};

		struct ShapeIter
		{
			union {
			Shape*	m_shape;
			uint8*	m_byte;
			void*	m_ptr;
			};
			ShapeIter&		operator ++()		{ m_byte += m_shape->m_size; return *this; }
			ShapeIter&		operator ++(int)	{ ShapeIter iter = *this; ++(*this); return iter; }
			Shape const&	operator *() const	{ return *m_shape; }
			Shape&			operator *()		{ return *m_shape; }
			Shape const*	operator ->() const	{ return m_shape; }
			Shape*			operator ->()		{ return m_shape; }
			operator ShapeCIter() const			{ ShapeCIter i={{m_shape}}; return i; }
		};

		inline bool operator == (ShapeCIter lhs, ShapeCIter rhs) { return lhs.m_shape == rhs.m_shape; }
		inline bool operator != (ShapeCIter lhs, ShapeCIter rhs) { return lhs.m_shape != rhs.m_shape; }
		inline bool operator <  (ShapeCIter lhs, ShapeCIter rhs) { return lhs.m_shape <  rhs.m_shape; }
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_SHAPE_ITERATOR_H
