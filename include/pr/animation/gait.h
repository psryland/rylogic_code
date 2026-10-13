//*****************************************************************************
// Procedural animation - gait description
//  Copyright (c) Rylogic Ltd 2026
//*****************************************************************************
// A gait description gives the stepping pattern of a character's walking limbs. The locomotion controller (see locomotion.h) blends from a
// walk at low speed to a trot at high speed. Lengths are in model units, speeds in model units per second, and times in seconds.
//
// A '.gait' file is a JSON gait description:
//
//  {
//    "schema_version": 1,
//    "limbs": [{
//      "role": "leg" | "arm",            // the limb is the rig chain with this role and side
//      "side": "left" | "right",
//      "walk_phase": 0.0,                // in [0,1]. The fraction of the step cycle at which the foot touches down, in the walk.
//      "trot_phase": 0.0                 // in [0,1]. As 'walk_phase', in the trot.
//    }],
//    "walk_speed": 0.9,
//    "trot_speed": 1.4,
//    "stand_speed": 0.09,
//    "walk_duty": 0.75,
//    "trot_duty": 0.5,
//    "stride_min": 0.1,
//    "stride_per_speed": 0.2,
//    "max_stance_travel": 0.2,
//    "swing_height": 0.05,
//    "settle_distance": 0.03,
//    "body_response": 0.2,
//    "posture_time": 0.6,
//    "rear_delay": 1.0
//  }
#pragma once
#include "pr/animation/forward.h"
#include "pr/animation/rig.h"
#include "pr/animation/rig_json.h"

namespace pr::animation
{
	// The '.gait' schema version that this code reads and writes
	inline constexpr int GaitSchemaVersion = 1;

	// One walking limb: a Leg or Arm chain, and when its foot touches down in each gait.
	//  - Phases are fractions of the step cycle. The controller blends linearly between the walk and trot phases, so choose values that do
	//    not cross a whole cycle between the two gaits (for example, 0.75 to 1.0 rather than 0.75 to 0.0).
	struct GaitLimb
	{
		EChainRole role = EChainRole::Leg;
		ESide side = ESide::Left;
		float walk_phase = 0.0f;
		float trot_phase = 0.0f;
	};

	// How a character steps. See the file layout above for the units.
	struct GaitDesc
	{
		std::vector<GaitLimb> limbs;    // The limbs that step. Other limb chains keep their rest targets.
		float walk_speed = 1.0f;        // At and below this speed the gait is a walk
		float trot_speed = 2.0f;        // At and above this speed the gait is a trot. Must be greater than 'walk_speed'.
		float stand_speed = 0.1f;       // Below this speed the character stands, stepping only to bring its feet back under its body
		float walk_duty = 0.75f;        // In (0,1). The fraction of the cycle that each foot is on the ground, in the walk.
		float trot_duty = 0.5f;         // In (0,1). As 'walk_duty', in the trot.
		float stride_min = 0.1f;        // The distance the body moves per cycle at the slowest speeds. Must be positive.
		float stride_per_speed = 0.2f;  // The growth of the stride with speed (seconds). The stride is also limited by 'max_stance_travel'.
		float max_stance_travel = 0.2f; // The longest distance that the body may move over a planted foot. Must be positive.
		float swing_height = 0.05f;     // The height that a foot is lifted at the middle of its swing
		float settle_distance = 0.03f;  // A standing character steps any foot further than this from its neutral position
		float body_response = 0.2f;     // The time for the body to follow changes in the ground beneath its feet. Must be positive.
		float posture_time = 0.6f;      // The time to change between the standing and walking postures (see locomotion.h). Must be positive.
		float rear_delay = 1.0f;        // How long a character stands still before it returns to the standing posture. Must not be negative.
	};

	// Return the index of the contact that a limb chain stands on (a sole for legs, a palm for arms), or -1 if it has none
	inline int LimbContact(Rig const& rig, Chain const& chain)
	{
		// The contact is on the chain's end bone
		auto kind = chain.role == EChainRole::Leg ? EContactKind::Sole : EContactKind::Palm;
		for (int i = 0, iend = static_cast<int>(rig.contacts.size()); i != iend; ++i)
		{
			if (rig.contacts[i].bone == chain.bones.back() && rig.contacts[i].kind == kind)
				return i;
		}
		return -1;
	}

	// Return the index of the Leg or Arm chain of 'rig' with 'role' and 'side', or -1 if there is none
	inline int LimbChain(Rig const& rig, EChainRole role, ESide side)
	{
		// Rigs have at most one chain per role and side
		for (int c = 0, cend = static_cast<int>(rig.chains.size()); c != cend; ++c)
		{
			if (rig.chains[c].role == role && rig.chains[c].side == side)
				return c;
		}
		return -1;
	}

