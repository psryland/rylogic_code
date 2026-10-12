//*****************************************************************************
// Procedural animation - aim chain
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/pose.h"

namespace pr::animation
{
	// Turn a Neck chain so that its last bone faces 'target' (a model-space point, w = 1).
	//  - The turn is shared out along the chain: each bone, from the base, takes an equal share of the turn that is still needed.
	//    So a neck curves smoothly instead of bending at one joint.
	//  - Each bone's 'Cone' limit caps its total rotation from its rest orientation relative to its parent. If a limit is hit, later bones take
	//    more of the turn, and the last bone may not reach the target.
	//  - The chain's bones keep their parent-relative positions. Only their rotations change, starting from the current pose.
	//  - The model transforms of the chain's ancestors must be current. The chain's descendants are not updated.
	inline void AimChain(Rig const& rig, Chain const& chain, Pose& pose, v4 target)
	{
		pr_assert(chain.role == EChainRole::Neck && "Not a neck chain");
		auto const& bones = chain.bones;
		auto count = static_cast<int>(bones.size());
		auto last = bones.back();

		auto refresh = [&](int b)
		{
			// Compose the bone's local transform with its parent's model transform
			auto parent = rig.bones[b].parent;
			pose.model[b] = parent < 0 ? pose.local[b] : pose.model[parent] * pose.local[b];
		};

		// Make the chain's model transforms current from its parent
		for (auto b : bones)
			refresh(b);


		// Turn each bone about its head by its share of the remaining turn
		for (int i = 0; i != count; ++i)
		{
			// The remaining turn rotates the last bone's facing direction onto the direction from its head to the target
			auto face = Rotate(pose.model[last].rot * ~rig.rest_model[last].rot, chain.forward);
			auto want = Normalise(target - pose.model[last].pos, face);
			auto remaining = quat(face, want);
			auto share = Slerp(Identity<quat>(), remaining, 1.0f / (count - i));

			// Apply the share in model space, then express it as a rotation from the rest local orientation so the limit can cap it
			auto b = bones[i];
			auto const& bone = rig.bones[b];
			auto parent_rot = bone.parent < 0 ? Identity<quat>() : pose.model[bone.parent].rot;
			auto local_rot = ~parent_rot * share * pose.model[b].rot;
			auto delta = local_rot * ~bone.rest.rot;
			switch (bone.limit.kind)
			{
				case fabrik::ELimit::None:
				{
					break;
				}
				case fabrik::ELimit::Cone:
				{
					// Scale the rotation back to the cone's half-angle
					auto aa = AxisAngle(delta);
					if (aa.angle > bone.limit.max_angle)
						delta = quat(aa.axis, bone.limit.max_angle);

					break;
				}
				default:
				{
					throw std::invalid_argument(std::format("Bone '{}' has a limit kind that aim chains do not support", bone.name));
				}
			}
			pose.local[b].rot = Normalise(delta * bone.rest.rot);

			// Recompute the model transforms of this bone and the rest of the chain
			for (int j = i; j != count; ++j)
				refresh(bones[j]);
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(AimChainTests)
	{
		PRUnitTestMethod(RestTarget, Quick)
		{
			// A target straight ahead of the head leaves the rest pose unchanged
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& neck = rig.chains[1];
			auto head = rig.rest_model[neck.bones.back()].pos;
			AimChain(rig, neck, pose, head + neck.forward * 2.0f);
			for (auto b : neck.bones)
				PR_EXPECT(FEqlAbsolute(pose.model[b].pos, rig.rest_model[b].pos, 1e-5f) && FEqlOrientation(pose.model[b].rot, rig.rest_model[b].rot, 1e-4f));
		}
		PRUnitTestMethod(FacesTarget, Quick)
		{
			// The head faces a target to the side, and both neck bones share the turn
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& neck = rig.chains[1];
			auto target = v4(1.0f, -1.5f, 1.4f, 1);
			AimChain(rig, neck, pose, target);

			auto head = neck.bones.back();
			auto face = Rotate(pose.model[head].rot * ~rig.rest_model[head].rot, neck.forward);
			PR_EXPECT(FEqlAbsolute(face, Normalise(target - pose.model[head].pos), 1e-4f));

			auto turn0 = AxisAngle(pose.local[neck.bones[0]].rot * ~rig.bones[neck.bones[0]].rest.rot).angle;
			auto turn1 = AxisAngle(pose.local[head].rot * ~rig.bones[head].rest.rot).angle;
			PR_EXPECT(turn0 > 0.1f && turn1 > 0.1f);
		}
		PRUnitTestMethod(ConeLimit, Quick)
		{
			// A cone limit caps each bone's turn
			auto desc = TestRigDesc();
			for (auto b : desc.chains[1].bones)
				desc.bones[b].limit = JointLimit{ .kind = fabrik::ELimit::Cone, .max_angle = 0.2f };

			auto limited = Rig(std::move(desc));
			auto pose = Pose(limited);
			auto const& neck = limited.chains[1];
			AimChain(limited, neck, pose, v4(3.0f, 0.0f, 1.0f, 1));
			for (auto b : neck.bones)
				PR_EXPECT(AxisAngle(pose.local[b].rot * ~limited.bones[b].rest.rot).angle <= 0.2f + 1e-4f);
		}
	};
}
#endif
