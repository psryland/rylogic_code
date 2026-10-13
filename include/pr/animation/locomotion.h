//*****************************************************************************
// Procedural animation - locomotion controller
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
// Poses a character's body to follow its motion through the world:
//  - The character has two postures. Standing is the rig's rest pose. Walking is derived from the rest pose: the spine is turned about the
//    pelvis until it is level, facing the way the neck faces, and each hand is placed on the ground beneath its shoulder. A character that
//    starts moving changes to the walking posture, and one that has stood still for a while changes back. Both changes are smooth.
//  - Walking limbs step in a cycle given by a gait description (see gait.h). Planted feet stay fixed in the world, swinging feet arc to a
//    predicted landing point on the ground, and a standing character steps until its feet are back beneath it. Arms carry weight only in
//    the walking posture; in the standing posture they hang from the chest as in the rest pose.
//  - The pelvis and chest follow the height of the ground under the hind and fore feet, so the body pitches on slopes.
//  - Neck chains turn to look at a world point, fading in and out.
//  - Tail chains hang from the body and are lifted out of the ground.
//
// The body's motion comes from outside (for example, a physics simulation); the controller does not move the character's origin.
// World space and model space both have +Z up. The ground is found by vertical probes, gathered so that each step queries the ground at most three
// times: once for the feet beneath the body, once for the landings of swinging feet, and once for the tail joints.
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/pose.h"
#include "pr/animation/targets.h"
#include "pr/animation/target_path.h"
#include "pr/animation/solve.h"
#include "pr/animation/tail.h"
#include "pr/animation/gait.h"

namespace pr::animation
{
	// A point on the ground and the ground's upward unit normal there, in world space
	struct GroundHit
	{
		v4 point = v4::Origin();
		v4 normal = v4::ZAxis();
	};

	// The ground that a character stands on
	struct IGround
	{
		virtual ~IGround() = default;

		// Write into 'hits[i]' the ground directly above or below 'points[i]' (world space, w = 1). The spans have equal lengths.
		// Callers gather their queries so that one call answers many points.
		virtual void Probe(std::span<v4 const> points, std::span<GroundHit> hits) const = 0;
	};

	// The world-space motion of a character's model origin
	struct BodyState
	{
		xform model_to_world = xform::Identity();
		v4 velocity = {};           // Linear velocity (w = 0)
		v4 angular_velocity = {};   // Axis * radians per second (w = 0)
		bool supported = true;      // False while the body is not resting on the ground, for example when falling
	};

	// What a walking limb's foot is doing
	enum class EFootState
	{
		// Planted on the ground, fixed in world space
		Stance,

		// Moving through the air from its lift-off frame to its landing frame
		Swing,

		// Held beneath the body because the body is not supported
		Air,
	};

	// The state of one walking limb. Frames are world-space transforms of the limb's end bone.
	struct LimbState
	{
		int chain = -1;                       // The limb's chain index in the rig
		int contact = -1;                     // The contact that the limb stands on
		GaitLimb gait;                        // The limb's phases from the gait
		EFootState state = EFootState::Stance;
		float swing = 0.0f;                   // Swing progress in [0,1]
		float swing_window = 1.0f;            // The fraction of the step cycle that the current swing lasts
		xform foot = xform::Identity();       // The end bone's goal this step
		xform lift = xform::Identity();       // Swing: where the foot left the ground
		xform land = xform::Identity();       // Swing: where the foot will touch down
		v4 ground = v4::Origin();             // The point on the ground beneath the foot's contact, ignoring the swing's arc
		v4 walk_contact = v4::Origin();       // The limb's contact point in the walking posture, in model space. Its height is the foot's clearance.
		quat walk_rot = Identity<quat>();     // The end bone's model-space orientation in the walking posture, on flat ground
		float support = 1.0f;                 // In [0,1]. How much the limb stands on 'foot'. Otherwise it hangs from the body in its rest pose.
	};

	// Animates a rig from its body's motion. See the top of this file.
	struct Locomotion
	{
		// Construct for 'rig', which must outlive the controller, with a gait that is valid for it. Throws 'std::invalid_argument' otherwise.
		Locomotion(Rig const& rig, GaitDesc gait)
			: m_rig(rig)
			, m_gait(std::move(gait))
			, m_pose(rig)
			, m_rest_targets(RestTargets(rig))
			, m_targets(m_rest_targets)
			, m_limbs()
			, m_neutral()
			, m_landings()
			, m_tail_joints()
			, m_probe_points()
			, m_probe_hits()
			, m_tail()
			, m_tail_effectors()
			, m_spine(-1)
			, m_pelvis()
			, m_chest()
			, m_walk_pelvis()
			, m_walk_chest()
			, m_posture(0.0f)
			, m_posture_goal(0.0f)
			, m_still_time(0.0f)
			, m_look(v4::Origin())
			, m_look_weight(0.0f)
			, m_phase(0.0f)
			, m_blend(0.0f)
			, m_started(false)
		{
			// Reject gaits that do not fit the rig at the construction boundary so that stepping can rely on them
			auto errors = Validate(m_gait, rig);
			if (!errors.empty())
			{
				std::string msg = "Invalid gait description:";
				for (auto const& e : errors)
					msg.append("\n  ").append(e);

				throw std::invalid_argument(msg);
			}

			// Resolve each limb's chain and contact. Arms start hanging, because the character starts in the standing posture.
			for (auto const& limb : m_gait.limbs)
			{
				auto chain = LimbChain(rig, limb.role, limb.side);
				m_limbs.push_back(LimbState{ .chain = chain, .contact = LimbContact(rig, rig.chains[chain]), .gait = limb, .support = limb.role == EChainRole::Leg ? 1.0f : 0.0f });
			}
			m_neutral.resize(m_limbs.size());

			// The first spine chain carries the pelvis and chest
			for (int c = 0, cend = static_cast<int>(rig.chains.size()); c != cend && m_spine < 0; ++c)
			{
				if (rig.chains[c].role == EChainRole::Spine)
					m_spine = c;
			}
			if (m_spine >= 0)
			{
				m_pelvis = TargetPath(m_rest_targets[m_spine].base);
				m_chest = TargetPath(m_rest_targets[m_spine].frame);
			}
			DeriveWalkingPosture();
		}

