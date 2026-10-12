//*****************************************************************************
// Procedural animation - two-bone IK
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/pose.h"

namespace pr::animation
{
	// The solved joint positions of a two-bone chain
	struct TwoBonePositions
	{
		v4 mid;   // The joint between the two bones (knee, elbow)
		v4 end;   // The end of the second bone (ankle, wrist)
	};

	// Place the joints of a two-bone chain rooted at 'root' so that its end reaches 'target'.
	//  - 'len0' and 'len1' are the bone lengths (positive).
	//  - 'pole' is the direction the middle joint bends towards (w = 0). Only its part perpendicular to the root-to-target line matters.
	//  - A target closer than |len0 - len1| or further than len0 + len1 is out of reach. The end then stops at the nearest reachable point on
	//    the line from the root towards the target.
	inline TwoBonePositions SolveTwoBone(v4 root, float len0, float len1, v4 target, v4 pole)
	{
		// The reach direction, and the in-plane direction the middle joint bends towards.
		// Degenerate directions (target at the root, pole along the reach line) fall back to an arbitrary perpendicular.
		auto dir = Normalise(target - root, Normalise(Perpendicular(pole)));
		auto bend = Normalise(pole - dir * Dot3(pole, dir), Normalise(Perpendicular(dir)));

		// Clamp the reach, then find the angle at the root. The sine comes from the triangle's side factors (Heron's form) rather than
		// sqrt(1 - cos^2), so that a fully extended or fully folded limb is exactly straight instead of amplifying rounding error.
		auto dist = std::clamp(Length(target - root), std::abs(len0 - len1), len0 + len1);
		auto area16 = (len0 + len1 + dist) * (-len0 + len1 + dist) * (len0 - len1 + dist) * (len0 + len1 - dist);
		auto cos_a = dist > 0 ? std::clamp((len0 * len0 + dist * dist - len1 * len1) / (2 * len0 * dist), -1.0f, 1.0f) : 0.0f;
		auto sin_a = dist > 0 ? std::min(std::sqrt(std::max(area16, 0.0f)) / (2 * len0 * dist), 1.0f) : 1.0f;

		return TwoBonePositions{
			.mid = root + (dir * cos_a + bend * sin_a) * len0,
			.end = root + dir * dist,
		};
	}

	// Return the orientation whose X axis is 'x' and whose Z axis is 'normal' made perpendicular to 'x'. 'x' must be a unit direction.
	inline quat BendFrame(v4 x, v4 normal)
	{
		// Complete a right-handed orthonormal basis
		auto z = Normalise(normal - x * Dot3(normal, x), Normalise(Perpendicular(x)));
		auto y = Cross(z, x);
		return quat(math::Mat3x3<float>(x, y, z));
	}

	// Return the normal of the plane a limb bends in, given its root-to-end direction and pole
	inline v4 BendNormal(v4 reach, v4 pole)
	{
		// The pole is not parallel to the reach for valid rigs; fall back to any perpendicular otherwise
		return Normalise(Cross(reach, pole), Normalise(Perpendicular(reach)));
	}

