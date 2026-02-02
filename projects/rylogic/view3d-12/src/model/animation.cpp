//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	// Read a value from 'data' at 'index', or return 'def' if 'data.empty()'
	template <typename T> T Get(RangeOf<T> auto const& data, int index, T const& def)
	{
		assert(data.empty() || (index >= 0 && index < isize(data)));
		return data.empty() ? def : data[index];
	}

	// --------------------------------------------------------------------------------------------

	struct CalcDynamics
	{
		// A bitmask indicating the active channels
		enum class EDynamicsChannels : uint32_t
		{
			// 'D' == first derivative, 'DD' == second derivative
			None = 0,
			Value = 1 << 0,
			ValueD = 1 << 1,
			ValueDD = 1 << 2,
			Rotation = 1 << 3,
			RotationD = 1 << 4,
			RotationDD = 1 << 5,
			Translation = 1 << 6,
			TranslationD = 1 << 7,
			TranslationDD = 1 << 8,
			Scale = 1 << 9,
			ScaleD = 1 << 10,
			ScaleDD = 1 << 11,
			_flags_enum = 0,
		};
		struct ScalarDynamics
		{
			float value, dvalue, ddvalue;
			EDynamicsChannels active; // The values that are not equal to default values
		};
		struct TransformDynamics
		{
			struct Rot { quat value; v3 dvalue; v3 ddvalue; } rot;
			struct Pos { v3 value; v3 dvalue; v3 ddvalue; } pos;
			struct Scl { v3 value; v3 dvalue; v3 ddvalue; } scl;
			EDynamicsChannels active; // The values that are not equal to default values
		};

		IAnimSource const& m_src;
		KinematicKeyFrameAnimation& m_out;

		CalcDynamics(IAnimSource const& src, KinematicKeyFrameAnimation& out)
			: m_src(src)
			, m_out(out)
		{}

		// Calculate the dynamics data for the given frame numbers
		void Run(std::span<int const> frames)
		{
			m_out.m_native_frame_rate = m_src.frame_rate();

			InitBoneMap();
			CopyFrames(frames);
			CalcBoneDynamics();
			CalcFCurveDynamics();
			CalcTCurveDynamics();
		}

		// Initialize the mapping from track index to bone index
		void InitBoneMap()
		{
			m_out.m_bone_map.resize(m_src.track_count());
			for (int i = 0, iend = isize(m_out.m_bone_map); i != iend; ++i)
				m_out.m_bone_map[i] = m_src.track_to_bone(i);
		}

		// Copy the frame indices and times to 'm_out'
		void CopyFrames(std::span<int const> frames)
		{
			// If no kinematic frames are given, then assume all frames from 'm_src' are kinematic frames
			if (frames.empty())
			{
				m_out.m_times.clear();
				m_out.m_fidxs.clear();
				m_out.m_key_count = m_src.key_count();
				return;
			}

			auto fps = s_cast<float>(m_src.frame_rate());

			m_out.m_key_count = isize(frames);
			m_out.m_times.resize(m_out.m_key_count);
			m_out.m_fidxs.resize(m_out.m_key_count);
			for (int i = 0; i != m_out.m_key_count; ++i)
			{
				auto fidx = frames[i];
				m_out.m_times[i] = fidx / fps;
				m_out.m_fidxs[i] = fidx;
			}
		}

		// Calculate positions, velocities, and accelerations for bones (linear and rotational)
		void CalcBoneDynamics()
		{
			auto key_count = m_src.key_count();
			auto track_count = m_src.track_count();
			auto kinematic_key_count = m_out.key_count();
			auto dt = s_cast<float>(1.0 / m_src.frame_rate());

			// Pre-allocate for parallel for
			auto count = key_count * track_count;
			m_out.m_rotation.resize(count);
			m_out.m_ang_vel.resize(count);
			m_out.m_ang_acc.resize(count);
			m_out.m_position.resize(count);
			m_out.m_lin_vel.resize(count);
			m_out.m_lin_acc.resize(count);
			m_out.m_scale.resize(count);

			// Detect unused channels
			std::atomic_int active_channels = 0;

			// Generate the frames for each bone
			auto track_range = std::ranges::views::iota(0, track_count);
			std::for_each(std::execution::par, track_range.begin(), track_range.end(), [&, this](int track_index)
			{
				auto q0 = quat::Identity();
				auto active = EDynamicsChannels::None;
				for (int k = 0; k != kinematic_key_count; ++k)
				{
					auto iframe = m_out.src_frame(k);

					// Sample the bone transforms at times that surround 'iframe'
					xform samples[5] = {};
					m_src.ReadTrackValues(track_index, iframe, samples);

					// Ensure shortest arcs
					for (auto& sample : samples)
					{
						if (Dot(q0, sample.rot) < 0)
							sample.rot = -sample.rot;

						q0 = sample.rot;
					}

					// Calculate dynamics for the frame
					auto dynamics = DynamicsFromFiniteDifference(samples, dt);

					// Where to write the data in the output
					auto j = k * track_count + track_index;

					// Record the dynamics values
					m_out.m_rotation[j] = dynamics.rot.value;
					m_out.m_ang_vel[j] = dynamics.rot.dvalue;
					m_out.m_ang_acc[j] = dynamics.rot.ddvalue;
					m_out.m_position[j] = dynamics.pos.value;
					m_out.m_lin_vel[j] = dynamics.pos.dvalue;
					m_out.m_lin_acc[j] = dynamics.pos.ddvalue;
					m_out.m_scale[j] = dynamics.scl.value;

					active |= dynamics.active;
				}

				active_channels.fetch_or((int)active);
			});

			// Resize unused channels to zero
			auto active = static_cast<EDynamicsChannels>(active_channels.load());
			if (!AllSet(active, EDynamicsChannels::Rotation)) m_out.m_rotation.resize(0);
			if (!AllSet(active, EDynamicsChannels::RotationD)) m_out.m_ang_vel.resize(0);
			if (!AllSet(active, EDynamicsChannels::RotationDD)) m_out.m_ang_acc.resize(0);
			if (!AllSet(active, EDynamicsChannels::Translation)) m_out.m_position.resize(0);
			if (!AllSet(active, EDynamicsChannels::TranslationD)) m_out.m_lin_vel.resize(0);
			if (!AllSet(active, EDynamicsChannels::TranslationDD)) m_out.m_lin_acc.resize(0);
			if (!AllSet(active, EDynamicsChannels::Scale)) m_out.m_scale.resize(0);
		}

		// Calculate values, derivatives, 2nd derivatives for float curves
		void CalcFCurveDynamics()
		{
			auto key_count = m_src.key_count();
			auto fcurve_count = m_src.fcurve_count();
			auto kinematic_key_count = m_out.key_count();
			auto dt = s_cast<float>(1.0 / m_src.frame_rate());

			// Pre-allocate for parallel for
			auto count = key_count * fcurve_count;
			m_out.m_fcurves.resize(count);

			// Detect unused channels
			std::atomic_int active_channels = 0;

			// Generate the frames for each float curve
			auto fcurve_range = std::ranges::views::iota(0, fcurve_count);
			std::for_each(std::execution::par, fcurve_range.begin(), fcurve_range.end(), [&, this](int curve_index)
			{
				auto active = EDynamicsChannels::None;
				for (int k = 0; k != kinematic_key_count; ++k)
				{
					auto iframe = m_out.src_frame(k);

					// Sample the float curve values at times that surround 'i'
					float samples[5] = {};
					m_src.ReadFCurveValues(curve_index, iframe, samples);

					// Calculate dynamics for the curve value
					auto dynamics = DynamicsFromFiniteDifference(samples, dt);

					// Where to write the data in the output
					auto j = k * fcurve_count + curve_index;

					// Record the dynamics values
					m_out.m_fcurves[j] = dynamics.value;

					active |= dynamics.active;
				}
			});

			// Resize unused channels to zero
			auto active = static_cast<EDynamicsChannels>(active_channels.load());
			if (!AllSet(active, EDynamicsChannels::Value)) m_out.m_fcurves.resize(0);
		}

		// Calculate values, derivatives, 2nd derivatives for transform curves
		void CalcTCurveDynamics()
		{
			auto key_count = m_src.key_count();
			auto tcurve_count = m_src.tcurve_count();
			auto kinematic_key_count = m_out.key_count();
			auto dt = s_cast<float>(1.0 / m_src.frame_rate());

			// Pre-allocate for parallel for
			auto count = key_count * tcurve_count;
			m_out.m_tcurves.resize(count);

			// Detect unused channels
			std::atomic_int active_channels = 0;

			// Generate the frames for each transform curve
			auto tcurve_range = std::ranges::views::iota(0, tcurve_count);
			std::for_each(std::execution::par, tcurve_range.begin(), tcurve_range.end(), [&, this](int curve_index)
			{
				auto active = EDynamicsChannels::None;
				for (int k = 0; k != kinematic_key_count; ++k)
				{
					auto iframe = m_out.src_frame(k);

					// Sample the transform curves at times that surround 'i'
					xform samples[5] = {};
					m_src.ReadTCurveValues(curve_index, iframe, samples);

					// Calculate dynamics for the curve value
					auto dynamics = DynamicsFromFiniteDifference(samples, dt);

					// Where to write the data in the output
					auto j = k * tcurve_count + curve_index;

					// Record the dynamics values
					m_out.m_tcurves[j] = xform{
						dynamics.rot.value,
						dynamics.pos.value.w1(),
						dynamics.scl.value.w1(),
					};

					active |= dynamics.active;
				}
			});

			// Resize unused channels to zero
			auto active = static_cast<EDynamicsChannels>(active_channels.load());
			if (!AllSet(active, EDynamicsChannels::Value)) m_out.m_tcurves.resize(0);
		}

		// Determine the dynamics values for 'samples[2]' because on the surrounding values
		static TransformDynamics DynamicsFromFiniteDifference(std::span<xform const> samples, float dt)
		{
			auto [rot0, rot1, rot2] = CalculateRotationalDynamics(samples, dt);
			auto [pos0, pos1, pos2] = CalculateTranslationalDynamics(samples, dt);
			auto [scl0, scl1, scl2] = CalculateScaleDynamics(samples, dt);

			constexpr auto tol0 = 0.0001f;
			constexpr auto tol1 = 0.0001f;
			constexpr auto tol2 = 0.001f;

			// Check for active channels
			auto active = EDynamicsChannels::None;
			if (!FEqlAbsolute(rot0, quat::Identity(), tol0)) active |= EDynamicsChannels::Value | EDynamicsChannels::Rotation;
			if (!FEqlAbsolute(rot1, v4::Zero(), tol1)) active |= EDynamicsChannels::ValueD | EDynamicsChannels::RotationD;
			if (!FEqlAbsolute(rot2, v4::Zero(), tol2)) active |= EDynamicsChannels::ValueDD | EDynamicsChannels::RotationDD;
			if (!FEqlAbsolute(pos0, v4::Zero(), tol0)) active |= EDynamicsChannels::Value | EDynamicsChannels::Translation;
			if (!FEqlAbsolute(pos1, v4::Zero(), tol1)) active |= EDynamicsChannels::ValueD | EDynamicsChannels::TranslationD;
			if (!FEqlAbsolute(pos2, v4::Zero(), tol2)) active |= EDynamicsChannels::ValueDD | EDynamicsChannels::TranslationDD;
			if (!FEqlAbsolute(scl0, v4::One() , tol0)) active |= EDynamicsChannels::Value | EDynamicsChannels::Scale;
			if (!FEqlAbsolute(scl1, v4::Zero(), tol1)) active |= EDynamicsChannels::ValueD | EDynamicsChannels::ScaleD;
			if (!FEqlAbsolute(scl2, v4::Zero(), tol2)) active |= EDynamicsChannels::ValueDD | EDynamicsChannels::ScaleDD;

			return TransformDynamics{
				{rot0,     rot1.xyz, rot2.xyz},
				{pos0.xyz, pos1.xyz, pos2.xyz},
				{scl0.xyz, scl1.xyz, scl2.xyz},
				active,
			};
		}

		// Determine the dynamics values for 'samples[2]' based on the surrounding values
		static ScalarDynamics DynamicsFromFiniteDifference(std::span<float const> samples, float dt)
		{
			auto [val0, val1, val2] = CalculateScalarDynamics(samples, dt);

			constexpr auto tol0 = 0.0001f;
			constexpr auto tol1 = 0.0001f;
			constexpr auto tol2 = 0.001f;

			// Check for active channels
			auto active = EDynamicsChannels::None;
			if (!FEqlAbsolute(val0, 0.0f, tol0)) active |= EDynamicsChannels::Value;
			if (!FEqlAbsolute(val1, 0.0f, tol1)) active |= EDynamicsChannels::ValueD;
			if (!FEqlAbsolute(val2, 0.0f, tol2)) active |= EDynamicsChannels::ValueDD;

			return ScalarDynamics{
				val0, val1, val2,
				active,
			};
		}

		#if 0
		// Transform the values in 'dynamics' into 'RootData.root_to_world' frame.
		void TransformToRoot(int pdp_index, int bone_index, ETransformSpace xform_space, TransformDynamics& dynamics)
		{
			auto current_to_root = float4x4::Identity();
			switch (xform_space)
			{
				case ETransformSpace::ParentRelative:
				{
					// Bone transforms are parent relative, so we only need to transform the root bone.
					if (bone_index == 0)
					{
						auto const& rd = m_pdp_collection.root_data[pdp_index];
						current_to_root = InvertOrthonormal(rd.root_to_animspace);
					}
					break;
				}
				case ETransformSpace::RootRelative:
				{
					// Bone transforms are all root bone relative, so we need to transform all dynamics values
					auto const& rd = m_pdp_collection.root_data[pdp_index];
					current_to_root = InvertOrthonormal(rd.root_to_animspace);
					break;
				}
				case ETransformSpace::AnimOriginRelative:
				default:
				{
					throw std::runtime_error(std::format("Unsupported transform space: {}", (int)xform_space));
				}
			}

			dynamics.rot.value = RotationFrom(current_to_root) * dynamics.rot.value;
			dynamics.rot.dvalue = (current_to_root * dynamics.rot.dvalue.w0()).xyz;
			dynamics.rot.ddvalue = (current_to_root * dynamics.rot.ddvalue.w0()).xyz;
			dynamics.pos.value = (current_to_root * dynamics.pos.value.w1()).xyz;
			dynamics.pos.dvalue = (current_to_root * dynamics.pos.dvalue.w0()).xyz;
			dynamics.pos.ddvalue = (current_to_root * dynamics.pos.ddvalue.w0()).xyz;
		}
		#endif
	};

	// --------------------------------------------------------------------------------------------
	
	// Interpolate between two key frames
	BoneKey Interp(BoneKey const& lhs, BoneKey const& rhs, float frac, EAnimInterpolation interp)
	{
		switch (interp)
		{
			case EAnimInterpolation::Constant: frac = 0; break;
			case EAnimInterpolation::Linear: frac = frac; break;
			case EAnimInterpolation::Cubic: frac = SmoothStep(0.0f, 1.0f, frac); break;
			default: throw std::runtime_error("Unknown interpolation style");
		}
		return BoneKey{
			.m_rot = Slerp(lhs.m_rot, rhs.m_rot, frac),
			.m_pos = Lerp(lhs.m_pos, rhs.m_pos, frac),
			.m_scl = Lerp(lhs.m_scl, rhs.m_scl, frac),
			.m_time = Lerp(lhs.m_time, rhs.m_time, frac),
		};
	}

	// Interpolate between two key frames
	KinematicKey Interp(KinematicKey const& lhs, KinematicKey const& rhs, float frac, EAnimInterpolation interp)
	{
		switch (interp)
		{
			case EAnimInterpolation::Constant: frac = 0; break;
			case EAnimInterpolation::Linear: frac = frac; break;
			case EAnimInterpolation::Cubic: frac = SmoothStep(0.0f, 1.0f, frac); break;
			default: throw std::runtime_error("Unknown interpolation style");
		}
		return KinematicKey{
			.m_rot = Slerp(lhs.m_rot, rhs.m_rot, frac),
			.m_pos = Lerp(lhs.m_pos, rhs.m_pos, frac),
			.m_scl = Lerp(lhs.m_scl, rhs.m_scl, frac),
			.m_lin_vel = Lerp(lhs.m_lin_vel, rhs.m_lin_vel, frac),
			.m_ang_vel = Slerp(lhs.m_ang_vel, rhs.m_ang_vel, frac),
			.m_lin_acc = Lerp(lhs.m_lin_acc, rhs.m_lin_acc, frac),
			.m_ang_acc = Slerp(lhs.m_ang_acc, rhs.m_ang_acc, frac),
			.m_time = Lerp(lhs.m_time, rhs.m_time, frac),
		};
	}

	// --------------------------------------------------------------------------------------------
	
	RootAnimation::RootAnimation()
		: m_vel(v4::Zero())
		, m_acc(v4::Zero())
		, m_avel(v4::Zero())
		, m_aacc(v4::Zero())
		, m_period(1.0)
		, m_style(EAnimStyle::NoAnimation)
	{
	}

	// Return a transform representing the offset added by this object at time 'time_s'
	m4x4 RootAnimation::EvaluateAtTime(double time_s) const
	{
		auto time = static_cast<float>(AdjTime(time_s, TimeRange{ 0.0, m_period }, m_style));
		auto lin = 0.5f * m_acc * Sqr(time) + m_vel * time + v4::Origin();
		auto ang = RotationAt(time, m3x4::Identity(), m_avel, m_aacc);
		return m4x4{ ang, lin };
	}

	// Ref-counting clean up function
	void RootAnimation::RefCountZero(RefCounted<RootAnimation>* doomed)
	{
		auto anim = static_cast<RootAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KeyFrameAnimation::KeyFrameAnimation(uint32_t skel_id, double duration, double native_frame_rate)
		: m_skel_id(skel_id)
		, m_native_duration(duration)
		, m_native_frame_rate(native_frame_rate)
		, m_bone_map()
		, m_rotation()
		, m_position()
		, m_scale()
	{}
	
	// Number of tracks in this animation
	int KeyFrameAnimation::track_count() const
	{
		return isize(m_bone_map);
	}
	
	// Number of float curves in this animation
	int KeyFrameAnimation::fcurve_count() const
	{
		return 0; // Future work
	}

	// Number of transform curves in this animation
	int KeyFrameAnimation::tcurve_count() const
	{
		return 0; // Future work
	}

	// Number of keys in this animation
	int KeyFrameAnimation::key_count() const
	{
		auto count = s_cast<int>(
			!m_rotation.empty() ? isize(m_rotation) :
			!m_position.empty() ? isize(m_position) :
			!m_scale   .empty() ? isize(m_scale   ) :
			track_count());

		assert(count % track_count() == 0 && "Expect track length to be a multiple of the track count");
		return count / track_count();
	}
	
	// The length (in seconds) of this animation
	double KeyFrameAnimation::duration() const
	{
		return m_native_duration;
	}

	// The effective frame rate implied by the duration and number of keys
	double KeyFrameAnimation::frame_rate() const
	{
		return m_native_frame_rate;
	}

	// Convert a time in seconds to a key index. Returns the key with time just less that 'time_s'
	int KeyFrameAnimation::TimeToKeyIndex(float time_s) const
	{
		auto kcount = key_count();
		if (kcount == 0 || duration() == 0)
			return 0;

		// Convert the time into a key number.
		// Note, by scaling 'm_duration' or 'time_s' the playback rate of the animation can be changed.
		auto tfrac = Frac<double>(0.0, time_s, duration());
		auto kidx = Lerp(0, kcount - 1, tfrac);
		return Clamp(kidx, 0, kcount - 1);
	}

	// Converts a key index to a time in seconds
	float KeyFrameAnimation::KeyIndexToTime(int key_index) const
	{
		auto kcount = key_count();
		auto period = s_cast<float>(1.0 / frame_rate());

		if (kcount == 0 || duration() == 0)
			return 0.0f;

		// Convert the key index to a time
		return std::clamp(key_index, 0, kcount - 1) * period;
	}

	// Read keys starting at 'frame' for all tracks. 'out' should be a multiple of the track count
	template <typename Key> requires (std::is_assignable_v<Key, BoneKey>)
	static void ReadKeysImpl(KeyFrameAnimation const& kfa, int key_idx, std::span<Key> out) 
	{
		auto tcount = kfa.track_count();
		auto kcount = kfa.key_count();
		auto period = s_cast<float>(1.0 / kfa.frame_rate());
		int i = 0;

		// Read in the same order as the keys are stored
		assert((isize(out) % tcount) == 0 && "Output size must be a multiple of the track count");
		for (int f = 0, fend = isize(out) / tcount; f != fend; ++f)
		{
			auto kidx = Clamp(key_idx + f, 0, kcount - 1);
			auto idx = kidx * tcount;

			for (int t = 0; t != tcount; ++t, ++idx)
			{
				out[i++] = BoneKey{
					.m_rot = Get(kfa.m_rotation, idx, quat::Identity()),
					.m_pos = Get(kfa.m_position, idx, v3::Zero()),
					.m_scl = Get(kfa.m_scale, idx, v3::One()),
					.m_time = kidx * period,
					.m_idx = kidx,
				};
			}
		}
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, std::span<BoneKey> out) const
	{
		ReadKeysImpl<BoneKey>(*this, key_idx, out);
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, std::span<xform> out) const
	{
		ReadKeysImpl<xform>(*this, key_idx, out);
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, std::span<m4x4> out) const
	{
		ReadKeysImpl<m4x4>(*this, key_idx, out);
	}

	// Read keys starting at 'frame' for the given 'track_index'. 'out.size()' is the number of keys to read
	template <typename Key> requires (std::is_assignable_v<Key, BoneKey>)
	static void ReadKeysImpl(KeyFrameAnimation const& kfa, int key_idx, int track_index, std::span<Key> out) 
	{
		auto tcount = kfa.track_count();
		auto kcount = kfa.key_count();
		auto period = s_cast<float>(1.0 / kfa.frame_rate());
		int i = 0;

		for (int f = 0, fend = isize(out); f != fend; ++f)
		{
			auto kidx = Clamp(key_idx + f, 0, kcount - 1);
			auto idx = kidx * tcount + track_index;

			out[i++] = BoneKey{
				.m_rot = Get(kfa.m_rotation, idx, quat::Identity()),
				.m_pos = Get(kfa.m_position, idx, v3::Zero()),
				.m_scl = Get(kfa.m_scale, idx, v3::One()),
				.m_time = kidx * period,
				.m_idx = kidx,
			};
		}
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, int track_index, std::span<BoneKey> out) const
	{
		ReadKeysImpl<BoneKey>(*this, key_idx, track_index, out);
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, int track_index, std::span<xform> out) const
	{
		ReadKeysImpl<xform>(*this, key_idx, track_index, out);
	}
	void KeyFrameAnimation::ReadKeys(int key_idx, int track_index, std::span<m4x4> out) const
	{
		ReadKeysImpl<m4x4>(*this, key_idx, track_index, out);
	}

	// Ref-counting clean up function
	void KeyFrameAnimation::RefCountZero(RefCounted<KeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KinematicKeyFrameAnimation::KinematicKeyFrameAnimation(uint32_t skel_id, double duration, double native_frame_rate)
		: m_skel_id(skel_id)
		, m_native_duration(duration)
		, m_native_frame_rate(native_frame_rate)
		, m_key_count()
		, m_bone_map()
		, m_rotation()
		, m_ang_vel()
		, m_ang_acc()
		, m_position()
		, m_lin_vel()
		, m_lin_acc()
		, m_scale()
		, m_times()
		, m_fidxs()
	{}
	KinematicKeyFrameAnimation::KinematicKeyFrameAnimation(KeyFrameAnimation const& kfa, std::span<int const> frames)
		:KinematicKeyFrameAnimation(kfa.m_skel_id, kfa.duration(), kfa.m_native_frame_rate)
	{
		struct AnimSource : IAnimSource
		{
			KeyFrameAnimation const& m_kfa;
			AnimSource(KeyFrameAnimation const& kfa) : m_kfa(kfa) {}
			int key_count() const noexcept { return m_kfa.key_count(); }
			int track_count() const noexcept { return m_kfa.track_count(); }
			int fcurve_count() const noexcept { return m_kfa.fcurve_count(); }
			int tcurve_count() const noexcept { return m_kfa.tcurve_count(); }
			double frame_rate() const noexcept override { return m_kfa.frame_rate(); }
			int key_to_frame(int key_index) const { return key_index; }
			uint16_t track_to_bone(int track_index) const { return m_kfa.m_bone_map[track_index]; }
			void ReadTrackValues(int track_index, int iframe, std::span<xform> samples) const
			{
				m_kfa.ReadKeys(iframe - 2, track_index, samples.subspan(0, 5));
			}
			void ReadFCurveValues(int track_index, int iframe, std::span<float> samples) const
			{
				(void)track_index, iframe, samples; // todo
			}
			void ReadTCurveValues(int track_index, int iframe, std::span<xform> samples) const
			{
				(void)track_index, iframe, samples; // todo
			}
		};

		AnimSource src = { kfa };
		CalcDynamics calc(src, *this);
		calc.Run(frames);
	}
	
	// Number of tracks in this animation
	int KinematicKeyFrameAnimation::track_count() const
	{
		return isize(m_bone_map);
	}

	// Number of float curves in this animation
	int KinematicKeyFrameAnimation::fcurve_count() const
	{
		return isize(m_fcurve_ids);
	}

	// Number of transform curves in this animation
	int KinematicKeyFrameAnimation::tcurve_count() const
	{
		return isize(m_tcurve_ids);
	}
	
	// Number of keys in this animation
	int KinematicKeyFrameAnimation::key_count() const
	{
		return m_key_count;
	}

	// Get the original frame number for the given key index
	int KinematicKeyFrameAnimation::src_frame(int key_index) const
	{
		assert(key_index >= 0 && key_index < key_count());
		return Get(m_fidxs, key_index, key_index);
	}

	// The length (in seconds) of this animation
	double KinematicKeyFrameAnimation::duration() const
	{
		return m_native_duration;
	}

	// The effective frame rate implied by the duration and number of keys
	double KinematicKeyFrameAnimation::frame_rate() const
	{
		return m_native_frame_rate;
	}

	// Convert a time in seconds to a key index. Returns the key with time just less that 'time_s'
	int KinematicKeyFrameAnimation::TimeToKeyIndex(float time_s) const
	{
		auto kcount = key_count();
		if (kcount <= 1 || duration() == 0)
			return 0;

		// If using a fixed frame rate, we can directly compute the key index.
		// If the kinematic keys are sparce, we need to binary search for the surrounding keys
		// Note, by scaling 'm_duration' or 'time_s' the playback rate of the animation can be changed.
		if (m_times.empty())
		{
			auto tfrac = Frac<double>(0.0, time_s, duration());
			auto kidx = Lerp(0, kcount - 1, tfrac);
			return Clamp(kidx, 0, kcount - 1);
		}
		else
		{
			auto it = std::ranges::upper_bound(m_times, time_s);
			auto kidx = s_cast<int>(std::distance(m_times.begin(), it) - 1);
			return Clamp(kidx, 0, kcount - 1);
		}
	}

	// Converts a key index to a time in seconds
	float KinematicKeyFrameAnimation::KeyIndexToTime(int key_index) const
	{
		auto kcount = key_count();
		if (kcount == 0 || duration() == 0)
			return 0.0f;

		key_index = Clamp(key_index, 0, kcount - 1);

		// If using a fixed frame rate, we can directly compute the key time
		if (m_times.empty())
		{
			// Convert the key index to a time
			auto period = s_cast<float>(1.0 / frame_rate());
			return key_index * period;
		}
		else
		{
			return m_times[key_index];
		}
	}

	// Read keys starting at 'frame' for all tracks. 'out' should be a multiple of the track count
	template <typename Key> requires (std::is_assignable_v<Key, KinematicKey>)
	static void ReadKeysImpl(KinematicKeyFrameAnimation const& kkfa, int key_idx, std::span<Key> out) 
	{
		auto tcount = kkfa.track_count();
		auto kcount = kkfa.key_count();
		auto period = s_cast<float>(1.0 / kkfa.frame_rate());
		int i = 0;

		// Read in the same order as the keys are stored
		assert((isize(out) % tcount) == 0 && "Output size must be a multiple of the track count");
		for (int f = 0, fend = isize(out) / tcount; f != fend; ++f)
		{
			auto kidx = Clamp(key_idx + f, 0, kcount - 1);
			auto idx = kidx * tcount;

			for (int t = 0; t != tcount; ++t, ++idx)
			{
				out[i++] =  KinematicKey {
					.m_rot = Get(kkfa.m_rotation, idx, quat::Identity()),
					.m_pos = Get(kkfa.m_position, idx, v3::Zero()),
					.m_scl = Get(kkfa.m_scale, idx, v3::One()),
					.m_lin_vel = Get(kkfa.m_lin_vel, idx, v3::Zero()),
					.m_ang_vel = Get(kkfa.m_ang_vel, idx, v3::Zero()),
					.m_lin_acc = Get(kkfa.m_lin_acc, idx, v3::Zero()),
					.m_ang_acc = Get(kkfa.m_ang_acc, idx, v3::Zero()),
					.m_time = Get(kkfa.m_times, kidx, kidx * period),
					.m_idx = kidx,
				};
			}
		}
	}
	void KinematicKeyFrameAnimation::ReadKeys(int key_idx, std::span<KinematicKey> out) const
	{
		ReadKeysImpl<KinematicKey>(*this, key_idx, out);
	}
	void KinematicKeyFrameAnimation::ReadKeys(int key_idx, std::span<xform> out) const
	{
		ReadKeysImpl<xform>(*this, key_idx, out);
	}

	// Read keys starting at 'frame' for all tracks. 'out' should be a multiple of the track count
	template <typename Key> requires (std::is_assignable_v<Key, KinematicKey>)
	static void ReadKeysImpl(KinematicKeyFrameAnimation const& kkfa, int key_idx, int track_index, std::span<Key> out) 
	{
		auto tcount = kkfa.track_count();
		auto kcount = kkfa.key_count();
		auto period = s_cast<float>(1.0 / kkfa.frame_rate());
		int i = 0;

		// Read in the same order as the keys are stored
		assert((isize(out) % tcount) == 0 && "Output size must be a multiple of the track count");
		for (int f = 0, fend = isize(out) / tcount; f != fend; ++f)
		{
			auto kidx = Clamp(key_idx + f, 0, kcount - 1);
			auto idx = kidx * tcount + track_index;

			out[i++] =  KinematicKey {
				.m_rot = Get(kkfa.m_rotation, idx, quat::Identity()),
				.m_pos = Get(kkfa.m_position, idx, v3::Zero()),
				.m_scl = Get(kkfa.m_scale, idx, v3::One()),
				.m_lin_vel = Get(kkfa.m_lin_vel, idx, v3::Zero()),
				.m_ang_vel = Get(kkfa.m_ang_vel, idx, v3::Zero()),
				.m_lin_acc = Get(kkfa.m_lin_acc, idx, v3::Zero()),
				.m_ang_acc = Get(kkfa.m_ang_acc, idx, v3::Zero()),
				.m_time = Get(kkfa.m_times, kidx, kidx * period),
				.m_idx = kidx,
			};
		}
	}
	void KinematicKeyFrameAnimation::ReadKeys(int key_idx, int track_index, std::span<KinematicKey> out) const
	{
		ReadKeysImpl<KinematicKey>(*this, key_idx, track_index, out);
	}
	void KinematicKeyFrameAnimation::ReadKeys(int key_idx, int track_index, std::span<xform> out) const
	{
		ReadKeysImpl<xform>(*this, key_idx, track_index, out);
	}

	// Ref-counting clean up function
	void KinematicKeyFrameAnimation::RefCountZero(RefCounted<KinematicKeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KinematicKeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	double AdjTime(double time_s, TimeRange time_range, EAnimStyle style)
	{
		// Relative time
		auto rtime_s = time_s - time_range.begin();

		// Wrap time into the track's time range
		switch (style)
		{
			case EAnimStyle::NoAnimation:
			{
				rtime_s = 0.0;
				break;
			}
			case EAnimStyle::Once:
			{
				rtime_s = Clamp(rtime_s, 0.0, time_range.size());
				break;
			}
			case EAnimStyle::Repeat:
			{
				rtime_s = Wrap(rtime_s, 0.0, time_range.size());
				break;
			}
			case EAnimStyle::Continuous:
			{
				rtime_s = rtime_s;
				break;
			}
			case EAnimStyle::PingPong:
			{
				rtime_s = Wrap(rtime_s, 0.0, 2.0 * time_range.size());
				rtime_s = rtime_s >= time_range.size() ? 2.0 * time_range.size() - rtime_s : rtime_s;
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown animation style");
			}
		}
		
		// Convert the wrapped time back to absolute time
		return rtime_s + time_range.begin();
	}
}
