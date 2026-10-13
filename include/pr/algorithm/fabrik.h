//*****************************************************************************
// FABRIK inverse kinematics
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
#pragma once
#include <span>
#include <vector>
#include "pr/math/math.h"

namespace pr::algorithm::fabrik
{
	// Notes:
	//  - FABRIK (Forward And Backward Reaching Inverse Kinematics) moves joint positions directly instead of solving for joint angles.
	//    Each iteration has two passes:
	//      - The backward pass starts at the effectors and moves each joint towards its target, then pulls each parent to the correct bone length
	//        from its child. This continues to the root.
	//      - The forward pass starts at the root and places each joint at the correct bone length from its parent. Joint limits and stiffness are
	//        applied here.
	//  - The skeleton is a tree stored as a flat array. Joint 0 is the only root, and every parent comes before its children. A chain is a tree with
	//    one child per joint.
	//  - Where branches meet (for example, a spine with four limbs), each child chain proposes a position for the shared parent. The parent moves
	//    to the weighted mean of those proposals, weighted by the total effector weight below each child.
	//  - Joints with no effector below them are not pulled in the backward pass. The forward pass drags them behind their moved parents, like a rope,
	//    unless stiffness returns them towards their rest direction.
	//  - Rest directions, swing axes and limits are all given in rest-pose model space. During a solve, each bone carries a frame: the rotation from
	//    rest-pose model space to its current world orientation. A bone's frame is its parent's frame followed by the shortest-arc rotation from its
	//    rotated rest direction to its current direction. So limits follow the chain as it bends, and the solver never adds twist about a bone.
	//    Only the direction of a bone is solved, so only its swing can be limited (see 'SwingLimit').
	//  - Stiffness and limits change the forward pass result, so a target can be missed even when it is reachable. Stiffness is a soft preference
	//    for the rest shape, not an exact constraint.
	//  - The input positions are the starting point, so passing the previous frame's result gives temporally stable results.

	// The allowed swing of a bone away from its rest direction.
	//  - A swing is a rotation about an axis perpendicular to the bone. It is measured as a swing vector: the rotation axis scaled by the rotation angle
	//    (radians). The vector has two components, 'x' and 'z', along two perpendicular swing axes that are both perpendicular to the bone.
	//  - Each component has a range [min, max]. The allowed region is the ellipse whose half-axes are the range ends, with a different half-axis on each
	//    side of zero. So a range that is the same on both axes is a cone, and a range of zero width on one axis is a hinge about the other axis.
	//  - Every range must lie in [-pi, pi] and contain zero, so the rest direction is always allowed. The default allows any swing.
	template <ScalarTypeFP S>
	struct SwingLimit
	{
		using Vec2 = math::Vec2<S>;
		static constexpr S pi = math::constants<S>::tau_by_2;

		S min_x = -pi;             // The range of the swing about the 'x' swing axis
		S max_x = +pi;
		S min_z = -pi;             // The range of the swing about the 'z' swing axis
		S max_z = +pi;

		// True if the limit allows any swing. A swing angle is never more than pi, so the full range cannot restrict it.
		bool IsFree() const
		{
			return min_x <= -pi && max_x >= pi && min_z <= -pi && max_z >= pi;
		}

		// True if the ranges are within [-pi, pi] and contain zero
		bool IsValid() const
		{
			return -pi <= min_x && min_x <= S(0) && S(0) <= max_x && max_x <= pi && -pi <= min_z && min_z <= S(0) && S(0) <= max_z && max_z <= pi;
		}

		// Return an allowed swing vector near 'swing'. Swings that are already allowed are returned unchanged.
		Vec2 Clamp(Vec2 swing) const
		{
			// Clamp each component to its range first. This sets a component with a zero-width range to zero, so a hinge stays in its plane.
			auto x = math::Clamp(swing.x, min_x, max_x);
			auto z = math::Clamp(swing.y, min_z, max_z);

			// Then scale towards zero onto the ellipse of the quadrant that the swing is in. A zero half-axis adds nothing because its component is zero.
			auto rx = x >= S(0) ? max_x : -min_x;
			auto rz = z >= S(0) ? max_z : -min_z;
			auto e = (rx > S(0) ? Sqr(x / rx) : S(0)) + (rz > S(0) ? Sqr(z / rz) : S(0));
			if (e > S(1))
			{
				auto scale = S(1) / Sqrt(e);
				x *= scale;
				z *= scale;
			}
			return Vec2{ x, z };
		}
	};

