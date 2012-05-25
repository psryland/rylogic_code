//****************************************************
//
//	Static Physics Instance
//
//****************************************************
// This is an instance of a static collision shape
#ifndef PR_PHYSICS_STATIC_INSTANCE_H
#define PR_PHYSICS_STATIC_INSTANCE_H

namespace pr
{
	namespace ph
	{
		struct StaticInstance
		{
			StaticInstance();

			// Accessors
			const BoundingBox&	BBox() const						{ return m_rigid_body->m_bbox; }
			const BoundingBox&	BBoxWS() const						{ return m_ws_bbox; }
			const Shape&		GetShape() const					{ return *m_rigid_body; }
			const m4x4&			ObjectToWorld() const				{ return *m_object_to_world; }

		private:
			Shape*		m_shape;					// The collision shape
			m4x4*		m_object_to_world;			// The transform from physics object space into world space
			BoundingBox	m_ws_bbox;					// The world space bounding box for this object. Calculated per step
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_STATIC_INSTANCE_H