		// The current pose, the chain targets that produced it, and the walking limbs
		Pose const& pose() const
		{
			return m_pose;
		}
		std::span<Target const> targets() const
		{
			return m_targets;
		}
		std::span<LimbState const> limbs() const
		{
			return m_limbs;
		}

		// The step cycle position in [0,1), and the gait blend from walk (0) to trot (1)
		float phase() const
		{
			return m_phase;
		}
		float blend() const
		{
			return m_blend;
		}

		// The posture, from standing (0) to walking (1). See the top of this file.
		float posture() const
		{
			return m_posture.Value();
		}

		// Turn the neck chains towards 'world_point', or back to rest if it is empty, over 'fade_s' seconds (> 0)
		void SetLook(std::optional<v4> world_point, float fade_s)
		{
			// Fading out keeps the last point so that the head turns back along the same path
			if (world_point)
				m_look = *world_point;

			m_look_weight.Start(world_point ? 1.0f : 0.0f, fade_s);
		}

		// Advance by 'dt' seconds (>= 0) and pose the rig for 'body' on 'ground'
		void Step(float dt, BodyState const& body, IGround const& ground)
		{
			// Find where each foot would stand beneath the body now. Planting, settling and lifting all compare the feet with these frames.
			FindNeutralFeet(body.model_to_world, ground);

			// The first step plants every foot beneath the body
			if (!m_started)
			{
				for (int i = 0, iend = static_cast<int>(m_limbs.size()); i != iend; ++i)
				{
					m_limbs[i].foot = m_neutral[i];
					m_limbs[i].state = EFootState::Stance;
				}
				m_started = true;
			}

			// Measure the speed that the feet must keep up with, including turning on the spot
			auto up = v4::ZAxis();
			auto horizontal = body.velocity - up * Dot(body.velocity, up);
			auto turn = Dot(body.angular_velocity, up) * TurnRadius();
			auto speed = Sqrt(LengthSq(horizontal) + Sqr(turn));
			auto moving = body.supported && speed >= m_gait.stand_speed;

			// Change to the walking posture on moving, and back to standing after standing still for the rear delay. An unsupported body holds its posture.
			m_still_time = moving ? 0.0f : m_still_time + dt;
			auto posture_goal = !body.supported ? m_posture_goal : moving ? 1.0f : m_still_time >= m_gait.rear_delay ? 0.0f : m_posture_goal;
			if (posture_goal != m_posture_goal)
			{
				m_posture.Start(posture_goal, m_gait.posture_time);
				m_posture_goal = posture_goal;
			}
			m_posture.Step(dt);
			for (auto& limb : m_limbs)
				limb.support = limb.gait.role == EChainRole::Leg ? 1.0f : m_posture.Value();

			// Advance the cycle, and blend the gait no faster than one full change per cycle
			auto blend_goal = Clamp((speed - m_gait.walk_speed) / (m_gait.trot_speed - m_gait.walk_speed), 0.0f, 1.0f);
			auto cadence = !body.supported ? 0.0f : moving ? speed / Stride(speed, Duty()) : NeedsSettling() ? WalkCadence() : 0.0f;
			auto dphase = cadence * dt;
			m_phase = Frac(m_phase + dphase);
			m_blend += Clamp(blend_goal - m_blend, -dphase, dphase);

			// Advance each limb's stance and swing, collecting the landings that swinging feet still need to aim at
			auto duty = Duty();
			m_landings.clear();
			m_probe_points.clear();
			for (int i = 0, iend = static_cast<int>(m_limbs.size()); i != iend; ++i)
				AdvanceLimb(i, body, moving, cadence, dphase, duty);

			// Aim the swinging feet at the ground beneath their predicted landings
			auto hits = ProbeGround(ground);
			for (int k = 0, kend = static_cast<int>(m_landings.size()); k != kend; ++k)
			{
				auto& limb = m_limbs[m_landings[k].limb];
				limb.land = FootOnGround(limb, hits[k], m_landings[k].body_rot);
			}

			// Move each foot along its swing, then find the ground beneath it
			for (auto& limb : m_limbs)
			{
				MoveSwingingFoot(limb);
				limb.ground = GroundUnder(limb);
			}

			// Pose the body, then the chains that hang from it. A limb's goal moves from its rest frame relative to the chest to its foot as
			// it takes the body's weight.
			auto world_to_model = Invert(body.model_to_world);
			StepSpine(dt, world_to_model);
			for (auto const& limb : m_limbs)
			{
				auto foot = world_to_model * limb.foot;
				m_targets[limb.chain].frame = limb.support == 1.0f ? foot : BlendFrame(HangingFrame(limb), foot, limb.support);
			}

			StepNecks(dt, world_to_model);
			SolvePose(m_rig, m_targets, m_pose);
			StepTails(body.model_to_world, world_to_model, ground);
		}