	// One joint in the skeleton. The bone of joint 'i' runs from its parent's position to its own position.
	template <ScalarTypeFP S>
	struct Joint
	{
		using Vec4 = math::Vec4<S>;

		int parent = -1;           // The index of the parent joint, or -1 for the root (joint 0 only). Parents must come before their children.
		Vec4 rest_dir = {};        // The unit direction from the parent to this joint in the rest pose, in model space (w = 0). Ignored for the root.
		S length = S(0);           // The length of the bone from the parent to this joint. Must be positive. Ignored for the root.
		Vec4 swing_axis = {};      // The 'x' swing axis in rest-pose model space (unit, w = 0, perpendicular to 'rest_dir'). The 'z' swing axis is
		                           // 'Cross(swing_axis, rest_dir)'. Only needed if 'limit' is not free.
		SwingLimit<S> limit = {};  // The allowed swing from the rest direction under the parent's frame
		S stiffness = S(0);        // In [0,1]. The fraction of the way the bone is rotated towards its rest direction on each forward pass.
	};

	// A target position for a joint
	template <ScalarTypeFP S>
	struct Effector
	{
		using Vec4 = math::Vec4<S>;

		int joint = 0;             // The index of the joint to move to 'target'
		Vec4 target = {};          // The target position in world space (w = 1)
		S weight = S(1);           // The relative influence of this effector where branches compete. Must be positive.
	};

	// Solve options
	template <ScalarTypeFP S>
	struct Settings
	{
		int max_iterations = 10;   // The maximum number of backward/forward iterations. At least one iteration is always done.
		S tolerance = S(0.001);    // Stop when every effector is within this distance of its target
		bool fixed_root = true;    // If true, the root joint keeps its input position. Otherwise it moves with the backward pass.
	};

	// Solve outcome
	template <ScalarTypeFP S>
	struct Result
	{
		int iterations = 0;        // The number of iterations done
		S error = S(0);            // The largest distance from an effector's joint to its target
	};

	// Validate a skeleton description. Returns true if 'joints' meets the requirements documented on 'Joint'.
	template <ScalarTypeFP S>
	bool IsValid(std::span<Joint<S> const> joints)
	{
		// The first joint must be the only root
		if (joints.empty() || joints[0].parent != -1)
			return false;

		// Check each bone
		for (int i = 1, iend = static_cast<int>(joints.size()); i != iend; ++i)
		{
			// Parents come before children, bones have length, and directions are unit vectors
			auto const& joint = joints[i];
			if (joint.parent < 0 || joint.parent >= i)
				return false;
			if (!(joint.length > S(0)) || joint.rest_dir.w != S(0) || !FEql(LengthSq(joint.rest_dir), S(1)))
				return false;
			if (joint.stiffness < S(0) || joint.stiffness > S(1))
				return false;

			// Check the limit ranges, and the swing axis when the limit uses it
			if (!joint.limit.IsValid())
				return false;
			if (!joint.limit.IsFree() && (joint.swing_axis.w != S(0) || !FEql(LengthSq(joint.swing_axis), S(1)) || !FEql(Dot(joint.swing_axis, joint.rest_dir), S(0))))
				return false;
		}
		return true;
	}

	// A reusable FABRIK solver. Holds scratch buffers so that repeated solves do not allocate.
	template <ScalarTypeFP S>
	struct Solver
	{
		using Vec4 = math::Vec4<S>;
		using Quat = math::Quat<S>;

		std::vector<Vec4> m_sum;      // The weighted sum of proposed positions for each joint during the backward pass
		std::vector<S> m_weight;      // The total weight of the proposals in 'm_sum'
		std::vector<S> m_influence;   // The total effector weight in each joint's subtree, including the joint itself
		std::vector<Quat> m_frame;    // The rotation from rest-pose model space to the current world orientation of each joint's bone

