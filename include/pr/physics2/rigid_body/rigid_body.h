//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/physics2/forward.h"
#include "pr/physics2/shape/inertia.h"
#include "pr/physics2/shape/shape_mass.h"
#include "pr/physics2/utility/misc.h"

namespace pr::physics
{
	struct RigidBody
	{
	protected:

		// Notes:
		//  - Object space is the space that the collision model is given in. It has the model origin
		//    at (0,0,0), the coordinate frame equal to the root object in the collision shape, and
		//    the centre of mass at 'm_os_com'.
		//  - Dynamics state is stored in world space but relative to the model origin. If world space
		//    spatial vectors were relative to the world origin then floating point accuracy would be
		//    an issue.
		//  - Careful with spatial vectors, transforming a spatial vector does not move it, it describes
		//    it from a new position/orientation. Changing 'o2w' does move the spatial vectors though.

		// World space position/orientation of the rigid body
		// This is the position of the model origin in world space (not the CoM)
		m4x4 m_o2w;

		// Offset from the model origin to the CoM (in object space). 
		v4 m_os_com;

		// World space spatial momentum, measured at the model origin (not CoM)
		v8force m_ws_momentum;

		// The external forces and torques applied to this body (in world space), measured at the model origin (not CoM).
		// This value is an accumulator and is reset to zero after each physics step so forces that should
		// be constant need to be applied each frame.
		v8force m_ws_force;

		// Inertia, measured at the model origin (not CoM). Currently this is just simple 3x3 inertia. Articulated bodies will need 6x6 inertia.
		InertiaInv m_os_inertia_inv;

		// Collision shape
		Shape const* m_shape;

	public:

		// Construct the rigid body with a collision shape
		// Inertia is not automatically derived from the collision shape, that is left to the caller.
		template <ShapeType TShape>
		explicit RigidBody(TShape const* shape, m4_cref o2w = m4x4::Identity(), Inertia const& inertia = {})
			:RigidBody(shape_cast(shape), o2w, inertia)
		{}
		explicit RigidBody(Shape const* shape = nullptr, m4_cref o2w = m4x4::Identity(), Inertia const& inertia = {})
			:m_o2w(o2w)
			,m_os_com()
			,m_ws_momentum()
			,m_ws_force()
			,m_os_inertia_inv()
			,m_shape(collision::shape_cast(shape))
		{
			SetMassProperties(inertia);
		}

		// Raised after the collision shape changes.
		EventHandler<RigidBody&, ChangeEventArgs<collision::Shape const*>> ShapeChange;

		// Get/Set the collision shape for the rigid body
		template <ShapeType TShape> TShape const& Shape() const
		{
			return shape_cast<TShape>(Shape());
		}
		collision::Shape const& Shape() const
		{
			return *m_shape;
		}
		bool HasShape() const
		{
			return m_shape != nullptr;
		}
		
		// Set the shape only, leave the mass properties unchanged
		void Shape(collision::Shape const* shape)
		{
			ShapeChange(*this, ChangeEventArgs<collision::Shape const*>(m_shape, true));
			m_shape = shape;
			ShapeChange(*this, ChangeEventArgs<collision::Shape const*>(m_shape, false));
		}

		// Set the shape and derive mass properties from the shape.
		void Shape(collision::Shape const* shape, float mass, bool mass_is_actually_density = false)
		{
			// Set the shape
			Shape(shape);

			// Derive the mass properties from the shape
			auto mp = CalcMassProperties(*m_shape, mass_is_actually_density ? mass : 1.0f);
			if (!mass_is_actually_density) mp.m_mass = mass;
			SetMassProperties(Inertia{mp}, mp.m_centre_of_mass);
		}

		// Set the shape and mass properties explicitly
		void Shape(collision::Shape const* shape, Inertia inertia, v4_cref com = v4{})
		{
			// Set the shape
			Shape(shape);

			// Set the mass properties explicitly
			SetMassProperties(inertia, com);
		}

