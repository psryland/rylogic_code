//****************************************************
//
//	Object used to find collisions between oriented cylinders
//
//****************************************************

#ifndef PR_PHYSICS_CYLINDER_COLLIDER_H
#define PR_PHYSICS_CYLINDER_COLLIDER_H

namespace pr
{
	namespace ph
	{
		class CylinderCollider
		{
		public:
			struct Cylinder
			{
				v4		m_centre;
				v4		m_normal[2];		// Along the axis of the cylinder, in the direction of the other cylinder (can be zero)
				v4		m_radius[2];
			};
			struct Overlap
			{
				enum Type { Edge = 0, Wall = 1, Face = 2 };
				struct Point { v4 m_point; int m_type; bool m_DOF[3]; };

				Overlap();
				void		Reverse();
				Overlap&	operator =(const Overlap& other);

				v4		m_axis;				// Always from pA to pB
				float	m_penetration;
				Point	m_A;
				Point	m_B;
				Point*	m_pA;
				Point*	m_pB;
				bool	m_reversed;
			};

		public:
			CylinderCollider(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, Contact& contact);
			
		private:
			void	GetMinOverlap(const v4& axis, const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& min_overlap);
			void	GetPointOfContact(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& min_overlap);
			void	CornerToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap);
			void	EdgeToEdge(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap);
			void	EdgeToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap);
			void	FaceToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap);
			void	Clip(v4& start, v4& end, const v4& pt, const v4& normal);

		private:
		};

		//*****************************************
		// Implementation
		inline CylinderCollider::Overlap::Overlap()
		{
			m_penetration	= FLT_MAX;
			m_pA			= &m_A;
			m_pB			= &m_B;
			m_reversed		= false;
		}

		inline void	CylinderCollider::Overlap::Reverse()
		{
			m_reversed	= !m_reversed;
			Point* tmp	=  m_pA;
			m_pA		=  m_pB;
			m_pB		=  tmp;
			m_axis		= -m_axis;
		}

		inline CylinderCollider::Overlap& CylinderCollider::Overlap::operator =(const CylinderCollider::Overlap& other)
		{
			m_axis			= other.m_axis;
			m_reversed		= other.m_reversed;
			m_penetration	= other.m_penetration;
			memcpy(&m_A, &other.m_A, sizeof(m_A));
			memcpy(&m_B, &other.m_B, sizeof(m_B));
			if( !m_reversed )
			{
				m_pA = &m_A;
				m_pB = &m_B;
			}
			else
			{
				m_pA = &m_B;
				m_pB = &m_A;
			}
			return *this;
		}
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_CYLINDER_COLLIDER_H
