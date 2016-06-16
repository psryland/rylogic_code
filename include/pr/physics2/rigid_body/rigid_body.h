//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/inertia.h"

namespace pr
{
	namespace physics
	{
		struct RigidBody
		{

		protected:

			// World space position/orientation of the rigid bodies' centre of mass
			m4x4 m_o2w;

			// Spatial momentum
			v8m m_os_momentum;

			// The external forces and torques applied to this body (in object space)
			v8f m_os_force;

			// Mass properties
			m3x4  m_os_inertia;     // The object space inertia tensor (normalised to mass = 1)
			m3x4  m_os_inertia_inv; // The object space inverse inertia matrix
			m3x4  m_ws_inertia_inv; // The world space inverse inertia matrix (updated whenever the o2w changes)
			kg_t  m_mass;           // The mass of the object
			float m_mass_inv;       // The inverse mass

			// Collision shape
			Shape const* m_shape;

		public:

			// Construct the rigid body with a collision shape
			template <typename TShape, typename = enable_if_shape<TShape>> RigidBody(TShape const* shape)
				:m_o2w(m4x4Identity)
				,m_os_momentum(v4Zero, v4Zero)
				,m_os_force(v4Zero, v4Zero)
				,m_os_inertia(m3x4Identity)
				,m_os_inertia_inv(m3x4Identity)
				,m_mass(1.0_kg)
				,m_mass_inv(1.0f / m_mass)
				,m_shape(&shape->m_base)
			{}

			// Get/Set the body object to world transform
			m4x4 O2W() const
			{
				return m_o2w;
			}
			void O2W(m4x4 const& o2w, bool update_inertia = true)
			{
				m_o2w = o2w;

				// Update the world space inertia matrix when the orientation changes
				if (update_inertia)
				{
					//' Iw = (o2w * Io * w2o)^-1  =  w2o^-1 * Io^-1 * o2w^-1  =  o2w * Io^-1 * w2o
					//' where Iw = world space inertia, Io = object space inertia
					m_ws_inertia_inv = m_o2w.rot * m_os_inertia_inv * Transpose(m_o2w.rot); 
				}
			}

			// Get/Set the momentum of the rigid body (in object space)
			v8m MomentumOS() const
			{
				return m_os_momentum;
			}
			void MomentumOS(v8m const& os_momentum)
			{
				m_os_momentum = os_momentum;
			}

			// Get/Set the momentum of the rigid body (in world space)
			v8m MomentumWS() const
			{
				return m_o2w * MomentumOS();
			}
			void MomentumWS(v8m const& ws_momentum)
			{
				MomentumOS(InvertFast(m_o2w) * ws_momentum);
			}

			// Get/Set mass
			float Mass() const
			{
				return m_mass;
			}
			void Mass(float mass)
			{
				m_mass = mass;
			//	m_mass_inv = mass > maths::tiny ? 1.0f / mass : maths::float_inf;
			}

			// Get/Set the current forces applied to this body
			v8f const& ForceOS() const
			{
				return m_os_force;
			}
			void ForceOS(v8f const& force)
			{
				m_os_force = force;
			}

			// Add an object space force acting on the rigid body
			void ApplyForceOS(v8f const& force)
			{
				m_os_force += force;
			}
			void ApplyForceOS(v4 const& force, v4 const& torque, v4 const& at)
			{
				assert("'at' should be an offset (in object space) from the object centre of mass" && at.w == 0.0f);
				ApplyForceOS(v8f(torque + Cross3(at, force), force));
			}

			// Add a world space force acting on the rigid body
			void ApplyForceWS(v8f const& force)
			{
				auto w2o = InvertFast(m_o2w);
				ApplyForceOS(w2o * force);
			}
			void ApplyForceWS(v4 const& force, v4 const& torque, v4 const& at)
			{
				auto w2o = InvertFast(m_o2w);
				ApplyForceOS(w2o * force, w2o * torque, w2o * at);
			}

			// Get the object space inertia
			m3x4 InertiaOS() const
			{
				return m_os_inertia;
			}

			// Get the object space inverse inertia
			m3x4 InertiaInvOS() const
			{
				return m_os_inertia_inv;
			}

			// Get the world space inverse inertia
			m3x4 InertiaInvWS() const
			{
				return m_ws_inertia_inv;
			}

			// Returns the object space spatial inertia at the centre of mass
			m6x8_m2f SpatialInertiaInvWS() const
			{
				throw std::exception("not implemented");
			}
	//		InertiaOS() const
	//		{
	//			return m6x8_m2f(
	//				m_os_inertia*m_mass , m3x4Zero,
	//				m3x4Zero            , m3x4Identity*m_mass);
	//		}

	//		// Returns the world space spatial inertia at the centre of mass
	//		m6x8_m2f InertiaWS() const
	//		{
	//			//return m6x8_m2f(
	//			//	m_os_inertia*m_mass , m3x4Zero,
	//			//	m3x4Zero            , m3x4Identity*m_mass);
	//		}

	//		// Returns the inverse of the object-space spatial inertia at the centre of mass, in object space.
	//		m6x8_m2f InertiaInvOS() const
	//		{
	///*			return m6x8_m2f(
	//				m_mass * m_os_inertia_inv, m3x4Zero,
	//				m3x4Zero, m_mass * m3x4Identity);
	//*/		}

	//		// Returns the object space spatial inertia for an arbitrary point 'os_pt'
	//		// that is relative to the body centre of mass, in object space.
	//		m6x8_m2f InertiaOS(v4 const& os_pt) const
	//		{
	//			auto pt_cross = CrossProductMatrix(os_pt);
	//			auto pt_cross_t = Transpose3x3(pt_cross);
	//			return m6x8_m2f(
	//				m_mass*m_os_inertia + m_mass*pt_cross*pt_cross_t , m_mass*pt_cross,
	//				m_mass*pt_cross_t                                , m_mass*m3x4Identity);
	//		}
		};
	}
}
