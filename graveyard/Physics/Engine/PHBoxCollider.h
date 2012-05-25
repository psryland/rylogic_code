//****************************************************
//
//	Object used to find collisions between oriented boxes
//
//****************************************************

#ifndef PR_PHYSICS_BOX_COLLIDER_H
#define PR_PHYSICS_BOX_COLLIDER_H

namespace pr
{
    namespace ph
	{
		class BoxCollider
		{
		public:
			struct Box
			{
				v4	m_centre;
				v4	m_normal[3];
				v4	m_radius[3];
			};
			struct Overlap
			{
				enum Type { Corner = 0, Edge = 1, Face = 2 };
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
			BoxCollider(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, Contact& contact);
			
		private:
			void	GetMinOverlap(const v4& axis, const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& min_overlap);
			void	GetPointOfContact(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& min_overlap);
			void	CornerToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap);
			void	EdgeToEdge(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap);
			void	EdgeToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap);
			void	FaceToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap);
			void	Clip(v4& start, v4& end, const v4& pt, const v4& normal);

		private:
		};

		//*****************************************
		// Implementation
		inline BoxCollider::Overlap::Overlap()
		{
			m_penetration	= FLT_MAX;
			m_pA			= &m_A;
			m_pB			= &m_B;
			m_reversed		= false;
		}

		inline void	BoxCollider::Overlap::Reverse()
		{
			m_reversed	= !m_reversed;
			Point* tmp	=  m_pA;
			m_pA		=  m_pB;
			m_pB		=  tmp;
			m_axis		= -m_axis;
		}

		inline BoxCollider::Overlap& BoxCollider::Overlap::operator =(const BoxCollider::Overlap& other)
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

#endif//PR_PHYSICS_BOX_COLLIDER_H