	private:

		// A swinging foot whose landing is found by the next ground query
		struct Landing
		{
			int limb;      // Index into 'm_limbs'
			quat body_rot; // The body's predicted orientation at the landing
		};

		// A tail joint whose height is checked against the ground
		struct TailJoint
		{
			int chain;     // The tail's chain index
			int joint;     // The joint's index within the chain, where the chain's base is 0
		};

		Rig const& m_rig;
		GaitDesc m_gait;
		Pose m_pose;
		std::vector<Target> m_rest_targets;
		std::vector<Target> m_targets;
		std::vector<LimbState> m_limbs;
		std::vector<xform> m_neutral;              // Per limb, the foot frame beneath the body this step
		std::vector<Landing> m_landings;           // The landings that this step's second ground query answers
		std::vector<TailJoint> m_tail_joints;      // The tail joints that this step's third ground query answers
		std::vector<v4> m_probe_points;            // The points of the next ground query
		std::vector<GroundHit> m_probe_hits;       // The answers to the latest ground query
		TailSolver m_tail;
		std::vector<fabrik::Effector<float>> m_tail_effectors;
		int m_spine;                // The spine chain index, or -1 if the rig has none
		TargetPath m_pelvis;        // Model-space pelvis frame, smoothed
		TargetPath m_chest;         // Model-space chest frame, smoothed
		xform m_walk_pelvis;        // Model-space pelvis frame in the walking posture, on flat ground
		xform m_walk_chest;         // Model-space chest frame in the walking posture, on flat ground
		Fade m_posture;             // Standing (0) to walking (1)
		float m_posture_goal;       // The posture that 'm_posture' is moving to
		float m_still_time;         // Seconds since the body last moved
		v4 m_look;                  // World-space look point
		Fade m_look_weight;         // How strongly the necks face 'm_look'
		float m_phase;              // The step cycle position in [0,1)
		float m_blend;              // Walk (0) to trot (1)
		bool m_started;             // False until the feet have been planted

		// Return the fractional part of 'x'
		static float Frac(float x)
		{
			return x - std::floor(x);
		}

		// The duty factor for the current gait blend
		float Duty() const
		{
			return m_gait.walk_duty + (m_gait.trot_duty - m_gait.walk_duty) * m_blend;
		}

		// The distance the body moves per cycle at 'speed'. A planted foot must not be passed by more than the maximum stance travel.
		float Stride(float speed, float duty) const
		{
			auto stride = m_gait.stride_min + m_gait.stride_per_speed * speed;
			return std::min(stride, m_gait.max_stance_travel / duty);
		}

		// Cycles per second at walking speed, used to step the feet back beneath a standing body
		float WalkCadence() const
		{
			return m_gait.walk_speed / Stride(m_gait.walk_speed, m_gait.walk_duty);
		}

		// The limb's contact point in the rest pose, in model space
		v4 RestContact(LimbState const& limb) const
		{
			auto const& contact = m_rig.contacts[limb.contact];
			return m_rig.rest_model[contact.bone] * contact.offset;
		}

		// Blend from frame 'a' (frac = 0) to frame 'b' (frac = 1)
		static xform BlendFrame(xform const& a, xform const& b, float frac)
		{
			// Positions blend linearly, rotations by the shortest arc
			return xform(Lerp(a.pos, b.pos, frac), Slerp(a.rot, b.rot, frac));
		}

		// The end bone frame of a limb hanging from the body in its rest pose, relative to the current chest (model space)
		xform HangingFrame(LimbState const& limb) const
		{
			// Without a spine the body does not move relative to the model origin
			auto end = m_rig.rest_model[m_rig.chains[limb.chain].bones.back()];
			if (m_spine < 0)
				return end;

			return m_chest.Frame() * Invert(m_rest_targets[m_spine].frame) * end;
		}

		// The distance from the model origin at which turning on the spot moves the feet: the mean horizontal distance of the walking
		// contacts, weighted by how much each limb supports the body
		float TurnRadius() const
		{
			// Hanging limbs do not need to keep up with turning
			auto radius = 0.0f;
			auto support = 0.0f;
			for (auto const& limb : m_limbs)
			{
				radius += limb.support * Sqrt(Sqr(limb.walk_contact.x) + Sqr(limb.walk_contact.y));
				support += limb.support;
			}
			return support > 0 ? radius / support : 0.0f;
		}