	// Check 'gait' against 'rig'. Returns a description of each problem, or an empty list if the gait is valid.
	inline std::vector<std::string> Validate(GaitDesc const& gait, Rig const& rig)
	{
		std::vector<std::string> errors;

		// Each limb must name a distinct limb chain that has a contact to stand on
		for (size_t i = 0; i != gait.limbs.size(); ++i)
		{
			auto const& limb = gait.limbs[i];
			if (limb.role != EChainRole::Leg && limb.role != EChainRole::Arm)
			{
				errors.push_back(std::format("Limb {} is not a leg or an arm", i));
				continue;
			}
			auto chain = LimbChain(rig, limb.role, limb.side);
			if (chain < 0)
				errors.push_back(std::format("Limb {} has no matching chain in the rig", i));
			else if (LimbContact(rig, rig.chains[chain]) < 0)
				errors.push_back(std::format("Limb {} has no sole or palm contact on its end bone", i));

			if (!(limb.walk_phase >= 0 && limb.walk_phase <= 1) || !(limb.trot_phase >= 0 && limb.trot_phase <= 1))
				errors.push_back(std::format("Limb {} has a phase outside [0,1]", i));

			for (size_t j = 0; j != i; ++j)
			{
				if (gait.limbs[j].role == limb.role && gait.limbs[j].side == limb.side)
					errors.push_back(std::format("Limb {} repeats limb {}", i, j));
			}
		}

		// The speeds must be ordered and the cycle parameters in range
		if (!(gait.stand_speed >= 0 && gait.stand_speed < gait.walk_speed && gait.walk_speed < gait.trot_speed))
			errors.push_back("Speeds must satisfy 0 <= stand_speed < walk_speed < trot_speed");
		if (!(gait.walk_duty > 0 && gait.walk_duty < 1) || !(gait.trot_duty > 0 && gait.trot_duty < 1))
			errors.push_back("Duty factors must be in (0,1)");
		if (!(gait.stride_min > 0) || !(gait.stride_per_speed >= 0) || !(gait.max_stance_travel > 0))
			errors.push_back("Stride parameters must be positive");
		if (!(gait.swing_height >= 0) || !(gait.settle_distance >= 0) || !(gait.body_response > 0))
			errors.push_back("Swing height and settle distance must not be negative, and the body response must be positive");
		if (!(gait.posture_time > 0) || !(gait.rear_delay >= 0))
			errors.push_back("The posture time must be positive and the rear delay must not be negative");

		return errors;
	}

	// Return a gait for 'rig' derived from its proportions: a lateral-sequence walk blending to a trot. Every Leg and Arm chain with a
	// left or right side and a sole or palm contact is a walking limb. Speeds assume that model units are metres.
	inline GaitDesc DefaultGait(Rig const& rig)
	{
		GaitDesc gait;

		// Hind limbs are legs and fore limbs are arms. A lateral-sequence walk steps left hind, left fore, right hind, right fore, and the trot
		// moves diagonal pairs together. With no arms, the legs alternate.
		auto has_arms = false;
		for (auto const& chain : rig.chains)
			has_arms |= chain.role == EChainRole::Arm && chain.side != ESide::Centre && LimbContact(rig, chain) >= 0;

		auto add = [&](EChainRole role, ESide side, float walk_phase, float trot_phase)
		{
			// Only limbs that the rig has, and that can stand, step
			auto chain = LimbChain(rig, role, side);
			if (chain >= 0 && LimbContact(rig, rig.chains[chain]) >= 0)
				gait.limbs.push_back(GaitLimb{ .role = role, .side = side, .walk_phase = walk_phase, .trot_phase = trot_phase });
		};
		if (has_arms)
		{
			add(EChainRole::Leg, ESide::Left, 0.0f, 0.0f);
			add(EChainRole::Arm, ESide::Left, 0.25f, 0.5f);
			add(EChainRole::Leg, ESide::Right, 0.5f, 0.5f);
			add(EChainRole::Arm, ESide::Right, 0.75f, 1.0f);
		}
		else
		{
			add(EChainRole::Leg, ESide::Left, 0.0f, 0.0f);
			add(EChainRole::Leg, ESide::Right, 0.5f, 0.5f);
		}

		// Measure the limbs: the mean height of their roots above their contacts, and the shortest distance that an ankle can move forward or
		// back from its rest position before the limb is straight. Sprawled or straight rest poses reach little, so the reach has a floor.
		auto hip_height = 0.0f;
		auto reach = std::numeric_limits<float>::max();
		for (auto const& limb : gait.limbs)
		{
			auto const& chain = rig.chains[LimbChain(rig, limb.role, limb.side)];
			auto const& contact = rig.contacts[LimbContact(rig, chain)];
			auto root = rig.rest_model[chain.bones[0]].pos;
			auto ankle = rig.rest_model[chain.bones[2]].pos;
			auto length = rig.bones[chain.bones[0]].length + rig.bones[chain.bones[1]].length;
			hip_height += root.z - (rig.rest_model[contact.bone] * contact.offset).z;
			reach = std::min(reach, std::max(Sqrt(std::max(Sqr(length) - LengthSq(ankle - root), 0.0f)), 0.25f * length));
		}
		if (gait.limbs.empty())
			return gait;

		hip_height /= static_cast<float>(gait.limbs.size());

		// Animals change gait at similar values of speed squared over (gravity * hip height). The walk stride uses most of the reach.
		constexpr float gravity = 9.81f;
		gait.walk_speed = Sqrt(0.25f * gravity * hip_height);
		gait.trot_speed = Sqrt(0.6f * gravity * hip_height);
		gait.stand_speed = 0.1f * gait.walk_speed;
		gait.walk_duty = 0.75f;
		gait.trot_duty = 0.5f;
		gait.max_stance_travel = 1.6f * reach;
		auto walk_stride = 0.8f * gait.max_stance_travel / gait.walk_duty;
		gait.stride_min = 0.4f * walk_stride;
		gait.stride_per_speed = (walk_stride - gait.stride_min) / gait.walk_speed;
		gait.swing_height = 0.15f * hip_height;
		gait.settle_distance = 0.25f * gait.stride_min;
		gait.body_response = 0.2f;
		gait.posture_time = 0.6f;
		gait.rear_delay = 1.0f;
		return gait;
	}