		// Get/Set the body object to world transform
		m4_cref O2W() const
		{
			return m_o2w;
		}
		m4x4 W2O() const
		{
			return InvertAffine(O2W());
		}
		void O2W(m4_cref o2w)
		{
			assert(IsOrthonormal(o2w));
			m_o2w = o2w;
		}

		// Extrapolate the position based on the current momentum and forces
		m4x4 O2W(float dt) const
		{
			return Abs(dt) > maths::tinyf
				? ExtrapolateO2W(O2W(), MomentumWS(), ForceWS(), InertiaInvWS(), dt)
				: O2W();
		}

		// Return the world space bounding box for this object
		BBox BBoxWS() const
		{
			return O2W() * Shape().m_bbox;
		}

		// The mass of the rigid body
		float Mass() const
		{
			return InertiaInvOS().Mass();
		}
		void Mass(float mass)
		{
			m_os_inertia_inv.Mass(mass);
		}
		float InvMass() const
		{
			return InertiaInvOS().InvMass();
		}
		void InvMass(float invmass)
		{
			return m_os_inertia_inv.InvMass(invmass);
		}

		// Offset to the centre of mass (w = 0) (Object relative)
		v4_cref CentreOfMassOS() const
		{
			return m_os_com;
		}
		v4 CentreOfMassWS() const
		{
			return O2W() * CentreOfMassOS();
		}

		// InertiaInv (use 'SetMassProperties' to change)
		InertiaInv InertiaInvOS() const
		{
			return m_os_inertia_inv;
		}
		InertiaInv InertiaInvWS() const
		{
			return Rotate(InertiaInvOS(), O2W().rot);
		}
		Inertia InertiaOS() const
		{
			return Invert(InertiaInvOS());
		}
		Inertia InertiaWS() const
		{
			return Invert(InertiaInvWS());
		}

		// Return the inertia rotated from object space to 'A' space
		// 'com' is the position of this object's CoM in 'A' space
		Inertia InertiaOS(m3_cref o2a, v4_cref com = v4{}) const
		{
			auto inertia = InertiaOS();
			inertia = Rotate(inertia, o2a);
			inertia.CoM(com);
			return inertia;
		}
		InertiaInv InertiaInvOS(m3_cref o2a, v4_cref com = v4{}) const
		{
			auto inertia_inv = InertiaInvOS();
			inertia_inv = Rotate(inertia_inv, o2a);
			inertia_inv.CoM(com);
			return inertia_inv;
		}
		Inertia InertiaOS(m4_cref o2a) const
		{
			return InertiaOS(o2a.rot, o2a.pos);
		}
		InertiaInv InertiaInvOS(m4_cref o2a) const
		{
			return InertiaInvOS(o2a.rot, o2a.pos);
		}

		// Get/Set the velocity
		v8motion VelocityWS() const
		{
			auto ws_velocity = InertiaInvWS() * MomentumWS();
			return ws_velocity;
		}
		v8motion VelocityOS() const
		{
			return W2O().rot * VelocityWS();
		}
		void VelocityWS(v8motion const& ws_velocity)
		{
			auto ws_momentum = InertiaWS() * ws_velocity;
			MomentumWS(ws_momentum);
		}
		void VelocityOS(v8motion const& os_velocity)
		{
			auto ws_velocity = O2W().rot * os_velocity;
			VelocityWS(ws_velocity);
		}
		void VelocityWS(v4_cref ws_ang, v4_cref ws_lin, v4_cref ws_at = v4{})
		{
			// 'ws_ang' and 'ws_lin' are model origin relative
			auto spatial_velocity = v8motion{ws_ang, ws_lin};
			spatial_velocity = Shift(spatial_velocity, CentreOfMassWS() - ws_at);
			VelocityWS(spatial_velocity);
		}
		void VelocityOS(v4_cref os_ang, v4_cref os_lin, v4_cref os_at = v4{})
		{
			auto ws_ang = O2W() * os_ang;
			auto ws_lin = O2W() * os_lin;
			auto ws_at  = O2W() * os_at;
			VelocityWS(ws_ang, ws_lin);
		}