		// Derive the walking posture from the rest pose (see the top of this file): the spine frames and each limb's walking contact
		void DeriveWalkingPosture()
		{
			// Turn the spine about the pelvis until it points level, the way the first neck faces. Rigs without a neck have no facing
			// direction, so their spine keeps its rest pitch.
			auto pitch = Identity<quat>();
			auto pivot = m_spine >= 0 ? m_rest_targets[m_spine].base.pos : v4::Origin();
			if (m_spine >= 0)
			{
				// Only the first neck sets the facing
				auto span = m_rest_targets[m_spine].frame.pos - pivot;
				auto neck = std::find_if(m_rig.chains.begin(), m_rig.chains.end(), [](Chain const& c) { return c.role == EChainRole::Neck; });
				auto ahead = neck != m_rig.chains.end() ? v4(neck->forward.x, neck->forward.y, 0, 0) : v4::Zero();
				if (LengthSq(ahead) > tiny<float> && LengthSq(span) > tiny<float>)
					pitch = quat(Normalise(span), Normalise(ahead));

				m_walk_pelvis = xform(pivot, Normalise(pitch * m_rest_targets[m_spine].base.rot));
				m_walk_chest = xform(pivot + Rotate(pitch, span), Normalise(pitch * m_rest_targets[m_spine].frame.rot));
			}

			// Legs keep their rest contacts. Their mean height is the clearance that the hands stand at.
			auto clearance = 0.0f;
			auto legs = 0;
			for (auto& limb : m_limbs)
			{
				// Arms are placed after the clearance is known
				if (limb.gait.role != EChainRole::Leg)
					continue;

				limb.walk_contact = RestContact(limb);
				limb.walk_rot = m_rig.rest_model[m_rig.chains[limb.chain].bones.back()].rot;
				clearance += limb.walk_contact.z;
				legs += 1;
			}

			// Place each hand beneath its turned shoulder, pointing the way the foot on its side points. Lower the body where a hand could
			// not reach the ground from its shoulder without nearly straightening the arm.
			constexpr float max_extension = 0.9f;
			auto lower = 0.0f;
			for (auto& limb : m_limbs)
			{
				// The arm's rest end bone direction is turned onto the rest direction of the same side's foot, if there is one
				if (limb.gait.role == EChainRole::Leg)
					continue;

				auto const& chain = m_rig.chains[limb.chain];
				auto rot = m_rig.rest_model[chain.bones.back()].rot;
				auto leg = LimbChain(m_rig, EChainRole::Leg, chain.side);
				if (leg >= 0)
				{
					auto foot_rot = m_rig.rest_model[m_rig.chains[leg].bones.back()].rot;
					rot = Normalise(quat(Rotate(rot, v4::YAxis()), Rotate(foot_rot, v4::YAxis())) * rot);
				}
				auto shoulder = pivot + Rotate(pitch, m_rig.rest_model[chain.bones[0]].pos - pivot);
				limb.walk_rot = rot;
				limb.walk_contact = v4(shoulder.x, shoulder.y, legs != 0 ? clearance / legs : RestContact(limb).z, 1);

				// The limb reaches from its root to the end bone's head
				auto reach = 0.0f;
				for (int b = 0, bend = static_cast<int>(chain.bones.size()) - 1; b < bend; ++b)
					reach += m_rig.bones[chain.bones[b]].length;

				reach *= max_extension;
				auto wrist = limb.walk_contact - Rotate(rot, m_rig.contacts[limb.contact].offset.w0());
				auto offset = shoulder - wrist;
				if (LengthSq(offset) > Sqr(reach))
					lower = std::max(lower, offset.z - Sqrt(std::max(Sqr(reach) - Sqr(offset.x) - Sqr(offset.y), 0.0f)));
			}
			m_walk_pelvis.pos.z -= lower;
			m_walk_chest.pos.z -= lower;
		}

		// The world-space contact point of a foot frame
		v4 ContactOf(LimbState const& limb, xform const& foot) const
		{
			return foot * m_rig.contacts[limb.contact].offset;
		}

		// Query the ground at every point in 'm_probe_points' in one call. The answers stay valid until the next query.
		std::span<GroundHit const> ProbeGround(IGround const& ground)
		{
			// An empty query does not reach the ground at all
			m_probe_hits.resize(m_probe_points.size());
			if (!m_probe_points.empty())
				ground.Probe(m_probe_points, m_probe_hits);

			return m_probe_hits;
		}

		// The end bone frame that puts the limb's contact on the ground at 'hit', with the walking posture's clearance and orientation relative
		// to the body, tilted to match the ground's slope
		xform FootOnGround(LimbState const& limb, GroundHit const& hit, quat const& body_rot) const
		{
			// The walking contact's height above the model origin is kept as clearance above the ground
			auto const& contact = m_rig.contacts[limb.contact];
			auto rot = Normalise(quat(v4::ZAxis(), hit.normal) * body_rot * limb.walk_rot);
			auto point = hit.point + hit.normal * limb.walk_contact.z;
			return xform(point - Rotate(rot, contact.offset.w0()), rot);
		}

		// Find the foot frame beneath each walking contact for a body at 'model_to_world'
		void FindNeutralFeet(xform const& model_to_world, IGround const& ground)
		{
			// One query covers every limb
			m_probe_points.clear();
			for (auto const& limb : m_limbs)
				m_probe_points.push_back(model_to_world * limb.walk_contact);

			auto hits = ProbeGround(ground);
			for (int i = 0, iend = static_cast<int>(m_limbs.size()); i != iend; ++i)
				m_neutral[i] = FootOnGround(m_limbs[i], hits[i], model_to_world.rot);
		}

