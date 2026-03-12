//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#include "pr/physics-2/integrator/engine.h"
#include "pr/physics-2/integrator/integrator.h"
#include "pr/physics-2/integrator/impulse.h"
#include "pr/physics-2/collision/ibroadphase.h"
#include "pr/physics-2/collision/contact.h"
#include "pr/physics-2/materials/imaterials.h"
#include "src/integrator/gpu_integrator.h"
#include "src/collision/collision_shape_cache.h"
#include "src/collision/gpu_sort_and_sweep.h"
#include "src/collision/gpu_collision_detector.h"
#include "src/collision/gpu_collision_types.h"
#include "src/utility/gpu.h"

namespace pr::physics
{
	struct BodyPair
	{
		m4x4 b2a;
		RigidBody const* objA;
		RigidBody const* objB;
	};

	struct EngineBufferCache
	{
		// Persists across frames
		CollisionShapeCache m_shape_cache;

		// Per-frame buffers
		std::vector<GpuCollisionPair> m_col_pairs;
		std::vector<BodyPair> m_body_pairs;
		std::vector<RbContact> m_collision_queue;
		std::vector<GpuContact> m_gpu_contacts;

		// Reset per-frame buffers
		void BeginFrame()
		{
			m_shape_cache.BeginFrame();
			m_col_pairs.resize(0);
			m_body_pairs.resize(0);
			m_collision_queue.resize(0);
			m_gpu_contacts.resize(0);
		}
	};

	Engine::Engine(IMaterials& mats, ID3D12Device4* existing_device)
		: m_gpu(new Gpu(existing_device))
		, m_gpu_integrator(new GpuIntegrator(*m_gpu))
		, m_gpu_sort_and_sweep(new GpuSortAndSweep(*m_gpu))
		, m_gpu_collision_detector(new GpuCollisionDetector(*m_gpu))
		, m_materials(mats)
		, m_cache(new EngineBufferCache)
		, m_rb_dynamics()
		, m_integrate_output()
		, m_integrate_aabbs()
		, m_cache_body_ptrs()
		, PostCollisionDetection()
	{}

	// Get/Set whether the GPU is used for integration and collision detection.
	bool Engine::UseGpu() const
	{
		return true;
	}
	void Engine::UseGpu(bool use_gpu)
	{
		// Dropping non-gpu support
		(void)use_gpu;
	}
	
	// Access the broadphase for registering bodies and enumerating overlapping pairs.
	IBroadphase& Engine::Broadphase() const
	{
		return *m_gpu_sort_and_sweep;
	}

	// Evolve the physics objects forward in time and resolve any collisions.
	void Engine::Step(float dt, std::span<RigidBody*> bodies)
	{
		// Before here, callers should have set forces on the rigid bodies (including gravity).
		// The simulation pipeline is: Evolve → Broad Phase → Narrow Phase → Resolve.

		// Pack the required data into a GPU-friendly format. Skip static bodies (infinite mass) since they don't need integration.
		m_rb_dynamics.resize(0);
		for (auto body : bodies)
		{
			if (body->Mass() >= 0.5f * InfiniteMass) continue;
			m_rb_dynamics.push_back(PackDynamics(*body));
		}

		// GPU split pipeline: integrate + readback AABBs → broadphase → GJK → readback bodies.
		// Bodies stay GPU-resident between the integrate and GJK steps, avoiding a round trip.
		Integrate(m_rb_dynamics, dt);

		// For now, read back and update the RBs immediately so that collision detection and resolution
		// apply to the updated dynamics. When the full pipeline is on the GPU, we'll do this after collision detection and resolution
		ReadbackBodies();

		// Unpack the integrated state back into the rigid bodies
		for (auto [body, i] : with_index(bodies))
		{
			if (body->Mass() >= 0.5f * InfiniteMass) continue;
			UnpackDynamics(*body, m_rb_dynamics[i]);
		}

		// Broadphase uses GPU-computed AABBs
		DetectAndResolveCollisions(dt);


		#if PR_DBG&&0
		CompareIntegrationPaths(dt, bodies);
		#endif
	}

	// CPU integration dispatch.
	void Engine::Integrate(std::span<RigidBodyDynamics> dynamics, float dt)
	{
		// Split pipeline: integrate + readback AABBs only (bodies stay GPU-resident)
		m_integrate_output.resize(dynamics.size());
		m_integrate_aabbs.resize(dynamics.size());
		m_gpu_integrator->IntegrateAndReadbackAABBs(m_gpu->m_job, dynamics, dt, m_integrate_output, m_integrate_aabbs);
	}