		// Get/Set the momentum of the rigid body
		v8force MomentumWS() const
		{
			return m_ws_momentum;
		}
		v8force MomentumOS() const
		{
			return W2O().rot * MomentumWS();
		}
		void MomentumWS(v8force const& ws_momentum)
		{
			m_ws_momentum = ws_momentum;
		}
		void MomentumOS(v8force const& os_momentum)
		{
			auto ws_momentum = O2W().rot * os_momentum;
			MomentumWS(ws_momentum);
		}

		// Reset the state of the body
		void ZeroForces()
		{
			m_ws_force = v8force{};
		}
		void ZeroMomentum()
		{
			m_ws_momentum = v8force{};
		}

		// Get/Set the current forces applied to this body.
		v8force ForceWS() const
		{
			return m_ws_force;
		}
		v8force ForceOS() const
		{
			return W2O().rot * ForceWS();
		}

		// Add a force acting on the rigid body at position 'at' (world space, object origin relative, not CoM relative)
		void ApplyForceWS(v4_cref ws_force, v4_cref ws_torque, v4_cref ws_at = v4Zero)
		{
			assert("'at' should be an offset (in world space) from the object origin" && ws_at.w == 0);
			auto spatial_force = v8force{ws_torque, ws_force};
			spatial_force = Shift(spatial_force, CentreOfMassWS() - ws_at);
			ApplyForceWS(spatial_force);
		}
		void ApplyForceWS(v8force const& ws_force)
		{
			m_ws_force += ws_force;
		}

		// Add a force acting on the rigid body at position 'at' (object space, not CoM relative)
		void ApplyForceOS(v4_cref os_force, v4_cref os_torque, v4_cref os_at = v4Zero)
		{
			assert("'at' should be an offset (in object space) from the object origin" && os_at.w == 0);
			auto o2w = O2W();
			auto ws_force  = o2w * os_force;
			auto ws_torque = o2w * os_torque;
			auto ws_at     = o2w * os_at;
			ApplyForceWS(ws_force, ws_torque, ws_at);
		}
		void ApplyForceOS(v8force const& os_force)
		{
			auto ws_force = O2W().rot * os_force;
			ApplyForceWS(ws_force);
		}

		// Set the mass properties of the body.
		// 'os_inertia' is the inertia for the body, measured at the model origin (not CoM) (in object space)
		// 'os_model_to_com' is the vector from the model origin to the body's centre of mass (in object space)
		void SetMassProperties(Inertia const& os_inertia, v4_cref os_model_to_com = v4{})
		{
			// Notes:
			//  - os_inertia.CoM() vs. os_model_to_com:
			//    See comments for 'Inertia', but you probably want 'os_inertia.CoM()' to be zero. It is really only
			//    used with spatial vectors. 'os_model_to_com' is the more common case where the inertia has been
			//    measured at a point that isn't the CoM (typically the model origin). This is recorded so that
			//    callers can apply forces to the CoM.
			assert("'os_model_to_com' should be an offset (in world space) from the object origin" && os_model_to_com.w == 0);
			
			// Object space inertia inverse
			m_os_inertia_inv = Invert(os_inertia);

			// Position of the centre of mass (in object space)
			m_os_com = os_model_to_com;
		}

		// Return the kinetic energy of the body
		float KineticEnergy() const
		{
			// KE = 0.5 v.h = 0.5 v.Iv
			auto ke = 0.5f * Dot(VelocityWS(), MomentumWS());
			return ke;
		}
	};