		// True if limb 'i' is planted further than the settle distance from its neutral position
		bool IsDisplaced(int i) const
		{
			// Only the horizontal distance matters; the neutral frame is already on the ground
			auto const& limb = m_limbs[i];
			auto offset = ContactOf(limb, limb.foot) - ContactOf(limb, m_neutral[i]);
			return Sqr(offset.x) + Sqr(offset.y) > Sqr(m_gait.settle_distance);
		}

		// True if a standing body must keep stepping: a supporting foot is swinging or is away from its neutral position
		bool NeedsSettling() const
		{
			for (int i = 0, iend = static_cast<int>(m_limbs.size()); i != iend; ++i)
			{
				if (m_limbs[i].support != 0 && (m_limbs[i].state != EFootState::Stance || IsDisplaced(i)))
					return true;
			}
			return false;
		}

		// The body's model-to-world transform after 't' seconds, assuming constant velocity
		static xform PredictBody(BodyState const& body, float t)
		{
			// Rotate about the angular velocity's axis, if there is any rotation
			auto rot = body.model_to_world.rot;
			auto angle = Length(body.angular_velocity) * t;
			if (angle > tiny<float>)
				rot = Normalise(quat(Normalise(body.angular_velocity), angle) * rot);

			return xform(body.model_to_world.pos + body.velocity * t, rot);
		}

		// Advance limb 'i' through its stance and swing. A swinging foot that still needs a landing adds its query to 'm_probe_points'.
		void AdvanceLimb(int i, BodyState const& body, bool moving, float cadence, float dphase, float duty)
		{
			// An unsupported body holds its feet beneath it. On regaining support, the feet swing down to the ground.
			auto& limb = m_limbs[i];
			if (!body.supported)
			{
				limb.state = EFootState::Air;
				limb.foot = body.model_to_world * m_rig.rest_model[m_rig.chains[limb.chain].bones.back()];
				return;
			}
			if (limb.state == EFootState::Air)
				StartSwing(limb, 1.0f - duty);

			// A limb that carries no weight does not step. Its foot follows the ground beneath its walking contact, ready to take weight.
			if (limb.support == 0)
			{
				limb.state = EFootState::Stance;
				limb.foot = m_neutral[i];
				return;
			}

			// A planted foot lifts once the cycle reaches its swing, if the body is moving or the foot is out of place. A swing that would be
			// too short to look like a step waits for the next cycle.
			auto offset = limb.gait.walk_phase + (limb.gait.trot_phase - limb.gait.walk_phase) * m_blend;
			auto local = Frac(m_phase - offset);
			constexpr float min_swing_window = 0.05f;
			if (limb.state == EFootState::Stance && local >= duty && 1.0f - local > min_swing_window && (moving || IsDisplaced(i)))
				StartSwing(limb, 1.0f - local);

			if (limb.state != EFootState::Swing)
				return;

			// Aim the landing at the neutral position at the middle of the next stance, so that the foot passes beneath the body. The landing
			// is fixed late in the swing so that the foot settles onto a steady point.
			limb.swing = std::min(limb.swing + dphase / limb.swing_window, 1.0f);
			constexpr float land_freeze = 0.7f;
			if (limb.swing < land_freeze)
			{
				auto to_landing = cadence > 0 ? (1.0f - limb.swing) * limb.swing_window / cadence : 0.0f;
				auto half_stance = moving && cadence > 0 ? 0.5f * duty / cadence : 0.0f;
				auto predicted = PredictBody(body, to_landing + half_stance);
				m_landings.push_back(Landing{ .limb = i, .body_rot = predicted.rot });
				m_probe_points.push_back(predicted * limb.walk_contact);
			}
		}

		// Move a swinging foot from lift-off towards its landing, touching down at the end of the swing
		void MoveSwingingFoot(LimbState& limb) const
		{
			// Only swinging feet move
			if (limb.state != EFootState::Swing)
				return;

			if (limb.swing >= 1.0f)
			{
				limb.foot = limb.land;
				limb.state = EFootState::Stance;
				return;
			}

			// Move smoothly from lift-off to landing, raised by an arc that is flat at both ends
			auto s = limb.swing;
			auto ease = s * s * s * (10.0f + s * (-15.0f + 6.0f * s));
			auto arc = 64.0f * Cube(s * (1.0f - s));
			limb.foot = xform(limb.lift.pos + (limb.land.pos - limb.lift.pos) * ease + v4::ZAxis() * m_gait.swing_height * arc, Slerp(limb.lift.rot, limb.land.rot, ease));
		}

		// Lift a foot from where it is, for a swing lasting 'window' of the step cycle
		static void StartSwing(LimbState& limb, float window)
		{
			limb.state = EFootState::Swing;
			limb.swing = 0.0f;
			limb.swing_window = window;
			limb.lift = limb.foot;
			limb.land = limb.foot;
		}

		// The point on the ground beneath a limb's contact, ignoring the swing's arc
		v4 GroundUnder(LimbState const& limb) const
		{
			switch (limb.state)
			{
				case EFootState::Stance:
				case EFootState::Air:
				{
					return ContactOf(limb, limb.foot);
				}
				case EFootState::Swing:
				{
					auto s = limb.swing;
					auto ease = s * s * s * (10.0f + s * (-15.0f + 6.0f * s));
					return ContactOf(limb, limb.lift) + (ContactOf(limb, limb.land) - ContactOf(limb, limb.lift)) * ease;
				}
				default:
				{
					throw std::invalid_argument("Unknown foot state");
				}
			}
		}