		// Move 'positions' so that the effector joints reach their targets. 'positions' (w = 1) is the starting pose and receives the result.
		// 'root_orientation' is the rotation from rest-pose model space to the root's world orientation. If 'frames' is not empty, it receives
		// each joint's frame (see Notes), so a bone's world orientation is 'frames[i]' applied to its rest-pose model orientation.
		Result<S> Solve(std::span<Joint<S> const> joints, std::span<Effector<S> const> effectors, std::span<Vec4> positions, Quat root_orientation, Settings<S> const& settings, std::span<Quat> frames = {})
		{
			// Size the scratch buffers and find each joint's effector influence
			auto count = static_cast<int>(joints.size());
			pr_assert(IsValid<S>(joints) && "Invalid FABRIK skeleton");
			pr_assert(static_cast<int>(positions.size()) == count && "One position per joint is required");
			pr_assert((frames.empty() || static_cast<int>(frames.size()) == count) && "One frame per joint is required");
			pr_assert(settings.max_iterations >= 1 && "At least one iteration is required");
			m_sum.resize(count);
			m_weight.resize(count);
			m_influence.assign(count, S(0));
			m_frame.resize(count);
			for (auto const& effector : effectors)
			{
				// Effector weights accumulate up the tree so that each joint knows the weight of the effectors below it
				pr_assert(effector.joint >= 0 && effector.joint < count && effector.weight > S(0) && "Invalid effector");
				m_influence[effector.joint] += effector.weight;
			}
			for (int i = count - 1; i > 0; --i)
				m_influence[joints[i].parent] += m_influence[i];

			// Iterate until every effector is close enough or the iteration budget is used up
			auto root_position = positions[0];
			Result<S> result = {};
			for (;;)
			{
				// One backward/forward iteration
				BackwardPass(joints, effectors, positions);
				if (settings.fixed_root)
					positions[0] = root_position;

				ForwardPass(joints, positions, root_orientation);
				result.iterations++;
				result.error = Error(effectors, positions);
				if (result.error <= settings.tolerance || result.iterations == settings.max_iterations)
					break;
			}

			// Output the bone frames
			if (!frames.empty())
				std::copy(m_frame.begin(), m_frame.end(), frames.begin());

			return result;
		}

	private:

		// Move joints towards their effector targets, from the leaves to the root
		void BackwardPass(std::span<Joint<S> const> joints, std::span<Effector<S> const> effectors, std::span<Vec4> positions)
		{
			// Each effector proposes its target as its joint's position
			std::fill(m_sum.begin(), m_sum.end(), Vec4{});
			std::fill(m_weight.begin(), m_weight.end(), S(0));
			for (auto const& effector : effectors)
			{
				m_sum[effector.joint] += effector.target.w0() * effector.weight;
				m_weight[effector.joint] += effector.weight;
			}

			// Children come after their parents, so a reverse sweep finishes every child before its parent
			for (int i = static_cast<int>(joints.size()) - 1; i > 0; --i)
			{
				// Joints without effectors below them are left for the forward pass to drag
				if (m_influence[i] == S(0))
					continue;

				// Move the joint to the weighted mean of its proposals
				auto const& joint = joints[i];
				pr_assert(m_weight[i] > S(0));
				positions[i] = (m_sum[i] / m_weight[i]).w1();

				// Propose a parent position one bone length back towards the parent's current position.
				// If the joints coincide, use the rest direction so that the proposal is still well defined.
				auto dir = Normalise(positions[joint.parent] - positions[i], -joint.rest_dir);
				auto proposal = positions[i] + dir * joint.length;
				m_sum[joint.parent] += proposal.w0() * m_influence[i];
				m_weight[joint.parent] += m_influence[i];
			}

			// The root moves to its proposals as well. The caller restores it if it is fixed.
			if (m_weight[0] > S(0))
				positions[0] = (m_sum[0] / m_weight[0]).w1();
		}

		// Place each joint one bone length from its parent, applying stiffness and limits, from the root to the leaves
		void ForwardPass(std::span<Joint<S> const> joints, std::span<Vec4> positions, Quat root_orientation)
		{
			m_frame[0] = root_orientation;
			for (int i = 1, iend = static_cast<int>(joints.size()); i != iend; ++i)
			{
				// Find the bone's rest direction under the parent's current frame, and its current direction.
				// If the joints coincide, the bone takes its rest direction.
				auto const& joint = joints[i];
				auto const& parent_frame = m_frame[joint.parent];
				auto rest = Rotate(parent_frame, joint.rest_dir);
				auto dir = Normalise(positions[i] - positions[joint.parent], rest);

				// Turn part of the way back towards the rest direction
				if (joint.stiffness != S(0))
					dir = Rotate(Slerp(Identity<Quat>(), Quat(dir, rest), joint.stiffness), dir);

				// Keep the direction within the joint's limits
				dir = ApplyLimit(joint, parent_frame, rest, dir);

				// Place the joint and record its bone's frame
				positions[i] = positions[joint.parent] + dir * joint.length;
				m_frame[i] = Normalise(Quat(rest, dir) * parent_frame);
			}
		}