	// Return the world space bounding box for 'rb'
	inline BBox BBoxWS(RigidBody const& rb)
	{
		return rb.BBoxWS();
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/physics2/shape/inertia.h"
#include "pr/physics2/integrator/integrator.h"

namespace pr::physics
{
	PRUnitTest(RigidBodyTests)
	{
		auto mass = 5.0f;
		{// Simple case
			auto rb = RigidBody{};
			rb.SetMassProperties(Inertia::Sphere(1, mass), v4{});

			// Apply a force and torque. The force at (0,1,0) cancels out the torque
			rb.ApplyForceWS(v4{1,0,0,0}, v4{0,0,1,0}, v4{0,1,0,0});
			
			// Check force applied
			auto ws_force = rb.ForceWS();
			auto os_force = rb.ForceOS();
			PR_EXPECT(FEql(ws_force, v8force{0,0,0, 1,0,0}));
			PR_EXPECT(FEql(os_force, v8force{0,0,0, 1,0,0}));

			// Integrate for 1 sec
			Evolve(rb, 1.0f);

			// Check position
			// Distance travelled: S = So + Vot + 0.5At²; So = 0, Vo = 0, t = 1, A = F/m, F = 1  =>  S = 0.5/mass
			auto o2w = rb.O2W();
			PR_EXPECT(FEql(o2w.rot, m3x4::Identity()));
			PR_EXPECT(FEql(o2w.pos, v4{0.5f / mass,0,0,1}));

			// Check the momentum
			auto ws_mom = rb.MomentumWS();
			auto os_mom = rb.MomentumOS();
			PR_EXPECT(FEql(ws_mom, v8force{0,0,0, 1,0,0}));
			PR_EXPECT(FEql(os_mom, v8force{0,0,0, 1,0,0}));

			// Check the velocity
			// Velocity: V = Vo + At; Vo = 0, t = 1, A = F/m, F = 1  =>  V = 1/mass
			auto ws_vel = rb.VelocityWS();
			auto os_vel = rb.VelocityOS();
			PR_EXPECT(FEql(ws_vel, v8motion{0,0,0, 1/mass,0,0}));
			PR_EXPECT(FEql(os_vel, v8motion{0,0,0, 1/mass,0,0}));
		}
		{// Simple case with rotation
			auto rb = RigidBody{};
			rb.SetMassProperties(Inertia::Sphere(1, mass), v4{});

			// Apply a force and torque. The force at (0,-1,0) doubles the torque
			rb.ApplyForceWS(v4{1,0,0,0}, v4{0,0,1,0}, v4{0,-1,0,0});

			// Check force applied
			auto ws_force = rb.ForceWS();
			auto os_force = rb.ForceOS();
			PR_EXPECT(FEql(ws_force, v8force{0,0,2, 1,0,0}));
			PR_EXPECT(FEql(os_force, v8force{0,0,2, 1,0,0}));

			// Integrate for 1 sec
			Evolve(rb, 1.0f);

			// Check position
			// Distance: S = So + Vot + 0.5At²; So = 0, Vo = 0, t = 1, A = F/m, F = 1  =>  S = 0.5/mass
			// Rotation: O = Oo + Wot + 0.5At²; Oo = 0, Wo = 0, t = 1, A = I^T, T = 2  =>  O = 0.5*I^(0,0,2)
			auto o2w = rb.O2W();
			auto pos = v4{0.5f / mass,0,0,1};
			auto rot = m3x4::Rotation(0.5f * (rb.InertiaInvWS() * v4{0,0,2,0}));
			auto invrot = InvertAffine(rot);
			PR_EXPECT(FEql(o2w.pos, pos));
			PR_EXPECT(FEql(o2w.rot, rot));

			// Check the momentum
			auto ws_mom = rb.MomentumWS();
			auto os_mom = rb.MomentumOS();
			auto WS_MOM = v8force{0,0,2, 1,0,0};
			auto OS_MOM = invrot * WS_MOM;
			PR_EXPECT(FEql(ws_mom, WS_MOM));
			PR_EXPECT(FEql(os_mom, OS_MOM));

			// Check the velocity
			// Velocity: V = Vo + At; Vo = 0, t = 1, A = F/m, F = 1  =>  V = 1/mass
			// Rotation: W = Wo + At; Wo = 0, t = 1, A = I^T, T = 2  =>  W = I^(0,0,2)
			auto ws_vel = rb.VelocityWS();
			auto os_vel = rb.VelocityOS();
			auto WS_VEL = v8motion{(rb.InertiaInvWS() * v4{0,0,2,0}), v4{1/mass,0,0,0}};
			auto OS_VEL = invrot * WS_VEL;
			PR_EXPECT(FEql(ws_vel, WS_VEL));
			PR_EXPECT(FEql(os_vel, OS_VEL));
		}
		{// Off-centre CoM
			auto rb = RigidBody{};
			auto model_to_com = v4{0,1,0,0};
			rb.SetMassProperties(Inertia::Sphere(1, mass, model_to_com), model_to_com);
			PR_EXPECT(FEql(rb.InertiaOS().To3x3(1), m3x4::Scale(1.4f,0.4f,1.4f)));

			// Apply a force and torque at the CoM.
			rb.ApplyForceWS(v4{1,0,0,0}, v4{}, rb.CentreOfMassWS());

			// Check force applied
			// Spatial force measured at the model origin
			auto ws_force = rb.ForceWS();
			auto os_force = rb.ForceOS();
			PR_EXPECT(FEql(ws_force, v8force{0,0,0, 1,0,0}));
			PR_EXPECT(FEql(os_force, v8force{0,0,0, 1,0,0}));

			// Integrate for 1 sec
			Evolve(rb, 1.0f);

			// Check position
			auto o2w = rb.O2W();
			PR_EXPECT(FEql(o2w.rot, m3x4Identity));
			PR_EXPECT(FEql(o2w.pos, v4{0.5f / mass,0,0,1}));

			// Check the momentum
			auto ws_mom = rb.MomentumWS();
			auto os_mom = rb.MomentumOS();
			PR_EXPECT(FEql(ws_mom, v8force{0,0,0, 1,0,0}));
			PR_EXPECT(FEql(os_mom, v8force{0,0,0, 1,0,0}));

			// Check the velocity
			auto ws_vel = rb.VelocityWS();
			auto os_vel = rb.VelocityOS();
			PR_EXPECT(FEql(ws_vel, v8motion{0,0,0, 1/mass,0,0}));
			PR_EXPECT(FEql(os_vel, v8motion{0,0,0, 1/mass,0,0}));
		}
		{// Off-centre CoM with rotation
			auto rb = RigidBody{};
			auto model_to_com = v4{0,1,0,0};
			rb.SetMassProperties(Inertia::Sphere(1, mass, model_to_com), model_to_com);
			
			// Apply a force and torque at the model origin.
			rb.ApplyForceWS(v4{1,0,0,0}, v4{0,0,1,0});

			// Check force applied
			// Spatial force measured at the model origin
			auto ws_force = rb.ForceWS();
			auto os_force = rb.ForceOS();
			PR_EXPECT(FEql(ws_force, v8force{0,0,2, 1,0,0}));
			PR_EXPECT(FEql(os_force, v8force{0,0,2, 1,0,0}));

			// Integrate for 1 sec
			Evolve(rb, 1.0f);

			// Check position
			auto o2w = rb.O2W();
			auto pos = v4{0.5f / mass,0,0,1};
			auto rot = m3x4::Rotation(0.5f * (rb.InertiaInvWS() * v4{0,0,2,0}));
			auto invrot = InvertAffine(rot);
			PR_EXPECT(FEql(o2w.pos, pos));
			PR_EXPECT(FEql(o2w.rot, rot));

			// Check the momentum
			auto ws_mom = rb.MomentumWS();
			auto os_mom = rb.MomentumOS();
			auto WS_MOM = v8force{0,0,2, 1,0,0};
			auto OS_MOM = invrot * WS_MOM;
			PR_EXPECT(FEql(ws_mom, WS_MOM));
			PR_EXPECT(FEql(os_mom, OS_MOM));

			// Check the velocity
			auto ws_vel = rb.VelocityWS();
			auto os_vel = rb.VelocityOS();
			auto WS_VEL = v8motion{(rb.InertiaInvWS() * v4{0,0,2,0}), v4{1/mass,0,0,0}};
			auto OS_VEL = invrot * WS_VEL;
			PR_EXPECT(FEql(ws_vel, WS_VEL));
			PR_EXPECT(FEql(os_vel, OS_VEL));
		}
		{// Off-centre CoM with complex rotation
			auto rb = RigidBody{};
			auto model_to_com = v4{0,1,0,0};
			rb.SetMassProperties(Inertia::Sphere(1, mass, model_to_com), model_to_com);
			
			// Apply a force and torque at the model origin.
			rb.ApplyForceWS(v4{1,0,0,0}, v4{0,-1,0,0}, v4{0,1,1,0}); // +X push at (0,1,1) + -Y twist to cancel rotation => translating along X
			rb.ApplyForceWS(v4{0,-1,0,0}, v4{0,-1,0,0}, v4{1,1,0,0}); // -Y push at (1,1,0) + -Y twist => translating down Y, screwing around -Y and around -Z

			// Check force applied
			// Spatial force measured at the model origin
			auto ws_force = rb.ForceWS();
			auto os_force = rb.ForceOS();
			PR_EXPECT(FEql(ws_force, v8force{0,-1,-1, 1,-1,0}));
			PR_EXPECT(FEql(os_force, v8force{0,-1,-1, 1,-1,0}));

			// Expected position - the inertia changes with orientation
			// so predicting the orientation after the step is hard...
			auto ws_inertia_inv = rb.InertiaInvWS();
			auto ws_velocity = ws_inertia_inv * ws_force;
			auto dpos = m3x4::Rotation(0.5f * ws_velocity.ang); // mid-step rotation
			ws_inertia_inv = Rotate(ws_inertia_inv, dpos);
			auto pos = v4{0.5f/mass,-0.5f/mass,0,1};
			auto rot = m3x4::Rotation(0.5f * (ws_inertia_inv * v4{0,-1,-1,0}));
			
			// Integrate for 1 sec
			Evolve(rb, 1.0f);

			// Check position
			auto o2w = rb.O2W();
			PR_EXPECT(FEql(o2w.pos, pos));
			PR_EXPECT(FEqlRelative(o2w.rot, rot, 0.01f));
		}
		{// Extrapolation
			auto rb = RigidBody{};
			rb.SetMassProperties(Inertia::Sphere(1, mass), v4{});
			
			auto vel = v8motion{0,0,1, 0,1,0};
			rb.VelocityWS(vel);

			auto o2w0 = rb.O2W();
			auto O2W0 = m4x4Identity;
			PR_EXPECT(FEql(o2w0, O2W0));

			auto o2w1 = rb.O2W(1.0f);
			auto O2W1 = m4x4::Transform(1*vel.ang, (1*vel.lin).w1());
			PR_EXPECT(FEql(o2w1, O2W1));

			auto o2w2 = rb.O2W(2.0f);
			auto O2W2 = m4x4::Transform(2*vel.ang, (2*vel.lin).w1());
			PR_EXPECT(FEql(o2w2, O2W2));

			auto o2w3 = rb.O2W(-2.0f);
			auto O2W3 = m4x4::Transform(-2*vel.ang, (-2*vel.lin).w1());
			PR_EXPECT(FEql(o2w3, O2W3));
		}
		{// Kinetic Energy
			std::default_random_engine rng;

			// KE should be the same no matter what frame it's measured in
			auto rb = RigidBody{};
			rb.SetMassProperties(Inertia::Sphere(1, mass), v4{});
			rb.MomentumWS(v8force{0,0,1, 0,1,0});
			rb.O2W(m4x4::Random(rng, v4::Origin(), 5.0f));

			auto ws_ke = 0.5f * Dot(rb.VelocityWS(), rb.MomentumWS());
			auto os_ke = 0.5f * Dot(rb.VelocityOS(), rb.MomentumOS());
			PR_EXPECT(FEql(ws_ke, os_ke));
		}
	}
}
#endif