	// Broad phase overlap query → narrow phase collision detection → impulse resolution.
	void Engine::DetectAndResolveCollisions(float dt)
	{
		// This is the core collision pipeline, called after all bodies have been evolved.
		m_cache->BeginFrame();

		auto& shape_cache = m_cache->m_shape_cache;
		auto& col_pairs = m_cache->m_col_pairs;
		auto& body_pairs = m_cache->m_body_pairs;
		auto& collision_queue = m_cache->m_collision_queue;
		auto& gpu_contacts = m_cache->m_gpu_contacts;

		// Phase 1: Broadphase — collect overlapping pairs and look up cached shapes.
		// Use GPU-computed AABBs when available to avoid recomputing on CPU.
		int pair_idx = 0;
		m_gpu_sort_and_sweep->EnumOverlappingPairs(m_gpu->m_job, std::span<IntegrateAABB const>(m_integrate_aabbs), [&](RigidBody const& objA, RigidBody const& objB)
		{
			auto idx_a = shape_cache.GetOrAdd(objA.Shape());
			auto idx_b = shape_cache.GetOrAdd(objB.Shape());

			// Compute B-to-A transform (collision runs in A's object space)
			auto w2a = InvertOrthonormal(objA.O2W());
			auto b2a = w2a * objB.O2W();

			col_pairs.push_back(GpuCollisionPair{
				.shape_idx_a = idx_a,
				.shape_idx_b = idx_b,
				.pair_index = pair_idx,
				.pad0 = 0,
				.b2a = b2a,
			});				

			body_pairs.push_back(BodyPair{
				.b2a = b2a,
				.objA = &objA,
				.objB = &objB,
			});

			++pair_idx;
		});

		// Nothing close enough to be colliding? Skip the GPU GJK step.
		if (col_pairs.empty())
			return;

		// Periodically flush stale shapes (every 10 frames)
		if (shape_cache.m_frame % CollisionShapeCache::StaleFrameLimit == 0)
			shape_cache.Flush();

		// Phase 2: Dispatch GPU GJK collision detection.
		// Pass the dirty flag so the detector knows whether to re-upload shapes.
		m_gpu_collision_detector->DetectCollisions(m_gpu->m_job, col_pairs, shape_cache.m_shapes, shape_cache.m_verts, gpu_contacts, shape_cache.IsDirty());
		shape_cache.ClearDirty();

		// Phase 3: Convert GPU contacts to physics::Contact structs and resolve.
		for (auto const& gc : gpu_contacts)
		{
			auto const& bp = body_pairs[gc.pair_index];
			auto c = RbContact{*bp.objA, *bp.objB, gc};

			// Check if the collision point is separating (relative velocity positive along axis)
			auto rel_vel_at_point = c.m_velocity.LinAt(c.m_point);
			if (Dot(rel_vel_at_point, c.m_axis) > 0)
				continue;

			// Look up the combined material properties
			c.m_mat = m_materials(c.m_mat_idA, c.m_mat_idB);

			// Estimate sub-step collision time
			auto point_at_t0 = c.m_point - dt * c.m_velocity.LinAt(c.m_point);
			auto distance = Abs(Dot(c.m_point - point_at_t0, c.m_axis));
			auto sub_step = distance > c.m_depth ? -c.m_depth / distance : 0.0f;

			// Recompute contact data at the estimated collision time
			c.Update(sub_step * dt);

			collision_queue.push_back(c);
		}

		// Sort the collisions by estimated time of impact so earlier collisions are resolved first.
		std::sort(std::begin(collision_queue), std::end(collision_queue), [](auto& lhs, auto& rhs)
		{
			return lhs.m_time < rhs.m_time;
		});

		// Notify of detected collisions, and allow updates/additions
		PostCollisionDetection(*this, { collision_queue });

		// Apply restitution impulses to resolve each collision
		for (auto& c : collision_queue)
			ResolveCollision(c);
	}

	// Narrow phase collision detection.
	// Tests whether 'objA' and 'objB' are geometrically in contact using GJK/SAT.
	// All collision data (point, axis, depth) is computed in objA's object space to
	// minimise floating-point error. Returns true if the objects are in contact and
	// the contact is approaching (not separating).
	bool Engine::NarrowPhaseCollision(float dt, RbContact& c)
	{
		auto& objA = *c.m_objA;
		auto& objB = *c.m_objB;

		// Collision detection in objA space: objA is at identity, objB is at c.m_b2a.
		if (!collision::Collide(objA.Shape(), m4x4::Identity(), objB.Shape(), c.m_b2a, c))
			return false;

		// If the collision point is moving out of collision, ignore the collision.
		// This prevents re-resolving contacts that are already separating.
		auto rel_vel_at_point = c.m_velocity.LinAt(c.m_point);
		if (Dot(rel_vel_at_point, c.m_axis) > 0)
			return false;

		// Look up the combined material properties for this contact pair
		c.m_mat = m_materials(c.m_mat_idA, c.m_mat_idB);

		// Estimate the sub-step time when the collision actually occurred.
		// The bodies have already been evolved past the collision point, so we
		// need to estimate how far back in time the actual contact was. We project
		// the contact point backward along the relative velocity to find the
		// pre-overlap position, then compute sub_step as the fraction of dt to
		// rewind. This gives a more accurate contact point and lever arms for
		// the impulse calculation.
		auto point_at_t0 = c.m_point - dt * c.m_velocity.LinAt(c.m_point);
		auto distance = Abs(Dot(c.m_point - point_at_t0, c.m_axis));
		auto sub_step = distance > c.m_depth ? -c.m_depth / distance : 0.0f;

		// Recompute contact data (b2a, velocity, contact point) at the estimated collision time.
		c.Update(sub_step * dt);

		return true;
	}

