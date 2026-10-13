//*****************************************************************************
// Procedural animation - tail solver
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/pose.h"

namespace pr::animation
{
	// Poses Tail chains with FABRIK. Holds scratch buffers so that repeated solves do not allocate.
	//  - Tail joint 0 is the head of the chain's first bone and follows the chain's parent. Joint 'k' (k >= 1) is the end of chain bone 'k - 1'
	//    (see 'TailJoints').
	//  - Each solve starts from the chain's current model-space positions, so solving every frame from the previous result is temporally stable.
	struct TailSolver
	{
		fabrik::Solver<float> m_solver;
		std::vector<v4> m_positions;
		std::vector<quat> m_frames;

		// Pose tail chain 'chain_index' so that its joints move towards 'effectors' (model-space targets, by tail joint index).
		//  - The model transforms of the chain and its ancestors must be current. The chain's descendants are not updated.
		//  - With no effectors, the chain is dragged behind its parent and returns towards its rest shape by each bone's stiffness.
		fabrik::Result<float> Solve(Rig const& rig, int chain_index, Pose& pose, std::span<fabrik::Effector<float> const> effectors, fabrik::Settings<float> const& settings)
		{
			// Start from the chain's current joint positions, with the base joint attached to the parent
			auto const& chain = rig.chains[chain_index];
			auto const& joints = rig.tails[chain_index];
			pr_assert(chain.role == EChainRole::Tail && "Not a tail chain");
			auto count = static_cast<int>(chain.bones.size());
			auto base = chain.bones.front();
			auto parent = rig.bones[base].parent;
			m_positions.resize(count + 1);
			m_frames.resize(count + 1);
			m_positions[0] = (parent < 0 ? pose.local[base] : pose.model[parent] * pose.local[base]).pos;
			for (int k = 1; k != count; ++k)
				m_positions[k] = pose.model[chain.bones[k]].pos;

			m_positions[count] = pose.Tip(rig, chain.bones.back());

			// The base joint's frame is the parent's rotation from rest
			auto root_orientation = parent < 0 ? Identity<quat>() : pose.model[parent].rot * ~rig.rest_model[parent].rot;
			auto result = m_solver.Solve(joints, effectors, m_positions, root_orientation, settings, m_frames);

			// Each bone starts at its head joint and takes the frame of the joint at its tip
			for (int k = 0; k != count; ++k)
			{
				auto b = chain.bones[k];
				pose.SetModel(rig, b, xform(m_positions[k], Normalise(m_frames[k + 1] * rig.rest_model[b].rot)));
			}
			return result;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(TailSolverTests)
	{
		PRUnitTestMethod(NoEffectorsAtRest, Quick)
		{
			// A rest-pose tail with no effectors stays at rest
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto solver = TailSolver{};
			solver.Solve(rig, 4, pose, {}, fabrik::Settings<float>{});
			for (auto b : rig.chains[4].bones)
				PR_EXPECT(FEqlAbsolute(pose.model[b].pos, rig.rest_model[b].pos, 1e-5f) && FEqlOrientation(pose.model[b].rot, rig.rest_model[b].rot, 1e-4f));
		}
		PRUnitTestMethod(TipReachesTarget, Quick)
		{
			// The tail tip reaches a target, and each bone's tip meets the next bone's head
			auto rig = TestRig();
			auto pose = Pose(rig);
			auto solver = TailSolver{};
			auto const& tail = rig.chains[4];
			fabrik::Effector<float> const effectors[] = { {.joint = 3, .target = v4(0.3f, 0.5f, 0.5f, 1)} };
			auto result = solver.Solve(rig, 4, pose, effectors, fabrik::Settings<float>{ .max_iterations = 50, .tolerance = 1e-5f });
			pose.UpdateModel(rig);

			PR_EXPECT(result.error < 1e-3f);
			PR_EXPECT(FEqlAbsolute(pose.Tip(rig, tail.bones.back()), effectors[0].target, 1e-3f));
			for (int k = 0; k + 1 != static_cast<int>(tail.bones.size()); ++k)
				PR_EXPECT(FEqlAbsolute(pose.Tip(rig, tail.bones[k]), pose.model[tail.bones[k + 1]].pos, 1e-4f));
		}
	};
}
#endif
