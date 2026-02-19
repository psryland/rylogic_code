//************************************
// Physics-2 Engine
//  Copyright (c) Rylogic Ltd 2016
//************************************
#pragma once

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/physics-2/integrator/integrator.h"
#include "pr/physics-2/shape/inertia.h"

namespace pr::physics
{
	PRUnitTestClass(IntegratorTests)
	{
		PRUnitTestMethod(IntegratorTests)
		{
			auto mass = 5.0f;
			auto force = v8force{1,1,1, 1,1,-1};

			// Set up a rigid body at rest
			auto rb = RigidBody{};
			rb.SetMassProperties(Inertia::Sphere(1, mass));

			// Initial KE should be zero
			auto ke0 = rb.KineticEnergy();
			PR_EXPECT(FEql(ke0, 0.f));

			// Get it moving by applying forces/torques
			auto dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
			rb.ApplyForceWS(force);
			Evolve(rb, 1.0f);

			// KE Gained
			ke0 += dke;
			auto ke1 = rb.KineticEnergy();
			PR_EXPECT(FEql(ke0, ke1));

			// More force
			dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
			rb.ApplyForceWS(force);
			Evolve(rb, 1.0f);

			// KE Gained again
			ke0 += dke;
			auto ke2 = rb.KineticEnergy();
			PR_EXPECT(FEql(ke0, ke2));

			// No force
			dke = 0;
			Evolve(rb, 1.0f);

			// KE unchanged
			ke0 += dke;
			auto ke3 = rb.KineticEnergy();
			PR_EXPECT(FEql(ke0, ke3));

			// Apply a force to stop the motion
			force = -rb.MomentumWS();
			dke = KineticEnergyChange(force, rb.MomentumWS(), rb.InertiaInvWS(), 1.0f);
			rb.ApplyForceWS(force);
			Evolve(rb, 1.0f);

			// KE lost
			ke0 += dke;
			auto ke4 = rb.KineticEnergy();
			PR_EXPECT(FEql(ke0, ke4));

			// KE back to zero
			PR_EXPECT(FEql(ke0, 0.f));
		}
	};
}
#endif