		// Return 'dir' moved to the nearest direction allowed by the joint's limit. 'rest' is the rest direction under the parent's frame.
		static Vec4 ApplyLimit(Joint<S> const& joint, Quat parent_frame, Vec4 rest, Vec4 dir)
		{
			// Any direction is allowed by a free limit
			if (joint.limit.IsFree())
				return dir;

			// Find the swing axes under the parent's frame. 'z' completes a right-handed frame with the bone as 'y'.
			auto x_axis = Rotate(parent_frame, joint.swing_axis);
			auto z_axis = Cross(x_axis, rest);

			// Measure the shortest-arc swing from 'rest' to 'dir' as a swing vector in the (x, z) plane.
			// When the directions are opposite, any axis perpendicular to the rest direction works.
			auto angle = Acos(Clamp(Dot(rest, dir), S(-1), S(1)));
			auto axis = Normalise(Cross(rest, dir), Perpendicular(rest));
			auto swing = math::Vec2<S>{ Dot(axis, x_axis) * angle, Dot(axis, z_axis) * angle };

			// Clamp the swing, then rebuild the direction from it. A zero swing is the rest direction.
			auto clamped = joint.limit.Clamp(swing);
			if (clamped.x == swing.x && clamped.y == swing.y)
				return dir;

			auto clamped_angle = Length(clamped);
			if (clamped_angle == S(0))
				return rest;

			auto clamped_axis = (x_axis * clamped.x + z_axis * clamped.y) / clamped_angle;
			return Rotate(Quat(clamped_axis, clamped_angle), rest);
		}

