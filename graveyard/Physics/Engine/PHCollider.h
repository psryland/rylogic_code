//**********************************************************************************
//
//	A class for doing collision detection between orientated geometric objects
//
//**********************************************************************************

#ifndef PR_PHYSICS_COLLIDER_H
#define PR_PHYSICS_COLLIDER_H

namespace pr
{
    namespace ph
	{
		class Collider
		{
		public:
			struct Info
			{
				v4		m_centre;
				v4		m_normal[3];
				v4		m_radius[3];
				uint	m_num_radii;
			};
			struct Overlap
			{
				enum Type { Point = 0, Edge = 1, Face = 2 };
				struct Pt { v4 m_point; int m_type; bool m_DOF[3]; };

				Overlap();
				void		Reverse();
				Overlap&	operator =(const Overlap& other);

				v4		m_axis;				// Always from pA to pB
				float	m_penetration;
				Pt		m_A;
				Pt		m_B;
				Pt*		m_pA;
				Pt*		m_pB;
				bool	m_reversed;
			};
			struct Params
			{
				v4*		m_separating_axis;
				uint	m_num_separating_axes;
				Info	m_objectA;
				Info	m_objectB;
				Overlap	m_min_overlap;
			};

		public:
			Collider(Params& params);
			void	GetMinOverlap(const v4& axis, const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& min_overlap);
			
		private:
			void	GetPointOfContact(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& min_overlap);
			void	PointToPoint(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
			void	PointToEdge (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
			void	PointToFace (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
			void	EdgeToEdge  (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
			void	EdgeToFace  (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
			void	FaceToFace  (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap);
		//PSR...	void	Clip(v4& start, v4& end, const v4& pt, const v4& normal);
		};

		//*****************************************
		// Implementation
		inline Collider::Overlap::Overlap()
		{
			m_penetration	= FLT_MAX;
			m_pA			= &m_A;
			m_pB			= &m_B;
			m_reversed		= false;
		}

		inline void	Collider::Overlap::Reverse()
		{
			m_reversed	= !m_reversed;
			Pt* tmp		=  m_pA;
			m_pA		=  m_pB;
			m_pB		=  tmp;
			m_axis		= -m_axis;
		}

		inline Collider::Overlap& Collider::Overlap::operator =(const Collider::Overlap& other)
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

#endif//PR_PHYSICS_COLLIDER_H
