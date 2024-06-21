#include "forward.h"
#include "synth.h"

using namespace allkeys;

namespace allkeys
{
	// Global synth instances
	std::vector<std::unique_ptr<Synth>> g_synths;

	// Convert a handle to a synth instance pointer
	inline Synth* get(jlong handle)
	{
		return reinterpret_cast<Synth*>(handle);
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



// Create a synth instance
extern "C" JNIEXPORT jlong JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_createSynth(JNIEnv* env, jobject obj)
{
	return TryCatch<jlong>([]
	{
		g_synths.emplace_back(new Synth());
		return reinterpret_cast<jlong>(g_synths.back().get());
	}, "Failed to create synth");
}

// Destroy the synth instance
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_destroySynth(JNIEnv* env, jobject obj, jlong handle)
{
	TryCatch([handle]
	{
		auto synth = get(handle);
		auto it = std::remove_if(g_synths.begin(), g_synths.end(), [synth](const auto& s) { return s.get() == synth; });
		g_synths.erase(it, g_synths.end());
	}, "Failed to destroy synth");
}

// Load a soundfont
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_loadSoundFont(JNIEnv* env, jobject obj, jlong handle, jstring sf_path)
{
	TryCatch([=]
	{
		jni_string path(env, sf_path);
		get(handle)->LoadSoundFont(path);
	}, "Loading soundfont failed");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_playNote(JNIEnv* env, jobject obj, jlong handle, jint channel, jint key, jint velocity)
{
	TryCatch([=]
	{
		get(handle)->NoteOn(channel, key, velocity);
	}, "Failed to play note");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_stopNote(JNIEnv* env, jobject obj, jlong handle, jint channel, jint key)
{
	TryCatch([=]
	{
		get(handle)->NoteOff(channel, key);
	}, "Failed to stop note");
}

// Get/Set Master gain
extern "C" JNIEXPORT jfloat  JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_masterGainGet(JNIEnv* env, jobject obj, jlong handle)
{
	return TryCatch<jfloat>([=]
	{
		return get(handle)->MasterGain();
	}, "Failed to get master gain", 0.0f);
}
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_masterGainSet(JNIEnv* env, jobject obj, jlong handle, jfloat gain)
{
	TryCatch([=]
	{
		get(handle)->MasterGain(gain);
	}, "Failed to set master gain");
}