		// The largest distance from an effector's joint to its target
		static S Error(std::span<Effector<S> const> effectors, std::span<Vec4 const> positions)
		{
			auto error = S(0);
			for (auto const& effector : effectors)
				error = std::max(error, Length(positions[effector.joint] - effector.target));

			return error;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::algorithm::tests
{
	PRUnitTestClass(FabrikTests)
	{
		using S = double;
		using Vec4 = math::Vec4<S>;
		using Quat = math::Quat<S>;
		using Joint = fabrik::Joint<S>;
		using Effector = fabrik::Effector<S>;
		using Settings = fabrik::Settings<S>;

		// A straight chain of 'count' joints along +X with unit bones, rooted at the origin
		static std::vector<Joint> Chain(int count)
		{
			std::vector<Joint> joints(count);
			for (int i = 1; i != count; ++i)
				joints[i] = Joint{ .parent = i - 1, .rest_dir = Vec4::XAxis(), .length = S(1) };

			return joints;
		}

		// The rest positions of 'joints', with the root at the origin
		static std::vector<Vec4> RestPositions(std::span<Joint const> joints)
		{
			std::vector<Vec4> positions(joints.size(), Vec4::Origin());
			for (int i = 1, iend = static_cast<int>(joints.size()); i != iend; ++i)
				positions[i] = positions[joints[i].parent] + joints[i].rest_dir * joints[i].length;

			return positions;
		}

		// True if every bone has its required length
		static bool LengthsPreserved(std::span<Joint const> joints, std::span<Vec4 const> positions)
		{
			for (int i = 1, iend = static_cast<int>(joints.size()); i != iend; ++i)
			{
				if (!FEqlAbsolute(Length(positions[i] - positions[joints[i].parent]), joints[i].length, S(1e-9)))
					return false;
			}
			return true;
		}

		PRUnitTestMethod(ReachableTarget, Quick)
		{
			// A three-bone chain reaches a target inside its range, keeping its root and bone lengths
			auto joints = Chain(4);
			auto positions = RestPositions(joints);
			Effector effectors[] = { {.joint = 3, .target = Vec4{1.5, 1.5, 0.5, 1}} };
			fabrik::Solver<S> solver;
			auto result = solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50, .tolerance = 1e-6 });
			PR_EXPECT(result.error <= 1e-6);
			PR_EXPECT(FEql(positions[0], Vec4::Origin()));
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(UnreachableTarget, Quick)
		{
			// A target beyond reach leaves the chain fully extended and pointing at the target
			auto joints = Chain(4);
			auto positions = RestPositions(joints);
			auto target = Vec4{0, 10, 0, 1};
			Effector effectors[] = { {.joint = 3, .target = target} };
			fabrik::Solver<S> solver;
			auto result = solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50 });
			PR_EXPECT(FEqlAbsolute(result.error, S(7), S(1e-6)));
			PR_EXPECT(FEqlAbsolute(positions[3], Vec4{0, 3, 0, 1}, S(1e-6)));
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(FreeRoot, Quick)
		{
			// With a free root, the whole chain moves to put its end on the target
			auto joints = Chain(3);
			auto positions = RestPositions(joints);
			auto target = Vec4{10, 0, 0, 1};
			Effector effectors[] = { {.joint = 2, .target = target} };
			fabrik::Solver<S> solver;
			auto result = solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50, .tolerance = 1e-6, .fixed_root = false });
			PR_EXPECT(result.error <= 1e-6);
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(TreeWithTwoEffectors, Quick)
		{
			// A 'Y' shaped tree: a trunk with two branches. Both branch tips reach reachable targets.
			std::vector<Joint> joints = {
				Joint{},
				Joint{ .parent = 0, .rest_dir = Vec4::YAxis(), .length = 1 },
				Joint{ .parent = 1, .rest_dir = Normalise(Vec4{-1, 1, 0, 0}), .length = 1 },
				Joint{ .parent = 1, .rest_dir = Normalise(Vec4{+1, 1, 0, 0}), .length = 1 },
			};
			auto positions = RestPositions(joints);
			Effector effectors[] = {
				{.joint = 2, .target = Vec4{-0.5, 1.6, 0.4, 1}},
				{.joint = 3, .target = Vec4{+0.9, 1.2, 0.3, 1}},
			};
			fabrik::Solver<S> solver;
			auto result = solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 200, .tolerance = 1e-5 });
			PR_EXPECT(result.error <= 1e-5);
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(SwingClamp, Quick)
		{
			// Allowed swings are unchanged, a zero-width range flattens its component, and other swings scale onto their quadrant's ellipse
			auto limit = fabrik::SwingLimit<S>{ .min_x = -0.5, .max_x = 1.0, .min_z = 0.0, .max_z = 0.0 };
			PR_EXPECT(limit.IsValid() && !limit.IsFree());
			PR_EXPECT(FEql(limit.Clamp(math::Vec2<S>{ 0.7, 0.0 }), math::Vec2<S>{ 0.7, 0.0 }));
			PR_EXPECT(FEql(limit.Clamp(math::Vec2<S>{ -0.9, 0.3 }), math::Vec2<S>{ -0.5, 0.0 }));

			auto ellipse = fabrik::SwingLimit<S>{ .min_x = -0.2, .max_x = 0.4, .min_z = -0.6, .max_z = 0.8 };
			auto clamped = ellipse.Clamp(math::Vec2<S>{ 0.4, -0.6 });
			PR_EXPECT(FEql(Sqr(clamped.x / 0.4) + Sqr(clamped.y / 0.6), S(1)));
			PR_EXPECT(FEql(clamped.x * 0.6, -clamped.y * 0.4));
			PR_EXPECT(fabrik::SwingLimit<S>{}.IsFree());
			PR_EXPECT(!fabrik::SwingLimit<S>{ .min_x = 0.1 }.IsValid());
		}
		PRUnitTestMethod(ConeLimit, Quick)
		{
			// A symmetric swing limit is a cone. The bone cannot bend further than its half-angle from the parent bone's direction.
			auto joints = Chain(3);
			auto half_angle = math::DegreesToRadians(S(30));
			joints[2].swing_axis = Vec4::YAxis();
			joints[2].limit = fabrik::SwingLimit<S>{ .min_x = -half_angle, .max_x = half_angle, .min_z = -half_angle, .max_z = half_angle };
			auto positions = RestPositions(joints);
			Effector effectors[] = { {.joint = 2, .target = Vec4{0, 1, 0, 1}} };
			fabrik::Solver<S> solver;
			solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50 });
			auto bone1 = Normalise(positions[1] - positions[0]);
			auto bone2 = Normalise(positions[2] - positions[1]);
			PR_EXPECT(Acos(Clamp(Dot(bone1, bone2), S(-1), S(1))) <= half_angle + 1e-9);
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(HingeLimit, Quick)
		{
			// A swing range of zero width on 'z' is a hinge about the 'x' swing axis. The bone stays in the plane perpendicular to that axis
			// and within its angle range, even when the target is out of that plane.
			auto joints = Chain(3);
			joints[2].swing_axis = Vec4::ZAxis();
			joints[2].limit = fabrik::SwingLimit<S>{ .min_x = 0, .max_x = math::DegreesToRadians(S(90)), .min_z = 0, .max_z = 0 };
			auto positions = RestPositions(joints);
			Effector effectors[] = { {.joint = 2, .target = Vec4{1, -0.5, 0.8, 1}} };
			fabrik::Solver<S> solver;
			std::vector<Quat> frames(joints.size());
			solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50 }, frames);

			// Measure the second bone's hinge angle in the first bone's frame
			auto axis = Rotate(frames[1], joints[2].swing_axis);
			auto bone1 = Normalise(positions[1] - positions[0]);
			auto bone2 = Normalise(positions[2] - positions[1]);
			auto angle = Atan2(Dot(Cross(bone1, bone2), axis), Dot(bone1, bone2));
			PR_EXPECT(FEqlAbsolute(Dot(bone2, axis), S(0), S(1e-9)));
			PR_EXPECT(angle >= joints[2].limit.min_x - 1e-9 && angle <= joints[2].limit.max_x + 1e-9);
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(EllipticalSwingLimit, Quick)
		{
			// An asymmetric swing limit allows more bend about one axis than the other
			auto joints = Chain(3);
			joints[2].swing_axis = Vec4::ZAxis();
			joints[2].limit = fabrik::SwingLimit<S>{ .min_x = -0.2, .max_x = 1.0, .min_z = -0.3, .max_z = 0.3 };
			auto positions = RestPositions(joints);
			Effector effectors[] = { {.joint = 2, .target = Vec4{1, 0.8, -0.9, 1}} };
			fabrik::Solver<S> solver;
			std::vector<Quat> frames(joints.size());
			solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50 }, frames);

			// Measure the final swing vector in the parent's frame. It must be inside its quadrant's ellipse.
			auto rest = Normalise(positions[1] - positions[0]);
			auto dir = Normalise(positions[2] - positions[1]);
			auto x_axis = Rotate(frames[1], joints[2].swing_axis);
			auto z_axis = Cross(x_axis, rest);
			auto angle = Acos(Clamp(Dot(rest, dir), S(-1), S(1)));
			auto axis = Normalise(Cross(rest, dir), x_axis);
			auto swing = math::Vec2<S>{ Dot(axis, x_axis) * angle, Dot(axis, z_axis) * angle };
			auto rx = swing.x >= 0 ? joints[2].limit.max_x : -joints[2].limit.min_x;
			auto rz = swing.y >= 0 ? joints[2].limit.max_z : -joints[2].limit.min_z;
			PR_EXPECT(Sqr(swing.x / rx) + Sqr(swing.y / rz) <= S(1) + 1e-9);
			PR_EXPECT(LengthsPreserved(joints, positions));
		}
		PRUnitTestMethod(StiffnessStraightens, Quick)
		{
			// Full stiffness returns a chain without effectors to its rest shape under the root's orientation
			auto joints = Chain(5);
			for (auto& joint : joints)
				joint.stiffness = S(1);

			std::vector<Vec4> positions = { Vec4{0, 0, 0, 1}, Vec4{1, 1, 0, 1}, Vec4{1, 2, 1, 1}, Vec4{2, 2, 0, 1}, Vec4{2, 3, 2, 1} };
			auto root_orientation = Quat(Vec4::ZAxis(), math::DegreesToRadians(S(90)));
			fabrik::Solver<S> solver;
			solver.Solve(joints, {}, positions, root_orientation, Settings{});
			for (int i = 0; i != 5; ++i)
				PR_EXPECT(FEqlAbsolute(positions[i], Vec4{0, S(i), 0, 1}, S(1e-9)));
		}
		PRUnitTestMethod(FramesFollowTheChain, Quick)
		{
			// Each frame rotates the bone's rest direction onto its current direction
			auto joints = Chain(4);
			auto positions = RestPositions(joints);
			Effector effectors[] = { {.joint = 3, .target = Vec4{1, 2, -1, 1}} };
			fabrik::Solver<S> solver;
			std::vector<Quat> frames(joints.size());
			solver.Solve(joints, effectors, positions, Identity<Quat>(), Settings{ .max_iterations = 50 }, frames);
			for (int i = 1; i != 4; ++i)
				PR_EXPECT(FEqlAbsolute(Rotate(frames[i], joints[i].rest_dir), Normalise(positions[i] - positions[i - 1]), S(1e-9)));
		}
	};
}
#endif
