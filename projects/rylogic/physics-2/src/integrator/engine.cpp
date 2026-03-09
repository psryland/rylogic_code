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
#include "pr/physics-2/utility/ldraw.h"
#include "src/integrator/gpu_integrator.h"
#include "src/collision/gpu_collision_detector.h"
#include "src/collision/gpu_collision_types.h"

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
		std::vector<GpuShape> m_col_shapes;
		std::vector<GpuCollisionPair> m_col_pairs;
		std::vector<v4> m_col_shape_verts;
		std::vector<BodyPair> m_body_pairs;
		std::unordered_map<Shape const*, int> m_shape_map;

		void Reset()
		{
			m_col_shapes.resize(0);
			m_col_pairs.resize(0);
			m_col_shape_verts.resize(0);
			m_body_pairs.resize(0);
			m_shape_map.clear();
		}
	};

	void Deleter<EngineBufferCache>::operator()(EngineBufferCache* cache) const
	{
		delete cache;
	}

	Engine::Engine(IBroadphase& bp, IMaterials& mats, ID3D12Device4* existing_device)
		: m_broadphase(bp)
		, m_materials(mats)
		, m_gpu(new Gpu(existing_device))
		, m_gpu_integrator(new GpuIntegrator(*m_gpu))
		, m_gpu_collision_detector(new GpuCollisionDetector(*m_gpu))
		, m_cache(new EngineBufferCache)
		, m_rb_dynamics()
		, m_use_gpu(true)
		, PostCollisionDetection()
	{}

	// Get/Set whether the GPU is used for integration and collision detection.
	bool Engine::UseGpu() const
	{
		return m_use_gpu;
	}
	void Engine::UseGpu(bool use_gpu)
	{
		m_use_gpu = use_gpu;
	}

	// CPU integration dispatch.
	void Engine::Integrate(std::span<RigidBodyDynamics> dynamics, float dt)
	{
		m_integrate_output.resize(dynamics.size());
		if (m_use_gpu)
		{
			m_gpu_integrator->Integrate(dynamics, dt, m_integrate_output);
		}
		else
		{
			for (auto& body : dynamics)
				Evolve(body, dt);
		}
	}

	// Broad phase overlap query → narrow phase collision detection → impulse resolution.
	void Engine::DetectAndResolveCollisions(float dt)
	{
		// This is the core collision pipeline, called after all bodies have been evolved.
		m_cache->Reset();

		auto& col_shapes = m_cache->m_col_shapes;
		auto& col_shape_verts = m_cache->m_col_shape_verts;
		auto& col_pairs = m_cache->m_col_pairs;
		auto& body_pairs = m_cache->m_body_pairs;
		auto& shape_map = m_cache->m_shape_map;

		if (m_use_gpu)
		{
			// Phase 1: Broadphase on CPU — collect overlapping pairs and pack shapes for GPU.
			
			// Track body pointers per pair so we can build physics::Contact structs after readback.
			// Shape deduplication: map shape pointer → index in col_shapes buffer
			auto get_or_add_shape = [&](Shape const& shape) -> int
			{
				auto it = shape_map.find(&shape);
				if (it != shape_map.end())
					return it->second;

				auto idx = static_cast<int>(col_shapes.size());
				col_shapes.push_back(PackShapeGeneric(shape, col_shape_verts));
				shape_map[&shape] = idx;
				return idx;
			};

			int pair_idx = 0;
			m_broadphase.EnumOverlappingPairs([&](RigidBody const& objA, RigidBody const& objB)
			{
				auto idx_a = get_or_add_shape(objA.Shape());
				auto idx_b = get_or_add_shape(objB.Shape());

				// Compute B-to-A transform (collision runs in A's object space)
				auto w2a = InvertAffine(objA.O2W());
				auto b2a = w2a * objB.O2W();

				GpuCollisionPair pair = {};
				pair.shape_idx_a = idx_a;
				pair.shape_idx_b = idx_b;
				pair.pair_index = pair_idx;
				pair.b2a = b2a;
				col_pairs.push_back(pair);

				body_pairs.push_back(BodyPair{ b2a, &objA, &objB });
				++pair_idx;
			});

			if (col_pairs.empty())
				return;

			// Phase 2: Dispatch GPU GJK collision detection.
			std::vector<GpuContact> gpu_contacts;
			m_gpu_collision_detector->DetectCollisions(col_pairs, col_shapes, col_shape_verts, gpu_contacts);

			// Phase 3: Convert GPU contacts to physics::Contact structs and resolve.
			std::vector<RbContact> collision_queue;
			for (auto const& gc : gpu_contacts)
			{
				auto const& bp = body_pairs[gc.pair_index];
				auto c = RbContact{*bp.objA, *bp.objB};

				// Copy geometric data from GPU contact (already in objA's space)
				c.m_axis = gc.axis;
				c.m_point = gc.pt;
				c.m_depth = gc.depth;
				c.m_mat_idA = gc.mat_id_a;
				c.m_mat_idB = gc.mat_id_b;

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

			// Sort by time of impact
			std::sort(std::begin(collision_queue), std::end(collision_queue), [](auto& lhs, auto& rhs)
			{
				return lhs.m_time < rhs.m_time;
			});

			// Notify subscribers
			PostCollisionDetection(*this, { collision_queue });

			// Apply impulses
			for (auto& c : collision_queue)
				ResolveCollision(c);
		}
		else
		{
			// Broad phase: find pairs of bodies whose bounding volumes overlap.
			// Narrow phase: test each pair for actual geometric contact.
			std::vector<RbContact> collision_queue;
			m_broadphase.EnumOverlappingPairs([&](RigidBody const& objA, RigidBody const& objB)
			{
				auto c = RbContact{ objA, objB };
				if (NarrowPhaseCollision(dt, c))
				{
					#ifdef PR_PHYSICS_DUMP_CONTACTS
					Dump(c);
					#endif
					collision_queue.push_back(c);
				}
			});

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
}
