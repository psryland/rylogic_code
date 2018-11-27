//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/inertia.h"

namespace pr::physics
{
	struct RigidBody
	{
	protected:

		// World space position/orientation of the rigid bodies' centre of mass
		m4x4 m_o2w;

		// World space spatial velocity
		v8f m_ws_momentum;

		// The external forces and torques applied to this body (in world space).
		// This accumulator is reset to zero after each physics step, so forces that
		// should be constant need to be applied each frame.
		v8f m_ws_force;

		// Mass properties.
		// Currently this is just simple 3x3 inertia. Articulated bodies will need 6x6 inertia.
		Inertia m_os_inertia;

		// Vector from the 
		v4 m_model_origin_to_com;

		// Collision shape
		Shape const* m_shape;

	public:

		// Construct the rigid body with a collision shape
		// Inertia is not automatically derived from the collision shape,
		// that is left to the caller.
		template <typename TShape, typename = enable_if_shape<TShape>>
		RigidBody(TShape const* shape)
			:m_o2w(m4x4Identity)
			,m_ws_velocity()
			,m_ws_force()
			,m_os_inertia()
			,m_model_origin_to_com()
			,m_shape(&shape->m_base)
		{}

		// Get/Set the body object to world transform
		m4_cref<> O2W() const
		{
			return m_o2w;
		}
		m4x4 W2O() const
		{
			return InvertFast(O2W());
		}
		void O2W(m4_cref<> o2w, bool update_inertia = true)
		{
			m_o2w = o2w;

			//// Update the world space inertia matrix when the orientation changes
			//if (update_inertia)
			//{
			//	//' Iw = (o2w * Io * w2o)^-1  =  w2o^-1 * Io^-1 * o2w^-1  =  o2w * Io^-1 * w2o
			//	//' where Iw = world space inertia, Io = object space inertia
			//	m_ws_inertia_inv = m_o2w.rot * m_os_inertia_inv * Transpose(m_o2w.rot); 
			//}
		}

		// Object space inertia
		Inertia const& InertiaOS() const
		{
			return m_os_inertia;
		}
		InertiaInv InertiaInvOS() const
		{
			return Invert(InertiaOS());
		}

		// World space inertia
		InertiaInv InertiaInvWS() const
		{
			auto inv_inertia_os = InertiaInvOS().To3x3();
			return W2O() * ;
		}

		// Get/Set the object space velocity
		v8m VelocityWS() const
		{
			auto os_momentum = W2O() * MomentumWS();
			auto os_velocity = InertiaInvOS() * os_momentum;
			return O2W() * os_velocity;
		}
		void VelocityWS(v8m const& velocity)
		{
			auto os_velocity = W2O() * velocity;
			auto os_momentum = InertiaOS() * os_velocity;
			auto ws_momentum = W2O() * os_momentum;
			MomentumWS(ws_momentum);
		}

		// Get/Set the momentum of the rigid body (in world space)
		v8f MomentumWS() const
		{
			return m_ws_momentum;
		}
		void MomentumWS(v8f const& ws_momentum)
		{
			m_ws_momentum = ws_momentum;
		}

		// Get/Set the current forces applied to this body
		v8f const& ForceWS() const
		{
			return m_ws_force;
		}
		void ForceWS(v8f const& force)
		{
			m_ws_force = force;
		}

		// Add an object space force acting on the rigid body
		void ApplyForceOS(v8f const& force)
		{
			// Todo: Store object space forces separately, so they can
			// be rotated with the body during integration.
			ApplyForceWS(m_o2w * force);
		}
		void ApplyForceOS(v4 const& force, v4 const& torque, v4 const& at)
		{
			assert("'at' should be an offset (in object space) from the object centre of mass" && at.w == 0.0f);
			ApplyForceOS(v8f(torque + Cross3(at, force), force));
		}

		// Add a world space force acting on the rigid body
		void ApplyForceWS(v8f const& force)
		{
			m_ws_force += force;
		}
		void ApplyForceWS(v4 const& force, v4 const& torque, v4 const& at)
		{
			ApplyForceWS(v8f(torque + Cross3(at, force), force));
		}

		// Reset the state of the body
		void UpdateDerivedState()
		{
		//	m_ws_bbox = rb.ObjectToWorld() * rb.BBoxOS();
		//	m_ws_inv_inertia_tensor = InvInertiaTensorWS(rb.Orientation(), rb.m_os_inv_inertia_tensor); //Iw = (o2w * Io * w2o)^-1  =  w2o^-1 * Io^-1 * o2w^-1  =  o2w * Io^-1 * w2o
		}
	};
}