	// Read a gait description from a parsed '.gait' document. Throws 'std::runtime_error' if the document does not follow the schema.
	// The result is not checked against a rig; call 'Validate'.
	inline GaitDesc ReadGaitDesc(json::Value const& doc)
	{
		using namespace rig_json;
		GaitDesc gait;

		// Check the schema version before reading anything else
		auto const& root = ReadObject(doc, "gait");
		auto version = static_cast<int>(ReadFloat(Member(root, "schema_version", "gait"), "gait.schema_version"));
		if (version != GaitSchemaVersion)
			throw std::runtime_error(std::format("gait.schema_version: unsupported version {} (expected {})", version, GaitSchemaVersion));

		// Read the limbs
		auto const& limbs = ReadArray(Member(root, "limbs", "gait"), "gait.limbs");
		for (size_t i = 0; i != limbs.size(); ++i)
		{
			auto context = std::format("gait.limbs[{}]", i);
			auto const& obj = ReadObject(limbs[i], context);
			gait.limbs.push_back(GaitLimb{
				.role = ParseEnum<EChainRole>(ReadString(Member(obj, "role", context), context + ".role"), ChainRoleNames, context + ".role"),
				.side = ParseEnum<ESide>(ReadString(Member(obj, "side", context), context + ".side"), SideNames, context + ".side"),
				.walk_phase = ReadFloat(Member(obj, "walk_phase", context), context + ".walk_phase"),
				.trot_phase = ReadFloat(Member(obj, "trot_phase", context), context + ".trot_phase"),
			});
		}

		// Read the cycle parameters, which are all required
		auto read = [&](std::string_view key)
		{
			return ReadFloat(Member(root, key, "gait"), std::format("gait.{}", key));
		};
		gait.walk_speed = read("walk_speed");
		gait.trot_speed = read("trot_speed");
		gait.stand_speed = read("stand_speed");
		gait.walk_duty = read("walk_duty");
		gait.trot_duty = read("trot_duty");
		gait.stride_min = read("stride_min");
		gait.stride_per_speed = read("stride_per_speed");
		gait.max_stance_travel = read("max_stance_travel");
		gait.swing_height = read("swing_height");
		gait.settle_distance = read("settle_distance");
		gait.body_response = read("body_response");
		gait.posture_time = read("posture_time");
		gait.rear_delay = read("rear_delay");
		return gait;
	}