	// Calculate and apply the restitution impulse to resolve a collision.
	// The impulse is computed in objA's space (where all contact data lives),
	// then transformed to each body's own object space before being applied.
	//
	// Important: When multiple collisions are resolved in a single time step,
	// earlier resolutions change body momenta. We must recompute the relative
	// velocity using CURRENT momenta before computing each impulse, otherwise
	// stale velocity data causes catastrophic energy injection.
	void Engine::ResolveCollision(RbContact& c)
	{
		auto& objA = const_cast<RigidBody&>(*c.m_objA);
		auto& objB = const_cast<RigidBody&>(*c.m_objB);

		// Recompute relative velocity using current momenta.
		// The geometric data (contact point, axis, depth) is still valid because
		// only momenta changed, not positions. But the velocity field is stale.
		auto va = Shift(objA.VelocityOS(), -objA.CentreOfMassOS());
		auto vb = Shift(objB.VelocityOS(), -objB.CentreOfMassOS());
		c.m_velocity = c.m_b2a * vb - va;

		// Re-check the separating condition with updated velocities.
		// A previous impulse in this step may have already resolved this contact.
		auto rel_vel_at_point = c.m_velocity.LinAt(c.m_point_at_t);
		auto sep_dot = Dot(rel_vel_at_point, c.m_axis);
		if (sep_dot > 0)
			return;

		// Measure pre-collision kinetic energy of the pair
		auto ke_before = objA.KineticEnergy() + objB.KineticEnergy();

		// Compute the equal-and-opposite impulse pair as spatial force wrenches.
		// Each wrench is expressed at the body's own model origin, in its own frame.
		auto impulse_pair = RestitutionImpulse(c);
		auto ja = impulse_pair.m_os_impulse_objA;
		auto jb = impulse_pair.m_os_impulse_objB;

		// Pre-compute the "impulse kinetic energy" term: the KE that the impulse
		// alone would create if applied to stationary bodies. This is the coefficient
		// of the α² term in KE(α) = KE₀ + αB + α²A.
		auto va_j = objA.InertiaInvOS() * ja;
		auto vb_j = objB.InertiaInvOS() * jb;
		auto A = 0.5f * (Dot(va_j, ja) + Dot(vb_j, jb));

		// Apply the impulses to each body's momentum (stored as spatial force at CoM).
		// The impulse changes both linear momentum (causing velocity change) and angular
		// momentum (causing spin change proportional to the lever arm from CoM to contact).
		objA.MomentumOS(objA.MomentumOS() + ja);
		objB.MomentumOS(objB.MomentumOS() + jb);

		// Energy conservation guard: if the impulse injected energy, scale it back.
		// For elastic collisions (e=1), KE should be conserved exactly. For inelastic (e<1),
		// KE must decrease. Numerical errors in contact geometry, sub-step approximation,
		// or the collision mass matrix can cause small energy gains that compound over
		// many collisions, leading to objects "exploding" apart.
		//
		// KE(α) = KE₀ + αB + α²A is quadratic in the impulse scale factor α.
		// At α=1: KE₁ = KE₀ + B + A, so B = (KE₁ - KE₀) - A = δ - A.
		// For KE(α) = KE₀: α²A + αB = 0 → α = -B/A = (A - δ)/A.
		auto ke_after = objA.KineticEnergy() + objB.KineticEnergy();
		auto delta = ke_after - ke_before;
		if (delta > 0 && A > math::tiny<float>)
		{
			auto alpha = Clamp((A - delta) / A, 0.0f, 1.0f);
			auto correction = 1.0f - alpha;
			objA.MomentumOS(objA.MomentumOS() - correction * ja);
			objB.MomentumOS(objB.MomentumOS() - correction * jb);

			#if PR_DBG
			{
				auto ke_clamped = objA.KineticEnergy() + objB.KineticEnergy();
				if (delta > 0.1f || alpha < 0.5f)
				{
					char buf[256];
					snprintf(buf, sizeof(buf),
						"[CLAMP] ke_before=%.4f delta=%.4f A=%.4f alpha=%.4f ke_clamped=%.4f\n",
						ke_before, delta, A, alpha, ke_clamped);
					auto f = fopen("dump\\clamp.log", "a");
					if (f) { fputs(buf, f); fclose(f); }
				}
			}
			#endif
		}
	}

	// Deferred readback of bodies from the GPU after the collision pipeline.
	void Engine::ReadbackBodies()
	{
		m_gpu_integrator->ReadbackBodies(m_gpu->m_job, m_rb_dynamics);
	}

	void Deleter<EngineBufferCache>::operator()(EngineBufferCache* cache) const
	{
		delete cache;
	}
	void Deleter<Gpu>::operator()(Gpu* p) const
	{
		delete p;
	}

}