		// Raise or lower the pelvis and chest by the height of the ground under the hind and fore feet, pitching the body between them. The
		// spine's frames on flat ground blend from the rest pose to the walking posture by the posture weight.
		void StepSpine(float dt, xform const& world_to_model)
		{
			// Hind feet are legs and fore feet are arms. With only one group, the body moves without pitching.
			if (m_spine < 0)
				return;

			float height[2] = {};
			int count[2] = {};
			auto arm_support = 0.0f;
			for (auto const& limb : m_limbs)
			{
				if (limb.state == EFootState::Air)
					continue;

				auto i = limb.gait.role == EChainRole::Leg ? 0 : 1;
				height[i] += (world_to_model * limb.ground).z - limb.walk_contact.z;
				count[i] += 1;
				arm_support += i == 1 ? limb.support : 0.0f;
			}

			// Hanging arms do not set the height of the chest, so the fore height moves from the hind height to the arms' height as they
			// take weight
			auto hind = count[0] != 0 ? height[0] / count[0] : count[1] != 0 ? height[1] / count[1] : 0.0f;
			auto fore = count[1] != 0 ? hind + (height[1] / count[1] - hind) * (count[0] != 0 ? arm_support / count[1] : 1.0f) : hind;

			// Each end moves by its own height, and both turn by the pitch of the line between them
			auto w = m_posture.Value();
			auto pelvis0 = BlendFrame(m_rest_targets[m_spine].base, m_walk_pelvis, w);
			auto chest0 = BlendFrame(m_rest_targets[m_spine].frame, m_walk_chest, w);
			auto span = chest0.pos - pelvis0.pos;
			auto pitch = quat(Normalise(span), Normalise(span + v4::ZAxis() * (fore - hind)));
			auto pelvis = xform(pelvis0.pos + v4::ZAxis() * hind, Normalise(pitch * pelvis0.rot));
			auto chest = xform(chest0.pos + v4::ZAxis() * fore, Normalise(pitch * chest0.rot));

			// Follow the goals smoothly so that steps on uneven ground do not jolt the body
			m_pelvis.Retarget(pelvis, m_gait.body_response);
			m_chest.Retarget(chest, m_gait.body_response);
			m_pelvis.Step(dt);
			m_chest.Step(dt);
			m_targets[m_spine].base = m_pelvis.Frame();
			m_targets[m_spine].frame = m_chest.Frame();
		}

		// Turn each neck from its rest aim towards the look point by the look weight
		void StepNecks(float dt, xform const& world_to_model)
		{
			m_look_weight.Step(dt);
			auto weight = m_look_weight.Value();
			auto look = world_to_model * m_look;
			for (int c = 0, cend = static_cast<int>(m_rig.chains.size()); c != cend; ++c)
			{
				// Rotate the rest aim point about the neck's base, so that a zero weight gives the rest aim exactly
				auto const& chain = m_rig.chains[c];
				if (chain.role != EChainRole::Neck)
					continue;

				auto base = m_rig.rest_model[chain.bones.front()].pos;
				auto rest_aim = m_rest_targets[c].frame.pos - base;
				auto turn = Slerp(Identity<quat>(), quat(Normalise(rest_aim), Normalise(look - base, Normalise(rest_aim))), weight);
				m_targets[c].frame.pos = base + Rotate(turn, rest_aim);
			}
		}

