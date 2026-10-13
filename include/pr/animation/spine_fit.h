//*****************************************************************************
// Procedural animation - spine curve fit
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/pose.h"

namespace pr::animation
{
	// Pose a Spine chain along a smooth curve from the first bone's model-space frame 'pelvis' to the last bone's model-space frame 'chest'.
	//  - The first and last bones take 'pelvis' and 'chest' exactly. The heads of the bones between are placed along the curve.
	//  - The curve leaves the pelvis and arrives at the chest along their rest-pose spine directions, rotated by each frame's change from rest.
	//  - Bones are spaced along the curve in proportion to their rest spacing. If the chest is not at its rest distance from the pelvis, the
	//    in-between bones stretch or compress slightly so that both ends stay exact.
	//  - The twist of each in-between bone blends from the pelvis's change from rest to the chest's.
	//  - The model transforms of the chain's ancestors must be current. The chain's descendants are not updated.
	inline void FitSpine(Rig const& rig, Chain const& chain, Pose& pose, xform const& pelvis, xform const& chest)
	{
		pr_assert(chain.role == EChainRole::Spine && "Not a spine chain");
		auto const& bones = chain.bones;
		auto count = static_cast<int>(bones.size());
		auto head = [&](int k) { return rig.rest_model[bones[k]].pos; };

		// The rest spacing of the bone heads along the spine
		auto rest_length = 0.0f;
		for (int k = 1; k != count; ++k)
			rest_length += Length(head(k) - head(k - 1));

		// Each end's rotation from its rest orientation
		auto dq0 = pelvis.rot * ~rig.rest_model[bones.front()].rot;
		auto dq1 = chest.rot * ~rig.rest_model[bones.back()].rot;

		// A cubic Hermite curve between the ends, with tangents scaled by the chord so the curve bends gently
		auto p0 = pelvis.pos;
		auto p1 = chest.pos;
		auto chord = Length(p1 - p0);
		auto m0 = Rotate(dq0, Normalise(head(1) - head(0))) * chord;
		auto m1 = Rotate(dq1, Normalise(head(count - 1) - head(count - 2))) * chord;
		auto curve = [&](float u)
		{
			// Standard cubic Hermite basis
			auto u2 = u * u;
			auto u3 = u2 * u;
			return
				p0 * (2 * u3 - 3 * u2 + 1) +
				m0 * (u3 - 2 * u2 + u) +
				p1 * (-2 * u3 + 3 * u2) +
				m1 * (u3 - u2);
		};

		// Tabulate arc length so that joints can be spaced by distance along the curve
		constexpr int Samples = 32;
		std::array<float, Samples + 1> arc = {};
		auto prev = curve(0);
		for (int i = 1; i <= Samples; ++i)
		{
			auto pt = curve(static_cast<float>(i) / Samples);
			arc[i] = arc[i - 1] + Length(pt - prev);
			prev = pt;
		}
		auto joint_at = [&](float frac)
		{
			// Find the curve point at fraction 'frac' of the arc length by interpolating the table
			auto s = frac * arc[Samples];
			auto i = static_cast<int>(std::upper_bound(arc.begin() + 1, arc.end(), s) - arc.begin());
			i = std::clamp(i, 1, Samples);
			auto span = arc[i] - arc[i - 1];
			auto t = span > 0 ? (s - arc[i - 1]) / span : 0.0f;
			return curve((i - 1 + t) / Samples);
		};

		// The pelvis takes its frame exactly
		pose.SetModel(rig, bones.front(), pelvis);

		// Place each in-between bone at its arc position, then turn it to point at the next joint
		auto rest_arc = 0.0f;
		auto pos = joint_at(0);
		for (int k = 1; k < count - 1; ++k)
		{
			// Each bone head is placed in proportion to its rest spacing
			rest_arc += Length(head(k) - head(k - 1));
			auto next_arc = rest_arc + Length(head(k + 1) - head(k));
			pos = joint_at(rest_arc / rest_length);
			auto next = k + 1 == count - 1 ? chest.pos : joint_at(next_arc / rest_length);

			// Blend the twist between the ends, then align the bone with the curve
			auto twist = Slerp(dq0, dq1, rest_arc / rest_length);
			auto rot = twist * rig.rest_model[bones[k]].rot;
			auto current = Rotate(twist, Normalise(head(k + 1) - head(k)));
			auto wanted = Normalise(next - pos, current);
			pose.SetModel(rig, bones[k], xform(pos, quat(current, wanted) * rot));
		}

		// The chest takes its frame exactly
		pose.SetModel(rig, bones.back(), chest);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(SpineFitTests)
	{
		PRUnitTestMethod(RestTargets, Quick)
		{
			// Rest-pose end frames reproduce the rest spine
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& spine = rig.chains[0];
			FitSpine(rig, spine, pose, rig.rest_model[spine.bones.front()], rig.rest_model[spine.bones.back()]);
			for (auto b : spine.bones)
				PR_EXPECT(FEqlAbsolute(pose.model[b], rig.rest_model[b], 1e-4f));
		}
		PRUnitTestMethod(BentSpine, Quick)
		{
			// A chest turned and moved sideways bends the spine smoothly: the ends are exact and the middle bone lies between them, pointing
			// from its head towards the chest's head.
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto const& spine = rig.chains[0];
			auto pelvis = rig.rest_model[spine.bones.front()];
			auto turn = quat(v4::ZAxis(), 0.4f);
			auto chest = xform(v4(0.15f, -0.58f, 1.0f, 1), turn * rig.rest_model[spine.bones.back()].rot);
			FitSpine(rig, spine, pose, pelvis, chest);

			PR_EXPECT(FEqlAbsolute(pose.model[spine.bones.front()], pelvis, 1e-5f));
			PR_EXPECT(FEqlAbsolute(pose.model[spine.bones.back()], chest, 1e-5f));

			auto mid = pose.model[spine.bones[1]];
			PR_EXPECT(mid.pos.x > 0 && mid.pos.x < chest.pos.x);
			PR_EXPECT(mid.pos.y < pelvis.pos.y && mid.pos.y > chest.pos.y);
			auto mid_dir = Rotate(mid.rot, v4::YAxis());
			PR_EXPECT(FEqlAbsolute(mid_dir, Normalise(chest.pos - mid.pos), 1e-4f));
		}
	};
}
#endif