	// Write a gait description as a '.gait' document
	inline json::Document WriteGaitDesc(GaitDesc const& gait)
	{
		using namespace rig_json;
		json::Document doc;
		auto& root = doc.root();
		root["schema_version"] = GaitSchemaVersion;

		// Write the limbs
		json::Array limbs;
		for (auto const& limb : gait.limbs)
		{
			limbs.push_back(json::Object{
				{ "role", EnumName(limb.role, ChainRoleNames) },
				{ "side", EnumName(limb.side, SideNames) },
				{ "walk_phase", limb.walk_phase },
				{ "trot_phase", limb.trot_phase },
			});
		}
		root["limbs"] = std::move(limbs);

		// Write the cycle parameters
		root["walk_speed"] = gait.walk_speed;
		root["trot_speed"] = gait.trot_speed;
		root["stand_speed"] = gait.stand_speed;
		root["walk_duty"] = gait.walk_duty;
		root["trot_duty"] = gait.trot_duty;
		root["stride_min"] = gait.stride_min;
		root["stride_per_speed"] = gait.stride_per_speed;
		root["max_stance_travel"] = gait.max_stance_travel;
		root["swing_height"] = gait.swing_height;
		root["settle_distance"] = gait.settle_distance;
		root["body_response"] = gait.body_response;
		root["posture_time"] = gait.posture_time;
		root["rear_delay"] = gait.rear_delay;
		return doc;
	}

	// Read and write '.gait' files
	inline GaitDesc LoadGaitDesc(std::filesystem::path const& filepath)
	{
		// Schema errors name the file so that cook errors are actionable
		try
		{
			return ReadGaitDesc(json::Read(filepath));
		}
		catch (std::exception const& ex)
		{
			throw std::runtime_error(std::format("{}: {}", filepath.string(), ex.what()));
		}
	}
	inline void SaveGaitDesc(std::filesystem::path const& filepath, GaitDesc const& gait)
	{
		// Indented output keeps cooked gaits readable and diffable
		json::Write(filepath, WriteGaitDesc(gait));
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::animation::tests
{
	PRUnitTestClass(AnimationGaitTests)
	{
		PRUnitTestMethod(DefaultsFromRig, Quick)
		{
			// The test rig has one leg and one arm, both on the left, which step half a cycle apart in the walk
			auto rig = TestRig();
			auto gait = DefaultGait(rig);
			PR_EXPECT(Validate(gait, rig).empty());
			PR_EXPECT(gait.limbs.size() == 2);
			PR_EXPECT(gait.limbs[0].role == EChainRole::Leg && gait.limbs[1].role == EChainRole::Arm);
			PR_EXPECT(FEql(gait.walk_speed, Sqrt(0.25f * 9.81f * 1.0f)));
			PR_EXPECT(gait.max_stance_travel / gait.walk_duty > gait.stride_min + gait.stride_per_speed * gait.walk_speed - 1e-5f);
		}
		PRUnitTestMethod(RoundTrip, Quick)
		{
			// Writing then reading a gait reproduces it
			auto rig = TestRig();
			auto gait = DefaultGait(rig);
			gait.swing_height = 0.123f;
			auto text = json::Write(WriteGaitDesc(gait));
			auto read = ReadGaitDesc(json::Read(std::string_view(text)));
			PR_EXPECT(Validate(read, rig).empty());
			PR_EXPECT(read.limbs.size() == gait.limbs.size());
			for (size_t i = 0; i != gait.limbs.size(); ++i)
			{
				auto const& l = read.limbs[i];
				auto const& r = gait.limbs[i];
				PR_EXPECT(l.role == r.role && l.side == r.side && l.walk_phase == r.walk_phase && l.trot_phase == r.trot_phase);
			}
			PR_EXPECT(read.walk_speed == gait.walk_speed && read.trot_speed == gait.trot_speed && read.stand_speed == gait.stand_speed);
			PR_EXPECT(read.walk_duty == gait.walk_duty && read.trot_duty == gait.trot_duty);
			PR_EXPECT(read.stride_min == gait.stride_min && read.stride_per_speed == gait.stride_per_speed && read.max_stance_travel == gait.max_stance_travel);
			PR_EXPECT(read.swing_height == gait.swing_height && read.settle_distance == gait.settle_distance && read.body_response == gait.body_response);
						PR_EXPECT(read.posture_time == gait.posture_time && read.rear_delay == gait.rear_delay);
					}
		PRUnitTestMethod(Errors, Quick)
		{
			// Schema errors name the field, and gaits that do not fit the rig are reported
			auto rig = TestRig();
			auto text = json::Write(WriteGaitDesc(DefaultGait(rig)));
			auto bad = text;
			bad.replace(bad.find("\"leg\""), 5, "\"wing\"");
			try
			{
				(void)ReadGaitDesc(json::Read(std::string_view(bad)));
				PR_EXPECT(false && "Expected a schema error");
			}
			catch (std::runtime_error const& ex)
			{
				PR_EXPECT(std::string_view(ex.what()).find("gait.limbs[0].role") != std::string_view::npos);
			}

			auto gait = DefaultGait(rig);
			gait.limbs.push_back(GaitLimb{ .role = EChainRole::Leg, .side = ESide::Right });
			gait.trot_speed = gait.walk_speed;
			PR_EXPECT(Validate(gait, rig).size() == 2);
		}
	};
}
#endif