		// Let each tail hang from the posed body, then lift any part of it that is below the ground back onto the ground
		void StepTails(xform const& model_to_world, xform const& world_to_model, IGround const& ground)
		{
			// Hang every tail first, so that one ground query covers the joints of all of them
			m_tail_joints.clear();
			m_probe_points.clear();
			for (int c = 0, cend = static_cast<int>(m_rig.chains.size()); c != cend; ++c)
			{
				// Check each joint after the base, which is attached to the body
				auto const& chain = m_rig.chains[c];
				if (chain.role != EChainRole::Tail)
					continue;

				m_tail.Solve(m_rig, c, m_pose, {}, fabrik::Settings<float>{});
				auto count = static_cast<int>(chain.bones.size());
				for (int k = 1; k <= count; ++k)
				{
					auto joint = k != count ? m_pose.model[chain.bones[k]].pos : m_pose.Tip(m_rig, chain.bones.back());
					m_tail_joints.push_back(TailJoint{ .chain = c, .joint = k });
					m_probe_points.push_back(model_to_world * joint);
				}
			}

			// Pull each buried joint up to the ground directly above it. The joints of one tail are adjacent in the query.
			auto hits = ProbeGround(ground);
			for (int j = 0, jend = static_cast<int>(m_tail_joints.size()); j != jend;)
			{
				// Gather the effectors of one tail, then solve it again if any joint was buried
				auto chain = m_tail_joints[j].chain;
				m_tail_effectors.clear();
				for (; j != jend && m_tail_joints[j].chain == chain; ++j)
				{
					auto const& world = m_probe_points[j];
					auto ground_z = hits[j].point.z;
					if (world.z < ground_z)
						m_tail_effectors.push_back(fabrik::Effector<float>{ .joint = m_tail_joints[j].joint, .target = world_to_model * v4(world.x, world.y, ground_z, 1) });
				}
				if (!m_tail_effectors.empty())
					m_tail.Solve(m_rig, chain, m_pose, m_tail_effectors, fabrik::Settings<float>{});
			}
			m_pose.UpdateModel(m_rig);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	// A ground plane through the origin with height 'slope' * y. Counts the queries it answers.
	struct TestGround : IGround
	{
		float m_slope;
		mutable int m_queries;

		explicit TestGround(float slope = 0.0f)
			: m_slope(slope)
			, m_queries(0)
		{
		}

		void Probe(std::span<v4 const> points, std::span<GroundHit> hits) const override
		{
			// Every probe drops straight onto the plane
			auto normal = Normalise(v4(0, -m_slope, 1, 0));
			for (size_t i = 0; i != points.size(); ++i)
				hits[i] = GroundHit{ .point = v4(points[i].x, points[i].y, m_slope * points[i].y, 1), .normal = normal };

			++m_queries;
		}
	};

	PRUnitTestClass(LocomotionTests)
	{
		PRUnitTestMethod(StandingAtRest, Quick)
		{
			// A still body on flat ground keeps its rest pose and does not step
			auto rig = TestRig();
			auto loco = Locomotion(rig, DefaultGait(rig));
			auto ground = TestGround();
			for (int i = 0; i != 60; ++i)
				loco.Step(1.0f / 60, BodyState{}, ground);

			PR_EXPECT(loco.phase() == 0.0f);
			for (int b = 0, bend = static_cast<int>(rig.bones.size()); b != bend; ++b)
				PR_EXPECT(FEqlAbsolute(loco.pose().model[b].pos, rig.rest_model[b].pos, 1e-3f));
		}
		PRUnitTestMethod(WalkingPlantsFeet, Quick)
		{
			// Walking forward (-Y), planted feet stay fixed and on the ground, every foot steps, and the cycle advances by distance over stride
			auto rig = TestRig();
			auto gait = DefaultGait(rig);
			auto loco = Locomotion(rig, gait);
			auto ground = TestGround();
			auto dt = 1.0f / 60;
			auto body = BodyState{ .velocity = v4(0, -gait.walk_speed, 0, 0) };
			loco.Step(0, body, ground);
			auto previous = std::vector<LimbState>(loco.limbs().begin(), loco.limbs().end());
			std::vector<int> touchdowns(previous.size());
			for (int i = 0; i != 120; ++i)
			{
				body.model_to_world.pos += body.velocity * dt;
				loco.Step(dt, body, ground);
				for (size_t l = 0; l != previous.size(); ++l)
				{
					auto const& limb = loco.limbs()[l];
					if (limb.state == EFootState::Stance && previous[l].state == EFootState::Stance)
						PR_EXPECT(FEqlAbsolute(limb.foot, previous[l].foot, 1e-6f));
					if (limb.state == EFootState::Stance && previous[l].state == EFootState::Swing)
						touchdowns[l] += 1;
					if (limb.state == EFootState::Stance)
						PR_EXPECT(FEqlAbsolute(limb.ground.z, 0.0f, 1e-5f));

					previous[l] = limb;
				}
			}
			for (auto n : touchdowns)
				PR_EXPECT(n >= 2);

			// Each step queries the ground at most three times however many points it needs
			PR_EXPECT(ground.m_queries <= 3 * 121);

			auto stride = std::min(gait.stride_min + gait.stride_per_speed * gait.walk_speed, gait.max_stance_travel / gait.walk_duty);
			auto cycles = gait.walk_speed * 120 * dt / stride;
			PR_EXPECT(FEqlAbsolute(loco.phase(), cycles - std::floor(cycles), 1e-3f));
		}
		PRUnitTestMethod(StoppingSettles, Quick)
		{
			// After walking then stopping, the feet step back beneath the body and the cycle stops
			auto rig = TestRig();
			auto gait = DefaultGait(rig);
			auto loco = Locomotion(rig, gait);
			auto ground = TestGround();
			auto dt = 1.0f / 60;
			auto body = BodyState{ .velocity = v4(0, -gait.walk_speed, 0, 0) };
			for (int i = 0; i != 50; ++i)
			{
				body.model_to_world.pos += body.velocity * dt;
				loco.Step(dt, body, ground);
			}
			body.velocity = v4::Zero();
			for (int i = 0; i != 300; ++i)
				loco.Step(dt, body, ground);

			auto phase = loco.phase();
			loco.Step(dt, body, ground);
			PR_EXPECT(loco.phase() == phase);
			for (auto const& limb : loco.limbs())
			{
				auto neutral = body.model_to_world * limb.walk_contact;
				PR_EXPECT(limb.state == EFootState::Stance);
				PR_EXPECT(Length((limb.ground - neutral).w0()) <= gait.settle_distance + 1e-4f);
			}
		}
		PRUnitTestMethod(SlopePitchesBody, Quick)
		{
			// Walking across ground that falls away ahead (-Y is downhill when the slope is positive), then standing in the walking posture,
			// the supporting feet rest on the ground and the chest drops
			auto rig = TestRig();
			auto gait = DefaultGait(rig);
			gait.rear_delay = 100.0f;
			auto loco = Locomotion(rig, gait);
			auto ground = TestGround(0.2f);
			auto dt = 1.0f / 60;
			auto body = BodyState{ .velocity = v4(gait.walk_speed, 0, 0, 0) };
			for (int i = 0; i != 60; ++i)
			{
				body.model_to_world.pos += body.velocity * dt;
				loco.Step(dt, body, ground);
			}
			body.velocity = v4::Zero();
			for (int i = 0; i != 180; ++i)
				loco.Step(dt, body, ground);

			PR_EXPECT(loco.posture() == 1.0f);
			for (auto const& limb : loco.limbs())
			{
				auto contact = body.model_to_world * (loco.pose().model[rig.contacts[limb.contact].bone] * rig.contacts[limb.contact].offset);
				PR_EXPECT(FEqlAbsolute(contact.z, 0.2f * contact.y, 1e-3f));
			}
			auto chest = rig.BoneIndex("chest");
			PR_EXPECT(loco.pose().model[chest].pos.z < rig.rest_model[chest].pos.z - 0.05f);
		}
		PRUnitTestMethod(PostureFollowsMotion, Quick)
		{
			// A rig that stands upright on its hind legs drops onto all fours to walk, stays down through a short stop, and rears back up
			// after a long one
			auto desc = TestRigDesc();
			{
				// Stand the test rig up: turn the spine to point up about the hips, and carry the neck and arm up with the chest's tip
				std::vector<xform> model;
				for (auto const& bone : desc.bones)
					model.push_back(bone.parent < 0 ? bone.rest : model[bone.parent] * bone.rest);

				auto pivot = model[1].pos;
				auto turn = quat(-v4::YAxis(), v4::ZAxis());
				for (int b : { 1, 2, 3 })
					model[b] = xform(pivot + Rotate(turn, model[b].pos - pivot), Normalise(turn * model[b].rot));

				auto lift = v4(0, 0.9f, 0.9f, 0);
				for (int b : { 4, 5, 9, 10, 11 })
					model[b].pos += lift;

				for (int b = 0, bend = static_cast<int>(desc.bones.size()); b != bend; ++b)
				{
					auto parent = desc.bones[b].parent;
					desc.bones[b].rest = parent < 0 ? model[b] : Invert(model[parent]) * model[b];
				}
			}
			auto rig = Rig(desc);
			auto chest = rig.BoneIndex("chest");
			auto standing_z = rig.rest_model[chest].pos.z;
			auto gait = DefaultGait(rig);
			auto loco = Locomotion(rig, gait);
			auto ground = TestGround();
			auto dt = 1.0f / 60;
			auto body = BodyState{};
			for (int i = 0; i != 30; ++i)
				loco.Step(dt, body, ground);

			PR_EXPECT(loco.posture() == 0.0f);
			PR_EXPECT(FEqlAbsolute(loco.pose().model[chest].pos.z, standing_z, 1e-3f));

			// Walking puts the chest down near the height of the shoulders above the ground, with the hands planted
			body.velocity = v4(0, -gait.walk_speed, 0, 0);
			for (int i = 0; i != 120; ++i)
			{
				body.model_to_world.pos += body.velocity * dt;
				loco.Step(dt, body, ground);
			}
			PR_EXPECT(loco.posture() == 1.0f);
			PR_EXPECT(loco.pose().model[chest].pos.z < 1.1f);
			for (auto const& limb : loco.limbs())
			{
				if (limb.gait.role != EChainRole::Arm || limb.state != EFootState::Stance)
					continue;

				auto contact = loco.pose().model[rig.contacts[limb.contact].bone] * rig.contacts[limb.contact].offset;
				PR_EXPECT(FEqlAbsolute(contact.z, 0.0f, 1e-2f));
			}

			// A short stop keeps the walking posture
			body.velocity = v4::Zero();
			for (int i = 0; i != 30; ++i)
				loco.Step(dt, body, ground);

			PR_EXPECT(loco.posture() == 1.0f);

			// A long stop rears back up to the standing posture
			for (int i = 0; i != 180; ++i)
				loco.Step(dt, body, ground);

			PR_EXPECT(loco.posture() == 0.0f);
			PR_EXPECT(FEqlAbsolute(loco.pose().model[chest].pos.z, standing_z, 1e-2f));
		}
		PRUnitTestMethod(LookFadesInAndOut, Quick)
		{
			// The head turns to face a look point, then back to rest
			auto rig = TestRig();
			auto loco = Locomotion(rig, DefaultGait(rig));
			auto ground = TestGround();
			auto head = rig.BoneIndex("head");
			auto const& neck = rig.chains[1];
			auto facing = [&]
			{
				// The direction the head faces, from its rotation away from rest
				return Rotate(loco.pose().model[head].rot * ~rig.rest_model[head].rot, neck.forward);
			};

			auto look = v4(3, -1.1f, 1.2f, 1);
			loco.SetLook(look, 0.25f);
			for (int i = 0; i != 60; ++i)
				loco.Step(1.0f / 60, BodyState{}, ground);

			PR_EXPECT(Dot(facing(), Normalise(look - loco.pose().model[head].pos)) > 0.95f);

			loco.SetLook(std::nullopt, 0.25f);
			for (int i = 0; i != 60; ++i)
				loco.Step(1.0f / 60, BodyState{}, ground);

			PR_EXPECT(Dot(facing(), neck.forward) > 0.999f);
		}
	};
}
#endif