	// Pose a Leg or Arm chain so that its end bone has the model-space frame 'end', with the middle joint bending towards 'pole' (model space, w = 0).
	//  - The model transforms of the chain's ancestors must be current. The chain's descendants are not updated.
	//  - If 'end' is out of reach, the end bone keeps the orientation of 'end' at the nearest reachable position (see 'SolveTwoBone').
	//  - The upper and lower bones turn from their rest orientations by the rotation that maps the rest bend plane onto the solved bend plane, so
	//    they do not twist about the limb.
	inline void SolveLimb(Rig const& rig, Chain const& chain, Pose& pose, xform const& end, v4 pole)
	{
		// The rest geometry gives the bone lengths and the rest bend plane
		pr_assert((chain.role == EChainRole::Leg || chain.role == EChainRole::Arm) && "Not a limb chain");
		auto b0 = chain.bones[0];
		auto b1 = chain.bones[1];
		auto b2 = chain.bones[2];
		auto r0 = rig.rest_model[b0].pos;
		auto r1 = rig.rest_model[b1].pos;
		auto r2 = rig.rest_model[b2].pos;

		// The upper bone's head follows its parent
		auto parent = rig.bones[b0].parent;
		auto root = (parent < 0 ? pose.local[b0] : pose.model[parent] * pose.local[b0]).pos;
		auto joints = SolveTwoBone(root, Length(r1 - r0), Length(r2 - r1), end.pos, pole);

		// Rotate each bone by the change of its bend frame
		auto rest_normal = BendNormal(r2 - r0, chain.pole);
		auto normal = BendNormal(joints.end - root, pole);
		auto upper = BendFrame(Normalise(joints.mid - root), normal) * ~BendFrame(Normalise(r1 - r0), rest_normal);
		auto lower = BendFrame(Normalise(joints.end - joints.mid), normal) * ~BendFrame(Normalise(r2 - r1), rest_normal);

		// Write the results from the base of the chain down
		pose.SetModel(rig, b0, xform(root, upper * rig.rest_model[b0].rot));
		pose.SetModel(rig, b1, xform(joints.mid, lower * rig.rest_model[b1].rot));
		pose.SetModel(rig, b2, xform(joints.end, end.rot));
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(TwoBoneIKTests)
	{
		PRUnitTestMethod(Reachable, Quick)
		{
			// The end reaches the target, the bones keep their lengths, and the middle joint is on the pole side
			auto root = v4(0, 0, 0, 1);
			auto target = v4(1.2f, 0.3f, -0.2f, 1);
			auto pole = v4(0, 1, 0, 0);
			auto j = SolveTwoBone(root, 1.0f, 0.8f, target, pole);
			PR_EXPECT(FEqlAbsolute(j.end, target, 1e-5f));
			PR_EXPECT(FEqlAbsolute(Length(j.mid - root), 1.0f, 1e-5f));
			PR_EXPECT(FEqlAbsolute(Length(j.end - j.mid), 0.8f, 1e-5f));

			auto dir = Normalise(target - root);
			auto offset = j.mid - root;
			PR_EXPECT(Dot3(offset - dir * Dot3(offset, dir), pole) > 0);
		}
		PRUnitTestMethod(Unreachable, Quick)
		{
			// Too far: the limb is straight and points at the target. Too near: the end stops at the minimum reach.
			auto root = v4(0, 0, 0, 1);
			auto too_far = SolveTwoBone(root, 1.0f, 0.8f, v4(5, 0, 0, 1), v4(0, 1, 0, 0));
			PR_EXPECT(FEqlAbsolute(too_far.end, v4(1.8f, 0, 0, 1), 1e-5f));
			PR_EXPECT(FEqlAbsolute(too_far.mid, v4(1.0f, 0, 0, 1), 1e-5f));

			auto too_near = SolveTwoBone(root, 1.0f, 0.8f, v4(0.1f, 0, 0, 1), v4(0, 1, 0, 0));
			PR_EXPECT(FEqlAbsolute(too_near.end, v4(0.2f, 0, 0, 1), 1e-5f));
		}
		PRUnitTestMethod(LimbAtRest, Quick)
		{
			// Targeting the rest foot frame with the rest pole reproduces the rest pose
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& leg = rig.chains[2];
			SolveLimb(rig, leg, pose, rig.rest_model[leg.bones[2]], leg.pole);
			for (auto b : leg.bones)
				PR_EXPECT(FEqlAbsolute(pose.model[b], rig.rest_model[b], 1e-4f));
		}
		PRUnitTestMethod(LimbReachesTarget, Quick)
		{
			// The foot reaches a new frame, the knee stays forward, and the shin's tip meets the foot
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& leg = rig.chains[2];
			auto foot_frame = xform(v4(0.3f, -0.3f, 0.2f, 1), quat(v4::ZAxis(), 0.3f) * rig.rest_model[leg.bones[2]].rot);
			SolveLimb(rig, leg, pose, foot_frame, leg.pole);
			pose.UpdateModel(rig);

			PR_EXPECT(FEqlAbsolute(pose.model[leg.bones[2]], foot_frame, 1e-4f));
			PR_EXPECT(FEqlAbsolute(pose.Tip(rig, leg.bones[0]), pose.model[leg.bones[1]].pos, 1e-4f));
			PR_EXPECT(FEqlAbsolute(pose.Tip(rig, leg.bones[1]), pose.model[leg.bones[2]].pos, 1e-4f));
			PR_EXPECT(pose.model[leg.bones[1]].pos.y < Lerp(pose.model[leg.bones[0]].pos, foot_frame.pos, 0.5f).y);
		}
	};
}
#endif
