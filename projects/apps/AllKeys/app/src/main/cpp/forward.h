#pragma once

#include <set>
#include <memory>
#include <string>
#include <string_view>
#include <sstream>
#include <concepts>
#include <type_traits>
#include <cstdint>
#include <span>

#include <jni.h>
#include <android/log.h>

#include "id_type.h"
#include "jni_string.h"
#include "jni_field.h"
#include "jni_bytearray.h"

// If missing, you need to run the project setup script first. That will download the
// fluidsynth SDK and copy files into the correct location.
#include "fluidsynth.h"

// See: https://github.com/FluidSynth/fluidsynth
// Documentation: https://www.fluidsynth.org/api/index.html

namespace allkeys
{
	using midi_key_t = Id<int16_t, 0, 127>;
	using midi_channel_t = Id<int16_t, 0, 0x10>;
	using midi_velocity_t = Id<int16_t, 0, 127>;
	using milliseconds_t = uint32_t;
	using synth_handle_t = jlong;
	using seq_handle_t = jlong;
	using event_handle_t = jlong;
	using player_handle_t = jlong;

	class Synth;
	class Sequencer;
	class Event;
	class Player;

	// Check for fluidsynth errors
	inline int Check(int result, std::string_view message)
	{
		if (result >= FLUID_OK) return result;
		throw std::runtime_error(std::string{message});
	}

	// Convert to string
	template<typename T> inline std::string ToString(const T& arg)
	{
		if constexpr (std::is_same_v<T, std::string>)
		{
			return arg;
		}
		else if constexpr (std::is_arithmetic_v<T>)
		{
			return std::to_string(arg);
		}
		else
		{
			std::ostringstream oss;
			oss << arg;
			return oss.str();
		}
	}
	template<typename... Args> inline std::string StrJoin(Args... args) {
		std::string str;
		(str.append(ToString(args)), ...);
		return str;
	}

	// Common handling of exceptions at the dll interface
	template <typename Ret, typename Func> requires std::is_invocable_r_v<Ret, Func>
	auto TryCatch(Func fn, std::string_view message, Ret error_result = {}) -> Ret
	{
		try
		{
			return fn();
		}
		catch (std::exception const& ex)
		{
			__android_log_print(ANDROID_LOG_ERROR, "%s: %s", message.data(), ex.what());
			return error_result;
		}
		catch (...)
		{
			auto ex = std::runtime_error("Unknown exception");
			__android_log_print(ANDROID_LOG_ERROR, "%s: %s", message.data(), ex.what());
			return error_result;
		}
	}
	template <typename Func> requires std::is_invocable_v<Func>
	void TryCatch(Func fn, std::string_view message)
	{
		auto fn_ = [&] { fn(); return 0; };
		TryCatch<int>(fn_, message, 0);
	}
}